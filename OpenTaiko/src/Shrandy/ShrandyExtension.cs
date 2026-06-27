using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace OpenTaiko.Shrandy
{
	public enum Hand
	{
		None,
		Left,
		Right,
	}

	public enum Note
	{
		Don,
		Ka,
	}

	internal struct HitParams
	{
		public CChip Chip;
		public ENoteJudge JudgeResult;
		public Hand Hand;
		public Note Note;
		public double HitErrorMs;
		public double HitTjaTimeMs;
	}

	class ShrandyExtension
	{
		public const string SaveDirectoryPath = "ShrandySaveData";
		private List<Tool> m_Tools = new();

		public ShrandyExtension()
		{
			if (!Directory.Exists(SaveDirectoryPath))
			{
				Directory.CreateDirectory(SaveDirectoryPath);
			}
			m_Tools.Add(new Tools.TrainingTool("Training Tool", SlimDXKeys.Key.T));
			m_Tools.Add(new Tools.NoteVisualizer("Note Visualizer", SlimDXKeys.Key.V));
			m_Tools.Add(new Tools.SongBrowserTool("Song Browser", SlimDXKeys.Key.S));
			m_Tools.Add(new Tools.HitErrorBarTool("Hit Error Bar", SlimDXKeys.Key.E));
			m_Tools.Add(new Tools.ModMenuTool("Game Mods", SlimDXKeys.Key.M));
			m_Tools.Add(new Tools.MarathonTool(SlimDXKeys.Key.P));
			m_Tools.Add(new Tools.PatternTool("Pattern Tool", SlimDXKeys.Key.L));
			
			m_Tools.Add(new Tools.ResultsTool("Results Tool"));
			m_Tools.Add(new Tools.PreviewTool("Preview Tool"));
			m_Tools.Add(new Tools.SongLoadingOverlayTool());
		}

		public T? GetTool<T>() where T : Tool
		{
			return m_Tools.Find(x => x is T) as T;
		}

		public void SetToolEnabled<T>(bool enabled) where T : Tool
		{
			m_Tools.Find(x => x.GetType() == typeof(T))?.SetEnabled(enabled);
		}

		public void OnStageChanged(CStage stage)
		{
			foreach (Tool tool in m_Tools)
			{
				tool.OnStageChanged(stage);
			}
		}
		
		public void OnSongComplete()
		{
			foreach (Tool tool in m_Tools)
			{
				tool.OnSongComplete();
			}
		}
		
		public bool HandleSongCompleteTransition()
		{
			foreach (Tool tool in m_Tools)
			{
				if (tool.HandleSongCompleteTransition())
				{
					return true;
				}
			}
			return false;
		}

		public void OnTrainingModeResumePlay()
		{
			foreach (Tool tool in m_Tools)
			{
				tool.OnTrainingModeResumePlay();
			}
		}

		public void OnSongRestart()
		{
			foreach (Tool tool in m_Tools)
			{
				tool.OnSongRestart();
			}
		}

		public void OnPerformanceInfoActivate()
		{
			OpenTaiko.stageGameScreen.m_ShrandyGameOverlay.Reset();
		}

		public void OnResultsActivate(CStage結果 resultsScreen)
		{
			foreach (Tool tool in m_Tools)
			{
				tool.OnResultsActivate(resultsScreen);
			}
		}

		public void OnNoteMiss(CChip? chip)
		{
			foreach (Tool tool in m_Tools)
			{
				tool.OnNoteMiss(chip);
			}
		}

		public void OnNoteHit(CChip? chip, ENoteJudge judgeResult, EPad pad, double hitTjaTimeMs)
		{
			HitParams hitParams = new()
			{
				Chip = chip,
				JudgeResult = judgeResult,
				Hand = GetHandFromPad(pad),
				Note = GetNoteFromPad(pad),
				HitErrorMs = chip != null ? chip.nLag : 0,
				HitTjaTimeMs = hitTjaTimeMs,
			};
			OpenTaiko.stageGameScreen.m_ShrandyGameOverlay.OnNoteHit(hitParams);

			foreach (Tool tool in m_Tools)
			{
				tool.OnNoteHit(hitParams);
			}
		}

		public void Draw()
		{
			Toolbar.Draw(m_Tools);
			bool blockHotkeys = ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup);
				
			foreach (Tool tool in m_Tools)
			{
				if (!blockHotkeys)
				{
					tool.UpdateEnabledState();
				}
				if (tool.Enabled)
				{
					tool.DrawWindow();
				}
			}
		}

		public bool IsGameInputAllowed()
		{
			if (ImGui.IsPopupOpen("", ImGuiPopupFlags.AnyPopup))
			{
				return false;
			}
			else if (m_Tools.Exists(x => x.Enabled && x.IsBlockingInput()))
			{
				return false;
			}

			return true;
		}

		private static Hand GetHandFromPad(EPad pad)
		{
			switch (pad)
			{
				case EPad.LRed:
				case EPad.LBlue:
				case EPad.LRed2P:
				case EPad.LBlue2P:
					return Hand.Left;
				case EPad.RRed:
				case EPad.RBlue:
				case EPad.RRed2P:
				case EPad.RBlue2P:
					return Hand.Right;
			}
			return Hand.None;
		}

		private static Note GetNoteFromPad(EPad pad)
		{
			switch (pad)
			{
				case EPad.RRed:
				case EPad.LRed:
				case EPad.RRed2P:
				case EPad.LRed2P:
					return Note.Don;
				case EPad.RBlue:
				case EPad.LBlue:
				case EPad.RBlue2P:
				case EPad.LBlue2P:
					return Note.Ka;
			}
			return Note.Don;
		}

		public static bool IsGood(ENoteJudge judgement)
		{
			return judgement <= ENoteJudge.Perfect;
		}

		public static bool IsOkay(ENoteJudge judgement)
		{
			return judgement == ENoteJudge.Good;
		}
	}
}


