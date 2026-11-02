// G2 ↔ Clorium AutoSizing Desk call check: the real G2 client source (Classes/CloriumAutoSizing.cs) against the
// real built Desk executable, on an isolated pipe name (USERNAME) and settings file (CLORIUM_AUTOSIZING_SETTINGS).
// Fixture photos are generated in %TEMP%; no user photo, Desk setting or installed copy is touched.
// Usage: AutoSizingRegression <folder containing "Clorium AutoSizing.exe">
#pragma warning disable CS0436 // local MessageBox stub replaces the WPF one for the linked G2 source
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
	public class Settings { public bool UseAutoSizing = true; public string AutoSizingPath = ""; }
	public static class ServiceProvider { public static Settings Settings = new Settings(); }
}

static class Program
{
	static int passed;
	static void Check(string name, bool ok) { if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
	static string Str(JsonElement? e, string name) => e.HasValue && e.Value.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
	static bool Ok(JsonElement? e) => e.HasValue && e.Value.GetProperty("ok").ValueKind == JsonValueKind.True;
	static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
	const string DeskProcess = "Clorium AutoSizing";

	static int Main(string[] args)
	{
		string exe = Path.Combine(Path.GetFullPath(args[0]), "Clorium AutoSizing.exe");
		string user = "g2as" + Guid.NewGuid().ToString("N").Substring(0, 8);
		string temp = Path.Combine(Path.GetTempPath(), "G2-AutoSizing-" + user);
		Environment.SetEnvironmentVariable("USERNAME", user);
		Environment.SetEnvironmentVariable("CLORIUM_AUTOSIZING_SETTINGS", Path.Combine(temp, "desk-settings.json"));
		string userSettings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CloriumMeasure", "settings.json");
		string userSettingsHash = File.Exists(userSettings) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(userSettings))) : "none";
		int existingDesks = Process.GetProcessesByName(DeskProcess).Length;
		Check("Desk executable built", File.Exists(exe));

		// G2 session fixture: DownloadFolder/.../C05_32987 with PR front, plain back, RAW and the _M measurement copy.
		string session = Path.Combine(temp, "2026년11월", "03일", "C05_32987"), measure = Path.Combine(session, CloriumAutoSizing.MeasureFolder);
		Directory.CreateDirectory(measure);
		string front = Path.Combine(session, "C05_32987_001_PR_F.jpg"), back = Path.Combine(session, "C05_32987_001_B.jpg"), raw = Path.Combine(session, "C05_32987_001_F.cr2");
		string frontM = Path.Combine(measure, "C05_32987_001_F_M.jpeg"), korean = Path.Combine(temp, "한글 폴더 'quote'", "상품 사진.jpg");
		Jpeg(front, null); Jpeg(back, null); File.WriteAllText(raw, "raw"); Jpeg(frontM, "0.25"); Jpeg(korean, "0.3");
		string other = Path.Combine(temp, "other"); Directory.CreateDirectory(other); string loose = Path.Combine(other, "IMG_0001.cr2"); File.WriteAllText(loose, "raw");

