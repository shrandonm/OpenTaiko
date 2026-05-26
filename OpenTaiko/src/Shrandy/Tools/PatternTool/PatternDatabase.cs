using OpenTaiko.Shrandy.Utilities;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternDatabase
	{
		public const string FillerOddTitle = "Filler_Odd";
		public const string FillerEvenTitle = "Filler_Even";

		public static bool IsBuiltIn(PatternData p)
		{
			return p.Title == FillerOddTitle || p.Title == FillerEvenTitle;
		}

		public List<PatternData> Patterns { get; set; } = new();
		public List<DrillData> Drills { get; set; } = new();
		
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
			if (IsBuiltIn(pattern))
			{
				return;
			}
			Patterns.Remove(pattern);
		}

		public void AddDrill(DrillData drill)
		{
			Drills.Add(drill);
		}
		public void RemoveDrill(DrillData drill)
		{
			Drills.Remove(drill);
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
				for (int i = 0; i < drill.Patterns.Count; i++)
				{
					DrillData.PatternWeight patternWeight = drill.Patterns[i];
					if (patternMap.TryGetValue(patternWeight.Pattern.Title, out PatternData? canonical))
					{
						drill.Patterns[i] = new DrillData.PatternWeight { Pattern = canonical!, Weight = patternWeight.Weight };
					}
				}

				for (int i = 0; i < drill.FillerPatterns.Count; i++)
				{
					DrillData.PatternWeight patternWeight = drill.FillerPatterns[i];
					if (patternMap.TryGetValue(patternWeight.Pattern.Title, out PatternData? canonical))
					{
						drill.FillerPatterns[i] = new DrillData.PatternWeight { Pattern = canonical!, Weight = patternWeight.Weight };
					}
				}
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
				for (int i = 0; i < drill.Patterns.Count; i++)
				{
					if (drill.Patterns[i].Pattern.Title == oldTitle)
					{
						drill.Patterns[i].Pattern.Title = newTitle;
					}
				}

				for (int i = 0; i < drill.FillerPatterns.Count; i++)
				{
					if (drill.FillerPatterns[i].Pattern.Title == oldTitle)
					{
						drill.FillerPatterns[i].Pattern.Title = newTitle;
					}
				}
			}
		}

		public void Save()
		{
			SaveHelper.Save("PatternDatabase.json", this);
		}
	}
}