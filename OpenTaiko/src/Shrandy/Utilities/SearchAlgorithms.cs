namespace OpenTaiko.Shrandy
{
	public static class SearchAlgorithms
	{
		public static bool FuzzyMatch(string pattern, string text)
		{
			return FuzzyMatch(pattern, text, out _);
		}

		public static bool FuzzyMatch(string pattern, string text, out int score)
		{
			score = 0;
			if (string.IsNullOrEmpty(pattern))
				return true;
			if (string.IsNullOrEmpty(text))
				return false;

			// Try strict subsequence match first (highest quality match)
			if (pattern.Length <= text.Length &&
				FuzzyMatchRecursive(pattern, text, 0, 0, null, new int[256], 0, out score))
				return true;

			// Fall back to word-level matching with typo tolerance
			return WordFuzzyMatch(pattern, text, out score);
		}

		private const int SequentialBonus = 15;
		private const int SeparatorBonus = 30;
		private const int CamelBonus = 30;
		private const int FirstLetterBonus = 15;
		private const int LeadingLetterPenalty = -5;
		private const int MaxLeadingLetterPenalty = -15;
		private const int UnmatchedLetterPenalty = -1;
		private const int MaxRecursionDepth = 10;

		private static bool FuzzyMatchRecursive(
			string pattern, string text,
			int patternIdx, int textIdx,
			int[]? srcMatches, int[] matches,
			int recursionDepth, out int outScore)
		{
			outScore = 0;

			if (recursionDepth >= MaxRecursionDepth)
				return false;

			int patternLen = pattern.Length;
			int textLen = text.Length;
			bool hadRecursiveMatch = false;
			int bestRecursiveScore = 0;
			int[]? bestRecursiveMatches = null;

			if (srcMatches != null)
				Array.Copy(srcMatches, matches, patternLen);

			while (patternIdx < patternLen && textIdx < textLen)
			{
				if (char.ToLowerInvariant(pattern[patternIdx]) == char.ToLowerInvariant(text[textIdx]))
				{
					// Try a recursive match where we skip this text character
					int[] recursiveMatches = new int[256];
					if (FuzzyMatchRecursive(pattern, text, patternIdx, textIdx + 1,
						matches, recursiveMatches, recursionDepth + 1, out int recursiveScore))
					{
						if (!hadRecursiveMatch || recursiveScore > bestRecursiveScore)
						{
							bestRecursiveScore = recursiveScore;
							bestRecursiveMatches = recursiveMatches;
						}
						hadRecursiveMatch = true;
					}

					matches[patternIdx] = textIdx;
					patternIdx++;
				}
				textIdx++;
			}

			if (patternIdx != patternLen)
				return false;

			// Calculate score
			int score = 0;

			for (int i = 0; i < patternLen; i++)
			{
				int matchIdx = matches[i];

				// Penalty for distance from start
				if (i == 0)
				{
					int penalty = matchIdx * LeadingLetterPenalty;
					score += Math.Max(penalty, MaxLeadingLetterPenalty);
				}

				// Sequential bonus
				if (i > 0 && matches[i] == matches[i - 1] + 1)
					score += SequentialBonus;

				// Separator / camel case bonus
				if (matchIdx > 0)
				{
					char prev = text[matchIdx - 1];
					char curr = text[matchIdx];

					if (prev == '_' || prev == ' ' || prev == '.' || prev == '-' || prev == '/')
						score += SeparatorBonus;
					else if (char.IsLower(prev) && char.IsUpper(curr))
						score += CamelBonus;
				}

				// First letter bonus
				if (matchIdx == 0)
					score += FirstLetterBonus;

				// Exact case bonus
				if (pattern[i] == text[matchIdx])
					score += 1;
			}

			// Penalty for unmatched text letters
			int unmatched = text.Length - patternLen;
			score += unmatched * UnmatchedLetterPenalty;

			if (hadRecursiveMatch && bestRecursiveScore > score)
			{
				outScore = bestRecursiveScore;
				return true;
			}

			outScore = score;
			return true;
		}

		private static bool WordFuzzyMatch(string pattern, string text, out int score)
		{
			score = 0;
			string[] patternTokens = pattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (patternTokens.Length == 0)
				return true;

			string[] textTokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (textTokens.Length == 0)
				return false;

			int totalScore = 0;

			foreach (string pt in patternTokens)
			{
				int maxDist = pt.Length <= 2 ? 0 : (pt.Length <= 4 ? 1 : 2);
				int bestDist = int.MaxValue;

				foreach (string tt in textTokens)
				{
					int dist = PrefixEditDistance(pt, tt);
					bestDist = Math.Min(bestDist, dist);
					if (bestDist == 0) break;
				}

				if (bestDist > maxDist)
					return false;

				totalScore += (10 - bestDist * 3);
			}

			score = totalScore;
			return true;
		}

		/// <summary>
		/// Minimum edit distance between pattern and any prefix of text.
		/// Allows partial word matching (e.g. "yu" matches "yuugen").
		/// </summary>
		private static int PrefixEditDistance(string pattern, string text)
		{
			int m = pattern.Length;
			int n = text.Length;
			int[] prev = new int[n + 1];
			int[] curr = new int[n + 1];

			for (int j = 0; j <= n; j++)
				prev[j] = j;

			for (int i = 1; i <= m; i++)
			{
				curr[0] = i;
				for (int j = 1; j <= n; j++)
				{
					int cost = char.ToLowerInvariant(pattern[i - 1]) == char.ToLowerInvariant(text[j - 1]) ? 0 : 1;
					curr[j] = Math.Min(
						Math.Min(curr[j - 1] + 1, prev[j] + 1),
						prev[j - 1] + cost);
				}
				(prev, curr) = (curr, prev);
			}

			int min = prev[0];
			for (int j = 1; j <= n; j++)
				min = Math.Min(min, prev[j]);

			return min;
		}
	}
}
