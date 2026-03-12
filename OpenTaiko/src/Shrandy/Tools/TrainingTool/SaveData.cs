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
		public Dictionary<BookmarkKey, AggregateNoteStats> History { get; set; } = new();

		public AggregateNoteStats GetAggregateStats(BookmarkKey key)
		{
			if (!History.ContainsKey(key))
			{
				History.Add(key, new());
			}
			return History[key];
		}

		public void AddToHistory(BookmarkInstance bookmarkInstance)
		{
			AggregateNoteStats aggregateNoteStats = GetAggregateStats(bookmarkInstance.GetBookmarkKey());
			aggregateNoteStats.CombinedNoteStats += bookmarkInstance.NoteStats;
			aggregateNoteStats.TotalRuns++;
			if (bookmarkInstance.NoteStats.IsDFC)
			{
				aggregateNoteStats.DFCCount++;
			}
			if (bookmarkInstance.NoteStats.IsFC)
			{
				aggregateNoteStats.FCCount++;
			}
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
				try
				{
					return JsonSerializer.Deserialize<SaveData>(json);
				}
				catch
				{
					return null;
				}
			}
			return null;
		}
	}
}
