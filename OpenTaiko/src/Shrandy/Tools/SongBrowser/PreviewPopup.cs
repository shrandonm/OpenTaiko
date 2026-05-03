using System;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PreviewPopup
	{
		private const string WindowTitle = "Now Playing##preview";

		private SongBrowserData m_Data;

		private CSongListNode? m_Song;
		private int m_Difficulty;
		private bool m_IsOpen = false;

		// Cached display data
		private string m_Title = "";
		private string m_DifficultyLabel = "";
		private int m_ChartLevel;
		private double m_BaseBpm;
		private double m_MinBpm;
		private double m_MaxBpm;

		private SongEntry? m_BestPlayNoMods;
		private SongEntry? m_BestPlayMatchingMods;
		private SongAggregateStats m_AggStatsNoMods;
		private SongAggregateStats m_AggStatsMatchingMods;
		private bool m_ModsAreDefault;
		private string m_CurrentModsLabel = "";
		private string m_CurrentJudgementLabel = "";
		private double m_DaysSinceLastPlayed;
		private double m_DaysSinceLastPB;

		public PreviewPopup(SongBrowserData data)
		{
			m_Data = data;
		}

		public bool IsOpen()
		{
			return m_IsOpen;
		}
		
		public void Show(CSongListNode song, int difficulty)
		{
			m_Song = song;
			m_Difficulty = difficulty;
			RefreshData();
			m_IsOpen = true;
		}
		
		public void Hide()
		{
			m_IsOpen = false;
			m_Song = null;
		}

		private void RefreshData()
		{
			if (m_Song == null) return;

			m_Title = m_Song.ldTitle.GetString("");
			m_DifficultyLabel = Utilities.SongTable.GetDifficultyLabel(m_Difficulty);
			m_ChartLevel = m_Difficulty >= 0 && m_Difficulty < m_Song.nLevel.Length
				? m_Song.nLevel[m_Difficulty]
				: 0;

			var scoreInfo = m_Song.score[m_Difficulty]?.譜面情報 ?? default;
			m_BaseBpm = scoreInfo.BaseBpm;
			m_MinBpm = scoreInfo.MinBpm;
			m_MaxBpm = scoreInfo.MaxBpm;

			string currentMods = m_Data.GetCurrentModsLabel();
			int currentJudgement = m_Data.GetCurrentJudgement();

			m_CurrentModsLabel = currentMods;
			m_CurrentJudgementLabel = CLangManager.LangInstance.GetString($"MOD_TIMING{currentJudgement + 1}");

			m_ModsAreDefault = currentMods == "None" && currentJudgement == 2;
			m_BestPlayNoMods = m_Data.GetBestPlayNoMods(m_Title, m_Difficulty);
			m_AggStatsNoMods = m_Data.GetAggregateStatsNoMods(m_Title, m_Difficulty);

			if (m_ModsAreDefault)
			{
				m_BestPlayMatchingMods = null;
				m_AggStatsMatchingMods = default;
			}
			else
			{
				m_BestPlayMatchingMods = m_Data.GetBestPlayMatchingMods(m_Title, m_Difficulty, currentMods, currentJudgement);
				m_AggStatsMatchingMods = m_Data.GetAggregateStatsMatchingMods(m_Title, m_Difficulty, currentMods, currentJudgement);
			}

			m_DaysSinceLastPlayed = m_Data.GetDaysSinceLastPlayed(m_Title, m_Difficulty);
			m_DaysSinceLastPB = m_Data.GetDaysSinceLastPB(m_Title, m_Difficulty);
		}

		public void Draw()
		{
			if (!m_IsOpen) return;

			var displaySize = ImGui.GetIO().DisplaySize;
			ImGui.SetNextWindowSize(new Vector2(380, 0), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(displaySize.X * 0.5f, displaySize.Y * 0.5f), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
			if (ImGui.Begin(WindowTitle, ref m_IsOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
			{
				ImGui.TextUnformatted(m_Title);
				ImGui.SameLine();
				ImGui.TextDisabled($"  {m_DifficultyLabel} Lv.{m_ChartLevel}");
				if (m_BaseBpm > 0)
				{
					ImGui.SameLine();
					DrawBpmInline();
				}
				ImGui.Separator();

				if (ImGui.BeginTable("##previewstats", 2, ImGuiTableFlags.None))
				{
					ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, 180);
					ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch);

					// No Mods
					BeginStatRow("Best Score (No Mods)");
					if (m_BestPlayNoMods != null)
					{
						Utilities.ScoreHelper.DrawScoreRank(m_BestPlayNoMods.ScoreRank, 14f);
						ImGui.SameLine();
						ImGui.TextUnformatted($"{m_BestPlayNoMods.Score:N0}");
					}
					else
					{
						ImGui.TextUnformatted("-");
					}

					DrawStatRow("Plays", $"{m_AggStatsNoMods.PlayCount}");
					DrawStatRow("FC", $"{m_AggStatsNoMods.FCCount}");
					DrawStatRow("DFC", $"{m_AggStatsNoMods.DFCCount}");

					// Current mods (if not default)
					if (!m_ModsAreDefault)
					{
						string modsDesc = m_CurrentModsLabel == "None"
							? m_CurrentJudgementLabel
							: $"{m_CurrentModsLabel}, {m_CurrentJudgementLabel}";

						BeginStatRow($"Best Score ({modsDesc})");
						if (m_BestPlayMatchingMods != null)
						{
							Utilities.ScoreHelper.DrawScoreRank(m_BestPlayMatchingMods.ScoreRank, 14f);
							ImGui.SameLine();
							ImGui.TextUnformatted($"{m_BestPlayMatchingMods.Score:N0}");
						}
						else
						{
							ImGui.TextUnformatted("-");
						}

						DrawStatRow($"Plays ({modsDesc})", $"{m_AggStatsMatchingMods.PlayCount}");
						DrawStatRow($"FC ({modsDesc})", $"{m_AggStatsMatchingMods.FCCount}");
						DrawStatRow($"DFC ({modsDesc})", $"{m_AggStatsMatchingMods.DFCCount}");
					}

					DrawStatRow("Last Played", FormatDays(m_DaysSinceLastPlayed));
					DrawStatRow("Last PB", FormatDays(m_DaysSinceLastPB));

					ImGui.EndTable();
				}
			}
			ImGui.End();
		}

		private void DrawBpmInline()
		{
			bool hasRange = m_MinBpm > 0 && m_MaxBpm > 0 && (m_MaxBpm - m_MinBpm) > 0.5;
			if (hasRange)
			{
				ImGui.TextDisabled($"BPM {m_MinBpm:F0}–{m_MaxBpm:F0}");
			}
			else
			{
				ImGui.TextDisabled($"BPM {m_BaseBpm:F0}");
			}
		}

		private static void BeginStatRow(string label)
		{
			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted(label);
			ImGui.TableSetColumnIndex(1);
		}

		private static void DrawStatRow(string label, string value)
		{
			BeginStatRow(label);
			ImGui.TextUnformatted(value);
		}

		private static string FormatDays(double days)
		{
			if (days == double.MaxValue) return "Never";
			if (days < 1.0) return "Today";
			if (days < 2.0) return "Yesterday";
			return $"{(int)days}d ago";
		}
	}
}
