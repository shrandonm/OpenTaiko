using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy.Tools
{
	internal struct Bookmark
	{
		public string Name { get; set; }
		public int StartMeasure { get; set; }
		public int EndMeasure { get; set; }
		
		public static bool operator ==(Bookmark left, Bookmark right)
		{
			return left.Name == right.Name && left.StartMeasure == right.StartMeasure && left.EndMeasure == right.EndMeasure;
		}

		public static bool operator !=(Bookmark left, Bookmark right)
		{
			return !(left == right);
		}

		public override bool Equals(object? obj)
		{
			if (obj is Bookmark other)
			{
				return this == other;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, StartMeasure, EndMeasure);
		}
	}
}
