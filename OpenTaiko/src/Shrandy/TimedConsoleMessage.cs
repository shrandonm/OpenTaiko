using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
    class TimedConsoleMessage
    {
        public TimedConsoleMessage(int x, int y, CTextConsole.EFontType font, string message, int durationMs, float scale, FDK.Color4 color)
        {
            m_X = x;
            m_Y = y;
            m_Font = font;
            m_Message = message;
            m_DurationMs = durationMs;
            m_StartTimeMs = OpenTaiko.TimeMs;
            m_Color = color;
            m_Scale = scale;
        }

        public void Draw()
        {
            if (!IsExpired())
            {
                OpenTaiko.actTextConsole.Print(m_X, m_Y, m_Font, m_Message, m_Scale, m_Color);
            }
        }

        public bool IsExpired()
        {
            return OpenTaiko.TimeMs >= m_StartTimeMs + m_DurationMs;
        }

        private int m_X;
        private int m_Y;
        private CTextConsole.EFontType m_Font;
        private string m_Message;
        private int m_DurationMs;
        private long m_StartTimeMs;
        private FDK.Color4 m_Color;
        private float m_Scale = 1.0f;
    }
}
