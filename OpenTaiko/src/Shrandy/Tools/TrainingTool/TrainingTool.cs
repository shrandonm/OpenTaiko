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
		private BookmarkInstance? m_BookmarkInstance;
		private List<BookmarkInstance> m_BookmarkSessionHistory = new();

		private bool m_SaveRequested = false;

		private string m_BookmarkNameInput = "";
		private int m_StartMeasureInput;
		private int m_EndMeasureInput;

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

			if (m_BookmarkInstance != null && IsNoteWithinBookmarkRange(hitParams, m_BookmarkInstance.Bookmark))
			{
				m_BookmarkInstance.NoteStats.OnNoteHit(hitParams);
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
			if (m_BookmarkInstance == null)
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
			m_BookmarkInstance = CreateBookmarkInstance(bookmark);
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
			if (m_BookmarkInstance != null && m_BookmarkInstance.Bookmark.EndMeasure == measure)
			{
				CompleteBookmark(m_BookmarkInstance);
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
				AddToHistory(m_SaveData.BookmarkHistory, bookmarkInstance);
				AddToHistory(m_BookmarkSessionHistory, bookmarkInstance);
				RequestSave();
			}

			BookmarkInstance newInstance = CreateBookmarkInstance(bookmarkInstance.Bookmark);
			m_BookmarkInstance = newInstance;
			JumpToStartOfBookmark(newInstance.Bookmark);
		}

		private void AddToHistory(List<BookmarkInstance> history, BookmarkInstance bookmarkInstance)
		{
			BookmarkInstance? recordedInstance = history.Find(x => x.BookmarkName == bookmarkInstance.BookmarkName);
			if (recordedInstance == null)
			{
				recordedInstance = CreateBookmarkInstance(bookmarkInstance.Bookmark);
				history.Add(recordedInstance);
			}

			recordedInstance.PlayCount++;
			recordedInstance.NoteStats = recordedInstance.NoteStats + bookmarkInstance.NoteStats;
		}

		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);

			m_SaveData = new();
			m_BookmarkSessionHistory.Clear();

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
			foreach (BookmarkInstance bookmarkInstance in saveData.BookmarkHistory)
			{
				bookmarkInstance.Bookmark = saveData.Bookmarks.Find(x => x.Name == bookmarkInstance.BookmarkName);
			}
		}

		private void DeleteBookmark(Bookmark bookmark)
		{
			m_SaveData.Bookmarks.Remove(bookmark);
			m_SaveData.BookmarkHistory.RemoveAll(x => x.BookmarkName == bookmark.Name);
			m_BookmarkSessionHistory.RemoveAll(x => x.BookmarkName == bookmark.Name);
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
			ImGui.Text("Bookmarked Measures");

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
			for (int i = m_SaveData.Bookmarks.Count - 1; i >= 0; --i)
			{
				DrawBookmark(m_SaveData.Bookmarks[i]);
			}
		}

		private void DrawBookmark(Bookmark bookmark)
		{
			ImGui.PushID(bookmark.Name);

			if (ImGui.CollapsingHeader(bookmark.Name))
			{
				ImGui.Indent();

				ImGui.Text(bookmark.Name);
				ImGui.Text($"Start Measure: {bookmark.StartMeasure}");
				ImGui.Text($"End Measure: {bookmark.EndMeasure}");
				DrawBookmarkStats(bookmark);
				if (ImGui.Button("Go"))
				{
					SelectBookmark(bookmark);
				}
				ImGui.SameLine();
				if (ImGui.Button("Delete"))
				{
					DeleteBookmark(bookmark);
				}
				ImGui.Separator();

				ImGui.PopID();
				ImGui.Unindent();
			}
		}

		private void DrawBookmarkStats(Bookmark bookmark)
		{
			DrawBookmarkStats("This Session", m_BookmarkSessionHistory, bookmark);
			DrawBookmarkStats("All Time", m_SaveData.BookmarkHistory, bookmark);
		}

		private void DrawBookmarkStats(string title, List<BookmarkInstance> history, Bookmark bookmark)
		{
			BookmarkInstance? instance = history.Find(x => x.BookmarkName == bookmark.Name);
			if (instance != null)
			{
				ImGui.PushID(title);

				if (ImGui.CollapsingHeader(title, ImGuiTreeNodeFlags.DefaultOpen))
				{
					ImGui.Indent();
					ImGui.Text($"Play Count: {instance.PlayCount}");
					int totalNotes = instance.NoteStats.TotalNotes;
					if (totalNotes > 0)
					{
						float goodPercent = (instance.NoteStats.GoodCount / (float)totalNotes) * 100.0f;
						float okayPercent = (instance.NoteStats.OkayCount / (float)totalNotes) * 100.0f;
						float badPercent = (instance.NoteStats.BadCount   / (float)totalNotes) * 100.0f;

						ImGui.Text($"Total Notes: {totalNotes}");
						ImGui.Separator();
						ImGui.Text($"Goods: {instance.NoteStats.GoodCount} ({goodPercent:F2}%%)");
						ImGui.Text($"Okays: {instance.NoteStats.OkayCount} ({okayPercent:F2}%%)");
						ImGui.Text($"Bads: {instance.NoteStats.BadCount} ({badPercent:F2}%%)");
						ImGui.Text($"Average Error: +/- {instance.NoteStats.AverageHitError}ms");
					}

					if (ImGui.Button("Reset"))
					{
						history.Clear();
					}

					ImGui.Unindent();
				}
				ImGui.PopID();
			}
		}
	}
}
