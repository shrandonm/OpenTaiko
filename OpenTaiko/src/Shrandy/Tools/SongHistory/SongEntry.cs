namespace OpenTaiko.Shrandy
{
	using System.Text.Json.Serialization;

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
		public int EarlyHits { get; set; }
		public int LateHits { get; set; }
		public int DdrScore { get; set; }
		public string DdrGrade { get; set; } = "";
		public string DdrComboType { get; set; } = "";
		public int DdrMarvelousCount { get; set; }
		public int DdrPerfectCount { get; set; }
		public int DdrGreatPlusCount { get; set; }
		
		[JsonIgnore]
		public int TotalNotes => Goods + Okays + Bads;
		[JsonIgnore]
		public string GoodPercentString => StringHelpers.GetPercentString(Goods, TotalNotes);
		/// <summary>0 = no clear, 1 = pass, 2 = FC, 3 = DFC</summary>
		public int Crown { get; set; }
		/// <summary>Like Crown but infers pass/FC/DFC from note data for legacy entries where Crown was not stored.</summary>
		[JsonIgnore]
		public int EffectiveCrown
		{
			get
			{
				if (Crown > 0 || TotalNotes == 0)
					return Crown;
				if (Bads == 0 && Okays == 0)
					return 3;
				if (Bads == 0)
					return 2;
				return 1;
			}
		}
	}
}
