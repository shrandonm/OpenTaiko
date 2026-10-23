using System;

namespace OpenTaiko.Shrandy
{
	/// <summary>
	/// Shadows the live game and works out what the score would have been with default timing windows.
	/// Notes are re-judged from their recorded hit error, then scored with the game's own scoring rules.
	/// </summary>
	internal class NormalTimingScoreTracker
	{
		private const int TrackedPlayer = 0;

		private long m_ActualNoteScore;
		private long m_NormalNoteScore;
		private int m_CurrentCombo;

		public int Goods { get; private set; }
		public int Okays { get; private set; }
		public int Bads { get; private set; }
		public int MaxCombo { get; private set; }

		public void Reset()
		{
			m_ActualNoteScore = 0;
			m_NormalNoteScore = 0;
			m_CurrentCombo = 0;
			Goods = 0;
			Okays = 0;
			Bads = 0;
			MaxCombo = 0;
		}

		public void OnNoteScoreAwarded(int player, long noteScore)
		{
			if (player == TrackedPlayer)
			{
				m_ActualNoteScore += noteScore;
			}
		}

		public void OnNoteJudged(CChip chip, int player, ENoteJudge actualJudge)
		{
			if (player != TrackedPlayer)
			{
				return;
			}

			if (NotesManager.IsMine(chip))
			{
				if (actualJudge == ENoteJudge.Bad)
				{
					m_CurrentCombo = 0;
				}
				return;
			}

			if (!NotesManager.IsMissableNote(chip))
			{
				return;
			}

			ENoteJudge? normalJudge = GetNormalJudge(chip, player, actualJudge);
			if (normalJudge == null)
			{
				return;
			}

			ApplyJudge(chip, player, normalJudge.Value);
		}

		/// <summary>Converts the final score of the play into the score it would have had with default timing.</summary>
		public int ConvertScore(int actualScore)
		{
			long convertedScore = actualScore - m_ActualNoteScore + m_NormalNoteScore;
			return (int)Math.Max(0, convertedScore);
		}

		private static ENoteJudge? GetNormalJudge(CChip chip, int player, ENoteJudge actualJudge)
		{
			switch (actualJudge)
			{
				case ENoteJudge.Perfect:
				case ENoteJudge.Great:
				case ENoteJudge.Good:
				case ENoteJudge.Poor:
				case ENoteJudge.Bad:
					return OpenTaiko.stageGameScreen.JudgeChipWithDefaultTiming(chip, player);
				case ENoteJudge.Miss:
					return chip.IsMissed ? ENoteJudge.Miss : null;
				default:
					return null;
			}
		}

		private void ApplyJudge(CChip chip, int player, ENoteJudge normalJudge)
		{
			if (normalJudge == ENoteJudge.Perfect || normalJudge == ENoteJudge.Great || normalJudge == ENoteJudge.Good)
			{
				m_CurrentCombo++;
				MaxCombo = Math.Max(MaxCombo, m_CurrentCombo);
				m_NormalNoteScore += OpenTaiko.stageGameScreen.CalculateNoteScore(chip, player, normalJudge, m_CurrentCombo);

				if (normalJudge == ENoteJudge.Perfect)
				{
					Goods++;
				}
				else
				{
					Okays++;
				}
				return;
			}

			m_CurrentCombo = 0;
			Bads++;
		}
	}
}
