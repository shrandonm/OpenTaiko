using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy.TrainingTool
{
	[JsonConverter(typeof(BookmarkKeyConverter))]
	internal struct BookmarkKey
	{
		public string Key { get; set; }

		public BookmarkKey(string bookmarkName, int speed)
		{
			Key = $"{bookmarkName}_{speed}";
		}
	}

	internal class BookmarkKeyConverter : JsonConverter<BookmarkKey>
	{
		public override BookmarkKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			string s = reader.GetString() ?? "";
			return new BookmarkKey { Key = s };
		}

		public override void Write(Utf8JsonWriter writer, BookmarkKey value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.Key);
		}

		public override BookmarkKey ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			string s = reader.GetString() ?? "";
			return new BookmarkKey { Key = s };
		}

		public override void WriteAsPropertyName(Utf8JsonWriter writer, BookmarkKey value, JsonSerializerOptions options)
		{
			writer.WritePropertyName(value.Key);
		}
	}
}
