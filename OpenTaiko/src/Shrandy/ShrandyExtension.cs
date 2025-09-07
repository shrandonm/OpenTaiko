using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	public enum Hand
	{
		Left,
		Right,
	}

	public enum Note
	{
		Don,
		Ka,
	}

	internal struct HitParams
	{
		public CChip Chip;
		public ENoteJudge JudgeResult;
		public Hand Hand;
		public Note Note;
	}

	static class ShrandyExtension
	{
		public static void OnNoteHit(CChip chip, ENoteJudge judgeResult, EPad pad)
		{
			HitParams hitParams = new()
			{
				Chip = chip,
				JudgeResult = judgeResult,
				Hand = GetHandFromPad(pad),
				Note = GetNoteFromPad(pad),
			};
			OpenTaiko.stageGameScreen.m_ShrandyGameOverlay.OnNoteHit(hitParams);
		}

		private static Hand GetHandFromPad(EPad pad)
		{
			switch (pad)
			{
				case EPad.LRed:
				case EPad.LBlue:
				case EPad.LRed2P:
				case EPad.LBlue2P:
					return Hand.Left;
				case EPad.RRed:
				case EPad.RBlue:
				case EPad.RRed2P:
				case EPad.RBlue2P:
					return Hand.Right;
			}
			return Hand.Left;
		}

		private static Note GetNoteFromPad(EPad pad)
		{
			switch (pad)
			{
				case EPad.RRed:
				case EPad.LRed:
				case EPad.RRed2P:
				case EPad.LRed2P:
					return Note.Don;
				case EPad.RBlue:
				case EPad.LBlue:
				case EPad.RBlue2P:
				case EPad.LBlue2P:
					return Note.Ka;
			}
			return Note.Don;
		}

		public static void OnPerformanceInfoActivate()
		{
			OpenTaiko.stageGameScreen.m_ShrandyGameOverlay.Reset();
		}

		public static bool IsGood(ENoteJudge judgement)
		{
			return judgement <= ENoteJudge.Perfect;
		}
	}
}
