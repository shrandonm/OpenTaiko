using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class ResultsTool : Tool
	{
		private static readonly Vector4 DeltaPositiveColor = new Vector4(0.2f, 0.9f, 0.2f, 1.0f);
		private static readonly Vector4 DeltaNegativeColor = new Vector4(0.9f, 0.2f, 0.2f, 1.0f);

		public ResultsTool(string toolName = "Results", SlimDXKeys.Key enableHotkey = SlimDXKeys.Key.Unknown)
			: base(toolName, enableHotkey)
		{
		}

		public override bool IsBlockingInput()
		{
			return false;
		}

		public override void OnSongComplete()
		{
			base.OnSongComplete();
			SetEnabled(true);
		}

		public override void OnResultsActivate(CStage結果 resultsScreen)
		{
			base.OnResultsActivate(resultsScreen);
			SetEnabled(true);
		}

		public override void OnStageChanged(CStage stage)
		{
			if (stage is not CStage結果 && stage is not CStage曲読み込み)
			{
				SetEnabled(false);
			}
		}

		protected override void Draw()
		{
			ResultsSnapshot? snapshot = OpenTaiko.ShrandyExtension.GetTool<SongBrowserTool>()?.Data.CurrentResultsSnapshot;
			if (snapshot == null)
			{
				ImGui.Text("No results data.");
				return;
			}

			DrawHeader(snapshot.Value);
			DrawDeltaTable(snapshot.Value);
		}

		private static void DrawHeader(ResultsSnapshot snapshot)
		{
			ImGui.Text(snapshot.CurrentEntry.SongTitle);
			ImGui.SameLine();
			ImGui.TextDisabled($"({snapshot.CurrentEntry.Difficulty})");

			if (snapshot.PreviousBest == null)
			{
				ImGui.TextColored(DeltaPositiveColor, "First play with these mods!");
			}
			else
			{
				ImGui.TextDisabled("vs. Previous Best (matching mods)");
			}
		}

		private void DrawDeltaTable(ResultsSnapshot snapshot)
		{
			SongEntry current = snapshot.CurrentEntry;
			SongEntry? previous = snapshot.PreviousBest;
			SongEntry? noMod = snapshot.NoModBest;
			bool showNoMod = noMod != null;
			int columnCount = showNoMod ? 5 : 4;

			ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
			if (!ImGui.BeginTable("ResultsDelta", columnCount, tableFlags))
			{
				return;
			}

			ImGui.TableSetupColumn("Stat",         ImGuiTableColumnFlags.WidthFixed, 140);
			if (showNoMod) ImGui.TableSetupColumn("No-Mod Best", ImGuiTableColumnFlags.WidthFixed, 110);
			ImGui.TableSetupColumn("Best",         ImGuiTableColumnFlags.WidthFixed, 110);
			ImGui.TableSetupColumn("Current",      ImGuiTableColumnFlags.WidthFixed, 110);
			ImGui.TableSetupColumn("Difference",   ImGuiTableColumnFlags.WidthFixed, 100);
			ImGui.TableHeadersRow();

			DrawScoreRow("Score",     current.Score, current.ScoreRank, previous?.Score, previous?.ScoreRank, noMod?.Score, noMod?.ScoreRank, showNoMod);
			DrawIntRow("Goods",       current.Goods,       previous?.Goods,       noMod?.Goods,       showNoMod, invertColors: false);
			DrawIntRow("Okays",       current.Okays,       previous?.Okays,       noMod?.Okays,       showNoMod, invertColors: true);
			DrawIntRow("Bads",        current.Bads,        previous?.Bads,        noMod?.Bads,        showNoMod, invertColors: true);
			DrawIntRow("Drum Roll",   current.Rolls,       previous?.Rolls,       noMod?.Rolls,       showNoMod, invertColors: false);
			DrawHitErrorRow(current.AvgHitError, previous?.AvgHitError, noMod?.AvgHitError, showNoMod, invertColors: true);

			ImGui.EndTable();
		}

		private static void DrawScoreRow(string label, int currentValue, int currentBadge, int? previousValue, int? previousBadge, int? noModValue, int? noModBadge, bool showNoMod)
		{
			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted(label);

			int col = 1;
			if (showNoMod)
			{
				ImGui.TableSetColumnIndex(col++);
				if (noModValue.HasValue)
				{
					ImGui.TextUnformatted(noModValue.Value.ToString());
					DrawBadge(noModBadge ?? 0);
				}
				else
				{
					ImGui.TextUnformatted("\u2014");
				}
			}

			ImGui.TableSetColumnIndex(col++);
			if (previousValue.HasValue)
			{
				ImGui.TextUnformatted(previousValue.Value.ToString());
				DrawBadge(previousBadge ?? 0);
			}
			else
			{
				ImGui.TextUnformatted("\u2014");
			}

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(currentValue.ToString());
			DrawBadge(currentBadge);

			ImGui.TableSetColumnIndex(col);
			DrawIntDelta(currentValue, previousValue, invertColors: false);
		}

		private static void DrawIntRow(string label, int currentValue, int? previousValue, int? noModValue, bool showNoMod, bool invertColors)
		{
			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted(label);

			int col = 1;
			if (showNoMod)
			{
				ImGui.TableSetColumnIndex(col++);
				ImGui.TextUnformatted(noModValue.HasValue ? noModValue.Value.ToString() : "\u2014");
			}

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(previousValue.HasValue ? previousValue.Value.ToString() : "\u2014");

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(currentValue.ToString());

			ImGui.TableSetColumnIndex(col);
			DrawIntDelta(currentValue, previousValue, invertColors);
		}

		private static void DrawHitErrorRow(float currentValue, float? previousValue, float? noModValue, bool showNoMod, bool invertColors)
		{
			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted("Avg Hit Error");

			int col = 1;
			if (showNoMod)
			{
				ImGui.TableSetColumnIndex(col++);
				ImGui.TextUnformatted(noModValue.HasValue ? $"{noModValue.Value:F1}ms" : "\u2014");
			}

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted(previousValue.HasValue ? $"{previousValue.Value:F1}ms" : "\u2014");

			ImGui.TableSetColumnIndex(col++);
			ImGui.TextUnformatted($"{currentValue:F1}ms");

			ImGui.TableSetColumnIndex(col);
			DrawFloatDelta(currentValue, previousValue, invertColors);
		}

		private static void DrawBadge(int rank)
		{
			if (rank <= 0) return;
			ImGui.SameLine();
			Utilities.SongHelper.DrawScoreRank(rank);
		}

		private static void DrawIntDelta(int currentValue, int? previousValue, bool invertColors)
		{
			if (!previousValue.HasValue)
			{
				ImGui.TextUnformatted("\u2014");
				return;
			}

			int delta = currentValue - previousValue.Value;
			DrawDeltaText(delta, delta.ToString("+0;-0;0"), invertColors);
		}

		private static void DrawFloatDelta(float currentValue, float? previousValue, bool invertColors)
		{
			if (!previousValue.HasValue)
			{
				ImGui.TextUnformatted("\u2014");
				return;
			}

			float delta = currentValue - previousValue.Value;
			DrawDeltaText(delta, $"{delta:+0.0;-0.0;0.0}ms", invertColors);
		}

		private static void DrawDeltaText(float delta, string text, bool invertColors)
		{
			if (delta > 0)
			{
				ImGui.TextColored(invertColors ? DeltaNegativeColor : DeltaPositiveColor, text);
			}
			else if (delta < 0)
			{
				ImGui.TextColored(invertColors ? DeltaPositiveColor : DeltaNegativeColor, text);
			}
			else
			{
				ImGui.TextUnformatted(text);
			}
		}
	}
}
