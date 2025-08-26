using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
    static class ShrandyExtension
    {
        public static void OnNoteHit(CChip chip, ENoteJudge judgeResult)
        {
            OpenTaiko.stageGameScreen.m_ShrandyGameOverlay.OnNoteHit(chip, judgeResult);
        }

        public static void OnPerformanceInfoActivate()
        {
            OpenTaiko.stageGameScreen.m_ShrandyGameOverlay.OnPerformanceInfoActivate();
        }
    }
}
