using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenTaiko.Shrandy
{
	internal class SaveData
	{
		public List<SongSaveData> SongSaveData { get; private set; } = new();
	}
}
