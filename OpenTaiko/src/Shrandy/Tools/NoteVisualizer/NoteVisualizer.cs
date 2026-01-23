using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using System.Numerics;

namespace OpenTaiko.Shrandy.Tools
{
	internal class NoteVisualizer : Tool
	{
		private List<HitParams> m_HitHistory = new();
		private float m_Zoom = 1.0f;
		private float m_Pan = 0.0f;
		private Vector2 m_CursorPos;
		private ImDrawListPtr m_DrawList;
		private Vector2 m_WidgetTopLeft;
		private Vector2 m_WidgetBottomRight;
		private int m_NoteErrorFilterMs = 0;

		public NoteVisualizer(string toolName, SlimDXKeys.Key enableHotkey)
			: base(toolName, enableHotkey)
		{
		}

		protected override void Draw()
		{
			if (OpenTaiko.TJA != null && OpenTaiko.TJA.listChip.Count > 0)
			{
				ImGui.Text("Note Error Filter (ms)");
				ImGui.SameLine();
				ImGui.SliderInt("", ref m_NoteErrorFilterMs, 0, 100);

				double songDuration = OpenTaiko.TJA.listChip[^1].n発声時刻ms;
				DrawTimelineWidget("##NoteTimeline", (float)songDuration,
					tjaNotes: OpenTaiko.TJA.listNoteChip,
					tjaBars: OpenTaiko.TJA.listBarLineChip,
					m_HitHistory);
			}
		}

		public override void OnTrainingModeResumePlay()
		{
			base.OnTrainingModeResumePlay();
			Reset();
		}

		public override void OnNoteHit(HitParams hitParams)
		{
			base.OnNoteHit(hitParams);
			m_HitHistory.Add(hitParams);
		}

		public override void OnStageChanged(CStage stage)
		{
			base.OnStageChanged(stage);
			Reset();
		}

		public override void OnSongRestart()
		{
			base.OnSongRestart();
			Reset();
		}

		private void Reset()
		{
			m_HitHistory.Clear();
		}

		private void DrawNote(bool isMissed, NotesManager.ENoteType noteType, in Vector2 position, float alpha, uint outlineColour)
		{
			const float smallCircleRadius = 16.0f;
			const float bigCircleRadius = 20.0f;
			const int circleSegments = 16;
			
			float circleThickness = isMissed ? 4.0f : 2.0f;
			uint donColour = Utilities.ColourHelper.GetDonImGuiColour();
			uint kaColour = Utilities.ColourHelper.GetKaImGuiColour();
			uint rollColour = Utilities.ColourHelper.GetRollImGuiColour();
			uint balloonColour = Utilities.ColourHelper.GetBalloonImGuiColour();

			bool isDon = noteType == NotesManager.ENoteType.Don || noteType == NotesManager.ENoteType.DonBig;
			bool isBig = noteType == NotesManager.ENoteType.DonBig || noteType == NotesManager.ENoteType.KaBig;
			bool isRoll = NotesManager.IsRoll(noteType);
			bool isBalloon = NotesManager.IsBalloon(noteType);

			float radius = isBig ? bigCircleRadius : smallCircleRadius;
			uint fillColour = isRoll ? rollColour
				: isBalloon ? balloonColour
				: isDon ? donColour
				: kaColour;
			fillColour = Utilities.ColourHelper.SetAlpha(fillColour, alpha);

			m_DrawList.AddCircleFilled(position, radius, fillColour, circleSegments);
			m_DrawList.AddCircle(position, radius, outlineColour, circleSegments, circleThickness);
		}

		private void DrawBar(float xPosition, int index, uint colour)
		{
			const float barHeight = 8.0f;
			Vector2 start = new Vector2(xPosition, m_CursorPos.Y + (barHeight / 2.0f));
			Vector2 end = new Vector2(xPosition, m_WidgetBottomRight.Y - (barHeight / 2.0f));
			m_DrawList.AddLine(start, end, colour, 2.0f);
			m_DrawList.AddText(end, colour, index.ToString());
		}

