using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class LiveScoreHudTool : Tool
	{
		private string m_SongTitle = "";
		private string m_DifficultyLabel = "";
		private List<SongEntry> m_TopPlays = new();

		public LiveScoreHudTool()
			: base("Live Score HUD", SlimDXKeys.Key.Unknown)
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
				m_TopPlays.Clear();
				return;
			}

			m_SongTitle = song.ldTitle.GetString("") ?? "";

			int difficulty = OpenTaiko.stageSongSelect?.nChoosenSongDifficulty[0] ?? 0;
			m_DifficultyLabel = difficulty < SongBrowserData.DifficultyNames.Length
				? SongBrowserData.DifficultyNames[difficulty]
				: difficulty.ToString();

			SongBrowserData? songBrowserData = GetTool<SongBrowserTool>()?.Data;
			if (songBrowserData == null)
			{
				m_TopPlays.Clear();
				return;
			}

			m_TopPlays = songBrowserData.GetTopPlaysMatchingMods(
				m_SongTitle,
				difficulty,
				songBrowserData.GetCurrentModsLabel(),
				songBrowserData.GetCurrentJudgement(),
				songBrowserData.GetCurrentSongSpeed());
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

			if (ImGui.Begin("##live_score_hud", flags))
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

			if (m_TopPlays.Count > 0)
			{
				for (int index = 0; index < m_TopPlays.Count; index++)
				{
					SongEntry entry = m_TopPlays[index];
					ImGui.TextDisabled($"#{index + 1}");
					ImGui.SameLine();
					Utilities.SongHelper.DrawScoreRank(entry.ScoreRank, 14f);
					ImGui.SameLine();
					ImGui.TextUnformatted($"{entry.Score:N0}");
					ImGui.SameLine();
					Utilities.SongHelper.DrawCrown(entry.EffectiveCrown);
					ImGui.SameLine();
					ImGui.TextDisabled($"  Good: {entry.GoodPercentString}%");
				}
			}
			else
			{
				ImGui.SameLine();
				ImGui.TextDisabled("  No best score");
			}
		}
	}
}
