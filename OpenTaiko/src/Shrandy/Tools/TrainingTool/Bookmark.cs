using System;
using System.Collections.Generic;
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
	}
}
