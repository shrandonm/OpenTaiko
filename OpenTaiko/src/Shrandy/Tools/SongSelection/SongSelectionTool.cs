using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ImGuiNET;
using System.Numerics;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SongSelectionTool : Tool
	{
		private List<CSongListNode> m_AllSongs = new();
		private List<CSongListNode> m_FilteredSongs = new();
		private int m_SelectedDifficulty = (int)Difficulty.Oni;
		private string m_FilterText = "";
		private bool m_NeedsRefresh = true;

		private static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard", "Oni", "Ura" };

		// badge name -> rank value: 0=none 1=white 2=bronze 3=silver 4=gold 5=pink 6=purple 7=rainbow
		private static readonly Dictionary<string, int> BadgeNames = new(StringComparer.OrdinalIgnoreCase)
		{
			["none"] = 0, ["white"] = 1, ["bronze"] = 2, ["silver"] = 3,
			["gold"] = 4, ["pink"] = 5, ["purple"] = 6, ["rainbow"] = 7,
		};

		// fc/clear value names: 0=uncleared 1=cleared 2=fc 3=dfc
		private static readonly Dictionary<string, int> ClearNames = new(StringComparer.OrdinalIgnoreCase)
		{
			["none"] = 0, ["clear"] = 1, ["fc"] = 2, ["dfc"] = 3,
		};

		private static readonly Regex FilterTokenRegex = new(@"(\w+)\s*(>=|<=|>|<|=)\s*(\S+)", RegexOptions.Compiled);

		public SongSelectionTool(string toolName, SlimDXKeys.Key enableHotkey)
			: base(toolName, enableHotkey)
		{
		}

		public override bool IsBlockingInput()
		{
			return true;
		}
		
		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);
			if (stage is CStageSongSelect)
			{
				RefreshSongList();
			}
		}

		protected override void Draw()
		{
			if (OpenTaiko.stageSongSelect == null || OpenTaiko.Songs管理 == null)
			{
				ImGui.Text("Song select not available.");
				return;
			}

			if (m_AllSongs.Count == 0)
			{
				RefreshSongList();
			}

			DrawDifficultySelector();
			DrawFilters();
			DrawSongTable();
		}

		private void DrawDifficultySelector()
		{
			ImGui.SeparatorText("Difficulty");
			for (int i = 0; i < DifficultyNames.Length; i++)
			{
				if (i > 0) ImGui.SameLine();
				bool selected = m_SelectedDifficulty == i;
				if (selected) ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
				if (ImGui.Button(DifficultyNames[i]))
				{
					m_SelectedDifficulty = i;
					m_NeedsRefresh = true;
				}
				if (selected) ImGui.PopStyleColor();
			}
		}

		private void DrawFilters()
		{
			ImGui.SeparatorText("Filter");

			ImGui.SetNextItemWidth(-1);
			if (ImGui.InputTextWithHint("##filter", "e.g. bpm>100 badge<purple fc<=0 song title words", ref m_FilterText, 512))
			{
				m_NeedsRefresh = true;
			}

			if (m_NeedsRefresh)
			{
				ApplyFilters();
				m_NeedsRefresh = false;
			}

			ImGui.Text($"{m_FilteredSongs.Count} / {m_AllSongs.Count} songs");
		}

		private void DrawSongTable()
		{
			ImGui.SeparatorText("Songs");

			if (m_FilteredSongs.Count == 0)
			{
				ImGui.Text("No songs match the current filters.");
				return;
			}

			float availableHeight = ImGui.GetContentRegionAvail().Y - 30;
			if (Utilities.SongTable.BeginTable("SongList", ImGuiTableFlags.ScrollY, availableHeight))
			{
				for (int i = 0; i < m_FilteredSongs.Count; i++)
				{
					CSongListNode song = m_FilteredSongs[i];
					var row = Utilities.SongTable.FromSongNode(song, m_SelectedDifficulty);
					string creator = song.strNotesDesigner?[m_SelectedDifficulty] ?? "";

					// Override the title column to include a Play button
					ImGui.TableNextRow();
					ImGui.TableSetColumnIndex(0);
					ImGui.PushID(i);
					string label = row.Title;
					if (ImGui.Selectable(label))
					{
						Utilities.SongTable.PlaySong(song, m_SelectedDifficulty);
					}
					ImGui.PopID();

					// Draw remaining columns starting from column 1
					Utilities.SongTable.DrawRowFromColumn1(in row, creator);
				}

				Utilities.SongTable.EndTable();
			}
		}

		private void RefreshSongList()
		{
			m_AllSongs.Clear();

			if (OpenTaiko.stageSongSelect?.actSongList == null || OpenTaiko.Songs管理?.list曲ルート == null)
			{
				return;
			}

			var allNodes = OpenTaiko.stageSongSelect.actSongList.flattenList(OpenTaiko.Songs管理.list曲ルート);
			foreach (var node in allNodes)
			{
				if (node.nodeType == CSongListNode.ENodeType.SCORE || node.nodeType == CSongListNode.ENodeType.SCORE_MIDI)
				{
					m_AllSongs.Add(node);
				}
			}

			m_NeedsRefresh = true;
		}

		private void ApplyFilters()
		{
			m_FilteredSongs.Clear();

			ParseFilterText(m_FilterText, out var filters, out string titleSearch);

			foreach (var song in m_AllSongs)
			{
				CScore score = song.score[m_SelectedDifficulty];
				if (score == null) continue;

				int level = song.nLevel[m_SelectedDifficulty];
				if (level < 0) continue;

				if (!string.IsNullOrEmpty(titleSearch))
				{
					string title = song.ldTitle.GetString("").ToLowerInvariant();
					if (!title.Contains(titleSearch)) continue;
				}

				if (!PassesAllFilters(song, score, level, filters)) continue;

				m_FilteredSongs.Add(song);
			}
		}

		private void ParseFilterText(string text, out List<(string field, string op, string value)> filters, out string titleSearch)
		{
			filters = new();
			string remaining = text;

			foreach (Match match in FilterTokenRegex.Matches(text))
			{
				filters.Add((match.Groups[1].Value.ToLowerInvariant(), match.Groups[2].Value, match.Groups[3].Value));
				remaining = remaining.Replace(match.Value, "");
			}

			titleSearch = remaining.Trim().ToLowerInvariant();
		}

		private bool PassesAllFilters(CSongListNode song, CScore score, int level, List<(string field, string op, string value)> filters)
		{
			foreach (var (field, op, value) in filters)
			{
				double? songValue = GetFieldValue(song, score, level, field);
				double? targetValue = ResolveValue(field, value);

				if (songValue == null || targetValue == null) continue;

				if (!CompareValues(songValue.Value, op, targetValue.Value)) return false;
			}
			return true;
		}

		private double? GetFieldValue(CSongListNode song, CScore score, int level, string field)
		{
			return field switch
			{
				"bpm" => score.譜面情報.BaseBpm,
				"level" or "lv" => level,
				"badge" or "rank" => Utilities.SongTable.GetScoreRank(song, m_SelectedDifficulty),
				"fc" or "clear" => GetClearStatus(score, m_SelectedDifficulty),
				_ => null,
			};
		}

		private static double? ResolveValue(string field, string value)
		{
			if (double.TryParse(value, out double numeric))
			{
				return numeric;
			}

			if ((field == "badge" || field == "rank") && BadgeNames.TryGetValue(value, out int badgeVal))
			{
				return badgeVal;
			}

			if ((field == "fc" || field == "clear") && ClearNames.TryGetValue(value, out int clearVal))
			{
				return clearVal;
			}

			return null;
		}

		private static bool CompareValues(double songValue, string op, double target)
		{
			return op switch
			{
				">" => songValue > target,
				"<" => songValue < target,
				">=" => songValue >= target,
				"<=" => songValue <= target,
				"=" => Math.Abs(songValue - target) < 0.001,
				_ => true,
			};
		}

		private static int GetClearStatus(CScore score, int difficulty)
		{
			int[] clears = score.譜面情報.nクリア;
			if (clears != null && difficulty < clears.Length)
			{
				return clears[difficulty];
			}
			return 0;
		}

	}
}
