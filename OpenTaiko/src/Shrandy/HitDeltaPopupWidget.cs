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
		private TimedConsoleMessage? m_Bar;

		public override void Draw()
		{
			base.Draw();
			m_Message?.Draw();
			m_Bar?.Draw();
		}

		public override void OnNoteHit(in HitParams hitParams)
		{
			base.OnNoteHit(hitParams);
			if (hitParams.JudgeResult != ENoteJudge.Miss)
			{
				int screenWidth = OpenTaiko.Skin.Resolution[0];
				int screenHeight = OpenTaiko.Skin.Resolution[1];
				int textX = (int)(screenWidth * 0.275f);
				int barX = (int)(screenWidth * 0.225f);
				int textY = (int)(screenHeight * 0.175f);
				int barY = textY + 64;

				const int durationMs = 500;
				const float textScale = 3.0f;
				const float barScale = 2.0f;
				int error = hitParams.Chip.nLag;

				FDK.Color4 color = error == 0 ? new(0.0f, 1.0f, 0.0f, 1.0f) // green
					: ShrandyExtension.IsGood(hitParams.JudgeResult) ? new(1.0f, 0.6f, 0.2f, 1.0f) // orange
					: new FDK.Color4(1.0f, 1.0f, 1.0f, 1.0f); // white

				string prefix = error == 0 ? "Perfect!"
							  : error > 0  ? "Late "
										   : "Early";

				const int maxErrorBars = 10;
				StringBuilder errorBars = new(maxErrorBars);
				for (int i = maxErrorBars; i >= -maxErrorBars; --i)
				{
					bool addBar = (error < 0 && i < 0 && i >= error) || (error > 0 && i > 0 && i <= error);
					errorBars.Append(addBar ? '|' : ' ');
				}

				m_Message = new(textX, textY,
					CTextConsole.EFontType.White, $"{prefix} {error:+#;-#;0}",
					durationMs,
					textScale,
					color);

				m_Bar = new(barX, barY,
					CTextConsole.EFontType.White, $"{errorBars.ToString()}",
					durationMs,
					barScale,
					color);
			}
		}
	}
}
