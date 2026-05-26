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
		public List<PatternWeight> FillerPatterns { get; set; } = new();
		public int MinFillerPatternFrequency { get; set; } = 4;
		public int MaxFillerPatternFrequency { get; set; } = 8;
	}
}