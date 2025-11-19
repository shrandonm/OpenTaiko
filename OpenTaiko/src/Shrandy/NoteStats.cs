using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace OpenTaiko.Shrandy
{
	internal class NoteStats
	{
		public int StatEntryCount { get; set; }
		public int GoodCount { get; set; }
		public int OkayCount { get; set; }
		public int BadCount { get; set; }
		public float TotalHitError { get; set; }

		public int TotalNotes { get { return GoodCount + OkayCount + BadCount; } }
		public float AverageHitError { get { return TotalNotes > 0 ? TotalHitError / TotalNotes : 0.0f; } }

		public void OnNoteHit(HitParams hitParams)
		{
			int correctTiming = hitParams.Chip.n発声時刻ms;
			int error = Math.Abs(hitParams.Chip.nLag);
			TotalHitError += error;

			if (hitParams.JudgeResult == ENoteJudge.Perfect)
			{
				++GoodCount;
			}
			else if (hitParams.JudgeResult == ENoteJudge.Good)
			{
				++OkayCount;
			}
			else
			{
				++BadCount;
			}
		}

		public static NoteStats operator +(NoteStats left, NoteStats right)
		{
			return new NoteStats()
			{
				StatEntryCount = left.StatEntryCount + right.StatEntryCount,
				GoodCount = left.GoodCount + right.GoodCount,
				OkayCount = left.OkayCount + right.OkayCount,
				BadCount = left.BadCount + right.BadCount,
				TotalHitError = left.TotalHitError + right.TotalHitError,
			};
		}

		public float GetPercent(int hits, int totalNotes)
		{
			return totalNotes > 0 ? hits / (float)totalNotes : 0.0f;
		}

		public string GetPercentString(int hits, int totalNotes)
		{
			return $"{GetPercent(hits, totalNotes) * 100.0f:F2}%";
		}

		public void Draw()
		{
			int totalNotes = TotalNotes;
			if (totalNotes > 0)
			{
				ImGui.Text($"Count: {StatEntryCount}");
				ImGui.Text($"Total Notes: {totalNotes}");
				ImGui.Separator();
				ImGui.Text($"Goods: {GoodCount} ({GetPercentString(GoodCount, totalNotes)}%)");
				ImGui.Text($"Okays: {OkayCount} ({GetPercentString(OkayCount, totalNotes)}%)");
				ImGui.Text($"Bads: {BadCount} ({GetPercentString(BadCount, totalNotes)}%)");
				ImGui.Text($"Average Error: +/- {AverageHitError}ms");
			}
		}
	}
}
