using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternToolUI
	{
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
			if (ImGui.Button("Pattern Editor"))
			{
				m_PatternEditor.Open();
			}
			m_PatternEditor.Draw();

			m_DrillEditor.Draw();
		}
	}
}

