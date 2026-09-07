using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class GenreFilterBar
	{
		private readonly Func<List<CSongListNode>> m_GetSongs;
		private readonly Func<string> m_GetFilterText;
		private readonly Action<string> m_SetFilterText;

		public GenreFilterBar(
			Func<List<CSongListNode>> getSongs,
			Func<string> getFilterText,
			Action<string> setFilterText)
		{
			m_GetSongs = getSongs;
			m_GetFilterText = getFilterText;
			m_SetFilterText = setFilterText;
		}

		public void Draw()
		{
			List<string> genres = m_GetSongs()
				.Select(song => song.songGenre)
				.Where(genre => !string.IsNullOrWhiteSpace(genre))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.OrderBy(genre => genre, StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (genres.Count == 0)
			{
				return;
			}

			ImGui.Text("Genre:");
			ImGui.SameLine();
			for (int i = 0; i < genres.Count; i++)
			{
				if (i > 0)
				{
					ImGui.SameLine();
				}

				DrawGenreButton(genres[i], i);
			}
		}

		private void DrawGenreButton(string genre, int index)
		{
			string token = $"genre={genre}";
			string filterText = m_GetFilterText();
			bool isActive = ContainsToken(filterText, token);

			if (isActive)
			{
				ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
			}

			if (ImGui.SmallButton($"{genre}##genre{index}"))
			{
				string newFilterText = isActive
					? RemoveToken(filterText, token)
					: AppendToken(filterText, token);
				m_SetFilterText(newFilterText);
			}

			if (isActive)
			{
				ImGui.PopStyleColor();
			}
		}

		private static bool ContainsToken(string filterText, string token)
		{
			foreach (string filterToken in filterText.Split(' ', StringSplitOptions.RemoveEmptyEntries))
			{
				if (filterToken.Equals(token, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private static string RemoveToken(string filterText, string token)
		{
			return string.Join(" ", filterText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
				.Where(filterToken => !filterToken.Equals(token, StringComparison.OrdinalIgnoreCase)));
		}

		private static string AppendToken(string filterText, string token)
		{
			if (string.IsNullOrWhiteSpace(filterText))
			{
				return token;
			}

			return filterText.TrimEnd() + " " + token;
		}
	}
}