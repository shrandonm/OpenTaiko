using System;
using System.Collections.Generic;
using ImGuiNET;
using System.Numerics;

namespace OpenTaiko.Shrandy.Utilities
{
	internal struct SongTableRow
	{
		public string Title;
		public string TimeSince;
		public int ScoreRank;
		public int ChartLevel;
		public int Score;
		public int Goods;
		public int Okays;
		public int Bads;
		public int Rolls;
		public int MaxCombo;
		public string Duration;
		public string Difficulty;
		public int DifficultyIndex;
		public string Speed;
		public string RandomMod;
		public double BaseBpm;
		public double MinBpm;
		public double MaxBpm;
		public float AvgHitError;
		public float AvgSync;
		public float AvgLeftHandError;
		public float AvgRightHandError;
		public int LeftHandOkays;
		public int RightHandOkays;

		public int TotalNotes => Goods + Okays + Bads;
	}

	internal static class SongTable
	{
		private const int BaseColumnCount = 24;
		private const int AggregateColumnCount = 3;

		public static bool BeginTable(string id, ImGuiTableFlags extraFlags = ImGuiTableFlags.None, float height = 0, bool showAggregates = false)
		{
			int columnCount = showAggregates ? BaseColumnCount + AggregateColumnCount : BaseColumnCount;

			ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
				| ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollX
				| ImGuiTableFlags.Hideable | extraFlags;

			Vector2 size = height > 0 ? new Vector2(0, height) : Vector2.Zero;

			if (!ImGui.BeginTable(id, columnCount, flags, size))
			{
				return false;
			}

			ImGuiTableColumnFlags hide = ImGuiTableColumnFlags.DefaultHide;

			ImGui.TableSetupColumn("Song", ImGuiTableColumnFlags.WidthFixed, 128);
			ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 64);
			ImGui.TableSetupColumn("Badge", ImGuiTableColumnFlags.WidthFixed, 16);
			ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("BPM", ImGuiTableColumnFlags.WidthFixed, 80);
			ImGui.TableSetupColumn("Score", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Good%", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Goods", ImGuiTableColumnFlags.WidthFixed | hide, 48);
			ImGui.TableSetupColumn("Okays", ImGuiTableColumnFlags.WidthFixed | hide, 48);
			ImGui.TableSetupColumn("Bads", ImGuiTableColumnFlags.WidthFixed | hide, 48);
			ImGui.TableSetupColumn("Rolls", ImGuiTableColumnFlags.WidthFixed | hide, 48);
			ImGui.TableSetupColumn("Combo", ImGuiTableColumnFlags.WidthFixed | hide, 48);
			ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed | hide, 64);
			ImGui.TableSetupColumn("Total Notes", ImGuiTableColumnFlags.WidthFixed | hide, 64);
			ImGui.TableSetupColumn("Diff", ImGuiTableColumnFlags.WidthFixed, 20);
			ImGui.TableSetupColumn("Speed", ImGuiTableColumnFlags.WidthFixed | hide, 56);
			ImGui.TableSetupColumn("Random", ImGuiTableColumnFlags.WidthFixed | hide, 72);
			ImGui.TableSetupColumn("Creator", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide, 100);
			ImGui.TableSetupColumn("Avg Error", ImGuiTableColumnFlags.WidthFixed, 72);
			ImGui.TableSetupColumn("Avg Sync", ImGuiTableColumnFlags.WidthFixed | hide, 72);
			ImGui.TableSetupColumn("L.Avg Error", ImGuiTableColumnFlags.WidthFixed | hide, 80);
			ImGui.TableSetupColumn("R.Avg Error", ImGuiTableColumnFlags.WidthFixed | hide, 80);
			ImGui.TableSetupColumn("L.Okays", ImGuiTableColumnFlags.WidthFixed | hide, 56);
			ImGui.TableSetupColumn("R.Okays", ImGuiTableColumnFlags.WidthFixed | hide, 56);

			if (showAggregates)
			{
				ImGui.TableSetupColumn("Plays", ImGuiTableColumnFlags.WidthFixed, 40);
				ImGui.TableSetupColumn("FC", ImGuiTableColumnFlags.WidthFixed | hide, 40);
				ImGui.TableSetupColumn("DFC", ImGuiTableColumnFlags.WidthFixed | hide, 40);
			}

			ImGui.TableHeadersRow();

