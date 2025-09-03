using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class HitDeltaInfoWidget : OverlayWidget
	{
		public HitDeltaInfoWidget(CStage演奏ドラム画面 drumDisplayStage)
			: base(drumDisplayStage)
		{
		}

		private List<int> m_HitNoteDeltas = new();
		private List<int> m_GoodNoteDeltas = new();

		public override void Reset()
		{
			base.Reset();
			m_HitNoteDeltas.Clear();
			m_GoodNoteDeltas.Clear();
		}

		public override void OnNoteHit(CChip chip, ENoteJudge judgeResult)
		{
			base.OnNoteHit(chip, judgeResult);
			const int maxHitDeltaMs = 75;
			const int maxGoodNoteDeltaMs = 25;

			int absDelta = Math.Abs(chip.nLag);
			if (absDelta <= maxHitDeltaMs)
			{
				m_HitNoteDeltas.Add(chip.nLag);
				if (Math.Abs(chip.nLag) <= maxGoodNoteDeltaMs)
				{
					m_GoodNoteDeltas.Add(chip.nLag);
				}
			}
		}

		private double GetAbsAverage(List<int> values)
		{
			int average = 0;
			foreach (int i in values)
			{
				average += Math.Abs(i);
			}
			return values.Count > 0 ? (double)average / values.Count : 0.0;
		}

		private int PrintText(int x, int y, string text)
		{
			float scale = 2.0f;
			y -= (int)(OpenTaiko.actTextConsole.fontHeight * scale);
			OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, text, scale);
			return y;
		}

		public override void Draw()
		{
			base.Draw();

			int screenWidth = OpenTaiko.Skin.Resolution[0];
			int screenHeight = OpenTaiko.Skin.Resolution[1];
			int x = (int)(screenWidth * 0.4f);
			int y = screenHeight - (screenHeight / 8);

			y = PrintText(x, y, $"Hit Count: {m_HitNoteDeltas.Count}");
			y = PrintText(x, y, $"Early hits: {m_HitNoteDeltas.Count(v => v < -25)}");
			y = PrintText(x, y, $"Late hits: {m_HitNoteDeltas.Count(v => v > 25)}");

			if (m_HitNoteDeltas.Count > 0)
			{
				y = PrintText(x, y,
					$"Hit Average Delta: {m_HitNoteDeltas.Average():F2} ms");
				y = PrintText(x, y,
					$"Hit Average Error: {GetAbsAverage(m_HitNoteDeltas):F2} ms");
			}

			if (m_GoodNoteDeltas.Count > 0)
			{
				y = PrintText(x, y,
					$"Good Average Delta: {m_GoodNoteDeltas.Average():F2} ms");
				y = PrintText(x, y,
					$"Good Average Error: {GetAbsAverage(m_GoodNoteDeltas):F2} ms");
			}
		}
	}
}
