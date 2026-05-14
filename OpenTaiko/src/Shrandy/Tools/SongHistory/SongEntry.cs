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
		public int ChartLevel { get; set; }
		public double BaseBpm { get; set; }
		public double MinBpm { get; set; }
		public double MaxBpm { get; set; }
		public string RandomMod { get; set; } = "None";
		public int SongSpeed { get; set; } = CConfigIni.DefaultSongSpeed;
		public int Judgement { get; set; } = 2;
		public float AvgHitError { get; set; }
		public float AvgSync { get; set; }
		public float AvgLeftHandError { get; set; }
		public float AvgRightHandError { get; set; }
		public int LeftHandOkays { get; set; }
		public int RightHandOkays { get; set; }
		public bool UsedFadingNote { get; set; }
	}
}
