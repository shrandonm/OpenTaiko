using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SongLoadingOverlayTool : Tool
	{
		private string m_SongTitle = "";
		private string m_DifficultyLabel = "";
		private SongEntry? m_BestPlay = null;

		public SongLoadingOverlayTool()
			: base("Song Loading Overlay", SlimDXKeys.Key.Unknown)
		{
		}

		public override void OnStageChanged(CStage stage)
		{
			if (stage == OpenTaiko.stageSongLoading || stage == OpenTaiko.stageGameScreen)
			{
				RefreshData();
				SetEnabled(true);
			}
			else
			{
				SetEnabled(false);
			}
		}

		private void RefreshData()
		{
			CSongListNode? song = OpenTaiko.stageSongSelect?.rChoosenSong;
			if (song == null)
			{
				m_SongTitle = "";
				m_DifficultyLabel = "";
				m_BestPlay = null;
				return;
			}

			m_SongTitle = song.ldTitle.GetString("") ?? "";

			int difficulty = OpenTaiko.stageSongSelect?.nChoosenSongDifficulty[0] ?? 0;
			m_DifficultyLabel = difficulty < SongBrowserData.DifficultyNames.Length
				? SongBrowserData.DifficultyNames[difficulty]
				: difficulty.ToString();

			m_BestPlay = GetTool<SongBrowserTool>()?.Data.GetBestPlay(m_SongTitle, difficulty);
		}

		private T? GetTool<T>() where T : Tool
		{
			return OpenTaiko.ShrandyExtension.GetTool<T>();
		}

		public override void DrawWindow()
		{
			if (!Enabled) return;

			var displaySize = ImGui.GetIO().DisplaySize;
			ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.Always);
			ImGui.SetNextWindowPos(
				new Vector2(displaySize.X * 0.5f, displaySize.Y * 0.85f),
				ImGuiCond.Always,
				new Vector2(0.5f, 0.5f));

			ImGuiWindowFlags flags =
				ImGuiWindowFlags.NoDecoration |
				ImGuiWindowFlags.NoInputs |
				ImGuiWindowFlags.NoMove |
				ImGuiWindowFlags.NoSavedSettings |
				ImGuiWindowFlags.NoFocusOnAppearing |
				ImGuiWindowFlags.NoNav |
				ImGuiWindowFlags.AlwaysAutoResize;

			if (ImGui.Begin("##song_loading_overlay", flags))
			{
				Draw();
			}
			ImGui.End();
		}

		protected override void Draw()
		{
			if (string.IsNullOrEmpty(m_SongTitle))
			{
				ImGui.TextDisabled("(no song selected)");
				return;
			}

			ImGui.TextUnformatted(m_SongTitle);
			ImGui.SameLine();
			ImGui.TextDisabled($"({m_DifficultyLabel})");

			if (m_BestPlay != null)
			{
				ImGui.SameLine();
				Utilities.SongHelper.DrawScoreRank(m_BestPlay.ScoreRank, 14f);
				ImGui.SameLine();
				ImGui.TextUnformatted($"{m_BestPlay.Score:N0}");
				ImGui.SameLine();
				Utilities.SongHelper.DrawCrown(m_BestPlay.EffectiveCrown);
				ImGui.SameLine();
				ImGui.TextDisabled($"  Good: {m_BestPlay.GoodPercentString}%");
			}
			else
			{
				ImGui.SameLine();
				ImGui.TextDisabled("  No best score");
			}
		}
	}
}
