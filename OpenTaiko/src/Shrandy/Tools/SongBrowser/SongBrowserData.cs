using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenTaiko.Shrandy.Tools
{
	internal struct SongAggregateStats
	{
		public int PlayCount;
		public int FCCount;
		public int DFCCount;
	}

	internal class SongBrowserData
	{
		private const string SaveFileName = "song_history.json";

		// History
		private SongHistorySaveData m_SaveData = new();
		private Dictionary<(string title, int difficulty), SongEntry> m_BestPlays = new();
		private Dictionary<(string title, int difficulty), SongAggregateStats> m_AggregateStats = new();

		// Session tracking
		private int m_SongCountAtStartOfSession = 0;
		private DateTime m_SessionStartTime = DateTime.Now;
		public int SessionTargetSongs = 25;
		public int DailyTargetSongs = 50;

		// History filter
		public int FilterDays = 0;

		// All songs
		private List<CSongListNode> m_AllSongs = new();
		private List<CSongListNode> m_FilteredSongs = new();
		private int m_SelectedDifficulty = (int)Difficulty.Oni;
		private string m_FilterText = "";
		private bool m_NeedsRefresh = true;

		public static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard", "Oni", "Ura" };

		private static readonly Dictionary<string, int> BadgeNames = new(StringComparer.OrdinalIgnoreCase)
		{
			["none"] = 0, ["white"] = 1, ["bronze"] = 2, ["silver"] = 3,
			["gold"] = 4, ["pink"] = 5, ["purple"] = 6, ["rainbow"] = 7,
		};

		private static readonly Dictionary<string, int> ClearNames = new(StringComparer.OrdinalIgnoreCase)
		{
			["none"] = 0, ["clear"] = 1, ["fc"] = 2, ["dfc"] = 3,
		};

		private static readonly Regex FilterTokenRegex = new(@"(\w+)\s*(>=|<=|>|<|=)\s*(\S+)", RegexOptions.Compiled);

		// Public accessors
		public SongHistorySaveData SaveData => m_SaveData;
		public List<CSongListNode> AllSongs => m_AllSongs;
		public List<CSongListNode> FilteredSongs => m_FilteredSongs;

		public int SelectedDifficulty
		{
			get => m_SelectedDifficulty;
			set
			{
				m_SelectedDifficulty = value;
				m_NeedsRefresh = true;
			}
		}

		public string FilterText
		{
			get => m_FilterText;
			set
			{
				m_FilterText = value;
				m_NeedsRefresh = true;
			}
		}

		public SongBrowserData()
		{
			m_SaveData = Utilities.SaveHelper.LoadOrCreate<SongHistorySaveData>(SaveFileName);
			m_SongCountAtStartOfSession = m_SaveData.SongEntries.Count;

#if DEBUG
			AddDummyData();
#endif
			RebuildBestPlaysCache();
		}

		private void AddDummyData()
		{
			for (int i = 0; i < 10; i++)
			{
				m_SaveData.SongEntries.Add(new SongEntry
				{
					SongTitle = $"Sample Song {i + 1}",
					Difficulty = "Hard",
					Timestamp = DateTime.Now.AddMinutes(-i * 5),
					Score = 987654,
					Goods = 300,
					Okays = 50,
					Bads = 10,
					Rolls = 20,
					MaxCombo = 350,
					DurationMs = 120000,
					ScoreRank = i % 5,
					ChartLevel = 8,
					RandomMod = "None",
					SongSpeed = CConfigIni.DefaultSongSpeed,
				});
			}
		}

		// --- Session tracking ---

		public TimeSpan GetSessionElapsed()
		{
			return DateTime.Now - m_SessionStartTime;
		}

		public int GetSessionSongCount()
		{
			return Math.Max(0, m_SaveData.SongEntries.Count - m_SongCountAtStartOfSession);
		}

		public int GetSessionDurationMs()
		{
			int sessionSongCount = GetSessionSongCount();
			if (sessionSongCount <= 0)
			{
				return 0;
			}
			return m_SaveData.SongEntries.Skip(m_SongCountAtStartOfSession).Sum(x => x.DurationMs);
		}

		public void ResetSessionStats()
		{
			m_SongCountAtStartOfSession = m_SaveData.SongEntries.Count;
			m_SessionStartTime = DateTime.Now;
		}

		// --- History ---

		public int GetSongCountSince(DateTime cutoff)
		{
			return m_SaveData.SongEntries.Count(x => x.Timestamp >= cutoff);
		}

		public int GetDailySongCount()
		{
			return GetSongCountSince(DateTime.Today);
		}

		public int GetWeeklySongCount()
		{
			DayOfWeek today = DateTime.Today.DayOfWeek;
			int daysSinceMonday = ((int)today + 6) % 7;
			DateTime weekStart = DateTime.Today.AddDays(-daysSinceMonday);
			return GetSongCountSince(weekStart);
		}

		public int GetMonthlySongCount()
		{
			DateTime monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
			return GetSongCountSince(monthStart);
		}

		public int GetSongCountFromCutoff(int days)
		{
			if (days == 0)
			{
				return m_SaveData.SongEntries.Count;
			}

			DateTime cutoff = DateTime.Today.AddDays(-(days - 1));
			return GetSongCountSince(cutoff);
		}

		public int CalculateHistoryStartIndex()
		{
			int startIndex = 0;
			if (FilterDays != 0)
			{
				startIndex = Math.Max(0, m_SaveData.SongEntries.Count - GetSongCountFromCutoff(FilterDays));
			}
			return startIndex;
		}

		// --- History lookup ---

		public SongEntry? GetBestPlay(string title, int difficulty)
		{
			if (m_BestPlays.TryGetValue((title.ToLowerInvariant(), difficulty), out SongEntry? entry))
			{
				return entry;
			}
			return null;
		}

		public SongAggregateStats GetAggregateStats(string title, int difficulty)
		{
			if (m_AggregateStats.TryGetValue((title.ToLowerInvariant(), difficulty), out SongAggregateStats stats))
			{
				return stats;
			}
			return default;
		}

		public void RebuildBestPlaysCache()
		{
			m_BestPlays.Clear();
			m_AggregateStats.Clear();

			foreach (SongEntry entry in m_SaveData.SongEntries)
			{
				int diff = Utilities.SongTable.GetDifficultyFromLabel(entry.Difficulty);
				(string title, int difficulty) key = (entry.SongTitle.ToLowerInvariant(), diff);

				if (!m_BestPlays.TryGetValue(key, out SongEntry? existing) || entry.Score > existing.Score)
				{
					m_BestPlays[key] = entry;
				}

				m_AggregateStats.TryGetValue(key, out SongAggregateStats agg);
				agg.PlayCount++;
				if (entry.Bads == 0)
				{
					agg.FCCount++;
					if (entry.Okays == 0)
					{
						agg.DFCCount++;
					}
				}
				m_AggregateStats[key] = agg;
			}
		}

		// --- Song list ---

		public CSongListNode? GetRandomFilteredSong()
		{
			if (m_FilteredSongs.Count == 0)
			{
				return null;
			}
			int index = Random.Shared.Next(m_FilteredSongs.Count);
			return m_FilteredSongs[index];
		}

		public void RefreshSongList()
		{
			m_AllSongs.Clear();

			if (OpenTaiko.stageSongSelect?.actSongList == null || OpenTaiko.Songs管理?.list曲ルート == null)
			{
				return;
			}

			List<CSongListNode> allNodes = OpenTaiko.stageSongSelect.actSongList.flattenList(OpenTaiko.Songs管理.list曲ルート);
			foreach (CSongListNode node in allNodes)
			{
				if (node.nodeType == CSongListNode.ENodeType.SCORE || node.nodeType == CSongListNode.ENodeType.SCORE_MIDI)
				{
					m_AllSongs.Add(node);
				}
			}

			m_NeedsRefresh = true;
		}

		public void ApplyFiltersIfNeeded()
		{
			if (!m_NeedsRefresh)
			{
				return;
			}
			ApplyFilters();
			m_NeedsRefresh = false;
		}

		private void ApplyFilters()
		{
			m_FilteredSongs.Clear();

			ParseFilterText(m_FilterText, out List<(string field, string op, string value)> filters, out string titleSearch);

			foreach (CSongListNode song in m_AllSongs)
			{
				CScore score = song.score[m_SelectedDifficulty];
				if (score == null)
				{
					continue;
				}

				int level = song.nLevel[m_SelectedDifficulty];
				if (level < 0)
				{
					continue;
				}

				if (!string.IsNullOrEmpty(titleSearch))
				{
					string title = song.ldTitle.GetString("").ToLowerInvariant();
					if (!SearchAlgorithms.FuzzyMatch(titleSearch, title))
					{
						continue;
					}
				}

				if (!PassesAllFilters(song, score, level, filters))
				{
					continue;
				}

				m_FilteredSongs.Add(song);
			}
		}

		private static void ParseFilterText(string text, out List<(string field, string op, string value)> filters, out string titleSearch)
		{
			filters = new();
			string remaining = text;

			foreach (Match match in FilterTokenRegex.Matches(text))
			{
				filters.Add((match.Groups[1].Value.ToLowerInvariant(), match.Groups[2].Value, match.Groups[3].Value));
				remaining = remaining.Replace(match.Value, "");
			}

			titleSearch = remaining.Trim().ToLowerInvariant();
		}

		private bool PassesAllFilters(CSongListNode song, CScore score, int level, List<(string field, string op, string value)> filters)
		{
			foreach ((string field, string op, string value) in filters)
			{
				double? songValue = GetFieldValue(song, score, level, field);
				double? targetValue = ResolveValue(field, value);

				if (songValue == null || targetValue == null)
				{
					continue;
				}

				if (!CompareValues(songValue.Value, op, targetValue.Value))
				{
					return false;
				}
			}
			return true;
		}

		private double? GetFieldValue(CSongListNode song, CScore score, int level, string field)
		{
			SongEntry? bestPlay = GetBestPlay(song.ldTitle.GetString(""), m_SelectedDifficulty);
			int scoreRank = bestPlay?.ScoreRank ?? 0;
			
			return field switch
			{
				"bpm" => score.譜面情報.BaseBpm,
				"level" or "lv" => level,
				"badge" or "rank" => scoreRank,
				"score" => bestPlay?.Score,
				_ => null,
			};
		}

		private static double? ResolveValue(string field, string value)
		{
			if (double.TryParse(value, out double numeric))
			{
				return numeric;
			}

			if ((field == "badge" || field == "rank") && BadgeNames.TryGetValue(value, out int badgeVal))
			{
				return badgeVal;
			}

			if ((field == "fc" || field == "clear") && ClearNames.TryGetValue(value, out int clearVal))
			{
				return clearVal;
			}

			return null;
		}

		private static bool CompareValues(double songValue, string op, double target)
		{
			return op switch
			{
				">" => songValue > target,
				"<" => songValue < target,
				">=" => songValue >= target,
				"<=" => songValue <= target,
				"=" => Math.Abs(songValue - target) < 0.001,
				_ => true,
			};
		}

		private static int GetClearStatus(CScore score, int difficulty)
		{
			int[] clears = score.譜面情報.nクリア;
			if (clears != null && difficulty < clears.Length)
			{
				return clears[difficulty];
			}
			return 0;
		}

		// --- Song recording ---

		public void TryAddCurrentSongStats()
		{
			if (OpenTaiko.stageGameScreen == null || OpenTaiko.stageSongSelect == null)
			{
				return;
			}

			CSongListNode? song = OpenTaiko.stageSongSelect.rChoosenSong;
			if (song == null)
			{
				return;
			}

			int player = 0;
			CStage演奏画面共通.CBRANCHSCORE score = OpenTaiko.stageGameScreen.CChartScore[player];
			int difficulty = OpenTaiko.stageSongSelect.nChoosenSongDifficulty[player];
			int chartLevel = difficulty >= 0 && difficulty < song.nLevel.Length ? song.nLevel[difficulty] : 0;
			int actualPlayer = OpenTaiko.GetActualPlayer(player);
			int currentScore = (int)OpenTaiko.stageGameScreen.actScore.Get(player);
			int scoreRank = Utilities.ScoreHelper.GetScoreRank(player, currentScore);

			SongEntry entry = new()
			{
				SongTitle = song.ldTitle.GetString(""),
				Difficulty = Utilities.SongTable.GetDifficultyLabel(difficulty),
				Timestamp = DateTime.Now,
				Score = score.nScore,
				Goods = score.nGreat,
				Okays = score.nGood,
				Bads = score.nMiss,
				Rolls = score.nRoll,
				MaxCombo = OpenTaiko.stageGameScreen.actCombo?.nCurrentCombo.最高値[player] ?? 0,
				DurationMs = Utilities.ScoreHelper.GetSongDurationMs(),
				ScoreRank = scoreRank,
				ChartLevel = chartLevel,
				BaseBpm = song.score[difficulty]?.譜面情報.BaseBpm ?? 0,
				MinBpm = song.score[difficulty]?.譜面情報.MinBpm ?? 0,
				MaxBpm = song.score[difficulty]?.譜面情報.MaxBpm ?? 0,
				SongSpeed = OpenTaiko.ConfigIni.nSongSpeed,
				RandomMod = GetRandomModLabel(OpenTaiko.ConfigIni.eRandom[actualPlayer])
			};

			m_SaveData.SongEntries.Add(entry);
		}

		public void SaveHistory()
		{
			Utilities.SaveHelper.Save(SaveFileName, m_SaveData);
		}

		private static string GetRandomModLabel(ERandomMode randomMode)
		{
			return randomMode switch
			{
				ERandomMode.Off => "None",
				ERandomMode.Random => "Shuffle",
				ERandomMode.SuperRandom => "Chaos",
				ERandomMode.Mirror => "Mirror",
				ERandomMode.MirrorRandom => "Mirror+Shuffle",
				_ => randomMode.ToString()
			};
		}
	}
}
