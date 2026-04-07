using System;
using System.Collections.Generic;
using System.Linq;
using SlimDXKeys;

namespace OpenTaiko.Shrandy.Tools
{
	internal class TrainingTool : Tool
	{
		internal enum Mode
		{
			None,
			AutoRewind,
			Bookmark,
		}

		internal Mode CurrentMode => m_Mode;
		internal int SongSpeed => m_SongSpeed;
		internal int AutoRewindErrorThreshold { get => m_AutoRewindErrorThreshold; set => m_AutoRewindErrorThreshold = value; }
		internal int LastSuccessfulMeasure => m_LastSuccessfulMeasure;
		internal List<int> MeasureFailCounts => m_MeasureFailCounts;
		internal SaveData TrainingSaveData => m_SaveData;
		internal BookmarkInstance? ActiveBookmarkInstance => m_ActiveBookmarkInstance;
		internal MicroStopwatch SaveStopwatch => m_SaveStopwatch;

		private Mode m_Mode = Mode.None;

		private int m_AutoRewindErrorThreshold = 50;
		private int m_LastSuccessfulMeasure = 0;
		private List<int> m_MeasureFailCounts = new();

		private SaveData m_SaveData = new();
		private MeasureListener m_MeasureListener = new();
		private BookmarkInstance? m_ActiveBookmarkInstance;
		private MicroStopwatch m_SaveStopwatch = new();

		private bool m_SaveRequested = false;
		private bool m_WaitingForBookmarkRestart = false;

		private int m_SongSpeed = 100;

		private TrainingToolUI m_UI;
		private SongMods m_SongMods = new();

		public TrainingTool(string toolName, Key enableHotkey) : base(toolName, enableHotkey)
		{
			m_UI = new TrainingToolUI(this, m_SongMods);
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

		internal void SetMode(Mode newMode)
		{
			ResetBookmarkState();
			m_LastSuccessfulMeasure = 0;
			if (newMode == Mode.AutoRewind)
			{
				m_MeasureFailCounts = new(Enumerable.Repeat(0, OpenTaiko.stageGameScreen.actTokkun.nMeasureCount).ToArray());
			}
			else
			{
				m_MeasureFailCounts.Clear();
			}

			if (newMode != Mode.None)
			{
				OpenTaiko.stageGameScreen.actTokkun.QueueJumpToMeasure(0);
			}

			m_Mode = newMode;
		}

		private void ResetBookmarkState()
		{
			m_ActiveBookmarkInstance = null;
			m_WaitingForBookmarkRestart = false;
		}

		internal void SetSongSpeed(int newSpeed)
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
				OpenTaiko.stageGameScreen.actTokkun.QueueJumpToMeasure(0);
			}
		}

		protected override void Draw()
		{
			base.Draw();
			m_UI.Draw();
		}

		protected override void DrawProfilingStats()
		{
			base.DrawProfilingStats();
			m_UI.DrawProfilingStats();
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
				SetMode(Mode.None);
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
				&& IsNoteWithinBookmarkRange(hitParams.Chip, m_ActiveBookmarkInstance.Bookmark))
			{
				m_ActiveBookmarkInstance.NoteStats.OnNoteHit(hitParams);
			}

			if (m_Mode == Mode.AutoRewind
				&& m_AutoRewindErrorThreshold > 0
				&& hitParams.Chip != null
				&& Math.Abs(hitParams.Chip.nLag) > m_AutoRewindErrorThreshold)
			{
				OnMistakeMade();
			}
		}

		public override void OnNoteMiss(CChip? chip)
		{
			if (chip != null
				&& m_ActiveBookmarkInstance != null
				&& IsNoteWithinBookmarkRange(chip, m_ActiveBookmarkInstance.Bookmark))
			{
				m_ActiveBookmarkInstance.NoteStats.OnNoteMissed();
			}

			if (m_Mode == Mode.AutoRewind)
			{
				OnMistakeMade();
			}
		}
		
		private bool IsNoteWithinBookmarkRange(CChip chip, Bookmark bookmark)
		{
			int startNoteIndex = OpenTaiko.TJA.GetListChipIndexOfMeasure(bookmark.StartMeasure);
			int lastNoteIndex = OpenTaiko.TJA.GetListChipIndexOfMeasure(bookmark.EndMeasure + 1) - 1;
			int hitIndex = OpenTaiko.TJA.listChip.IndexOf(chip);

			return hitIndex >= startNoteIndex && hitIndex <= lastNoteIndex;
		}

		private void OnMistakeMade()
		{
			if (m_Mode == Mode.AutoRewind)
			{
				m_MeasureFailCounts[OpenTaiko.stageGameScreen.actTokkun.nCurrentMeasure]++;
				OpenTaiko.stageGameScreen.actTokkun.QueueJumpToMeasure(m_LastSuccessfulMeasure + 1);
			}
		}

		private void JumpToStartOfBookmark(Bookmark bookmark)
		{
			OpenTaiko.stageGameScreen.actTokkun.QueueJumpToMeasure(bookmark.StartMeasure);
		}

		internal void CreateBookmark(string name, int startMeasure, int endMeasure)
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

		internal void SelectBookmark(Bookmark bookmark)
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

			if (m_Mode == Mode.AutoRewind && measure > m_LastSuccessfulMeasure)
			{
				m_LastSuccessfulMeasure = measure;
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

		internal void DeleteBookmark(Bookmark bookmark)
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

		internal void ResetStats(Bookmark bookmark, int speed)
		{
			m_SaveData.DeleteHistory(bookmark, speed);
			RequestSave();
		}
	}
}
