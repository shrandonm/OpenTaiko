using ImGuiNET;
using System.Numerics;

namespace OpenTaiko.Shrandy.Utilities
{
	internal static class ScoreHelper
	{
		private const int EndOfChartChannel = 0xff;
		private const int WavChannel = 0x01;
		private const int AviChannel = 0x54;

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
	}
}