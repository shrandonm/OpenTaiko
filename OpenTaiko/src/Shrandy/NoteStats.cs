using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy
{
	internal class NoteStats
	{
		public int GoodCount { get; set; }
		public int OkayCount { get; set; }
		public int BadCount { get; set; }
		public int EarlyCount { get; set; }
		public int LateCount { get; set; }
		public float TotalHitError { get; set; }
		public float TotalSync { get; set; }

		[JsonIgnore]
		public int TotalNotes { get { return GoodCount + OkayCount + BadCount; } }
		[JsonIgnore]
		public float AverageHitError { get { return TotalNotes > 0 ? TotalHitError / TotalNotes : 0.0f; } }
		[JsonIgnore]
		public float AverageSync { get { return TotalNotes > 0 ? TotalSync / TotalNotes : 0.0f; } }
		[JsonIgnore]
		public bool IsDFC { get { return OkayCount == 0 && BadCount == 0; } }
		[JsonIgnore]
		public bool IsFC { get { return BadCount == 0; } }

		public void OnNoteHit(HitParams hitParams)
		{
			int error = hitParams.Chip.nLag;
			TotalSync += error;

			int absError = Math.Abs(hitParams.Chip.nLag);
			TotalHitError += absError;

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

			if (hitParams.JudgeResult == ENoteJudge.Perfect || hitParams.JudgeResult == ENoteJudge.Good)
			{
				const int threshold = 10;
				if (error >= threshold)
				{
					LateCount++;
				}
				else if (error <= -threshold)
				{
					EarlyCount++;
				}
			}
		}

		public static NoteStats operator +(NoteStats left, NoteStats right)
		{
			return new NoteStats()
			{
				EarlyCount = left.EarlyCount + right.EarlyCount,
				LateCount = left.LateCount + right.LateCount,
				GoodCount = left.GoodCount + right.GoodCount,
				OkayCount = left.OkayCount + right.OkayCount,
				BadCount = left.BadCount + right.BadCount,
				TotalHitError = left.TotalHitError + right.TotalHitError,
				TotalSync = left.TotalSync + right.TotalSync,
			};
		}

		public float GetGoodPercent()
		{
			return StringHelpers.GetPercent(GoodCount, TotalNotes);
		}

		public string GetGoodPercentString()
		{
			return StringHelpers.GetPercentString(GoodCount, TotalNotes);
		}

		public void Draw()
		{
			int totalNotes = TotalNotes;
			if (totalNotes > 0)
			{
				ImGui.Text($"Early: {EarlyCount} ({StringHelpers.GetPercentString(EarlyCount, totalNotes)}%)");
				ImGui.Separator();
				ImGui.Text($"Late: {LateCount} ({StringHelpers.GetPercentString(LateCount, totalNotes)}%)");
				ImGui.Separator();
				ImGui.Text($"Goods: {GoodCount} ({StringHelpers.GetPercentString(GoodCount, totalNotes)}%)");
				ImGui.Separator();
				ImGui.Text($"Okays: {OkayCount} ({StringHelpers.GetPercentString(OkayCount, totalNotes)}%)");
				ImGui.Separator();
				ImGui.Text($"Bads: {BadCount} ({StringHelpers.GetPercentString(BadCount, totalNotes)}%)");
				ImGui.Separator();
				ImGui.Text($"Average Error: +/- {AverageHitError:F2}ms");
				ImGui.Separator();
				ImGui.Text($"Average Sync: {AverageSync:F2} ms");
			}
		}
	}
}
