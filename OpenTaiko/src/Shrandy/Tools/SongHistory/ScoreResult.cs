namespace OpenTaiko.Shrandy
{
	/// <summary>The headline numbers of a single play, used to store a score re-calculated under different rules.</summary>
	internal class ScoreResult
	{
		public int Score { get; set; }
		public int ScoreRank { get; set; }
		public int Goods { get; set; }
		public int Okays { get; set; }
		public int Bads { get; set; }
		public int MaxCombo { get; set; }
		public int Rolls { get; set; }
		public float AvgHitError { get; set; }
		/// <summary>0 = no clear, 1 = pass, 2 = FC, 3 = DFC</summary>
		public int Crown { get; set; }

		[System.Text.Json.Serialization.JsonIgnore]
		public int TotalNotes => Goods + Okays + Bads;
		[System.Text.Json.Serialization.JsonIgnore]
		public float GoodPercent => StringHelpers.GetPercent(Goods, TotalNotes);
	}
}
