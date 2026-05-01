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
				m_UI.OnEnabled();
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
				if (stage is CStage演奏ドラム画面)
				{
					m_Data.ResetCurrentNoteStats();
				}
				SetEnabled(false);
			}
		}

		public override void OnNoteHit(HitParams hitParams)
		{
			m_Data.CurrentNoteStats.OnNoteHit(hitParams);
		}

		public override void OnNoteMiss(CChip? chip)
		{
			m_Data.CurrentNoteStats.OnNoteMissed();
		}

		public override void OnSongRestart()
		{
			base.OnSongRestart();
			m_Data.ResetCurrentNoteStats();
		}

		public override void OnResultsActivate(CStage結果 resultsScreen)
		{
			base.OnResultsActivate(resultsScreen);
			m_Data.TryAddCurrentSongStats();
			m_Data.SaveHistory();
			m_Data.RebuildBestPlaysCache();
			m_UI.RequestResultsPopup();
		}

		protected override void Draw()
		{
			base.Draw();
			m_UI.Draw();
		}
	}
}
