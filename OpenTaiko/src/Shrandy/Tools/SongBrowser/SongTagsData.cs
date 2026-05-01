using System;
using System.Collections.Generic;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SongTagsData
	{
		private readonly SongTagsSaveData m_SaveData;
		private readonly Action m_OnChanged;

		public SongTagsData(SongTagsSaveData saveData, Action onChanged)
		{
			m_SaveData = saveData;
			m_OnChanged = onChanged;
		}

		private static string NormalizeKey(string title, int difficulty)
			=> title.ToLowerInvariant() + "::" + difficulty.ToString();

		public List<SongTag> GetTagsForSong(string title, int difficulty)
		{
			string key = NormalizeKey(title, difficulty);
			if (m_SaveData.SongTags.TryGetValue(key, out List<SongTag>? tags))
			{
				return tags;
			}
			return new List<SongTag>();
		}

		public void AddTag(string title, int difficulty, string tagName)
		{
			string key = NormalizeKey(title, difficulty);
			if (!m_SaveData.SongTags.TryGetValue(key, out List<SongTag>? tags))
			{
				tags = new List<SongTag>();
				m_SaveData.SongTags[key] = tags;
			}

			foreach (SongTag existing in tags)
			{
				if (existing.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}

			tags.Add(new SongTag { Name = tagName });
			m_OnChanged();
		}

		public void RemoveTag(string title, int difficulty, string tagName)
		{
			string key = NormalizeKey(title, difficulty);
			if (!m_SaveData.SongTags.TryGetValue(key, out List<SongTag>? tags))
			{
				return;
			}

			for (int i = tags.Count - 1; i >= 0; i--)
			{
				if (tags[i].Name.Equals(tagName, StringComparison.OrdinalIgnoreCase))
				{
					tags.RemoveAt(i);
					break;
				}
			}

			if (tags.Count == 0)
			{
				m_SaveData.SongTags.Remove(key);
			}

			m_OnChanged();
		}

		public List<SongTag> GetAllTags()
		{
			var seen = new Dictionary<string, SongTag>(StringComparer.OrdinalIgnoreCase);
			foreach (List<SongTag> tags in m_SaveData.SongTags.Values)
			{
				foreach (SongTag tag in tags)
				{
					seen.TryAdd(tag.Name, tag);
				}
			}
			var result = new List<SongTag>(seen.Values);
			result.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
			return result;
		}

		public bool SongHasTag(string title, int difficulty, string tagName)
		{
			foreach (SongTag t in GetTagsForSong(title, difficulty))
			{
				if (t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}
	}
}
