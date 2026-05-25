using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class PatternToolUI
	{
		private PatternTool m_Tool;

		private int m_SelectedIndex = -1;
		private string m_TitleInput = "";
		private string m_TJAInput = "";
		private bool m_EditIsNew = false;

		private const string EditPopupId = "Edit Pattern";
		private const string DeletePopupId = "Delete Pattern?";

		public PatternToolUI(PatternTool tool)
		{
			m_Tool = tool;
		}

		public void Draw()
		{
			if (m_Tool.IsActive())
			{
				DrawPatternEditor();
			}
			else
			{
				DrawEnterGameButton();
			}
		}

		private void DrawEnterGameButton()
		{
			if (ImGui.Button("Enter Pattern Mode"))
			{
				m_Tool.EnterPatternMode();
			}
		}

		private void DrawPatternEditor()
		{
			var patterns = m_Tool.Database.Patterns;

			ImGui.SeparatorText("Patterns");

			var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
				| ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit;
			if (ImGui.BeginTable("##PatternTable", 4, tableFlags, new Vector2(0, 180)))
			{
				ImGui.TableSetupScrollFreeze(0, 1);
				ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthFixed, 120);
				ImGui.TableSetupColumn("TJA", ImGuiTableColumnFlags.WidthStretch);
				ImGui.TableSetupColumn("##play_col", ImGuiTableColumnFlags.WidthFixed, 40);
				ImGui.TableSetupColumn("##edit_col", ImGuiTableColumnFlags.WidthFixed, 40);
				ImGui.TableHeadersRow();

				for (int i = 0; i < patterns.Count; i++)
				{
					ImGui.TableNextRow();

					ImGui.TableSetColumnIndex(0);
					bool selected = m_SelectedIndex == i;
					string title = patterns[i].Title.Length > 0 ? patterns[i].Title : "(unnamed)";
					if (ImGui.Selectable(title + $"##row{i}", selected,
						ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap))
					{
						m_SelectedIndex = i;
					}

					ImGui.TableSetColumnIndex(1);
					string tja = patterns[i].TJA;
					string preview = tja.Length > 32 ? tja[..32] + "..." : tja;
					ImGui.TextUnformatted(preview);

					ImGui.TableSetColumnIndex(2);
					if (ImGui.SmallButton($"Play##p{i}"))
					{
						m_Tool.PlayPattern(patterns[i]);
					}

					ImGui.TableSetColumnIndex(3);
					if (ImGui.SmallButton($"Edit##e{i}"))
					{
						m_SelectedIndex = i;
						m_TitleInput = patterns[i].Title;
						m_TJAInput = patterns[i].TJA;
						m_EditIsNew = false;
						ImGui.OpenPopup(EditPopupId);
					}
				}

				ImGui.EndTable();
			}

			bool hasSelection = m_SelectedIndex >= 0 && m_SelectedIndex < patterns.Count;

			if (ImGui.Button("Add"))
			{
				m_TitleInput = "";
				m_TJAInput = "";
				m_EditIsNew = true;
				ImGui.OpenPopup(EditPopupId);
			}

			if (hasSelection)
			{
				ImGui.SameLine();
				if (ImGui.Button("Delete"))
				{
					ImGui.OpenPopup(DeletePopupId);
				}
			}

			ImGui.SameLine();
			if (ImGui.Button("Save"))
			{
				m_Tool.SaveDatabase();
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

				ImGui.InputText("Title", ref m_TitleInput, 256);
				ImGui.InputTextMultiline("TJA", ref m_TJAInput, 8192, new Vector2(400, 200));

				bool canApply = m_TitleInput.Length > 0;
				if (!canApply)
				{
					ImGui.BeginDisabled();
				}

				if (ImGui.Button("OK") || (canApply && ImGui.IsKeyPressed(ImGuiKey.Enter)))
				{
					ApplyChanges();
					ImGui.CloseCurrentPopup();
				}

				if (!canApply)
				{
					ImGui.EndDisabled();
				}

				ImGui.SameLine();
				if (ImGui.Button("Cancel"))
				{
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
		}

		private void DrawDeletePopup()
		{
			bool open = true;
			if (ImGui.BeginPopupModal(DeletePopupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
			{
				var patterns = m_Tool.Database.Patterns;
				string name = (m_SelectedIndex >= 0 && m_SelectedIndex < patterns.Count)
					? patterns[m_SelectedIndex].Title
					: "";
				ImGui.Text($"Delete \"{name}\"?");
				ImGui.Separator();

				if (ImGui.Button("Yes"))
				{
					m_Tool.Database.RemovePattern(patterns[m_SelectedIndex]);
					m_SelectedIndex = -1;
					ImGui.CloseCurrentPopup();
				}
				ImGui.SameLine();
				if (ImGui.Button("Cancel"))
				{
					ImGui.CloseCurrentPopup();
				}
				ImGui.EndPopup();
			}
		}

		private void ApplyChanges()
		{
			var patterns = m_Tool.Database.Patterns;
			if (m_EditIsNew)
			{
				m_Tool.Database.AddPattern(new PatternData { Title = m_TitleInput, TJA = m_TJAInput });
				m_SelectedIndex = patterns.Count - 1;
			}
			else if (m_SelectedIndex >= 0 && m_SelectedIndex < patterns.Count)
			{
				patterns[m_SelectedIndex].Title = m_TitleInput;
				patterns[m_SelectedIndex].TJA = m_TJAInput;
			}
		}
	}
}
