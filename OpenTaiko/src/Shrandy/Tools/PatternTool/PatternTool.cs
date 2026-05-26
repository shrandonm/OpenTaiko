using OpenTaiko.Shrandy.Utilities;
using SlimDXKeys;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternTool : Tool
	{
		private PatternToolUI m_UI;
		private PatternDatabase m_Database;
		private Random m_Rng = new();

		internal PatternDatabase Database => m_Database;

		public PatternTool(string toolName, Key enableHotkey) : base(toolName, enableHotkey)
		{
			m_Database = SaveHelper.LoadOrCreate<PatternDatabase>("PatternDatabase.json");
			m_Database.Reconcile();
			m_UI = new PatternToolUI(this);
		}

		internal void SaveDatabase()
		{
			m_Database.Save();
		}

		internal void PlayDrill(DrillData drill, int count)
		{
			List<DrillData.PatternWeight> availablePatterns = drill.Patterns.Where(pw => pw.Weight > 0).ToList();
			int totalWeight = availablePatterns.Sum(pw => pw.Weight);
			if (totalWeight == 0 || availablePatterns.Count == 0)
			{
				return;
			}

			List<PatternData> selected = new(count);
			for (int i = 0; i < count; i++)
			{
				int roll = m_Rng.Next(totalWeight);
				int accumulatedWeight = 0;
				foreach (DrillData.PatternWeight patternWeight in availablePatterns)
				{
					accumulatedWeight += patternWeight.Weight;
					if (roll < accumulatedWeight)
					{
						selected.Add(patternWeight.Pattern);
						break;
					}
				}
			}

			string combinedTJA = string.Join(",\n", selected.Select(p => p.TJA));
			PlayPattern(new PatternData
			{
				Title = drill.Title,
				TJA = combinedTJA
			});
		}

		internal void PlayPattern(PatternData pattern)
		{
			string tjaPath = Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Songs", "PatternTool", "PatternTool.tja");
			string folderPath = Path.GetDirectoryName(tjaPath) + Path.DirectorySeparatorChar;

			string tjaContent =
				$"TITLE:{pattern.Title}\n" +
				"BPM:120\n" +
				"WAVE:\n" +
				"OFFSET:0.000\n" +
				"COURSE:Easy\n" +
				"LEVEL:1\n" +
				"#START\n" +
				pattern.TJA + ",\n" +
				"#END\n";

			var newTja = new CTja();
			newTja.Activate();
			newTja.t入力FromString(tjaContent, tjaPath, folderPath, 0, 0, true, 0);
			newTja.tInitLocalStores(0);

			OpenTaiko.TJA!.t全チップの再生停止とミキサーからの削除();
			OpenTaiko.SetTJA(0, newTja);
			OpenTaiko.stageGameScreen.RefreshChipListReferences();
			OpenTaiko.stageGameScreen.actTokkun.Activate();
			OpenTaiko.stageGameScreen.t演奏やりなおし();
		}

		internal bool IsActive()
		{
			return OpenTaiko.rCurrentStage is CStage演奏ドラム画面 && OpenTaiko.ConfigIni.bTokkunMode;
		}

		protected override void Draw()
		{
			base.Draw();
			m_UI.Draw();
		}

		internal void EnterPatternMode()
		{
			string tjaPath = Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Songs", "PatternTool", "PatternTool.tja");
			string folderPath = Path.GetDirectoryName(tjaPath) + Path.DirectorySeparatorChar;

			Directory.CreateDirectory(Path.GetDirectoryName(tjaPath)!);
			File.WriteAllText(tjaPath,
				"TITLE:PatternTool\n" +
				"BPM:120\n" +
				"WAVE:\n" +
				"OFFSET:0.000\n" +
				"COURSE:Easy\n" +
				"LEVEL:1\n" +
				"#START\n" +
				",\n" +
				"#END\n");

			var score = new CScore();
			score.ファイル情報.ファイルの絶対パス = tjaPath;
			score.ファイル情報.フォルダの絶対パス = folderPath;
			score.譜面情報.タイトル = "PatternTool";

			var node = new CSongListNode();
			node.DanSongs = [];
			node.nodeType = CSongListNode.ENodeType.SCORE;
			node.ldTitle.SetString("default", "PatternTool");
			node.score[0] = score;

			OpenTaiko.stageSongSelect.rChoosenSong = node;
			OpenTaiko.stageSongSelect.r確定されたスコア = score;
			OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] = 0;
			OpenTaiko.ConfigIni.bTokkunMode = true;
			OpenTaiko.ConfigIni.nPlayerCount = 1;

			OpenTaiko.app.ChangeStage(OpenTaiko.stageSongLoading);
		}
	}
}
