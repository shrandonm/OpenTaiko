using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	/// <summary>
	/// Renders a single-bar TJA pattern as a row of colored circles over a beat grid.
	/// Don notes (1, 3) are drawn in red; Ka notes (2, 4) are drawn in blue.
	/// The bar is subdivided evenly by the number of digits in the TJA string.
	/// Note centers align exactly with quarter/eighth beat grid lines.
	/// </summary>
	internal static class PatternBarVisualizer
	{
		// Note geometry — outer outline then white ring then fill, drawn back-to-front.
		public  const float NoteRadius           = 6.0f;
		private const float OuterOutlineRadius   = NoteRadius + 2.0f;
		private const float InnerOutlineRadius   = NoteRadius + 1.0f;

		// Default row height: full circle diameter plus 8px breathing room.
		public const float DefaultHeight = OuterOutlineRadius * 2.0f + 8.0f;

		// Shared fixed width used for all inline bar previews across every UI surface.
		public const float PreviewWidth = 200.0f;

		// Note colors
		private static readonly uint DonColor           = ImGui.ColorConvertFloat4ToU32(new Vector4(1.000f, 0.259f, 0.259f, 1.0f));
		private static readonly uint KaColor            = ImGui.ColorConvertFloat4ToU32(new Vector4(0.263f, 0.784f, 1.000f, 1.0f));
		private static readonly uint NoteOuterRingColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.00f, 0.00f, 0.00f, 1.0f));
		private static readonly uint NoteInnerRingColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.00f, 1.00f, 1.00f, 1.0f));

		// Background and grid colors
		private static readonly uint BackgroundColor  = ImGui.ColorConvertFloat4ToU32(new Vector4(0.502f, 0.502f, 0.502f, 1.0f));
		private static readonly uint BarLineColor     = ImGui.ColorConvertFloat4ToU32(new Vector4(1.00f,  1.00f,  1.00f,  1.0f));
		private static readonly uint QuarterLineColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.55f,  0.55f,  0.55f,  1.0f));
		private static readonly uint EighthLineColor  = ImGui.ColorConvertFloat4ToU32(new Vector4(0.78f,  0.78f,  0.78f,  1.0f));

		/// <summary>
		/// Draws the pattern visualization inline at the current ImGui cursor position,
		/// then advances the cursor by reserving space with Dummy.
		/// </summary>
		public static void DrawInline(string tja, float width, float height)
		{
			Vector2 position = ImGui.GetCursorScreenPos();
			Draw(ImGui.GetWindowDrawList(), position, new Vector2(width, height), tja);
			ImGui.Dummy(new Vector2(width, height));
		}

		/// <summary>
		/// Draws the pattern visualization onto the given draw list inside the specified bounds.
		/// </summary>
		public static void Draw(ImDrawListPtr drawList, Vector2 position, Vector2 size, string tja)
		{
			drawList.AddRectFilled(position, new Vector2(position.X + size.X, position.Y + size.Y), BackgroundColor);

			int digitCount = 0;
			foreach (char c in tja)
			{
				if (c >= '0' && c <= '9')
				{
					digitCount++;
				}
			}

			if (digitCount == 0)
			{
				return;
			}

			const float BarHorizontalPadding = OuterOutlineRadius + 8.0f;
			float barLeft  = position.X + BarHorizontalPadding;
			float barRight = position.X + size.X - BarHorizontalPadding;
			float barWidth = barRight - barLeft;

			if (barWidth <= 0.0f)
			{
				return;
			}

			float centerY = position.Y + size.Y * 0.5f;
			float topY    = position.Y;
			float bottomY = position.Y + size.Y;

			DrawGridLines(drawList, barLeft, barRight, barWidth, topY, bottomY);
			DrawNotes(drawList, tja, digitCount, barLeft, barWidth, centerY);
		}

		/// <summary>
		/// Draws the beat grid: 8th note lines, then 4th note lines on top, then bar start/end on top.
		/// Each layer overdraws the previous so colours read correctly.
		/// </summary>
		private static void DrawGridLines(
			ImDrawListPtr drawList,
			float barLeft, float barRight, float barWidth,
			float topY, float bottomY)
		{
			for (int lineIndex = 0; lineIndex < 8; lineIndex++)
			{
				float lineX = barLeft + (float)lineIndex / 8.0f * barWidth;
				drawList.AddLine(new Vector2(lineX, topY), new Vector2(lineX, bottomY), EighthLineColor);
			}

			for (int lineIndex = 0; lineIndex < 4; lineIndex++)
			{
				float lineX = barLeft + (float)lineIndex / 4.0f * barWidth;
				drawList.AddLine(new Vector2(lineX, topY), new Vector2(lineX, bottomY), QuarterLineColor);
			}

			drawList.AddLine(new Vector2(barLeft,  topY), new Vector2(barLeft,  bottomY), BarLineColor, 1.5f);
			drawList.AddLine(new Vector2(barRight, topY), new Vector2(barRight, bottomY), BarLineColor, 1.5f);
		}

		/// <summary>
		/// Draws each non-zero note as a filled circle with a black outer ring and white inner ring.
		/// Note positions align with beat grid lines: note i is at i/digitCount of the bar width.
		/// </summary>
		private static void DrawNotes(
			ImDrawListPtr drawList,
			string tja, int digitCount,
			float barLeft, float barWidth,
			float centerY)
		{
			// Iterate in reverse (back to front) so that earlier notes (leftmost) are
			// drawn last and appear on top when notes overlap.
			int digitIndex = digitCount - 1;
			for (int charIndex = tja.Length - 1; charIndex >= 0; charIndex--)
			{
				char c = tja[charIndex];
				if (c < '0' || c > '9')
				{
					continue;
				}

				if (c != '0')
				{
					float centerX  = barLeft + (float)digitIndex / digitCount * barWidth;
					uint  noteColor = (c == '2' || c == '4') ? KaColor : DonColor;

					drawList.AddCircleFilled(new Vector2(centerX, centerY), OuterOutlineRadius, NoteOuterRingColor);
					drawList.AddCircleFilled(new Vector2(centerX, centerY), InnerOutlineRadius, NoteInnerRingColor);
					drawList.AddCircleFilled(new Vector2(centerX, centerY), NoteRadius,         noteColor);
				}

				digitIndex--;
			}
		}
	}
}
