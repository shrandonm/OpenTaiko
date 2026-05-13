using System;
using System.Linq;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class ModMenuTool : Tool
	{
		private bool m_RemoveEvenNotes = false;
		private bool m_RemoveOddNotes = false;

		private static string[] RandomModeNames => Enum.GetValues<ERandomMode>()
			.Select(m => m switch
			{
				ERandomMode.Off          => CLangManager.LangInstance.GetString("MOD_SWITCH_OFF"),
				ERandomMode.Random       => CLangManager.LangInstance.GetString("MOD_RANDOM"),
				ERandomMode.Mirror       => CLangManager.LangInstance.GetString("MOD_FLIP"),
				ERandomMode.SuperRandom  => CLangManager.LangInstance.GetString("MOD_RANDOM_CHAOS"),
				ERandomMode.MirrorRandom => CLangManager.LangInstance.GetString("MOD_RANDOM_SHUFFLE"),
				_                        => m.ToString(),
			})
			.ToArray();

		private static string[] TimingNames => Enumerable.Range(1, OpenTaiko.ConfigIni.tzLevels.Length)
			.Select(i => CLangManager.LangInstance.GetString($"MOD_TIMING{i}"))
			.ToArray();

		internal ModMenuTool(string toolName, SlimDXKeys.Key enableHotkey)
			: base(toolName, enableHotkey)
		{
		}

		protected override void Draw()
		{
			DrawScrollSpeed();
			DrawSongSpeed();
			DrawRandom();
			DrawTiming();
			DrawConstantScrollSpeed();
			DrawFadingNoteTime();
			DrawNoteRemoval();
		}

		public static void DrawScrollSpeed()
		{
			float scrollSpeed = Utilities.SpeedConversions.GetActualScrollSpeed(OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile]);
			ImGui.InputFloat("Scroll Speed", ref scrollSpeed, 0.1f);
			OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] = Utilities.SpeedConversions.GetScrollSpeedIntValue(scrollSpeed);
		}

		private static void DrawSongSpeed()
		{
			int songSpeed = OpenTaiko.ConfigIni.nSongSpeed;
			ImGui.SetNextItemWidth(120);
			if (ImGui.InputInt($"Song Speed % ({CConfigIni.MinSongSpeed}-{CConfigIni.MaxSongSpeed})", ref songSpeed, 1, 10))
			{
				OpenTaiko.ConfigIni.nSongSpeed = Math.Clamp(songSpeed, CConfigIni.MinSongSpeed, CConfigIni.MaxSongSpeed);
			}
		}

		private static void DrawRandom()
		{
			int randomIndex = (int)OpenTaiko.ConfigIni.eRandom[OpenTaiko.SaveFile];
			if (ImGui.Combo("Random", ref randomIndex, RandomModeNames, RandomModeNames.Length))
			{
				OpenTaiko.ConfigIni.eRandom[OpenTaiko.SaveFile] = (ERandomMode)randomIndex;
			}
		}

		private static void DrawTiming()
		{
			int timingIndex = OpenTaiko.ConfigIni.nTimingZones[OpenTaiko.SaveFile];
			if (ImGui.Combo("Timing", ref timingIndex, TimingNames, TimingNames.Length))
			{
				OpenTaiko.ConfigIni.nTimingZones[OpenTaiko.SaveFile] = timingIndex;
			}
		}

		private static void DrawConstantScrollSpeed()
		{
			ImGui.Checkbox("Constant Scroll Speed", ref OpenTaiko.ConfigIni.bTokkunConstantScrollSpeed);
		}

		private static void DrawFadingNoteTime()
		{
			int fadingNoteTime = OpenTaiko.ConfigIni.nFadingNoteTime;
			ImGui.SetNextItemWidth(120);
			if (ImGui.InputInt("Fading Note Time (ms)", ref fadingNoteTime, 10, 100))
			{
				OpenTaiko.ConfigIni.nFadingNoteTime = Math.Max(0, fadingNoteTime);
			}
		}

		private void DrawNoteRemoval()
		{
			ImGui.SeparatorText("Note Removal");

			if (ImGui.Checkbox("Remove Even Notes", ref m_RemoveEvenNotes))
			{
				ApplyNoteMods();
			}

			if (ImGui.Checkbox("Remove Odd Notes", ref m_RemoveOddNotes))
			{
				ApplyNoteMods();
			}
		}

		private void ApplyNoteMods()
		{
			var listChip = OpenTaiko.TJA?.listChip;
			if (listChip == null) return;

			int noteIndex = 0;
			foreach (var chip in listChip)
			{
				if (NotesManager.IsMissableNote(chip))
				{
					noteIndex++;
					bool isEven = noteIndex % 2 == 0;
					bool shouldRemove = (isEven && m_RemoveEvenNotes) || (!isEven && m_RemoveOddNotes);
					chip.bVisible = !shouldRemove;
					chip.bShow = !shouldRemove;
				}
			}
		}
	}
}
