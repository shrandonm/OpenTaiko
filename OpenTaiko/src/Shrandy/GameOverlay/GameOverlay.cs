using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class GameOverlay
	{
		private List<OverlayWidget> m_OverlayWidgets = new();

		public GameOverlay(CStage演奏ドラム画面 stage)
		{
			m_OverlayWidgets.Add(new HitDeltaInfoWidget(stage));
			m_OverlayWidgets.Add(new HitDeltaPopupWidget(stage));
		}

		public void Reset()
		{
			m_OverlayWidgets.ForEach(x => x.Reset());
		}

		public void Draw()
		{
			if (OpenTaiko.ConfigIni.nPlayerCount == 1)
			{
				m_OverlayWidgets.ForEach(x => x.Draw());
			}
		}

		public void OnNoteHit(in HitParams hitParams)
		{
			foreach (OverlayWidget widget in m_OverlayWidgets)
			{
				widget.OnNoteHit(hitParams);
			}
		}
	}
}
