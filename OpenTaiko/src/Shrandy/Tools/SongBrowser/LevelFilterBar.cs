using System;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class LevelFilterBar
	{
		private readonly struct LevelFilter
		{
			public string Label { get; init; }
			public string Token { get; init; }
		}

		private static readonly LevelFilter[] s_LevelFilters =
		{
			new() { Label = "1-5", Token = "level<6"  },
			new() { Label = "6",   Token = "level=6"  },
			new() { Label = "7",   Token = "level=7"  },
			new() { Label = "8",   Token = "level=8"  },
			new() { Label = "9",   Token = "level=9"  },
			new() { Label = "10",  Token = "level=10" },
		};

		private readonly Func<string> m_GetFilterText;
		private readonly Action<string> m_SetFilterText;

		public LevelFilterBar(Func<string> getFilterText, Action<string> setFilterText)
		{
			m_GetFilterText = getFilterText;
			m_SetFilterText = setFilterText;
		}

		public void Draw()
		{
			ImGui.Text("Level:");
			ImGui.SameLine();

			for (int i = 0; i < s_LevelFilters.Length; i++)
			{
				if (i > 0)
				{
					ImGui.SameLine();
				}

				LevelFilter filter = s_LevelFilters[i];
				DrawLevelButton(filter.Label, filter.Token, i);
			}
		}

		private void DrawLevelButton(string label, string token, int index)
		{
			string filterText = m_GetFilterText();
			bool isActive = ContainsToken(filterText, token);

			if (isActive)
			{
				ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));
			}

			if (ImGui.SmallButton($"{label}##lvl{index}"))
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

		private static string RemoveToken(string filterText, string token)
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
