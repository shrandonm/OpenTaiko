using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using SlimDXKeys;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy.Tools
{
	internal class TrainingTool : Tool
	{
		private SaveData m_SaveData = new();
		private MeasureListener m_MeasureListener = new();
		private BookmarkInstance? m_ActiveBookmarkInstance;
		private MicroStopwatch m_SaveStopwatch = new();

		private bool m_SaveRequested = false;
		private bool m_WaitingForBookmarkRestart = false;

		private const string BookmarkPopupName = "Edit Bookmark";
		private string m_BookmarkNameInput = "";
		private int m_StartMeasureInput;
		private int m_EndMeasureInput;

		private int m_SongSpeed = 100;

		public TrainingTool(string toolName, Key enableHotkey) : base(toolName, enableHotkey)
		{
		}

		protected override void Update()
		{
			base.Update();

			if (OpenTaiko.rCurrentStage == OpenTaiko.stageGameScreen && OpenTaiko.ConfigIni.bTokkunMode)
			{
				m_MeasureListener.Update();

				if (m_SaveRequested)
				{
					Save();
				}

				if (m_WaitingForBookmarkRestart && OpenTaiko.Pad.IsPressingDecide())
				{
					RestartBookmark();
					m_WaitingForBookmarkRestart = false;
				}
			}
		}

		private void DrawSpeedControls()
		{
			ImGui.Text($"Current BPM: {(int)Math.Round(OpenTaiko.GetTJA(0)?.BPM * (m_SongSpeed * 0.01f) ?? 0)}");
			int newSongSpeed = m_SongSpeed;
			ImGui.InputInt("Song Speed % (50-400)", ref newSongSpeed, 1, 10);
			newSongSpeed = Math.Clamp(newSongSpeed, 50, 400);

			if (newSongSpeed != m_SongSpeed)
			{
				SetSongSpeed(newSongSpeed);
			}

			float scrollSpeed = Utilities.SpeedConversions.GetActualScrollSpeed(OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile]);
			ImGui.InputFloat("Scroll Speed", ref scrollSpeed, 0.1f);
			OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] = Utilities.SpeedConversions.GetScrollSpeedIntValue(scrollSpeed);
		}

		protected override void Draw()
		{
			base.Draw();

			if (OpenTaiko.stageGameScreen.actTokkun != null && OpenTaiko.ConfigIni.bTokkunMode)
			{
				DrawSpeedControls();
				DrawBookmarks();
				DrawNewBookmarkPopup();
			}
		}

		private void SetSongSpeed(int newSpeed)
		{
			m_SongSpeed = newSpeed;
			OpenTaiko.stageGameScreen.actTokkun.SetSongSpeed(newSpeed);

			if (m_ActiveBookmarkInstance != null)
			{
				m_ActiveBookmarkInstance.Speed = newSpeed;
				RestartBookmark();
			}
			else
			{
				OpenTaiko.stageGameScreen.actTokkun.QueueAutoSkipBack();
			}
		}

		private void Cleanup()
		{
			StopActiveBookmark();
		}

		private void StopActiveBookmark()
		{
			m_ActiveBookmarkInstance = null;
		}

		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);

			m_SaveData = new();

			if (stage == OpenTaiko.stageGameScreen && OpenTaiko.ConfigIni.bTokkunMode)
			{
				SaveData? loadedSaveData = SaveData.Load(OpenTaiko.GetTJA(0)?.strFileName ?? "");
				if (loadedSaveData != null)
				{
					m_SaveData = loadedSaveData;
					OnSaveLoaded(m_SaveData);
				}

				m_MeasureListener.OnMeasureCompleted += OnMeasureCompleted;
				m_MeasureListener.Reset();
			}
			else
			{
				m_MeasureListener.OnMeasureCompleted -= OnMeasureCompleted;
				m_WaitingForBookmarkRestart = false;
			}
		}

		public override void OnNoteHit(HitParams hitParams)
		{
			CActImplTrainingMode trainingMode = OpenTaiko.stageGameScreen.actTokkun;
			if (trainingMode == null || !OpenTaiko.ConfigIni.bTokkunMode)
			{
				return;
			}

			if (hitParams.Chip != null
				&& m_ActiveBookmarkInstance != null
				&& IsNoteWithinBookmarkRange(hitParams, m_ActiveBookmarkInstance.Bookmark))
			{
				m_ActiveBookmarkInstance.NoteStats.OnNoteHit(hitParams);
			}

			if (hitParams.Chip != null && OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold > 0)
			{
				TryAutoSkipBack(hitParams);
			}
		}

		private void TryAutoSkipBack(HitParams hitParams)
		{
			int absDelta = Math.Abs(hitParams.Chip.nLag);
			if (absDelta > OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold || hitParams.JudgeResult == ENoteJudge.Miss)
			{
				OnMistakeMade();
			}
		}

		private bool IsNoteWithinBookmarkRange(HitParams hitParams, Bookmark bookmark)
		{
			int startNoteIndex = OpenTaiko.TJA.GetListChipIndexOfMeasure(bookmark.StartMeasure);
			int lastNoteIndex = OpenTaiko.TJA.GetListChipIndexOfMeasure(bookmark.EndMeasure + 1) - 1;
			int hitIndex = OpenTaiko.TJA.listChip.IndexOf(hitParams.Chip);

			return hitIndex >= startNoteIndex && hitIndex <= lastNoteIndex;
		}

		private void OnMistakeMade()
		{
			if (m_ActiveBookmarkInstance == null)
			{
				OpenTaiko.stageGameScreen.actTokkun.QueueAutoSkipBack();
			}
		}

		private void JumpToStartOfBookmark(Bookmark bookmark)
		{
			OpenTaiko.stageGameScreen.actTokkun.QueueJumpToMeasure(bookmark.StartMeasure);
		}

		private void CreateBookmark(string name, int startMeasure, int endMeasure)
		{
			int existingIndex = m_SaveData.Bookmarks.FindIndex(x => x.Name == name);
			if (existingIndex != -1)
			{
				Bookmark bookmark = m_SaveData.Bookmarks[existingIndex];
				bookmark.StartMeasure = startMeasure;
				bookmark.EndMeasure = endMeasure;
				m_SaveData.Bookmarks[existingIndex] = bookmark;
			}
			else
			{
				m_SaveData.Bookmarks.Add(new Bookmark()
				{
					Name = name,
					StartMeasure = startMeasure,
					EndMeasure = endMeasure,
				});
			}
			m_SaveData.Bookmarks.Sort((a, b) => b.StartMeasure.CompareTo(a.StartMeasure));
			RequestSave();
		}

		private int GetScrollSpeedIndex(int songSpeed)
		{
			return 0;
		}

		private void SelectBookmark(Bookmark bookmark)
		{
			m_ActiveBookmarkInstance = CreateBookmarkInstance(bookmark, m_SongSpeed);
			CActImplTrainingMode trainingMode = OpenTaiko.stageGameScreen.actTokkun;
			if (trainingMode != null)
			{
				JumpToStartOfBookmark(bookmark);
			}
		}

		private BookmarkInstance CreateBookmarkInstance(Bookmark bookmark, int speed)
		{
			return new()
			{
				BookmarkName = bookmark.Name,
				Bookmark = bookmark,
				Speed = speed,
			};
		}

		private void OnMeasureCompleted(int measure)
		{
			if (m_ActiveBookmarkInstance != null && m_ActiveBookmarkInstance.Bookmark.EndMeasure == measure)
			{
				CompleteBookmark(m_ActiveBookmarkInstance);
			}
		}

		private bool DidParticipate(BookmarkInstance bookmarkInstance)
		{
			return bookmarkInstance.NoteStats.BadCount < bookmarkInstance.NoteStats.TotalNotes * 0.75f;
		}

		private void CompleteBookmark(BookmarkInstance bookmarkInstance)
		{
			if (DidParticipate(bookmarkInstance))
			{
				bookmarkInstance.TimestampUtc = DateTime.UtcNow.Ticks;
				m_SaveData.AddToHistory(bookmarkInstance);
				RequestSave();
			}

			OpenTaiko.stageGameScreen.actTokkun.tPausePlay();
			m_WaitingForBookmarkRestart = true;
		}

		private void RestartBookmark()
		{
			if (m_ActiveBookmarkInstance != null)
			{
				BookmarkInstance newInstance = CreateBookmarkInstance(m_ActiveBookmarkInstance.Bookmark, m_SongSpeed);
				m_ActiveBookmarkInstance = newInstance;
				JumpToStartOfBookmark(newInstance.Bookmark);
			}
		}

		private void OnSaveLoaded(SaveData saveData)
		{
		}

		private void DeleteBookmark(Bookmark bookmark)
		{
			m_SaveData.DeleteBookmark(bookmark);
			RequestSave();
		}

		private void RequestSave()
		{
			m_SaveRequested = true;
		}

		private void Save()
		{
			m_SaveStopwatch = new();
			m_SaveStopwatch.Start();

			m_SaveData.Save();
			m_SaveRequested = false;

			m_SaveStopwatch.Stop();
		}

		protected override void DrawProfilingStats()
		{
			base.DrawProfilingStats();
			ImGui.SameLine();
			ImGui.Text($"|  Last save time: {m_SaveStopwatch.ElapsedMicroseconds / 1000.0}ms");
		}

		private void DrawBookmarks()
		{
			DrawBookmarkTable(m_SongSpeed);
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
				bool existingBookmark = m_SaveData.Bookmarks.Exists(x => x.Name == m_BookmarkNameInput);

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
					CreateBookmark(m_BookmarkNameInput.Trim(), startMeasure, endMeasure);
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
				Bookmark bookmark = m_SaveData.Bookmarks.Find(x => x.Name == m_BookmarkNameInput);
				ImGui.SameLine();
				if (ImGui.Button("Delete"))
				{
					DeleteBookmark(bookmark);
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Reset stats"))
				{
					ResetStats(bookmark, m_SongSpeed);
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

		private void ResetStats(Bookmark bookmark, int speed)
		{
			m_SaveData.DeleteHistory(bookmark, speed);
			RequestSave();
		}

		private System.Numerics.Vector4 GetPerformanceColour(float percent, float min)
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
				return new System.Numerics.Vector4(r, g, b, 1.0f);
			}
			else
			{
				float t = (percent - mid) / (1.0f - mid);
				float r = 1.0f - t;
				float g = 1.0f;
				float b = 0.0f;
				return new System.Numerics.Vector4(r, g, b, 1.0f);
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

				for (int i = m_SaveData.Bookmarks.Count - 1; i >= 0; --i)
				{
					ImGui.PushID(i);
					Bookmark bookmark = m_SaveData.Bookmarks[i];
					AggregateNoteStats stats = m_SaveData.GetAggregateStats(new BookmarkKey(bookmark.Name, speed));
					bool isActiveBookmark = m_ActiveBookmarkInstance != null && m_ActiveBookmarkInstance.Bookmark == bookmark;

					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);

					if (isActiveBookmark)
					{
						ImGui.PushStyleColor(ImGuiCol.Text, Utilities.ColourHelper.GetKaImGuiColour());
					}

					if (ImGui.Button(bookmark.Name))
					{
						SelectBookmark(bookmark);
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
