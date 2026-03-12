using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal static class StringHelpers
	{
		public static float GetPercent(int amount, int total)
		{
			return total > 0 ? amount / (float)total : 0.0f;
		}

		public static string GetPercentString(int amount, int total)
		{
			return GetPercentString(GetPercent(amount, total));
		}

		public static string GetPercentString(float percent)
		{
			return $"{percent * 100.0f:F2}%";
		}

	}
}
