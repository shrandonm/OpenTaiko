using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SinglePatternDrillPopup
	{
		private const string PopupId = "Drill Pattern";

		private PatternTool m_Tool;
		private DrillEditorUI m_DrillEditor;
		private DrillData m_StagedDrill = new();
		private PatternFilter m_FillerPatternFilter = new();
		private bool m_PendingOpen = false;

		public SinglePatternDrillPopup(PatternTool tool, DrillEditorUI drillEditor)
		{
			m_Tool = tool;
			m_DrillEditor = drillEditor;
		}

		public void Open(PatternData pattern)
		{
			m_StagedDrill = new DrillData
			{
				Title = $"Drill: {pattern.Title}",
				Patterns = new List<DrillData.PatternWeight>
				{
					new DrillData.PatternWeight { Pattern = pattern, Weight = 1 },
				},
				FillerPatterns = DrillEditorUI.CreateDefaultFillerPatterns(m_Tool.Database.Patterns),
			};
			m_PendingOpen = true;
		}

		/// <summary>Must be called from inside the parent popup so the popups nest.</summary>
		/// <returns>True if a drill was started and the parent popup should close.</returns>
		public bool Draw()
		{
			if (m_PendingOpen)
			{
				ImGui.OpenPopup(PopupId);
				m_PendingOpen = false;
			}

			bool drillStarted = false;
			bool open = true;
			if (ImGui.BeginPopupModal(PopupId, ref open, ImGuiWindowFlags.None))
			{
				ImGui.TextUnformatted(m_StagedDrill.Title);
				ImGui.Spacing();

				DrillEditorUI.DrawFillerSettings(m_StagedDrill, m_Tool.Database.Patterns, m_FillerPatternFilter);

				ImGui.Spacing();
				if (ImGui.Button("Start##sdstart"))
				{
					m_DrillEditor.PlayTemporaryDrill(m_StagedDrill);
					drillStarted = true;
					ImGui.CloseCurrentPopup();
				}

				ImGui.SameLine();
				if (ImGui.Button("Cancel##sdcancel"))
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
			return drillStarted;
		}
	}
}
