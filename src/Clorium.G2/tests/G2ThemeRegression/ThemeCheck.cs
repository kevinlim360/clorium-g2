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
	[System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] static extern IntPtr FindWindow(string cls, string title);
	[System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
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

			// Status icon tooltips (StatusTip): camera mode + battery glyph, PC RAM/SSD bars with the percentage after them.
			{
				Type tipType = g2.GetType("CameraControl.Classes.StatusTip", true);
				MethodInfo tipText = tipType.GetMethod("Text"), tipBattery = tipType.GetMethod("Battery"), tipUsage = tipType.GetMethod("Usage"), tipNote = tipType.GetMethod("Note"), tipMode = tipType.GetMethod("ModeName");
				PropertyInfo tipPanel = tipType.GetProperty("Panel");
				Check("mode 3 is M (" + tipMode.Invoke(null, new object[] { 3L, "" }) + ")", ((string)tipMode.Invoke(null, new object[] { 3L, "" })).StartsWith("M "));
				foreach (bool d in new[] { false, true })
				{
					apply.Invoke(null, new object[] { d }); Pump(200);
					string mode = d ? "dark" : "light";
					var tw = new Window { Width = 1100, Height = 260, WindowStyle = WindowStyle.None, ShowInTaskbar = false };
					tw.Resources.MergedDictionaries.Add((ResourceDictionary)Activator.CreateInstance(g2.GetType("CameraControl.Classes.G2Palette", true)));
					tw.Background = (Brush)tw.TryFindResource("G2Page") ?? Brushes.White;
					var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(14) };
					System.Windows.Documents.TextElement.SetForeground(row, (Brush)tw.TryFindResource("G2Text") ?? Brushes.Black);
					tw.Content = row;
					tw.Show(); Pump(100);
					object cam = Activator.CreateInstance(tipType, row, "카메라 연결됨");
					tipText.Invoke(cam, new object[] { "모델", "Canon EOS R" });
					tipText.Invoke(cam, new object[] { "모드", tipMode.Invoke(null, new object[] { 3L, "" }) });
					tipBattery.Invoke(cam, new object[] { "배터리", 64 });
					tipText.Invoke(cam, new object[] { "오늘 촬영", "51컷" });
					tipText.Invoke(cam, new object[] { "오늘 세션", "1개" });
					object low = Activator.CreateInstance(tipType, row, "배터리 낮음");
					tipBattery.Invoke(low, new object[] { "배터리", 15 });
					object pc = Activator.CreateInstance(tipType, row, "PC 상태");
					tipUsage.Invoke(pc, new object[] { "RAM", 68.2, "21.6 GB / 31.7 GB" });
					tipUsage.Invoke(pc, new object[] { "SSD C:", 92.4, "880.1 GB / 952.6 GB" });
					tipUsage.Invoke(pc, new object[] { "SSD D:", 41.0, "763.0 GB / 1863.0 GB" });
					tipNote.Invoke(pc, new object[] { "90% 이상이면 아이콘이 빨갛게 깜빡입니다." });
					foreach (object t in new[] { cam, low, pc })
					{
						var card = new Border { Child = (UIElement)tipPanel.GetValue(t, null), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 14, 0), CornerRadius = new CornerRadius(6), BorderThickness = new Thickness(1), BorderBrush = (Brush)tw.TryFindResource("G2Line") ?? Brushes.Gray, VerticalAlignment = VerticalAlignment.Top };
						row.Children.Add(card);
					}
					Pump(300);
					Snapshot(tw, Path.Combine(outDir, "status-tips-" + mode + ".png"));
					var pcPanel = (StackPanel)tipPanel.GetValue(pc, null);
					Check("PC tooltip has RAM + 2 drive rows with a percentage in " + mode, pcPanel.Children.Count == 5 && ((TextBlock)((Grid)pcPanel.Children[2]).Children[2]).Text == "92%");
					tw.Close();
				}
				apply.Invoke(null, new object[] { false }); Pump(200);
			}

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
				// As in G2: the live view's click handler (a worker thread) is the first to touch the window class.
				System.Threading.Tasks.Task.Run(() => sf.GetMethod("TrySelectPreviewBox").Invoke(null, new object[] { cam, null, 0, 0 })).Wait();
				string uiError = null;
				DispatcherUnhandledExceptionEventHandler onError = (s2, a2) => { uiError = a2.Exception.GetBaseException().Message; a2.Handled = true; };
				app.DispatcherUnhandledException += onError;
				var w = (Window)sf.GetMethod("For").Invoke(null, new object[] { cam, null });
				w.Show(); Pump(400);
				app.DispatcherUnhandledException -= onError;
				Snapshot(w, Path.Combine(outDir, "superfocus.png"));
				Check("수퍼포커스 window draws after a worker thread touched it first (" + (uiError ?? "no UI error") + ")", uiError == null);
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
				// RF24-240 (no AF/MF switch) profile: lens name match and the AF notice when 자동 초점 찾기 is on.
				var finds = sf.GetMethod("FindsFocusAutomatically");
				Check("RF24-240 finds focus automatically, other lenses do not", (bool)finds.Invoke(null, new object[] { "RF24-240mm F4-6.3 IS USM" }) && !(bool)finds.Invoke(null, new object[] { "RF24-105mm F4 L IS USM" }) && !(bool)finds.Invoke(null, new object[] { "" }));
				sf.GetProperty("UseFocusBox").SetValue(w, true, null);
				sf.GetMethod("TrySelectPreviewBox").Invoke(null, new object[] { cam, lvFrame, 300, 300 });
				sf.GetMethod("InvalidateRange").Invoke(w, null); Pump(100);
				Snapshot(w, Path.Combine(outDir, "superfocus-auto.png"));
				string autoText = ((TextBlock)((DockPanel)notice.Child).Children[1]).Text;
				Check("자동 초점 찾기 shows the AF notice (" + autoText + ")", autoText.Contains("박스에 초점 맞추기"));
				var focusHere = (Button)sf.GetField("focusHere", inst).GetValue(w);
				Check("자동 초점 찾기 shows the ‘" + focusHere.Content + "’ button", focusHere.IsVisible && ((string)focusHere.Content).Contains("초점 맞추기"));
				Check("↺ keeps the chosen box on screen", sf.GetMethod("PreviewBoxFor").Invoke(null, new object[] { cam }) != null);
				var noAfBox = FocusBoxContrast.From(new CameraControl.Devices.Classes.LiveViewData { ImageWidth = 1000, ImageHeight = 800 });
				Check("a frame without the camera AF box still gives a box (" + noAfBox.Width + ")", Math.Abs(noAfBox.Width - 0.1) < 1e-9 && Math.Abs(noAfBox.X - 0.5) < 1e-9);
				sf.GetProperty("UseFocusBox").SetValue(w, false, null);
				sf.GetMethod("CloseAll").Invoke(null, null);
				// English (언어 설정 not Korean) once Data\Messages.xml carries the reviewed wording.
				if (File.Exists(Path.Combine(bin, "Data", "Messages.xml")))
				{
					CameraControl.Devices.UiText.Language = "en_US";
					var en = (Window)sf.GetMethod("For").Invoke(null, new object[] { new CameraControl.Devices.BaseCameraDevice { SerialNumber = "THEME-SF-EN" }, null });
					en.Show(); Pump(400);
					Snapshot(en, Path.Combine(outDir, "superfocus-en.png"));
					string near = (string)((Button)sf.GetField("markA", inst).GetValue(en)).Content;
					Check("English language shows English wording (" + near + ")", near.Length > 0 && near.All(ch => ch < 0x3131));
					CameraControl.Devices.UiText.Language = "ko";
					sf.GetMethod("CloseAll").Invoke(null, null);
				}
			}

			// 수퍼포커스 자동 초점 찾기 with a simulated lens: near subject sharp at -40, far at +50 (one unit per drive command).
			// Saving 가까운 곳/먼 곳 never moves the lens; quality buttons show their counts; the count can be typed; the distance is
			// measured from one end to the other (as 촬영 does before shooting).
			{
				Type sf = g2.GetType("CameraControl.windows.SuperfocusWindow", true);
				var lens = new FakeLens { SerialNumber = "THEME-LENS" };
				var w = (Window)sf.GetMethod("For").Invoke(null, new object[] { lens, null });
				var inst = BindingFlags.Instance | BindingFlags.NonPublic;
				sf.GetField("stopNativeAutofocus", inst).SetValue(w, (Action)(() => { }));
				w.Show(); Pump(200);
				sf.GetProperty("UseFocusBox").SetValue(w, true, null);
				sf.GetMethod("InvalidateRange").Invoke(w, null);
				Func<System.Threading.Tasks.Task, bool> Run = t => { DateTime until = DateTime.Now.AddSeconds(120); while (!t.IsCompleted && DateTime.Now < until) Pump(20); return t.IsCompleted; };
				object range = sf.GetField("range", inst).GetValue(w);
				Func<string, object> R = n => range.GetType().GetProperty(n).GetValue(range, null);
				var lensFrame = lens.GetLiveViewImage();
				sf.GetMethod("TrySelectPreviewBox").Invoke(null, new object[] { lens, lensFrame, (int)(0.3 * lensFrame.ImageWidth), lensFrame.ImageHeight / 2 });
				Check("박스에 초점 맞추기 (near box) finishes and unlocks", Run((System.Threading.Tasks.Task)sf.GetMethod("FocusHereAsync").Invoke(w, null)) && !(bool)sf.GetProperty("IsWorking").GetValue(w, null));
				Check("AF went to the near subject (lens " + lens.Position + ")", lens.Position == FakeLens.Near);
				Run((System.Threading.Tasks.Task)sf.GetMethod("MarkSelectedAsync").Invoke(w, new object[] { true }));
				sf.GetMethod("TrySelectPreviewBox").Invoke(null, new object[] { lens, lensFrame, (int)(0.7 * lensFrame.ImageWidth), lensFrame.ImageHeight / 2 });
				Run((System.Threading.Tasks.Task)sf.GetMethod("FocusHereAsync").Invoke(w, null));
				Check("AF went to the far subject (lens " + lens.Position + ")", lens.Position == FakeLens.Far);
				var saveClock = System.Diagnostics.Stopwatch.StartNew();
				var save = (System.Threading.Tasks.Task)sf.GetMethod("MarkSelectedAsync").Invoke(w, new object[] { false });
				Check("먼 곳 saves at once without moving the lens (" + saveClock.ElapsedMilliseconds + " ms, lens " + lens.Position + ")", save.IsCompleted && lens.Position == FakeLens.Far && saveClock.ElapsedMilliseconds < 500);
				Pump(100);
				var markA = (Button)sf.GetField("markA", inst).GetValue(w); var markB = (Button)sf.GetField("markB", inst).GetValue(w);
				var qb = (System.Collections.IList)sf.GetField("qualityButtons", inst).GetValue(w);
				string labels = ((Button)qb[0]).Content + " / " + ((Button)qb[1]).Content + " / " + ((Button)qb[2]).Content;
				Check("both ends marked (" + markA.Content + ", " + markB.Content + ") and quality buttons show counts (" + labels + ")", (string)markA.Content == "가까운 곳 ✓" && (string)markB.Content == "먼 곳 ✓" && labels == "고 15 / 중 10 / 저 5");
				var cutsBox = (TextBox)sf.GetField("cuts", inst).GetValue(w);
				Check("cuts box is editable and shows 고 (" + cutsBox.Text + ")", cutsBox.IsEnabled && cutsBox.Text == "15");
				cutsBox.Text = "7"; Pump(50);
				Check("typed 7 cuts can be shot", (bool)sf.GetProperty("CanCapture").GetValue(w, null) && (int)sf.GetMethod("AfCount", inst).Invoke(w, null) == 7);
				cutsBox.Text = "1"; Pump(50);
				Check("1 cut is refused", !(bool)sf.GetProperty("CanCapture").GetValue(w, null));
				// Real keyboard path: pick 저, click into the box (focus selects all), type 6.
				sf.GetMethod("SelectQuality").Invoke(w, new object[] { 2 }); Pump(50);
				w.Activate(); cutsBox.Focus(); System.Windows.Input.Keyboard.Focus(cutsBox); Pump(150);
				System.Windows.Input.TextCompositionManager.StartComposition(new System.Windows.Input.TextComposition(System.Windows.Input.InputManager.Current, cutsBox, "6")); Pump(150);
				Check("typing 6 after choosing 저 gives 6 (text '" + cutsBox.Text + "', focus " + cutsBox.IsKeyboardFocusWithin + ", start " + ((Button)sf.GetField("start", inst).GetValue(w)).IsEnabled + ")", cutsBox.Text == "6" && (bool)sf.GetProperty("CanCapture").GetValue(w, null));
				// Key presses themselves (keypad 1, top-row 2) are taken as digits even when no text input follows (IME, keypad).
				cutsBox.SelectAll();
				foreach (var key in new[] { System.Windows.Input.Key.NumPad1, System.Windows.Input.Key.D2 })
					cutsBox.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, PresentationSource.FromVisual(cutsBox), 0, key) { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent });
				Pump(50);
				Check("keypad 1 + key 2 give 12 (text '" + cutsBox.Text + "')", cutsBox.Text == "12" && (int)sf.GetMethod("AfCount", inst).Invoke(w, null) == 12);
				sf.GetMethod("SelectQuality").Invoke(w, new object[] { 1 }); Pump(50);
				Check("choosing 중 goes back to its count (" + cutsBox.Text + ")", cutsBox.Text == "10" && (bool)sf.GetProperty("CanCapture").GetValue(w, null));
				Snapshot(w, Path.Combine(outDir, "superfocus-af-marked.png"));
				// 촬영 AFs on evenly spaced points from the end the lens is on (먼 곳, saved last) to the other end.
				var boxA = sf.GetField("boxA", inst).GetValue(w); var boxB = sf.GetField("boxB", inst).GetValue(w);
				Check("촬영 starts at 먼 곳, where the lens is (afLensAt " + sf.GetField("afLensAt", inst).GetValue(w) + ")", (bool?)sf.GetField("afLensAt", inst).GetValue(w) == false);
				var pts = (System.Collections.Generic.IReadOnlyList<Point>)sf.GetMethod("AfPoints").Invoke(null, new object[] { boxB, boxA, 5 });
				string xs = string.Join(",", pts.Select(p => p.X.ToString("0.00")).ToArray());
				Check("5 AF points from 먼 곳 to 가까운 곳 (" + xs + ")", pts.Count == 5 && xs == "0.70,0.60,0.50,0.40,0.30");
				var order = new List<int>(); foreach (var p in pts) { lens.Focus((int)(p.X * 400), 150); lens.AutoFocus(); order.Add(lens.Position); }
				Check("each AF point focuses nearer than the last (lens " + string.Join(",", order.Select(v => v.ToString()).ToArray()) + ")", order[0] == FakeLens.Far && order[4] == FakeLens.Near && order.Zip(order.Skip(1), (x, y) => y < x).All(v => v));
				sf.GetMethod("CloseAll").Invoke(null, null);
			}

			// G2 message box (G2Dialog): every kind in both themes, buttons, Enter/Esc results.
			{
				Func<Window, List<Button>> ButtonsIn = null;
				ButtonsIn = win => { var found = new List<Button>(); var stack = new Stack<DependencyObject>(); stack.Push(win); while (stack.Count > 0) { var d = stack.Pop(); if (d is Button) found.Add((Button)d); for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++) stack.Push(VisualTreeHelper.GetChild(d, i)); } return found; };
				Func<Button, string> LabelOf = b => ((TextBlock)((Border)b.Content).Child).Text;
				var kinds = new[] { MessageBoxImage.Information, MessageBoxImage.Warning, MessageBoxImage.Error, MessageBoxImage.Question };
				foreach (bool d in new[] { false, true })
				{
					G2Dialog.Dark = d;
					foreach (var kind in kinds)
					{
						MessageBoxResult got = MessageBoxResult.None;
						var buttons = kind == MessageBoxImage.Question ? MessageBoxButton.YesNo : kind == MessageBoxImage.Warning ? MessageBoxButton.OKCancel : MessageBoxButton.OK;
						var dw = G2Dialog.Create("카메라 초기화에 실패했습니다.\n카메라 전원을 껐다 켠 뒤 다시 시도해 주세요.", kind == MessageBoxImage.Question ? "" : "알림", buttons, kind, MessageBoxResult.None, r => got = r);
						dw.Show(); Pump(200);
						Snapshot(dw, Path.Combine(outDir, "dialog-" + kind + "-" + (d ? "dark" : "light") + ".png"));
						var labels = ButtonsIn(dw).Select(LabelOf).ToList();
						labels.Reverse();
						string expect = buttons == MessageBoxButton.YesNo ? "아니요,예" : buttons == MessageBoxButton.OKCancel ? "취소,확인" : "확인";
						Check("dialog " + kind + " (" + (d ? "dark" : "light") + ") buttons " + string.Join(",", labels.ToArray()), string.Join(",", labels.ToArray()) == expect || string.Join(",", Enumerable.Reverse(labels).ToArray()) == expect);
						if (kind == MessageBoxImage.Question)
						{
							var no = ButtonsIn(dw).First(b => LabelOf(b) == "아니요");
							no.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent)); Pump(100);
							Check("아니요 returns No (" + got + ")", got == MessageBoxResult.No && !dw.IsVisible);
						}
						else if (kind == MessageBoxImage.Warning)
						{
							dw.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, PresentationSource.FromVisual(dw), 0, System.Windows.Input.Key.Escape) { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent }); Pump(100);
							Check("Esc on 확인/취소 returns Cancel (" + got + ")", got == MessageBoxResult.Cancel && !dw.IsVisible);
						}
						else
						{
							dw.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, PresentationSource.FromVisual(dw), 0, System.Windows.Input.Key.Escape) { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent }); Pump(100);
							Check("Esc on 확인 returns OK (" + got + ")", got == MessageBoxResult.OK && !dw.IsVisible);
						}
					}
				}
				G2Dialog.Dark = false;
				// From a worker thread (camera events): runs on its own STA thread and returns a result.
				MessageBoxResult workerResult = MessageBoxResult.None;
				var worker = System.Threading.Tasks.Task.Run(() => workerResult = G2Dialog.Show("다운로드하지 못했습니다.\n다시 촬영해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning));
				DateTime untilDialog = DateTime.Now.AddSeconds(10); IntPtr dialogHandle = IntPtr.Zero;
				while (DateTime.Now < untilDialog && dialogHandle == IntPtr.Zero) { Pump(100); dialogHandle = FindWindow(null, "알림"); }
				if (dialogHandle != IntPtr.Zero) PostMessage(dialogHandle, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
				Check("worker-thread dialog opens on its own thread and returns (" + workerResult + ")", dialogHandle != IntPtr.Zero && worker.Wait(5000) && workerResult == MessageBoxResult.OK);
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

// Simulated lens for 수퍼포커스: two textured subjects (left = near, right = far) whose contrast peaks at their focus position.
class FakeLens : CameraControl.Devices.BaseCameraDevice
{
	public const int Near = -40, Far = 50;
	public int Position = 7;
	double afX = 0.5;
	public override void Focus(int x, int y) { afX = x / 400.0; }
	public override void AutoFocus() { Position = (int)Math.Round(Near + (Far - Near) * Math.Max(0, Math.Min(1, (afX - 0.3) / 0.4))); }
	public override CameraControl.Devices.Classes.LiveViewData GetLiveViewImage()
	{
		int w = 400, h = 300, stride = w * 3;
		var px = new byte[stride * h];
		double near = 120 / (1 + Math.Abs(Position - Near) / 6.0), far = 120 / (1 + Math.Abs(Position - Far) / 6.0);
		for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
		{
			double amp = x < w / 2 ? near : far;
			int v = (int)(128 + (((x / 3) + (y / 3)) % 2 == 0 ? amp : -amp));
			int p = y * stride + x * 3; px[p] = px[p + 1] = px[p + 2] = (byte)Math.Max(0, Math.Min(255, v));
		}
		var enc = new JpegBitmapEncoder { QualityLevel = 95 };
		enc.Frames.Add(BitmapFrame.Create(BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgr24, null, px, stride)));
		var ms = new MemoryStream(); enc.Save(ms);
		return new CameraControl.Devices.Classes.LiveViewData { ImageData = ms.ToArray(), ImageWidth = w, ImageHeight = h, FocusX = 200, FocusY = 150, FocusFrameXSize = 40, FocusFrameYSize = 40, HaveFocusData = true };
	}
}

class FakeLensAdapter : IFocusCaptureCamera, IDisposable
{
	readonly FakeLens lens;
	public FakeLensAdapter(FakeLens lens) { this.lens = lens; }
	public bool IsConnected { get { return true; } }
	public void MoveFocus(CameraControl.Devices.Classes.FocusDirection direction, CameraControl.Devices.Classes.FocusAmount amount) { lens.Position += direction == CameraControl.Devices.Classes.FocusDirection.Far ? 1 : -1; }
	public System.Threading.Tasks.Task<string> CaptureAsync() { throw new NotSupportedException(); }
	public void Dispose() { }
}
