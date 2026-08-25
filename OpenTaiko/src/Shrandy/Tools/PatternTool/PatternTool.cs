using OpenTaiko.Shrandy.Utilities;
using SlimDXKeys;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternTool : Tool
	{
		private PatternToolUI m_UI;
		private PatternDatabase m_Database;
		private Random m_Rng = new();

		private DrillData? m_CurrentlyPlayedDrill;
		private float m_CurrentlyPlayedBpm;
		private DrillRandomMode m_CurrentlyPlayedMode;
		private bool m_ComboRecordDirty = false;
		private MicroStopwatch m_IdleStopwatch = new();

		private const long IdleSaveDelayMs = 3000;

		internal PatternDatabase Database => m_Database;
		internal DrillData? CurrentlyPlayedDrill => m_CurrentlyPlayedDrill;

		public PatternTool(string toolName, Key enableHotkey) : base(toolName, enableHotkey)
		{
			m_Database = PatternDatabase.LoadOrCreate();
			m_UI = new PatternToolUI(this);
		}

		internal void SaveDatabase()
		{
			m_Database.Save();
		}

		internal void PlayDrill(DrillData drill, int count, DrillRandomMode mode = DrillRandomMode.Normal, float bpm = 120f)
		{
			string? tja = BuildDrillTja(drill, count, mode);
			if (tja != null)
			{
				FlushComboRecordIfDirty();
				m_CurrentlyPlayedDrill = drill;
				m_CurrentlyPlayedBpm = bpm;
				m_CurrentlyPlayedMode = mode;
				PlayPattern(new PatternData { Title = drill.Title, TJA = tja }, bpm);
			}
		}

		internal string? BuildDrillTja(DrillData drill, int count, DrillRandomMode mode = DrillRandomMode.Normal)
		{
			List<DrillData.PatternWeight> availablePatterns = drill.Patterns.Where(pw => pw.Weight > 0).ToList();
			int totalWeight = availablePatterns.Sum(pw => pw.Weight);
			if (totalWeight == 0 || availablePatterns.Count == 0)
				return null;

			List<DrillData.PatternWeight> availableFillers = drill.FillerPatterns.Where(pw => pw.Weight > 0).ToList();
			int totalFillerWeight = availableFillers.Sum(pw => pw.Weight);
			bool hasFillers = availableFillers.Count > 0
				&& totalFillerWeight > 0
				&& drill.MinFillerPatternFrequency > 0
				&& drill.MaxFillerPatternFrequency >= drill.MinFillerPatternFrequency;

			List<PatternData> selected = new();
			int regularSinceLastFiller = 0;
			int nextFillerAfter = hasFillers ? RollFillerFrequency(drill) : int.MaxValue;

			for (int i = 0; i < count; i++)
			{
				if (hasFillers && regularSinceLastFiller >= nextFillerAfter)
				{
					selected.Add(PickWeightedRandom(availableFillers, totalFillerWeight));
					regularSinceLastFiller = 0;
					nextFillerAfter = RollFillerFrequency(drill);
				}

				selected.Add(PickWeightedRandom(availablePatterns, totalWeight));
				regularSinceLastFiller++;
			}

			return string.Join(",\n", selected.Select(p => ApplyRandomMode(p.TJA, mode)));
		}

		private string ApplyRandomMode(string tja, DrillRandomMode mode)
		{
			switch (mode)
			{
				case DrillRandomMode.Messy:
					return ApplyMessy(tja);
				case DrillRandomMode.RandomInvert:
					return m_Rng.Next(2) == 0 ? ApplyRandomInvert(tja) : tja;
				case DrillRandomMode.MonoDon:
					return ApplyMono(tja, '1');
				case DrillRandomMode.MonoKa:
					return ApplyMono(tja, '2');
				default:
					return tja;
			}
		}

		private static string ApplyMono(string tja, char note)
		{
			char[] chars = tja.ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				if (chars[i] == '1' || chars[i] == '2')
				{
					chars[i] = note;
				}
			}
			return new string(chars);
		}

		private static string ApplyRandomInvert(string tja)
		{
			char[] chars = tja.ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				switch (chars[i])
				{
					case '1':
						chars[i] = '2';
						break;
					case '2':
						chars[i] = '1';
						break;
					case '3':
						chars[i] = '4';
						break;
					case '4':
						chars[i] = '3';
						break;
				}
			}
			return new string(chars);
		}

		private string ApplyMessy(string tja)
		{
			char[] chars = tja.ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				bool flip = m_Rng.Next(2) == 0;
				switch (chars[i])
				{
					case '1':
						if (flip)
						{
							chars[i] = '2';
						}
						break;
					case '2':
						if (flip)
						{
							chars[i] = '1';
						}
						break;
					case '3':
						if (flip)
						{
							chars[i] = '4';
						}	
						break;
					case '4':
						if (flip)
						{
							chars[i] = '3';
						}
						break;
				}
			}
			return new string(chars);
		}

		private PatternData PickWeightedRandom(List<DrillData.PatternWeight> patterns, int totalWeight)
		{
			int roll = m_Rng.Next(totalWeight);
			int accumulated = 0;
			foreach (DrillData.PatternWeight pw in patterns)
			{
				accumulated += pw.Weight;
				if (roll < accumulated)
				{
					return pw.Pattern;
				}
			}
			return patterns[^1].Pattern;
		}

		private int RollFillerFrequency(DrillData drill)
		{
			int min = Math.Max(1, drill.MinFillerPatternFrequency);
			int max = Math.Max(min, drill.MaxFillerPatternFrequency);
			return m_Rng.Next(min, max + 1);
		}

		private static string GetTjaFilePath()
		{
			return Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Songs", "PatternTool", "PatternTool.tja");
		}

		private static string BuildTjaContent(string title, string body, float bpm = 120f)
		{
			return $"TITLE:{title}\n" +
				$"BPM:{bpm:0.##}\n" +
				"WAVE:\n" +
				"OFFSET:0.000\n" +
				"COURSE:Oni\n" +
				"LEVEL:1\n" +
				"#START\n" +
				body + ",\n" +
				"#END\n";
		}

		internal void PlayPattern(PatternData pattern, float bpm = 120f)
		{
			string tjaPath = GetTjaFilePath();
			string folderPath = Path.GetDirectoryName(tjaPath) + Path.DirectorySeparatorChar;
			string tjaContent = BuildTjaContent(pattern.Title, pattern.TJA, bpm);

			CTja newTja = new CTja();
			newTja.Activate();
			newTja.t入力FromString(tjaContent, tjaPath, folderPath, 0, 0, true, (int)Difficulty.Oni);
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

		public override void OnNoteHit(HitParams hitParams)
		{
			base.OnNoteHit(hitParams);
			CheckComboRecord();
		}

		public override void OnNoteMiss(CChip? chip)
		{
			base.OnNoteMiss(chip);
			CheckComboRecord();
		}

		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);
			FlushComboRecordIfDirty();
		}

		private void CheckComboRecord()
		{
			if (m_CurrentlyPlayedDrill == null || !IsActive())
			{
				return;
			}

			m_IdleStopwatch.Restart();

			int currentCombo = OpenTaiko.stageGameScreen.actCombo.nCurrentCombo.最高値[0];
			if (m_CurrentlyPlayedDrill.TryRecordCombo(m_CurrentlyPlayedBpm, m_CurrentlyPlayedMode, currentCombo))
			{
				m_ComboRecordDirty = true;
			}
		}

		private void FlushComboRecordIfDirty()
		{
			if (m_ComboRecordDirty)
			{
				SaveDatabase();
				m_ComboRecordDirty = false;
			}
		}

		protected override void Update()
		{
			base.Update();

			if (m_ComboRecordDirty && m_IdleStopwatch.ElapsedMilliseconds >= IdleSaveDelayMs)
			{
				FlushComboRecordIfDirty();
			}
		}

		protected override void Draw()
		{
			base.Draw();
			m_UI.Draw();
		}

		internal void EnterPatternMode()
		{
			string tjaPath = GetTjaFilePath();
			string folderPath = Path.GetDirectoryName(tjaPath) + Path.DirectorySeparatorChar;

			Directory.CreateDirectory(Path.GetDirectoryName(tjaPath)!);
			File.WriteAllText(tjaPath, BuildTjaContent("PatternTool", ""));

			CScore score = new CScore();
			score.ファイル情報.ファイルの絶対パス = tjaPath;
			score.ファイル情報.フォルダの絶対パス = folderPath;
			score.譜面情報.タイトル = "PatternTool";

			CSongListNode node = new CSongListNode();
			node.DanSongs = [];
			node.nodeType = CSongListNode.ENodeType.SCORE;
			node.ldTitle.SetString("default", "PatternTool");
			node.score[0] = score;

			OpenTaiko.stageSongSelect.rChoosenSong = node;
			OpenTaiko.stageSongSelect.r確定されたスコア = score;
			OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] = (int)Difficulty.Oni;
			OpenTaiko.ConfigIni.bTokkunMode = true;
			OpenTaiko.ConfigIni.nPlayerCount = 1;

			OpenTaiko.app.ChangeStage(OpenTaiko.stageSongLoading);
		}
	}
}
