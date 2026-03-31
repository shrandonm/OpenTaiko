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
		public string Speed;
		public string RandomMod;
		public double BaseBpm;
		public double MinBpm;
		public double MaxBpm;

		public int TotalNotes => Goods + Okays + Bads;
	}

	internal static class SongTable
	{
		private const int ColumnCount = 18;

		public static bool BeginTable(string id, ImGuiTableFlags extraFlags = ImGuiTableFlags.None, float height = 0)
		{
			ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
				| ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollX | extraFlags;

			Vector2 size = height > 0 ? new Vector2(0, height) : Vector2.Zero;

			if (!ImGui.BeginTable(id, ColumnCount, flags, size))
			{
				return false;
			}

			ImGui.TableSetupColumn("Song", ImGuiTableColumnFlags.WidthFixed, 128);
			ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 64);
			ImGui.TableSetupColumn("Badge", ImGuiTableColumnFlags.WidthFixed, 16);
			ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("BPM", ImGuiTableColumnFlags.WidthFixed, 80);
			ImGui.TableSetupColumn("Score", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Good%", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Goods", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Okays", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Bads", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Rolls", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Combo", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 64);
			ImGui.TableSetupColumn("Total Notes", ImGuiTableColumnFlags.WidthFixed, 64);
			ImGui.TableSetupColumn("Diff", ImGuiTableColumnFlags.WidthFixed, 48);
			ImGui.TableSetupColumn("Speed", ImGuiTableColumnFlags.WidthFixed, 56);
			ImGui.TableSetupColumn("Random", ImGuiTableColumnFlags.WidthFixed, 72);
			ImGui.TableSetupColumn("Creator", ImGuiTableColumnFlags.WidthFixed, 100);
			ImGui.TableHeadersRow();

			return true;
		}

		public static void DrawRow(in SongTableRow row, string creator = "")
		{
			int totalNotes = row.TotalNotes;

			ImGui.TableNextRow();

			int col = 0;

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.Title);

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
			ImGui.TextUnformatted(row.Difficulty);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.Speed);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.RandomMod);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(creator);
		}

		public static void EndTable()
		{
			ImGui.EndTable();
		}

		/// <summary>
		/// Draws columns 1+ for a row (caller handles column 0 and TableNextRow).
		/// </summary>
		public static void DrawRowFromColumn1(in SongTableRow row, string creator = "")
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
			ImGui.TextUnformatted(row.Difficulty);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.Speed);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.RandomMod);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(creator);
		}

		private static void DrawBpm(double baseBpm, double minBpm, double maxBpm)
		{
			if (baseBpm <= 0) return;

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
				Speed = CConfigIni.SongPlaybackSpeedToString(entry.SongSpeed),
				RandomMod = entry.RandomMod,
			};
		}

		public static SongTableRow FromSongNode(CSongListNode song, int difficulty)
		{
			CScore score = song.score[difficulty];
			var info = score?.譜面情報 ?? default;

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
				Speed = "",
				RandomMod = "",
			};
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
	}
}
