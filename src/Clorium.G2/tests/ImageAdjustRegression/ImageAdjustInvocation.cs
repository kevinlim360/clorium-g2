// net48 check of the G2 left-rail 이미지보정 call (run by Run-ImageAdjust.ps1 against a built CameraControl.exe):
// CloriumImageAdjust.Open with the switch off/on, no/missing/RAW/unsupported/odd-path selections, window failures,
// and the real default path that opens the actual G2ColorAdjustWnd modal on a generated JPEG (C# 5 for Framework csc).
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CameraControl.Core;
using CameraControl.Core.Classes;

class ImageAdjustInvocation
{
	static int count;
	static Type client;
	static List<string> messages = new List<string>();
	static List<string> opened = new List<string>();

	static void Check(string name, bool value) { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); count++; }

	static string Open(string selected) { return (string)client.GetMethod("Open").Invoke(null, new object[] { selected, null }); }

	static object Hook(string name) { return client.GetField(name, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null); }

	static void SetHook(string name, object value) { client.GetField(name, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, value); }

	static string Sha(string path)
	{
		using (var s = File.OpenRead(path)) using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(s)).Replace("-", "");
	}

	static string Jpeg(string dir, string name)
	{
		int w = 1200, h = 800, stride = w * 3;
		var px = new byte[stride * h];
		for (int i = 0; i < px.Length; i += 3) { px[i] = 90; px[i + 1] = (byte)(i % 200); px[i + 2] = 180; }
		var enc = new JpegBitmapEncoder { QualityLevel = 92 };
		enc.Frames.Add(BitmapFrame.Create(BitmapSource.Create(w, h, 300, 300, PixelFormats.Bgr24, null, px, stride)));
		string path = Path.Combine(dir, name);
		using (var f = File.Create(path)) enc.Save(f);
		return path;
	}

	static void Expect(string label, string selected, string target, string message)
	{
		messages.Clear(); opened.Clear();
		string result = Open(selected);
		Check(label, result == target && (target == null ? opened.Count == 0 : opened.Count == 1 && opened[0] == target) && (message == null ? messages.Count == 0 : messages.Count == 1 && messages[0].Contains(message)));
	}

	[STAThread]
	static int Main(string[] args)
	{
		string bin = args[0];
		AppDomain.CurrentDomain.AssemblyResolve += delegate(object s, ResolveEventArgs e)
		{
			string f = Path.Combine(bin, new AssemblyName(e.Name).Name + ".dll");
			return File.Exists(f) ? Assembly.LoadFrom(f) : null;
		};
		string dir = Path.Combine(Path.GetTempPath(), "G2-ImageAdjust-" + Guid.NewGuid().ToString("N").Substring(0, 8));
		Directory.CreateDirectory(dir);
		try
		{
			Run(bin, dir);
			Console.WriteLine(count + " image adjust invocation checks passed");
			return 0;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine(e);
			return 1;
		}
		finally
		{
			try { Directory.Delete(dir, true); } catch (Exception) { }
		}
	}

	static void Run(string bin, string dir)
	{
		var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
		SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
		ServiceProvider.Branding = (Branding)FormatterServices.GetUninitializedObject(typeof(Branding));
		var settings = new Settings(); ServiceProvider.Settings = settings;
		Assembly g2 = Assembly.LoadFrom(Path.Combine(bin, "CameraControl.exe"));
		client = g2.GetType("CameraControl.Classes.CloriumImageAdjust", true);
		Type wndType = g2.GetType("CameraControl.windows.G2ColorAdjustWnd", true);
		Check("legacy ColorCompensationWnd / view model are not in the build", g2.GetType("CameraControl.windows.ColorCompensationWnd") == null && g2.GetType("CameraControl.ViewModel.ColorCompensationViewModel") == null);
		// templates of this run stay in the temp folder, never the operator's %APPDATA% file
		wndType.GetProperty("TemplateStorePath").SetValue(null, Path.Combine(dir, "templates", "color-templates.xml"));

		var realWindow = Hook("ShowWindow");
		SetHook("ShowMessage", new Action<string>(delegate(string m) { messages.Add(m); }));
		SetHook("ShowWindow", new Action<string, Window>(delegate(string f, Window o) { opened.Add(f); }));

		string shots = Path.Combine(dir, "촬영 폴더 (F)");
		Directory.CreateDirectory(shots);
		string jpg = Jpeg(shots, "P01_F.jpg");

		settings.UseImageAdjust = false;
		Expect("switch OFF: rail call does nothing (no window, no message)", jpg, null, null);
		settings.UseImageAdjust = true;
		Expect("nothing selected → asks to select a photo", null, null, "먼저 선택");
		Expect("blank selection → asks to select a photo", "  ", null, "먼저 선택");
		Expect("selected file deleted meanwhile → asks to select a photo", Path.Combine(shots, "gone.jpg"), null, "먼저 선택");
		Expect("JPEG with spaces/Korean/brackets in the path opens itself", jpg, jpg, null);

		foreach (string ext in new[] { ".JPG", ".jpeg", ".png", ".tif", ".tiff", ".bmp" })
		{
			string f = Path.Combine(shots, "ext" + ext.Replace(".", "_") + ext);
			File.Copy(jpg, f);
			Expect("supported " + ext + " opens itself", f, f, null);
		}

		string raw = Path.Combine(shots, "P02_F.CR3");
		File.WriteAllBytes(raw, new byte[] { 1, 2, 3 });
		Expect("RAW without a same-name photo → unsupported message", raw, null, "JPEG, PNG, TIFF, BMP");
		string rawJpg = Path.Combine(shots, "P02_F.JPG");
		File.Copy(jpg, rawJpg);
		Expect("RAW+JPEG capture: RAW selected opens the same-name JPEG", raw, Path.GetFullPath(Path.ChangeExtension(raw, ".jpg")), null);
		string nef = Path.Combine(shots, "P03_F.nef");
		File.WriteAllBytes(nef, new byte[] { 1 });
		File.Copy(jpg, Path.Combine(shots, "P03_F.tif"));
		Expect("RAW with only a same-name TIFF opens the TIFF", nef, Path.Combine(shots, "P03_F.tif"), null);
		string webp = Path.Combine(shots, "P04_F.webp");
		File.WriteAllBytes(webp, new byte[] { 1 });
		Expect("WEBP (not supported by the engine) → unsupported message", webp, null, "JPEG, PNG, TIFF, BMP");
		string mp4 = Path.Combine(shots, "P05.mp4");
		File.WriteAllBytes(mp4, new byte[] { 1 });
		Expect("video selected → unsupported message", mp4, null, "JPEG, PNG, TIFF, BMP");

		SetHook("ShowWindow", new Action<string, Window>(delegate(string f, Window o) { throw new InvalidOperationException("Magick.NET native missing"); }));
		messages.Clear();
		string failed = Open(jpg);
		Check("window failure is reported, not thrown into G2", failed == null && messages.Count == 1 && messages[0].Contains("열지 못했습니다") && messages[0].Contains("Magick.NET native missing"));

		// Real default: the actual G2ColorAdjustWnd modal opens on the selected JPEG; a timer checks it and closes it.
		SetHook("ShowWindow", realWindow);
		string hash = Sha(jpg);
		string title = null, header = null, stage = null; int width = 0; bool loaded = false;
		var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
		DateTime giveUp = DateTime.Now.AddSeconds(60);
		timer.Tick += delegate
		{
			foreach (Window w in app.Windows)
			{
				if (!wndType.IsInstanceOfType(w)) continue;
				var busy = (Border)w.FindName("BusyOverlay");
				if (busy.Visibility != Visibility.Collapsed && DateTime.Now < giveUp) return;
				loaded = busy.Visibility == Visibility.Collapsed;
				title = w.Title; header = ((TextBlock)w.FindName("FileTitle")).Text; stage = ((TextBlock)w.FindName("FileInfoText")).Text;
				var src = ((Image)w.FindName("StageImage")).Source as BitmapSource; width = src == null ? 0 : src.PixelWidth;
				timer.Stop(); w.Close(); return;
			}
		};
		timer.Start();
		messages.Clear();
		string real = Open(jpg);
		timer.Stop();
		Check("real call opens the actual G2ColorAdjustWnd modal and returns after it closes", real == jpg && messages.Count == 0 && title != null);
		Check("window titled 이미지보정 with the photo name", title == "이미지보정 — P01_F.jpg" && header == "P01_F.jpg");
		Check("photo loads in the window (1200x800, preview shown)", loaded && stage != null && stage.StartsWith("1200×800") && width > 0);
		Check("opening and closing without saving leaves the original untouched and writes nothing", Sha(jpg) == hash && !Directory.Exists(Path.Combine(shots, "_cc")) && Directory.GetFiles(shots, "*_cc*").Length == 0);
		app.Shutdown();
	}
}
