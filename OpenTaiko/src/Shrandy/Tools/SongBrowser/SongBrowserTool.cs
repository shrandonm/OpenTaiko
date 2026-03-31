namespace OpenTaiko.Shrandy.Tools
{
	internal class SongBrowserTool : Tool
	{
		private SongBrowserData m_Data;
		private SongBrowserUI m_UI;
		private bool m_AutoShowInSongSelect = false;

		public SongBrowserTool(string toolName, SlimDXKeys.Key enableHotkey)
			: base(toolName, enableHotkey)
		{
			m_Data = new SongBrowserData();
			m_UI = new SongBrowserUI(m_Data);
		}

		public override bool IsBlockingInput()
		{
			return true;
		}

		public override void SetEnabled(bool enabled)
		{
			base.SetEnabled(enabled);
			if (enabled)
			{
				m_AutoShowInSongSelect = true;
			}
		}

		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);
			if (stage is CStageSongSelect)
			{
				if (m_AutoShowInSongSelect)
				{
					SetEnabled(true);
				}
				m_Data.RefreshSongList();
			}
			else
			{
				SetEnabled(false);
			}
		}

		public override void OnResultsActivate(CStage結果 resultsScreen)
		{
			base.OnResultsActivate(resultsScreen);
			m_Data.TryAddCurrentSongStats();
			m_Data.SaveHistory();
			m_Data.RebuildBestPlaysCache();
		}

		protected override void Draw()
		{
			base.Draw();
			m_UI.Draw();
		}
	}
}
