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
		public List<BookmarkInstance> AllTimeBookmarkHistory { get; set; } = new();
		public Dictionary<string, List<BookmarkInstance>> RollingRecords { get; set; } = new();
		private const int HistoryLimit = 30;

		public NoteStats GetAggregateStats(string bookmarkName)
		{
			NoteStats stats = new();
			foreach (BookmarkInstance instance in GetRollingHistory(bookmarkName))
			{
				stats += instance.NoteStats;
			}
			return stats;
		}

		public void AddToHistory(BookmarkInstance bookmarkInstance)
		{
			AddToHistory(AllTimeBookmarkHistory, bookmarkInstance);

			List<BookmarkInstance> rollingHistory = GetRollingHistory(bookmarkInstance.BookmarkName);
			if (rollingHistory.Count >= HistoryLimit)
			{
				rollingHistory.RemoveAt(0);
			}
			AddToHistory(rollingHistory, bookmarkInstance);
		}

		private List<BookmarkInstance> GetRollingHistory(string bookmarkName)
		{
			if (!RollingRecords.ContainsKey(bookmarkName))
			{
				RollingRecords.Add(bookmarkName, new());
			}

			if (RollingRecords[bookmarkName] == null)
			{
				RollingRecords[bookmarkName] = new();
			}

			return RollingRecords[bookmarkName];
		}

		private void AddToHistory(List<BookmarkInstance> history, BookmarkInstance bookmarkInstance)
		{
			BookmarkInstance? recordedInstance = history.Find(x => x.BookmarkName == bookmarkInstance.BookmarkName);
			if (recordedInstance == null)
			{
				recordedInstance = new BookmarkInstance()
				{
					BookmarkName = bookmarkInstance.BookmarkName,
					Bookmark = bookmarkInstance.Bookmark,
				};
				history.Add(recordedInstance);
			}
			recordedInstance.NoteStats += bookmarkInstance.NoteStats;
		}

		public void DeleteBookmark(Bookmark bookmark)
		{
			Bookmarks.Remove(bookmark);
			AllTimeBookmarkHistory.RemoveAll(x => x.BookmarkName == bookmark.Name);

			foreach (var kvp in RollingRecords)
			{
				if (kvp.Value != null)
				{
					kvp.Value.RemoveAll(x => x.BookmarkName == bookmark.Name);
				}
			}
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
