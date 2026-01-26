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

		private MicroStopwatch m_DrawTime = new();

		public Tool(string toolName, SlimDXKeys.Key enableHotkey)
		{
			m_ToolName = toolName;
			m_EnableHotkey = enableHotkey;
		}

		public void UpdateEnabledState()
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

		public virtual void OnTrainingModeResumePlay()
		{
		}

		public virtual void OnSongRestart()
		{
		}

		public void DrawWindow()
		{
			Update();

			ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 300), ImGuiCond.FirstUseEver);
			if (ImGui.Begin(m_ToolName))
			{
				m_DrawTime.Restart();
				Draw();
				m_DrawTime.Stop();

				ImGui.Separator();

				DrawProfilingStats();

				ImGui.End();
			}
		}

		protected virtual void Draw()
		{
		}

		protected virtual void Update()
		{
		}

		protected virtual void DrawProfilingStats()
		{
			ImGui.Text("Performance Metrics");
			ImGui.SameLine();
			ImGui.Text($"Draw time: {m_DrawTime.ElapsedMicroseconds / 1000.0}ms");
		}
	}
}
