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
		public string ToolName = "Tool";
		public bool Enabled { get; private set; } = false;
		private SlimDXKeys.Key m_ModifierHotkey = SlimDXKeys.Key.LeftShift;
		private SlimDXKeys.Key m_Hotkey = SlimDXKeys.Key.Unknown;

		private MicroStopwatch m_DrawTime = new();

		public Tool(string toolName, SlimDXKeys.Key enableHotkey)
		{
			ToolName = toolName;
			m_Hotkey = enableHotkey;
		}

		public string GetHotkeyString()
		{
			string modifier = m_ModifierHotkey.ToString().Replace("Left", "");
			return $"{modifier}+{m_Hotkey}";
		}
		
		public bool ShowInToolbar()
		{
			return m_Hotkey != SlimDXKeys.Key.Unknown;
		}

		public void UpdateEnabledState()
		{
			if (OpenTaiko.InputManager != null
				&& m_Hotkey != SlimDXKeys.Key.Unknown
				&& OpenTaiko.InputManager.Keyboard.KeyPressing((int)m_ModifierHotkey)
				&& OpenTaiko.InputManager.Keyboard.KeyPressed((int)m_Hotkey))
			{
				SetEnabled(!Enabled);
			}
		}
		
		public virtual void SetEnabled(bool enabled)
		{
			Enabled = enabled;
		}

		public virtual void OnNoteMiss(CChip? chip)
		{
		}

		public virtual void OnNoteHit(HitParams hitParams)
		{
		}

		public virtual void OnStageChanged(CStage stage)
		{
		}

		public virtual void OnResultsActivate(CStage結果 resultsScreen)
		{
		}

		public virtual void OnTrainingModeResumePlay()
		{
		}

		public virtual void OnSongRestart()
		{
		}

		public virtual void DrawWindow()
		{
			Update();

			ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 300), ImGuiCond.FirstUseEver);
			bool open = Enabled;
			if (ImGui.Begin(ToolName, ref open))
			{
				m_DrawTime.Restart();
				Draw();
				m_DrawTime.Stop();

				ImGui.Separator();

				DrawProfilingStats();

				ImGui.End();
				Enabled = open;
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

		public virtual bool IsBlockingInput()
		{
			return false;
		}
	}
}
