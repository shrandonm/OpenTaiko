using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	// Hit error bar modelled after the Simply Love "Colorful" error bar.
	// Shows colored judgment-window bands and fading tick marks at each hit offset.
	internal class HitErrorBarTool : Tool
	{
		private const float TickDurationMs  = 500.0f;
		private const int   MaxTicks = 10;
		private const float BandAlpha = 0.3f;

		// Colors in ImGui ABGR format (0xAABBGGRR).
		private const uint ColorGoodFull = 0xFF20C820; // Green – RGBA (  32, 200,  32, 255)
		private const uint ColorOkayFull = 0xFF00D7FF; // Yellow  – RGBA (255, 215,   0, 255)
		private const uint ColorBadFull  = 0xFF3232DC; // Red   – RGBA ( 220,  50,  50, 255)
		private const uint ColorBandBg   = 0xCC000000; // Semi-transparent black background
		private const uint ColorCenter   = 0xFFFFFFFF; // White center line

		private struct Tick
		{
			public float OffsetMs;
			public uint FullAlphaColor;
			public long SpawnMs;
		}

		private Queue<Tick> m_Ticks = new(MaxTicks);

		public HitErrorBarTool(string toolName, SlimDXKeys.Key enableHotkey)
			: base(toolName, enableHotkey)
		{
		}

		// Returns a copy of 'color' with its alpha channel replaced by 'normalizedAlpha' [0..1].
		private static uint WithAlpha(uint color, float normalizedAlpha)
		{
			byte alpha = (byte)(Math.Clamp(normalizedAlpha, 0f, 1f) * 255);
			return (color & 0x00FFFFFF) | ((uint)alpha << 24);
		}

		public override void OnNoteHit(HitParams hitParams)
		{
			base.OnNoteHit(hitParams);

			if (hitParams.Chip == null) return;
			if (hitParams.JudgeResult == ENoteJudge.Miss) return;

			uint tickColor = ShrandyExtension.IsGood(hitParams.JudgeResult) ? ColorGoodFull
				: ShrandyExtension.IsOkay(hitParams.JudgeResult) ? ColorOkayFull
				: ColorBadFull;

			if (m_Ticks.Count >= MaxTicks)
				m_Ticks.Dequeue();

			long now = Environment.TickCount64;
			m_Ticks.Enqueue(new Tick
			{
				OffsetMs = -(float)hitParams.HitErrorMs,
				FullAlphaColor = tickColor,
				SpawnMs = now,
			});
		}

		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);
			m_Ticks.Clear();
			if (stage is CStage演奏ドラム画面)
			{
				SetEnabled(true);
			}
			else
			{
				SetEnabled(false);
			}
		}

		public override void OnSongRestart()
		{
			base.OnSongRestart();
			m_Ticks.Clear();
		}

		public override void DrawWindow()
		{
			Update();
			if (!Enabled) return;

			var flags = ImGuiWindowFlags.NoTitleBar
					  | ImGuiWindowFlags.NoScrollbar
					  | ImGuiWindowFlags.NoScrollWithMouse
					  | ImGuiWindowFlags.NoCollapse;

			ImGui.SetNextWindowPos(new Vector2(500, 400), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new Vector2(320, 50), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSizeConstraints(new Vector2(160, 36), new Vector2(float.MaxValue, 200));
			ImGui.SetNextWindowBgAlpha(0f); // We draw our own background.

			// Zero padding so bar content fills edge-to-edge.
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
			// No ref-open overload → no close button.
			bool opened = ImGui.Begin("##HitErrorBar", flags);
			ImGui.PopStyleVar();

			if (opened)
			{
				var windowPos  = ImGui.GetWindowPos();
				var windowSize = ImGui.GetWindowSize();
				var drawList   = ImGui.GetWindowDrawList();

				// ── Error bar ─────────────────────────────────────────────────
				float barTop     = windowPos.Y;
				float barBottom  = windowPos.Y + windowSize.Y;
				float barLeft    = windowPos.X;
				float barRight   = windowPos.X + windowSize.X;
				float barCenterX = (barLeft + barRight) * 0.5f;
				float halfBarW   = (barRight - barLeft) * 0.5f;

				float goodMs = (float)OpenTaiko.ConfigIni.nHitRangeMs.Perfect; // 25 ms
				float okayMs = (float)OpenTaiko.ConfigIni.nHitRangeMs.Good;   // 75 ms
				float badMs  = (float)OpenTaiko.ConfigIni.nHitRangeMs.Poor;   // 108 ms
				float scale  = halfBarW / badMs; // px per ms

				long nowMs = Environment.TickCount64;

				// Background
				drawList.AddRectFilled(
					new Vector2(barLeft, barTop),
					new Vector2(barRight, barBottom),
					ColorBandBg);

				// Bad window bands (outer segments: Okay edge → Bad edge)
				DrawBandPair(drawList, barCenterX, barTop, barBottom, okayMs, badMs, scale, WithAlpha(ColorBadFull,  BandAlpha));

				// Okay window bands (middle segments: Good edge → Okay edge)
				DrawBandPair(drawList, barCenterX, barTop, barBottom, goodMs, okayMs, scale, WithAlpha(ColorOkayFull, BandAlpha));

				// Good window bands (inner segments: center → Good edge)
				DrawBandPair(drawList, barCenterX, barTop, barBottom, 0f, goodMs, scale, WithAlpha(ColorGoodFull, BandAlpha));

				// Center line
				drawList.AddLine(
					new Vector2(barCenterX, barTop + 1f),
					new Vector2(barCenterX, barBottom - 1f),
					ColorCenter, 1.5f);

				// Ticks (fade out over TickDurationMs)
				while (m_Ticks.Count > 0 && (nowMs - m_Ticks.Peek().SpawnMs) >= TickDurationMs)
					m_Ticks.Dequeue();

				foreach (Tick tick in m_Ticks)
				{
					float ageMs = (float)(nowMs - tick.SpawnMs);
					float alpha = 1f - (ageMs / TickDurationMs);
					float tickX = Math.Clamp(barCenterX + tick.OffsetMs * scale, barLeft, barRight);

					drawList.AddLine(
						new Vector2(tickX, barTop + 1f),
						new Vector2(tickX, barBottom - 1f),
						WithAlpha(tick.FullAlphaColor, alpha),
						2f);
				}

				// Outer border: black outline then white highlight on top
				drawList.AddRect(
					new Vector2(barLeft - 1f, barTop - 1f),
					new Vector2(barRight + 1f, barBottom + 1f),
					0xFF000000, 0f, ImDrawFlags.None, 8f);
			}
			ImGui.End();
		}

		private static void DrawBandPair(
			ImDrawListPtr drawList,
			float centerX, float top, float bottom,
			float innerMs, float outerMs, float scale,
			uint color)
		{
			float innerPx = innerMs * scale;
			float outerPx = outerMs * scale;

			// Early (left) band
			drawList.AddRectFilled(
				new Vector2(centerX - outerPx, top),
				new Vector2(centerX - innerPx, bottom),
				color);

			// Late (right) band
			drawList.AddRectFilled(
				new Vector2(centerX + innerPx, top),
				new Vector2(centerX + outerPx, bottom),
				color);
		}
	}
}
