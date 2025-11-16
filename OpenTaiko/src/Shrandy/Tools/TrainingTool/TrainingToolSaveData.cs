using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy
{
	internal class TrainingToolSaveData
	{
		public string SongName { get; set; } = "";
		public List<MeasureSaveData> MeasureData { get; set; } = new();
		public List<CActImplTrainingMode.STJUMPP> Bookmarks { get; set; } = new();

		public class MeasureSaveData
		{
			public int GoodCount { get; set; }
			public int OkayCount { get; set; }
			public int BadCount { get; set; }
			public float AverageError { get; set; }
		}

		public void Save()
		{
			CTja? tja = OpenTaiko.GetTJA(0);
			CActImplTrainingMode trainingMode = OpenTaiko.stageGameScreen.actTokkun;

			if (tja == null || trainingMode == null)
			{
				return;
			}

			Bookmarks = new(trainingMode.JumpPointList);

			string path = tja.strFileName + ".json";
			JsonSerializerOptions options = new()
			{
				WriteIndented = true,
			};
			string json = JsonSerializer.Serialize(this, options);
			File.WriteAllText(Path.Combine(ShrandyExtension.SaveDirectoryPath, path), json);
		}

		public static TrainingToolSaveData? Load(string songFileName)
		{
			string path = Path.Combine(ShrandyExtension.SaveDirectoryPath, songFileName + ".json");
			if (File.Exists(path))
			{
				string json = File.ReadAllText(path);
				return JsonSerializer.Deserialize<TrainingToolSaveData>(json);
			}
			return null;
		}
	}
}
