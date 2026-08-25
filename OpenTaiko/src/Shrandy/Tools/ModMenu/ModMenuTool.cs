using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal enum NoteColorMode
	{
		Off,
		Don,
		Ka,
	}

	internal class ModMenuTool : Tool
	{
		private static readonly Dictionary<NotesManager.ENoteType, NotesManager.ENoteType> ToDonMap = new()
		{
			[NotesManager.ENoteType.Ka] = NotesManager.ENoteType.Don,
			[NotesManager.ENoteType.KaBig] = NotesManager.ENoteType.DonBig,
			[NotesManager.ENoteType.KaHand] = NotesManager.ENoteType.DonHand,
			[NotesManager.ENoteType.Kadon] = NotesManager.ENoteType.Don,
		};

		private static readonly Dictionary<NotesManager.ENoteType, NotesManager.ENoteType> ToKaMap = new()
		{
			[NotesManager.ENoteType.Don] = NotesManager.ENoteType.Ka,
			[NotesManager.ENoteType.DonBig] = NotesManager.ENoteType.KaBig,
			[NotesManager.ENoteType.DonHand] = NotesManager.ENoteType.KaHand,
			[NotesManager.ENoteType.Kadon] = NotesManager.ENoteType.Ka,
		};

		private static NoteColorMode s_NoteColorMode = NoteColorMode.Off;
		private readonly Dictionary<CChip, int> m_OriginalNoteChannels = new();

		internal static NoteColorMode CurrentNoteColorMode => s_NoteColorMode;

		internal static string NoteColorModLabel => s_NoteColorMode switch
		{
			NoteColorMode.Don => "AllDon",
			NoteColorMode.Ka  => "AllKa",
			_                 => "None",
		};

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
			DrawAllowAnyHitColour();
			DrawNoteColorMod();
			DrawNoteRemoval();
			ImGui.Separator();
			if (ImGui.Button("Reset to Defaults"))
			{
				ResetToDefaults();
			}
		}

		private void ResetToDefaults()
		{
			OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.SaveFile] = 9;
			OpenTaiko.ConfigIni.nSongSpeed = CConfigIni.DefaultSongSpeed;
			OpenTaiko.ConfigIni.eRandom[OpenTaiko.SaveFile] = ERandomMode.Off;
			OpenTaiko.ConfigIni.nTimingZones[OpenTaiko.SaveFile] = 2;
			OpenTaiko.ConfigIni.bTokkunConstantScrollSpeed = false;
			OpenTaiko.ConfigIni.nFadingNoteTime = 0;
			OpenTaiko.ConfigIni.bAllowAnyHitColour = false;
			m_RemoveEvenNotes = false;
			m_RemoveOddNotes = false;
			ApplyNoteMods();
			s_NoteColorMode = NoteColorMode.Off;
			ApplyNoteColorMod();
			m_OriginalNoteChannels.Clear();
		}

		private void DrawNoteColorMod()
		{
			ImGui.SeparatorText("Note Color");

			string[] names = { "Off", "Force All Don", "Force All Ka" };
			int mode = (int)s_NoteColorMode;
			if (ImGui.Combo("Force Note Color", ref mode, names, names.Length))
			{
				s_NoteColorMode = (NoteColorMode)mode;
				ApplyNoteColorMod();
			}
		}

		private void ApplyNoteColorMod()
		{
			List<CChip>? listChip = OpenTaiko.TJA?.listChip;
			if (listChip == null) return;

			foreach (CChip chip in listChip)
			{
				if (!m_OriginalNoteChannels.TryGetValue(chip, out int originalChannel))
				{
					if (!NotesManager.IsMissableNote(chip)) continue;
					originalChannel = chip.nChannelNo;
					m_OriginalNoteChannels[chip] = originalChannel;
				}

				NotesManager.ENoteType originalType = NotesManager.GetNoteType(originalChannel);
				NotesManager.ENoteType targetType = s_NoteColorMode switch
				{
					NoteColorMode.Don => ToDonMap.GetValueOrDefault(originalType, originalType),
					NoteColorMode.Ka  => ToKaMap.GetValueOrDefault(originalType, originalType),
					_                 => originalType,
				};
				chip.nChannelNo = NotesManager.ToChannelNo(targetType);
			}
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

		private static void DrawAllowAnyHitColour()
		{
			ImGui.Checkbox("Allow Any Hit Colour", ref OpenTaiko.ConfigIni.bAllowAnyHitColour);
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