		// Target selection (pure)
		Check("PR front -> its _M measurement copy (PR stripped)", Same(CloriumAutoSizing.ResolveTarget(front), frontM));
		Check("plain front name -> same _M copy", Same(CloriumAutoSizing.ResolveTarget(Path.Combine(session, "C05_32987_001_F.jpg")), frontM));
		Check("back without measurement -> the photo itself", Same(CloriumAutoSizing.ResolveTarget(back), back));
		Check("RAW front -> its _M measurement copy", Same(CloriumAutoSizing.ResolveTarget(raw), frontM));
		string rawBack = Path.Combine(session, "C05_32987_002_B.cr2"); File.WriteAllText(rawBack, "raw");
		Check("RAW without measurement -> 치수측정 folder", Same(CloriumAutoSizing.ResolveTarget(rawBack), measure));
		Check("measurement photo itself -> unchanged", Same(CloriumAutoSizing.ResolveTarget(frontM), frontM));
		Check("RAW in folder without 치수측정 -> its folder", Same(CloriumAutoSizing.ResolveTarget(loose), other));
		Check("no selection -> nothing (just show Desk)", CloriumAutoSizing.ResolveTarget(null) == null && CloriumAutoSizing.ResolveTarget("  ") == null);
		Check("invalid path -> nothing, no exception", CloriumAutoSizing.ResolveTarget("bad\0path") == null && CloriumAutoSizing.ResolveTarget(@"Z:\없는\폴더\a.jpg") == null);
		Check("open request JSON", JsonDocument.Parse(CloriumAutoSizing.BuildRequest(korean)).RootElement is var o && o.GetProperty("command").GetString() == "open" && o.GetProperty("path").GetString() == korean);
		Check("browse request JSON", JsonDocument.Parse(CloriumAutoSizing.BuildRequest(measure)).RootElement.GetProperty("command").GetString() == "browse");
		Check("show request when nothing/missing", CloriumAutoSizing.BuildRequest(null).Contains("show") && CloriumAutoSizing.BuildRequest(Path.Combine(session, "missing.jpg")).Contains("show"));

		// Disabled / missing executable
		ServiceProvider.Settings.UseAutoSizing = false;
		Check("disabled: rail call is a no-op", CloriumAutoSizing.Open(frontM).Result == null && System.Windows.MessageBox.Shown == 0 && !CloriumAutoSizing.IsRunning());
		ServiceProvider.Settings.UseAutoSizing = true;
		ServiceProvider.Settings.AutoSizingPath = @"C:\nonexistent\Clorium AutoSizing.exe";
		var watch = Stopwatch.StartNew();
		Check("missing executable: one message, no throw", CloriumAutoSizing.Open(frontM).Result == null && System.Windows.MessageBox.Shown == 1);
		Check("missing executable: bounded UI time", watch.Elapsed.TotalSeconds < 2);
		ServiceProvider.Settings.AutoSizingPath = "\"" + exe + "\"";
		Check("quoted executable path accepted", CloriumAutoSizing.ExecutablePath == exe);

		try
		{
			// Cold start with the measurement photo on the command line
			watch.Restart();
			var first = CloriumAutoSizing.Open(frontM);
			Check("cold start returns without waiting for Desk", watch.Elapsed.TotalSeconds < 2 && first.Result == null);
			WaitRunning();
			var state = CloriumAutoSizing.Open(null).Result;
			Check("cold start opened the _M photo with G2 scale", Ok(state) && Same(Str(state, "file"), frontM) && state.Value.GetProperty("scale").GetBoolean());
			Check("Desk browses the 치수측정 folder", Same(Str(state, "folder"), measure));

			// Running: open / browse / show
			var opened = CloriumAutoSizing.Open(CloriumAutoSizing.ResolveTarget(back)).Result;
			Check("running: open another photo", Ok(opened) && Same(Str(opened, "file"), back) && !opened.Value.GetProperty("scale").GetBoolean());
			var unicode = CloriumAutoSizing.Open(korean).Result;
			Check("running: Korean/space/quote path", Ok(unicode) && Same(Str(unicode, "file"), korean) && unicode.Value.GetProperty("scale").GetBoolean());
			var browsed = CloriumAutoSizing.Open(CloriumAutoSizing.ResolveTarget(rawBack)).Result;
			Check("running: browse 치수측정 folder keeps photo", Ok(browsed) && Same(Str(browsed, "folder"), measure) && Same(Str(browsed, "file"), korean));
			var again = CloriumAutoSizing.Open(korean).Result;
			Check("running: same photo again is not reloaded", Ok(again) && Same(Str(again, "file"), korean) && !again.Value.GetProperty("dirty").GetBoolean());
			for (int i = 0; i < 5; i++) Check("repeated rail clicks #" + i, Same(Str(CloriumAutoSizing.Open(i % 2 == 0 ? frontM : back).Result, "file"), i % 2 == 0 ? frontM : back));
			Check("single Desk window process", DeskCount() == 1);

			// Operator double-clicks Desk directly: forwarded to the same window
			using (var second = Process.Start(new ProcessStartInfo(exe) { ArgumentList = { korean }, UseShellExecute = false }))
			{
				Check("second Desk start exits after forwarding", second.WaitForExit(15000));
			}
			Check("second start's photo shown in the first window", Same(Str(CloriumAutoSizing.Open(null).Result, "file"), korean) && DeskCount() == 1);
		}
		finally { CloseDesks(); }

