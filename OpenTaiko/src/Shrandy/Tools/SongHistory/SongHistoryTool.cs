using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SongHistoryTool : Tool
	{
		private const string m_SaveFileName = "song_history.json";
		private SongHistorySaveData m_SaveData = new();
		private int m_SongCountAtStartOfSession = 0;

		public SongHistoryTool(string toolName, SlimDXKeys.Key enableHotkey)
			: base(toolName, enableHotkey)
		{
			m_SaveData = Utilities.SaveHelper.LoadOrCreate<SongHistorySaveData>(m_SaveFileName);
			m_SongCountAtStartOfSession = m_SaveData.SongEntries.Count;

			AddDummyData();
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
					DurationMs = 120000
				});
			}
		}

		public override void OnResultsActivate(CStage結果 resultsScreen)
		{
			base.OnResultsActivate(resultsScreen);
			TryAddCurrentSongStats();
			Utilities.SaveHelper.Save(m_SaveFileName, m_SaveData);
		}

		protected override void Draw()
		{
			base.Draw();

			DrawSummary();
			DrawTable();
		}

		private void DrawSummary()
		{
			int totalSongs = m_SaveData.SongEntries.Count;
			int totalDurationMs = m_SaveData.SongEntries.Sum(x => x.DurationMs);
			int sessionSongCount = 0;
			int sessionDurationMs = 0;
			
			if (totalSongs > m_SongCountAtStartOfSession)
			{
				sessionSongCount = totalSongs - m_SongCountAtStartOfSession;
				sessionDurationMs = m_SaveData.SongEntries.Skip(m_SongCountAtStartOfSession).Sum(x => x.DurationMs);
			}
	
			ImGui.Text($"Total Songs Played: {totalSongs}");
			ImGui.SameLine();
			ImGui.Text($"Total Playtime: {FormatDuration(totalDurationMs)}");
			ImGui.Separator();

			ImGui.Text($"Session Songs Played: {sessionSongCount}");
			ImGui.SameLine();
			ImGui.Text($"Session Playtime: {FormatDuration(sessionDurationMs)}");
			ImGui.Separator();
		}

		private void DrawTable()
		{
			if (m_SaveData.SongEntries.Count == 0)
			{
				ImGui.Text("No song history data yet.");
				return;
			}

			const int columnCount = 12;
			ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollX;
			if (ImGui.BeginTable("Song History", columnCount, flags))
			{
				// Make columns auto-resize to fit content, with Song column stretching
				ImGui.TableSetupColumn("Song", ImGuiTableColumnFlags.WidthFixed, 128);
				ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 64);
				ImGui.TableSetupColumn("Diff", ImGuiTableColumnFlags.WidthFixed, 48);
				ImGui.TableSetupColumn("Score", ImGuiTableColumnFlags.WidthFixed, 48);
				ImGui.TableSetupColumn("Good%", ImGuiTableColumnFlags.WidthFixed, 48);
				ImGui.TableSetupColumn("Goods", ImGuiTableColumnFlags.WidthFixed, 48);
				ImGui.TableSetupColumn("Okays", ImGuiTableColumnFlags.WidthFixed, 48);
				ImGui.TableSetupColumn("Bads", ImGuiTableColumnFlags.WidthFixed, 48);
				ImGui.TableSetupColumn("Rolls", ImGuiTableColumnFlags.WidthFixed, 48);
				ImGui.TableSetupColumn("Combo", ImGuiTableColumnFlags.WidthFixed, 48);
				ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 64);
				ImGui.TableSetupColumn("Total Notes", ImGuiTableColumnFlags.WidthFixed, 64);
				ImGui.TableHeadersRow();

				foreach (SongEntry entry in m_SaveData.SongEntries)
				{
					int totalNotes = entry.Goods + entry.Okays + entry.Bads;

					ImGui.TableNextRow();

					ImGui.TableSetColumnIndex(0);
					ImGui.TextUnformatted($"{entry.SongTitle}");

					ImGui.TableSetColumnIndex(1);
					ImGui.TextUnformatted(StringHelpers.GetTimeSinceString(entry.Timestamp));

					ImGui.TableSetColumnIndex(2);
					ImGui.TextUnformatted(entry.Difficulty);

					ImGui.TableSetColumnIndex(3);
					ImGui.Text($"{entry.Score}");

					ImGui.TableSetColumnIndex(4);
					ImGui.Text($"{StringHelpers.GetPercentString(entry.Goods, totalNotes)}%");

					ImGui.TableSetColumnIndex(5);
					ImGui.Text($"{entry.Goods}");

					ImGui.TableSetColumnIndex(6);
					ImGui.Text($"{entry.Okays}");

					ImGui.TableSetColumnIndex(7);
					ImGui.Text($"{entry.Bads}");

					ImGui.TableSetColumnIndex(8);
					ImGui.Text($"{entry.Rolls}");

					ImGui.TableSetColumnIndex(9);
					ImGui.Text($"{entry.MaxCombo}");

					ImGui.TableSetColumnIndex(10);
					ImGui.TextUnformatted(FormatDuration(entry.DurationMs));

					ImGui.TableSetColumnIndex(11);
					ImGui.Text($"{totalNotes}");
				}

				ImGui.EndTable();
			}
		}

		private void TryAddCurrentSongStats()
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
			var score = OpenTaiko.stageGameScreen.CChartScore[player];
			int difficulty = OpenTaiko.stageSongSelect.nChoosenSongDifficulty[player];

			SongEntry entry = new()
			{
				SongTitle = song.ldTitle.GetString(""),
				Difficulty = GetDifficultyLabel(difficulty),
				Timestamp = DateTime.Now,
				Score = score.nScore,
				Goods = score.nGreat,
				Okays = score.nGood,
				Bads = score.nMiss,
				Rolls = score.nRoll,
				MaxCombo = OpenTaiko.stageGameScreen.actCombo?.nCurrentCombo.最高値[player] ?? 0,
				DurationMs = GetSongDurationMs(),
			};

			m_SaveData.SongEntries.Add(entry);
		}

		private static int GetSongDurationMs()
		{
			CTja? tja = OpenTaiko.TJA;
			if (tja == null || tja.listChip.Count == 0)
			{
				return 0;
			}

			for (int index = tja.listChip.Count - 1; index >= 0; index--)
			{
				CChip chip = tja.listChip[index];
				if (chip.nChannelNo == 0x01)
				{
					int duration = chip.GetDuration();
					int tjaEndMs = chip.n発声時刻ms + duration;
					return (int)Math.Round(CTja.TjaDurationToGameDuration(tjaEndMs));
				}
			}

			return (int)Math.Round(CTja.TjaDurationToGameDuration(tja.listChip[^1].n発声時刻ms));
		}

		private static string GetDifficultyLabel(int difficulty)
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

		private static string FormatDuration(int totalMs)
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
