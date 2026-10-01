using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternToolUI
	{
		private const string BackingTrackPopupId = "Select Backing Track";

		private PatternTool m_Tool;
		private PatternEditorUI m_PatternEditor;
		private DrillEditorUI m_DrillEditor;

		public PatternToolUI(PatternTool tool)
		{
			m_Tool = tool;
			m_PatternEditor = new PatternEditorUI(tool);
			m_DrillEditor = new DrillEditorUI(tool);
		}

		public void Draw()
		{
			if (m_Tool.IsActive())
			{
				DrawEditor();
			}
			else
			{
				if (ImGui.Button("Enter Pattern Mode"))
				{
					m_Tool.EnterPatternMode();
				}
			}
		}

		private void DrawEditor()
		{
			string selectedBackingTrackName = m_Tool.BackingTrackSelector.Selected?.DisplayName ?? "None";
			if (ImGui.Button($"Backing Track: {selectedBackingTrackName}"))
			{
				ImGui.OpenPopup(BackingTrackPopupId);
			}

			SongBrowserTool? songBrowserTool = OpenTaiko.ShrandyExtension.GetTool<SongBrowserTool>();
			songBrowserTool?.DrawChartPickerPopup(
				BackingTrackPopupId,
				m_Tool.BackingTrackSelector.IsEligible,
				m_Tool.SelectBackingTrack);

			if (ImGui.Button("Pattern Editor"))
			{
				m_PatternEditor.Open();
			}
			m_PatternEditor.Draw();

			m_DrillEditor.Draw();
		}
	}
}