		// Rapid double click while Desk is still starting: the second request waits for the pipe and wins.
		typeof(CloriumAutoSizing).GetField("_lastLaunch", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, DateTime.MinValue);
		try
		{
			Check("Desk closed before start-up race", !CloriumAutoSizing.IsRunning());
			var launch = CloriumAutoSizing.Open(frontM);
			var queued = CloriumAutoSizing.Open(back);
			var delivered = queued.Result;
			Check("click during start-up delivered after Desk is ready", launch.Result == null && Ok(delivered) && Same(Str(delivered, "file"), back));
			Check("start-up race left one Desk", DeskCount() == 1);
		}
		finally { CloseDesks(); }

		Check("Desk exits cleanly (no unsaved work)", DeskCount() == 0);
		Check("user's Desk settings untouched", userSettingsHash == (File.Exists(userSettings) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(userSettings))) : "none"));
		Check("isolated Desk settings used", File.Exists(Path.Combine(temp, "desk-settings.json")));
		Check("fixture photos unchanged / no results saved", Directory.GetFiles(temp, "*_sd.*", SearchOption.AllDirectories).Length == 0 && Directory.GetFiles(temp, "*.csv", SearchOption.AllDirectories).Length == 0);
		try { Directory.Delete(temp, true); } catch (IOException) { }
		Console.WriteLine($"{passed} checks passed");
		return 0;

		int DeskCount() => Process.GetProcessesByName(DeskProcess).Length - existingDesks;
	}

	static void WaitRunning()
	{
		DateTime deadline = DateTime.Now.AddSeconds(30);
		while (!CloriumAutoSizing.IsRunning() && DateTime.Now < deadline) Thread.Sleep(300);
		Check("Desk pipe reachable", CloriumAutoSizing.IsRunning());
	}

	// Close only the Desk windows this run started (isolated pipe => found by the env-specific window title is not possible; use start time).
	static readonly DateTime started = DateTime.Now;
	static void CloseDesks()
	{
		foreach (var desk in Process.GetProcessesByName(DeskProcess).Where(p => { try { return p.StartTime >= started; } catch { return false; } }))
		{
			desk.CloseMainWindow();
			if (!desk.WaitForExit(10000)) desk.Kill();
		}
		Thread.Sleep(500);
	}

	// Same metadata as Clorium G2 MainWindow.WriteMetadata (Title = Clorium ID, Subject = mm/pixel, Comment = API host).
	static void Jpeg(string path, string scale)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		var pixels = new byte[160 * 120 * 3]; for (int i = 0; i < pixels.Length; i++) pixels[i] = (byte)(i % 200);
		var source = BitmapSource.Create(160, 120, 96, 96, PixelFormats.Rgb24, null, pixels, 160 * 3);
		var metadata = new BitmapMetadata("jpg") { Title = "C05_" };
		if (scale != null) { metadata.Subject = scale; metadata.Comment = "https://test.invalid"; }
		var encoder = new JpegBitmapEncoder { QualityLevel = 30 }; encoder.Frames.Add(BitmapFrame.Create(source, null, metadata, null));
		using var output = File.Create(path); encoder.Save(output);
	}
}
