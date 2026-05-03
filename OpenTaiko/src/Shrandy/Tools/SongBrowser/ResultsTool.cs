using System.Numerics;
using ImGuiNET;

namespace OpenTaiko.Shrandy.Tools
{
	internal class ResultsTool : Tool
	{
		private static readonly Vector4 DeltaPositiveColor = new Vector4(0.2f, 0.9f, 0.2f, 1.0f);
		private static readonly Vector4 DeltaNegativeColor = new Vector4(0.9f, 0.2f, 0.2f, 1.0f);

		private SongBrowserData m_Data;

		public ResultsTool(SongBrowserData data, string toolName = "Results", SlimDXKeys.Key enableHotkey = SlimDXKeys.Key.Unknown)
			: base(toolName, enableHotkey)
		{
			m_Data = data;
		}

		public override bool IsBlockingInput()
		{
			return false;
		}

		public override void OnResultsActivate(CStage結果 resultsScreen)
		{
			SetEnabled(true);
		}

		public override void OnStageChanged(CStage stage)
		{
			if (stage is not CStage結果)
			{
				SetEnabled(false);
			}
		}

		public override void DrawWindow()
		{
			Vector2 displaySize = ImGui.GetIO().DisplaySize;
			ImGui.SetNextWindowPos(displaySize * 0.5f, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
			bool open = Enabled;
			if (ImGui.Begin("Results##resultstool", ref open, ImGuiWindowFlags.AlwaysAutoResize))
			{
				Draw();
			}
			ImGui.End();
			SetEnabled(open);
		}

		protected override void Draw()
		{
			ResultsSnapshot? snapshot = m_Data.CurrentResultsSnapshot;
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

			ImGuiTableFlags tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
			if (!ImGui.BeginTable("ResultsDelta", 4, tableFlags))
			{
				return;
			}

			ImGui.TableSetupColumn("Stat",       ImGuiTableColumnFlags.WidthFixed, 140);
			ImGui.TableSetupColumn("Current",    ImGuiTableColumnFlags.WidthFixed, 90);
			ImGui.TableSetupColumn("Best",       ImGuiTableColumnFlags.WidthFixed, 90);
			ImGui.TableSetupColumn("Difference", ImGuiTableColumnFlags.WidthFixed, 100);
			ImGui.TableHeadersRow();

			DrawIntRow("Score",      current.Score,        previous?.Score,        invertColors: false);
			DrawIntRow("Goods",      current.Goods,        previous?.Goods,        invertColors: false);
			DrawIntRow("Okays",      current.Okays,        previous?.Okays,        invertColors: false);
			DrawIntRow("Bads",       current.Bads,         previous?.Bads,         invertColors: true);
			DrawIntRow("Drum Roll",  current.Rolls,        previous?.Rolls,        invertColors: false);
			DrawHitErrorRow(current.AvgHitError, previous?.AvgHitError,            invertColors: true);

			ImGui.EndTable();
		}

		private static void DrawIntRow(string label, int currentValue, int? previousValue, bool invertColors)
		{
			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted(label);

			ImGui.TableSetColumnIndex(1);
			ImGui.TextUnformatted(currentValue.ToString());

			ImGui.TableSetColumnIndex(2);
			ImGui.TextUnformatted(previousValue.HasValue ? previousValue.Value.ToString() : "\u2014");

			ImGui.TableSetColumnIndex(3);
			DrawIntDelta(currentValue, previousValue, invertColors);
		}

		private static void DrawHitErrorRow(float currentValue, float? previousValue, bool invertColors)
		{
			ImGui.TableNextRow();

			ImGui.TableSetColumnIndex(0);
			ImGui.TextUnformatted("Avg Hit Error");

			ImGui.TableSetColumnIndex(1);
			ImGui.TextUnformatted($"{currentValue:F1}ms");

			ImGui.TableSetColumnIndex(2);
			ImGui.TextUnformatted(previousValue.HasValue ? $"{previousValue.Value:F1}ms" : "\u2014");

			ImGui.TableSetColumnIndex(3);
			DrawFloatDelta(currentValue, previousValue, invertColors);
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
