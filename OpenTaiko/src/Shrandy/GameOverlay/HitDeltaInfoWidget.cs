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

		private struct HitInfo
		{
			public int Delta;
			public Hand Hand;
			public ENoteJudge JudgeResult;
		}

		private struct Summary
		{
			public float AverageGoodError;
			public float AverageLeftHandGoodError;
			public float AverageRightHandGoodError;
			public float AverageHitDelta;
			public int EarlyHits;
			public int LateHits;
		}

		private List<HitInfo> m_NoteHistory = new();

		public override void Reset()
		{
			base.Reset();
			m_NoteHistory.Clear();
		}

		public override void OnNoteHit(in HitParams hitParams)
		{
			base.OnNoteHit(hitParams);
			const int maxHitDeltaMs = 75;

			if (hitParams.Chip != null)
			{
				int absDelta = Math.Abs(hitParams.Chip.nLag);
				if (absDelta <= maxHitDeltaMs)
				{
					m_NoteHistory.Add(new HitInfo()
					{
						Delta = hitParams.Chip.nLag,
						Hand = hitParams.Hand,
						JudgeResult = hitParams.JudgeResult
					});
				}
			}
		}

		private Summary CalculateSummary(List<HitInfo> hits)
		{
			Summary summary = new();
			int goodCount = 0;
			int leftGoods = 0;
			int rightGoods = 0;

			foreach (HitInfo hit in hits)
			{
				int error = Math.Abs(hit.Delta);
				const int includeHitThreshold = 75;
				if (error <= includeHitThreshold)
				{
					goodCount++;
					summary.AverageGoodError += error;
					if (hit.Hand == Hand.Left)
					{
						leftGoods++;
						summary.AverageLeftHandGoodError += error;
					}
					else
					{
						rightGoods++;
						summary.AverageRightHandGoodError += error;
					}
				}
				summary.AverageHitDelta += hit.Delta;

				if (hit.Delta > 25)
				{
					summary.LateHits++;
				}
				else if (hit.Delta < -25)
				{
					summary.EarlyHits++;
				}
			}

			if (goodCount > 0)
			{
				summary.AverageGoodError /= goodCount;
			}
			if (leftGoods > 0)
			{
				summary.AverageLeftHandGoodError /= leftGoods;
			}
			if (rightGoods > 0)
			{
				summary.AverageRightHandGoodError /= rightGoods;
			}
			if (hits.Count > 0)
			{
				summary.AverageHitDelta /= hits.Count;
			}

			return summary;
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

			if (m_NoteHistory.Count > 0)
			{
				Summary summary = CalculateSummary(m_NoteHistory);
				y = PrintText(x, y, $"Hit Count: {m_NoteHistory.Count}");
				y = PrintText(x, y, $"Early Okays: {summary.EarlyHits}");
				y = PrintText(x, y, $"Late Okays: {summary.LateHits}");
				y = PrintText(x, y, $"Sync: {summary.AverageHitDelta:F2} ms");
				y = PrintText(x, y, $"Average Left Error: {summary.AverageLeftHandGoodError:F2} ms");
				y = PrintText(x, y, $"Average Right Error: {summary.AverageRightHandGoodError:F2} ms");
				y = PrintText(x, y, $"Average Error: {summary.AverageGoodError:F2} ms");
			}
		}
	}
}