		void DrawTimelineWidget(string id, float songDuration,
			List<CChip> tjaNotes, List<CChip> tjaBars, List<HitParams> hitHistory)
		{
			float height = 100.0f;
			float width = ImGui.GetContentRegionAvail().X;
			m_CursorPos = ImGui.GetCursorScreenPos();

			ImGui.InvisibleButton(id, new Vector2(width, height),
				ImGuiButtonFlags.MouseButtonLeft |
				ImGuiButtonFlags.MouseButtonMiddle);

			m_DrawList = ImGui.GetWindowDrawList();

			// Background
			m_WidgetTopLeft = m_CursorPos;
			m_WidgetBottomRight = new Vector2(m_CursorPos.X + width, m_CursorPos.Y + height);
			m_DrawList.AddRectFilled(m_WidgetTopLeft, m_WidgetBottomRight, ImGui.GetColorU32(ImGuiCol.FrameBg), 6);

			const float TimelineHorizontalPaddingPx = 12.0f;

			float timelineCenterY = (m_CursorPos.Y + m_WidgetBottomRight.Y) * 0.5f;
			float timelineLeftX = m_WidgetTopLeft.X + TimelineHorizontalPaddingPx;
			float timelineRightX = m_WidgetBottomRight.X - TimelineHorizontalPaddingPx;

			// Visible time window derived from pan + zoom
			bool isPaused = OpenTaiko.stageGameScreen.bPAUSE;
			float songTime = (float)OpenTaiko.TJA.GameTimeToTjaTime(FDK.SoundManager.PlayTimer.NowTimeMs);
			float clampedZoomValue = Math.Max(0.0001f, m_Zoom);
			float visibleTimeSpan = songDuration / clampedZoomValue;

			if (!isPaused)
			{
				const float lookAheadPercent = 0.8f;
				m_Pan = songTime - visibleTimeSpan * lookAheadPercent;
				m_Zoom = 30.0f;
			}
			float visibleStartTime = m_Pan;
			float visibleEndTime = visibleStartTime + visibleTimeSpan;

			float TFromX(float x)
			{
				float u = (x - timelineLeftX) / (timelineRightX - timelineLeftX);
				return visibleStartTime + u * (visibleEndTime - visibleStartTime);
			}

			float XFromT(float t)
			{
				float u = (t - visibleStartTime) / (visibleEndTime - visibleStartTime);
				return timelineLeftX + u * (timelineRightX - timelineLeftX);
			}

			if (isPaused)
			{
				// Zoom
				if (ImGui.IsItemHovered())
				{
					float wheel = ImGui.GetIO().MouseWheel;
					if (wheel != 0.0f)
					{
						var mp = ImGui.GetMousePos();
						float tAtMouse = TFromX(mp.X);

						float zoomFactor = (wheel > 0) ? 1.1f : 1.0f / 1.1f;
						m_Zoom = Math.Clamp(m_Zoom * zoomFactor, 1.0f, 100.0f);

						// Keep tAtMouse anchored
						float newSpan = songDuration / m_Zoom;
						m_Pan = tAtMouse - ((mp.X - timelineLeftX) / (timelineRightX - timelineLeftX)) * newSpan;
					}
				}

				// Pan
				if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
				{
					float dx = ImGui.GetIO().MouseDelta.X;
					float dt = -dx / (timelineRightX - timelineLeftX) * (visibleEndTime - visibleStartTime);
					m_Pan += dt;
				}

				// Clamp pan to base range
				m_Pan = Math.Clamp(m_Pan, 0f, Math.Max(0f, songDuration - songDuration / m_Zoom));
			}

			m_DrawList.AddLine(new Vector2(timelineLeftX, timelineCenterY),
				new Vector2(timelineRightX, timelineCenterY),
				ImGui.GetColorU32(ImGuiCol.Separator), 2.0f);

			for (int i = 0; i < tjaBars.Count; ++i)
			{
				float barTime = XFromT(tjaBars[i].n発声時刻ms);
				if ((i % 3) == 0)
				{
					DrawBar(barTime, i / 3, ImGui.GetColorU32(ImGuiCol.PlotLines));
				}
			}

			for (int i = tjaNotes.Count - 1; i >= 0; --i)
			{
				CChip note = tjaNotes[i];
				float noteTime = note.n発声時刻ms;
				if (noteTime >= visibleStartTime && noteTime <= visibleEndTime)
				{
					float x = XFromT(noteTime);

					DrawNote(note.IsMissed, NotesManager.GetNoteType(note), new Vector2(x, timelineCenterY), alpha: 1.0f,
						outlineColour: GetOutlineColour(note, ENoteJudge.Auto));
				}
			}

			for (int i = hitHistory.Count - 1; i >= 0; --i)
			{
				HitParams hitParams = hitHistory[i];
				double noteTime = hitParams.Chip != null ? hitParams.Chip.n発声時刻ms : hitParams.HitTjaTimeMs;
				if (noteTime < visibleStartTime || noteTime > visibleEndTime)
				{
					continue;
				}

				if (Math.Abs(hitParams.HitErrorMs) < m_NoteErrorFilterMs)
				{
					continue;
				}

				double hitTime = noteTime + hitParams.HitErrorMs;
				float hitX = XFromT((float)hitTime);
				const float offsetY = 32.0f;
				Vector2 position = new Vector2(hitX, timelineCenterY + offsetY);

				NotesManager.ENoteType noteType = NotesManager.ENoteType.Unknown;
				if (hitParams.Chip != null)
				{
					noteType = NotesManager.GetNoteType(hitParams.Chip);
				}
				else
				{
					noteType = hitParams.Note == Note.Don ? NotesManager.ENoteType.Don : NotesManager.ENoteType.Ka;
				}

				DrawNote(hitParams.JudgeResult == ENoteJudge.Bad, noteType, position, alpha: 0.5f,
					outlineColour: GetOutlineColour(hitParams.Chip, hitParams.JudgeResult));

				const float textOffsetY = 16.0f;
				Vector2 textPosition = new Vector2(hitX, position.Y + textOffsetY);

				textPosition.Y += textOffsetY;
				m_DrawList.AddText(textPosition, 0xFFFFFFFF, hitParams.HitErrorMs.ToString("+0;-0;0"));

				string errorText = hitParams.HitErrorMs == 0 ? "Perfect"
					: hitParams.HitErrorMs < 0 ? "Early"
					: "Late";
				textPosition.Y += textOffsetY;
				m_DrawList.AddText(textPosition, 0xFFFFFFFF, errorText);

				textPosition.Y += textOffsetY;
				m_DrawList.AddText(textPosition, 0xFFFFFFFF, hitParams.Hand.ToString());
			}

			// Optional tooltip
			if (ImGui.IsItemHovered())
			{
				float tHover = TFromX(ImGui.GetMousePos().X);
				ImGui.BeginTooltip();
				ImGui.Text($"t = {tHover:0.###}");
				ImGui.EndTooltip();
			}
		}

		private uint GetOutlineColour(CChip? note, ENoteJudge judgeResult)
		{
			if (note == null)
			{
				return 0xFFFF00FF;
			}
			if (note.IsMissed)
			{
				return 0xFF0000BB;
			}
			else if (ShrandyExtension.IsOkay(judgeResult))
			{
				return 0xFF00EFFD;
			}
			return 0xFFFFFFFF;
		}
	}
}
