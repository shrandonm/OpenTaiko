using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class Tool
	{
		public bool Enabled { get; private set; } = false;
		protected SlimDXKeys.Key m_EnableHotkey = SlimDXKeys.Key.Unknown;

		public Tool(SlimDXKeys.Key enableHotkey)
		{
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

		public virtual void Draw()
		{
		}
	}
}
