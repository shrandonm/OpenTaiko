using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	/// <summary>
	/// Renders the hit-timing distribution bar graph shown in the Results tool window.
	/// </summary>
	internal static class HitDistributionGraph
	{
		// ── Constants ─────────────────────────────────────────────────────────────
		private const int   BucketMs   = 3;      // ms span per bar; center bucket covers {-1, 0, +1}
		private const float GraphH     = 120.0f;
		private const float GraphW     = 500.0f;
		private const float StatsH     = 44.0f;
		private const float LabelAreaH = 18.0f;
		private const uint  ColorGood  = 0xFF20C820;
		private const uint  ColorOkay  = 0xFF00D7FF;
		private const uint  ColorBad   = 0xFF3232DC;

		// ── Data types ────────────────────────────────────────────────────────────

		private struct HitStats
		{
			public float MeanAbsMs, MeanMs, StdDev;
			public int   MaxAbsMs;
		}

		/// <summary>
		/// Shared rendering state built once per frame and threaded through all draw helpers.
		/// Using a class avoids copying the ImDrawListPtr and bucket array on every call.
		/// </summary>
		private sealed class RenderContext
		{
			public ImDrawListPtr DrawList;
			public Vector2       Cursor;
			public float         BarW;
			public int           HalfBuckets;
			public int           NumBuckets;
			public int[]         Buckets = Array.Empty<int>();
			public int           MaxCount;
			public int           GoodMs;
			public int           OkayMs;
			public float         GoodXN, GoodX, OkayXN, OkayX, CenterX;
		}

		// ── Public entry point ────────────────────────────────────────────────────

		public static void Draw(Dictionary<int, int>? distribution)
		{
			if (distribution == null || distribution.Count == 0)
			{
				return;
			}

			ImGui.Separator();

			HitStats stats = ComputeStats(distribution);
			DrawStatsHeader(stats);

			RenderContext context = BuildRenderContext(distribution);
			if (context.MaxCount == 0)
			{
				return;
			}

			BarGraph.Bar[] bars = BuildBars(context);
			BarGraph.Draw(bars, BuildBarGraphOptions());

			DrawOverlays(context);
			DrawZoneLabels(context);
		}

		// ── Stats computation ─────────────────────────────────────────────────────

		private static HitStats ComputeStats(Dictionary<int, int> distribution)
		{
			long   totalHits = 0;
			double sumAbsMs  = 0, sumMs = 0;
			int    rawMaxAbs = 0;

			foreach (var kvp in distribution)
			{
				totalHits += kvp.Value;
				sumAbsMs  += (double)Math.Abs(kvp.Key) * kvp.Value;
				sumMs     += (double)kvp.Key           * kvp.Value;
				rawMaxAbs  = Math.Max(rawMaxAbs, Math.Abs(kvp.Key));
			}

			float meanAbsMs = totalHits > 0 ? (float)(sumAbsMs / totalHits) : 0f;
			float meanMs    = totalHits > 0 ? (float)(sumMs    / totalHits) : 0f;

			double sumSqDiff = 0;
			foreach (var kvp in distribution)
				sumSqDiff += (kvp.Key - meanMs) * (kvp.Key - meanMs) * kvp.Value;
			float stdDev = totalHits > 1 ? MathF.Sqrt((float)(sumSqDiff / totalHits)) : 0f;

			return new HitStats
			{
				MeanAbsMs = meanAbsMs,
				MeanMs    = meanMs,
				StdDev    = stdDev,
				MaxAbsMs  = rawMaxAbs,
			};
		}

		// ── Render context construction ───────────────────────────────────────────

		private static RenderContext BuildRenderContext(Dictionary<int, int> distribution)
		{
			int goodMs    = OpenTaiko.ConfigIni.nHitRangeMs.Perfect;
			int okayMs    = OpenTaiko.ConfigIni.nHitRangeMs.Good;
			int maxExtent = OpenTaiko.ConfigIni.nHitRangeMs.Poor;

			int minKey = distribution.Keys.Min();
			int maxKey = distribution.Keys.Max();
			int extent = Math.Min(Math.Max(Math.Abs(minKey), Math.Abs(maxKey)), maxExtent);
			extent = Math.Max(extent, BucketMs);

			int halfBuckets = (extent + BucketMs - 1) / BucketMs;
			int numBuckets  = halfBuckets * 2 + 1;

			var buckets = BuildBuckets(distribution, halfBuckets, numBuckets);

			Vector2 cursor = ImGui.GetCursorScreenPos();
			float   barW   = GraphW / numBuckets;

			float BarLeftEdge(int offset) => cursor.X + (halfBuckets + offset) * barW;

			return new RenderContext
			{
				DrawList    = ImGui.GetWindowDrawList(),
				Cursor      = cursor,
				BarW        = barW,
				HalfBuckets = halfBuckets,
				NumBuckets  = numBuckets,
				Buckets     = buckets,
				MaxCount    = buckets.Max(),
				GoodMs      = goodMs,
				OkayMs      = okayMs,
				GoodXN      = BarLeftEdge(-(goodMs / BucketMs)),
				GoodX       = BarLeftEdge(goodMs  / BucketMs + 1),
				OkayXN      = BarLeftEdge(-(okayMs / BucketMs)),
				OkayX       = BarLeftEdge(okayMs  / BucketMs + 1),
				CenterX     = cursor.X + halfBuckets * barW + barW * 0.5f,
			};
		}

		private static int[] BuildBuckets(Dictionary<int, int> distribution, int halfBuckets, int numBuckets)
		{
			var buckets = new int[numBuckets];
			foreach (var kvp in distribution)
			{
				int absV = Math.Abs(kvp.Key);
				int bi   = (absV + 1) / BucketMs;   // 0,1→0  2,3,4→1  5,6,7→2 ...
				if (kvp.Key < 0) bi = -bi;
				int idx = bi + halfBuckets;
				if (idx >= 0 && idx < numBuckets)
					buckets[idx] += kvp.Value;
			}
			return buckets;
		}

		// ── Draw helpers ──────────────────────────────────────────────────────────

		private static void DrawStatsHeader(HitStats stats)
		{
			var     drawList = ImGui.GetWindowDrawList();
			Vector2 pos      = ImGui.GetCursorScreenPos();
			float   colW     = GraphW / 4f;
			float   textH    = ImGui.GetTextLineHeight();

			drawList.AddRectFilled(pos, new Vector2(pos.X + GraphW, pos.Y + StatsH), 0xDD111111);

			(string label, string value)[] columns =
			{
				("mean abs error",               $"{stats.MeanAbsMs:F1}ms"),
				("mean",                         $"{stats.MeanMs:F1}ms"),
				("std dev * 10 (Unstable Rate)", $"{stats.StdDev * 10f:F1}ms"),
				("max error",                    $"{stats.MaxAbsMs}ms"),
			};

			for (int c = 0; c < columns.Length; c++)
			{
				float cx = pos.X + c * colW + colW * 0.5f;
				float lw = ImGui.CalcTextSize(columns[c].label).X;
				float vw = ImGui.CalcTextSize(columns[c].value).X;
				drawList.AddText(new Vector2(cx - lw * 0.5f, pos.Y + 4f),                  0xFFAAAAAA, columns[c].label);
				drawList.AddText(new Vector2(cx - vw * 0.5f, pos.Y + StatsH - textH - 4f), 0xFFFFFFFF, columns[c].value);
			}

			ImGui.Dummy(new Vector2(GraphW, StatsH));
		}

		private static BarGraph.Bar[] BuildBars(RenderContext context)
		{
			BarGraph.Bar[] bars = new BarGraph.Bar[context.NumBuckets];
			for (int i = 0; i < bars.Length; i++)
			{
				int bucketMs = (i - context.HalfBuckets) * BucketMs;
				int absoluteMs = Math.Abs(bucketMs);
				uint color = absoluteMs <= context.GoodMs ? ColorGood
				           : absoluteMs <= context.OkayMs ? ColorOkay
				           : ColorBad;

				bars[i] = new BarGraph.Bar
				{
					Value   = context.Buckets[i],
					Color   = color,
					Tooltip = BuildBucketTooltip(i, context),
				};
			}
			return bars;
		}

		private static string BuildBucketTooltip(int bucketIndex, RenderContext context)
		{
			int bucketOffset     = bucketIndex - context.HalfBuckets;
			int lowMilliseconds  = bucketOffset * BucketMs - 1;
			int highMilliseconds = bucketOffset * BucketMs + 1;
			int count            = context.Buckets[bucketIndex];
			string FormatMilliseconds(int milliseconds) => milliseconds >= 0 ? $"+{milliseconds}" : $"{milliseconds}";
			return $"{FormatMilliseconds(lowMilliseconds)} to {FormatMilliseconds(highMilliseconds)}ms: {count} hit{(count == 1 ? "" : "s")}";
		}

		private static BarGraph.Options BuildBarGraphOptions()
		{
			BarGraph.Options options = BarGraph.Options.Default;
			options.Width           = GraphW;
			options.Height          = GraphH;
			options.BarGap          = 1.0f;
			options.LabelAreaH      = LabelAreaH;
			options.ShowValueLabels = false;
			options.ShowMaxLine     = false;
			return options;
		}

		private static void DrawOverlays(RenderContext ctx)
		{
			uint threshColor = 0x55FFFFFF;
			ctx.DrawList.AddLine(new Vector2(ctx.GoodXN, ctx.Cursor.Y), new Vector2(ctx.GoodXN, ctx.Cursor.Y + GraphH), threshColor);
			ctx.DrawList.AddLine(new Vector2(ctx.GoodX,  ctx.Cursor.Y), new Vector2(ctx.GoodX,  ctx.Cursor.Y + GraphH), threshColor);
			ctx.DrawList.AddLine(new Vector2(ctx.OkayXN, ctx.Cursor.Y), new Vector2(ctx.OkayXN, ctx.Cursor.Y + GraphH), threshColor);
			ctx.DrawList.AddLine(new Vector2(ctx.OkayX,  ctx.Cursor.Y), new Vector2(ctx.OkayX,  ctx.Cursor.Y + GraphH), threshColor);

			ctx.DrawList.AddLine(new Vector2(ctx.CenterX, ctx.Cursor.Y), new Vector2(ctx.CenterX, ctx.Cursor.Y + GraphH), 0xFFFFFFFF, 1.5f);

			ctx.DrawList.AddText(new Vector2(ctx.Cursor.X + 4f, ctx.Cursor.Y + 4f), 0xFFFFFFFF, "EARLY");
			float lateW = ImGui.CalcTextSize("LATE").X;
			ctx.DrawList.AddText(new Vector2(ctx.Cursor.X + GraphW - lateW - 4f, ctx.Cursor.Y + 4f), 0xFFFFFFFF, "LATE");
		}

		private static void DrawZoneLabels(RenderContext ctx)
		{
			float labelY = ctx.Cursor.Y + GraphH + 2f;

			void DrawZoneLabel(float x0z, float x1z, string text, uint color)
			{
				if (x1z <= x0z) return;
				float midX = (x0z + x1z) * 0.5f;
				float tw   = ImGui.CalcTextSize(text).X;
				if (tw < x1z - x0z)
					ctx.DrawList.AddText(new Vector2(midX - tw * 0.5f, labelY), color, text);
				ctx.DrawList.AddLine(
					new Vector2(x0z, ctx.Cursor.Y + GraphH),
					new Vector2(x0z, ctx.Cursor.Y + GraphH + 4f),
					0x88FFFFFF);
			}

			DrawZoneLabel(ctx.Cursor.X,          ctx.OkayXN, "Bad",  ColorBad);
			DrawZoneLabel(ctx.OkayXN,             ctx.GoodXN, "Okay", ColorOkay);
			DrawZoneLabel(ctx.GoodXN,             ctx.GoodX,  "Good", ColorGood);
			DrawZoneLabel(ctx.GoodX,              ctx.OkayX,  "Okay", ColorOkay);
			DrawZoneLabel(ctx.OkayX,  ctx.Cursor.X + GraphW,  "Bad",  ColorBad);

			// Right-edge tick mark
			ctx.DrawList.AddLine(
				new Vector2(ctx.Cursor.X + GraphW, ctx.Cursor.Y + GraphH),
				new Vector2(ctx.Cursor.X + GraphW, ctx.Cursor.Y + GraphH + 4f),
				0x88FFFFFF);
		}
	}
}
