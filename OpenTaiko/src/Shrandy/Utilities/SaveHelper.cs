using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy.Utilities
{
	internal static class SaveHelper
	{
		public static void Save(string fileName, object obj)
		{
			string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(Path.Combine(ShrandyExtension.SaveDirectoryPath, fileName), json);
		}

		public static T LoadOrCreate<T>(string fileName) where T : class, new()
		{
			string filePath = Path.Combine(ShrandyExtension.SaveDirectoryPath, fileName);
			if (File.Exists(filePath))
			{
				string jsonText = File.ReadAllText(filePath);
				if (!string.IsNullOrEmpty(jsonText))
				{
					try
					{
						T? result = JsonSerializer.Deserialize<T>(jsonText);
						if (result != null)
						{
							return result;
						}
					}
					catch
					{
						return new();
					}
				}
			}
			return new();
		}
	}
}
