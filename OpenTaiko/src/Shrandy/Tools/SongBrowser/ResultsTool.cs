using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class ResultsTool : Tool
	{
		private static readonly Vector4 DeltaPositiveColor = new Vector4(0.2f, 0.9f, 0.2f, 1.0f);
		private static readonly Vector4 DeltaNegativeColor = new Vector4(0.9f, 0.2f, 0.2f, 1.0f);

		public ResultsTool(string toolName = "Results", SlimDXKeys.Key enableHotkey = SlimDXKeys.Key.Unknown)
			: base(toolName, enableHotkey)
		{
		}

		public override bool IsBlockingInput()
		{
			return false;
		}

		public override void OnSongComplete()
		{
			base.OnSongComplete();
			SetEnabled(true);
		}

		public override void OnResultsActivate(CStage結果 resultsScreen)
		{
			base.OnResultsActivate(resultsScreen);
			SetEnabled(true);
		}

		public override void OnStageChanged(CStage stage)
		{
			if (stage is not CStage結果 && stage is not CStage曲読み込み)
			{
				SetEnabled(false);
			}
		}

		protected override void Draw()
		{
			ResultsSnapshot? snapshot = OpenTaiko.ShrandyExtension.GetTool<SongBrowserTool>()?.Data.CurrentResultsSnapshot;
			if (snapshot == null)
			{
				ImGui.Text("No results data.");
				return;
			}

			DrawHeader(snapshot.Value);
			DrawDeltaTable(snapshot.Value);
			DrawHitDistribution(snapshot.Value.HitDistribution);
		}

		private static void DrawHeader(ResultsSnapshot snapshot)
		{
			ImGui.Text(snapshot.CurrentEntry.SongTitle);
			ImGui.SameLine();
			ImGui.TextDisabled($"({snapshot.CurrentEntry.Difficulty})");

			if (snapshot.PreviousBest == null)
			{
				ImGui.TextColored(DeltaPositiveColor, "First play with these mods!");
			}
			else
			{
				ImGui.TextDisabled("vs. Previous Best (matching mods)");
			}
		}

		private void DrawDeltaTable(ResultsSnapshot snapshot)
		{
			SongEntry current = snapshot.CurrentEntry;
			SongEntry? previous = snapshot.PreviousBest;
			SongEntry? noMod = snapshot.NoModBest;
			bool showNoMod = noMod != null;
			int columnCount = showNoMod ? 5 : 4;

			ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
			if (!ImGui.BeginTable("ResultsDelta", columnCount, tableFlags))
			{
				return;
			}

			ImGui.TableSetupColumn("Stat",         ImGuiTableColumnFlags.WidthFixed, 140);
			if (showNoMod) ImGui.TableSetupColumn("No-Mod Best", ImGuiTableColumnFlags.WidthFixed, 110);
			ImGui.TableSetupColumn("Best",         ImGuiTableColumnFlags.WidthFixed, 110);
			ImGui.TableSetupColumn("Current",      ImGuiTableColumnFlags.WidthFixed, 110);
			ImGui.TableSetupColumn("Difference",   ImGuiTableColumnFlags.WidthFixed, 100);
			ImGui.TableHeadersRow();

			DrawScoreRow("Score",     current.Score, current.ScoreRank, previous?.Score, previous?.ScoreRank, noMod?.Score, noMod?.ScoreRank, showNoMod);
			DrawGoodPercentRow(
				StringHelpers.GetPercent(current.Goods, current.TotalNotes),
				previous != null ? StringHelpers.GetPercent(previous.Goods, previous.TotalNotes) : null,
				noMod    != null ? StringHelpers.GetPercent(noMod.Goods, noMod.TotalNotes): null,
				showNoMod);
			DrawIntRow("Goods",       current.Goods,       previous?.Goods,       noMod?.Goods,       showNoMod, invertColors: false);
			DrawIntRow("Okays",       current.Okays,       previous?.Okays,       noMod?.Okays,       showNoMod, invertColors: true);
			DrawIntRow("Bads",        current.Bads,        previous?.Bads,        noMod?.Bads,        showNoMod, invertColors: true);
			DrawIntRow("Drum Roll",   current.Rolls,       previous?.Rolls,       noMod?.Rolls,       showNoMod, invertColors: false);
			DrawHitErrorRow(current.AvgHitError, previous?.AvgHitError, noMod?.AvgHitError, showNoMod, invertColors: true);

			ImGui.EndTable();
		}

		private static void DrawScoreRow(string label, int currentValue, int currentBadge, int? previousValue, int? previousBadge, int? noModValue, int? noModBadge, bool showNoMod)
		{
			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted(label);

			int col = 1;
			if (showNoMod)
			{
				ImGui.TableSetColumnIndex(col++);
				if (noModValue.HasValue)
				{
					ImGui.TextUnformatted(noModValue.Value.ToString());
					DrawBadge(noModBadge ?? 0);
				}
				else
				{
					ImGui.TextUnformatted("\u2014");
				}
			}

			ImGui.TableSetColumnIndex(col++);
			if (previousValue.HasValue)
			{
				ImGui.TextUnformatted(previousValue.Value.ToString());
				DrawBadge(previousBadge ?? 0);
			}
			else
			{
				ImGui.TextUnformatted("\u2014");
			}

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(currentValue.ToString());
			DrawBadge(currentBadge);

			ImGui.TableSetColumnIndex(col);
			DrawIntDelta(currentValue, previousValue, invertColors: false);
		}

		private static void DrawIntRow(string label, int currentValue, int? previousValue, int? noModValue, bool showNoMod, bool invertColors)
		{
			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted(label);

			int col = 1;
			if (showNoMod)
			{
				ImGui.TableSetColumnIndex(col++);
				ImGui.TextUnformatted(noModValue.HasValue ? noModValue.Value.ToString() : "\u2014");
			}

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(previousValue.HasValue ? previousValue.Value.ToString() : "\u2014");

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(currentValue.ToString());

			ImGui.TableSetColumnIndex(col);
			DrawIntDelta(currentValue, previousValue, invertColors);
		}

		private static void DrawGoodPercentRow(float currentValue, float? previousValue, float? noModValue, bool showNoMod)
		{
			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted("Good %");

			int col = 1;
			if (showNoMod)
			{
				ImGui.TableSetColumnIndex(col++);
				ImGui.TextUnformatted(noModValue.HasValue ? StringHelpers.GetPercentString(noModValue.Value) : "\u2014");
			}

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(previousValue.HasValue ? StringHelpers.GetPercentString(previousValue.Value) : "\u2014");

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(StringHelpers.GetPercentString(currentValue));

			ImGui.TableSetColumnIndex(col);
			if (previousValue.HasValue)
			{
				float delta = currentValue - previousValue.Value;
				DrawDeltaText(delta, StringHelpers.GetPercentString(delta), invertColors: false);
			}
			else
			{
				ImGui.TextUnformatted("\u2014");
			}
		}

		private static void DrawHitErrorRow(float currentValue, float? previousValue, float? noModValue, bool showNoMod, bool invertColors)
		{
			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted("Avg Hit Error");

			int col = 1;
			if (showNoMod)
			{
				ImGui.TableSetColumnIndex(col++);
				ImGui.TextUnformatted(noModValue.HasValue ? $"{noModValue.Value:F1}ms" : "\u2014");
			}

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(previousValue.HasValue ? $"{previousValue.Value:F1}ms" : "\u2014");

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted($"{currentValue:F1}ms");

			ImGui.TableSetColumnIndex(col);
			DrawFloatDelta(currentValue, previousValue, invertColors);
		}

		private static void DrawBadge(int rank)
		{
			if (rank <= 0) return;
			ImGui.SameLine();
			Utilities.SongHelper.DrawScoreRank(rank);
		}

		private static void DrawIntDelta(int currentValue, int? previousValue, bool invertColors)
		{
			if (!previousValue.HasValue)
			{
				ImGui.TextUnformatted("\u2014");
				return;
			}

			int delta = currentValue - previousValue.Value;
			DrawDeltaText(delta, delta.ToString("+0;-0;0"), invertColors);
		}

		private static void DrawFloatDelta(float currentValue, float? previousValue, bool invertColors)
		{
			if (!previousValue.HasValue)
			{
				ImGui.TextUnformatted("\u2014");
				return;
			}

			float delta = currentValue - previousValue.Value;
			DrawDeltaText(delta, $"{delta:+0.0;-0.0;0.0}ms", invertColors);
		}

		private static void DrawDeltaText(float delta, string text, bool invertColors)
		{
			if (delta > 0)
			{
				ImGui.TextColored(invertColors ? DeltaNegativeColor : DeltaPositiveColor, text);
			}
			else if (delta < 0)
			{
				ImGui.TextColored(invertColors ? DeltaPositiveColor : DeltaNegativeColor, text);
			}
			else
			{
				ImGui.TextUnformatted(text);
			}
		}

		private static void DrawHitDistribution(Dictionary<int, int>? distribution)
		{
			if (distribution == null || distribution.Count == 0)
				return;

			ImGui.Separator();

			const int   BucketMs  = 3;
			const float GraphH    = 120f;
			const float GraphW    = 500f;
			const uint  ColorGood = 0xFF20C820;
			const uint  ColorOkay = 0xFF00D7FF;
			const uint  ColorBad  = 0xFF3232DC;

			int goodMs    = OpenTaiko.ConfigIni.nHitRangeMs.Perfect;
			int okayMs    = OpenTaiko.ConfigIni.nHitRangeMs.Good;
			int maxExtent = OpenTaiko.ConfigIni.nHitRangeMs.Poor;

			// Compute stats
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

			// --- Stats header ---
			var     drawList = ImGui.GetWindowDrawList();
			Vector2 statsPos = ImGui.GetCursorScreenPos();
			const float StatsH = 44f;
			float   colW      = GraphW / 4f;
			float   textH     = ImGui.GetTextLineHeight();

			drawList.AddRectFilled(statsPos, new Vector2(statsPos.X + GraphW, statsPos.Y + StatsH), 0xDD111111);

			(string label, string value)[] stats =
			{
				("mean abs error", $"{meanAbsMs:F1}ms"),
				("mean",           $"{meanMs:F1}ms"),
				("std dev * 10 (Unstable Rate)",    $"{stdDev * 10f:F1}ms"),
				("max error",      $"{rawMaxAbs}ms"),
			};

			for (int c = 0; c < stats.Length; c++)
			{
				float cx = statsPos.X + c * colW + colW * 0.5f;
				float lw = ImGui.CalcTextSize(stats[c].label).X;
				float vw = ImGui.CalcTextSize(stats[c].value).X;
				drawList.AddText(new Vector2(cx - lw * 0.5f, statsPos.Y + 4f),                  0xFFAAAAAA, stats[c].label);
				drawList.AddText(new Vector2(cx - vw * 0.5f, statsPos.Y + StatsH - textH - 4f), 0xFFFFFFFF, stats[c].value);
			}

			ImGui.Dummy(new Vector2(GraphW, StatsH));

			// --- Build buckets ---
			int minKey = distribution.Keys.Min();
			int maxKey = distribution.Keys.Max();
			int extent = Math.Min(Math.Max(Math.Abs(minKey), Math.Abs(maxKey)), maxExtent);
			extent = Math.Max(extent, BucketMs);

			int halfBuckets = (extent + BucketMs - 1) / BucketMs;
			int numBuckets  = halfBuckets * 2 + 1;

			var buckets = new int[numBuckets];
			foreach (var kvp in distribution)
			{
				int absV = Math.Abs(kvp.Key);
				int bi   = (absV + 1) / BucketMs;   // 0,1→0  2,3,4→1  5,6,7→2 ...
				if (kvp.Key < 0) bi = -bi;
				int idx  = bi + halfBuckets;
				if (idx >= 0 && idx < numBuckets)
					buckets[idx] += kvp.Value;
			}

			int maxCount = buckets.Max();
			if (maxCount == 0) return;

			Vector2 cursor = ImGui.GetCursorScreenPos();
			float   barW   = GraphW / numBuckets;

			// Threshold lines snapped to exact bar boundaries via integer offsets
			float BarLeftEdge(int offset) => cursor.X + (halfBuckets + offset) * barW;

			float goodXN  = BarLeftEdge(-(goodMs / BucketMs));
			float goodX   = BarLeftEdge(goodMs  / BucketMs + 1);
			float okayXN  = BarLeftEdge(-(okayMs / BucketMs));
			float okayX   = BarLeftEdge(okayMs  / BucketMs + 1);
			float centerX = cursor.X + halfBuckets * barW + barW * 0.5f;

			drawList.AddRectFilled(cursor, new Vector2(cursor.X + GraphW, cursor.Y + GraphH), 0xAA000000);

			// Bars
			for (int i = 0; i < numBuckets; i++)
			{
				if (buckets[i] == 0) continue;

				int  bucketMs = (i - halfBuckets) * BucketMs;
				int  absMs    = Math.Abs(bucketMs);
				uint color    = absMs <= goodMs ? ColorGood
				              : absMs <= okayMs ? ColorOkay
				              :                   ColorBad;

				float barH = GraphH * (buckets[i] / (float)maxCount);
				float x0   = cursor.X + i * barW;
				float x1   = x0 + barW - 1f;
				float y0   = cursor.Y + GraphH - barH;
				float y1   = cursor.Y + GraphH;
				drawList.AddRectFilled(new Vector2(x0, y0), new Vector2(x1, y1), color);
			}

			// Zone threshold lines
			uint threshColor = 0x55FFFFFF;
			drawList.AddLine(new Vector2(goodXN, cursor.Y), new Vector2(goodXN, cursor.Y + GraphH), threshColor);
			drawList.AddLine(new Vector2(goodX,  cursor.Y), new Vector2(goodX,  cursor.Y + GraphH), threshColor);
			drawList.AddLine(new Vector2(okayXN, cursor.Y), new Vector2(okayXN, cursor.Y + GraphH), threshColor);
			drawList.AddLine(new Vector2(okayX,  cursor.Y), new Vector2(okayX,  cursor.Y + GraphH), threshColor);

			// Center line
			drawList.AddLine(new Vector2(centerX, cursor.Y), new Vector2(centerX, cursor.Y + GraphH), 0xFFFFFFFF, 1.5f);

			// EARLY / LATE labels
			drawList.AddText(new Vector2(cursor.X + 4f, cursor.Y + 4f), 0xFFFFFFFF, "EARLY");
			float lateW = ImGui.CalcTextSize("LATE").X;
			drawList.AddText(new Vector2(cursor.X + GraphW - lateW - 4f, cursor.Y + 4f), 0xFFFFFFFF, "LATE");

			// Hover highlight + tooltip
			if (ImGui.IsMouseHoveringRect(cursor, new Vector2(cursor.X + GraphW, cursor.Y + GraphH)))
			{
				int hovIdx = (int)((ImGui.GetMousePos().X - cursor.X) / barW);
				if (hovIdx >= 0 && hovIdx < numBuckets)
				{
					float hx0 = cursor.X + hovIdx * barW;
					drawList.AddRectFilled(new Vector2(hx0, cursor.Y), new Vector2(hx0 + barW, cursor.Y + GraphH), 0x33FFFFFF);

					int    bi    = hovIdx - halfBuckets;
					int    bLow  = bi * BucketMs - 1;  // center bi=0: -1  bi=1: +2  bi=-1: -4
					int    bHigh = bi * BucketMs + 1;  // center bi=0: +1  bi=1: +4  bi=-1: -2
					int    count = buckets[hovIdx];
					string Fmt(int ms) => ms >= 0 ? $"+{ms}" : $"{ms}";
					ImGui.SetTooltip($"{Fmt(bLow)} to {Fmt(bHigh)}ms: {count} hit{(count == 1 ? "" : "s")}");
				}
			}

			// Zone labels below graph
			const float LabelAreaH = 18f;
			float labelY = cursor.Y + GraphH + 2f;

			void DrawZoneLabel(float x0z, float x1z, string text, uint color)
			{
				if (x1z <= x0z) return;
				float midX = (x0z + x1z) * 0.5f;
				float tw   = ImGui.CalcTextSize(text).X;
				if (tw < x1z - x0z)
					drawList.AddText(new Vector2(midX - tw * 0.5f, labelY), color, text);
				drawList.AddLine(new Vector2(x0z, cursor.Y + GraphH), new Vector2(x0z, cursor.Y + GraphH + 4f), 0x88FFFFFF);
			}

			DrawZoneLabel(cursor.X,          okayXN, "Bad",  ColorBad);
			DrawZoneLabel(okayXN,             goodXN, "Okay", ColorOkay);
			DrawZoneLabel(goodXN,             goodX,  "Good", ColorGood);
			DrawZoneLabel(goodX,              okayX,  "Okay", ColorOkay);
			DrawZoneLabel(okayX,  cursor.X + GraphW,  "Bad",  ColorBad);
			// right edge tick
			drawList.AddLine(new Vector2(cursor.X + GraphW, cursor.Y + GraphH), new Vector2(cursor.X + GraphW, cursor.Y + GraphH + 4f), 0x88FFFFFF);

			ImGui.Dummy(new Vector2(GraphW, GraphH + LabelAreaH));
		}
	}
}
