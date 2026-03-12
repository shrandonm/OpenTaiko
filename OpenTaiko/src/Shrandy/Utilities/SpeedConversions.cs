using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy.Utilities
{
	internal static class SpeedConversions
	{
		public static float GetActualScrollSpeed(int scrollSpeed)
		{
			return (scrollSpeed + 1) * 0.1f;
		}

		public static int GetScrollSpeedIntValue(float actualScrollSpeed)
		{
			return (int)Math.Round(actualScrollSpeed * 10.0f) - 1;
		}
	}
}
