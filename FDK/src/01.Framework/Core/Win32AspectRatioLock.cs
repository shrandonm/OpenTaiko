using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace FDK;

/// <summary>
/// Constrains window resize to maintain a fixed aspect ratio on Windows.
/// Used as a fallback when GLFW is not the windowing backend (e.g. SDL).
/// </summary>
[SupportedOSPlatform("windows")]
internal static class Win32AspectRatioLock {
	private const int GWL_WNDPROC = -4;
	private const uint WM_SIZING = 0x0214;
	private const int WMSZ_LEFT = 1;
	private const int WMSZ_RIGHT = 2;
	private const int WMSZ_TOP = 3;
	private const int WMSZ_TOPLEFT = 4;
	private const int WMSZ_TOPRIGHT = 5;
	private const int WMSZ_BOTTOM = 6;
	private const int WMSZ_BOTTOMLEFT = 7;
	private const int WMSZ_BOTTOMRIGHT = 8;

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint SetWindowLongPtr(nint hwnd, int nIndex, nint dwNewLong);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint GetWindowLongPtr(nint hwnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint msg, nuint wParam, nint lParam);

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hwnd, ref RECT rect);

	[DllImport("user32.dll")]
	private static extern bool AdjustWindowRectEx(ref RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);

	[DllImport("user32.dll")]
	private static extern uint GetWindowLong(nint hwnd, int nIndex);

	private const int GWL_STYLE = -16;
	private const int GWL_EXSTYLE = -20;

	[StructLayout(LayoutKind.Sequential)]
	private struct RECT {
		public int Left, Top, Right, Bottom;
	}

	private delegate nint WndProcDelegate(nint hwnd, uint msg, nuint wParam, nint lParam);

	// Keep a static reference to prevent GC of the delegate
	private static WndProcDelegate? _wndProcDelegate;
	private static nint _prevWndProc;
	private static int _ratioW;
	private static int _ratioH;

	internal static void Apply(nint hwnd, int ratioW, int ratioH) {
		_ratioW = ratioW;
		_ratioH = ratioH;

		_wndProcDelegate = SubclassedWndProc;
		_prevWndProc = GetWindowLongPtr(hwnd, GWL_WNDPROC);
		SetWindowLongPtr(hwnd, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
	}

	private static nint SubclassedWndProc(nint hwnd, uint msg, nuint wParam, nint lParam) {
		if (msg == WM_SIZING) {
			unsafe {
				RECT* rect = (RECT*)lParam;
				int width = rect->Right - rect->Left;
				int height = rect->Bottom - rect->Top;

				// Adjust height or width based on which edge is being dragged
				switch ((int)wParam) {
					case WMSZ_LEFT:
					case WMSZ_RIGHT:
						// Width is driving; adjust height
						height = (int)Math.Round((double)width * _ratioH / _ratioW);
						rect->Bottom = rect->Top + height;
						break;
					case WMSZ_TOP:
					case WMSZ_BOTTOM:
						// Height is driving; adjust width
						width = (int)Math.Round((double)height * _ratioW / _ratioH);
						rect->Right = rect->Left + width;
						break;
					case WMSZ_TOPLEFT:
					case WMSZ_TOPRIGHT:
					case WMSZ_BOTTOMLEFT:
					case WMSZ_BOTTOMRIGHT:
						// Corner drag: use width to set height
						height = (int)Math.Round((double)width * _ratioH / _ratioW);
						if ((int)wParam == WMSZ_TOPLEFT || (int)wParam == WMSZ_TOPRIGHT)
							rect->Top = rect->Bottom - height;
						else
							rect->Bottom = rect->Top + height;
						break;
				}
			}
			return 1;
		}

		return CallWindowProc(_prevWndProc, hwnd, msg, wParam, lParam);
	}
}
