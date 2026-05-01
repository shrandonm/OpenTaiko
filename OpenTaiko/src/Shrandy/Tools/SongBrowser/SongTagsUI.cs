using System;
using System.Collections.Generic;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class SongTagsUI
	{
		private readonly SongTagsData m_Tags;
		private readonly Action m_SaveCallback;

		private const string PopupId = "##tags_edit_popup";

		private string m_EditTitle = "";
		private int m_EditDifficulty = -1;
		private string m_NewTagInput = "";
		private bool m_PendingOpen = false;

		public SongTagsUI(SongTagsData tags, Action saveCallback)
		{
			m_Tags = tags;
			m_SaveCallback = saveCallback;
		}

		public void DrawCell(string title, int difficulty, int rowId)
		{
			ImGui.TableSetColumnIndex(Utilities.SongTable.TagsColumnIndex);

			List<SongTag> tags = m_Tags.GetTagsForSong(title, difficulty);
			if (tags.Count > 0)
			{
				ImGui.TextUnformatted(string.Join(", ", tags.ConvertAll(t => t.Name)));
				ImGui.SameLine();
			}

			bool openPopup = false;
			ImGui.PushID(rowId);
			if (ImGui.SmallButton("+"))
			{
				m_EditTitle = title;
				m_EditDifficulty = difficulty;
				m_NewTagInput = "";
				openPopup = true;
			}
			ImGui.PopID();

			if (openPopup)
			{
				m_PendingOpen = true;
			}
		}

		public void DrawPopup()
		{
			if (m_PendingOpen)
			{
				ImGui.OpenPopup(PopupId);
				m_PendingOpen = false;
			}

			if (!ImGui.BeginPopup(PopupId))
			{
				return;
			}

			if (m_EditDifficulty < 0)
			{
				ImGui.EndPopup();
				return;
			}

			ImGui.SeparatorText($"Tags: {m_EditTitle}");

			// New tag creation
			ImGui.SetNextItemWidth(180);
			ImGui.InputTextWithHint("##newtag", "New tag name...", ref m_NewTagInput, 64);
			bool submitByEnter = ImGui.IsItemDeactivatedAfterEdit();
			ImGui.SameLine();
			bool addClicked = ImGui.Button("Add");

			if ((addClicked || submitByEnter) && !string.IsNullOrWhiteSpace(m_NewTagInput))
			{
				m_Tags.AddTag(m_EditTitle, m_EditDifficulty, m_NewTagInput.Trim());
				m_SaveCallback();
				m_NewTagInput = "";
			}

			// Existing tags as checkboxes
			List<SongTag> allTags = m_Tags.GetAllTags();
			if (allTags.Count > 0)
			{
				ImGui.Separator();
				foreach (SongTag tag in allTags)
				{
					bool hasTag = m_Tags.SongHasTag(m_EditTitle, m_EditDifficulty, tag.Name);
					bool prev = hasTag;
					ImGui.Checkbox(tag.Name, ref hasTag);
					if (hasTag != prev)
					{
						if (hasTag)
						{
							m_Tags.AddTag(m_EditTitle, m_EditDifficulty, tag.Name);
						}
						else
						{
							m_Tags.RemoveTag(m_EditTitle, m_EditDifficulty, tag.Name);
						}
						m_SaveCallback();
					}
				}
			}

			ImGui.EndPopup();
		}
	}
}
