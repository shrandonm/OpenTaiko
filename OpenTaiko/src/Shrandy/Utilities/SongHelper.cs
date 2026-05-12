using FDK;
using ImGuiNET;
using System.Numerics;

namespace OpenTaiko.Shrandy.Utilities
{
	internal static class SongHelper
	{
		private const int EndOfChartChannel = 0xff;
		private const int WavChannel = 0x01;
		private const int AviChannel = 0x54;

		public static string GetDifficultyLabel(int difficulty)
		{
			return difficulty switch
			{
				(int)Difficulty.Easy => "Easy",
				(int)Difficulty.Normal => "Normal",
				(int)Difficulty.Hard => "Hard",
				(int)Difficulty.Oni => "Oni",
				(int)Difficulty.Edit => "Ura",
				(int)Difficulty.Tower => "Tower",
				(int)Difficulty.Dan => "Dan",
				_ => difficulty.ToString()
			};
		}

		public static void PlaySong(Chart chart)
		{
			if (OpenTaiko.stageSongSelect == null)
			{
				return;
			}

			CActSelect曲リスト songList = OpenTaiko.stageSongSelect.actSongList;
			songList.rCurrentlySelectedSong = chart.Song;

			OpenTaiko.ShrandyExtension.SetToolEnabled<Tools.SongBrowserTool>(false);
			OpenTaiko.ShrandyExtension.GetTool<Tools.PreviewTool>()?.Show(chart.Song, chart.Difficulty);
		}

		public static int GetScoreRank(int player, int currentScore)
		{
			int scoreRank = 0;
			if (currentScore >= 500000)
			{
				var thresholds = OpenTaiko.stageGameScreen.ScoreRank.ScoreRank[player];
				for (int i = 0; i < thresholds.Length; i++)
				{
					if (currentScore >= thresholds[i])
					{
						scoreRank = i + 1;
					}
				}
			}
			return scoreRank;
		}

		public static int GetSongDurationMs()
		{
			CTja? tja = OpenTaiko.TJA;
			if (tja == null || tja.listChip.Count == 0)
			{
				return 0;
			}

			int furthestTjaMs = tja.listChip[^1].n発声時刻ms;
			return (int)Math.Round(CTja.TjaDurationToGameDuration(furthestTjaMs));
		}
		
		public static int GetSongDurationMs(in Chart chart)
		{
			if (chart.Song == null || chart.Difficulty < 0 || chart.Difficulty >= chart.Song.score.Length)
			{
				return 0;
			}
			return chart.Song.score[chart.Difficulty].譜面情報.Duration;
		}

		public static void DrawScoreRank(int scoreRank, float iconHeight = 16.0f)
		{
			var texture = OpenTaiko.Tx.SongSelect_ScoreRank;
			if (texture == null || scoreRank <= 0)
			{
				return;
			}

			int clampedRank = Math.Clamp(scoreRank, 1, 7);
			float frameHeightUv = 1f / 7f;
			Vector2 uv0 = new(0f, (clampedRank - 1) * frameHeightUv);
			Vector2 uv1 = new(1f, clampedRank * frameHeightUv);

			float framePixelHeight = texture.szTextureSize.Height / 7f;
			float aspectRatio = texture.szTextureSize.Width / framePixelHeight;
			Vector2 size = new(iconHeight * aspectRatio, iconHeight);

			ImGui.Image((nint)texture.Pointer, size, uv0, uv1);
		}

		public static void DrawDifficultyIcon(int difficultyIndex, float iconHeight = 16.0f)
		{
			CTexture texture = OpenTaiko.Tx.SongSelect_Difficulty_Cymbol ?? OpenTaiko.Tx.Dani_Difficulty_Cymbol;
			if (texture == null || difficultyIndex < 0 || difficultyIndex > 4)
			{
				return;
			}

			float frameWidthUv = 1f / 5f;
			Vector2 uv0 = new(difficultyIndex * frameWidthUv, 0f);
			Vector2 uv1 = new((difficultyIndex + 1) * frameWidthUv, 1f);

			float framePixelWidth = texture.szTextureSize.Width / 5f;
			float aspectRatio = framePixelWidth / texture.szTextureSize.Height;
			Vector2 size = new(iconHeight * aspectRatio, iconHeight);

			ImGui.Image((nint)texture.Pointer, size, uv0, uv1);
		}
	}
}