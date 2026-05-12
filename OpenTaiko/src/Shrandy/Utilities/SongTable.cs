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
		public string TimeSinceLastPB;
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
		public int Judgement;

		public int TotalNotes => Goods + Okays + Bads;
	}

	internal static class SongTable
	{
		private const int BaseColumnCount = 27;
		private const int AggregateColumnCount = 3;
		public const int TagsColumnIndex = 26;

		private struct ColumnDef
		{
			public string Label;
			public bool Hidden;
			public bool UseLargeSize;

			public ColumnDef(string label, bool hidden, bool useLargeSize = false)
			{
				Label = label;
				Hidden = hidden;
				UseLargeSize = useLargeSize;
			}
		}

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

			// (label, hidden by default, use large size)
			ColumnDef[] baseColumns =
			{
				new ColumnDef("Song", 			hidden:false, useLargeSize:true),
				new ColumnDef("Last Played", 	hidden:false),
				new ColumnDef("Last PB", 		hidden:true),
				new ColumnDef("Badge", 			hidden:false),
				new ColumnDef("Level", 			hidden:false),
				new ColumnDef("BPM",			hidden:false),
				new ColumnDef("Score",			hidden:false, useLargeSize:true),
				new ColumnDef("Good%",			hidden:false),
				new ColumnDef("Goods",			hidden:true),
				new ColumnDef("Okays",			hidden:true),
				new ColumnDef("Bads",			hidden:true),
				new ColumnDef("Rolls",			hidden:true),
				new ColumnDef("Combo",			hidden:true),
				new ColumnDef("Duration",		hidden:true),
				new ColumnDef("Total Notes",	hidden:true),
				new ColumnDef("Diff",			hidden:false),
				new ColumnDef("Speed",			hidden:true),
				new ColumnDef("Random",			hidden:true),
				new ColumnDef("Judgement",		hidden:true),
				new ColumnDef("Creator",		hidden:true, useLargeSize:true),
				new ColumnDef("Avg Error",		hidden:false),
				new ColumnDef("Avg Sync",		hidden:true),
				new ColumnDef("L.Avg Error",	hidden:true),
				new ColumnDef("R.Avg Error",	hidden:true),
				new ColumnDef("L.Okays",		hidden:true),
				new ColumnDef("R.Okays",		hidden:true),
				new ColumnDef("Tags",			hidden:false, useLargeSize:true),
			};
			ColumnDef[] aggregateColumns =
			{
				new ColumnDef("Plays", hidden:false),
				new ColumnDef("FC",    hidden:true),
				new ColumnDef("DFC",   hidden:true),
			};

			// Large columns count as 2 units; total units determines the base column width
			int visibleUnits = 0;
			foreach (var col in baseColumns)
			{
				if (!col.Hidden)
				{
					visibleUnits += col.UseLargeSize ? 2 : 1;
				}
			}

			if (showAggregates)
			{
				foreach (var col in aggregateColumns)
				{
					if (!col.Hidden)
					{
						visibleUnits += col.UseLargeSize ? 2 : 1;
					}
				}
			}

			float colWidth = ImGui.GetContentRegionAvail().X / visibleUnits;

			foreach (var col in baseColumns)
			{
				float width = col.UseLargeSize ? colWidth * 2 : colWidth;
				ImGui.TableSetupColumn(col.Label, ImGuiTableColumnFlags.WidthFixed | (col.Hidden ? hide : ImGuiTableColumnFlags.None), width);
			}

			if (showAggregates)
			{
				foreach (var col in aggregateColumns)
				{
					float width = col.UseLargeSize ? colWidth * 2 : colWidth;
					ImGui.TableSetupColumn(col.Label, ImGuiTableColumnFlags.WidthFixed | (col.Hidden ? hide : ImGuiTableColumnFlags.None), width);
				}
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
			ImGui.TextUnformatted(row.TimeSinceLastPB);

			ImGui.TableSetColumnIndex(col++);
			SongHelper.DrawScoreRank(row.ScoreRank, 16.0f);

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
			SongHelper.DrawDifficultyIcon(row.DifficultyIndex, 16.0f);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.Speed);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(row.RandomMod);

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(CLangManager.LangInstance.GetString($"MOD_TIMING{row.Judgement + 1}"));

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
				Judgement = entry.Judgement,
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
				Difficulty = SongHelper.GetDifficultyLabel(difficulty),
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
			row.Judgement = entry.Judgement;
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
