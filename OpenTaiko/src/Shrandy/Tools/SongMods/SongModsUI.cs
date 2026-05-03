using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SongModsUI
	{
		private ExtraSongMods m_Mods;

		private bool m_RemoveEvenNotes = false;
		private bool m_RemoveOddNotes = false;

		internal SongModsUI(ExtraSongMods mods)
		{
			m_Mods = mods;
		}

		internal void Draw()
		{
			ImGui.SeparatorText("Note Removal");

			if (ImGui.Checkbox("Remove Even Notes", ref m_RemoveEvenNotes))
			{
				m_Mods.SetRemoveEvenNotes(m_RemoveEvenNotes);
			}

			if (ImGui.Checkbox("Remove Odd Notes", ref m_RemoveOddNotes))
			{
				m_Mods.SetRemoveOddNotes(m_RemoveOddNotes);
			}
		}
	}
}
