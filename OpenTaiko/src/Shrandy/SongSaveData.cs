using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class SongSaveData
	{
		public DateTime Timestamp { get; private set; }
		public string SongName { get; private set; } = "";
		public int GoodCount { get; private set; }
		public int OkayCount { get; private set; }
		public int BadCount { get; private set; }
		public int DrumRollCount { get; private set; }
	}
}
