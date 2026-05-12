using System;
using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PreviewTool : Tool
	{
		private const string WindowTitle = "Now Playing##preview";

		private CSongListNode? m_Song;
		private int m_Difficulty;
		private DateTime m_ConfirmAllowedAfter = DateTime.MinValue;

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
		private double m_DaysSinceLastPlayedNoMods;
		private double m_DaysSinceLastPBNoMods;
		private double m_DaysSinceLastPlayedMatchingMods;
		private double m_DaysSinceLastPBMatchingMods;

		public PreviewTool(string toolName)
			: base(toolName, SlimDXKeys.Key.Unknown)
		{
		}

		public void Show(CSongListNode song, int difficulty)
		{
			m_Song = song;
			m_Difficulty = difficulty;
			m_ConfirmAllowedAfter = DateTime.UtcNow.AddMilliseconds(300);
			RefreshData();
			base.SetEnabled(true);
		}

		private void Confirm()
		{
			if (m_Song == null || OpenTaiko.stageSongSelect == null)
			{
				return;
			}
			int difficulty = m_Difficulty;
			OpenTaiko.stageSongSelect.t曲を選択する(difficulty, 0);
			SetEnabled(false);
		}

		private void Cancel()
		{
			SetEnabled(false);
			OpenTaiko.ShrandyExtension.SetToolEnabled<SongBrowserTool>(true);
			m_Song = null;
		}

		public override void SetEnabled(bool enabled)
		{
			base.SetEnabled(enabled);
			if (!enabled)
			{
				m_Song = null;
			}
		}

		public override bool IsBlockingInput()
		{
			return Enabled;
		}

		protected override void Update()
		{
			if (DateTime.UtcNow < m_ConfirmAllowedAfter)
			{
				return;
			}

			if (OpenTaiko.Pad.IsPressingCancel(forceAllowGameInput: true))
			{
				Cancel();
			}
			else if (OpenTaiko.Pad.IsPressingDecide(forceAllowGameInput: true))
			{
				Confirm();
			}
		}

		public override void DrawWindow()
		{
			Update();
			if (!Enabled) return;

			var displaySize = ImGui.GetIO().DisplaySize;
			ImGui.SetNextWindowSize(new Vector2(380, 0), ImGuiCond.Appearing);
			ImGui.SetNextWindowPos(new Vector2(displaySize.X * 0.5f, displaySize.Y * 0.5f), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
			bool open = Enabled;
			if (ImGui.Begin(WindowTitle, ref open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse))
			{
				Draw();
			}
			ImGui.End();
			SetEnabled(open);
		}

		protected override void Draw()
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

			if (!m_ModsAreDefault)
			{
				// No Mods section (inactive — dimmed)
				ImGui.TextDisabled("No Mods");
				ImGui.Separator();
				if (ImGui.BeginTable("##previewstats_nomods", 2, ImGuiTableFlags.None))
				{
					ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, 180);
					ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch);

					ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.55f, 0.55f, 0.55f, 1f));

					BeginStatRow("Best Score (No Mods)");
					if (m_BestPlayNoMods != null)
					{
						Utilities.SongHelper.DrawScoreRank(m_BestPlayNoMods.ScoreRank, 14f);
						ImGui.SameLine();
						ImGui.TextUnformatted($"{m_BestPlayNoMods.Score:N0}");
					}
					else
					{
						ImGui.TextUnformatted("-");
					}

					DrawStatRow("Good / Okay / Bad", m_BestPlayNoMods != null
						? $"{m_BestPlayNoMods.Goods} / {m_BestPlayNoMods.Okays} / {m_BestPlayNoMods.Bads}"
						: "-");
					DrawStatRow("Plays / FC / DFC", $"{m_AggStatsNoMods.PlayCount} / {m_AggStatsNoMods.FCCount} / {m_AggStatsNoMods.DFCCount}");
					DrawStatRow("Last Played", FormatDays(m_DaysSinceLastPlayedNoMods));
					DrawStatRow("Last PB", FormatDays(m_DaysSinceLastPBNoMods));

					ImGui.PopStyleColor();
					ImGui.EndTable();
				}

				ImGui.Spacing();
				ImGui.Separator();

				// Current Mods section (active — bright)
				string modsDesc = m_CurrentModsLabel == "None"
					? m_CurrentJudgementLabel
					: $"{m_CurrentModsLabel}, {m_CurrentJudgementLabel}";

				ImGui.TextColored(new Vector4(0.4f, 0.85f, 1f, 1f), $"Current Mods ({modsDesc})");
				ImGui.Separator();
				if (ImGui.BeginTable("##previewstats_mods", 2, ImGuiTableFlags.None))
				{
					ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, 180);
					ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch);

					BeginStatRow("Best Score");
					if (m_BestPlayMatchingMods != null)
					{
						Utilities.SongHelper.DrawScoreRank(m_BestPlayMatchingMods.ScoreRank, 14f);
						ImGui.SameLine();
						ImGui.TextUnformatted($"{m_BestPlayMatchingMods.Score:N0}");
					}
					else
					{
						ImGui.TextUnformatted("-");
					}

					DrawStatRow("Good / Okay / Bad", m_BestPlayMatchingMods != null
						? $"{m_BestPlayMatchingMods.Goods} / {m_BestPlayMatchingMods.Okays} / {m_BestPlayMatchingMods.Bads}"
						: "-");
					DrawStatRow("Plays / FC / DFC", $"{m_AggStatsMatchingMods.PlayCount} / {m_AggStatsMatchingMods.FCCount} / {m_AggStatsMatchingMods.DFCCount}");
					DrawStatRow("Last Played", FormatDays(m_DaysSinceLastPlayedMatchingMods));
					DrawStatRow("Last PB", FormatDays(m_DaysSinceLastPBMatchingMods));

					ImGui.EndTable();
				}
			}
			else
			{
				// No mods — single section, no header needed
				if (ImGui.BeginTable("##previewstats", 2, ImGuiTableFlags.None))
				{
					ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthFixed, 180);
					ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch);

					BeginStatRow("Best Score");
					if (m_BestPlayNoMods != null)
					{
						Utilities.SongHelper.DrawScoreRank(m_BestPlayNoMods.ScoreRank, 14f);
						ImGui.SameLine();
						ImGui.TextUnformatted($"{m_BestPlayNoMods.Score:N0}");
					}
					else
					{
						ImGui.TextUnformatted("-");
					}

					DrawStatRow("Good / Okay / Bad", m_BestPlayNoMods != null
						? $"{m_BestPlayNoMods.Goods} / {m_BestPlayNoMods.Okays} / {m_BestPlayNoMods.Bads}"
						: "-");
					DrawStatRow("Plays / FC / DFC", $"{m_AggStatsNoMods.PlayCount} / {m_AggStatsNoMods.FCCount} / {m_AggStatsNoMods.DFCCount}");
					DrawStatRow("Last Played", FormatDays(m_DaysSinceLastPlayedNoMods));
					DrawStatRow("Last PB", FormatDays(m_DaysSinceLastPBNoMods));

					ImGui.EndTable();
				}
			}

			ImGui.Spacing();
			ImGui.TextDisabled("Don / Enter to start  |  Esc to cancel");
		}

		private void RefreshData()
		{
			if (m_Song == null)
			{
				return;
			}
			
			SongBrowserData? data = OpenTaiko.ShrandyExtension.GetTool<SongBrowserTool>()?.Data;
			if (data == null)
			{
				return;
			}

			m_Title = m_Song.ldTitle.GetString("");
			m_DifficultyLabel = Utilities.SongHelper.GetDifficultyLabel(m_Difficulty);
			m_ChartLevel = m_Difficulty >= 0 && m_Difficulty < m_Song.nLevel.Length
				? m_Song.nLevel[m_Difficulty]
				: 0;

			var scoreInfo = m_Song.score[m_Difficulty]?.譜面情報 ?? default;
			m_BaseBpm = scoreInfo.BaseBpm;
			m_MinBpm = scoreInfo.MinBpm;
			m_MaxBpm = scoreInfo.MaxBpm;

			string currentMods = data.GetCurrentModsLabel();
			int currentJudgement = data.GetCurrentJudgement();

			m_CurrentModsLabel = currentMods;
			m_CurrentJudgementLabel = CLangManager.LangInstance.GetString($"MOD_TIMING{currentJudgement + 1}");

			m_ModsAreDefault = currentMods == "None" && currentJudgement == 2;
			m_BestPlayNoMods = data.GetBestPlayNoMods(m_Title, m_Difficulty);
			m_AggStatsNoMods = data.GetAggregateStatsNoMods(m_Title, m_Difficulty);

			if (m_ModsAreDefault)
			{
				m_BestPlayMatchingMods = null;
				m_AggStatsMatchingMods = default;
			}
			else
			{
				m_BestPlayMatchingMods = data.GetBestPlayMatchingMods(m_Title, m_Difficulty, currentMods, currentJudgement);
				m_AggStatsMatchingMods = data.GetAggregateStatsMatchingMods(m_Title, m_Difficulty, currentMods, currentJudgement);
			}

			SongEntry? lastPlayedNoMods = data.GetLastPlayNoMods(m_Title, m_Difficulty);
			m_DaysSinceLastPlayedNoMods = lastPlayedNoMods == null ? double.MaxValue : (DateTime.Now - lastPlayedNoMods.Timestamp).TotalDays;
			m_DaysSinceLastPBNoMods = m_BestPlayNoMods == null ? double.MaxValue : (DateTime.Now - m_BestPlayNoMods.Timestamp).TotalDays;

			if (m_ModsAreDefault)
			{
				m_DaysSinceLastPlayedMatchingMods = double.MaxValue;
				m_DaysSinceLastPBMatchingMods = double.MaxValue;
			}
			else
			{
				SongEntry? lastPlayedMatchingMods = data.GetLastPlayMatchingMods(m_Title, m_Difficulty, currentMods, currentJudgement);
				m_DaysSinceLastPlayedMatchingMods = lastPlayedMatchingMods == null ? double.MaxValue : (DateTime.Now - lastPlayedMatchingMods.Timestamp).TotalDays;
				m_DaysSinceLastPBMatchingMods = m_BestPlayMatchingMods == null ? double.MaxValue : (DateTime.Now - m_BestPlayMatchingMods.Timestamp).TotalDays;
			}
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
