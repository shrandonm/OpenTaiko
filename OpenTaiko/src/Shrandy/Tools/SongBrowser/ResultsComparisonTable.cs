using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	/// <summary>Draws the Best / Current / Difference table for a <see cref="ScoreResult"/>, so every results section shows identical rows.</summary>
	internal static class ResultsComparisonTable
	{
		private static readonly Vector4 PositiveColor = new Vector4(0.2f, 0.9f, 0.2f, 1.0f);
		private static readonly Vector4 NegativeColor = new Vector4(0.9f, 0.2f, 0.2f, 1.0f);
		private const string EmptyCell = "\u2014";

		public static void Draw(string tableId, ScoreResult current, ScoreResult? previous)
		{
			if (!ImGui.BeginTable(tableId, 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
			{
				return;
			}

			ImGui.TableSetupColumn("Stat", ImGuiTableColumnFlags.WidthFixed, 140.0f);
			ImGui.TableSetupColumn("Best", ImGuiTableColumnFlags.WidthFixed, 110.0f);
			ImGui.TableSetupColumn("Current", ImGuiTableColumnFlags.WidthFixed, 110.0f);
			ImGui.TableSetupColumn("Difference", ImGuiTableColumnFlags.WidthFixed, 100.0f);
			ImGui.TableHeadersRow();

			DrawScoreRow(current, previous);
			DrawGoodsRow(current, previous);
			DrawIntegerRow("Okays", current.Okays, previous?.Okays, true);
			DrawIntegerRow("Bads", current.Bads, previous?.Bads, true);
			DrawIntegerRow("Max Combo", current.MaxCombo, previous?.MaxCombo, false);
			DrawIntegerRow("Drum Roll", current.Rolls, previous?.Rolls, false);
			DrawMillisecondRow("Avg Hit Error", current.AvgHitError, previous?.AvgHitError, true);

			ImGui.EndTable();
		}

		private static void DrawScoreRow(ScoreResult current, ScoreResult? previous)
		{
			BeginRow("Score");

			ImGui.TableSetColumnIndex(1);
			if (previous != null)
			{
				ImGui.TextUnformatted(previous.Score.ToString());
				DrawBadge(previous.ScoreRank);
			}
			else
			{
				ImGui.TextUnformatted(EmptyCell);
			}

			ImGui.TableSetColumnIndex(2);
			ImGui.TextUnformatted(current.Score.ToString());
			DrawBadge(current.ScoreRank);

			DrawDelta(current.Score - previous?.Score, "+0;-0;0", false);
		}

		private static void DrawIntegerRow(string label, int currentValue, int? previousValue, bool invertColors)
		{
			BeginRow(label);
			DrawValueCells(currentValue.ToString(), previousValue?.ToString());
			DrawDelta(currentValue - previousValue, "+0;-0;0", invertColors);
		}

		private static void DrawGoodsRow(ScoreResult current, ScoreResult? previous)
		{
			BeginRow("Goods");
			DrawValueCells(FormatGoods(current), previous != null ? FormatGoods(previous) : null);

			ImGui.TableSetColumnIndex(3);
			if (previous == null)
			{
				ImGui.TextUnformatted(EmptyCell);
				return;
			}

			int goodsDelta = current.Goods - previous.Goods;
			float percentDelta = current.GoodPercent - previous.GoodPercent;
			string percentText = StringHelpers.GetPercentString(percentDelta);
			string signedPercentText = percentDelta > 0.0f ? $"+{percentText}" : percentText;
			DrawDeltaText(goodsDelta, $"{goodsDelta:+0;-0;0} ({signedPercentText})", false);
		}

		private static string FormatGoods(ScoreResult result)
		{
			return $"{result.Goods} ({StringHelpers.GetPercentString(result.GoodPercent)})";
		}

		private static void DrawMillisecondRow(string label, float currentValue, float? previousValue, bool invertColors)
		{
			BeginRow(label);
			DrawValueCells($"{currentValue:F1}ms", previousValue.HasValue ? $"{previousValue.Value:F1}ms" : null);

			ImGui.TableSetColumnIndex(3);
			if (!previousValue.HasValue)
			{
				ImGui.TextUnformatted(EmptyCell);
				return;
			}

			float delta = currentValue - previousValue.Value;
			DrawDeltaText(delta, $"{delta:+0.0;-0.0;0.0}ms", invertColors);
		}

		private static void BeginRow(string label)
		{
			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted(label);
		}

		private static void DrawValueCells(string currentText, string? previousText)
		{
			ImGui.TableSetColumnIndex(1);
			ImGui.TextUnformatted(previousText ?? EmptyCell);

			ImGui.TableSetColumnIndex(2);
			ImGui.TextUnformatted(currentText);
		}

		private static void DrawDelta(int? delta, string format, bool invertColors)
		{
			ImGui.TableSetColumnIndex(3);
			if (!delta.HasValue)
			{
				ImGui.TextUnformatted(EmptyCell);
				return;
			}

			DrawDeltaText(delta.Value, delta.Value.ToString(format), invertColors);
		}

		private static void DrawDeltaText(float delta, string text, bool invertColors)
		{
			if (delta == 0.0f)
			{
				ImGui.TextUnformatted(text);
				return;
			}

			bool isImprovement = invertColors ? delta < 0.0f : delta > 0.0f;
			ImGui.TextColored(isImprovement ? PositiveColor : NegativeColor, text);
		}

		private static void DrawBadge(int rank)
		{
			if (rank <= 0)
			{
				return;
			}
			ImGui.SameLine();
			Utilities.SongHelper.DrawScoreRank(rank);
		}
	}
}
