namespace OpenTaiko.Shrandy.Tools
{
	internal enum DrillRandomMode
	{
		Normal,
		Messy,
		RandomInvert,
	}

	internal class DrillData
	{
		public struct PatternWeight
		{
			public required PatternData Pattern { get; set; }
			public int Weight { get; set; }
		}

		public class BestComboRecord
		{
			public float Bpm { get; set; }
			public DrillRandomMode Mode { get; set; }
			public int Combo { get; set; }
		}

		private const float BpmMatchTolerance = 0.01f;

		public string Title { get; set; } = "";
		public List<PatternWeight> Patterns { get; set; } = new();
		public List<PatternWeight> FillerPatterns { get; set; } = new();
		public int MinFillerPatternFrequency { get; set; } = 4;
		public int MaxFillerPatternFrequency { get; set; } = 8;
		public List<BestComboRecord> BestCombos { get; set; } = new();

		public int GetBestCombo(float bpm, DrillRandomMode mode)
		{
			BestComboRecord? record = BestCombos.FirstOrDefault(r => r.Mode == mode && Math.Abs(r.Bpm - bpm) < BpmMatchTolerance);
			return record?.Combo ?? 0;
		}

		/// <returns>True if the combo was a new record for this BPM and mode.</returns>
		public bool TryRecordCombo(float bpm, DrillRandomMode mode, int combo)
		{
			BestComboRecord? record = BestCombos.FirstOrDefault(r => r.Mode == mode && Math.Abs(r.Bpm - bpm) < BpmMatchTolerance);
			if (record == null)
			{
				if (combo <= 0)
				{
					return false;
				}
				BestCombos.Add(new BestComboRecord { Bpm = bpm, Mode = mode, Combo = combo });
				return true;
			}

			if (combo > record.Combo)
			{
				record.Combo = combo;
				return true;
			}
			return false;
		}
	}
}