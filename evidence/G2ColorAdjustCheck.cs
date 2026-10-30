// G2 colour-correction window check (actual WPF window from the isolated check runtime, C# 5 for Framework csc).
// Opens G2ColorAdjustWnd on a generated 6000x4000 JPEG, waits for the preview, applies settings, renders the window
// to PNG evidence, saves a corrected copy and verifies the original is untouched.
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CameraControl.Classes;
using CameraControl.windows;

class G2ColorAdjustCheck
{
	static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL " + message); }

	static void Pump(int ms)
	{
		DateTime end = DateTime.Now.AddMilliseconds(ms);
		while (DateTime.Now < end)
		{
			Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
			Thread.Sleep(15);
		}
	}

	static bool WaitUntil(Func<bool> ready, int timeoutMs)
	{
		DateTime end = DateTime.Now.AddMilliseconds(timeoutMs);
		while (DateTime.Now < end)
		{
			Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
			if (ready()) return true;
			Thread.Sleep(20);
		}
		return ready();
	}

	static object Field(object o, string name) { return o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o); }

	static object Call(object o, string name, params object[] args) { return o.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Invoke(o, args); }

	static string Sha(string path)
	{
		using (var s = File.OpenRead(path)) using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(s)).Replace("-", "");
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

	static string MakeJpeg(string dir) { return MakeJpeg(dir, "G2CC_TEST_F.jpg", 6000, 4000); }

	static string MakeJpeg(string dir, string name, int w, int h)
	{
		int stride = w * 3;
		var px = new byte[stride * h];
		byte[][] sw = { new byte[] { 40, 40, 200 }, new byte[] { 40, 180, 40 }, new byte[] { 200, 60, 40 }, new byte[] { 40, 200, 220 }, new byte[] { 128, 128, 128 }, new byte[] { 30, 30, 30 } };
		for (int y = 0; y < h; y++)
		{
			for (int x = 0; x < w; x++)
			{
				int i = y * stride + x * 3;
				double fx = (double)x / w, fy = (double)y / h;
				// soft studio backdrop with a "garment" block and colour swatches (BGR order)
				double bg = 200 + 30 * (1 - Math.Abs(fx - 0.5) * 2) - 25 * fy;
				byte b = (byte)bg, g = (byte)bg, r = (byte)bg;
				if (fx > 0.3 && fx < 0.7 && fy > 0.15 && fy < 0.85) { r = (byte)(150 + 40 * fy); g = (byte)(70 + 30 * fx); b = (byte)(60); }
				if (fy > 0.88 && fx > 0.05 && fx < 0.95) { int k = (int)((fx - 0.05) / 0.15); byte[] c = sw[Math.Min(5, k)]; b = c[0]; g = c[1]; r = c[2]; }
				px[i] = b; px[i + 1] = g; px[i + 2] = r;
			}
		}
		var bmp = BitmapSource.Create(w, h, 300, 300, PixelFormats.Bgr24, null, px, stride);
		var enc = new JpegBitmapEncoder { QualityLevel = 95 };
		enc.Frames.Add(BitmapFrame.Create(bmp));
		string path = Path.Combine(dir, name);
		using (var f = File.Create(path)) enc.Save(f);
		return path;
	}

	[STAThread]
	static int Main(string[] args)
	{
		string evidence = args.Length > 0 ? args[0] : AppDomain.CurrentDomain.BaseDirectory;
		string dir = Path.Combine(Path.GetTempPath(), "g2-cc-window-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(dir);
		try
		{
			var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
			// like the real app: UI events (and their awaits) run on the WPF dispatcher context
			SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));
			string src = MakeJpeg(dir);
			string hash = Sha(src);

			G2ColorAdjustWnd.TemplateStorePath = Path.Combine(dir, "templates", "color-templates.xml");
			var wnd = new G2ColorAdjustWnd(src, false);
			wnd.Width = 1440; wnd.Height = 900;
			wnd.WindowStartupLocation = WindowStartupLocation.Manual; wnd.Left = 20; wnd.Top = 20;
			wnd.Show();
			Check(WaitUntil(delegate { return ((Border)wnd.FindName("BusyOverlay")).Visibility == Visibility.Collapsed; }, 60000), "preview did not load: " + ((TextBlock)wnd.FindName("BusyText")).Text);
			var img = (Image)wnd.FindName("StageImage");
			var bs = img.Source as BitmapSource;
			Check(bs != null && bs.PixelWidth == 2000 && bs.PixelHeight == 1333, "preview proxy size");
			Check(((TextBlock)wnd.FindName("FileInfoText")).Text.StartsWith("6000×4000"), "file info: " + ((TextBlock)wnd.FindName("FileInfoText")).Text);
			Console.WriteLine("PASS actual WPF: window opens, 6000x4000 JPEG loads, 2000px preview proxy shown");
			Pump(300);
			Snapshot(wnd, Path.Combine(evidence, "color-adjust-01-open.png"));

			// slider interaction: drive the exposure slider (first row) like a user
			var rows = (System.Collections.IList)Field(wnd, "_rows");
			Check(rows.Count == 9, "expected 9 adjustment rows, got " + rows.Count);
			var exposure = (Slider)rows[0].GetType().GetField("Slider").GetValue(rows[0]);
			var exposureBox = (TextBox)rows[0].GetType().GetField("Box").GetValue(rows[0]);
			exposure.Value = 0.5;
			var settings = (G2AdjustSettings)Field(wnd, "_settings");
			Check(Math.Abs(settings.Exposure - 0.5) < 1e-9 && exposureBox.Text == "+0.50", "slider → setting/text: " + settings.Exposure + " " + exposureBox.Text);
			// typed value
			var tempRow = rows[4];
			var tempBox = (TextBox)tempRow.GetType().GetField("Box").GetValue(tempRow);
			tempBox.Text = "35"; Call(wnd, "CommitBox", tempRow);
			Check(Math.Abs(settings.Temperature - 35) < 1e-9, "typed temperature not applied");
			var satRow = rows[3];
			((Slider)satRow.GetType().GetField("Slider").GetValue(satRow)).Value = 25;
			// channel levels: select R, set black point via slider
			((RadioButton)wnd.FindName("ChRed")).IsChecked = true;
			var blackRow = rows[6];
			((Slider)blackRow.GetType().GetField("Slider").GetValue(blackRow)).Value = 20;
			Check(settings.Red.Black == 20 && settings.Master.Black == 0, "per-channel black point routed to wrong channel");
			Check(WaitUntil(delegate { return !(bool)Field(wnd, "_rendering") && Field(wnd, "_pending") == null; }, 30000), "render did not finish: rendering=" + Field(wnd, "_rendering") + " pending=" + (Field(wnd, "_pending") != null) + " status=" + ((TextBlock)wnd.FindName("StatusText")).Text);
			Pump(300);
			Check(((TextBlock)wnd.FindName("StateBadgeText")).Text == "보정 후", "badge");
			var shown = (BitmapSource)((Image)wnd.FindName("StageImage")).Source;
			Check(shown != Field(wnd, "_originalBitmap"), "stage must show the adjusted frame");
			var spx = new byte[shown.PixelWidth * shown.PixelHeight * 4];
			shown.CopyPixels(spx, shown.PixelWidth * 4, 0);
			long alphaLow = 0, rgbSum = 0;
			for (int i = 0; i < spx.Length; i += 4) { if (spx[i + 3] != 255) alphaLow++; rgbSum += spx[i] + spx[i + 1] + spx[i + 2]; }
			Check(alphaLow == 0 && rgbSum > 0, "adjusted preview blank/transparent: non-opaque=" + alphaLow + " sum=" + rgbSum);
			Console.WriteLine("PASS actual WPF: slider/typed input/channel selection update settings; preview re-renders");
			Snapshot(wnd, Path.Combine(evidence, "color-adjust-02-adjusted.png"));

			((RadioButton)wnd.FindName("ViewOriginal")).IsChecked = true;
			Pump(200);
			Check(((Image)wnd.FindName("StageImage")).Source == Field(wnd, "_originalBitmap"), "compare must show original");
			Snapshot(wnd, Path.Combine(evidence, "color-adjust-03-compare.png"));
			((RadioButton)wnd.FindName("ViewSide")).IsChecked = true;
			Pump(200);
			Check(((Grid)wnd.FindName("SidePanel")).Visibility == Visibility.Visible && ((Image)wnd.FindName("StageImage")).Visibility == Visibility.Collapsed, "side-by-side panel not shown");
			Check(((Image)wnd.FindName("SideBefore")).Source == Field(wnd, "_originalBitmap") && ((Image)wnd.FindName("SideAfter")).Source == Field(wnd, "_adjustedBitmap"), "side-by-side sources");
			Snapshot(wnd, Path.Combine(evidence, "color-adjust-05-side-by-side.png"));
			Console.WriteLine("PASS actual WPF: side-by-side before/after view");
			((RadioButton)wnd.FindName("ViewAdjusted")).IsChecked = true;
			Pump(100);
			Check(((Grid)wnd.FindName("SidePanel")).Visibility == Visibility.Collapsed && ((Image)wnd.FindName("StageImage")).Source == Field(wnd, "_adjustedBitmap"), "back to adjusted view");
			Console.WriteLine("PASS actual WPF: original compare toggle");

			Call(wnd, "Save_Click", wnd, new RoutedEventArgs());
			Check(WaitUntil(delegate { return !(bool)Field(wnd, "_saving") && wnd.SavedFile != null; }, 120000), "save did not finish: " + ((TextBlock)wnd.FindName("StatusText")).Text);
			Check(Path.GetFileName(wnd.SavedFile) == "G2CC_TEST_F_cc.jpg" && File.Exists(wnd.SavedFile), "saved name " + wnd.SavedFile);
			Check(Sha(src) == hash, "original changed");
			var saved = BitmapFrame.Create(new Uri(wnd.SavedFile), BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
			Check(saved.PixelWidth == 6000 && saved.PixelHeight == 4000, "saved size " + saved.PixelWidth + "x" + saved.PixelHeight);
			Console.WriteLine("PASS actual WPF: save writes G2CC_TEST_F_cc.jpg at 6000x4000, original byte-identical");
			Pump(200);
			Snapshot(wnd, Path.Combine(evidence, "color-adjust-04-saved.png"));

			// templates: save current values as a template, reset, then pick it from the list → applied immediately
			var keep = settings.Clone();
			Call(wnd, "NewTemplate_Click", wnd, new RoutedEventArgs());
			Check(((Grid)wnd.FindName("TemplateNameRow")).Visibility == Visibility.Visible, "name row not shown");
			((TextBox)wnd.FindName("TemplateNameBox")).Text = "따뜻한 흰배경";
			Call(wnd, "TemplateNameOk_Click", wnd, new RoutedEventArgs());
			var list = (ListBox)wnd.FindName("TemplateList");
			Check(list.Items.Count == 1 && File.Exists(G2ColorAdjustWnd.TemplateStorePath), "template not saved");
			Call(wnd, "Reset_Click", wnd, new RoutedEventArgs());
			Check(settings.IsIdentity, "reset must restore identity");
			Console.WriteLine("PASS actual WPF: reset restores defaults");
			exposure.Value = -0.3;
			Call(wnd, "NewTemplate_Click", wnd, new RoutedEventArgs());
			((TextBox)wnd.FindName("TemplateNameBox")).Text = "어둡게";
			Call(wnd, "TemplateNameOk_Click", wnd, new RoutedEventArgs());
			Check(list.Items.Count == 2, "second template");
			Call(wnd, "Reset_Click", wnd, new RoutedEventArgs());
			list.SelectedIndex = -1;
			foreach (object it in list.Items) { if (((G2ColorTemplate)it).Name == "따뜻한 흰배경") list.SelectedItem = it; }
			Check(settings.SameAs(keep) && exposureBox.Text == "+0.50", "selecting a template must apply it at once");
			Check(WaitUntil(delegate { return !(bool)Field(wnd, "_rendering") && Field(wnd, "_pending") == null; }, 30000), "template render");
			Pump(300);
			Check(((TextBlock)wnd.FindName("TemplateState")).Text.StartsWith("적용됨: 따뜻한 흰배경"), "template state: " + ((TextBlock)wnd.FindName("TemplateState")).Text);
			Snapshot(wnd, Path.Combine(evidence, "color-adjust-06-template.png"));
			var reload = new G2ColorTemplateStore(G2ColorAdjustWnd.TemplateStorePath); reload.Load();
			Check(reload.Items.Count == 2 && reload.Find("어둡게") != null && Math.Abs(reload.Find("어둡게").Settings.Exposure + 0.3) < 1e-9, "templates not persisted");
			Console.WriteLine("PASS actual WPF: save 2 templates, pick one → applied immediately, persisted to file");

			// batch: apply the selected template to every image of one folder
			string batchDir = Path.Combine(dir, "batch");
			Directory.CreateDirectory(batchDir);
			string[] bnames = { "P1_F.jpg", "P1_B.jpg", "P1_D.jpg" };
			var bh = new string[bnames.Length];
			for (int i = 0; i < bnames.Length; i++) bh[i] = Sha(MakeJpeg(batchDir, bnames[i], 1800, 1200));
			wnd.ConfirmBatch = false;
			System.Threading.Tasks.Task<G2BatchResult> run = wnd.RunBatchAsync(batchDir, "따뜻한 흰배경");
			Check(WaitUntil(delegate { return run.IsCompleted; }, 120000), "batch did not finish");
			G2BatchResult br = run.Result;
			Check(br != null && br.Total == 3 && br.Saved == 3 && br.Errors.Count == 0, "batch result");
			for (int i = 0; i < bnames.Length; i++)
			{
				Check(Sha(Path.Combine(batchDir, bnames[i])) == bh[i], "batch changed original");
				Check(File.Exists(Path.Combine(batchDir, "_cc", Path.GetFileNameWithoutExtension(bnames[i]) + "_cc.jpg")), "batch output missing");
			}
			Check(br.LogFile != null && File.Exists(br.LogFile), "batch log");
			Pump(300);
			Check(((Border)wnd.FindName("BusyOverlay")).Visibility == Visibility.Collapsed, "busy overlay left after batch");
			Snapshot(wnd, Path.Combine(evidence, "color-adjust-07-batch-done.png"));
			Console.WriteLine("PASS actual WPF: batch applies selected template to 3/3 images in _cc, originals byte-identical, log written");
			wnd.Close();
			app.Shutdown();
			Console.WriteLine("COLOR_ADJUST_WINDOW_PASSED=9");
			return 0;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return 1;
		}
		finally
		{
			try { Directory.Delete(dir, true); } catch (Exception) { }
		}
	}
}
