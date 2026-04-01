using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class TrainingToolUI
	{
		private TrainingTool m_Tool;

		private const string BookmarkPopupName = "Edit Bookmark";
		private string m_BookmarkNameInput = "";
		private int m_StartMeasureInput;
		private int m_EndMeasureInput;

		public TrainingToolUI(TrainingTool tool)
		{
			m_Tool = tool;
		}

		public void Draw()
		{
			if (OpenTaiko.stageGameScreen.actTokkun != null && OpenTaiko.ConfigIni.bTokkunMode)
			{
				DrawSpeedControls();
				ImGui.Checkbox("Constant Scroll Speed", ref OpenTaiko.ConfigIni.bTokkunConstantScrollSpeed);

				int modeInt = (int)m_Tool.CurrentMode;
				if (ImGui.Combo("Mode", ref modeInt, Enum.GetNames(typeof(TrainingTool.Mode)), Enum.GetValues(typeof(TrainingTool.Mode)).Length))
				{
					m_Tool.SetMode((TrainingTool.Mode)modeInt);
				}

				switch (m_Tool.CurrentMode)
				{
					case TrainingTool.Mode.AutoRewind:
						DrawAutoRewind();
						break;
					case TrainingTool.Mode.Bookmark:
						DrawBookmarks();
						DrawNewBookmarkPopup();
						break;
					default:
						break;
				}
			}
		}

		public void DrawProfilingStats()
		{
			ImGui.SameLine();
			ImGui.Text($"|  Last save time: {m_Tool.SaveStopwatch.ElapsedMicroseconds / 1000.0}ms");
		}

		private void DrawSpeedControls()
		{
			ImGui.Text($"Current BPM: {(int)Math.Round(OpenTaiko.GetTJA(0)?.BPM * (m_Tool.SongSpeed * 0.01f) ?? 0)}");
			int newSongSpeed = m_Tool.SongSpeed;
			ImGui.InputInt("Song Speed % (50-400)", ref newSongSpeed, 1, 10);
			newSongSpeed = Math.Clamp(newSongSpeed, 50, 400);

			if (newSongSpeed != m_Tool.SongSpeed)
			{
				m_Tool.SetSongSpeed(newSongSpeed);
			}

			float scrollSpeed = Utilities.SpeedConversions.GetActualScrollSpeed(OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile]);
			ImGui.InputFloat("Scroll Speed", ref scrollSpeed, 0.1f);
			OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] = Utilities.SpeedConversions.GetScrollSpeedIntValue(scrollSpeed);
		}

		private void DrawAutoRewind()
		{
			ImGui.SeparatorText("Auto Rewind Mode");
			int threshold = m_Tool.AutoRewindErrorThreshold;
			ImGui.InputInt("Auto rewind error (ms)", ref threshold);
			m_Tool.AutoRewindErrorThreshold = threshold;
			ImGui.Text($"Last successful measure: {m_Tool.LastSuccessfulMeasure}");
			if (ImGui.Button("Restart"))
			{
				m_Tool.SetMode(TrainingTool.Mode.AutoRewind);
			}

			if (m_Tool.MeasureFailCounts.Count > 0)
			{
				float[] values = m_Tool.MeasureFailCounts.Select(value => (float)value).ToArray();
				float maxValue = values.Max();
				float scaleMax = Math.Max(1.0f, maxValue);
				ImGui.Text("Measure failure count");
				ImGui.PlotHistogram("", ref values[0], values.Length, 0, null, 0.0f, scaleMax, new Vector2(0, 80));
			}
			else
			{
				ImGui.Text("Fail counts per measure: (no data)");
			}
		}

		private void DrawBookmarks()
		{
			ImGui.SeparatorText("Bookmark Mode");
			DrawBookmarkTable(m_Tool.SongSpeed);
			DrawCreateNewBookmarkButton();
		}

		private void DrawCreateNewBookmarkButton()
		{
			if (ImGui.Button("Create new bookmark"))
			{
				m_BookmarkNameInput = "";
				int currentMeasure = OpenTaiko.stageGameScreen?.actTokkun?.nCurrentMeasure ?? 0;
				m_StartMeasureInput = currentMeasure;
				m_EndMeasureInput = currentMeasure;
				ImGui.OpenPopup(BookmarkPopupName);
			}
		}

		private void DrawNewBookmarkPopup()
		{
			bool createPopupOpen = true;
			if (ImGui.BeginPopupModal(BookmarkPopupName, ref createPopupOpen, ImGuiWindowFlags.AlwaysAutoResize))
			{
				bool existingBookmark = m_Tool.TrainingSaveData.Bookmarks.Exists(x => x.Name == m_BookmarkNameInput);

				if (ImGui.IsWindowAppearing())
				{
					ImGui.SetKeyboardFocusHere();
				}
				ImGui.InputText("Bookmark Name", ref m_BookmarkNameInput, 256);
				ImGui.InputInt("Start Measure", ref m_StartMeasureInput);
				ImGui.InputInt("End Measure", ref m_EndMeasureInput);

				bool canCreate = !string.IsNullOrWhiteSpace(m_BookmarkNameInput);
				if (!canCreate)
				{
					ImGui.BeginDisabled();
				}

				if (ImGui.Button("Create/Update") || ImGui.IsKeyPressed(ImGuiKey.Enter))
				{
					int startMeasure = Math.Max(0, m_StartMeasureInput);
					int endMeasure = Math.Max(startMeasure, m_EndMeasureInput);
					m_Tool.CreateBookmark(m_BookmarkNameInput.Trim(), startMeasure, endMeasure);
					ImGui.CloseCurrentPopup();
				}

				if (!canCreate)
				{
					ImGui.EndDisabled();
				}

				if (!existingBookmark)
				{
					ImGui.BeginDisabled();
				}
				Bookmark bookmark = m_Tool.TrainingSaveData.Bookmarks.Find(x => x.Name == m_BookmarkNameInput);
				ImGui.SameLine();
				if (ImGui.Button("Delete"))
				{
					m_Tool.DeleteBookmark(bookmark);
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Reset stats"))
				{
					m_Tool.ResetStats(bookmark, m_Tool.SongSpeed);
					ImGui.CloseCurrentPopup();
				}
				if (!existingBookmark)
				{
					ImGui.EndDisabled();
				}

				ImGui.SameLine();
				if (ImGui.Button("Cancel"))
				{
					ImGui.CloseCurrentPopup();
				}

				if (!createPopupOpen)
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
		}

		private Vector4 GetPerformanceColour(float percent, float min)
		{
			if (percent == 0.0f)
			{
				return new(0.75f);
			}
			float mid = (1.0f + min) / 2.0f;

			percent = Math.Clamp(percent, min, 1.0f);
			if (percent < mid)
			{
				float t = (percent - min) / (mid - min);
				float r = 1.0f;
				float g = t;
				float b = 0.0f;
				return new Vector4(r, g, b, 1.0f);
			}
			else
			{
				float t = (percent - mid) / (1.0f - mid);
				float r = 1.0f - t;
				float g = 1.0f;
				float b = 0.0f;
				return new Vector4(r, g, b, 1.0f);
			}
		}

		private void DrawColumnStat(int columnIndex, int count, int total, float min, bool showCount)
		{
			float percent = StringHelpers.GetPercent(count, total);
			using (var scopedColour = new ImGuiHelpers.ScopedStyleColor(ImGuiCol.Text, GetPerformanceColour(percent, min)))
			{
				ImGui.TableSetColumnIndex(columnIndex);
				ImGui.Text($"{count} ({StringHelpers.GetPercentString(percent)}%)");
			}
		}

		private void DrawBookmarkTable(int speed)
		{
			bool requestingBookmarkPopup = false;
			if (ImGui.BeginTable("StatsTable", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
			{
				ImGui.TableSetupColumn($"Bookmark", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn($"Measures", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("Play Count", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("DFC Count", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("FC Count", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("Good%", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableHeadersRow();

				for (int i = m_Tool.TrainingSaveData.Bookmarks.Count - 1; i >= 0; --i)
				{
					ImGui.PushID(i);
					Bookmark bookmark = m_Tool.TrainingSaveData.Bookmarks[i];
					AggregateNoteStats stats = m_Tool.TrainingSaveData.GetAggregateStats(new BookmarkKey(bookmark.Name, speed));
					bool isActiveBookmark = m_Tool.ActiveBookmarkInstance != null && m_Tool.ActiveBookmarkInstance.Bookmark == bookmark;

					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);

					if (isActiveBookmark)
					{
						ImGui.PushStyleColor(ImGuiCol.Text, Utilities.ColourHelper.GetKaImGuiColour());
					}

					if (ImGui.Button(bookmark.Name))
					{
						m_Tool.SelectBookmark(bookmark);
					}

					if (isActiveBookmark)
					{
						ImGui.PopStyleColor();
					}

					ImGui.TableSetColumnIndex(1);
					ImGui.Text($"{bookmark.StartMeasure} - {bookmark.EndMeasure}");
					ImGui.SameLine();
					if (ImGui.Button("Edit"))
					{
						m_BookmarkNameInput = bookmark.Name;
						m_StartMeasureInput = bookmark.StartMeasure;
						m_EndMeasureInput = bookmark.EndMeasure;
						requestingBookmarkPopup = true;
					}

					ImGui.TableSetColumnIndex(2);
					ImGui.Text($"{stats.TotalRuns}");

					DrawColumnStat(3, stats.DFCCount, stats.TotalRuns, min: 0.0f, showCount: true);
					DrawColumnStat(4, stats.FCCount, stats.TotalRuns, min: 0.5f, showCount: true);
					DrawColumnStat(5, stats.CombinedNoteStats.GoodCount, stats.CombinedNoteStats.TotalNotes, min: 0.5f, showCount: false);

					ImGui.PopID();
				}

				ImGui.EndTable();
			}

			if (requestingBookmarkPopup)
			{
				ImGui.OpenPopup(BookmarkPopupName);
			}
		}
	}
}
