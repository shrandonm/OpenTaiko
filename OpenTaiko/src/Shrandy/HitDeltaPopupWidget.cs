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

		private FDK.Color4 CalculateColor(int error)
		{
			if (error == 0)
			{
				return new FDK.Color4(0.0f, 1.0f, 1.0f, 1.0f);
			}

			const float maxErrorColor = 15.0f;
			float colorInterpValue = Math.Clamp(error / maxErrorColor, 0.0f, 1.0f);

			float r, g;
			if (colorInterpValue < 0.5f)
			{
				r = colorInterpValue / 0.5f;
				g = 1.0f;
			}
			else
			{
				r = 1.0f;
				g = 1.0f - ((colorInterpValue - 0.5f) / 0.5f);
			}

			return new FDK.Color4(r, g, 0.0f, 1.0f);
		}

		public override void OnNoteHit(in HitParams hitParams)
		{
			base.OnNoteHit(hitParams);
			if (hitParams.JudgeResult != ENoteJudge.Miss)
			{
				int screenWidth = OpenTaiko.Skin.Resolution[0];
				int screenHeight = OpenTaiko.Skin.Resolution[1];
				int textX = (int)(screenWidth * 0.275f);
				int textY = (int)(screenHeight * 0.175f);
				int barX = (int)(screenWidth * 0.0575f);
				int barY = textY + 64;

				const int durationMs = 500;
				const float textScale = 3.0f;
				const float barScale = 2.5f;
				int delta = hitParams.Chip.nLag;

				string prefix = delta == 0 ? "Perfect!"
							  : delta > 0  ? "Late "
										   : "Early";

				const int maxErrorBars = 25;
				StringBuilder errorBars = new(maxErrorBars);
				for (int i = maxErrorBars; i >= -maxErrorBars; --i)
				{
					bool addBar = (delta < 0 && i < 0 && i >= delta) || (delta > 0 && i > 0 && i <= delta);
					errorBars.Append(addBar ? '|' : ' ');
				}

				FDK.Color4 color = CalculateColor(Math.Abs(delta));
				m_Message = new(textX, textY,
					CTextConsole.EFontType.White, $"{prefix} {delta:+#;-#;0}",
					durationMs,
					textScale,
					color);

				// m_Bar = new(barX, barY,
					// CTextConsole.EFontType.White, $"{errorBars.ToString()}",
					// durationMs,
					// barScale,
					// color);
			}
		}
	}
}
