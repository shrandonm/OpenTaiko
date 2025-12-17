using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy.Utilities
{
	internal static class ColourHelper
	{
		public static uint ImGuiU32FromRRGGBB(string hex, byte a = 255)
		{
			hex = hex.Trim().TrimStart('#');
			byte r = Convert.ToByte(hex.Substring(0, 2), 16);
			byte g = Convert.ToByte(hex.Substring(2, 2), 16);
			byte b = Convert.ToByte(hex.Substring(4, 2), 16);
			return (uint)(r | (g << 8) | (b << 16) | (a << 24));
		}

		public static uint GetDonImGuiColour()
		{
			return ImGuiU32FromRRGGBB("F64626");
		}

		public static uint GetKaImGuiColour()
		{
			return ImGuiU32FromRRGGBB("50C2C3");
		}
	}
}
