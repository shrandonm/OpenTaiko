using ImGuiNET;
using SlimDXKeys;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternTool : Tool
	{

		public PatternTool(string toolName, Key enableHotkey) : base(toolName, enableHotkey)
		{
		}

		private bool IsActive()
		{
			return OpenTaiko.rCurrentStage is CStage演奏ドラム画面 && OpenTaiko.ConfigIni.bTokkunMode;
		}
		
		protected override void Draw()
		{
			base.Draw();

			bool inGameStage = IsActive();
			if (!inGameStage)
			{
				if (ImGui.Button("Enter Pattern Mode"))
				{
					EnterPatternMode();
				}
			}
			else
			{
				ImGui.Text("Pattern mode active.");
			}
		}

		private void EnterPatternMode()
		{
			string tjaPath = Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Songs", "PatternTool", "PatternTool.tja");
			string folderPath = Path.GetDirectoryName(tjaPath) + Path.DirectorySeparatorChar;

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
