using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace OpenTaiko.Shrandy
{
	internal class AggregateNoteStats
	{
		public NoteStats CombinedNoteStats { get; set; } = new();
		public int TotalRuns { get; set; }
		public int DFCCount { get; set; }
		public int FCCount { get; set; }

		public void Draw()
		{
			if (TotalRuns == 0)
			{
				return;
			}

			ImGui.Text($"Total Runs: {TotalRuns}");
			ImGui.Separator();
			ImGui.Text($"DFCs: {DFCCount} ({StringHelpers.GetPercentString(DFCCount, TotalRuns)}%)");
			ImGui.Separator();
			ImGui.Text($"Full Combos: {FCCount} ({StringHelpers.GetPercentString(FCCount, TotalRuns)}%)");
			ImGui.Separator();
			ImGui.Separator();
			CombinedNoteStats.Draw();
		}
	}
}
