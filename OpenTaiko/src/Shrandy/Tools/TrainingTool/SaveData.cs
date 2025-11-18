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
		public List<BookmarkInstance> BookmarkHistory { get; set; } = new();

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
