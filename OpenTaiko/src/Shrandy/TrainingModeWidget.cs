using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class TrainingModeWidget : OverlayWidget
	{
		public TrainingModeWidget(CStage演奏ドラム画面 drumDisplayStage)
			: base(drumDisplayStage)
		{
		}

		public override void OnNoteHit(in HitParams hitParams)
		{
			base.OnNoteHit(hitParams);

			int absDelta = Math.Abs(hitParams.Chip.nLag);
			if (OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold > 0)
			{
				if (absDelta > OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold || hitParams.JudgeResult == ENoteJudge.Miss)
				{
					m_DrumDisplayStage.actTokkun.QueueAutoSkipBack();
				}
			}
		}
	}
}
