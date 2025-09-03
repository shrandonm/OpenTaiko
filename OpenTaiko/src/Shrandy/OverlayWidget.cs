using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class OverlayWidget
	{
		protected CStage演奏ドラム画面 m_DrumDisplayStage;

		public OverlayWidget(CStage演奏ドラム画面 drumDisplayStage)
		{
			m_DrumDisplayStage = drumDisplayStage;
		}

		public virtual void OnNoteHit(CChip chip, ENoteJudge judgeResult)
		{
		}

		public virtual void Draw()
		{
		}

		public virtual void Reset()
		{
		}
	}
}
