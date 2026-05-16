using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class HitDeltaPopupWidget : OverlayWidget
	{
		public HitDeltaPopupWidget(CStage演奏ドラム画面 drumDisplayStage)
			: base(drumDisplayStage)
		{
		}

		private TimedConsoleMessage? m_Message;

		public override void Draw()
		{
			base.Draw();
			m_Message?.Draw();
		}

		public override void OnNoteHit(in HitParams hitParams)
		{
			base.OnNoteHit(hitParams);
			if (hitParams.Chip != null && hitParams.JudgeResult != ENoteJudge.Miss)
			{
				int screenWidth = OpenTaiko.Skin.Resolution[0];
				int screenHeight = OpenTaiko.Skin.Resolution[1];
				int textX = (int)(screenWidth * 0.3f);
				int textY = (int)(screenHeight * 0.175f);

				const int durationMs = 500;
				const float textScale = 1.5f;
				int delta = hitParams.Chip.nLag;

				const int errorThresholdMs = 25;
				bool isLate = delta > errorThresholdMs;
				bool isEarly = delta < -errorThresholdMs;
				string message = isLate ? "Late"
							 : isEarly ? "Early"
							 	: "";

				FDK.Color4 color = isLate ? new FDK.Color4(1.0f, 0.75f, 0.0f, 1.0f) // Orange
							 : isEarly ? new FDK.Color4(0.0f, 0.75f, 1.0f, 1.0f) // Blue
							 : new FDK.Color4(1.0f, 1.0f, 1.0f, 1.0f); // White
							 
				if (isLate || isEarly)
				{
					m_Message = new(textX, textY,
					CTextConsole.EFontType.White, message,
					durationMs,
					textScale,
					color);
				}
			}
		}
	}
}
