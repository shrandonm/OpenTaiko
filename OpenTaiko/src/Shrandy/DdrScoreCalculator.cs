using System;
using System.Collections.Generic;

namespace OpenTaiko.Shrandy
{
	internal class DdrJudgementCounts
	{
		public int Marvelous { get; set; }
		public int Perfect { get; set; }
		public int Great { get; set; }
		public int Good { get; set; }
		public int Miss { get; set; }

		/// <summary>Non-Marvelous hits with a negative timing error (hit early).</summary>
		public int Fast { get; set; }

		/// <summary>Non-Marvelous hits with a positive timing error (hit late).</summary>
		public int Late { get; set; }

		/// <summary>DDR's "OK" judgement applies to freeze/hold arrows, which have no equivalent note type in OpenTaiko, so this is always 0.</summary>
		public int Ok { get; set; }
	}

	/// <summary>Calculates a DDR-style score and letter grade from a chart's step count and hit timing data.</summary>
	internal static class DdrScoreCalculator
	{
		public const int MaxScore = 1000000;

		public const int MarvelousWindowMs = 16;
		public const int PerfectWindowMs = 33;
		public const int GreatWindowMs = 108;
		public const int GoodWindowMs = 158;
		public const int MissWindowMs = 191;

		public static float CalculateStepScore(int totalSteps)
		{
			if (totalSteps <= 0)
			{
				return 0.0f;
			}

			return (float)MaxScore / totalSteps;
		}

		/// <summary>
		/// Buckets a hit-error histogram (ms error -> count) into DDR judgement counts.
		/// Any steps not accounted for by the histogram (true misses, or hits outside the Good window) are counted as Miss.
		/// </summary>
		public static DdrJudgementCounts ClassifyHitDistribution(Dictionary<int, int> hitDistribution, int totalSteps)
		{
			DdrJudgementCounts counts = new();

			foreach (KeyValuePair<int, int> bucket in hitDistribution)
			{
				int absError = Math.Abs(bucket.Key);

				if (absError <= MarvelousWindowMs)
				{
					counts.Marvelous += bucket.Value;
				}
				else if (absError <= PerfectWindowMs)
				{
					counts.Perfect += bucket.Value;
				}
				else if (absError <= GreatWindowMs)
				{
					counts.Great += bucket.Value;
				}
				else if (absError <= GoodWindowMs)
				{
					counts.Good += bucket.Value;
				}

				if (absError > MarvelousWindowMs && absError <= GoodWindowMs)
				{
					if (bucket.Key < 0)
					{
						counts.Fast += bucket.Value;
					}
					else if (bucket.Key > 0)
					{
						counts.Late += bucket.Value;
					}
				}
			}

			int classified = counts.Marvelous + counts.Perfect + counts.Great + counts.Good;
			counts.Miss = Math.Max(0, totalSteps - classified);
			return counts;
		}

		public static int CalculateScore(DdrJudgementCounts judgements, float stepScore)
		{
			double score = stepScore * (judgements.Marvelous + judgements.Ok)
				+ (stepScore - 10.0) * judgements.Perfect
				+ (stepScore * 3.0 / 5.0 - 10.0) * judgements.Great
				+ (stepScore * 1.0 / 5.0 - 10.0) * judgements.Good;

			return Math.Clamp((int)Math.Round(score), 0, MaxScore);
		}

		public static string CalculateGrade(int score, bool failed)
		{
			if (failed)
			{
				return "E";
			}

			return score switch
			{
				>= 990000 => "AAA",
				>= 950000 => "AA+",
				>= 900000 => "AA",
				>= 890000 => "AA-",
				>= 850000 => "A+",
				>= 800000 => "A",
				>= 790000 => "A-",
				>= 750000 => "B+",
				>= 700000 => "B",
				>= 690000 => "B-",
				>= 650000 => "C+",
				>= 600000 => "C",
				>= 590000 => "C-",
				>= 550000 => "D+",
				_ => "D",
			};
		}

		/// <summary>Returns the DDR-style combo type (MFC/PFC/GFC/FC), or "" if the play was not a full combo.</summary>
		public static string CalculateComboType(DdrJudgementCounts judgements)
		{
			if (judgements.Miss > 0)
			{
				return "";
			}

			if (judgements.Perfect == 0 && judgements.Great == 0 && judgements.Good == 0)
			{
				return "MFC";
			}

			if (judgements.Great == 0 && judgements.Good == 0)
			{
				return "PFC";
			}

			if (judgements.Good == 0)
			{
				return "GFC";
			}

			return "FC";
		}
	}
}
