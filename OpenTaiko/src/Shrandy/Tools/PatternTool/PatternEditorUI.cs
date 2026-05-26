using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternEditorUI
	{
		private PatternTool m_Tool;

		private int m_SelectedIndex = -1;
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
			if (ImGui.BeginPopupModal(MainPopupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
			{
				DrawContents();
				ImGui.Spacing();
				if (ImGui.Button("Close##pmclose"))
				{
					ImGui.CloseCurrentPopup();
				}
				ImGui.EndPopup();
			}
		}

		private void DrawContents()
		{
			List<PatternData> patterns = m_Tool.Database.Patterns;

			ImGui.SeparatorText("Patterns");

			ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
				| ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit;
			if (ImGui.BeginTable("##PatternTable", 3, tableFlags, new Vector2(0, 180)))
			{
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableSetupColumn("TJA", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("##edit_col", ImGuiTableColumnFlags.WidthFixed, 40);
				ImGui.TableHeadersRow();

				for (int i = 0; i < patterns.Count; i++)
				{
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
					string tja = patterns[i].TJA;
					string preview = tja.Length > 32 ? tja[..32] + "..." : tja;
					ImGui.TextUnformatted(preview);

					ImGui.TableSetColumnIndex(2);
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

			DrawEditPopup();
			DrawDeletePopup();
		}

		private void DrawEditPopup()
		{
			bool open = true;
			if (ImGui.BeginPopupModal(EditPopupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
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
				ImGui.InputTextMultiline("TJA##ptja", ref m_TJAInput, 8192, new Vector2(400, 200));

				bool canApply = m_TitleInput.Length > 0;
				if (!canApply)
				{
					ImGui.BeginDisabled();
				}

				if (ImGui.Button("OK##pok") || (canApply && ImGui.IsKeyPressed(ImGuiKey.Enter)))
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
				m_Tool.Database.AddPattern(new PatternData { Title = m_TitleInput, TJA = m_TJAInput });
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
