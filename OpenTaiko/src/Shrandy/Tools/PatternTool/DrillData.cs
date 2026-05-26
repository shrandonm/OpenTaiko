namespace OpenTaiko.Shrandy.Tools
{
	internal class DrillData
	{
		public struct PatternWeight
		{
			public required PatternData Pattern { get; set; }
			public int Weight { get; set; }
		}
		
		public string Title { get; set; } = "";
		public List<PatternWeight> Patterns { get; set; } = new();
	}
}