using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using System.Numerics;

namespace OpenTaiko.Shrandy
{
	internal class Toolbar
	{
		public static void Draw(List<Tool> tools)
		{
			if (ImGui.BeginMainMenuBar())
			{
				if (ImGui.BeginMenu("Tools"))
				{
					foreach (Tool tool in tools)
					{
						if (ImGui.MenuItem(tool.ToolName, tool.GetHotkeyString(), tool.Enabled))
						{
							tool.SetEnabled(!tool.Enabled);
						}
					}
					ImGui.EndMenu();
				}
				ImGui.EndMainMenuBar();
			}
		}
	}
}
