using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

// When the player hits a note, this renders a stationary copy of the note
// at the player's actual hit position, fading out over a short duration.
internal class FadingNotes : CActivity {
	private const int FADE_DURATION_MS = 50;
	private const int POOL_SIZE = 64;

	public FadingNotes() {
		base.IsDeActivated = true;
	}

	public void Start(NotesManager.ENoteType nLane, EGameType gameType, int nPlayer, int nChipXOffset) {
		if (OpenTaiko.ConfigIni.nPlayerCount > 2 || OpenTaiko.ConfigIni.SimpleMode)
			return;
		if (nLane is NotesManager.ENoteType.Empty or NotesManager.ENoteType.Unknown)
			return;

		for (int i = 0; i < POOL_SIZE; i++) {
			if (!Pool[i].IsUsing) {
				Pool[i].IsUsing = true;
				Pool[i].Lane = nLane;
				Pool[i].GameType = gameType;
				Pool[i].Player = nPlayer;
				// Derive center-based coords from the scroll field (top-left) + half note size
				Pool[i].X = OpenTaiko.Skin.Game_Effect_FlyingNotes_StartPoint_X[nPlayer]
					+ OpenTaiko.stageGameScreen.GetJPOSCROLLX(nPlayer)
					+ nChipXOffset;
				Pool[i].Y = OpenTaiko.Skin.nScrollFieldY[nPlayer]
					+ OpenTaiko.stageGameScreen.GetJPOSCROLLY(nPlayer)
					+ (OpenTaiko.Skin.Game_Notes_Size[1] / 2);
				Pool[i].Counter = new CCounter(0, FADE_DURATION_MS, 1, OpenTaiko.Timer);
				break;
			}
		}
	}

	public override void Activate() {
		for (int i = 0; i < POOL_SIZE; i++) {
			Pool[i] = new Status();
			Pool[i].Counter = new CCounter();
		}
		base.Activate();
	}

	public override void DeActivate() {
		for (int i = 0; i < POOL_SIZE; i++) {
			Pool[i].Counter = null;
		}
		base.DeActivate();
	}

	public override int Draw() {
		if (base.IsDeActivated || OpenTaiko.ConfigIni.SimpleMode)
			return base.Draw();

		for (int i = 0; i < POOL_SIZE; i++) {
			if (!Pool[i].IsUsing) continue;

			Pool[i].Counter.Tick();
			if (Pool[i].Counter.IsEnded) {
				Pool[i].Counter.Stop();
				Pool[i].IsUsing = false;
				continue;
			}

			double progress = Pool[i].Counter.CurrentValue / (double)FADE_DURATION_MS;
			int opacity = (int)(255 * (1.0 - progress));

			var tex = Pool[i].Lane == NotesManager.ENoteType.Kadon
				? OpenTaiko.Tx.Note_Swap
				: OpenTaiko.Tx.Notes[(int)Pool[i].GameType];
			if (tex != null) {
				int savedOpacity = tex.Opacity;
				tex.Opacity = opacity;
				NotesManager.DisplayNote(Pool[i].Player, Pool[i].X, Pool[i].Y, Pool[i].Lane, Pool[i].GameType);
				tex.Opacity = savedOpacity;
			}
		}

		return base.Draw();
	}

	#region [ private ]

	[StructLayout(LayoutKind.Sequential)]
	private struct Status {
		public bool IsUsing;
		public NotesManager.ENoteType Lane;
		public EGameType GameType;
		public int Player;
		public int X;
		public int Y;
		public CCounter Counter;
	}

	private Status[] Pool = new Status[POOL_SIZE];

	#endregion
}
// [End divergence]
