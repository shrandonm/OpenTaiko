using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SongBrowserOverview
	{
		private static readonly Vector4[] BadgeColors =
		{
			new(0.55f, 0.55f, 0.55f, 1.0f),  // 0 - none (gray)
			new(0.92f, 0.92f, 0.92f, 1.0f),  // 1 - white
			new(0.80f, 0.52f, 0.25f, 1.0f),  // 2 - bronze
			new(0.75f, 0.75f, 0.80f, 1.0f),  // 3 - silver
			new(1.00f, 0.84f, 0.10f, 1.0f),  // 4 - gold
			new(1.00f, 0.45f, 0.72f, 1.0f),  // 5 - pink
			new(0.72f, 0.25f, 0.95f, 1.0f),  // 6 - purple
			new(0.40f, 0.92f, 1.00f, 1.0f),  // 7 - rainbow
		};

		private SongBrowserData m_Data;

		private int m_MatchedSongs;
		private int m_PlayedSongs;
		private int m_TotalPlays;
		private long m_TotalPlaytimeMs;
		private int m_TotalClears;
		private int m_TotalFCs;
		private int m_TotalDFCs;
		private int[] m_BadgeCounts = new int[8];

		public SongBrowserOverview(SongBrowserData data)
		{
			m_Data = data;
		}

		public void Refresh()
		{
			m_MatchedSongs = m_Data.FilteredSongs.Count;
			m_PlayedSongs = 0;
			m_TotalPlays = 0;
			m_TotalClears = 0;
			m_TotalFCs = 0;
			m_TotalDFCs = 0;
			Array.Clear(m_BadgeCounts, 0, m_BadgeCounts.Length);

			var filteredSet = new HashSet<(string title, int diff)>(m_Data.FilteredSongs.Count);

			foreach ((CSongListNode song, int diff) in m_Data.FilteredSongs)
			{
				string titleKey = song.ldTitle.GetString("").ToLowerInvariant();
				filteredSet.Add((titleKey, diff));

				SongAggregateStats agg = m_Data.GetAggregateStats(song.ldTitle.GetString(""), diff);
				m_TotalPlays += agg.PlayCount;
				if (agg.PlayCount > 0) m_PlayedSongs++;
				if (agg.ClearCount > 0) m_TotalClears++;
				if (agg.FCCount > 0) m_TotalFCs++;
				if (agg.DFCCount > 0) m_TotalDFCs++;

				SongEntry? best = m_Data.GetBestPlay(song.ldTitle.GetString(""), diff);
				int rank = best?.ScoreRank ?? 0;
				rank = Math.Clamp(rank, 0, 7);
				m_BadgeCounts[rank]++;
			}

			m_TotalPlaytimeMs = 0;
			foreach (SongEntry e in m_Data.SaveData.SongEntries)
			{
				int diff = Utilities.SongTable.GetDifficultyFromLabel(e.Difficulty);
				if (filteredSet.Contains((e.SongTitle.ToLowerInvariant(), diff)))
				{
					m_TotalPlaytimeMs += e.DurationMs;
				}
			}
		}

		public void Draw()
		{
			if (!ImGui.BeginTable("##stats_outer", 2, ImGuiTableFlags.None))
				return;

			ImGui.TableSetupColumn("##stats_left", ImGuiTableColumnFlags.WidthStretch);
			ImGui.TableSetupColumn("##stats_right", ImGuiTableColumnFlags.WidthStretch);

			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			DrawOverview();

			ImGui.TableSetColumnIndex(1);
			DrawSessionStats();

			ImGui.EndTable();
		}

		private void DrawOverview()
		{
			ImGui.SeparatorText("Overview");

			ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
			if (!ImGui.BeginTable("##overview_table", 2, flags))
				return;

			ImGui.TableSetupColumn("##ovlabel", ImGuiTableColumnFlags.WidthFixed, 80);
			ImGui.TableSetupColumn("##ovvalue", ImGuiTableColumnFlags.WidthStretch);

			int unplayed = m_MatchedSongs - m_PlayedSongs;
			int clearPct = m_MatchedSongs > 0 ? (int)(m_TotalClears * 100f / m_MatchedSongs + 0.5f) : 0;
			int fcPct    = m_MatchedSongs > 0 ? (int)(m_TotalFCs    * 100f / m_MatchedSongs + 0.5f) : 0;
			int dfcPct   = m_MatchedSongs > 0 ? (int)(m_TotalDFCs   * 100f / m_MatchedSongs + 0.5f) : 0;
			string playtime = Utilities.SongTable.FormatDuration((int)Math.Min(m_TotalPlaytimeMs, int.MaxValue));

			DrawLabelValueRow("Songs",    $"{m_PlayedSongs} / {m_MatchedSongs}" + (unplayed > 0 ? $"  ({unplayed} new)" : ""));
			DrawLabelValueRow("Plays",    $"{m_TotalPlays:N0}");
			DrawLabelValueRow("Playtime", playtime);

			// Crowns row
			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.TextDisabled("Crowns");
			ImGui.TableSetColumnIndex(1);
			ImGui.TextColored(Utilities.SongHelper.GetCrownColor(1), Utilities.SongHelper.GetCrownString(1));
			ImGui.SameLine();
			ImGui.TextUnformatted($"{m_TotalClears} ({clearPct}%)");
			ImGui.SameLine(0, 16);
			ImGui.TextColored(Utilities.SongHelper.GetCrownColor(2), Utilities.SongHelper.GetCrownString(2));
			ImGui.SameLine();
			ImGui.TextUnformatted($"{m_TotalFCs} ({fcPct}%)");
			ImGui.SameLine(0, 16);
			ImGui.TextColored(Utilities.SongHelper.GetCrownColor(3), Utilities.SongHelper.GetCrownString(3));
			ImGui.SameLine();
			ImGui.TextUnformatted($"{m_TotalDFCs} ({dfcPct}%)");

			// Badges row
			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.TextDisabled("Badges");
			ImGui.TableSetColumnIndex(1);
			DrawBadgeBreakdown();

			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.TextDisabled("Milestone");
			ImGui.TableSetColumnIndex(1);
			int lifetimePlayCount = m_Data.SaveData.SongEntries.Count;
			int nextMilestone = (lifetimePlayCount / 1000 + 1) * 1000;
			float progress = (lifetimePlayCount % 1000) / 1000f;
			ImGui.ProgressBar(progress, new Vector2(-1, 0), $"{lifetimePlayCount:N0} / {nextMilestone:N0} lifetime plays");
			
			ImGui.EndTable();
		}

		private void DrawSessionStats()
		{
			ImGui.SeparatorText("Session Stats");

			TimeSpan elapsed = m_Data.GetSessionElapsed();
			int sessionSongCount = m_Data.GetSessionSongCount();
			int sessionDurationMs = m_Data.GetSessionDurationMs();
			float uptime = sessionDurationMs / (float)elapsed.TotalMilliseconds;
			float songsPerHour = sessionSongCount / (float)elapsed.TotalHours;
			int todayPlayCount = m_Data.GetDailySongCount();
			int todayFCCount = m_Data.GetDailyFCCount();

			ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
			if (!ImGui.BeginTable("##session_table", 2, flags))
				return;

			ImGui.TableSetupColumn("##sslabel", ImGuiTableColumnFlags.WidthFixed, 110);
			ImGui.TableSetupColumn("##ssvalue", ImGuiTableColumnFlags.WidthStretch);

			DrawLabelValueRow("Time Since Start", $"{elapsed:hh\\:mm\\:ss}");
			DrawLabelValueRow("Session Playtime", Utilities.SongTable.FormatDuration(sessionDurationMs));
			DrawLabelValueRow("Song Count",       $"{sessionSongCount}");
			DrawLabelValueRow("Today's Plays",    $"{todayPlayCount}");
			DrawLabelValueRow("Today's FCs",      $"{todayFCCount}");
			DrawLabelValueRow("Uptime",           $"{(int)(uptime * 100)}%");
			DrawLabelValueRow("Songs / Hour",     $"{(int)songsPerHour}");

			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.TableSetColumnIndex(1);
			if (ImGui.Button("Reset Session Stats"))
			{
				m_Data.ResetSessionStats();
			}

			ImGui.EndTable();
		}

		private static void DrawLabelValueRow(string label, string value)
		{
			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.TextDisabled(label);
			ImGui.TableSetColumnIndex(1);
			ImGui.TextUnformatted(value);
		}

		private void DrawBadgeBreakdown()
		{
			if (!ImGui.BeginTable("##badge_inner", 8, ImGuiTableFlags.None))
			{
				return;
			}

			for (int i = 0; i < 8; i++)
			{
				ImGui.TableSetupColumn($"##bi{i}", ImGuiTableColumnFlags.WidthStretch);
			}

			// Icons row
			ImGui.TableNextRow();
			for (int rank = 0; rank < 8; rank++)
			{
				ImGui.TableSetColumnIndex(rank);
				if (rank == 0)
					ImGui.TextDisabled("--");
				else
					Utilities.SongHelper.DrawScoreRank(rank, 14.0f);
			}

			// Counts row
			ImGui.TableNextRow();
			for (int rank = 0; rank < 8; rank++)
			{
				ImGui.TableSetColumnIndex(rank);
				ImGui.PushStyleColor(ImGuiCol.Text, BadgeColors[rank]);
				ImGui.TextUnformatted($"{m_BadgeCounts[rank]}");
				ImGui.PopStyleColor();
			}

			ImGui.EndTable();
		}
	}
}
