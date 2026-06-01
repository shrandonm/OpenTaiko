using System;
using System.Collections.Generic;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SongsPerDayGraph
	{
		private SongBrowserData m_Data;

		public SongsPerDayGraph(SongBrowserData data)
		{
			m_Data = data;
		}

		public void Draw()
		{
			List<DailySongCount> dailyCounts = m_Data.GetSongsPerDay(m_Data.FilterDays);
			if (dailyCounts.Count == 0)
			{
				return;
			}

			BarGraph.Bar[] bars = BuildBars(dailyCounts);

			ImGui.SeparatorText("Songs Per Day");
			BarGraph.Draw(bars, BuildOptions());
		}

		private static BarGraph.Bar[] BuildBars(List<DailySongCount> dailyCounts)
		{
			int labelStep = Math.Max(1, dailyCounts.Count / 10);
			BarGraph.Bar[] bars = new BarGraph.Bar[dailyCounts.Count];

			for (int i = 0; i < bars.Length; i++)
			{
				DailySongCount dailySongCount = dailyCounts[i];
				bool isToday = dailySongCount.Date == DateTime.Today;
				uint color = isToday ? 0xFF20C8FF : 0xFF20C820;

				string? label = (i % labelStep == 0 || i == bars.Length - 1) ? dailySongCount.Date.ToString("M/d") : null;
				string tooltip = $"{dailySongCount.Date:yyyy-MM-dd}: {dailySongCount.Count} song{(dailySongCount.Count == 1 ? "" : "s")}";

				bars[i] = new BarGraph.Bar { Value = dailySongCount.Count, Color = color, Label = label, Tooltip = tooltip };
			}

			return bars;
		}

		private static BarGraph.Options BuildOptions()
		{
			float availableWidth = ImGui.GetContentRegionAvail().X;
			if (availableWidth < 20.0f)
			{
				availableWidth = 500.0f;
			}

			BarGraph.Options options = BarGraph.Options.Default;
			options.Width           = availableWidth;
			options.Height          = 80.0f;
			options.BarGap          = 1.0f;
			options.ShowValueLabels = false;
			options.LabelAreaH      = 18.0f;
			return options;
		}
	}
}
