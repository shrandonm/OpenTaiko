using System;
using System.Diagnostics;

namespace OpenTaiko.Shrandy
{
	internal class MicroStopwatch : Stopwatch
	{
		readonly double MicroSecPerTick = 1000000D / Frequency;

		public long ElapsedMicroseconds
		{
			get
			{
				return (long)(ElapsedTicks * MicroSecPerTick);
			}
		}

		public MicroStopwatch()
		{
			if (!IsHighResolution)
			{
				throw new Exception("High resolution timer not available");
			}
		}
	}
}
