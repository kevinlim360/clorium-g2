// G2 ↔ Clorium Light Control protocol check: the real G2 client source against the light app's
// demo mode (six simulated lights, no Bluetooth) on an isolated pipe name.
// Usage: CloriumLightRegression <light-app python.exe> <light-app work/run.py>
//    or: CloriumLightRegression <built CloriumLightControl.exe>
using System;
using System.Diagnostics;
using System.Threading;
using CameraControl.Classes;
using CameraControl.Core;

namespace System.Windows
{
	public enum MessageBoxButton { OK }
	public enum MessageBoxImage { Exclamation }
	public static class MessageBox { public static int Shown; public static void Show(string text, string caption, MessageBoxButton b, MessageBoxImage i) { Shown++; } }
}
namespace CameraControl.Devices
{
	public static class Log { public static void Debug(string m) => Console.WriteLine("  log: " + m); public static void Error(string m, Exception e = null) => Console.WriteLine("  log error: " + m); }
}
namespace CameraControl.Core
{
	public class Settings { public bool UseCloriumLight = true; public string CloriumLightPath = ""; }
	public static class ServiceProvider { public static Settings Settings = new Settings(); }
}

static class Program
{
	static int passed;
	static void Check(string name, bool ok) { if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }

	static int Main(string[] args)
	{
		string user = "g2test" + Guid.NewGuid().ToString("N").Substring(0, 8);
		Environment.SetEnvironmentVariable("USERNAME", user);
		ServiceProvider.Settings.CloriumLightPath = @"C:\nonexistent\CloriumLightControl.exe";

		var watch = Stopwatch.StartNew();
		Check("no light app: not running", !CloriumLightControl.IsRunning());
		Check("no light app: power fails without throwing", !CloriumLightControl.PowerAsync(false, 2000).Result);
		Check("no light app: bounded wait", watch.Elapsed.TotalSeconds < 5);
		CloriumLightControl.OpenWindow();
		Check("missing executable reported once", System.Windows.MessageBox.Shown == 1);

		ServiceProvider.Settings.UseCloriumLight = false;
		Check("disabled: power is a no-op", !CloriumLightControl.PowerAsync(true).Result);
		ServiceProvider.Settings.UseCloriumLight = true;

		var start = new ProcessStartInfo(args[0]) { UseShellExecute = false };
		if (args.Length > 1) start.ArgumentList.Add(args[1]);
		foreach (string a in new[] { "--demo", "--language", "ko", "--control-pipe", "CloriumLightControl." + user }) start.ArgumentList.Add(a);
		using Process light = Process.Start(start);
		try
		{
			var deadline = DateTime.Now.AddSeconds(30);
			while (!CloriumLightControl.IsRunning() && DateTime.Now < deadline) Thread.Sleep(300);
			Check("demo light app reachable", CloriumLightControl.IsRunning());
			Check("power off all lights", CloriumLightControl.PowerAsync(false, 10000).Result);
			Check("power on all lights", CloriumLightControl.PowerAsync(true, 10000).Result);
			System.Windows.MessageBox.Shown = 0;
			CloriumLightControl.OpenWindow();
			Check("show running app without launching", System.Windows.MessageBox.Shown == 0);
			// After-shot "off" still running/queued when the next shot asks for "on": the "on" must succeed.
			for (int shot = 0; shot < 5; shot++)
			{
				CloriumLightControl.Power(false);
				Thread.Sleep(shot * 40); // 0 = still queued, later = already sent to the light app
				Check("next shot's on wins over pending off #" + shot, CloriumLightControl.PowerAsync(true, 10000).Result);
			}
			var older = CloriumLightControl.PowerAsync(false, 10000);
			var newer = CloriumLightControl.PowerAsync(true, 10000);
			Check("latest request applied, none hang", newer.Result && older.Wait(12000));
		}
		finally
		{
			if (!light.HasExited) light.Kill(true);
		}
		Console.WriteLine($"{passed} checks passed");
		return 0;
	}
}
