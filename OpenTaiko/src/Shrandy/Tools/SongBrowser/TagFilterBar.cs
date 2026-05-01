using System;
using System.Collections.Generic;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class TagFilterBar
	{
		private readonly SongTagsData m_Tags;
		private readonly Func<string> m_GetFilterText;
		private readonly Action<string> m_SetFilterText;

		public TagFilterBar(SongTagsData tags, Func<string> getFilterText, Action<string> setFilterText)
		{
			m_Tags = tags;
			m_GetFilterText = getFilterText;
			m_SetFilterText = setFilterText;
		}

		public void Draw()
		{
			List<SongTag> allTags = m_Tags.GetAllTags();
			if (allTags.Count == 0)
			{
				return;
			}

			DrawTagRow("Include tags:", allTags, include: true);
			DrawTagRow("Exclude tags:", allTags, include: false);
		}

		private void DrawTagRow(string label, List<SongTag> tags, bool include)
		{
			ImGui.Text(label);
			ImGui.SameLine();

			for (int i = 0; i < tags.Count; i++)
			{
				if (i > 0)
				{
					ImGui.SameLine();
				}

				DrawTagButton(tags[i].Name, include);
			}
		}

		private void DrawTagButton(string tagName, bool include)
		{
			string filterText = m_GetFilterText();
			string token = include ? BuildIncludeToken(tagName) : BuildExcludeToken(tagName);
			bool isActive = ContainsFilterToken(filterText, token);

			if (isActive)
			{
				ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
			}

			string buttonId = include ? tagName : $"{tagName}##excl";
			if (ImGui.SmallButton(buttonId))
			{
				string newFilterText = isActive
					? RemoveFilterToken(filterText, token)
					: AppendFilterToken(filterText, token);
				m_SetFilterText(newFilterText);
			}

			if (isActive)
			{
				ImGui.PopStyleColor();
			}
		}

		private static string BuildIncludeToken(string tagName)
		{
			return $"tag={tagName}";
		}

		private static string BuildExcludeToken(string tagName)
		{
			return $"tag!={tagName}";
		}

		private static bool ContainsFilterToken(string filterText, string token)
		{
			int searchIndex = 0;
			while (true)
			{
				int matchIndex = filterText.IndexOf(token, searchIndex, StringComparison.OrdinalIgnoreCase);
				if (matchIndex < 0)
				{
					return false;
				}

				int endIndex = matchIndex + token.Length;
				bool isTerminated = endIndex >= filterText.Length || char.IsWhiteSpace(filterText[endIndex]);
				if (isTerminated)
				{
					return true;
				}

				searchIndex = matchIndex + 1;
			}
		}

		private static string RemoveFilterToken(string filterText, string token)
		{
			int searchIndex = 0;
			while (true)
			{
				int matchIndex = filterText.IndexOf(token, searchIndex, StringComparison.OrdinalIgnoreCase);
				if (matchIndex < 0)
				{
					return filterText.Trim();
				}

				int endIndex = matchIndex + token.Length;
				bool isTerminated = endIndex >= filterText.Length || char.IsWhiteSpace(filterText[endIndex]);
				if (isTerminated)
				{
					return (filterText.Substring(0, matchIndex) + filterText.Substring(endIndex)).Trim();
				}

				searchIndex = matchIndex + 1;
			}
		}

		private static string AppendFilterToken(string filterText, string token)
		{
			if (string.IsNullOrWhiteSpace(filterText))
			{
				return token;
			}

			return filterText.TrimEnd() + " " + token;
		}
	}
}
