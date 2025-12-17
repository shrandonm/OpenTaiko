using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using System.Numerics;

namespace OpenTaiko.Shrandy.Tools
{
	internal class NoteVisualizer : Tool
	{
		private List<HitParams> m_HitHistory = new();

		public NoteVisualizer(string toolName, SlimDXKeys.Key enableHotkey)
			: base(toolName, enableHotkey)
		{
		}

		public override void OnNoteHit(HitParams hitParams)
		{
			m_HitHistory.Add(hitParams);
		}

		public override void OnStageChanged(CStage stage)
		{
			m_HitHistory.Clear();
		}

		public override void Draw()
		{
			float[] markers = { 1, 5, 9.2f, 14, 20, 45 };
			float currentT = 9.2f;
			DrawTimelineWidget("##tl", 0, 60, markers, ref currentT);
		}
		static float sZoom = 1.0f;   // 1 = fit
		static float sPan = 0.0f;    // time offset in seconds
		static int sSelected = -1;

		static void DrawTimelineWidget(
			string id,
			float baseMin, float baseMax,
			Span<float> markers, // mutable if you later want drag
			ref float currentT)
		{
			float height = 100.0f;
			float width = ImGui.GetContentRegionAvail().X;

			var cursorPos = ImGui.GetCursorScreenPos();

			ImGui.InvisibleButton(id, new Vector2(width, height),
				ImGuiButtonFlags.MouseButtonLeft |
				ImGuiButtonFlags.MouseButtonMiddle);

			var dl = ImGui.GetWindowDrawList();

			// Background
			var p1 = new Vector2(cursorPos.X + width, cursorPos.Y + height);
			dl.AddRectFilled(cursorPos, p1, ImGui.GetColorU32(ImGuiCol.FrameBg), 6);

			float padX = 12f;
			float yMid = (cursorPos.Y + p1.Y) * 0.5f;
			float x0 = cursorPos.X + padX;
			float x1 = p1.X - padX;

			// Visible range from base + pan/zoom
			float baseSpan = baseMax - baseMin;
			float span = baseSpan / Math.Max(0.0001f, sZoom);
			float vMin = baseMin + sPan;
			float vMax = vMin + span;

			float TFromX(float x)
			{
				float u = (x - x0) / (x1 - x0);
				return vMin + u * (vMax - vMin);
			}

			float XFromT(float t)
			{
				float u = (t - vMin) / (vMax - vMin);
				return x0 + u * (x1 - x0);
			}

			// Zoom with wheel (around mouse)
			if (ImGui.IsItemHovered())
			{
				float wheel = ImGui.GetIO().MouseWheel;
				if (wheel != 0.0f)
				{
					var mp = ImGui.GetMousePos();
					float tAtMouse = TFromX(mp.X);

					float zoomFactor = (wheel > 0) ? 1.1f : 1.0f / 1.1f;
					sZoom = Math.Clamp(sZoom * zoomFactor, 1.0f, 100.0f);

					// Keep tAtMouse anchored
					float newSpan = baseSpan / sZoom;
					float newVMin = tAtMouse - ((mp.X - x0) / (x1 - x0)) * newSpan;
					sPan = newVMin - baseMin;
				}
			}

			// Pan with middle-drag
			if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
			{
				float dx = ImGui.GetIO().MouseDelta.X;
				float dt = -dx / (x1 - x0) * (vMax - vMin);
				sPan += dt;
			}

			// Clamp pan to base range
			sPan = Math.Clamp(sPan, 0f, Math.Max(0f, baseSpan - baseSpan / sZoom));

			// Draw bar
			dl.AddLine(new Vector2(x0, yMid), new Vector2(x1, yMid), ImGui.GetColorU32(ImGuiCol.Separator), 2.0f);

			// Click select nearest marker
			if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
			{
				float tClick = TFromX(ImGui.GetMousePos().X);
				int best = -1;
				float bestAbs = float.MaxValue;
				for (int i = 0; i < markers.Length; i++)
				{
					float d = MathF.Abs(markers[i] - tClick);
					if (d < bestAbs) { bestAbs = d; best = i; }
				}
				sSelected = best;
				if (sSelected != -1) currentT = markers[sSelected];
			}

			// Current time line
			float cx = XFromT(currentT);
			dl.AddLine(new Vector2(cx, cursorPos.Y + 4), new Vector2(cx, p1.Y - 4),
				ImGui.GetColorU32(ImGuiCol.PlotLines), 2.0f);

			const float circleRadius = 16.0f;
			const int circleSegments = 16;
			const float circleThickness = 2.0f;
			uint donColour = Utilities.ColourHelper.GetDonImGuiColour();
			uint kaColour = Utilities.ColourHelper.GetKaImGuiColour();
			const uint outlineColour = 0xFFFFFFFF;

			for (int i = 0; i < markers.Length; i++)
			{
				float t = markers[i];
				if (t < vMin || t > vMax)
				{
					continue;
				}

				float x = XFromT(t);
				var circleCenter = new Vector2(x, yMid);

				uint fillColour = (i % 2 == 0) ? donColour : kaColour;
				dl.AddCircleFilled(circleCenter, circleRadius, fillColour, circleSegments);
				dl.AddCircle(circleCenter, circleRadius, outlineColour, circleSegments, circleThickness);

				circleCenter.Y += 32.0f;
				circleCenter.X += 10.0f;
				fillColour <<= 2;
				fillColour >>= 2;
				dl.AddCircleFilled(circleCenter, circleRadius, fillColour, circleSegments);
				dl.AddCircle(circleCenter, circleRadius, outlineColour, circleSegments, circleThickness);
			}

			// Optional tooltip
			if (ImGui.IsItemHovered())
			{
				float tHover = TFromX(ImGui.GetMousePos().X);
				ImGui.BeginTooltip();
				ImGui.Text($"t = {tHover:0.###}");
				ImGui.EndTooltip();
			}
		}
	}
}
