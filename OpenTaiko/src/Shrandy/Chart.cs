namespace OpenTaiko.Shrandy
{
	internal struct Chart
	{
		public Chart(CSongListNode song, int difficulty)
		{
			Song = song;
			Difficulty = difficulty;
		}
		public CSongListNode Song { get; init; }
		public int Difficulty { get; init; }
	}
}