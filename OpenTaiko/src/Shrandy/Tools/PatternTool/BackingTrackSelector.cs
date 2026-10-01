using System.Globalization;

namespace OpenTaiko.Shrandy.Tools
{
	internal sealed class BackingTrackSelector
	{
		private readonly Dictionary<string, bool> m_EligibilityCache = new(StringComparer.OrdinalIgnoreCase);

		internal BackingTrack? Selected { get; private set; }

		internal bool IsEligible(Chart chart)
		{
			CScore? score = GetScore(chart);
			if (score == null)
			{
				return false;
			}

			string chartPath = score.ファイル情報.ファイルの絶対パス;
			string cacheKey = $"{chartPath}|{chart.Difficulty}";
			if (m_EligibilityCache.TryGetValue(cacheKey, out bool isEligible))
			{
				return isEligible;
			}

			string audioFileName = score.譜面情報.strBGMファイル名;
			bool hasFlatBpm = score.譜面情報.BaseBpm > 0.0
				&& score.譜面情報.MinBpm == score.譜面情報.MaxBpm;
			bool hasTjaChart = Path.GetExtension(chartPath).Equals(".tja", StringComparison.OrdinalIgnoreCase);
			string audioPath = Path.GetFullPath(Path.Combine(score.ファイル情報.フォルダの絶対パス, audioFileName ?? ""));
			isEligible = hasFlatBpm
				&& hasTjaChart
				&& !string.IsNullOrWhiteSpace(audioFileName)
				&& File.Exists(chartPath)
				&& File.Exists(audioPath);
			m_EligibilityCache[cacheKey] = isEligible;
			return isEligible;
		}

		internal bool Select(Chart? chart)
		{
			if (chart == null)
			{
				if (Selected == null)
				{
					return false;
				}

				Selected = null;
				return true;
			}

			if (!IsEligible(chart.Value))
			{
				return false;
			}

			if (Selected != null
				&& ReferenceEquals(Selected.Song, chart.Value.Song)
				&& Selected.Difficulty == chart.Value.Difficulty)
			{
				return false;
			}

			CScore score = GetScore(chart.Value)!;
			string chartPath = score.ファイル情報.ファイルの絶対パス;
			string audioPath = Path.GetFullPath(Path.Combine(score.ファイル情報.フォルダの絶対パス, score.譜面情報.strBGMファイル名));
			string songTitle = chart.Value.Song.ldTitle.GetString("");
			string difficultyName = ((Difficulty)chart.Value.Difficulty).ToString();
			string displayName = $"{songTitle} [{difficultyName}]";
			Selected = new BackingTrack(chart.Value.Song, chart.Value.Difficulty, displayName, audioPath, score.譜面情報.BaseBpm, ReadChartOffset(chartPath));
			return true;
		}

		private static CScore? GetScore(Chart chart)
		{
			if (chart.Song == null || chart.Difficulty < 0 || chart.Difficulty >= chart.Song.score.Length)
			{
				return null;
			}

			return chart.Song.score[chart.Difficulty];
		}

		private static double ReadChartOffset(string chartPath)
		{
			string[] chartLines = CJudgeTextEncoding.ReadTextFile(chartPath).Split('\n');
			foreach (string chartLine in chartLines)
			{
				string headerLine = chartLine.Trim();
				if (headerLine.StartsWith("#START", StringComparison.OrdinalIgnoreCase))
				{
					break;
				}

				int separatorIndex = headerLine.IndexOf(':');
				if (separatorIndex < 0 || !headerLine[..separatorIndex].Trim().Equals("OFFSET", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				string offsetText = headerLine[(separatorIndex + 1)..].Trim().Replace(',', '.');
				if (double.TryParse(offsetText, NumberStyles.Float, CultureInfo.InvariantCulture, out double offset))
				{
					return offset;
				}
			}

			return 0.0;
		}
	}

	internal sealed class BackingTrack
	{
		internal CSongListNode Song { get; }
		internal int Difficulty { get; }
		internal string DisplayName { get; }
		internal string AudioPath { get; }
		internal double Bpm { get; }
		internal double Offset { get; }

		internal BackingTrack(CSongListNode song, int difficulty, string displayName, string audioPath, double bpm, double offset)
		{
			Song = song;
			Difficulty = difficulty;
			DisplayName = displayName;
			AudioPath = audioPath;
			Bpm = bpm;
			Offset = offset;
		}
	}
}