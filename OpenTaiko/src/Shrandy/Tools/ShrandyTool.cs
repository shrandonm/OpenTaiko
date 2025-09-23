using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace OpenTaiko.Shrandy
{
	internal class ShrandyTool : Tool
	{
		public override void Draw()
		{
			ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 300), ImGuiCond.FirstUseEver);
			if (ImGui.Begin("ShrandyTool"))
			{
				bool memes = false;
				ImGui.Checkbox("Show ImGui Demo Window", ref memes);
				ImGui.End();
			}
		}
	}
}
