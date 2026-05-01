using System.Collections.Generic;

namespace OpenTaiko.Shrandy
{
	internal class SongTagsSaveData
	{
		public Dictionary<string, List<SongTag>> SongTags { get; set; } = new();
	}
}
