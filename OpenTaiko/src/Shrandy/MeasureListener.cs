using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class MeasureListener
	{
		private int m_RecordingMeasureStart = 0;
		private int m_RecordingMeasureEnd = 0;
		private int m_PreviousMeasure = 0;

		public event Action<int>? OnMeasureCompleted;

		public void Reset()
		{
			m_RecordingMeasureStart = 0;
			m_RecordingMeasureEnd = OpenTaiko.stageGameScreen.actTokkun.nMeasureCount;
			m_PreviousMeasure = 0;
		}

		public void Update()
		{
			int currentMeasure = OpenTaiko.stageGameScreen.actTokkun.nCurrentMeasure;
			if (currentMeasure != m_PreviousMeasure)
			{
				if (currentMeasure > m_PreviousMeasure
					&& m_PreviousMeasure >= m_RecordingMeasureStart
					&& m_PreviousMeasure <= m_RecordingMeasureEnd)
				{
					OnMeasureCompleted?.Invoke(m_PreviousMeasure);
				}
				m_PreviousMeasure = currentMeasure;
			}
		}
	}
}
