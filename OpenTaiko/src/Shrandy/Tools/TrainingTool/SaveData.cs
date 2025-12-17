using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SaveData
	{
		public string SongName { get; set; } = "";
		public List<Bookmark> Bookmarks { get; set; } = new();
		public Dictionary<BookmarkKey, List<BookmarkInstance>> History { get; set; } = new();
		private const int HistoryLimit = int.MaxValue;

		public AggregateNoteStats GetAggregateStats(BookmarkKey key, int amount)
		{
			AggregateNoteStats aggregate = new();
			List<BookmarkInstance> instances = GetBookmarkEntryList(key);
			amount = Math.Min(amount, instances.Count);
			foreach (BookmarkInstance instance in instances[^amount..])
			{
				aggregate.TotalRuns++;
				aggregate.CombinedNoteStats += instance.NoteStats;
				if (instance.NoteStats.IsDFC)
				{
					aggregate.DFCCount++;
				}
				if (instance.NoteStats.IsFC)
				{
					aggregate.FCCount++;
				}
			}
			return aggregate;
		}

		public void AddToHistory(BookmarkInstance bookmarkInstance)
		{
			List<BookmarkInstance> bookmarkEntries = GetBookmarkEntryList(bookmarkInstance.GetBookmarkKey());
			if (bookmarkEntries.Count >= HistoryLimit)
			{
				bookmarkEntries.RemoveAt(0);
			}
			bookmarkEntries.Add(bookmarkInstance);
		}

		public List<BookmarkInstance> GetBookmarkEntryList(BookmarkKey bookmarkKey)
		{
			if (!History.ContainsKey(bookmarkKey))
			{
				History.Add(bookmarkKey, new());
			}

			if (History[bookmarkKey] == null)
			{
				History[bookmarkKey] = new();
			}

			return History[bookmarkKey];
		}

		public void DeleteBookmark(Bookmark bookmark)
		{
			Bookmarks.Remove(bookmark);
			foreach (BookmarkKey key in History.Keys.Where(x => x.Key.StartsWith($"{bookmark.Name}_")).ToList())
			{
				History.Remove(key);
			}
		}

		public void DeleteHistory(Bookmark bookmark, int speed)
		{
			History.Remove(new BookmarkKey(bookmark.Name, speed));
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
