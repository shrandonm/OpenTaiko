using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class RetryPopup
	{
		private SongBrowserData m_Data;
		private int m_Selection;

		private static readonly string[] Options = { "Random Song", "Retry", "Close" };

		public RetryPopup(SongBrowserData data)
		{
			m_Data = data;
		}

		public void Show()
		{
			ImGui.OpenPopup("Retry");
			m_Selection = 0;
		}

		public void Draw()
		{
			bool keepOpen = true;
			if (!ImGui.BeginPopupModal("Retry", ref keepOpen, ImGuiWindowFlags.AlwaysAutoResize))
			{
				return;
			}

			if (OpenTaiko.Pad.IsPressingLeftChange(forceAllowGameInput: true))
			{
				m_Selection = (m_Selection - 1 + Options.Length) % Options.Length;
			}
			if (OpenTaiko.Pad.IsPressingRightChange(forceAllowGameInput: true))
			{
				m_Selection = (m_Selection + 1) % Options.Length;
			}

			bool confirm = OpenTaiko.Pad.IsPressingDecide(forceAllowGameInput: true);
			bool cancel = OpenTaiko.Pad.IsPressingCancel(forceAllowGameInput: true);

			for (int i = 0; i < Options.Length; i++)
			{
				if (i > 0) ImGui.SameLine();

				bool isSelected = i == m_Selection;
				if (isSelected)
				{
					ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
				}

				if (ImGui.Button(Options[i]))
				{
					m_Selection = i;
					confirm = true;
				}

				if (isSelected)
				{
					ImGui.PopStyleColor();
				}
			}

			if (cancel || !keepOpen)
			{
				ImGui.CloseCurrentPopup();
			}
			else if (confirm)
			{
				switch (m_Selection)
				{
					case 0: // Random Song
						var random = m_Data.GetRandomFilteredSong();
						if (random != null)
						{
							Utilities.SongTable.PlaySong(random.Value.song, random.Value.difficulty);
						}
						break;
					case 1: // Retry
						var chosen = OpenTaiko.stageSongSelect.rChoosenSong;
						if (chosen != null)
						{
							Utilities.SongTable.PlaySong(chosen, m_Data.GetLastChosenDifficulty());
						}
						break;
					case 2: // Close
						break;
				}
				ImGui.CloseCurrentPopup();
			}

			ImGui.EndPopup();
		}
	}
}
