using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternEditorUI
	{
		private PatternTool m_Tool;

		private int m_SelectedIndex = -1;
		private PatternFilter m_Filter = new();
		private string m_TitleInput = "";
		private string m_TJAInput = "";
		private bool m_EditIsNew = false;
		private bool m_EditIsBuiltIn = false;
		private bool m_PendingEditOpen = false;
		private bool m_MainPendingOpen = false;

		private const string MainPopupId = "Pattern Editor";
		private const string EditPopupId = "Edit Pattern";
		private const string DeletePopupId = "Delete Pattern?";

		public PatternEditorUI(PatternTool tool)
		{
			m_Tool = tool;
		}

		public void Open()
		{
			m_MainPendingOpen = true;
		}

		public void Draw()
		{
			if (m_MainPendingOpen)
			{
				ImGui.OpenPopup(MainPopupId);
				m_MainPendingOpen = false;
			}

			bool open = true;
			if (ImGui.BeginPopupModal(MainPopupId, ref open, ImGuiWindowFlags.None))
			{
				DrawContents();
				ImGui.EndPopup();
			}
		}

		private void DrawContents()
		{
			List<PatternData> patterns = m_Tool.Database.Patterns;

			ImGui.SeparatorText("Patterns");
			m_Filter.Draw("patternfilter");

			ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
				| ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Resizable;
			float buttonRowHeight = ImGui.GetFrameHeightWithSpacing();
			float tableHeight = ImGui.GetContentRegionAvail().Y - buttonRowHeight;
			int moveFromIndex = -1;
			int moveToIndex = -1;
			if (ImGui.BeginTable("##PatternTable", 4, tableFlags, new Vector2(0, tableHeight)))
			{
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthFixed, 200);
				ImGui.TableSetupColumn("Pattern", ImGuiTableColumnFlags.WidthFixed, PatternBarVisualizer.PreviewWidth);
				ImGui.TableSetupColumn("##order_col", ImGuiTableColumnFlags.WidthFixed, 60);
				ImGui.TableSetupColumn("##edit_col", ImGuiTableColumnFlags.WidthFixed, 40);
				ImGui.TableHeadersRow();

				for (int i = 0; i < patterns.Count; i++)
				{
					if (!m_Filter.Matches(patterns[i]))
					{
						continue;
					}

					ImGui.TableNextRow();

					ImGui.TableSetColumnIndex(0);
					bool selected = m_SelectedIndex == i;
					string title = patterns[i].Title.Length > 0 ? patterns[i].Title : "(unnamed)";
					if (ImGui.Selectable(title + $"##prow{i}", selected,
						ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap))
					{
						m_SelectedIndex = i;
					}

					ImGui.TableSetColumnIndex(1);
					PatternBarVisualizer.DrawInline(patterns[i].TJA, PatternBarVisualizer.PreviewWidth, PatternBarVisualizer.DefaultHeight);

					ImGui.TableSetColumnIndex(2);
					if (ImGui.ArrowButton($"##pup{i}", ImGuiDir.Up))
					{
						moveFromIndex = i;
						moveToIndex = FindVisibleNeighbor(patterns, i, -1);
					}
					ImGui.SameLine();
					if (ImGui.ArrowButton($"##pdown{i}", ImGuiDir.Down))
					{
						moveFromIndex = i;
						moveToIndex = FindVisibleNeighbor(patterns, i, 1);
					}

					ImGui.TableSetColumnIndex(3);
					if (ImGui.SmallButton($"Edit##pe{i}"))
					{
						m_SelectedIndex = i;
						m_TitleInput = patterns[i].Title;
						m_TJAInput = patterns[i].TJA;
						m_EditIsNew = false;
						m_EditIsBuiltIn = PatternDatabase.IsBuiltIn(patterns[i]);
						m_PendingEditOpen = true;
					}
				}

				ImGui.EndTable();
			}

			MovePattern(patterns, moveFromIndex, moveToIndex);

			if (m_PendingEditOpen)
			{
				ImGui.OpenPopup(EditPopupId);
				m_PendingEditOpen = false;
			}

			bool hasSelection = m_SelectedIndex >= 0 && m_SelectedIndex < patterns.Count;

			if (ImGui.Button("Add##padd"))
			{
				m_TitleInput = "";
				m_TJAInput = "";
				m_EditIsNew = true;
				m_EditIsBuiltIn = false;
				ImGui.OpenPopup(EditPopupId);
			}

			if (hasSelection)
			{
				ImGui.SameLine();
				bool selectedIsBuiltIn = PatternDatabase.IsBuiltIn(patterns[m_SelectedIndex]);
				if (selectedIsBuiltIn)
				{
					ImGui.BeginDisabled();
				}
				if (ImGui.Button("Delete##pdel"))
				{
					ImGui.OpenPopup(DeletePopupId);
				}
				if (selectedIsBuiltIn)
				{
					ImGui.EndDisabled();
				}
			}

			ImGui.SameLine();
			if (ImGui.Button("Close##pmclose"))
			{
				ImGui.CloseCurrentPopup();
			}

			DrawEditPopup();
			DrawDeletePopup();
		}

		private int FindVisibleNeighbor(List<PatternData> patterns, int index, int direction)
		{
			for (int i = index + direction; i >= 0 && i < patterns.Count; i += direction)
			{
				if (m_Filter.Matches(patterns[i]))
				{
					return i;
				}
			}
			return -1;
		}

		private void MovePattern(List<PatternData> patterns, int fromIndex, int toIndex)
		{
			if (fromIndex < 0 || toIndex < 0)
			{
				return;
			}

			PatternData movedPattern = patterns[fromIndex];
			patterns[fromIndex] = patterns[toIndex];
			patterns[toIndex] = movedPattern;
			if (m_SelectedIndex == fromIndex)
			{
				m_SelectedIndex = toIndex;
			}
			else if (m_SelectedIndex == toIndex)
			{
				m_SelectedIndex = fromIndex;
			}
			m_Tool.SaveDatabase();
		}

		private void DrawEditPopup()
		{
			bool open = true;
			if (ImGui.BeginPopupModal(EditPopupId, ref open, ImGuiWindowFlags.None))
			{
				if (ImGui.IsWindowAppearing())
				{
					ImGui.SetKeyboardFocusHere();
				}

				if (m_EditIsBuiltIn)
				{
					ImGui.BeginDisabled();
				}
					ImGui.InputText("Title##ptitle", ref m_TitleInput, 256);
				if (m_EditIsBuiltIn)
				{
					ImGui.EndDisabled();
				}
				ImGui.SeparatorText("Preview");
				PatternBarVisualizer.DrawInline(m_TJAInput, PatternBarVisualizer.PreviewWidth, PatternBarVisualizer.DefaultHeight);
				ImGui.InputTextMultiline("TJA##ptja", ref m_TJAInput, 8192, new Vector2(400, 200));
				bool isTjaInputActive = ImGui.IsItemActive();
				bool canApply = m_TitleInput.Length > 0;
				if (!canApply)
				{
					ImGui.BeginDisabled();
				}

				if (ImGui.Button("OK##pok") || (canApply && !isTjaInputActive && ImGui.IsKeyPressed(ImGuiKey.Enter)))
				{
					ApplyChanges();
					ImGui.CloseCurrentPopup();
				}

				if (!canApply)
				{
					ImGui.EndDisabled();
				}

				ImGui.SameLine();
				if (ImGui.Button("Cancel##pcancel"))
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
		}

		private void DrawDeletePopup()
		{
			List<PatternData> patterns = m_Tool.Database.Patterns;
			bool open = true;
			if (ImGui.BeginPopupModal(DeletePopupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
			{
				string name = (m_SelectedIndex >= 0 && m_SelectedIndex < patterns.Count)
					? patterns[m_SelectedIndex].Title : "";
				ImGui.Text($"Delete \"{name}\"?");
				ImGui.Separator();

				if (ImGui.Button("Yes##pyes"))
				{
					m_Tool.Database.RemovePattern(patterns[m_SelectedIndex]);
					m_SelectedIndex = -1;
					m_Tool.SaveDatabase();
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel##pdcancel"))
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
		}

		private void ApplyChanges()
		{
			List<PatternData> patterns = m_Tool.Database.Patterns;
			if (m_EditIsNew)
			{
				m_Tool.Database.Patterns.Add(new PatternData { Title = m_TitleInput, TJA = m_TJAInput });
				m_SelectedIndex = patterns.Count - 1;
			}
			else if (m_SelectedIndex >= 0 && m_SelectedIndex < patterns.Count)
			{
				string oldTitle = patterns[m_SelectedIndex].Title;
				patterns[m_SelectedIndex].Title = m_TitleInput;
				patterns[m_SelectedIndex].TJA = m_TJAInput;
				if (oldTitle != m_TitleInput)
				{
					m_Tool.Database.PropagatePatternRename(oldTitle, m_TitleInput);
				}
			}
			m_Tool.SaveDatabase();
		}
	}
}
