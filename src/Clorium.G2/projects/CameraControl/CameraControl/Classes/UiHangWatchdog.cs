using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;
using CameraControl.Devices;
using Microsoft.Win32.SafeHandles;

namespace CameraControl.Classes
{
	/// <summary>
	/// Development runtimes only (g2-development-runtime.marker next to the program): when the UI thread stops answering for
	/// 20 seconds, writes one full memory dump (Log\hang-*.dmp) so the blocked threads can be read afterwards instead of
	/// guessed from the log. Added after G2 hung when the camera was switched off and on (2026-11-06).
	/// </summary>
	public static class UiHangWatchdog
	{
		private const int HangSeconds = 20;
		private static long _lastAnswer = DateTime.UtcNow.Ticks;
		private static bool _started;

		public static void Start(Dispatcher dispatcher, string logFolder)
		{
			if (_started || dispatcher == null) return;
			if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "g2-development-runtime.marker"))) return;
			_started = true;
			var thread = new Thread(() => Watch(dispatcher, logFolder)) { IsBackground = true, Name = "UI hang watchdog", Priority = ThreadPriority.AboveNormal };
			thread.Start();
			Log.Debug("UiHangWatchdog : watching the UI thread (dump after " + HangSeconds + " s without an answer)");
		}

		private static void Watch(Dispatcher dispatcher, string logFolder)
		{
			bool dumped = false;
			while (!dispatcher.HasShutdownStarted)
			{
				dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => Interlocked.Exchange(ref _lastAnswer, DateTime.UtcNow.Ticks)));
				Thread.Sleep(2000);
				TimeSpan silent = DateTime.UtcNow - new DateTime(Interlocked.Read(ref _lastAnswer), DateTimeKind.Utc);
				if (silent.TotalSeconds < HangSeconds)
				{
					dumped = false;
					continue;
				}
				if (dumped) continue;
				dumped = true;
				try
				{
					Directory.CreateDirectory(logFolder);
					string path = Path.Combine(logFolder, "hang-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".dmp");
					Log.Error("UiHangWatchdog : UI thread has not answered for " + (int)silent.TotalSeconds + " s, writing " + path);
					Write(path);
					Log.Error("UiHangWatchdog : dump written " + path);
				}
				catch (Exception ex)
				{
					Log.Error("UiHangWatchdog : dump failed", ex);
				}
			}
		}

		// MiniDumpWithFullMemory | MiniDumpWithHandleData | MiniDumpWithUnloadedModules | MiniDumpWithThreadInfo
		private const uint DumpType = 0x2 | 0x4 | 0x20 | 0x1000;

		[DllImport("dbghelp.dll", SetLastError = true)]
		private static extern bool MiniDumpWriteDump(IntPtr process, uint processId, SafeFileHandle file, uint type, IntPtr exception, IntPtr user, IntPtr callback);

		private static void Write(string path)
		{
			using (Process self = Process.GetCurrentProcess())
			using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
			{
				if (!MiniDumpWriteDump(self.Handle, (uint)self.Id, file.SafeFileHandle, DumpType, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
					throw new IOException("MiniDumpWriteDump failed: " + Marshal.GetLastWin32Error());
			}
		}
	}
}
