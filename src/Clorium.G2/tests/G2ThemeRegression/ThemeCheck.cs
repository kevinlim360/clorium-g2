// Actual WPF windows of the built G2 in 밝은 화면 and 어두운 화면 (G2Palette.Dark), rendered to PNG for review, plus the
// measurement-copy keyword round trip used by 정사각형 판 calibration (C# 5 for Framework csc; run by Run-ThemeCheck.ps1).
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CameraControl.Core;
using CameraControl.Core.Classes;

class ThemeCheck
{
	static int count;
	static string lastError = "";
	static void Check(string name, bool ok) { if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); count++; }

	static void Pump(int ms)
	{
		DateTime end = DateTime.Now.AddMilliseconds(ms);
		while (DateTime.Now < end) { Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { })); Thread.Sleep(15); }
	}

	static void Snapshot(Window w, string path)
	{
		var root = (FrameworkElement)w.Content;
		root.UpdateLayout();
		int pw = (int)Math.Ceiling(root.ActualWidth), ph = (int)Math.Ceiling(root.ActualHeight);
		var rtb = new RenderTargetBitmap(pw, ph, 96, 96, PixelFormats.Pbgra32);
		var dv = new DrawingVisual();
		using (DrawingContext dc = dv.RenderOpen())
		{
			dc.DrawRectangle(w.Background, null, new Rect(0, 0, pw, ph));
			dc.DrawRectangle(new VisualBrush(root), null, new Rect(0, 0, pw, ph));
		}
		rtb.Render(dv);
		var enc = new PngBitmapEncoder();
		enc.Frames.Add(BitmapFrame.Create(rtb));
		using (var f = File.Create(path)) enc.Save(f);
	}

	static bool EngineUp() { try { using (var c = new System.Net.Sockets.TcpClient()) return c.ConnectAsync("127.0.0.1", 8765).Wait(1000) && c.Connected; } catch { return false; } }

	static Color Bg(Window w) { return ((SolidColorBrush)w.Background).Color; }

	[STAThread]
	static int Main(string[] args)
	{
		string bin = args[0], outDir = args[1];
		Directory.CreateDirectory(outDir);
		try
		{
			var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
			SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
			ServiceProvider.Branding = (Branding)FormatterServices.GetUninitializedObject(typeof(Branding));
			ServiceProvider.Settings = new Settings();
			CameraControl.Devices.Log.LogError += a => { lastError = a.Message + " " + a.Exception; };
			Assembly g2 = Assembly.LoadFrom(Path.Combine(bin, "CameraControl.exe"));
			PropertyInfo dark = g2.GetType("CameraControl.Classes.G2Palette", true).GetProperty("Dark");
			Type license = g2.GetType("CameraControl.windows.G2LicenseWnd", true);
			Type adjust = g2.GetType("CameraControl.windows.G2ColorAdjustWnd", true);
			adjust.GetProperty("TemplateStorePath").SetValue(null, Path.Combine(outDir, "templates.xml"), null);
			string jpeg = Path.Combine(outDir, "G2THEME_F.jpg");
			MakeJpeg(jpeg);

			foreach (bool d in new[] { false, true })
			{
				dark.SetValue(null, d, null);
				string mode = d ? "dark" : "light";
				var lw = (Window)Activator.CreateInstance(license);
				lw.Show(); Pump(400);
				Snapshot(lw, Path.Combine(outDir, "license-" + mode + ".png"));
				Color page = Bg(lw);
				Check("라이선스 window opens in " + mode + " (" + page + ")", d ? page.R < 0x30 : page.R > 0xE0);
				var state = (TextBlock)lw.FindName("StateText");
				Check("라이선스 window shows a state (" + state.Text + ")", state.Text.Length > 0 && state.Text != "확인 중");
				lw.Close();

				var aw = (Window)Activator.CreateInstance(adjust, jpeg, false);
				aw.Width = 1320; aw.Height = 860; aw.Show();
				DateTime until = DateTime.Now.AddSeconds(60);
				while (((Border)aw.FindName("BusyOverlay")).Visibility != Visibility.Collapsed && DateTime.Now < until) Pump(100);
				Pump(400);
				Snapshot(aw, Path.Combine(outDir, "image-adjust-" + mode + ".png"));
				var title = (TextBlock)aw.FindName("FileTitle");
				Color text = ((SolidColorBrush)title.Foreground).Color;
				Check("이미지보정 window in " + mode + ": text " + text, d ? text.R > 0xC0 : text.R < 0x40);
				aw.Close();
			}
			dark.SetValue(null, false, null);

			// 바로 전환: a window opened light turns dark (and back) without reopening.
			MethodInfo apply = g2.GetType("CameraControl.Classes.G2Palette", true).GetMethod("Apply");
			var live = (Window)Activator.CreateInstance(license);
			live.Show(); Pump(300);
			Check("open window starts light", Bg(live).R > 0xE0);
			apply.Invoke(null, new object[] { true }); Pump(300);
			Snapshot(live, Path.Combine(outDir, "license-live-dark.png"));
			var liveState = (TextBlock)live.FindName("StateText");
			Check("same window is dark at once (" + Bg(live) + ", label " + ((SolidColorBrush)((TextBlock)((StackPanel)live.Content).Children[0]).Foreground).Color + ")", Bg(live).R < 0x30 && ((SolidColorBrush)((TextBlock)((StackPanel)live.Content).Children[0]).Foreground).Color.R > 0xC0);
			apply.Invoke(null, new object[] { false }); Pump(300);
			Check("and light again", Bg(live).R > 0xE0);
			live.Close();

			// 라이브뷰 (no camera) and 배경제거 (needs the engine on 8765, e.g. the G2 test runtime's) in both themes.
			// The legacy live view expects G2's App.xaml theme dictionaries (MahApps / MaterialDesign) at application level.
			foreach (string src in new[] { "MahApps.Metro;component/Styles/Controls.xaml", "MahApps.Metro;component/Styles/Fonts.xaml", "MahApps.Metro;component/Styles/Themes/Light.Blue.xaml", "MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml", "MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml", "MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Grey.xaml", "MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Blue.xaml" })
				app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/" + src) });
			Type liveView = g2.GetType("CameraControl.windows.LiveViewWnd", true);
			Type bgr = g2.GetType("CameraControl.windows.G2BackgroundRemovalWnd", true);
			foreach (bool d in new[] { false, true })
			{
				apply.Invoke(null, new object[] { d }); Pump(200);
				string mode = d ? "dark" : "light";
				lastError = "";
				var lv = (Window)Activator.CreateInstance(liveView, new object[] { null });
				if (lastError.Length > 0) throw new Exception("라이브뷰 did not load: " + lastError);
				lv.Width = 1280; lv.Height = 820; lv.Show(); Pump(600);
				Snapshot(lv, Path.Combine(outDir, "liveview-" + mode + ".png"));
				Check("라이브뷰 builds without a camera and uses the G2 page in " + mode + " (" + Bg(lv) + ")", lv.FindName("_image") != null && lv.FindName("ExpanderCalibration") != null && (d ? Bg(lv).R < 0x30 : Bg(lv).R > 0xE0));
				lv.Hide(); // its Closing hands over to WindowsManager (not set up here)

				if (!EngineUp()) { Console.WriteLine("SKIP: 배경제거 window checks (engine not running on 8765)"); continue; }
				var bw = (Window)Activator.CreateInstance(bgr);
				bw.Show();
				bgr.GetMethod("SetTarget", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bw, new object[] { jpeg });
				var card = (FrameworkElement)bw.FindName("ScanCard");
				var engine = (TextBlock)bw.FindName("EngineText");
				DateTime until2 = DateTime.Now.AddSeconds(20);
				while ((card.Visibility != Visibility.Visible || engine.Text == "엔진 확인 중") && DateTime.Now < until2) Pump(100);
				Pump(300);
				Snapshot(bw, Path.Combine(outDir, "background-removal-" + mode + ".png"));
				string summary = ((TextBlock)bw.FindName("ScanSummary")).Text;
				Console.WriteLine("  배경제거 chip tip=" + ((FrameworkElement)bw.FindName("EngineChip")).ToolTip + " scanError=" + ((TextBlock)bw.FindName("ScanError")).Text);
				Check("배경제거 window scans the photo with the engine in " + mode + " (" + engine.Text + ", " + summary + ")", engine.Text == "준비 완료" && summary.StartsWith("사진 1장") && ((Button)bw.FindName("StartButton")).IsEnabled && (d ? Bg(bw).R < 0x30 : Bg(bw).R > 0xE0));
				Check("scan wrote nothing next to the photo", !Directory.Exists(outDir + "_bgr"));
				var src = (Image)bw.FindName("SourceImage");
				DateTime until3 = DateTime.Now.AddSeconds(10);
				while (!(src.Source is BitmapSource) && DateTime.Now < until3) Pump(100);
				Check("원본 is the full-size photo, not a thumbnail (" + (src.Source is BitmapSource ? ((BitmapSource)src.Source).PixelWidth : 0) + " px)", src.Source is BitmapSource && ((BitmapSource)src.Source).PixelWidth == 1600);
				var zoomText = (TextBlock)bw.FindName("ZoomText");
				bgr.GetMethod("ZoomActual_Click", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bw, new object[] { null, null }); Pump(200);
				Snapshot(bw, Path.Combine(outDir, "background-removal-100-" + mode + ".png"));
				Check("100% button zooms both panes together (" + zoomText.Text + ")", zoomText.Text == "100%" && ((FrameworkElement)bw.FindName("SourceCanvas")).RenderTransform == ((FrameworkElement)bw.FindName("ResultCanvas")).RenderTransform && ((FrameworkElement)bw.FindName("SourceCanvas")).RenderTransform.Value.M11 > 1);
				bgr.GetMethod("ZoomFit_Click", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bw, new object[] { null, null }); Pump(100);
				Check("맞춤 returns to the whole photo (" + zoomText.Text + ")", zoomText.Text.StartsWith("맞춤") && ((FrameworkElement)bw.FindName("SourceCanvas")).RenderTransform.Value.M11 == 1);
				bgr.GetField("_output", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bw, @"D:\shoot_bgr");
				string mapped = (string)bgr.GetMethod("ResultPathOf", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bw, new object[] { @"sub\C1_001_F.jpg" });
				Check("result file name follows the engine (" + mapped + ")", mapped == @"D:\shoot_bgr\sub_bgr\C1_001_F_bgr.png");
				bw.Close();
			}
			apply.Invoke(null, new object[] { false });

			// 수퍼포커스 (no real camera): MF notice first, arrow row order, a box click never reaches the lens, 가까운 곳/먼 곳 save.
			{
				Type sf = g2.GetType("CameraControl.windows.SuperfocusWindow", true);
				var cam = new CameraControl.Devices.BaseCameraDevice { SerialNumber = "THEME-SF" };
				var w = (Window)sf.GetMethod("For").Invoke(null, new object[] { cam, null });
				w.Show(); Pump(400);
				Snapshot(w, Path.Combine(outDir, "superfocus.png"));
				var inst = BindingFlags.Instance | BindingFlags.NonPublic;
				var notice = (Border)sf.GetField("mfNotice", inst).GetValue(w);
				string noticeText = ((TextBlock)((DockPanel)notice.Child).Children[1]).Text;
				Check("수퍼포커스 opens with the MF notice (" + noticeText + ")", notice.Visibility == Visibility.Visible && noticeText.Contains("MF"));
				var markA = (Button)sf.GetField("markA", inst).GetValue(w);
				var row = (Panel)markA.Parent;
				var labels = new List<string>(); foreach (UIElement c in row.Children) labels.Add((string)((Button)c).Content);
				Check("수퍼포커스 row: " + string.Join(" ", labels.ToArray()), string.Join("|", labels.ToArray()) == "◀◀◀|◀◀|◀|가까운 곳|▶|▶▶|▶▶▶|먼 곳");
				var lvFrame = new CameraControl.Devices.Classes.LiveViewData { ImageWidth = 1000, ImageHeight = 800, FocusFrameXSize = 100, FocusFrameYSize = 100, FocusX = 500, FocusY = 400 };
				bool taken = (bool)sf.GetMethod("TrySelectPreviewBox").Invoke(null, new object[] { cam, lvFrame, 200, 600 });
				object box = sf.GetMethod("PreviewBoxFor").Invoke(null, new object[] { cam });
				Check("box click while open only moves the box (handled, x " + box.GetType().GetProperty("X").GetValue(box, null) + ")", taken && Math.Abs((double)box.GetType().GetProperty("X").GetValue(box, null) - 0.2) < 1e-6);
				Check("AF while open does not drive the lens", (bool)sf.GetMethod("TryFocusSelectedBox").Invoke(null, new object[] { cam }));
				((System.Threading.Tasks.Task)sf.GetMethod("MarkSelectedAsync").Invoke(w, new object[] { true })).Wait();
				Pump(100);
				Check("가까운 곳 saves the current focus (" + markA.Content + ")", (string)markA.Content == "가까운 곳 ✓");
				((System.Threading.Tasks.Task)sf.GetMethod("MarkSelectedAsync").Invoke(w, new object[] { false })).Wait();
				Pump(100);
				var markB = (Button)sf.GetField("markB", inst).GetValue(w);
				Check("먼 곳 at the same focus is refused (" + sf.GetProperty("Feedback").GetValue(w, null) + ")", (string)markB.Content == "먼 곳" && ((string)sf.GetProperty("Feedback").GetValue(w, null)).Contains("같은 초점"));
				Snapshot(w, Path.Combine(outDir, "superfocus-marked.png"));
				w.Hide(); Pump(100);
				Check("hidden window: clicks go to the camera again", !(bool)sf.GetMethod("TrySelectPreviewBox").Invoke(null, new object[] { cam, lvFrame, 200, 600 }));
				w.Show(); Pump(200);
				Check("MF notice shows again on reopen", notice.Visibility == Visibility.Visible);
				sf.GetMethod("CloseAll").Invoke(null, null);
			}

			// 시작 화면: the Clorium intro control (real-time ribbon), captured while it plays; then CloseWhenReady.
			ServiceProvider.Branding.ShowStartupScreenAnimation = true;
			Type startup = g2.GetType("CameraControl.StartUpWindow", true);
			var sw = (Window)Activator.CreateInstance(startup);
			var clock = System.Diagnostics.Stopwatch.StartNew();
			sw.Show();
			foreach (double at in new[] { 0.6, 1.2, 1.9, 2.6, 3.3, 4.0, 4.7 })
			{
				while (clock.Elapsed.TotalSeconds < at) Pump(15);
				Snapshot(sw, Path.Combine(outDir, "intro-" + at.ToString("0.0").Replace('.', '_') + "s.png"));
			}
			var introControl = (FrameworkElement)sw.FindName("Intro");
			var tags = (Panel)introControl.FindName("TagLayer");
			double lastTag = tags.Children[tags.Children.Count - 1].Opacity, rib = ((UIElement)introControl.FindName("Rib0")).Opacity;
			Check("시작 화면: intro ends with the logo ribbons, CLORIUM and AI PHOTO STATION (rib " + rib + ", last letter " + lastTag + ")", rib > 0.99 && lastTag > 0.99 && ((UIElement)introControl.FindName("WordLayer")).Clip != null);
			DateTime closeStart = DateTime.Now;
			startup.GetMethod("CloseWhenReady").Invoke(sw, null);
			DateTime until4 = DateTime.Now.AddSeconds(8);
			while (sw.IsVisible && DateTime.Now < until4) Pump(50);
			Check("시작 화면 fades the logo out and closes once G2 is ready (" + (DateTime.Now - closeStart).TotalSeconds.ToString("0.0") + " s)", !sw.IsVisible && (DateTime.Now - closeStart).TotalSeconds < 2);

			// 촬영음: the seven sounds next to the program, numbered 1..7, all playable (WAV and MP3).
			Type sounds = typeof(Settings).Assembly.GetType("CameraControl.Core.Classes.ShutterSounds", true);
			var all = (System.Collections.IEnumerable)sounds.GetProperty("All").GetValue(null, null);
			var list = new List<object>(); foreach (object o in all) list.Add(o);
			Func<object, string, object> P = (o, n) => o.GetType().GetProperty(n).GetValue(o, null);
			Check("촬영음 list: " + string.Join(", ", list.ConvertAll(o => (string)P(o, "Label")).ToArray()), list.Count == 7 && (int)P(list[0], "Number") == 1 && ((string)P(list[0], "Path")).EndsWith(".mp3"));
			Check("촬영음 default is 1", new Settings().ShutterSound == 1);
			int playable = 0;
			foreach (object o in list)
			{
				var mp = new System.Windows.Media.MediaPlayer(); bool done = false, ok = false;
				mp.MediaOpened += (s2, a2) => { done = true; ok = mp.NaturalDuration.HasTimeSpan && mp.NaturalDuration.TimeSpan.TotalMilliseconds > 0; };
				mp.MediaFailed += (s2, a2) => { done = true; Console.WriteLine("  sound failed: " + P(o, "Path") + " " + a2.ErrorException.Message); };
				mp.Open(new Uri((string)P(o, "Path")));
				DateTime u = DateTime.Now.AddSeconds(5); while (!done && DateTime.Now < u) Pump(50);
				if (ok) playable++;
				mp.Close();
			}
			Check("all 촬영음 files open in the player (" + playable + "/7)", playable == 7);

			// 정사각형 판 keyword on a measurement copy: the same calls as MainWindow.WriteMetadata.
			string value = "clorium-scale:v1;board=500;sx=0.2;sy=0.2001;kh=0.01;kv=0;rot=0.1;h=1,0,0,0,1,0,0,0,1;corners=1,1,2,1,2,2,1,2;at=2026-11-03T12:00:00";
			BitmapFrame frame;
			using (Stream s = File.OpenRead(jpeg)) frame = BitmapDecoder.Create(s, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
			var md = ((BitmapMetadata)frame.Metadata).Clone();
			md.Title = "C1_"; md.Subject = "0.20005"; md.Comment = "host";
			md.SetQuery(SquareBoardCalibration.MetadataQuery, System.Text.Encoding.Unicode.GetBytes(value + "\0"));
			string copy = Path.Combine(outDir, "G2THEME_F_M.jpeg");
			using (Stream o = File.Create(copy)) { var enc = new JpegBitmapEncoder { QualityLevel = 30 }; enc.Frames.Add(BitmapFrame.Create(frame, frame.Thumbnail, md, frame.ColorContexts)); enc.Save(o); }
			BitmapMetadata back;
			using (Stream s = File.OpenRead(copy)) back = (BitmapMetadata)BitmapDecoder.Create(s, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0].Metadata;
			string tag = System.Text.Encoding.Unicode.GetString((byte[])back.GetQuery(SquareBoardCalibration.MetadataQuery)).TrimEnd((char)0);
			Check("measurement copy keeps Subject scale and the square-board tag (XPKeywords)", back.Subject == "0.20005" && tag == value);
			// Windows shows XPKeywords split at ';' as several 태그; the raw value above is what G2/Desk read.
			Check("Windows 태그 view lists the calibration parts", back.Keywords != null && back.Keywords.Count > 1 && back.Keywords[0] == "clorium-scale:v1");
			Check("tag parses back to X/Y scale", SquareBoardCalibration.Parse(tag) != null && Math.Abs(SquareBoardCalibration.Parse(tag).ScaleY - 0.2001) < 1e-9);

			app.Shutdown();
			Console.WriteLine("THEME_CHECK_PASSED=" + count);
			return 0;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return 1;
		}
	}

	static void MakeJpeg(string path)
	{
		int w = 1600, h = 1067, stride = w * 3;
		var px = new byte[stride * h];
		for (int i = 0; i < px.Length; i += 3) { int x = (i / 3) % w; px[i] = (byte)(120 + x % 90); px[i + 1] = 110; px[i + 2] = 170; }
		var md = new BitmapMetadata("jpg");
		md.Title = "camera";
		md.Author = new ReadOnlyCollection<string>(new List<string> { "360photo" });
		var enc = new JpegBitmapEncoder { QualityLevel = 90 };
		enc.Frames.Add(BitmapFrame.Create(BitmapSource.Create(w, h, 300, 300, PixelFormats.Bgr24, null, px, stride), null, md, null));
		using (var f = File.Create(path)) enc.Save(f);
	}
}
