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
			DrawDdrScoreSection(snapshot.Value);
			HitDistributionGraph.Draw(snapshot.Value.HitDistribution);
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

		private static readonly Vector4 DdrMarvelousColor = new Vector4(1.0f, 0.75f, 0.25f, 1.0f);
		private static readonly Vector4 DdrPerfectColor = new Vector4(1.0f, 0.95f, 0.35f, 1.0f);
		private static readonly Vector4 DdrGreatColor = new Vector4(0.4f, 0.9f, 0.4f, 1.0f);
		private static readonly Vector4 DdrGoodColor = new Vector4(0.4f, 0.65f, 1.0f, 1.0f);
		private static readonly Vector4 DdrOkColor = new Vector4(0.85f, 0.85f, 0.85f, 1.0f);
		private static readonly Vector4 DdrMissColor = new Vector4(1.0f, 0.35f, 0.35f, 1.0f);
		private static readonly Vector4 DdrFastColor = new Vector4(0.35f, 0.85f, 0.95f, 1.0f);
		private static readonly Vector4 DdrLateColor = new Vector4(0.95f, 0.45f, 0.7f, 1.0f);
		private static readonly Vector4 DdrBadgeTextColor = new Vector4(0.05f, 0.05f, 0.05f, 1.0f);

		private static void DrawDdrScoreSection(ResultsSnapshot snapshot)
		{
			SongEntry current = snapshot.CurrentEntry;
			SongEntry? previous = snapshot.PreviousBest;
			Dictionary<int, int> hitDistribution = snapshot.HitDistribution ?? new Dictionary<int, int>();
			DdrJudgementCounts judgements = DdrScoreCalculator.ClassifyHitDistribution(hitDistribution, current.TotalNotes);

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.TextDisabled("DDR Score");

			DrawBigCenteredText(current.DdrGrade, DdrMarvelousColor, 1.0f);
			DrawDdrFullComboBanner(judgements);

			if (previous == null)
			{
				DrawCenteredText("First DDR score for this song!");
			}

			ImGui.Spacing();

			if (ImGui.BeginTable("DdrScorePanel", 3, ImGuiTableFlags.BordersInnerV))
			{
				ImGui.TableSetupColumn("Judgements", ImGuiTableColumnFlags.WidthStretch, 2.2f);
				ImGui.TableSetupColumn("Speed", ImGuiTableColumnFlags.WidthStretch, 1.0f);
				ImGui.TableSetupColumn("Stats", ImGuiTableColumnFlags.WidthStretch, 2.2f);

				ImGui.TableNextRow();

				ImGui.TableSetColumnIndex(0);
				DrawDdrJudgementList(judgements);

				ImGui.TableSetColumnIndex(1);
				DrawDdrSpeedBadges(judgements);

				ImGui.TableSetColumnIndex(2);
				DrawDdrStatsList(current, previous);

				ImGui.EndTable();
			}
		}

		private static void DrawDdrJudgementList(DdrJudgementCounts judgements)
		{
			DrawDdrJudgementRow("Marvelous", judgements.Marvelous, DdrMarvelousColor);
			DrawDdrJudgementRow("Perfect", judgements.Perfect, DdrPerfectColor);
			DrawDdrJudgementRow("Great", judgements.Great, DdrGreatColor);
			DrawDdrJudgementRow("Good", judgements.Good, DdrGoodColor);
			DrawDdrJudgementRow("O.K.", judgements.Ok, DdrOkColor);
			DrawDdrJudgementRow("Miss", judgements.Miss, DdrMissColor);
		}

		private static void DrawDdrSpeedBadges(DdrJudgementCounts judgements)
		{
			ImGui.Dummy(new Vector2(0.0f, 6.0f));
			DrawDdrBadge("FAST", judgements.Fast, DdrFastColor);
			ImGui.Dummy(new Vector2(0.0f, 14.0f));
			DrawDdrBadge("SLOW", judgements.Late, DdrLateColor);
		}

		private static void DrawDdrStatsList(SongEntry current, SongEntry? previous)
		{
			DrawDdrStatRow("SCORE", current.DdrScore.ToString("N0"), Vector4.One);

			if (previous != null)
			{
				int delta = current.DdrScore - previous.DdrScore;
				Vector4 deltaColor = delta >= 0 ? DeltaPositiveColor : DeltaNegativeColor;

				ImGui.Spacing();
				DrawDdrStatRow("BEST SCORE", previous.DdrScore.ToString("N0"), Vector4.One);
				DrawDdrStatRow("", delta.ToString("+#,0;-#,0;0"), deltaColor);
			}

			ImGui.Spacing();
			DrawDdrStatRow("MAX COMBO", $"{current.MaxCombo} / {current.TotalNotes}", Vector4.One);
		}

		/// <summary>Draws a label on the left and a value right-aligned to the cell's width, e.g. "Marvelous   250".</summary>
		private static void DrawDdrJudgementRow(string label, int count, Vector4 color)
		{
			float startX = ImGui.GetCursorPosX();
			float availableWidth = ImGui.GetContentRegionAvail().X;

			ImGui.TextColored(color, label);

			string countText = count.ToString();
			Vector2 countSize = ImGui.CalcTextSize(countText);
			ImGui.SameLine();
			ImGui.SetCursorPosX(startX + availableWidth - countSize.X);
			ImGui.TextColored(color, countText);
		}

		private static void DrawDdrStatRow(string label, string value, Vector4 valueColor)
		{
			float startX = ImGui.GetCursorPosX();
			float availableWidth = ImGui.GetContentRegionAvail().X;

			if (!string.IsNullOrEmpty(label))
			{
				ImGui.TextDisabled(label);
				ImGui.SameLine();
			}

			Vector2 valueSize = ImGui.CalcTextSize(value);
			ImGui.SetCursorPosX(startX + availableWidth - valueSize.X);
			ImGui.TextColored(valueColor, value);
		}

		/// <summary>Draws a small colored badge like the FAST/SLOW boxes on the DDR results screen.</summary>
		private static void DrawDdrBadge(string label, int count, Vector4 backgroundColor)
		{
			const float boxWidth = 64.0f;
			const float boxHeight = 46.0f;
			Vector2 padding = new Vector2(4.0f, 4.0f);

			ImGui.PushStyleColor(ImGuiCol.ChildBg, backgroundColor);
			ImGui.PushStyleColor(ImGuiCol.Border, backgroundColor);
			ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4.0f);
			ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1.0f);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding);

			float availableWidth = ImGui.GetContentRegionAvail().X;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + System.Math.Max(0.0f, (availableWidth - boxWidth) / 2.0f));

			ImGuiWindowFlags childFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
			ImGui.BeginChild($"##Ddr{label}Badge", new Vector2(boxWidth, boxHeight), true, childFlags);

			float innerWidth = boxWidth - padding.X * 2.0f;
			float innerHeight = boxHeight - padding.Y * 2.0f;

			string countText = count.ToString();
			Vector2 labelSize = ImGui.CalcTextSize(label);
			Vector2 countSize = ImGui.CalcTextSize(countText);
			float lineGap = 2.0f;
			float blockHeight = labelSize.Y + lineGap + countSize.Y;
			float startY = System.Math.Max(0.0f, (innerHeight - blockHeight) / 2.0f);

			ImGui.SetCursorPos(new Vector2(System.Math.Max(0.0f, (innerWidth - labelSize.X) / 2.0f), startY));
			ImGui.TextColored(DdrBadgeTextColor, label);

			ImGui.SetCursorPosX(System.Math.Max(0.0f, (innerWidth - countSize.X) / 2.0f));
			ImGui.TextColored(DdrBadgeTextColor, countText);

			ImGui.EndChild();

			ImGui.PopStyleVar(3);
			ImGui.PopStyleColor(2);
		}

		private static void DrawDdrFullComboBanner(DdrJudgementCounts judgements)
		{
			switch (DdrScoreCalculator.CalculateComboType(judgements))
			{
				case "MFC":
					DrawCenteredText("Marvelous Fullcombo!!", DdrMarvelousColor);
					break;
				case "PFC":
					DrawCenteredText("Perfect Fullcombo!!", DdrPerfectColor);
					break;
				case "GFC":
					DrawCenteredText("Great Fullcombo!!", DdrGreatColor);
					break;
			}
		}

		/// <summary>Draws text using the pre-baked large font instead of scaling up the default font, which keeps it crisp instead of blurry.</summary>
		private static unsafe void DrawBigCenteredText(string text, Vector4 color, float scale)
		{
			bool pushedLargeFont = OpenTaiko.LargeFont.NativePtr != null;
			if (pushedLargeFont)
			{
				ImGui.PushFont(OpenTaiko.LargeFont);
			}

			ImGui.SetWindowFontScale(scale);
			Vector2 size = ImGui.CalcTextSize(text);
			float availableWidth = ImGui.GetContentRegionAvail().X;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + System.Math.Max(0.0f, (availableWidth - size.X) / 2.0f));
			ImGui.TextColored(color, text);
			ImGui.SetWindowFontScale(1.0f);

			if (pushedLargeFont)
			{
				ImGui.PopFont();
			}
		}

		private static void DrawCenteredText(string text)
		{
			Vector2 size = ImGui.CalcTextSize(text);
			float availableWidth = ImGui.GetContentRegionAvail().X;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + System.Math.Max(0.0f, (availableWidth - size.X) / 2.0f));
			ImGui.TextUnformatted(text);
		}

		private static void DrawCenteredText(string text, Vector4 color)
		{
			Vector2 size = ImGui.CalcTextSize(text);
			float availableWidth = ImGui.GetContentRegionAvail().X;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + System.Math.Max(0.0f, (availableWidth - size.X) / 2.0f));
			ImGui.TextColored(color, text);
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
	}
}
