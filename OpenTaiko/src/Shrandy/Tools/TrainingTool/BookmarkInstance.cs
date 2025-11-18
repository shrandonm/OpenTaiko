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
		public int PlayCount { get; set; }
		public NoteStats NoteStats { get; set; } = new();

		[JsonIgnore]
		public Bookmark Bookmark { get; set; }
	}
}
