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

		private string m_BookmarkNameInput = "";
		private int m_StartMeasureInput;
		private int m_EndMeasureInput;

		const int RecentStatCount = 10;
		readonly int[] Speeds = { 100, 95, 90, 85, 80, 70, 50 };
		// Scroll speeds start at 9. They do (speed + 1) / 10 to get 1.0, 1.1, 1.2 etc
		readonly int[] ScrollSpeeds = { 9, 9, 10, 10, 11, 12, 14 };

		public TrainingTool(string toolName, Key enableHotkey) : base(toolName, enableHotkey)
		{
		}

		protected override void Update()
		{
			base.Update();

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

		protected override void Draw()
		{
			base.Draw();

			if (OpenTaiko.stageGameScreen.actTokkun != null && OpenTaiko.ConfigIni.bTokkunMode)
			{
				DrawBookmarks();
			}
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
			}
		}

		public override void OnNoteHit(HitParams hitParams)
		{
			CActImplTrainingMode trainingMode = OpenTaiko.stageGameScreen.actTokkun;
			if (trainingMode == null || !OpenTaiko.ConfigIni.bTokkunMode)
			{
				return;
			}

			if (m_ActiveBookmarkInstance != null && IsNoteWithinBookmarkRange(hitParams, m_ActiveBookmarkInstance.Bookmark))
			{
				m_ActiveBookmarkInstance.NoteStats.OnNoteHit(hitParams);
			}

			if (OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold > 0)
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

		private void SelectBookmark(Bookmark bookmark, int speed)
		{
			m_ActiveBookmarkInstance = CreateBookmarkInstance(bookmark, speed);
			CActImplTrainingMode trainingMode = OpenTaiko.stageGameScreen.actTokkun;
			if (trainingMode != null)
			{
				int scrollSpeedIndex = Array.IndexOf(Speeds, speed);
				OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] = ScrollSpeeds[scrollSpeedIndex];
				const int songSpeedInterval = 5;
				trainingMode.SetSongSpeed(speed / songSpeedInterval);
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
				BookmarkInstance newInstance = CreateBookmarkInstance(m_ActiveBookmarkInstance.Bookmark, m_ActiveBookmarkInstance.Speed);
				m_ActiveBookmarkInstance = newInstance;
				JumpToStartOfBookmark(newInstance.Bookmark);
			}
		}

		private void OnSaveLoaded(SaveData saveData)
		{
			foreach (var kvp in saveData.History)
			{
				if (kvp.Value == null)
				{
					continue;
				}

				foreach (BookmarkInstance instance in kvp.Value)
				{
					instance.Bookmark = saveData.Bookmarks.Find(x => x.Name == instance.BookmarkName);
				}
			}
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
			ImGui.Text("Bookmark Management");

			if (ImGui.CollapsingHeader("Create Bookmark"))
			{
				ImGui.Indent();
				ImGui.InputText("Bookmark Name", ref m_BookmarkNameInput, 256);
				ImGui.InputInt("Start Measure", ref m_StartMeasureInput);
				ImGui.InputInt("End Measure", ref m_EndMeasureInput);
				if (ImGui.Button("Create bookmark"))
				{
					CreateBookmark(m_BookmarkNameInput, m_StartMeasureInput, m_EndMeasureInput);
				}
				ImGui.Unindent();
			}

			ImGui.Separator();
			ImGui.Text("Bookmarks");
			for (int i = m_SaveData.Bookmarks.Count - 1; i >= 0; --i)
			{
				DrawBookmarkTabs(m_SaveData.Bookmarks[i]);
			}
		}

		private System.Numerics.Vector4 GetPerformanceColour(float percent)
		{
			if (percent == 0.0f)
			{
				return new(0.75f);
			}
			const float min = 0.80f;
			const float mid = (1.0f + min) / 2.0f;

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

		private bool DrawBookmarkHeader(Bookmark bookmark)
		{
			AggregateNoteStats recentStats = m_SaveData.GetAggregateStats(new BookmarkKey(bookmark.Name, 100), RecentStatCount);

			int totalNotes = recentStats.CombinedNoteStats.TotalNotes;
			string headerText = $"{StringHelpers.GetPercentString(recentStats.CombinedNoteStats.GoodCount, totalNotes)} {bookmark.Name}";
			var performanceColour = GetPerformanceColour(StringHelpers.GetPercent(recentStats.CombinedNoteStats.GoodCount, totalNotes));

			ImGui.PushStyleColor(ImGuiCol.Text, performanceColour);
			bool headerExpanded = ImGui.CollapsingHeader($"{headerText}###{bookmark.Name}");
			ImGui.PopStyleColor();

			return headerExpanded;
		}

		private void DrawBookmarkTab(Bookmark bookmark, int speed)
		{
			BookmarkKey key = new BookmarkKey(bookmark.Name, speed);
			AggregateNoteStats stats = m_SaveData.GetAggregateStats(key, RecentStatCount);
			var performanceColour = GetPerformanceColour(stats.CombinedNoteStats.GetGoodPercent());

			ImGui.PushStyleColor(ImGuiCol.Text, performanceColour);
			bool isTabActive = ImGui.BeginTabItem($"{speed}% ({stats.CombinedNoteStats.GetGoodPercentString()})###{key.Key}");
			ImGui.PopStyleColor();

			if (isTabActive)
			{
				DrawBookmark(bookmark, speed);
				ImGui.EndTabItem();
			}
		}

		private void DrawBookmarkTabs(Bookmark bookmark)
		{
			ImGui.PushID(bookmark.Name);

			if (DrawBookmarkHeader(bookmark))
			{
				ImGui.Text($"Measures {bookmark.StartMeasure} to {bookmark.EndMeasure}");
				ImGui.BeginTabBar(bookmark.Name);

				foreach (int speed in Speeds)
				{
					DrawBookmarkTab(bookmark, speed);
				}

				ImGui.EndTabBar();
			}
			ImGui.PopID();
		}

		private void ResetStats(Bookmark bookmark, int speed)
		{
			m_SaveData.DeleteHistory(bookmark, speed);
			RequestSave();
		}

		private void DrawBookmark(Bookmark bookmark, int speed)
		{
			ImGui.Indent();
			if (ImGui.Button("Go"))
			{
				SelectBookmark(bookmark, speed);
			}
			ImGui.SameLine();
			if (ImGui.Button("Delete Bookmark"))
			{
				DeleteBookmark(bookmark);
			}
			ImGui.SameLine();
			if (ImGui.Button("Reset stats"))
			{
				ResetStats(bookmark, speed);
			}

			BookmarkKey key = new BookmarkKey(bookmark.Name, speed);
			AggregateNoteStats recentStats = m_SaveData.GetAggregateStats(key, RecentStatCount);
			AggregateNoteStats allTimeStats = m_SaveData.GetAggregateStats(key, int.MaxValue);
			DrawBookmarkStats(bookmark, recentStats, allTimeStats);
			ImGui.Separator();
			DrawGraph(m_SaveData.GetBookmarkEntryList(key));

			ImGui.Unindent();
		}

		private void DrawBookmarkStats(Bookmark bookmark, AggregateNoteStats statsA, AggregateNoteStats statsB)
		{
			if (ImGui.BeginTable("StatsTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
			{
				ImGui.TableSetupColumn($"Last {RecentStatCount} runs", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("All Time", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableHeadersRow();

				ImGui.TableNextRow();

				ImGui.TableSetColumnIndex(0);
				statsA.Draw();
				ImGui.TableSetColumnIndex(1);
				statsB.Draw();

				ImGui.EndTable();
			}
		}

		private void DrawGraph(List<BookmarkInstance> instances)
		{
			if (instances.Count == 0)
			{
				return;
			}
			List<float> goodPercentages = new(instances.Count);
			foreach (BookmarkInstance instance in instances)
			{
				goodPercentages.Add(StringHelpers.GetPercent(instance.NoteStats.GoodCount, instance.NoteStats.TotalNotes));
			}


			ImGui.Text("Good percentage over time");
			ImGui.Text("100%%");
			ImGui.PlotLines("", ref goodPercentages.ToArray()[0], goodPercentages.Count,
				values_offset: 0,
				overlay_text: string.Empty,
				scale_min: 0.0f,
				scale_max: 1.0f,
				new System.Numerics.Vector2(0, 120.0f));
			ImGui.Text("0%%");
		}
	}
}
