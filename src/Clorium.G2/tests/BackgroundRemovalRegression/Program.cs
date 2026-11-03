// G2 ↔ Clorium 배경제거 (Matting Studio) call check: the real G2 client source (Classes/CloriumBackgroundRemoval.cs)
// against the real Start.ps1/app.py of a Matting Studio folder laid out like the G2 package
// (<root>\outputs\matting-studio\Start.ps1, <root>\work\matting-venv, <root>\work\matting-runtime).
// The G2 배경제거 window is captured instead of shown; the engine API it uses (health, scan, batch start/status/cancel)
// runs against the real server, including one real background removal into the fixture's _bgr folder. Port 8765 must be free.
// Usage: BackgroundRemovalRegression <...\outputs\matting-studio\Start.ps1> [--keep] [--use-running]   (--keep leaves the studio running)
#pragma warning disable CS0436 // local MessageBox stub replaces the WPF one for the linked G2 source
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CameraControl.Classes;
using CameraControl.Core;

namespace System.Windows
{
	public enum MessageBoxButton { OK }
	public enum MessageBoxImage { Exclamation }
	public static class MessageBox { public static ConcurrentQueue<string> Shown = new ConcurrentQueue<string>(); public static void Show(string text, string caption, MessageBoxButton b, MessageBoxImage i) { Shown.Enqueue(text); } }
}
namespace CameraControl.Devices
{
	public static class Log { public static void Debug(string m) => Console.WriteLine("  log: " + m); public static void Error(string m, Exception e = null) => Console.WriteLine("  log error: " + m); }
}
namespace CameraControl.windows
{
	public static class G2BackgroundRemovalWnd { public static void ShowFor(string target) { } }
}
namespace CameraControl.Core
{
	public class Settings { public bool UseBackgroundRemoval = true; public string BackgroundRemovalPath = ""; }
	public static class ServiceProvider { public static Settings Settings = new Settings(); }
}

static class Program
{
	static int passed;
	static readonly ConcurrentQueue<string> opened = new ConcurrentQueue<string>();
	static void Check(string name, bool ok) { if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
	static int Messages() => System.Windows.MessageBox.Shown.Count;
	static string LastMessage() => System.Windows.MessageBox.Shown.LastOrDefault() ?? "";
	static readonly DateTime runStart = DateTime.Now;
	static int NewPython() => Process.GetProcessesByName("python").Count(p => { try { return p.StartTime >= runStart; } catch { return false; } });
	static int Listeners() => IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Count(e => e.Port == 8765);

	static int Main(string[] args)
	{
		try { return Run(args); }
		catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }
	}

