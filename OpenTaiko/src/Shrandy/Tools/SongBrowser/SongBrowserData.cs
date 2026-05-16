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

	internal struct ResultsSnapshot
	{
		public SongEntry CurrentEntry;
		public SongEntry? PreviousBest;
		public SongEntry? NoModBest;
	}

	internal class SongBrowserData
	{
		private const string SaveFileName = "song_history.json";
		private const string TagsSaveFileName = "song_tags.json";

		// History
		private SongHistorySaveData m_SaveData = new();
		private SongTagsSaveData m_TagsSaveData = new();
		private Dictionary<(string title, int difficulty), SongEntry> m_BestPlays = new();
		private Dictionary<(string title, int difficulty), SongEntry> m_LastPlays = new();
		private Dictionary<(string title, int difficulty), SongAggregateStats> m_AggregateStats = new();

		public SongTagsData Tags { get; private set; } = null!;

		// Session tracking
		private int m_SongCountAtStartOfSession = 0;
		private DateTime m_SessionStartTime = DateTime.Now;

		// History filter
		public int FilterDays = 0;
		private string m_HistoryFilterText = "";

		public string HistoryFilterText
		{
			get => m_HistoryFilterText;
			set => m_HistoryFilterText = value;
		}

		// Note stats for current song
		public NoteStats CurrentNoteStats { get; private set; } = CreateFreshNoteStats();

		public void ResetCurrentNoteStats()
		{
			CurrentNoteStats = CreateFreshNoteStats();
		}

		private static NoteStats CreateFreshNoteStats()
		{
			return new NoteStats
			{
				LeftHandStats = new NoteStats(),
				RightHandStats = new NoteStats(),
			};
		}

		// All songs
		private List<CSongListNode> m_AllSongs = new();
		private List<(CSongListNode song, int difficulty)> m_FilteredSongs = new();
		private HashSet<int> m_SelectedDifficulties = new() { (int)Difficulty.Oni, (int)Difficulty.Edit };
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

		private static readonly Dictionary<string, int> TimingNames = new(StringComparer.OrdinalIgnoreCase)
		{
			["loose"] = 0, ["lenient"] = 1, ["normal"] = 2, ["strict"] = 3, ["rigorous"] = 4,
		};

		private static readonly Regex FilterTokenRegex = new(@"(\w+)\s*(!=|>=|<=|>|<|=)\s*(\S+)", RegexOptions.Compiled);

		// Public accessors
		public SongHistorySaveData SaveData => m_SaveData;
		public List<CSongListNode> AllSongs => m_AllSongs;
		public List<(CSongListNode song, int difficulty)> FilteredSongs => m_FilteredSongs;
		public ResultsSnapshot? CurrentResultsSnapshot { get; private set; }

		public bool IsDifficultySelected(int diff) => m_SelectedDifficulties.Contains(diff);

		public void ToggleDifficulty(int diff)
		{
			if (m_SelectedDifficulties.Contains(diff))
			{
				if (m_SelectedDifficulties.Count > 1)
				{
					m_SelectedDifficulties.Remove(diff);
				}
			}
			else
			{
				m_SelectedDifficulties.Add(diff);
			}
			m_NeedsRefresh = true;
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
			m_TagsSaveData = Utilities.SaveHelper.LoadOrCreate<SongTagsSaveData>(TagsSaveFileName);
			Tags = new SongTagsData(m_TagsSaveData, () => m_NeedsRefresh = true);
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
		public SongEntry? GetLastPlay(string title, int difficulty)
		{
			if (m_LastPlays.TryGetValue((title.ToLowerInvariant(), difficulty), out SongEntry? entry))
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
			m_LastPlays.Clear();
			m_AggregateStats.Clear();

			foreach (SongEntry entry in m_SaveData.SongEntries)
			{
				int diff = Utilities.SongTable.GetDifficultyFromLabel(entry.Difficulty);
				(string title, int difficulty) key = (entry.SongTitle.ToLowerInvariant(), diff);

				if (!m_BestPlays.TryGetValue(key, out SongEntry? existing) || entry.Score > existing.Score)
				{
					m_BestPlays[key] = entry;
				}

				// SongEntries is in chronological order, so later entries overwrite earlier ones
				m_LastPlays[key] = entry;

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

		public (CSongListNode song, int difficulty)? GetRandomFilteredSong()
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

		public bool ApplyFiltersIfNeeded()
		{
			if (!m_NeedsRefresh)
			{
				return false;
			}
			ApplyFilters();
			m_NeedsRefresh = false;
			return true;
		}

		private void ApplyFilters()
		{
			m_FilteredSongs.Clear();

			ParseFilterText(m_FilterText, out List<(string field, string op, string value)> filters, out string titleSearch);

			foreach (CSongListNode song in m_AllSongs)
			{
				string? title = null;

				foreach (int diff in m_SelectedDifficulties.OrderBy(d => d))
				{
					if (song.score[diff] == null || song.nLevel[diff] < 0)
					{
						continue;
					}

					if (!string.IsNullOrEmpty(titleSearch))
					{
						title ??= song.ldTitle.GetString("").ToLowerInvariant();
						if (!title.Contains(titleSearch))
						{
							break;
						}
					}

					CScore score = song.score[diff];
					int level = song.nLevel[diff];

					if (!PassesAllFilters(song, score, level, diff, filters))
					{
						continue;
					}

					m_FilteredSongs.Add((song, diff));
				}
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

		private bool PassesAllFilters(CSongListNode song, CScore score, int level, int difficulty, List<(string field, string op, string value)> filters)
		{
			// Tag filters are evaluated with AND semantics (each tag token must independently pass).
			// Numeric filters with the same (field, op) pair are grouped and evaluated with OR semantics
			// so that e.g. "level=7 level=8" shows songs at level 7 OR 8.
			// Groups with different (field, op) pairs are still AND'd together.

			var numericGroups = new Dictionary<(string field, string op), List<string>>();

			foreach ((string field, string op, string value) in filters)
			{
				if (field == "tag")
				{
					bool hasTag = Tags.SongHasTag(song.ldTitle.GetString(""), difficulty, value);
					if (op == "!=" && hasTag) return false;
					if (op != "!=" && !hasTag) return false;
					continue;
				}

				var key = (field, op);
				if (!numericGroups.TryGetValue(key, out List<string>? group))
				{
					group = new List<string>();
					numericGroups[key] = group;
				}
				group.Add(value);
			}

			double? songValue = null;
			string? lastField = null;

			foreach (var kvp in numericGroups)
			{
				string field = kvp.Key.field;
				string op = kvp.Key.op;

				if (field != lastField)
				{
					songValue = GetFieldValue(song, score, level, field, difficulty);
					lastField = field;
				}

				if (songValue == null)
				{
					continue;
				}

				bool groupPassed = false;
				foreach (string value in kvp.Value)
				{
					double? targetValue = ResolveValue(field, value);
					if (targetValue == null)
					{
						continue;
					}
					if (CompareValues(songValue.Value, op, targetValue.Value))
					{
						groupPassed = true;
						break;
					}
				}

				if (!groupPassed)
				{
					return false;
				}
			}

			return true;
		}

		private double? GetFieldValue(CSongListNode song, CScore score, int level, string field, int difficulty)
		{
			SongEntry? bestPlay = GetBestPlay(song.ldTitle.GetString(""), difficulty);
			int scoreRank = bestPlay?.ScoreRank ?? 0;

			SongAggregateStats agg = GetAggregateStats(song.ldTitle.GetString(""), difficulty);

			return field switch
			{
				"bpm" => score.譜面情報.BaseBpm,
				"level" or "lv" => level,
				"badge" or "rank" => scoreRank,
				"score" => bestPlay?.Score,
				"lastplayed" => GetDaysSinceLastPlayed(song.ldTitle.GetString(""), difficulty),
				"lastpb" => GetDaysSinceLastPB(song.ldTitle.GetString(""), difficulty),
				"judgement" => bestPlay?.Judgement,
				"fadingnote" => bestPlay?.UsedFadingNote == true ? 1 : 0,
				"plays" => agg.PlayCount,
				"fc" => agg.FCCount,
				"dfc" => agg.DFCCount,
				_ => null,
			};
		}

		public double GetDaysSinceLastPlayed(string title, int difficulty)
		{
			SongEntry? last = GetLastPlay(title, difficulty);
			return last == null ? double.MaxValue : (DateTime.Now - last.Timestamp).TotalDays;
		}

		public double GetDaysSinceLastPB(string title, int difficulty)
		{
			SongEntry? best = GetBestPlay(title, difficulty);
			return best == null ? double.MaxValue : (DateTime.Now - best.Timestamp).TotalDays;
		}

		public SongEntry? GetBestPlayNoMods(string title, int difficulty)
		{
			string diffLabel = Utilities.SongHelper.GetDifficultyLabel(difficulty);
			SongEntry? best = null;
			foreach (SongEntry e in m_SaveData.SongEntries)
			{
				if (!e.SongTitle.Equals(title, StringComparison.OrdinalIgnoreCase)) continue;
				if (e.Difficulty != diffLabel) continue;
				if (e.RandomMod != "None" || e.Judgement != 2) continue;
				if (best == null || e.Score > best.Score) best = e;
			}
			return best;
		}

		public SongEntry? GetBestPlayMatchingMods(string title, int difficulty, string randomMod, int judgement)
		{
			string diffLabel = Utilities.SongHelper.GetDifficultyLabel(difficulty);
			SongEntry? best = null;
			foreach (SongEntry e in m_SaveData.SongEntries)
			{
				if (!e.SongTitle.Equals(title, StringComparison.OrdinalIgnoreCase)) continue;
				if (e.Difficulty != diffLabel) continue;
				if (e.RandomMod != randomMod || e.Judgement != judgement) continue;
				if (best == null || e.Score > best.Score) best = e;
			}
			return best;
		}

		public SongAggregateStats GetAggregateStatsNoMods(string title, int difficulty)
		{
			string diffLabel = Utilities.SongHelper.GetDifficultyLabel(difficulty);
			SongAggregateStats agg = default;
			foreach (SongEntry e in m_SaveData.SongEntries)
			{
				if (!e.SongTitle.Equals(title, StringComparison.OrdinalIgnoreCase)) continue;
				if (e.Difficulty != diffLabel) continue;
				if (e.RandomMod != "None" || e.Judgement != 2) continue;
				agg.PlayCount++;
				if (e.Bads == 0) { agg.FCCount++; if (e.Okays == 0) agg.DFCCount++; }
			}
			return agg;
		}

		public SongAggregateStats GetAggregateStatsMatchingMods(string title, int difficulty, string randomMod, int judgement)
		{
			string diffLabel = Utilities.SongHelper.GetDifficultyLabel(difficulty);
			SongAggregateStats agg = default;
			foreach (SongEntry e in m_SaveData.SongEntries)
			{
				if (!e.SongTitle.Equals(title, StringComparison.OrdinalIgnoreCase)) continue;
				if (e.Difficulty != diffLabel) continue;
				if (e.RandomMod != randomMod || e.Judgement != judgement) continue;
				agg.PlayCount++;
				if (e.Bads == 0) { agg.FCCount++; if (e.Okays == 0) agg.DFCCount++; }
			}
			return agg;
		}

		public SongEntry? GetLastPlayNoMods(string title, int difficulty)
		{
			string diffLabel = Utilities.SongHelper.GetDifficultyLabel(difficulty);
			SongEntry? last = null;
			foreach (SongEntry e in m_SaveData.SongEntries)
			{
				if (!e.SongTitle.Equals(title, StringComparison.OrdinalIgnoreCase)) continue;
				if (e.Difficulty != diffLabel) continue;
				if (e.RandomMod != "None" || e.Judgement != 2) continue;
				if (last == null || e.Timestamp > last.Timestamp) last = e;
			}
			return last;
		}

		public SongEntry? GetLastPlayMatchingMods(string title, int difficulty, string randomMod, int judgement)
		{
			string diffLabel = Utilities.SongHelper.GetDifficultyLabel(difficulty);
			SongEntry? last = null;
			foreach (SongEntry e in m_SaveData.SongEntries)
			{
				if (!e.SongTitle.Equals(title, StringComparison.OrdinalIgnoreCase)) continue;
				if (e.Difficulty != diffLabel) continue;
				if (e.RandomMod != randomMod || e.Judgement != judgement) continue;
				if (last == null || e.Timestamp > last.Timestamp) last = e;
			}
			return last;
		}

		public string GetCurrentModsLabel()
		{
			int actualPlayer = OpenTaiko.GetActualPlayer(0);
			return BuildModsLabel(OpenTaiko.ConfigIni.eRandom[actualPlayer], OpenTaiko.ConfigIni.nFunMods[actualPlayer]);
		}

		public int GetCurrentJudgement()
		{
			return OpenTaiko.ConfigIni.nTimingZones[OpenTaiko.SaveFile];
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

			if (field == "clear" && ClearNames.TryGetValue(value, out int clearVal))
			{
				return clearVal;
			}

			if (field == "judgement" && TimingNames.TryGetValue(value, out int timingVal))
			{
				return timingVal;
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

		private static int ComputeCrown(int player, CStage演奏画面共通.CBRANCHSCORE score)
		{
			if (!HGaugeMethods.UNSAFE_FastNormaCheck(player))
				return 0;
			bool assistedClear = OpenTaiko.stageSongSelect.actPlayOption.tGetModMultiplier(CActPlayOption.EBalancingType.SCORE, false, player) < 1f;
			if (assistedClear)
				return 0;
			if (score.nMiss == 0 && score.nMine == 0)
				return score.nGood == 0 ? 3 : 2;
			return 1;
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
			int scoreRank = Utilities.SongHelper.GetScoreRank(player, currentScore);

			SongEntry entry = new()
			{
				SongTitle = song.ldTitle.GetString(""),
				Difficulty = Utilities.SongHelper.GetDifficultyLabel(difficulty),
				Timestamp = DateTime.Now,
				Score = score.nScore,
				Goods = score.nGreat,
				Okays = score.nGood,
				Bads = score.nMiss,
				Rolls = score.nRoll,
				MaxCombo = OpenTaiko.stageGameScreen.actCombo?.nCurrentCombo.最高値[player] ?? 0,
				DurationMs = Utilities.SongHelper.GetSongDurationMs(),
				ScoreRank = scoreRank,
				ChartLevel = chartLevel,
				BaseBpm = song.score[difficulty]?.譜面情報.BaseBpm ?? 0,
				MinBpm = song.score[difficulty]?.譜面情報.MinBpm ?? 0,
				MaxBpm = song.score[difficulty]?.譜面情報.MaxBpm ?? 0,
				SongSpeed = OpenTaiko.ConfigIni.nSongSpeed,
				Judgement = OpenTaiko.ConfigIni.nTimingZones[OpenTaiko.SaveFile],
				RandomMod = BuildModsLabel(OpenTaiko.ConfigIni.eRandom[actualPlayer], OpenTaiko.ConfigIni.nFunMods[actualPlayer]),
				AvgHitError = CurrentNoteStats.AverageHitError,
				AvgSync = CurrentNoteStats.AverageSync,
				AvgLeftHandError = CurrentNoteStats.LeftHandStats?.AverageHitError ?? 0,
				AvgRightHandError = CurrentNoteStats.RightHandStats?.AverageHitError ?? 0,
				LeftHandOkays = CurrentNoteStats.LeftHandStats?.OkayCount ?? 0,
				RightHandOkays = CurrentNoteStats.RightHandStats?.OkayCount ?? 0,
				UsedFadingNote = OpenTaiko.ConfigIni.nFadingNoteTime > 0,
				EarlyHits = CurrentNoteStats.EarlyCount,
				LateHits = CurrentNoteStats.LateCount,
				Crown = ComputeCrown(player, score),
			};

			SongEntry? previousBest = GetBestPlayMatchingMods(entry.SongTitle, difficulty, entry.RandomMod, entry.Judgement);
			bool hasMods = entry.RandomMod != "None" || entry.Judgement != 2 || entry.SongSpeed != CConfigIni.DefaultSongSpeed || entry.UsedFadingNote;
			SongEntry? noModBest = hasMods ? GetBestPlayMatchingMods(entry.SongTitle, difficulty, "None", 2) : null;

			m_SaveData.SongEntries.Add(entry);

			CurrentResultsSnapshot = new ResultsSnapshot
			{
				CurrentEntry = entry,
				PreviousBest = previousBest,
				NoModBest = noModBest,
			};
		}

		public void SaveHistory()
		{
			Utilities.SaveHelper.Save(SaveFileName, m_SaveData);
		}

		public void SaveTags()
		{
			Utilities.SaveHelper.Save(TagsSaveFileName, m_TagsSaveData);
		}

		private static string BuildModsLabel(ERandomMode randomMode, EFunMods funMod)
		{
			var parts = new List<string>();
			string random = GetRandomModLabel(randomMode);
			if (random != "None") parts.Add(random);
			string fun = GetFunModLabel(funMod);
			if (fun != "None") parts.Add(fun);
			return parts.Count > 0 ? string.Join(",", parts) : "None";
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

		private static string GetFunModLabel(EFunMods funMod)
		{
			return funMod switch
			{
				EFunMods.None => "None",
				EFunMods.ForceAllDon => "AllDon",
				EFunMods.Avalanche => "Avalanche",
				EFunMods.Minesweeper => "Minesweeper",
				_ => funMod.ToString()
			};
		}
		
		public int GetLastChosenDifficulty()
		{
			if (m_SaveData.SongEntries.Count == 0)
			{
				return 0;
			}

			SongEntry lastEntry = m_SaveData.SongEntries.Last();
			return Utilities.SongTable.GetDifficultyFromLabel(lastEntry.Difficulty);
		}
	}
}
