using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using SlimDXKeys;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenTaiko.Shrandy
{
	internal class TrainingTool : Tool
	{
		private TrainingToolSaveData m_SaveData = new();

		public TrainingTool(Key enableHotkey) : base(enableHotkey)
		{
		}

		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);
			if (stage == OpenTaiko.stageGameScreen)
			{
				TrainingToolSaveData? loadedSaveData = TrainingToolSaveData.Load(OpenTaiko.GetTJA(0)?.strFileName ?? "");
				if (loadedSaveData != null)
				{
					m_SaveData = loadedSaveData;
					OnSaveLoaded();
				}
			}
		}

		private void OnSaveLoaded()
		{
			OpenTaiko.stageGameScreen.actTokkun.JumpPointList = new(m_SaveData.Bookmarks);
		}

		public override void Draw()
		{
			base.Draw();
			ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Once);
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 300), ImGuiCond.Once);
			if (ImGui.Begin("ShrandyTool"))
			{
				DrawMeasures();

				if (ImGui.Button("Save"))
				{
					m_SaveData.Save();
				}

				ImGui.End();
			}
		}

		private void DrawMeasures()
		{
			CActImplTrainingMode trainingMode = OpenTaiko.stageGameScreen.actTokkun;
			if (trainingMode != null && trainingMode.JumpPointList != null)
			{
				ImGui.Text("Bookmarked Measures");
				foreach (CActImplTrainingMode.STJUMPP bookmark in trainingMode.JumpPointList)
				{
					if (ImGui.Button(bookmark.Measure.ToString()))
					{
						trainingMode.JumpToMeasure(bookmark.Measure);
					}
				}
			}
		}
	}
}
