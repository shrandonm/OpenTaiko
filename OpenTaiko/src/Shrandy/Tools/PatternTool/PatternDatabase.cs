using OpenTaiko.Shrandy.Utilities;

namespace OpenTaiko.Shrandy.Tools
{
	class PatternDatabase
	{
		public List<PatternData> Patterns { get; set; } = new();
		
		public void AddPattern(PatternData pattern)
		{
			Patterns.Add(pattern);
		}
		public void ClearPatterns()
		{
			Patterns.Clear();
		}
		public void RemovePattern(PatternData pattern)
		{
			Patterns.Remove(pattern);
		}
		
		public void Save()
		{
			SaveHelper.Save("PatternDatabase.json", this);
		}
	}
}