using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
    internal class GameOverlay
    {
        private CStage演奏ドラム画面 m_DrumDisplayStage;

        private List<int> m_HitNoteDeltas = new();
        private List<int> m_GoodNoteDeltas = new();

        private TimedConsoleMessage? m_HitDeltaMessage;

        public GameOverlay(CStage演奏ドラム画面 stage)
        {
            m_DrumDisplayStage = stage;
        }

        public void OnPerformanceInfoActivate()
        {
            m_HitNoteDeltas.Clear();
            m_GoodNoteDeltas.Clear();
        }

        public void Draw()
        {
            int screenWidth = OpenTaiko.Skin.Resolution[0];
            int screenHeight = OpenTaiko.Skin.Resolution[1];
            int x = (int)(screenWidth * 0.4f);
            int y = screenHeight - (screenHeight / 8);

            PrintNoteDeltas(x, y);
             
            m_HitDeltaMessage?.Draw();
        }

        public void OnNoteHit(CChip pChip, ENoteJudge judgeResult)
        {
            const int maxHitDeltaMs = 75;
            const int maxGoodNoteDeltaMs = 25;

            int absDelta = Math.Abs(pChip.nLag);
            if (absDelta <= maxHitDeltaMs)
            {
                m_HitNoteDeltas.Add(pChip.nLag);
                if (Math.Abs(pChip.nLag) <= maxGoodNoteDeltaMs)
                {
                    m_GoodNoteDeltas.Add(pChip.nLag);
                }
            }

            if (OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold > 0)
            {
                if (absDelta > OpenTaiko.ConfigIni.TokkunAutoSkipBackErrorThreshold || judgeResult == ENoteJudge.Miss)
                {
                    m_DrumDisplayStage.actTokkun.QueueAutoSkipBack();
                }
            }

            if (judgeResult != ENoteJudge.Miss)
            {
                UpdateHitDeltaMessage(pChip.nLag, judgeResult);
            }
        }

        private void UpdateHitDeltaMessage(int error, ENoteJudge judgeResult)
        {
            int screenWidth = OpenTaiko.Skin.Resolution[0];
            int screenHeight = OpenTaiko.Skin.Resolution[1];
            int x = (int)(screenWidth * 0.275f);
            int y = (int)(screenHeight * 0.175f);

            const int durationMs = 500;
            const float scale = 3.0f;

            FDK.Color4 color = error == 0 ? new(0.0f, 1.0f, 0.0f, 1.0f) // green
                : judgeResult <= ENoteJudge.Perfect ? new(1.0f, 1.0f, 0.0f, 1.0f) // yellow
                : new FDK.Color4(1.0f, 1.0f, 1.0f, 1.0f); // white

            string prefix = error == 0 ? "Perfect!"
                          : error > 0  ? "Late "
                                       : "Early";
            m_HitDeltaMessage = new(x, y,
                CTextConsole.EFontType.White, $"{prefix} {error:+#;-#;0}",
                durationMs,
                scale,
                color);
        }

        static double GetAbsAverage(List<int> values)
        {
            int average = 0;
            foreach (int i in values)
            {
                average += Math.Abs(i);
            }
            return values.Count > 0 ? (double)average / values.Count : 0.0;
        }

        int PrintText(int x, int y, string text)
        {
            float scale = 2.0f;
            y -= (int)(OpenTaiko.actTextConsole.fontHeight * scale);
            OpenTaiko.actTextConsole.Print(x, y, CTextConsole.EFontType.White, text, scale);
            return y;
        }

        int PrintNoteDeltas(int x, int y)
        {
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

            return y;
        }
    }
}
