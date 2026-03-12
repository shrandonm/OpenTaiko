using System;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy
{
	internal static class ImGuiHelpers
	{
		internal readonly struct ScopedStyleColor : IDisposable
		{
			private readonly int m_Count;

			public ScopedStyleColor(ImGuiCol col, uint color)
			{
				ImGui.PushStyleColor(col, color);
				m_Count = 1;
			}

			public ScopedStyleColor(ImGuiCol col, Vector4 color)
			{
				ImGui.PushStyleColor(col, color);
				m_Count = 1;
			}

			public ScopedStyleColor(ImGuiCol col1, uint color1, ImGuiCol col2, uint color2)
			{
				ImGui.PushStyleColor(col1, color1);
				ImGui.PushStyleColor(col2, color2);
				m_Count = 2;
			}

			public void Dispose()
			{
				if (m_Count > 0)
				{
					ImGui.PopStyleColor(m_Count);
				}
			}
		}
	}
}
