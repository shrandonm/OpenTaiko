using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using SlimDXKeys;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy.TrainingTool
{
	internal class TrainingTool : Tool
	{
		private SaveData m_SaveData = new();
		private MeasureListener m_MeasureListener = new();
		private BookmarkInstance? m_ActiveBookmarkInstance;

		private bool m_SaveRequested = false;

		private string m_BookmarkNameInput = "";
		private int m_StartMeasureInput;
		private int m_EndMeasureInput;

		const int RecentStatCount = 10;

		public TrainingTool(Key enableHotkey) : base(enableHotkey)
		{
			m_MeasureListener.OnMeasureCompleted += OnMeasureCompleted;
		}

		~TrainingTool()
		{
			m_MeasureListener.OnMeasureCompleted -= OnMeasureCompleted;
		}

		public override void OnNoteHit(HitParams hitParams)
		{
			CActImplTrainingMode trainingMode = OpenTaiko.stageGameScreen.actTokkun;
			if (trainingMode == null)
			{
				return;
			}

			if (OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold <= 0)
			{
				return;
			}

			if (m_ActiveBookmarkInstance != null && IsNoteWithinBookmarkRange(hitParams, m_ActiveBookmarkInstance.Bookmark))
			{
				m_ActiveBookmarkInstance.NoteStats.OnNoteHit(hitParams);
			}

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
			RequestSave();
		}

		private void SelectBookmark(Bookmark bookmark)
		{
			m_ActiveBookmarkInstance = CreateBookmarkInstance(bookmark);
			CActImplTrainingMode trainingMode = OpenTaiko.stageGameScreen.actTokkun;
			if (trainingMode != null)
			{
				JumpToStartOfBookmark(bookmark);
			}
		}

		private BookmarkInstance CreateBookmarkInstance(Bookmark bookmark)
		{
			return new()
			{
				BookmarkName = bookmark.Name,
				Bookmark = bookmark,
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

			BookmarkInstance newInstance = CreateBookmarkInstance(bookmarkInstance.Bookmark);
			m_ActiveBookmarkInstance = newInstance;
			JumpToStartOfBookmark(newInstance.Bookmark);
		}

		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);

			m_SaveData = new();

			if (stage == OpenTaiko.stageGameScreen)
			{
				SaveData? loadedSaveData = SaveData.Load(OpenTaiko.GetTJA(0)?.strFileName ?? "");
				if (loadedSaveData != null)
				{
					m_SaveData = loadedSaveData;
					OnSaveLoaded(m_SaveData);
				}

				m_MeasureListener.Reset();
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

		public override void Draw()
		{
			base.Draw();

			m_MeasureListener.Update();

			ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Once);
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 300), ImGuiCond.Once);
			if (ImGui.Begin("ShrandyTool"))
			{
				if (OpenTaiko.stageGameScreen.actTokkun != null)
				{
					DrawBookmarks();
				}

				ImGui.End();
			}

			if (m_SaveRequested)
			{
				m_SaveData.Save();
				m_SaveRequested = false;
			}
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
				DrawBookmark(m_SaveData.Bookmarks[i]);
			}
		}

		private System.Numerics.Vector4 GetPerformanceColour(float percent)
		{
			const float min = 0.85f;
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

		private void DrawBookmark(Bookmark bookmark)
		{
			ImGui.PushID(bookmark.Name);

			NoteStats recentStats = m_SaveData.GetAggregateStats(bookmark.Name, RecentStatCount, 1);
			NoteStats allTimeStats = m_SaveData.GetAggregateStats(bookmark.Name, int.MaxValue, 1);

			int totalNotes = recentStats.TotalNotes;
			string headerText = $"{recentStats.GetPercentString(recentStats.GoodCount, totalNotes)} {bookmark.Name}";
			var performanceColour = GetPerformanceColour(recentStats.GetPercent(recentStats.GoodCount, totalNotes));

			ImGui.PushStyleColor(ImGuiCol.Text, performanceColour);
			bool headerExpanded = ImGui.CollapsingHeader($"{headerText}###{bookmark.Name}");
			ImGui.PopStyleColor();
			if (headerExpanded)
			{
				ImGui.Indent();

				ImGui.Text($"Start Measure: {bookmark.StartMeasure}");
				ImGui.SameLine();
				ImGui.Text($"End Measure: {bookmark.EndMeasure}");

				if (ImGui.Button("Go"))
				{
					SelectBookmark(bookmark);
				}
				ImGui.SameLine();
				if (ImGui.Button("Delete Bookmark"))
				{
					DeleteBookmark(bookmark);
				}
				DrawBookmarkStats(bookmark, recentStats, allTimeStats);
				ImGui.Separator();
				DrawGraph(m_SaveData.GetBookmarkEntryList(bookmark.Name));

				ImGui.Unindent();
			}
			ImGui.PopID();
		}

		private void DrawBookmarkStats(Bookmark bookmark, NoteStats statsA, NoteStats statsB)
		{
			if (ImGui.Button("Reset stats"))
			{
				m_SaveData.History.Clear();
			}

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
				goodPercentages.Add(instance.NoteStats.GetPercent(instance.NoteStats.GoodCount, instance.NoteStats.TotalNotes));
			}

			ImGui.Text("Good percentage over time");
			ImGui.Text("100%%");
			ImGui.PlotLines("", ref goodPercentages.ToArray()[0], goodPercentages.Count,
				values_offset: 0,
				overlay_text: string.Empty,
				scale_min: 0.5f,
				scale_max: 1.0f,
				new System.Numerics.Vector2(0, 120.0f));
			ImGui.Text("50%%");
		}
	}
}
