using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class DrillEditorUI
	{
		private PatternTool m_Tool;

		private int m_SelectedIndex = -1;
		private string m_TitleInput = "";
		private List<(PatternData Pattern, int Weight)> m_StagedPatterns = new();
		private bool m_EditIsNew = false;
		private bool m_PendingEditOpen = false;
		private int m_DrillCount = 100;

		private const string EditPopupId = "Edit Drill";
		private const string DeletePopupId = "Delete Drill?";

		public DrillEditorUI(PatternTool tool)
		{
			m_Tool = tool;
		}

		public void Draw()
		{
			List<DrillData> drills = m_Tool.Database.Drills;

			ImGui.SeparatorText("Drills");

			ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
				| ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit;
			if (ImGui.BeginTable("##DrillTable", 3, tableFlags, new Vector2(0, 150)))
			{
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("##dplay_col", ImGuiTableColumnFlags.WidthFixed, 40);
				ImGui.TableSetupColumn("##dedit_col", ImGuiTableColumnFlags.WidthFixed, 40);
				ImGui.TableHeadersRow();

				for (int i = 0; i < drills.Count; i++)
				{
					ImGui.TableNextRow();

					ImGui.TableSetColumnIndex(0);
					bool selected = m_SelectedIndex == i;
					string title = drills[i].Title.Length > 0 ? drills[i].Title : "(unnamed)";
					if (ImGui.Selectable(title + $"##drow{i}", selected,
						ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap))
					{
						m_SelectedIndex = i;
					}

					ImGui.TableSetColumnIndex(1);
					if (ImGui.SmallButton($"Play##dplay{i}"))
					{
						m_Tool.PlayDrill(drills[i], m_DrillCount);
					}

					ImGui.TableSetColumnIndex(2);
					if (ImGui.SmallButton($"Edit##dedit{i}"))
					{
						m_SelectedIndex = i;
						OpenEditPopup(drills[i]);
					}
				}

				ImGui.EndTable();
			}

			if (m_PendingEditOpen)
			{
				ImGui.OpenPopup(EditPopupId);
				m_PendingEditOpen = false;
			}

			bool hasSelection = m_SelectedIndex >= 0 && m_SelectedIndex < drills.Count;

			if (ImGui.Button("Add##dadd"))
			{
				m_TitleInput = "";
				m_StagedPatterns.Clear();
				m_EditIsNew = true;
				ImGui.OpenPopup(EditPopupId);
			}

			if (hasSelection)
			{
				ImGui.SameLine();
				if (ImGui.Button("Delete##ddel"))
				{
					ImGui.OpenPopup(DeletePopupId);
				}
			}

			ImGui.SameLine();
			ImGui.SetNextItemWidth(60);
			ImGui.DragInt("Count##dcount", ref m_DrillCount, 1, 1, 1000);

			DrawEditPopup();
			DrawDeletePopup();
		}

		private void OpenEditPopup(DrillData drill)
		{
			m_TitleInput = drill.Title;
			m_StagedPatterns = drill.Patterns
				.Select(pw => (pw.Pattern, pw.Weight))
				.ToList();
			m_EditIsNew = false;
			m_PendingEditOpen = true;
		}

		private void DrawEditPopup()
		{
			bool open = true;
			if (ImGui.BeginPopupModal(EditPopupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
			{
				ImGui.InputText("Title##dtitle", ref m_TitleInput, 256);
				ImGui.Spacing();

				List<PatternData> allPatterns = m_Tool.Database.Patterns;
				HashSet<PatternData> includedSet = m_StagedPatterns.Select(s => s.Pattern).ToHashSet();
				List<PatternData> excluded = allPatterns.Where(p => !includedSet.Contains(p)).ToList();

				ImGuiTableFlags innerFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
					| ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit;

				// Two-column outer layout
				if (ImGui.BeginTable("##DrillEditOuter", 2, ImGuiTableFlags.None))
				{
					ImGui.TableSetupColumn("##dleft", ImGuiTableColumnFlags.WidthFixed, 220);
					ImGui.TableSetupColumn("##dright", ImGuiTableColumnFlags.WidthFixed, 300);
					ImGui.TableNextRow();

					// Available patterns (left)
					ImGui.TableSetColumnIndex(0);
					ImGui.TextUnformatted("Available");
					if (ImGui.BeginTable("##AvailableTable", 2, innerFlags, new Vector2(220, 200)))
					{
						ImGui.TableSetupColumn("Pattern", ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableSetupColumn("##add", ImGuiTableColumnFlags.WidthFixed, 28);
						ImGui.TableHeadersRow();

						for (int i = 0; i < excluded.Count; i++)
						{
							PatternData pattern = excluded[i];
							ImGui.TableNextRow();
							ImGui.TableSetColumnIndex(0);
							ImGui.TextUnformatted(pattern.Title.Length > 0 ? pattern.Title : "(unnamed)");
							ImGui.TableSetColumnIndex(1);
							if (ImGui.SmallButton($"+##add{i}"))
							{
								m_StagedPatterns.Add((pattern, 1));
							}
						}

						ImGui.EndTable();
					}

					// Included patterns (right)
					ImGui.TableSetColumnIndex(1);
					ImGui.TextUnformatted("Included");
					if (ImGui.BeginTable("##IncludedTable", 3, innerFlags, new Vector2(300, 200)))
					{
						ImGui.TableSetupColumn("Pattern", ImGuiTableColumnFlags.WidthStretch);
						ImGui.TableSetupColumn("Weight", ImGuiTableColumnFlags.WidthFixed, 58);
						ImGui.TableSetupColumn("##rm", ImGuiTableColumnFlags.WidthFixed, 24);
						ImGui.TableHeadersRow();

						int removeIdx = -1;
						for (int i = 0; i < m_StagedPatterns.Count; i++)
						{
							(PatternData pattern, int weight) = m_StagedPatterns[i];
							ImGui.TableNextRow();
							ImGui.TableSetColumnIndex(0);
							ImGui.TextUnformatted(pattern.Title.Length > 0 ? pattern.Title : "(unnamed)");
							ImGui.TableSetColumnIndex(1);
							ImGui.SetNextItemWidth(54);
							int adjustedWeight = weight;
							if (ImGui.DragInt($"##iw{i}", ref adjustedWeight, 1, 1, 100))
							{
								m_StagedPatterns[i] = (pattern, Math.Max(1, adjustedWeight));
							}
							ImGui.TableSetColumnIndex(2);
							if (ImGui.SmallButton($"x##irm{i}"))
							{
								removeIdx = i;
							}
						}
						if (removeIdx >= 0)
						{
							m_StagedPatterns.RemoveAt(removeIdx);
						}

						ImGui.EndTable();
					}

					ImGui.EndTable();
				}

				ImGui.Spacing();
				bool canApply = m_TitleInput.Length > 0;
				if (!canApply)
				{
					ImGui.BeginDisabled();
				}

				if (ImGui.Button("OK##dok"))
				{
					ApplyDrillChanges();
					ImGui.CloseCurrentPopup();
				}

				if (!canApply)
				{
					ImGui.EndDisabled();
				}

				ImGui.SameLine();
				if (ImGui.Button("Cancel##dcancel"))
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
		}

		private void DrawDeletePopup()
		{
			List<DrillData> drills = m_Tool.Database.Drills;
			bool open = true;
			if (ImGui.BeginPopupModal(DeletePopupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
			{
				string name = (m_SelectedIndex >= 0 && m_SelectedIndex < drills.Count)
					? drills[m_SelectedIndex].Title : "";
				ImGui.Text($"Delete \"{name}\"?");
				ImGui.Separator();

				if (ImGui.Button("Yes##dyes"))
				{
					m_Tool.Database.RemoveDrill(drills[m_SelectedIndex]);
					m_SelectedIndex = -1;
					m_Tool.SaveDatabase();
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel##ddcancel"))
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
		}

		private void ApplyDrillChanges()
		{
			List<DrillData> drills = m_Tool.Database.Drills;
			List<DrillData.PatternWeight> newPatterns = m_StagedPatterns
				.Select(s => new DrillData.PatternWeight { Pattern = s.Pattern, Weight = s.Weight })
				.ToList();

			if (m_EditIsNew)
			{
				m_Tool.Database.AddDrill(new DrillData { Title = m_TitleInput, Patterns = newPatterns });
				m_SelectedIndex = drills.Count - 1;
			}
			else if (m_SelectedIndex >= 0 && m_SelectedIndex < drills.Count)
			{
				drills[m_SelectedIndex].Title = m_TitleInput;
				drills[m_SelectedIndex].Patterns = newPatterns;
			}
			m_Tool.SaveDatabase();
		}
	}
}
