using System;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SongBrowserUI
	{
		private SongBrowserData m_Data;

		private bool m_FocusFilterInput = false;
		private bool m_ResultsPopupRequested = false;
		public ResultsPopup ResultsPopup { get; private set; }

		public SongBrowserUI(SongBrowserData data)
		{
			m_Data = data;
			ResultsPopup = new ResultsPopup(data);
		}
		
		public void OnEnabled()
		{
			m_FocusFilterInput = true;
		}

		public void Draw()
		{
			if (m_ResultsPopupRequested)
			{
				ResultsPopup.Show();
				m_ResultsPopupRequested = false;
			}
			
			ResultsPopup.Draw();
			DrawSessionStats();
			DrawGoals();

			if (ImGui.BeginTabBar("SongBrowserTabs"))
			{
				if (ImGui.BeginTabItem("All Songs"))
				{
					DrawAllSongsTab();
					ImGui.EndTabItem();
				}

				if (ImGui.BeginTabItem("History"))
				{
					DrawHistoryTab();
					ImGui.EndTabItem();
				}

				ImGui.EndTabBar();
			}
		}

		// --- Session Stats ---

		private void DrawSessionStats()
		{
			ImGui.SeparatorText("Session Stats");

			TimeSpan elapsed = m_Data.GetSessionElapsed();
			int sessionSongCount = m_Data.GetSessionSongCount();
			int sessionDurationMs = m_Data.GetSessionDurationMs();
			float uptime = sessionDurationMs / (float)elapsed.TotalMilliseconds;
			float songsPerHour = sessionSongCount / (float)elapsed.TotalHours;

			ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
			if (ImGui.BeginTable("Session Stats", 6, flags))
			{
				ImGui.TableSetupColumn("Time Since Start", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableSetupColumn("Session Playtime", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableSetupColumn("Session Song Count", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableSetupColumn("Uptime", ImGuiTableColumnFlags.WidthFixed, 100);
				ImGui.TableSetupColumn("Songs Per Hour", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableSetupColumn("Reset", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableHeadersRow();

				ImGui.TableNextRow();

				ImGui.TableSetColumnIndex(0);
				ImGui.TextUnformatted($"{elapsed:hh\\:mm\\:ss}");

				ImGui.TableSetColumnIndex(1);
				ImGui.TextUnformatted(Utilities.SongTable.FormatDuration(sessionDurationMs));

				ImGui.TableSetColumnIndex(2);
				ImGui.TextUnformatted($"{sessionSongCount}");

				ImGui.TableSetColumnIndex(3);
				ImGui.Text($"{(int)(uptime * 100)}%%");

				ImGui.TableSetColumnIndex(4);
				ImGui.Text($"{(int)songsPerHour}");

				ImGui.TableSetColumnIndex(5);
				if (ImGui.Button("Reset Session Stats"))
				{
					m_Data.ResetSessionStats();
				}

				ImGui.EndTable();
			}
		}

		// --- Goals ---

		private void DrawGoals()
		{
			ImGui.SeparatorText("Goals");

			DrawGoalInput(ref m_Data.SessionTargetSongs, "Session");
			ImGui.SameLine();
			DrawGoalInput(ref m_Data.DailyTargetSongs, "Daily");

			float barWidth = ImGui.GetWindowWidth() / 3.0f;
			DrawProgressGoal(m_Data.SessionTargetSongs, "Session", m_Data.GetSessionSongCount(), barWidth);
			
			ImGui.SameLine();
			DrawProgressGoal(m_Data.DailyTargetSongs, "Daily", m_Data.GetDailySongCount(), barWidth);
			
			ImGui.SameLine();
			int monthlyGoal = m_Data.DailyTargetSongs * DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);
			int expectedByToday = m_Data.DailyTargetSongs * DateTime.Now.Day;
			float expectedProgress = monthlyGoal > 0 ? expectedByToday / (float)monthlyGoal : 0.0f;
			DrawProgressGoal(monthlyGoal, "Monthly", m_Data.GetMonthlySongCount(), barWidth, expectedProgress);
		}

		private static void DrawGoalInput(ref int goal, string label)
		{
			ImGui.SetNextItemWidth(100);
			ImGui.InputInt($"{label}TargetSongs", ref goal, 1, 5);

			goal = Math.Max(0, goal);
		}

		private static void DrawProgressGoal(int goal, string label, int current, float barWidth, float expectedProgress = -1.0f)
		{
			float progress = goal > 0
				? MathF.Min(1.0f, current / (float)goal)
				: 0.0f;

			string overlay = goal > 0
				? $"{current} / {goal}"
				: $"{current} / -";

			Vector2 cursorPos = ImGui.GetCursorScreenPos();
			ImGui.ProgressBar(progress, new Vector2(barWidth, 0.0f), $"{label} Goal: {overlay}");

			if (expectedProgress >= 0.0f && goal > 0)
			{
				float barHeight = ImGui.GetFrameHeight();
				float markerX = cursorPos.X + barWidth * MathF.Min(1.0f, expectedProgress);
				var drawList = ImGui.GetWindowDrawList();
				uint markerColor = ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 0.9f));
				drawList.AddLine(
					new Vector2(markerX, cursorPos.Y),
					new Vector2(markerX, cursorPos.Y + barHeight),
					markerColor, 2.0f);
			}
		}

		// --- All Songs Tab ---

		private void DrawAllSongsTab()
		{
			if (OpenTaiko.stageSongSelect == null || OpenTaiko.Songs管理 == null)
			{
				ImGui.Text("Song select not available.");
				return;
			}

			if (m_Data.AllSongs.Count == 0)
			{
				m_Data.RefreshSongList();
			}

			DrawDifficultySelector();
			DrawFilters();
			DrawAllSongsTable();
		}

		private void DrawDifficultySelector()
		{
			ImGui.SeparatorText("Difficulty");

			for (int i = 0; i < SongBrowserData.DifficultyNames.Length; i++)
			{
				if (i > 0)
				{
					ImGui.SameLine();
				}

				bool selected = m_Data.IsDifficultySelected(i);
				if (selected)
				{
					ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
				}

				if (ImGui.Button(SongBrowserData.DifficultyNames[i]))
				{
					m_Data.ToggleDifficulty(i);
				}

				if (selected)
				{
					ImGui.PopStyleColor();
				}
			}
		}

		private void DrawFilters()
		{
			ImGui.SeparatorText("Filter");

			string filterText = m_Data.FilterText;
			ImGui.SetNextItemWidth(-1);
			
			if (m_FocusFilterInput && ImGui.IsWindowFocused())
			{
				ImGui.SetKeyboardFocusHere();
				m_FocusFilterInput = false;
			}
			
			if (ImGui.InputTextWithHint("##filter", "e.g. bpm>100 badge<purple fc<=0 song title words", ref filterText, 512))
			{
				m_Data.FilterText = filterText;
			}

			m_Data.ApplyFiltersIfNeeded();

			ImGui.Text($"{m_Data.FilteredSongs.Count} / {m_Data.AllSongs.Count} songs");
			ImGui.SameLine();
			if (ImGui.Button("Random"))
			{
				var random = m_Data.GetRandomFilteredSong();
				if (random != null)
				{
					Utilities.SongTable.PlaySong(random.Value.song, random.Value.difficulty);
				}
			}
		}

		private void DrawAllSongsTable()
		{
			ImGui.SeparatorText("Songs");

			if (m_Data.FilteredSongs.Count == 0)
			{
				ImGui.Text("No songs match the current filters.");
				return;
			}

			float availableHeight = ImGui.GetContentRegionAvail().Y - 30;
			if (Utilities.SongTable.BeginTable("SongList", ImGuiTableFlags.ScrollY, availableHeight, showAggregates: true))
			{
				for (int i = 0; i < m_Data.FilteredSongs.Count; i++)
				{
					(CSongListNode song, int difficulty) = m_Data.FilteredSongs[i];
					Utilities.SongTableRow row = Utilities.SongTable.FromSongNode(song, difficulty);
					string creator = song.strNotesDesigner?[difficulty] ?? "";

					SongEntry? bestPlay = m_Data.GetBestPlay(row.Title, difficulty);
					if (bestPlay != null)
					{
						Utilities.SongTable.MergeHistoryEntry(ref row, bestPlay);
					}

					SongAggregateStats aggStats = m_Data.GetAggregateStats(row.Title, difficulty);

					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);
					ImGui.PushID(i);
					if (ImGui.Selectable(row.Title))
					{
						Utilities.SongTable.PlaySong(song, difficulty);
					}
					ImGui.PopID();

					Utilities.SongTable.DrawRowFromColumn1(in row, creator);
					Utilities.SongTable.DrawAggregateColumns(aggStats.PlayCount, aggStats.FCCount, aggStats.DFCCount);
				}

				Utilities.SongTable.EndTable();
			}
		}

		// --- History Tab ---

		private void DrawHistoryTab()
		{
			DrawFilterDaysInput();
			DrawHistorySummary();
			DrawHistoryTable();
		}

		private void DrawFilterDaysInput()
		{
			if (ImGui.Button("Today"))
			{
				m_Data.FilterDays = 1;
			}
			ImGui.SameLine();
			if (ImGui.Button("7 Days"))
			{
				m_Data.FilterDays = 7;
			}
			ImGui.SameLine();
			if (ImGui.Button("30 Days"))
			{
				m_Data.FilterDays = 30;
			}
			ImGui.SameLine();
			if (ImGui.Button("All Time"))
			{
				m_Data.FilterDays = 0;
			}
		}

		private void DrawHistorySummary()
		{
			int startIndex = m_Data.CalculateHistoryStartIndex();

			int totalSongs = m_Data.SaveData.SongEntries.Count - startIndex;
			int totalDurationMs = 0;
			for (int i = startIndex; i < m_Data.SaveData.SongEntries.Count; i++)
			{
				totalDurationMs += m_Data.SaveData.SongEntries[i].DurationMs;
			}

			ImGui.Text($"Songs Played: {totalSongs}");
			ImGui.SameLine();
			ImGui.Text($"Playtime: {Utilities.SongTable.FormatDuration(totalDurationMs)}");

			ImGui.SameLine();
			if (m_Data.FilterDays > 0)
			{
				ImGui.Text($"(Showing last {m_Data.FilterDays} days)");
			}
			else
			{
				ImGui.Text("(Showing all time)");
			}
		}

		private void DrawHistoryTable()
		{
			if (m_Data.SaveData.SongEntries.Count == 0)
			{
				ImGui.Text("No song history data yet.");
				return;
			}

			if (Utilities.SongTable.BeginTable("Song History"))
			{
				int startIndex = m_Data.CalculateHistoryStartIndex();
				for (int i = startIndex; i < m_Data.SaveData.SongEntries.Count; ++i)
				{
					SongEntry entry = m_Data.SaveData.SongEntries[i];
					Utilities.SongTableRow row = Utilities.SongTable.FromSongEntry(entry);

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
		
		public void RequestResultsPopup()
		{
			m_ResultsPopupRequested = true;
		}
	}
}
