namespace OpenTaiko.Shrandy
{
	internal class SongTag
	{
		public string Name { get; set; } = "";
	}

	internal class SongHistorySaveData
	{
		public List<SongEntry> SongEntries { get; set; } = new();
	}
}