	static int Run(string[] args)
	{
		string script = Path.GetFullPath(args[0]);
		bool keep = args.Contains("--keep");
		if (args.Contains("--use-running")) return UseRunning();
		string runtime = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(script))), "work", "matting-runtime");
		CloriumBackgroundRemoval.ShowStudio = target => opened.Enqueue(target);
		File.Delete(Path.Combine(runtime, "server.pid")); // stale pid of an earlier test run in this test layout
		Check("Matting Studio Start.ps1 present", File.Exists(script));
		// Windows can keep a just-closed loopback listener for a while (previous aborted run); wait for it.
		var free = DateTime.Now.AddSeconds(180); while (Listeners() > 0 && DateTime.Now < free) Thread.Sleep(1000);
		Check("port 8765 free before the run", Listeners() == 0 && !CloriumBackgroundRemoval.IsRunning());

		// G2 session fixture with Korean/space/&/# names
		string temp = Path.Combine(Path.GetTempPath(), "G2-BgRemoval-" + Guid.NewGuid().ToString("N").Substring(0, 8));
		string session = Path.Combine(temp, "2026년11월", "03일", "C05_상품 A&B #1");
		Directory.CreateDirectory(session);
		string front = Path.Combine(session, "C05_32987_001_PR_F.jpg"), raw = Path.Combine(session, "C05_32987_001_F.cr2"), webp = Path.Combine(session, "detail.webp");
		File.WriteAllBytes(front, Jpeg()); File.WriteAllText(raw, "raw"); File.WriteAllText(webp, "x");
		string frontHash = Hash(front);

		// Target selection and links (pure)
		Check("supported photo -> itself", CloriumBackgroundRemoval.ResolveTarget(front) == front);
		Check("webp counts as photo", CloriumBackgroundRemoval.ResolveTarget(webp) == webp);
		Check("RAW -> its session folder", CloriumBackgroundRemoval.ResolveTarget(raw) == session);
		Check("missing photo in existing folder -> folder", CloriumBackgroundRemoval.ResolveTarget(Path.Combine(session, "gone.jpg")) == session);
		Check("no selection / invalid / missing drive -> window without target", CloriumBackgroundRemoval.ResolveTarget(null) == null && CloriumBackgroundRemoval.ResolveTarget("bad\0path") == null && CloriumBackgroundRemoval.ResolveTarget(@"Z:\없음\a.jpg") == null);
		string fileUrl = CloriumBackgroundRemoval.NormalizeTarget(front), folderUrl = CloriumBackgroundRemoval.NormalizeTarget(session);
		Check("window target: photo itself (Korean/space/&/# path kept as is)", fileUrl == front);
		Check("window target: folder", folderUrl == session);
		Check("RAW passed directly is sent as its folder", CloriumBackgroundRemoval.NormalizeTarget(raw) == folderUrl);
		Check("no target when nothing to send", CloriumBackgroundRemoval.NormalizeTarget(null) == "" && CloriumBackgroundRemoval.NormalizeTarget(Path.Combine(session, "gone.jpg")) == "");
		ServiceProvider.Settings.BackgroundRemovalPath = "\"" + Path.ChangeExtension(script, ".cmd") + "\"";
		Check("quoted Start.cmd setting runs Start.ps1 (no extra browser tab)", CloriumBackgroundRemoval.ScriptPath == script);
		ServiceProvider.Settings.BackgroundRemovalPath = "";
		Check("default script under the G2 folder", CloriumBackgroundRemoval.ScriptPath.EndsWith(@"CloriumBackgroundRemoval\outputs\matting-studio\Start.ps1"));

		// Disabled / missing script / failing script / port used by another program
		ServiceProvider.Settings.UseBackgroundRemoval = false;
		Check("disabled: no start, no window", CloriumBackgroundRemoval.Open(front).Result == null && opened.IsEmpty && Messages() == 0);
		ServiceProvider.Settings.UseBackgroundRemoval = true;
		ServiceProvider.Settings.BackgroundRemovalPath = Path.Combine(temp, "missing", "Start.ps1");
		Check("missing script: one message, no window", CloriumBackgroundRemoval.Open(front).Result == null && Messages() == 1 && LastMessage().Contains("찾을 수 없습니다") && opened.IsEmpty);
		string failing = Path.Combine(temp, "fail", "Start.ps1"); Directory.CreateDirectory(Path.GetDirectoryName(failing));
		File.WriteAllText(failing, "param([switch]$NoBrowser)\r\nthrow 'App startup failed. See work/matting-runtime/server-error.log.'");
		ServiceProvider.Settings.BackgroundRemovalPath = failing;
		Check("failing start (Start.ps1 throws): reason shown, no window", CloriumBackgroundRemoval.Open(front).Result == null && Messages() == 2 && LastMessage().Contains("App startup failed") && opened.IsEmpty);
		File.WriteAllText(failing, "param([switch]$NoBrowser)\r\nexit 3");
		Check("failing start (exit code): reported, no window", CloriumBackgroundRemoval.Open(front).Result == null && Messages() == 3 && LastMessage().Contains("exit 3") && opened.IsEmpty);
		System.Windows.MessageBox.Shown.TryDequeue(out _); // keep the running count used below
		ServiceProvider.Settings.BackgroundRemovalPath = script;
		using (var other = new OtherProgram())
		{
			Check("another program on 8765 is not mistaken for the studio", !CloriumBackgroundRemoval.IsRunning());
			var conflict = CloriumBackgroundRemoval.Open(front).Result; Console.WriteLine($"  conflict url={conflict} messages={Messages()} last={LastMessage().Split((char)10)[0]} opened={opened.Count} python={NewPython()}");
			Check("port conflict: G2 explains, starts nothing, no window", conflict == null && Messages() == 3 && LastMessage().Contains("다른 프로그램") && opened.IsEmpty && NewPython() == 0);
			var direct = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -NoBrowser") { UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true });
			string error = direct.StandardError.ReadToEnd(); direct.WaitForExit(30000);
			Console.WriteLine($"  direct exit={direct.ExitCode} stderr={error.Replace((char)10, (char)32).Substring(0, Math.Min(300, error.Length))}");
			Check("Start.ps1 itself reports the port conflict (fixed probe)", direct.ExitCode != 0 && error.Contains("Port 8765 is used by another application") && NewPython() == 0);
		}

		Process server = null;
		try
		{
			// Cold start through the real Start.ps1, with a second click while it is starting
			var watch = Stopwatch.StartNew();
			var first = CloriumBackgroundRemoval.Open(front);
			Thread.Sleep(300);
			var second = CloriumBackgroundRemoval.Open(raw);
			string firstUrl = first.Result, secondUrl = second.Result;
			Console.WriteLine($"  cold start {watch.Elapsed.TotalSeconds:0.0}s first={firstUrl} second={secondUrl} messages={Messages()} last={LastMessage().Split((char)10)[0]} python={NewPython()}");
			if (File.Exists(Path.Combine(runtime, "server.pid"))) server = Process.GetProcessById(int.Parse(File.ReadAllText(Path.Combine(runtime, "server.pid")).Trim()));
			Check("cold start: engine running and window opened for the photo", firstUrl == fileUrl && CloriumBackgroundRemoval.IsRunning());
			Check("second click during start-up waits and opens the window for its folder", secondUrl == folderUrl);
			int serverProcesses = NewPython();
			Check("one studio server only (venv launcher + interpreter)", serverProcesses >= 1 && serverProcesses <= 2);
			Check("start-up recorded the server pid", server != null);
			Check("no error messages on a good start", Messages() == 3);
			// Clicks while the model is still loading must not be mistaken for "not running".
			for (int i = 0; i < 3; i++) Check("click while the model loads #" + i, CloriumBackgroundRemoval.Open(front).Result == fileUrl && NewPython() == serverProcesses && Messages() == 3);

			// Running: window only, fast
			watch.Restart();
			Check("running: window for the photo", CloriumBackgroundRemoval.Open(webp).Result == webp);
			Check("running: window without selection", CloriumBackgroundRemoval.Open(null).Result == "");
			for (int i = 0; i < 5; i++) Check("repeated rail clicks #" + i, CloriumBackgroundRemoval.Open(i % 2 == 0 ? front : raw).Result == (i % 2 == 0 ? fileUrl : folderUrl));
			Check("running clicks are fast (<3 s for 7)", watch.Elapsed.TotalSeconds < 3);
			Check("still one server, no new start", NewPython() == serverProcesses && File.ReadAllText(Path.Combine(runtime, "server.pid")).Trim() == server.Id.ToString());
			Check("no browser page is opened (window targets only)", opened.All(o => o == "" || File.Exists(o) || Directory.Exists(o)));

			// Studio closed by the operator: next click starts it again
			server.Kill(true); server.WaitForExit(10000);
			var deadline = DateTime.Now.AddSeconds(10); while (CloriumBackgroundRemoval.IsRunning() && DateTime.Now < deadline) Thread.Sleep(200);
			Check("studio stopped", !CloriumBackgroundRemoval.IsRunning() && NewPython() == 0);
			string restarted = CloriumBackgroundRemoval.Open(front).Result;
			server = Process.GetProcessById(int.Parse(File.ReadAllText(Path.Combine(runtime, "server.pid")).Trim()));
			Check("after the studio was closed the next click restarts it", restarted == fileUrl && CloriumBackgroundRemoval.IsRunning() && NewPython() == serverProcesses && Messages() == 3);
			Check("source photo unchanged, nothing written next to it", Hash(front) == frontHash && !Directory.Exists(session + "_bgr"));

			EngineApi(temp, session, front, frontHash);
		}
		finally
		{
			// Only the server this run started (pid file of the test layout); stop it even when a check failed.
			if (server == null && File.Exists(Path.Combine(runtime, "server.pid")))
				try { server = Process.GetProcessById(int.Parse(File.ReadAllText(Path.Combine(runtime, "server.pid")).Trim())); } catch (ArgumentException) { }
			bool failed = passed == 0 || !CloriumBackgroundRemoval.IsRunning();
			if ((!keep || failed) && server != null && !server.HasExited && server.StartTime >= runStart) { server.Kill(true); server.WaitForExit(10000); }
		}
		Console.WriteLine("TARGETS:");
		Console.WriteLine(fileUrl); Console.WriteLine(folderUrl); Console.WriteLine(CloriumBackgroundRemoval.NormalizeTarget(webp));
		Console.WriteLine("FIXTURE: " + session);
		Console.WriteLine($"{passed} checks passed" + (keep ? " (studio left running)" : ""));
		return 0;
	}

	// Engine API used by the G2 배경제거 window (POSTs carry the server's per-run token).
	static void EngineApi(string temp, string session, string front, string frontHash)
	{
		var ready = DateTime.Now.AddSeconds(240); (bool Ready, string Message) health;
		while (!(health = CloriumBackgroundRemoval.Health()).Ready && DateTime.Now < ready) Thread.Sleep(1000);
		Console.WriteLine($"  health ready={health.Ready} message={health.Message}");
		Check("engine reports the model ready", health.Ready);
		var filePlan = CloriumBackgroundRemoval.Scan(new[] { front }, false);
		Check("scan photo: 1 photo, output beside the session folder, nothing written", filePlan.GetProperty("total").GetInt32() == 1 && filePlan.GetProperty("output").GetString() == session + "_bgr" && !Directory.Exists(session + "_bgr"));
		var folderPlan = CloriumBackgroundRemoval.Scan(new[] { session }, true);
		Check("scan folder: jpg + webp listed, RAW skipped", folderPlan.GetProperty("total").GetInt32() == 2);
		bool refused = false; try { CloriumBackgroundRemoval.Scan(new[] { Path.Combine(temp, "없는 폴더") }, true); } catch (CloriumBackgroundRemoval.ApiException e) { refused = e.Message.Contains("폴더"); }
		Check("scan of a missing folder: the engine's message comes back", refused);
		var job = CloriumBackgroundRemoval.StartBatch(filePlan.GetProperty("plan_id").GetString(), 1024, 1, true, false);
		string jobId = job.GetProperty("id").GetString(); var done = DateTime.Now.AddSeconds(240); System.Text.Json.JsonElement status;
		while ((status = CloriumBackgroundRemoval.BatchStatus(jobId)).GetProperty("status").GetString() == "running" && DateTime.Now < done) Thread.Sleep(500);
		Console.WriteLine($"  batch status={status.GetProperty("status").GetString()} success={status.GetProperty("success").GetInt32()} seconds={status.GetProperty("seconds")}");
		Check("batch: one photo processed", status.GetProperty("status").GetString() == "done" && status.GetProperty("success").GetInt32() == 1);
		Check("batch: before/after thumbnails for the window", status.GetProperty("preview").GetProperty("result").GetString().StartsWith("data:image/png;base64,"));
		Check("batch: transparent PNG saved in _bgr, original unchanged", File.Exists(Path.Combine(session + "_bgr", "C05_32987_001_PR_F_bgr.png")) && Hash(front) == frontHash);
		Check("cancel request is accepted", CloriumBackgroundRemoval.CancelBatch(jobId).GetProperty("message").GetString().Length > 0);
	}

	// --use-running: the engine is already running (e.g. started by the G2 test runtime). It is not stopped or
	// restarted; the window target rules and the engine API are checked against it with a temporary fixture.
	static int UseRunning()
	{
		CloriumBackgroundRemoval.ShowStudio = target => opened.Enqueue(target);
		Check("engine already running on 8765", CloriumBackgroundRemoval.IsRunning());
		string temp = Path.Combine(Path.GetTempPath(), "G2-BgRemoval-" + Guid.NewGuid().ToString("N").Substring(0, 8));
		string session = Path.Combine(temp, "2026년11월", "03일", "C05_상품 A&B #1");
		Directory.CreateDirectory(session);
		string front = Path.Combine(session, "C05_32987_001_PR_F.jpg"), raw = Path.Combine(session, "C05_32987_001_F.cr2"), webp = Path.Combine(session, "detail.webp");
		File.WriteAllBytes(front, Jpeg()); File.WriteAllText(raw, "raw"); File.WriteAllText(webp, "x");
		string frontHash = Hash(front);
		Check("rail click opens the window for the photo (running engine)", CloriumBackgroundRemoval.Open(front).Result == front && opened.Last() == front);
		Check("RAW selection opens the window for its folder", CloriumBackgroundRemoval.Open(raw).Result == session && opened.Last() == session);
		Check("no selection opens the window without target", CloriumBackgroundRemoval.Open(null).Result == "" && opened.Last() == "");
		Check("no error messages", Messages() == 0);
		EngineApi(temp, session, front, frontHash);
		Console.WriteLine("FIXTURE: " + session);
		Console.WriteLine($"{passed} checks passed (engine left running)");
		return 0;
	}
	static string Get(string url) { using var http = new System.Net.Http.HttpClient(); return http.GetStringAsync(url).Result; }
	static string Hash(string file) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(file)));

	static byte[] Jpeg()
	{
		using var bitmap = new System.Drawing.Bitmap(320, 240);
		using (var g = System.Drawing.Graphics.FromImage(bitmap)) { g.Clear(System.Drawing.Color.White); g.FillEllipse(System.Drawing.Brushes.DarkSlateBlue, 80, 40, 160, 160); }
		using var stream = new MemoryStream(); bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg); return stream.ToArray();
	}

	// A different HTTP program holding 127.0.0.1:8765 (answers /api/health with another app name).
	sealed class OtherProgram : IDisposable
	{
		readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 8765);
		readonly CancellationTokenSource stop = new CancellationTokenSource();
		public OtherProgram()
		{
			listener.Start();
			Task.Run(async () =>
			{
				while (!stop.IsCancellationRequested)
				{
					TcpClient client;
					try { client = await listener.AcceptTcpClientAsync(stop.Token); } catch { return; }
					_ = Task.Run(() =>
					{
						using (client)
						{
							var stream = client.GetStream(); var buffer = new byte[4096]; try { stream.Read(buffer, 0, buffer.Length); } catch { return; }
							byte[] body = Encoding.UTF8.GetBytes("{\"app\":\"other\"}");
							byte[] head = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nConnection: close\r\nContent-Length: " + body.Length + "\r\n\r\n");
							try { stream.Write(head); stream.Write(body); } catch { }
						}
					});
				}
			});
		}
		public void Dispose() { stop.Cancel(); listener.Stop(); }
	}
}
