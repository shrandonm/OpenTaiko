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

		public static string GetTimeSinceString(DateTime timestamp)
		{
			TimeSpan timeSince = DateTime.Now - timestamp;

			if (timeSince.TotalSeconds < 60)
				return $"<1m ago";
			else if (timeSince.TotalMinutes < 60)
				return $"{(int)timeSince.TotalMinutes}m ago";
			else if (timeSince.TotalHours < 24)
				return $"{(int)timeSince.TotalHours}h ago";
			else
				return $"{(int)timeSince.TotalDays}d ago";
		}
	}
}
