using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace OpenTaiko.Shrandy
{
	internal class Tool
	{
		public bool Enabled { get; private set; } = false;
		protected SlimDXKeys.Key m_EnableHotkey = SlimDXKeys.Key.Unknown;
		protected string m_ToolName = "Tool";

		public Tool(string toolName, SlimDXKeys.Key enableHotkey)
		{
			m_ToolName = toolName;
			m_EnableHotkey = enableHotkey;
		}

		public virtual void UpdateEnabledState()
		{
			if (OpenTaiko.InputManager != null && OpenTaiko.InputManager.Keyboard.KeyPressed((int)m_EnableHotkey))
			{
				Enabled = !Enabled;
			}
		}

		public virtual void OnNoteHit(HitParams hitParams)
		{
		}

		public virtual void OnStageChanged(CStage stage)
		{
		}

		public void DrawWindow()
		{
			ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Once);
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 300), ImGuiCond.Once);
			if (ImGui.Begin(m_ToolName))
			{
				Draw();
				ImGui.End();
			}
		}

		public virtual void Draw()
		{
		}
	}
}
