using FDK;

namespace OpenTaiko;

internal class CActFIFOResult : CActivity {
	// メソッド

	public void tフェードアウト開始() {
		this.mode = EFIFOMode.FadeOut;
		this.counter = new CCounter(0, 25, 30, OpenTaiko.Timer);
	}
	public void tフェードイン開始() {
		this.mode = EFIFOMode.FadeIn;
		this.counter = new CCounter(0, 300, 2, OpenTaiko.Timer);
	}
	public void tフェードイン完了() {
		this.counter.CurrentValue = (int)counter.BeginValue;
	}


	// CActivity 実装

	public override void DeActivate() {
		if (!base.IsDeActivated) {
			//CDTXMania.tテクスチャの解放( ref this.tx黒タイル64x64 );
			base.DeActivate();
		}
	}
	public override void CreateManagedResource() {
		//this.tx黒タイル64x64 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Tile black 64x64.png" ), false );
		base.CreateManagedResource();
	}
	public override int Draw() {
		if (base.IsDeActivated || (this.counter == null)) {
			return 0;
		}
		this.counter.Tick();
		// Size clientSize = CDTXMania.app.Window.ClientSize;	// #23510 2010.10.31 yyagi: delete as of no one use this any longer.
		if (OpenTaiko.Tx.Tile_Black != null) {
			if (this.mode == EFIFOMode.FadeIn) {
				int fadeStart = (int)(this.counter.EndValue * 2 / 3);
				int fadeRange = (int)this.counter.EndValue - fadeStart;
				if (this.counter.CurrentValue >= fadeStart) {
					OpenTaiko.Tx.Tile_Black.Opacity = (((fadeRange - (this.counter.CurrentValue - fadeStart)) * 0xff) / fadeRange);
				} else {
					OpenTaiko.Tx.Tile_Black.Opacity = 255;
				}
			} else {
				OpenTaiko.Tx.Tile_Black.Opacity = ((this.counter.CurrentValue * 0xff) / (int)this.counter.EndValue);
			}

			for (int i = 0; i <= (GameWindowSize.Width / OpenTaiko.Tx.Tile_Black.szTextureSize.Width); i++)      // #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
			{
				for (int j = 0; j <= (GameWindowSize.Height / OpenTaiko.Tx.Tile_Black.szTextureSize.Height); j++) // #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
				{
					OpenTaiko.Tx.Tile_Black.t2D描画(i * OpenTaiko.Tx.Tile_Black.szTextureSize.Width, j * OpenTaiko.Tx.Tile_Black.szTextureSize.Height);
				}
			}
		}
		if (!this.counter.IsEnded) {
			return 0;
		}
		return 1;
	}


	// その他

	#region [ private ]
	//-----------------
	private CCounter counter;
	private EFIFOMode mode;
	//private CTexture tx黒タイル64x64;
	//-----------------
	#endregion
}
