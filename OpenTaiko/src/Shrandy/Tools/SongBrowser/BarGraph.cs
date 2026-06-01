using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	/// <summary>
	/// Generic vertical bar graph rendered with ImGui draw-list primitives.
	/// Visual style matches HitDistributionGraph.
	/// </summary>
	internal static class BarGraph
	{
		public struct Bar
		{
			/// <summary>Value that determines bar height.</summary>
			public float Value;
			/// <summary>ABGR color, e.g. 0xFF20C820.</summary>
			public uint Color;
			/// <summary>Label shown below the bar (may be null/empty).</summary>
			public string? Label;
			/// <summary>Tooltip shown when hovering this bar (may be null/empty).</summary>
			public string? Tooltip;
		}

		public struct Options
		{
			/// <summary>Total pixel width of the graph area.</summary>
			public float Width;
			/// <summary>Total pixel height of the bar area (labels are drawn below).</summary>
			public float Height;
			/// <summary>Gap between bars in pixels (0 = touching).</summary>
			public float BarGap;
			/// <summary>Background fill color (ABGR). 0 = transparent.</summary>
			public uint BackgroundColor;
			/// <summary>If true a faint horizontal grid line is drawn at the max value.</summary>
			public bool ShowMaxLine;
			/// <summary>Height reserved for x-axis labels. 0 = skip labels.</summary>
			public float LabelAreaH;
			/// <summary>When true, value labels are drawn on top of each bar.</summary>
			public bool ShowValueLabels;

			public static Options Default => new Options
			{
				Width           = 500.0f,
				Height          = 120.0f,
				BarGap          = 1.0f,
				BackgroundColor = 0xAA000000,
				ShowMaxLine     = true,
				LabelAreaH      = 18.0f,
				ShowValueLabels = false,
			};
		}

		/// <summary>
		/// Draws a bar graph and advances the ImGui cursor by the total height.
		/// Call this between ImGui Begin/End. Does nothing if <paramref name="bars"/> is empty.
		/// </summary>
		public static void Draw(IReadOnlyList<Bar> bars, Options options)
		{
			if (bars == null || bars.Count == 0)
			{
				return;
			}

			float maxValue = ComputeMaxValue(bars);
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Vector2 cursor = ImGui.GetCursorScreenPos();
			float barWidth = options.Width / bars.Count;

			DrawBackground(drawList, cursor, options);
			DrawBars(drawList, cursor, bars, options, maxValue, barWidth);
			DrawMaxValueGuideLine(drawList, cursor, options);
			DrawXAxisLabels(drawList, cursor, bars, options, barWidth);
			DrawHoverHighlightAndTooltip(drawList, cursor, bars, options, barWidth);

			float totalHeight = options.Height + options.LabelAreaH;
			ImGui.Dummy(new Vector2(options.Width, totalHeight));
		}

		private static float ComputeMaxValue(IReadOnlyList<Bar> bars)
		{
			float maxValue = 0.0f;
			foreach (Bar bar in bars)
			{
				if (bar.Value > maxValue)
				{
					maxValue = bar.Value;
				}
			}

			if (maxValue <= 0.0f)
			{
				maxValue = 1.0f;
			}

			return maxValue;
		}

		private static void DrawBackground(ImDrawListPtr drawList, Vector2 cursor, Options options)
		{
			if (options.BackgroundColor == 0)
			{
				return;
			}

			drawList.AddRectFilled(
				cursor,
				new Vector2(cursor.X + options.Width, cursor.Y + options.Height),
				options.BackgroundColor);
		}

		private static void DrawBars(ImDrawListPtr drawList, Vector2 cursor, IReadOnlyList<Bar> bars, Options options, float maxValue, float barWidth)
		{
			for (int i = 0; i < bars.Count; i++)
			{
				Bar bar = bars[i];
				if (bar.Value <= 0.0f)
				{
					continue;
				}

				float normalizedValue = bar.Value / maxValue;
				float barHeight = options.Height * normalizedValue;

				float left   = cursor.X + i * barWidth + options.BarGap * 0.5f;
				float right  = cursor.X + (i + 1) * barWidth - options.BarGap * 0.5f;
				float top    = cursor.Y + options.Height - barHeight;
				float bottom = cursor.Y + options.Height;

				drawList.AddRectFilled(new Vector2(left, top), new Vector2(right, bottom), bar.Color);

				if (options.ShowValueLabels)
				{
					DrawValueLabel(drawList, cursor, bar, left, right, top);
				}
			}
		}

		private static void DrawValueLabel(ImDrawListPtr drawList, Vector2 cursor, Bar bar, float barLeft, float barRight, float barTop)
		{
			string valueString = bar.Value % 1.0f == 0.0f
				? ((int)bar.Value).ToString()
				: bar.Value.ToString("F1");

			Vector2 textSize = ImGui.CalcTextSize(valueString);
			float labelX = (barLeft + barRight) * 0.5f - textSize.X * 0.5f;
			float labelY = barTop - textSize.Y - 2.0f;
			if (labelY < cursor.Y)
			{
				labelY = cursor.Y + 2.0f;
			}

			drawList.AddText(new Vector2(labelX, labelY), 0xFFFFFFFF, valueString);
		}

		private static void DrawMaxValueGuideLine(ImDrawListPtr drawList, Vector2 cursor, Options options)
		{
			if (!options.ShowMaxLine)
			{
				return;
			}

			float lineY = cursor.Y + 1.0f;
			drawList.AddLine(
				new Vector2(cursor.X, lineY),
				new Vector2(cursor.X + options.Width, lineY),
				0x33FFFFFF);
		}

		private static void DrawXAxisLabels(ImDrawListPtr drawList, Vector2 cursor, IReadOnlyList<Bar> bars, Options options, float barWidth)
		{
			if (options.LabelAreaH <= 0.0f)
			{
				return;
			}

			for (int i = 0; i < bars.Count; i++)
			{
				string? label = bars[i].Label;
				if (string.IsNullOrEmpty(label))
				{
					continue;
				}

				float barLeft  = cursor.X + i * barWidth;
				float barRight = cursor.X + (i + 1) * barWidth;
				Vector2 labelTextSize = ImGui.CalcTextSize(label);
				float centerX = (barLeft + barRight) * 0.5f - labelTextSize.X * 0.5f;

				drawList.AddText(
					new Vector2(centerX, cursor.Y + options.Height + 2.0f),
					0xFFAAAAAA,
					label);
			}
		}

		private static void DrawHoverHighlightAndTooltip(ImDrawListPtr drawList, Vector2 cursor, IReadOnlyList<Bar> bars, Options options, float barWidth)
		{
			Vector2 graphMin = cursor;
			Vector2 graphMax = new Vector2(cursor.X + options.Width, cursor.Y + options.Height);
			if (!ImGui.IsMouseHoveringRect(graphMin, graphMax))
			{
				return;
			}

			int hoveredBarIndex = (int)((ImGui.GetMousePos().X - cursor.X) / barWidth);
			if (hoveredBarIndex < 0 || hoveredBarIndex >= bars.Count)
			{
				return;
			}

			float highlightLeft  = cursor.X + hoveredBarIndex * barWidth;
			float highlightRight = highlightLeft + barWidth;
			drawList.AddRectFilled(
				new Vector2(highlightLeft, cursor.Y),
				new Vector2(highlightRight, cursor.Y + options.Height),
				0x33FFFFFF);

			string? tooltip = bars[hoveredBarIndex].Tooltip;
			if (!string.IsNullOrEmpty(tooltip))
			{
				ImGui.SetTooltip(tooltip);
			}
		}
	}
}
