namespace OpenTaiko.Shrandy
{
	internal class SongEntry
	{
		public string SongTitle { get; set; } = "";
		public string Difficulty { get; set; } = "";
		public DateTime Timestamp { get; set; } = DateTime.Now;
		public int Score { get; set; }
		public int Goods { get; set; }
		public int Okays { get; set; }
		public int Bads { get; set; }
		public int Rolls { get; set; }
		public int MaxCombo { get; set; }
		public int DurationMs { get; set; }
		public int ScoreRank { get; set; }
	}
}
