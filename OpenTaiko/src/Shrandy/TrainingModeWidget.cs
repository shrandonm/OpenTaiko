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

		public override void OnNoteHit(CChip chip, ENoteJudge judgeResult)
		{
			base.OnNoteHit(chip, judgeResult);

			int absDelta = Math.Abs(chip.nLag);
			if (OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold > 0)
			{
				if (absDelta > OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold || judgeResult == ENoteJudge.Miss)
				{
					m_DrumDisplayStage.actTokkun.QueueAutoSkipBack();
				}
			}
		}
	}
}
