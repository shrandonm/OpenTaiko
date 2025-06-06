﻿using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class CActImplBalloon : CActivity {


	public CActImplBalloon() {
		base.IsDeActivated = true;

	}

	public override void Activate() {
		this.ct風船終了 = new CCounter();
		this.ct風船ふきだしアニメ = new CCounter();
		this.ct風船アニメ = new CCounter[5];
		for (int i = 0; i < 5; i++) {
			this.ct風船アニメ[i] = new CCounter();
		}

		this.ct風船ふきだしアニメ = new CCounter(0, 1, 100, OpenTaiko.Timer);

		KusudamaScript = new(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.BALLOON}{TextureLoader.KUSUDAMA}Script.lua"));
		KusudamaScript.Init();

		base.Activate();
	}

	public override void DeActivate() {
		KusudamaScript.Dispose();

		this.ct風船終了 = null;
		this.ct風船ふきだしアニメ = null;

		base.DeActivate();
	}

	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}

	public override int Draw() {
		return base.Draw();
	}

	public void KusuIn() {
		KusudamaScript.KusuIn();
		KusudamaIsActive = true;
	}
	public void KusuBroke() {
		KusudamaScript.KusuBroke();
		KusudamaIsActive = false;
	}
	public void KusuMiss() {
		KusudamaScript.KusuMiss();
		KusudamaIsActive = false;
	}

	public enum EBalloonType {
		BALLOON,
		KUSUDAMA,
		FUSEROLL
	}

	public bool KusudamaIsActive { get; private set; } = false;

	public void tDrawKusudama() {
		if (!OpenTaiko.stageGameScreen.bPAUSE) {
			KusudamaScript.Update();
		}
		if (!(OpenTaiko.stageGameScreen.bPAUSE && OpenTaiko.ConfigIni.bTokkunMode)) {
			KusudamaScript.Draw();
		}
	}

	public int On進行描画(int n連打ノルマ, int n連打数, int player, EBalloonType btype) {
		this.ct風船ふきだしアニメ.TickLoop();
		this.ct風船アニメ[player].Tick();

		//CDTXMania.act文字コンソール.tPrint( 0, 16, C文字コンソール.Eフォント種別.赤, this.ct風船終了.n現在の値.ToString() );
		int[] n残り打数 = new int[] { 0, 0, 0, 0, 0 };
		#region[  ]
		if (n連打ノルマ > 0) {
			if (n連打ノルマ < 5) {
				n残り打数 = new int[] { 4, 3, 2, 1, 0 };
			} else {
				n残り打数[0] = (n連打ノルマ / 5) * 4;
				n残り打数[1] = (n連打ノルマ / 5) * 3;
				n残り打数[2] = (n連打ノルマ / 5) * 2;
				n残り打数[3] = (n連打ノルマ / 5) * 1;
			}
		}
		#endregion

		if (n連打数 != 0) {
			int x;
			int y;
			int frame_x;
			int frame_y;
			int num_x;
			int num_y;
			if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
				x = OpenTaiko.Skin.Game_Balloon_Balloon_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * player);
				y = OpenTaiko.Skin.Game_Balloon_Balloon_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * player);
				frame_x = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * player);
				frame_y = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * player);
				num_x = OpenTaiko.Skin.Game_Balloon_Balloon_Number_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * player);
				num_y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * player);
			} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
				x = OpenTaiko.Skin.Game_Balloon_Balloon_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * player);
				y = OpenTaiko.Skin.Game_Balloon_Balloon_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * player);
				frame_x = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * player);
				frame_y = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * player);
				num_x = OpenTaiko.Skin.Game_Balloon_Balloon_Number_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * player);
				num_y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * player);
			} else {
				x = OpenTaiko.Skin.Game_Balloon_Balloon_X[player];
				y = OpenTaiko.Skin.Game_Balloon_Balloon_Y[player];
				frame_x = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_X[player];
				frame_y = OpenTaiko.Skin.Game_Balloon_Balloon_Frame_Y[player];
				num_x = OpenTaiko.Skin.Game_Balloon_Balloon_Number_X[player];
				num_y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Y[player];
			}
			//1P:0 2P:245
			//if (CDTXMania.Tx.Chara_Balloon_Breaking != null && CDTXMania.ConfigIni.ShowChara)
			//    CDTXMania.Tx.Chara_Balloon_Breaking.t2D描画(CDTXMania.app.Device, CDTXMania.Skin.Game_Chara_Balloon_X[player], CDTXMania.Skin.Game_Chara_Balloon_Y[player]);
			for (int j = 0; j < 5; j++) {

				if (n残り打数[j] < n連打数 && btype == EBalloonType.BALLOON) {
					if (OpenTaiko.Tx.Balloon_Breaking[j] != null)
						OpenTaiko.Tx.Balloon_Breaking[j].t2D描画(x + (this.ct風船ふきだしアニメ.CurrentValue == 1 ? 3 : 0), y);
					break;
				}
			}
			//1P:31 2P:329

			if (btype == EBalloonType.BALLOON) {
				if (OpenTaiko.Tx.Balloon_Balloon != null)
					OpenTaiko.Tx.Balloon_Balloon.t2D描画(frame_x, frame_y);
				this.t文字表示(num_x, num_y, n連打数, player);
			} else if (btype == EBalloonType.FUSEROLL) {
				if (OpenTaiko.Tx.Fuse_Balloon != null)
					OpenTaiko.Tx.Fuse_Balloon.t2D描画(frame_x, frame_y);
				this.tFuseNumber(num_x, num_y, n連打数, player);
			} else if (btype == EBalloonType.KUSUDAMA && player == 0) {
				/*
                if (TJAPlayer3.Tx.Kusudama_Back != null)
                    TJAPlayer3.Tx.Kusudama_Back.t2D描画(0, 0);
                if (TJAPlayer3.Tx.Kusudama != null)
                    TJAPlayer3.Tx.Kusudama.t2D描画(0, 0);
                    */
				if (!(OpenTaiko.stageGameScreen.bPAUSE && OpenTaiko.ConfigIni.bTokkunMode))
					this.tKusudamaNumber(n連打数);
			}

			//CDTXMania.act文字コンソール.tPrint( 0, 0, C文字コンソール.Eフォント種別.白, n連打数.ToString() );
		}

		return base.Draw();
	}

	public KusudamaScript KusudamaScript { get; private set; }


	private CCounter ct風船終了;
	private CCounter ct風船ふきだしアニメ;

	public CCounter[] ct風船アニメ;
	private float[] RollScale = new float[]
	{
		0.000f,
		0.123f, // リピート
		0.164f,
		0.164f,
		0.164f,
		0.137f,
		0.110f,
		0.082f,
		0.055f,
		0.000f
	};

	[StructLayout(LayoutKind.Sequential)]
	private struct ST文字位置 {
		public char ch;
		public Point pt;
	}

	private void _nbDisplay(CTexture tx, int num, int x, int y) {
		int[] nums = CConversion.SeparateDigits(num);
		for (int j = 0; j < nums.Length; j++) {
			float offset = j - (nums.Length / 2.0f);
			float _x = x - (OpenTaiko.Skin.Game_Balloon_Number_Interval[0] * offset);
			float _y = y - (OpenTaiko.Skin.Game_Balloon_Number_Interval[1] * offset);

			float width = tx.sz画像サイズ.Width / 10.0f;
			float height = tx.sz画像サイズ.Height;

			tx.t2D拡大率考慮下基準描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
		}
	}

	private void tKusudamaNumber(int num) {
		if (OpenTaiko.Tx.Kusudama_Number == null) return;
		OpenTaiko.Tx.Kusudama_Number.vcScaleRatio.X = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale;
		OpenTaiko.Tx.Kusudama_Number.vcScaleRatio.Y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale;
		int x = OpenTaiko.Skin.Game_Kusudama_Number_X;
		int y = OpenTaiko.Skin.Game_Kusudama_Number_Y;

		int[] nums = CConversion.SeparateDigits(num);
		for (int j = 0; j < nums.Length; j++) {
			float offset = j - ((nums.Length - 2) / 2.0f);
			float width = OpenTaiko.Tx.Kusudama_Number.sz画像サイズ.Width / 10.0f;
			float height = OpenTaiko.Tx.Kusudama_Number.sz画像サイズ.Height;
			float _x = x - (width * offset);
			float _y = y;

			OpenTaiko.Tx.Kusudama_Number.t2D拡大率考慮下基準描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
		}
	}

	private void tFuseNumber(int x, int y, int num, int nPlayer) {
		if (OpenTaiko.Tx.Fuse_Number == null) return;
		OpenTaiko.Tx.Fuse_Number.vcScaleRatio.X = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale;
		OpenTaiko.Tx.Fuse_Number.vcScaleRatio.Y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale + RollScale[this.ct風船アニメ[nPlayer].CurrentValue];

		_nbDisplay(OpenTaiko.Tx.Fuse_Number, num, x, y);
	}

	private void t文字表示(int x, int y, int num, int nPlayer) {
		if (OpenTaiko.Tx.Balloon_Number_Roll == null) return;
		OpenTaiko.Tx.Balloon_Number_Roll.vcScaleRatio.X = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale;
		OpenTaiko.Tx.Balloon_Number_Roll.vcScaleRatio.Y = OpenTaiko.Skin.Game_Balloon_Balloon_Number_Scale + RollScale[this.ct風船アニメ[nPlayer].CurrentValue];

		_nbDisplay(OpenTaiko.Tx.Balloon_Number_Roll, num, x, y);
	}

	public void tEnd() {
		this.ct風船終了 = new CCounter(0, 80, 10, SoundManager.PlayTimer);
	}
}
