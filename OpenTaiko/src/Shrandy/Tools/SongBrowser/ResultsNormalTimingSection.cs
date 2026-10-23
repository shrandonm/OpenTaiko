using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	/// <summary>Draws the score the play would have had with default timing, with its own badge.</summary>
	internal static class ResultsNormalTimingSection
	{
		public static void Draw(ResultsSnapshot snapshot)
		{
			ScoreResult? current = snapshot.CurrentEntry.NormalResult;
			if (current == null)
			{
				return;
			}

			ImGui.SeparatorText("Normal Timing Score");
			ImGui.TextDisabled("Re-calculated as if default timing was used");

			ResultsComparisonTable.Draw("ResultsNormalTiming", current, snapshot.PreviousBestNormal);
		}
	}
}
