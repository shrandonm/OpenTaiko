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
		private int m_SessionTargetSongs = 25;
		private DateTime m_SessionStartTime = DateTime.Now;

		public SongHistoryTool(string toolName, SlimDXKeys.Key enableHotkey)
			: base(toolName, enableHotkey)
		{
			m_SaveData = Utilities.SaveHelper.LoadOrCreate<SongHistorySaveData>(m_SaveFileName);
			m_SongCountAtStartOfSession = m_SaveData.SongEntries.Count;

#if DEBUG
			AddDummyData();
#endif
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

		public override void OnResultsActivate(CStage結果 resultsScreen)
		{
			base.OnResultsActivate(resultsScreen);
			TryAddCurrentSongStats();
			Utilities.SaveHelper.Save(m_SaveFileName, m_SaveData);
		}

		protected override void Draw()
		{
			base.Draw();

			DrawSessionElapsedTime();
			DrawSessionTargetProgress();

			ImGui.Text($"Song Duration: {FormatDuration(Utilities.ScoreHelper.GetSongDurationMs())}s");
			DrawSummary();
			DrawTable();
		}

		private void DrawSessionElapsedTime()
		{
			TimeSpan elapsed = DateTime.Now - m_SessionStartTime;
			ImGui.Text($"Time Since Session Start: {elapsed:hh\\:mm\\:ss}");
			ImGui.SameLine();
			if (ImGui.Button("Reset Session Stats"))
			{
				ResetSessionStats();
			}
		}

		private void ResetSessionStats()
		{
			m_SongCountAtStartOfSession = m_SaveData.SongEntries.Count;
			m_SessionStartTime = DateTime.Now;
		}

		private int GetSessionSongCount()
		{
			return Math.Max(0, m_SaveData.SongEntries.Count - m_SongCountAtStartOfSession);
		}

		private void DrawSessionTargetProgress()
		{
			int sessionSongCount = GetSessionSongCount();

			ImGui.Text("Session Target (Songs)");
			ImGui.SetNextItemWidth(100.0f);
			ImGui.InputInt("##SessionTargetSongs", ref m_SessionTargetSongs, 1, 5);
			m_SessionTargetSongs = Math.Max(0, m_SessionTargetSongs);

			float progress = m_SessionTargetSongs > 0
				? MathF.Min(1.0f, sessionSongCount / (float)m_SessionTargetSongs)
				: 0.0f;

			string overlay = m_SessionTargetSongs > 0
				? $"{sessionSongCount} / {m_SessionTargetSongs}"
				: $"{sessionSongCount} / -";

			ImGui.ProgressBar(progress, new System.Numerics.Vector2(-1.0f, 0.0f), overlay);
			ImGui.Separator();
		}

		private void DrawSummary()
		{
			int totalSongs = m_SaveData.SongEntries.Count;
			int totalDurationMs = m_SaveData.SongEntries.Sum(x => x.DurationMs);
			int sessionSongCount = GetSessionSongCount();
			int sessionDurationMs = sessionSongCount > 0
				? m_SaveData.SongEntries.Skip(m_SongCountAtStartOfSession).Sum(x => x.DurationMs)
				: 0;
	
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

			const int columnCount = 16;
			ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollX;
			if (ImGui.BeginTable("Song History", columnCount, flags))
			{
				// Make columns auto-resize to fit content, with Song column stretching
				ImGui.TableSetupColumn("Song", ImGuiTableColumnFlags.WidthFixed, 128);
				ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 64);
				ImGui.TableSetupColumn("Badge", ImGuiTableColumnFlags.WidthFixed, 16);
				ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 48);
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
					Utilities.ScoreHelper.DrawScoreRank(entry.ScoreRank, 16.0f);

					ImGui.TableSetColumnIndex(3);
					ImGui.TextUnformatted(entry.ChartLevel.ToString());

					ImGui.TableSetColumnIndex(4);
					ImGui.Text($"{entry.Score}");

					ImGui.TableSetColumnIndex(5);
					ImGui.Text($"{StringHelpers.GetPercentString(entry.Goods, totalNotes)}%");

					ImGui.TableSetColumnIndex(6);
					ImGui.Text($"{entry.Goods}");

					ImGui.TableSetColumnIndex(7);
					ImGui.Text($"{entry.Okays}");

					ImGui.TableSetColumnIndex(8);
					ImGui.Text($"{entry.Bads}");

					ImGui.TableSetColumnIndex(9);
					ImGui.Text($"{entry.Rolls}");

					ImGui.TableSetColumnIndex(10);
					ImGui.Text($"{entry.MaxCombo}");

					ImGui.TableSetColumnIndex(11);
					ImGui.TextUnformatted(FormatDuration(entry.DurationMs));

					ImGui.TableSetColumnIndex(12);
					ImGui.Text($"{totalNotes}");

					ImGui.TableSetColumnIndex(13);
					ImGui.TextUnformatted(entry.Difficulty);

					ImGui.TableSetColumnIndex(14);
					ImGui.TextUnformatted(CConfigIni.SongPlaybackSpeedToString(entry.SongSpeed));

					ImGui.TableSetColumnIndex(15);
					ImGui.TextUnformatted(entry.RandomMod);
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
			int chartLevel = difficulty >= 0 && difficulty < song.nLevel.Length ? song.nLevel[difficulty] : 0;
			int actualPlayer = OpenTaiko.GetActualPlayer(player);
			int currentScore = (int)OpenTaiko.stageGameScreen.actScore.Get(player);
			int scoreRank = Utilities.ScoreHelper.GetScoreRank(player, currentScore);

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
				DurationMs = Utilities.ScoreHelper.GetSongDurationMs(),
				ScoreRank = scoreRank,
				ChartLevel = chartLevel,
				SongSpeed = OpenTaiko.ConfigIni.nSongSpeed,
				RandomMod = GetRandomModLabel(OpenTaiko.ConfigIni.eRandom[actualPlayer])
			};

			m_SaveData.SongEntries.Add(entry);
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
