using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class NoteStats
	{
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
				GoodCount = left.GoodCount + right.GoodCount,
				OkayCount = left.OkayCount + right.OkayCount,
				BadCount = left.BadCount + right.BadCount,
				TotalHitError = left.TotalHitError + right.TotalHitError,
			};
		}
	}
}
