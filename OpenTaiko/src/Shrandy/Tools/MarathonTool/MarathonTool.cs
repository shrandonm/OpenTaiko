using OpenTaiko.Shrandy.Utilities;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class MarathonTool : Tool
	{
		private Queue<Chart> m_ChartQueue = new();
		private int m_TargetDurationMinutes = 25;

		public MarathonTool(SlimDXKeys.Key enableHotkey)
			: base("Marathon Tool", enableHotkey)
		{
		}

		public override void DrawWindow()
		{
			base.DrawWindow();

			if (ImGui.InputInt("Target Duration (minutes)", ref m_TargetDurationMinutes))
			{
				if (m_TargetDurationMinutes < 1)
				{
					m_TargetDurationMinutes = 1;
				}
			}

			DrawGeneratePlaylistButton();

			if (m_ChartQueue.Count == 0)
			{
				ImGui.BeginDisabled();
			}

			if (ImGui.Button("Play"))
			{
				PlayNextSong();
			}

			if (m_ChartQueue.Count == 0)
			{
				ImGui.EndDisabled();
			}

			ImGui.TextUnformatted("Upcoming songs:");
			ImGui.Spacing();
			foreach (Chart chart in m_ChartQueue)
			{
				string songTitle = chart.Song?.score[chart.Difficulty].譜面情報.タイトル ?? "Unknown Song";
				string difficultyName = SongHelper.GetDifficultyLabel(chart.Difficulty);
				ImGui.TextUnformatted($"{songTitle} - {difficultyName}");
			}
		}

		private void DrawGeneratePlaylistButton()
		{
			if (ImGui.Button("Generate Playlist"))
			{
				var filteredSongs = OpenTaiko.ShrandyExtension.GetTool<SongBrowserTool>()?.Data.FilteredSongs;
				if (filteredSongs == null || filteredSongs.Count == 0)
				{
					ImGui.OpenPopup("No Songs Available");
				}
				else
				{
					List<Chart> charts = new();
					foreach (var (song, difficulty) in filteredSongs)
					{
						Chart chart = new Chart(song, difficulty);
						if (song.score[difficulty] != null && SongHelper.GetSongDurationMs(chart) > 0)
						{
							charts.Add(chart);
						}
					}
					//CreateQueue(charts, m_TargetDurationMinutes * 60 * 1000);
				}
			}
		}

		public void CreateQueue(List<Chart> charts, int targetDurationMs)
		{
			m_ChartQueue.Clear();
			
			int remainingMs = targetDurationMs;
			List<Chart> shuffledCharts = charts.Shuffle();
			
			int index = 0;
			while (remainingMs > 0)
			{
				if (index >= shuffledCharts.Count)
				{
					shuffledCharts = charts.Shuffle();
					index = 0;
				}
				
				Chart chart = shuffledCharts[index];
				
				int durationMs = SongHelper.GetSongDurationMs(chart);
				if (durationMs <= remainingMs)
				{
					m_ChartQueue.Enqueue(chart);
					remainingMs -= durationMs;
				}
				index++;
			}
		}
		
		public override bool HandleSongCompleteTransition()
		{
			OpenTaiko.rCurrentStage.DeActivate();
			if (!OpenTaiko.ConfigIni.PreAssetsLoading)
			{
				OpenTaiko.rCurrentStage.ReleaseManagedResource();
				OpenTaiko.rCurrentStage.ReleaseUnmanagedResource();
			}
			
			if (m_ChartQueue.Count > 0)
			{
				PlayNextSong();
				return true;
			}
			return false;
		}
		
		public void PlayNextSong()
		{
			if (m_ChartQueue.Count == 0)
			{
				return;
			}
			
			Chart nextChart = m_ChartQueue.Dequeue();
			
			OpenTaiko.stageSongSelect.rNowSelectedSong = nextChart.Song;
			OpenTaiko.stageSongSelect.rChoosenSong = nextChart.Song;
			OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] = nextChart.Difficulty;
			OpenTaiko.stageSongSelect.r確定されたスコア = nextChart.Song.score[nextChart.Difficulty];
			
			OpenTaiko.app.ChangeStage(OpenTaiko.rCurrentStage);
		}
		
		public void Stop()
		{
			m_ChartQueue.Clear();
		}
	}
}
		