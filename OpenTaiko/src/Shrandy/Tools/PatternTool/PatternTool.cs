using System.Globalization;
using OpenTaiko.Shrandy.Utilities;
using SlimDXKeys;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternTool : Tool
	{
		private PatternToolUI m_UI;
		private PatternDatabase m_Database;
		private BackingTrackSelector m_BackingTrackSelector = new();
		private Random m_Rng = new();

		private DrillData? m_CurrentlyPlayedDrill;
		private PatternData? m_CurrentlyPlayedPattern;
		private float m_CurrentlyPlayedBpm;
		private float m_CurrentlyPlayedPatternBpm = 120f;
		private DrillRandomMode m_CurrentlyPlayedMode;
		private bool m_ComboRecordDirty = false;
		private MicroStopwatch m_IdleStopwatch = new();

		private const long IdleSaveDelayMs = 3000;

		internal PatternDatabase Database => m_Database;
		internal DrillData? CurrentlyPlayedDrill => m_CurrentlyPlayedDrill;
		internal BackingTrackSelector BackingTrackSelector => m_BackingTrackSelector;

		internal void SelectBackingTrack(Chart? chart)
		{
			if (m_BackingTrackSelector.Select(chart) && IsActive() && m_CurrentlyPlayedPattern != null)
			{
				PlayPattern(m_CurrentlyPlayedPattern, m_CurrentlyPlayedPatternBpm);
			}
		}

		public PatternTool(string toolName, Key enableHotkey) : base(toolName, enableHotkey)
		{
			m_Database = PatternDatabase.LoadOrCreate();
			m_UI = new PatternToolUI(this);
		}

		internal void SaveDatabase()
		{
			m_Database.Save();
		}

		internal void PlayDrill(DrillData drill, int count, DrillRandomMode mode, float bpm)
		{
			string? tja = BuildDrillTja(drill, count, mode, bpm);
			if (tja != null)
			{
				FlushComboRecordIfDirty();
				m_CurrentlyPlayedDrill = drill;
				m_CurrentlyPlayedBpm = bpm;
				m_CurrentlyPlayedMode = mode;
				PlayPattern(new PatternData { Title = drill.Title, TJA = tja }, bpm);
			}
		}

		internal string? BuildDrillTja(DrillData drill, int count, DrillRandomMode mode, float bpm)
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

			List<PatternData> selectedPatterns = new();
			int regularSinceLastFiller = 0;
			int nextFillerAfter = hasFillers ? RollFillerFrequency(drill) : int.MaxValue;
			int lastBpmChangeMeasure = 0;

			for (int i = 0; i < count; i++)
			{
				if (hasFillers && regularSinceLastFiller >= nextFillerAfter)
				{
					selectedPatterns.Add(PickWeightedRandom(availableFillers, totalFillerWeight));
					regularSinceLastFiller = 0;
					nextFillerAfter = RollFillerFrequency(drill);
				}
				else
				{
					selectedPatterns.Add(PickWeightedRandom(availablePatterns, totalWeight));
					regularSinceLastFiller++;					
				}
			}
			
			List<string> tjaMeasures = new(selectedPatterns.Count);
			for (int i = 0; i < selectedPatterns.Count; i++)
			{
				PatternData pattern = selectedPatterns[i];
				string notes = ApplyRandomMode(pattern.TJA, mode);
				
				if (drill.RandomBpmRange > 0 && (i - lastBpmChangeMeasure) >= drill.RandomBpmChangeFrequency)
				{
					float newBpm = bpm + m_Rng.Next(-drill.RandomBpmRange, drill.RandomBpmRange + 1);
					tjaMeasures.Add($"#BPMCHANGE {newBpm}\n{notes}");
					lastBpmChangeMeasure = i;
				}
				else
				{
					tjaMeasures.Add(notes);
				}
			}

			return string.Join(",\n", tjaMeasures);
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

		private static string BuildTjaContent(string title, string body, float bpm = 120f, BackingTrack? backingTrack = null)
		{
			string audioPath = backingTrack?.AudioPath ?? "";
			double offset = backingTrack?.Offset ?? 0.0;
			if (backingTrack != null && backingTrack.Bpm > 0.0 && bpm > 0.0f)
			{
				offset *= backingTrack.Bpm / bpm;
			}

			return $"TITLE:{title}\n" +
				$"BPM:{bpm:0.##}\n" +
				$"WAVE:{audioPath}\n" +
				$"OFFSET:{offset.ToString("0.###", CultureInfo.InvariantCulture)}\n" +
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
			BackingTrack? backingTrack = m_BackingTrackSelector.Selected;
			string tjaContent = BuildTjaContent(pattern.Title, pattern.TJA, bpm, backingTrack);

			CTja newTja = new CTja();
			if (backingTrack != null && backingTrack.Bpm > 0.0 && bpm > 0.0f)
			{
				newTja.BgmPlaySpeedMultiplier = bpm / backingTrack.Bpm;
			}

			newTja.Activate();
			newTja.t入力FromString(tjaContent, tjaPath, folderPath, 0, 0, true, (int)Difficulty.Oni);
			foreach (CTja.CWAV wave in newTja.listWAV.Values)
			{
				if (wave.listこのWAVを使用するチャンネル番号の集合.Count > 0)
				{
					newTja.tWAVの読み込み(wave);
				}
			}

			if (OpenTaiko.ConfigIni.bDynamicBassMixerManagement)
			{
				newTja.PlanToAddMixerChannel();
			}

			newTja.tInitLocalStores(0);
			m_CurrentlyPlayedPattern = pattern;
			m_CurrentlyPlayedPatternBpm = bpm;

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
			m_CurrentlyPlayedPattern = new PatternData { Title = "PatternTool", TJA = "" };
			m_CurrentlyPlayedPatternBpm = 120f;

			OpenTaiko.app.ChangeStage(OpenTaiko.stageSongLoading);
		}
	}
}