			return true;
		}

		public static void DrawAggregateColumns(int playCount, int fcCount, int dfcCount)
		{
			ImGui.TableSetColumnIndex(BaseColumnCount);
			ImGui.Text($"{playCount}");

			ImGui.TableSetColumnIndex(BaseColumnCount + 1);
			ImGui.Text($"{fcCount}");

			ImGui.TableSetColumnIndex(BaseColumnCount + 2);
			ImGui.Text($"{dfcCount}");
		}

		public static void EndTable()
		{
			ImGui.EndTable();
		}

		/// <summary>
		/// Draws columns 1+ for a row (caller handles column 0 and TableNextRow).
		/// </summary>
		public static void DrawRow(in SongTableRow row, string creator = "")
		{
			int totalNotes = row.TotalNotes;
			int col = 1;

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.TimeSince);

			ImGui.TableSetColumnIndex(col++);
			ScoreHelper.DrawScoreRank(row.ScoreRank, 16.0f);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.ChartLevel.ToString());

			ImGui.TableSetColumnIndex(col++);
			DrawBpm(row.BaseBpm, row.MinBpm, row.MaxBpm);

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.Score}");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{StringHelpers.GetPercentString(row.Goods, totalNotes)}%");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.Goods}");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.Okays}");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.Bads}");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.Rolls}");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.MaxCombo}");

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.Duration);

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{totalNotes}");

			ImGui.TableSetColumnIndex(col++);
			ScoreHelper.DrawDifficultyIcon(row.DifficultyIndex, 16.0f);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.Speed);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.RandomMod);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(creator);

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.AvgHitError:F2}ms");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.AvgSync:F2}ms");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.AvgLeftHandError:F2}ms");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.AvgRightHandError:F2}ms");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.LeftHandOkays}");

			ImGui.TableSetColumnIndex(col++);
			ImGui.Text($"{row.RightHandOkays}");
		}

		private static void DrawBpm(double baseBpm, double minBpm, double maxBpm)
		{
			if (baseBpm <= 0)
			{
				return;
			}

			if (minBpm > 0 && maxBpm > 0 && Math.Abs(minBpm - maxBpm) > 1)
			{
				ImGui.Text($"{minBpm:F0}-{maxBpm:F0}");
			}
			else
			{
				ImGui.Text($"{baseBpm:F0}");
			}
		}

		public static SongTableRow FromSongEntry(SongEntry entry)
		{
			return new SongTableRow
			{
				Title = entry.SongTitle,
				TimeSince = StringHelpers.GetTimeSinceString(entry.Timestamp),
				ScoreRank = entry.ScoreRank,
				ChartLevel = entry.ChartLevel,
				BaseBpm = entry.BaseBpm,
				MinBpm = entry.MinBpm,
				MaxBpm = entry.MaxBpm,
				Score = entry.Score,
				Goods = entry.Goods,
				Okays = entry.Okays,
				Bads = entry.Bads,
				Rolls = entry.Rolls,
				MaxCombo = entry.MaxCombo,
				Duration = FormatDuration(entry.DurationMs),
				Difficulty = entry.Difficulty,
				DifficultyIndex = GetDifficultyFromLabel(entry.Difficulty),
				Speed = CConfigIni.SongPlaybackSpeedToString(entry.SongSpeed),
				RandomMod = entry.RandomMod,
				AvgHitError = entry.AvgHitError,
				AvgSync = entry.AvgSync,
				AvgLeftHandError = entry.AvgLeftHandError,
				AvgRightHandError = entry.AvgRightHandError,
				LeftHandOkays = entry.LeftHandOkays,
				RightHandOkays = entry.RightHandOkays,
			};
		}

		public static SongTableRow FromSongNode(CSongListNode song, int difficulty)
		{
			CScore score = song.score[difficulty];
			CScore.ST譜面情報 info = score?.譜面情報 ?? default;

			return new SongTableRow
			{
				Title = song.ldTitle.GetString(""),
				TimeSince = "",
				ScoreRank = GetScoreRank(info, difficulty),
				ChartLevel = song.nLevel[difficulty],
				BaseBpm = info.BaseBpm,
				MinBpm = info.MinBpm,
				MaxBpm = info.MaxBpm,
				Score = info.nハイスコア != null && difficulty < info.nハイスコア.Length ? info.nハイスコア[difficulty] : 0,
				Goods = 0,
				Okays = 0,
				Bads = 0,
				Rolls = 0,
				MaxCombo = 0,
				Duration = info.Duration > 0 ? FormatDuration(info.Duration) : "",
				Difficulty = GetDifficultyLabel(difficulty),
				DifficultyIndex = difficulty,
				Speed = "",
				RandomMod = "",
			};
		}

		public static void MergeHistoryEntry(ref SongTableRow row, SongEntry entry)
		{
			row.TimeSince = StringHelpers.GetTimeSinceString(entry.Timestamp);
			row.ScoreRank = entry.ScoreRank;
			row.Score = entry.Score;
			row.Goods = entry.Goods;
			row.Okays = entry.Okays;
			row.Bads = entry.Bads;
			row.Rolls = entry.Rolls;
			row.MaxCombo = entry.MaxCombo;
			row.Duration = FormatDuration(entry.DurationMs);
			row.Speed = CConfigIni.SongPlaybackSpeedToString(entry.SongSpeed);
			row.RandomMod = entry.RandomMod;
			row.AvgHitError = entry.AvgHitError;
			row.AvgSync = entry.AvgSync;
			row.AvgLeftHandError = entry.AvgLeftHandError;
			row.AvgRightHandError = entry.AvgRightHandError;
			row.LeftHandOkays = entry.LeftHandOkays;
			row.RightHandOkays = entry.RightHandOkays;
		}

		public static int GetScoreRank(CSongListNode song, int difficulty)
		{
			CScore score = song.score[difficulty];
			if (score == null) return 0;
			return GetScoreRank(score.譜面情報, difficulty);
		}

		private static int GetScoreRank(CScore.ST譜面情報 info, int difficulty)
		{
			int[] ranks = info.nスコアランク;
			if (ranks != null && difficulty < ranks.Length)
			{
				return ranks[difficulty];
			}
			return 0;
		}

		public static string GetDifficultyLabel(int difficulty)
		{
			return difficulty switch
			{
				(int)Difficulty.Easy => "Easy",
				(int)Difficulty.Normal => "Normal",
				(int)Difficulty.Hard => "Hard",
				(int)Difficulty.Oni => "Oni",
				(int)Difficulty.Edit => "Ura",
				(int)Difficulty.Tower => "Tower",
				(int)Difficulty.Dan => "Dan",
				_ => difficulty.ToString()
			};
		}

		public static string FormatDuration(int totalMs)
		{
			if (totalMs <= 0)
			{
				return "0:00";
			}

			TimeSpan duration = TimeSpan.FromMilliseconds(totalMs);
			if (duration.TotalHours >= 1)
			{
				return $"{(int)duration.TotalHours}:{duration.Minutes:D2}:{duration.Seconds:D2}";
			}

			return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
		}

		public static void PlaySong(CSongListNode song, int difficulty)
		{
			if (OpenTaiko.stageSongSelect == null)
			{
				return;
			}

			CActSelect曲リスト songList = OpenTaiko.stageSongSelect.actSongList;
			songList.rCurrentlySelectedSong = song;

			OpenTaiko.stageSongSelect.t曲を選択する(difficulty, 0);
		}

		public static CSongListNode? FindSongByTitle(string title)
		{
			if (OpenTaiko.stageSongSelect?.actSongList == null || OpenTaiko.Songs管理?.list曲ルート == null)
			{
				return null;
			}

			List<CSongListNode> allNodes = OpenTaiko.stageSongSelect.actSongList.flattenList(OpenTaiko.Songs管理.list曲ルート);
			foreach (CSongListNode node in allNodes)
			{
				if ((node.nodeType == CSongListNode.ENodeType.SCORE || node.nodeType == CSongListNode.ENodeType.SCORE_MIDI)
					&& string.Equals(node.ldTitle.GetString(""), title, StringComparison.OrdinalIgnoreCase))
				{
					return node;
				}
			}
			return null;
		}

		public static int GetDifficultyFromLabel(string label)
		{
			return label?.ToLowerInvariant() switch
			{
				"easy" => (int)Difficulty.Easy,
				"normal" => (int)Difficulty.Normal,
				"hard" => (int)Difficulty.Hard,
				"oni" => (int)Difficulty.Oni,
				"ura" => (int)Difficulty.Edit,
				"tower" => (int)Difficulty.Tower,
				"dan" => (int)Difficulty.Dan,
				_ => (int)Difficulty.Oni,
			};
		}
	}
}
