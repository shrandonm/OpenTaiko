using OpenTaiko.Shrandy.Utilities;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternDatabase
	{
		public const string FillerOddTitle = "Filler_Odd";
		public const string FillerEvenTitle = "Filler_Even";
		private const string FileName = "pattern_database.json";

		public static bool IsBuiltIn(PatternData p)
		{
			return p.Title == FillerOddTitle || p.Title == FillerEvenTitle;
		}

		public List<PatternData> Patterns { get; set; } = new();
		public List<DrillData> Drills { get; set; } = new();
		
		public void RemovePattern(PatternData pattern)
		{
			if (IsBuiltIn(pattern))
			{
				return;
			}
			Patterns.Remove(pattern);
		}

		/// <summary>
		/// After deserialization, pattern objects inside drills are separate instances.
		/// This method replaces them with the canonical instances from Patterns (matched by title)
		/// so that in-memory renames propagate automatically to all drill references.
		/// </summary>
		public void Reconcile()
		{
			EnsureBuiltInPatterns();

			Dictionary<string, PatternData> patternMap = Patterns
				.GroupBy(p => p.Title)
				.ToDictionary(g => g.Key, g => g.First());

			foreach (DrillData drill in Drills)
			{
				ReconcileWeightList(drill.Patterns, patternMap);
				ReconcileWeightList(drill.FillerPatterns, patternMap);
			}
		}

		private void EnsureBuiltInPatterns()
		{
			if (!Patterns.Any(p => p.Title == FillerEvenTitle))
			{
				Patterns.Insert(0, new PatternData { Title = FillerEvenTitle, TJA = "1111" });
			}
			if (!Patterns.Any(p => p.Title == FillerOddTitle))
			{
				Patterns.Insert(0, new PatternData { Title = FillerOddTitle, TJA = "1011" });
			}
		}

		/// <summary>
		/// Propagates a pattern title rename to any drill pattern copies that still carry the old title.
		/// Handles edge cases where reconciliation was incomplete.
		/// </summary>
		public void PropagatePatternRename(string oldTitle, string newTitle)
		{
			foreach (DrillData drill in Drills)
			{
				PropagateRenameInWeightList(drill.Patterns, oldTitle, newTitle);
				PropagateRenameInWeightList(drill.FillerPatterns, oldTitle, newTitle);
			}
		}

		private static void ReconcileWeightList(List<DrillData.PatternWeight> list, Dictionary<string, PatternData> patternMap)
		{
			for (int i = 0; i < list.Count; i++)
			{
				DrillData.PatternWeight patternWeight = list[i];
				if (patternMap.TryGetValue(patternWeight.Pattern.Title, out PatternData? canonical))
				{
					list[i] = new DrillData.PatternWeight { Pattern = canonical!, Weight = patternWeight.Weight };
				}
			}
		}

		private static void PropagateRenameInWeightList(List<DrillData.PatternWeight> list, string oldTitle, string newTitle)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Pattern.Title == oldTitle)
				{
					list[i].Pattern.Title = newTitle;
				}
			}
		}

		public void Save()
		{
			SaveHelper.Save(FileName, this);
		}
		
		public static PatternDatabase LoadOrCreate()
		{
			PatternDatabase database = SaveHelper.LoadOrCreate<PatternDatabase>(FileName);
			database.Reconcile();
			return database;
		}
	}
}