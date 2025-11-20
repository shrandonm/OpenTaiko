using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy.TrainingTool
{
	internal class SaveData
	{
		public string SongName { get; set; } = "";
		public List<Bookmark> Bookmarks { get; set; } = new();
		public Dictionary<string, List<BookmarkInstance>> History { get; set; } = new();
		private const int HistoryLimit = int.MaxValue;

		public NoteStats GetAggregateStats(string bookmarkName, int amount, int bpm)
		{
			NoteStats stats = new();
			List<BookmarkInstance> instances = GetBookmarkEntryList(bookmarkName);
			amount = Math.Min(amount, instances.Count);
			foreach (BookmarkInstance instance in instances[^amount..])
			{
				//if (instance.BPM == bpm)
				{
					stats += instance.NoteStats;
				}
			}
			return stats;
		}

		public void AddToHistory(BookmarkInstance bookmarkInstance)
		{
			List<BookmarkInstance> bookmarkEntries = GetBookmarkEntryList(bookmarkInstance.BookmarkName);
			if (bookmarkEntries.Count >= HistoryLimit)
			{
				bookmarkEntries.RemoveAt(0);
			}
			bookmarkEntries.Add(bookmarkInstance);
		}

		private List<BookmarkInstance> GetBookmarkEntryList(string bookmarkName)
		{
			if (!History.ContainsKey(bookmarkName))
			{
				History.Add(bookmarkName, new());
			}

			if (History[bookmarkName] == null)
			{
				History[bookmarkName] = new();
			}

			return History[bookmarkName];
		}

		public void DeleteBookmark(Bookmark bookmark)
		{
			Bookmarks.Remove(bookmark);
			History.Remove(bookmark.Name);
		}

		public void Save()
		{
			CTja? tja = OpenTaiko.GetTJA(0);
			if (tja == null)
			{
				return;
			}

			string path = tja.strFileName + ".json";
			JsonSerializerOptions options = new()
			{
				WriteIndented = true,
			};
			string json = JsonSerializer.Serialize(this, options);
			File.WriteAllText(Path.Combine(ShrandyExtension.SaveDirectoryPath, path), json);
		}

		public static SaveData? Load(string songFileName)
		{
			string path = Path.Combine(ShrandyExtension.SaveDirectoryPath, songFileName + ".json");
			if (File.Exists(path))
			{
				string json = File.ReadAllText(path);
				return JsonSerializer.Deserialize<SaveData>(json);
			}
			return null;
		}
	}
}
