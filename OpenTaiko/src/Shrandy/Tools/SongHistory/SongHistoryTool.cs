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
		private int m_DailyTargetSongs = 50;
		private int m_FilterDays = 0;
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
			DrawGoals();
			
			ImGui.SeparatorText("History");
			DrawFilterDaysInput();
			DrawSummary();
			DrawTable();
		}
		
		private void DrawFilterDaysInput()
		{
			if (ImGui.Button("Today"))
			{
				m_FilterDays = 1;
			}
			ImGui.SameLine();
			if (ImGui.Button("7 Days"))
			{
				m_FilterDays = 7;
			}
			ImGui.SameLine();
			if (ImGui.Button("30 Days"))
			{
				m_FilterDays = 30;
			}
			ImGui.SameLine();
			if (ImGui.Button("All Time"))
			{
				m_FilterDays = 0;
			}
		}

		private void DrawSessionElapsedTime()
		{
			ImGui.SeparatorText("Session Stats");
			
			TimeSpan elapsed = DateTime.Now - m_SessionStartTime;
			int sessionSongCount = GetSessionSongCount();
			int sessionDurationMs = sessionSongCount > 0
				? m_SaveData.SongEntries.Skip(m_SongCountAtStartOfSession).Sum(x => x.DurationMs)
				: 0;
			float uptime = sessionDurationMs / (float)elapsed.TotalMilliseconds;
			float songsPerHour = sessionSongCount / (float)elapsed.TotalHours;

			ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
			if (ImGui.BeginTable("Session Stats", 5, flags))
			{
				ImGui.TableSetupColumn("Time Since Start", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableSetupColumn("Session Playtime", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableSetupColumn("Uptime", ImGuiTableColumnFlags.WidthFixed, 100);
				ImGui.TableSetupColumn("Songs Per Hour", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableSetupColumn("Reset", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableHeadersRow();

				ImGui.TableNextRow();
				
				ImGui.TableSetColumnIndex(0);
				ImGui.TextUnformatted($"{elapsed:hh\\:mm\\:ss}");
				
				ImGui.TableSetColumnIndex(1);
				ImGui.TextUnformatted(FormatDuration(sessionDurationMs));
				
				ImGui.TableSetColumnIndex(2);
				ImGui.Text($"{(int)(uptime * 100)}%%");
				
				ImGui.TableSetColumnIndex(3);
				ImGui.Text($"{(int)songsPerHour}");

				ImGui.TableSetColumnIndex(4);
				if (ImGui.Button("Reset Session Stats"))
				{
					ResetSessionStats();
				}

				ImGui.EndTable();
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
		
		private void DrawGoals()
		{
			ImGui.SeparatorText("Goals");
			DrawGoalInput(ref m_SessionTargetSongs, "Session");
			ImGui.SameLine();
			DrawGoalInput(ref m_DailyTargetSongs, "Daily");
			DrawProgressGoal(m_SessionTargetSongs, "Session", GetSessionSongCount());
			DrawProgressGoal(m_DailyTargetSongs, "Daily", GetSongCountFromCutoff(1));
			DrawProgressGoal(m_DailyTargetSongs * 7, "Weekly", GetSongCountFromCutoff(7));
			DrawProgressGoal(m_DailyTargetSongs * 30, "Monthly", GetSongCountFromCutoff(30));
		}

		private void DrawGoalInput(ref int goal, string label)
		{
			ImGui.SetNextItemWidth(100);
			ImGui.InputInt($"{label}TargetSongs", ref goal, 1, 5);
			
			goal = Math.Max(0, goal);
		}

		private void DrawProgressGoal(int goal, string label, int current)
		{
			float progress = goal > 0
				? MathF.Min(1.0f, current / (float)goal)
				: 0.0f;

			string overlay = goal > 0
				? $"{current} / {goal}"
				: $"{current} / -";

			ImGui.ProgressBar(progress, new System.Numerics.Vector2(-1.0f, 0.0f), $"{label} Goal: {overlay}");
		}

		private void DrawSummary()
		{
			int startIndex = CalculateHistoryStartIndex();
			
			int totalSongs = m_SaveData.SongEntries.Count - startIndex;
			int totalDurationMs = m_SaveData.SongEntries.Skip(startIndex).Sum(x => x.DurationMs);
			
			ImGui.Text($"Songs Played: {totalSongs}");
			ImGui.SameLine();
			ImGui.Text($"Playtime: {FormatDuration(totalDurationMs)}");

			ImGui.SameLine();
			if (m_FilterDays > 0)
			{
				ImGui.Text($"(Showing last {m_FilterDays} days)");
			}
			else
			{
				ImGui.Text("(Showing all time)");
			}
		}

		private void DrawTable()
		{
			if (m_SaveData.SongEntries.Count == 0)
			{
				ImGui.Text("No song history data yet.");
				return;
			}

			if (Utilities.SongTable.BeginTable("Song History"))
			{
				int startIndex = CalculateHistoryStartIndex();
				for (int i = startIndex; i < m_SaveData.SongEntries.Count; ++i)
				{
					var entry = m_SaveData.SongEntries[i];
					var row = Utilities.SongTable.FromSongEntry(entry);

					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);
					ImGui.PushID(i);
					if (ImGui.Selectable(row.Title))
					{
						CSongListNode? song = Utilities.SongTable.FindSongByTitle(entry.SongTitle);
						if (song != null)
						{
							int diff = Utilities.SongTable.GetDifficultyFromLabel(entry.Difficulty);
							Utilities.SongTable.PlaySong(song, diff);
						}
					}
					ImGui.PopID();

					Utilities.SongTable.DrawRowFromColumn1(in row);
				}

				Utilities.SongTable.EndTable();
			}
		}

		private int GetSongCountFromCutoff(int days)
		{
			if (days == 0)
			{
				return m_SaveData.SongEntries.Count;
			}

			// Subtracting an extra day to account for the 5 AM day boundary
			DateTime cutoff = DateTime.Today.AddHours(5).Subtract(TimeSpan.FromDays(days - 1));
			return m_SaveData.SongEntries.Count(x => x.Timestamp >= cutoff);
		}
		
		private int CalculateHistoryStartIndex()
		{
			int startIndex = 0;
			if (m_FilterDays != 0)
			{
				startIndex = Math.Max(0, m_SaveData.SongEntries.Count - GetSongCountFromCutoff(m_FilterDays));
			}
			
			return startIndex;
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
			return Utilities.SongTable.FormatDuration(totalMs);
		}
	}
}
