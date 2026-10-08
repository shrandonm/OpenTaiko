using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternFilter
	{
		private string m_Text = "";

		public bool IsActive
		{
			get
			{
				return m_Text.Length > 0;
			}
		}

		public void Draw(string id)
		{
			ImGui.SetNextItemWidth(200);
			ImGui.InputTextWithHint($"##{id}", "Filter...", ref m_Text, 256);
		}

		public bool Matches(PatternData pattern)
		{
			return !IsActive || pattern.Title.Contains(m_Text, StringComparison.OrdinalIgnoreCase);
		}
	}
}
