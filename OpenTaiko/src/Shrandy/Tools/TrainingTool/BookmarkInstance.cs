using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy.TrainingTool
{
	internal class BookmarkInstance
	{
		public string BookmarkName { get; set; } = "";
		public long TimestampUtc { get; set; }
		public NoteStats NoteStats { get; set; } = new();
		public int Speed { get; set; }

		[JsonIgnore]
		public Bookmark Bookmark { get; set; }

		public BookmarkKey GetBookmarkKey()
		{
			return new BookmarkKey(BookmarkName, Speed);
		}
	}
}
