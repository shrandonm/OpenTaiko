namespace OpenTaiko.Shrandy
{
	/// <summary>Decides whether the active mods allow a play to be converted into a regular (no timing mod) score.</summary>
	internal static class ModScoreFilter
	{
		/// <summary>A score is only valid if the timing mod is equal to or stricter than default.</summary>
		public static bool IsValidScore(int timingZoneIndex)
		{
			return timingZoneIndex >= CConfigIni.DefaultTimingZoneIndex;
		}

		public static bool IsValidScore(SongEntry entry)
		{
			return IsValidScore(entry.Judgement);
		}

		public static bool IsValidScoreForActiveMods()
		{
			return IsValidScore(OpenTaiko.ConfigIni.nTimingZones[OpenTaiko.SaveFile]);
		}
	}
}
