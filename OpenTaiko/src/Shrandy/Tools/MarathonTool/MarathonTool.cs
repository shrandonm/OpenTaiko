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

		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);
			if (stage is CStage演奏ドラム画面)
			{
				SetEnabled(false);
			}
		}

		protected override void Draw()
		{
			base.Draw();
			
			if (!ImGui.IsWindowFocused())
			{
				SetEnabled(false);
			}
			
			DrawTimeControls();
			DrawGeneratePlaylistButton();

			if (m_ChartQueue.Count == 0)
			{
				ImGui.BeginDisabled();
			}

			if (ImGui.Button("Play"))
			{
				PlayNextSong();
				SetEnabled(false);
			}

			ImGui.SameLine();

			if (ImGui.Button("Clear Playlist"))
			{
				m_ChartQueue.Clear();
			}

			if (m_ChartQueue.Count == 0)
			{
				ImGui.EndDisabled();
			}

			ImGui.TextUnformatted("Upcoming songs:");
			ImGui.SameLine();
			ImGui.Text("Duration: " + SongTable.FormatDuration(m_ChartQueue.Sum(c => SongHelper.GetSongDurationMs(c))));
			ImGui.Spacing();
			foreach (Chart chart in m_ChartQueue)
			{
				string songTitle = chart.Song?.score[chart.Difficulty].譜面情報.タイトル ?? "Unknown Song";
				string difficultyName = SongHelper.GetDifficultyLabel(chart.Difficulty);
				ImGui.TextUnformatted($"{songTitle} - {difficultyName}");
			}
		}

		private void DrawTimeControls()
		{
			if (ImGui.Button("5m"))
			{
				m_TargetDurationMinutes = 5;
			}
			ImGui.SameLine();
			if (ImGui.Button("10m"))
			{
				m_TargetDurationMinutes = 10;
			}
			ImGui.SameLine();
			if (ImGui.Button("15m"))
			{
				m_TargetDurationMinutes = 15;
			}
			ImGui.SameLine();
			if (ImGui.Button("25m"))
			{
				m_TargetDurationMinutes = 25;
			}
			
			if (ImGui.InputInt("Target Duration (minutes)", ref m_TargetDurationMinutes))
			{
				if (m_TargetDurationMinutes < 1)
				{
					m_TargetDurationMinutes = 1;
				}
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
					
					if (charts.Count > 0)
					{
						CreateQueue(charts, m_TargetDurationMinutes * 60 * 1000);
					}
					else
					{
						ImGui.OpenPopup("No Valid Songs");
					}
				}
			}
		}

		public void CreateQueue(List<Chart> charts, int targetDurationMs)
		{
			m_ChartQueue.Clear();
			
			int remainingMs = targetDurationMs;
			List<Chart> shuffledCharts = charts.Shuffle();
			
			const int maxIterations = 1000;
			const int graceDurationMs = 30 * 1000; // Allow 30 seconds over the target duration to find a good fit
			for (int i = 0; i < maxIterations; ++i)
			{
				if (i % shuffledCharts.Count == 0)
				{
					shuffledCharts = charts.Shuffle();
				}
				
				Chart chart = shuffledCharts[i % shuffledCharts.Count];
				
				int durationMs = SongHelper.GetSongDurationMs(chart);
				if (durationMs <= remainingMs + graceDurationMs)
				{
					m_ChartQueue.Enqueue(chart);
					remainingMs -= durationMs;
				}
				else
				{
					break;
				}
			}
		}
		
		public override bool HandleSongCompleteTransition()
		{
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
			
			OpenTaiko.app.ChangeStage(OpenTaiko.stageSongLoading);
		}
		
		public void Stop()
		{
			m_ChartQueue.Clear();
		}
	}
}
		