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

		private SongTagsUI m_TagsUI;
		private TagFilterBar m_TagFilterBar;
		private LevelFilterBar m_LevelFilterBar;
		private GenreFilterBar m_GenreFilterBar;
		private SongBrowserOverview m_OverviewWidget;
		private SongsPerDayGraph m_SongsPerDayGraph;
		public RetryPopup RetryPopup { get; private set; }

		public SongBrowserUI(SongBrowserData data)
		{
			m_Data = data;
			m_TagsUI = new SongTagsUI(data.Tags, data.SaveTags);
			m_TagFilterBar = new TagFilterBar(data.Tags, () => data.FilterText, value => data.FilterText = value);
			m_LevelFilterBar = new LevelFilterBar(() => data.FilterText, value => data.FilterText = value);
			m_GenreFilterBar = new GenreFilterBar(() => data.AllSongs, () => data.FilterText, value => data.FilterText = value);
			RetryPopup = new RetryPopup(data);
			m_OverviewWidget = new SongBrowserOverview(data);
			m_SongsPerDayGraph = new SongsPerDayGraph(data);
		}
		public void OnEnabled()
		{
			m_FocusFilterInput = true;
		}
		
		public void OnDisabled()
		{
		}

		public void Draw()
		{
			if (m_ResultsPopupRequested)
			{
				RetryPopup.Show();
				m_ResultsPopupRequested = false;
			}
			
			RetryPopup.Draw();
			m_OverviewWidget.Draw();

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

			DrawFilters();
			DrawDifficultySelector();
			DrawAllSongsTable();
		}

		private void DrawDifficultySelector()
		{
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

		private void DrawFilters(bool showSongActions = true)
		{
			ImGui.SeparatorText("Filter");

			string filterText = m_Data.FilterText;
			ImGui.SetNextItemWidth(-1);
			
			if (m_FocusFilterInput && ImGui.IsWindowFocused())
			{
				ImGui.SetKeyboardFocusHere();
				m_FocusFilterInput = false;
			}
			
			if (ImGui.InputTextWithHint("##filter", "e.g. bpm>100 level=8 genre=Pop tag=rock song title words", ref filterText, 512))
			{
				m_Data.FilterText = filterText;
			}

			m_TagFilterBar.Draw();
			m_LevelFilterBar.Draw();
			m_GenreFilterBar.Draw();

			if (m_Data.ApplyFiltersIfNeeded())
			{
				m_OverviewWidget.Refresh();
			}

			ImGui.Text($"{m_Data.FilteredSongs.Count} / {m_Data.AllSongs.Count} songs");
			if (!showSongActions)
			{
				return;
			}

			ImGui.SameLine();
			if (ImGui.Button("Random"))
			{
				var random = m_Data.GetRandomFilteredSong();
				if (random != null)
				{
					Utilities.SongHelper.PlaySong(new Chart { Song = random.Value.song, Difficulty = random.Value.difficulty });
				}
			}
			
			ImGui.SameLine();
			if (ImGui.Button("Marathon"))
			{
				OpenTaiko.ShrandyExtension.SetToolEnabled<MarathonTool>(true);
			}

			ImGui.SameLine();
			bool noModOnly = m_Data.NoModOnly;
			if (ImGui.Checkbox("No-mod only", ref noModOnly))
			{
				m_Data.NoModOnly = noModOnly;
			}

		}

		private void DrawAllSongsTable(Func<Chart, bool>? chartFilter = null, Action<Chart>? onChartSelected = null, string tableId = "SongList")
		{
			ImGui.SeparatorText("Songs");

			List<int>? visibleSongIndices = null;
			int visibleSongCount = m_Data.FilteredSongs.Count;
			if (chartFilter != null)
			{
				visibleSongIndices = new List<int>();
				for (int songIndex = 0; songIndex < m_Data.FilteredSongs.Count; songIndex++)
				{
					(CSongListNode song, int difficulty) = m_Data.FilteredSongs[songIndex];
					if (chartFilter(new Chart(song, difficulty)))
					{
						visibleSongIndices.Add(songIndex);
					}
				}

				visibleSongCount = visibleSongIndices.Count;
			}

			if (visibleSongCount == 0)
			{
				ImGui.Text(chartFilter == null ? "No songs match the current filters." : "No eligible backing tracks match the current filters.");
				return;
			}

			float availableHeight = ImGui.GetContentRegionAvail().Y - 30;
			if (Utilities.SongTable.BeginTable(tableId, ImGuiTableFlags.ScrollY, availableHeight, showAggregates: true,
				onSortChanged: (columnIndex, ascending) =>
				{
					m_Data.SortColumnIndex = columnIndex;
					m_Data.SortAscending = ascending;
				}))
			{
				unsafe
				{
					ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
					clipper.Begin(visibleSongCount);
					while (clipper.Step())
					{
						for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
						{
							int songIndex = visibleSongIndices == null ? i : visibleSongIndices[i];
							(CSongListNode song, int difficulty) = m_Data.FilteredSongs[songIndex];
							Utilities.SongTableRow row = Utilities.SongTable.FromSongNode(song, difficulty);
							string creator = song.strNotesDesigner?[difficulty] ?? "";

							SongEntry? bestPlay = m_Data.GetBestPlay(row.Title, difficulty);
							if (bestPlay != null)
							{
								Utilities.SongTable.MergeHistoryEntry(ref row, bestPlay);
								row.TimeSinceLastPB = StringHelpers.GetTimeSinceString(bestPlay.Timestamp);
							}

							SongEntry? lastPlay = m_Data.GetLastPlay(row.Title, difficulty);
							if (lastPlay != null)
							{
								row.TimeSince = StringHelpers.GetTimeSinceString(lastPlay.Timestamp);
							}

							SongAggregateStats aggStats = m_Data.GetAggregateStats(row.Title, difficulty);

							ImGui.TableNextRow();
							ImGui.TableSetColumnIndex(0);
							ImGui.PushID(songIndex);
							if (ImGui.Selectable(row.Title))
							{
								Chart chart = new Chart(song, difficulty);
								if (onChartSelected == null)
								{
									Utilities.SongHelper.PlaySong(chart);
								}
								else
								{
									onChartSelected(chart);
									ImGui.CloseCurrentPopup();
								}
							}
							ImGui.PopID();

							Utilities.SongTable.DrawRow(in row, creator);

							m_TagsUI.DrawCell(row.Title, difficulty, i);

							Utilities.SongTable.DrawAggregateColumns(aggStats.PlayCount, aggStats.FCCount, aggStats.DFCCount);
						}
					}
					clipper.End();
					clipper.Destroy();
				} // unsafe

				Utilities.SongTable.EndTable();
				m_TagsUI.DrawPopup();
			}
		}

		internal void DrawChartPickerPopup(string popupId, Func<Chart, bool> canSelectChart, Action<Chart?> onChartSelected)
		{
			ImGui.SetNextWindowSize(new Vector2(1000, 700), ImGuiCond.FirstUseEver);
			bool isOpen = true;
			if (!ImGui.BeginPopupModal(popupId, ref isOpen, ImGuiWindowFlags.None))
			{
				return;
			}

			ImGui.Text("Select Backing Track");
			if (ImGui.Button("None"))
			{
				onChartSelected(null);
				ImGui.CloseCurrentPopup();
			}

			ImGui.SameLine();
			if (ImGui.Button("Cancel"))
			{
				ImGui.CloseCurrentPopup();
			}

			if (m_Data.AllSongs.Count == 0)
			{
				m_Data.RefreshSongList();
			}

			DrawFilters(showSongActions: false);
			DrawDifficultySelector();
			DrawAllSongsTable(canSelectChart, chart => onChartSelected(chart), "BackingTrackSongList");
			ImGui.EndPopup();
		}

		// --- History Tab ---

		private void DrawHistoryTab()
		{
			DrawFilterDaysInput();
			m_SongsPerDayGraph.Draw();
			DrawHistoryFilterInput();
			DrawHistorySummary();
			DrawHistoryTable();
		}

		private void DrawHistoryFilterInput()
		{
			string filterText = m_Data.HistoryFilterText;
			ImGui.SetNextItemWidth(-1);
			if (ImGui.InputTextWithHint("##historyfilter", "Filter by song title...", ref filterText, 512))
			{
				m_Data.HistoryFilterText = filterText;
			}
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
				string filterLower = m_Data.HistoryFilterText.ToLowerInvariant();

				// Pre-filter so the clipper has a fixed item count to work with.
				var filteredEntries = new System.Collections.Generic.List<(SongEntry entry, int originalIndex)>();
				for (int i = startIndex; i < m_Data.SaveData.SongEntries.Count; ++i)
				{
					SongEntry entry = m_Data.SaveData.SongEntries[i];
					if (!string.IsNullOrEmpty(filterLower) && !entry.SongTitle.ToLowerInvariant().Contains(filterLower))
						continue;
					filteredEntries.Add((entry, i));
				}

				unsafe
				{
					ImGuiListClipperPtr clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
					clipper.Begin(filteredEntries.Count);
					while (clipper.Step())
					{
						for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
						{
							(SongEntry entry, int originalIndex) = filteredEntries[i];
							Utilities.SongTableRow row = Utilities.SongTable.FromSongEntry(entry);

							ImGui.TableNextRow();
							ImGui.TableSetColumnIndex(0);
							ImGui.PushID(originalIndex);
							if (ImGui.Selectable(row.Title))
							{
								CSongListNode? song = Utilities.SongTable.FindSongByTitle(entry.SongTitle);
								if (song != null)
								{
									int diff = Utilities.SongTable.GetDifficultyFromLabel(entry.Difficulty);
									Utilities.SongHelper.PlaySong(new Chart { Song = song, Difficulty = diff });
								}
							}
							ImGui.PopID();

							Utilities.SongTable.DrawRow(in row);
						}
					}
					clipper.End();
					clipper.Destroy();
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
