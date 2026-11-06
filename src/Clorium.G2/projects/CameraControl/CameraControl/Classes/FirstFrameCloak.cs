using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CameraControl.Classes;

// A large WPF window shows one blank white frame between Show() and its first render (seen at start-up as a white
// screen behind the logo). Cloak keeps the window invisible to the desktop compositor (DWMWA_CLOAK, Windows 8+) until
// its content has been drawn, then uncloaks it; the window is laid out and activated normally meanwhile.
// A timer uncloaks after a few seconds in any case, and on failure nothing is cloaked.
public static class FirstFrameCloak
{
	private const int DWMWA_CLOAK = 13;

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

	public static void Apply(Window window)
	{
		IntPtr hwnd;
		try
		{
			hwnd = new WindowInteropHelper(window).EnsureHandle();
			if (!Set(hwnd, true)) return;
		}
		catch (Exception)
		{
			return;
		}
		bool done = false;
		DispatcherTimer safety = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
		void Show()
		{
			if (done) return;
			done = true;
			safety.Stop();
			Set(hwnd, false);
		}
		safety.Tick += (s, e) => Show();
		safety.Start();
		// ContentRendered comes after the first frame is handed over; one idle pass lets the compositor take it.
		window.ContentRendered += (s, e) => window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(Show));
		window.Closed += (s, e) => safety.Stop();
	}

	private static bool Set(IntPtr hwnd, bool cloaked)
	{
		try
		{
			int value = cloaked ? 1 : 0;
			return DwmSetWindowAttribute(hwnd, DWMWA_CLOAK, ref value, sizeof(int)) == 0;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
