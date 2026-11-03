using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CameraControl.Classes;
using CameraControl.Devices;

namespace CameraControl.windows;

// G2 배경제거 screen. One window; the rail button brings it forward with the photo selected in G2 as the target.
// Flow: target (photos or a folder) → 확인 (/api/batch/scan, nothing written) → 배경 제거 시작 (/api/batch/start) → the
// job is polled for progress and the engine's before/after thumbnails, which are replaced by the full-size photo and
// saved PNG. Both panes zoom (wheel) and move (drag) together. The engine keeps running if the window closes.
public partial class G2BackgroundRemovalWnd : Window
{
	private static G2BackgroundRemovalWnd _instance;

	private readonly DispatcherTimer _healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };

	private readonly DispatcherTimer _jobTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };

	private bool _ready;

	private bool _running;

	private bool _polling;

	private bool _settingTarget;

	private string _planId;

	private string _jobId;

	private string _output;

	private string _shownSource;

	private string _shownResult;

	// What the last 확인 listed: the selected photos (or the one folder), so a preview name maps back to files.
	private string[] _scanPaths = new string[0];

	private bool _scanFolder;

	// Preview item on screen; its full-size source/result replace the engine's 400 px thumbnails once read.
	private string _currentName;

	private bool _sourceFromFile;

	private bool _resultFromFile;

	private bool _resultLoading;

	// Shared by both panes: zooming or moving one moves the other (same spot before/after).
	private readonly ScaleTransform _zoom = new ScaleTransform();

	private readonly TranslateTransform _pan = new TranslateTransform();

	private Point? _dragFrom;

	private Point _panFrom;

	public G2BackgroundRemovalWnd()
	{
		InitializeComponent();
		TransformGroup view = new TransformGroup();
		view.Children.Add(_zoom);
		view.Children.Add(_pan);
		SourceCanvas.RenderTransform = view;
		ResultCanvas.RenderTransform = view;
		_healthTimer.Tick += (s, e) => CheckHealth();
		_jobTimer.Tick += (s, e) => PollJob();
		Loaded += (s, e) => CheckHealth();
		Closed += (s, e) =>
		{
			_healthTimer.Stop();
			_jobTimer.Stop();
			if (_instance == this) _instance = null;
		};
	}

	// Rail button (UI thread). target: a photo, a folder or "" (keep what the window has).
	public static void ShowFor(string target)
	{
		if (_instance == null)
		{
			_instance = new G2BackgroundRemovalWnd();
			Window main = Application.Current?.MainWindow;
			if (main != null && main != _instance && main.IsVisible) _instance.Owner = main;
			_instance.Show();
		}
		else
		{
			if (_instance.WindowState == WindowState.Minimized) _instance.WindowState = WindowState.Normal;
			_instance.Activate();
		}
		if (!string.IsNullOrEmpty(target) && !_instance._running) _instance.SetTarget(target);
	}

	internal void SetTarget(string target)
	{
		bool folder = Directory.Exists(target);
		_settingTarget = true;
		(folder ? ModeFolder : ModeFile).IsChecked = true;
		TargetBox.Text = target;
		_settingTarget = false;
		Invalidate();
		Scan();
	}

	// ---------- engine state ----------

	private void CheckHealth()
	{
		_healthTimer.Stop();
		Task.Run(() => CloriumBackgroundRemoval.Health()).ContinueWith(t =>
		{
			if (t.IsFaulted)
			{
				_ready = false;
				SetChip("연결 안 됨", "G2Err", "G2Neutral", t.Exception.GetBaseException().Message);
			}
			else if (t.Result.Ready)
			{
				_ready = true;
				SetChip("준비 완료", "G2Ok", "G2OkBg", "배경제거 엔진(BiRefNet)이 준비됐습니다.");
			}
			else
			{
				_ready = false;
				SetChip("모델 준비 중", "G2NeutralText", "G2Neutral", string.IsNullOrEmpty(t.Result.Message) ? "모델을 불러오고 있습니다." : t.Result.Message);
			}
			UpdateButtons();
			if (!_ready && IsLoaded) _healthTimer.Start();
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	private void SetChip(string text, string foreground, string background, string tip)
	{
		EngineText.Text = text;
		EngineText.SetResourceReference(TextBlock.ForegroundProperty, foreground);
		EngineChip.SetResourceReference(Border.BackgroundProperty, background);
		EngineChip.ToolTip = tip;
	}

	private void UpdateButtons()
	{
		StartButton.IsEnabled = _ready && _planId != null && !_running;
		StartButton.Visibility = _running ? Visibility.Collapsed : Visibility.Visible;
		StopButton.Visibility = _running ? Visibility.Visible : Visibility.Collapsed;
		bool idle = !_running;
		ModeFile.IsEnabled = ModeFolder.IsEnabled = TargetBox.IsEnabled = BrowseButton.IsEnabled = ScanButton.IsEnabled = idle;
		Quality1024.IsEnabled = Quality2048.IsEnabled = Edge0.IsEnabled = Edge1.IsEnabled = Edge2.IsEnabled = OpaqueSwitch.IsEnabled = DetailSwitch.IsEnabled = idle;
		OpenOutputButton.IsEnabled = !string.IsNullOrEmpty(_output) && Directory.Exists(_output);
		if (!_running && _planId != null && !_ready) SettingsHint.Text = "모델 준비가 끝나면 시작할 수 있습니다.";
		else SettingsHint.Text = "원본은 그대로 두고, 옆의 _bgr 폴더에 투명 배경 PNG로 저장합니다.";
	}

	// ---------- target ----------

	private void Mode_Checked(object sender, RoutedEventArgs e)
	{
		if (BrowseText == null) return;
		BrowseText.Text = ModeFolder.IsChecked == true ? "폴더 선택" : "사진 선택";
		if (!_settingTarget) Invalidate();
	}

	private void TargetBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		if (!_settingTarget && IsLoaded) Invalidate();
	}

	private void Invalidate()
	{
		_planId = null;
		ScanCard.Visibility = Visibility.Collapsed;
		ScanError.Visibility = Visibility.Collapsed;
		UpdateButtons();
	}

	private void Browse_Click(object sender, RoutedEventArgs e)
	{
		string[] current = Paths();
		if (ModeFolder.IsChecked == true)
		{
			using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "배경을 지울 사진 폴더를 선택하세요. 하위 폴더도 함께 처리합니다." })
			{
				if (current.Length > 0 && Directory.Exists(current[0])) dialog.SelectedPath = current[0];
				if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
				SetTarget(dialog.SelectedPath);
			}
			return;
		}
		Microsoft.Win32.OpenFileDialog files = new Microsoft.Win32.OpenFileDialog
		{
			Title = "배경을 지울 사진 선택",
			Multiselect = true,
			Filter = "사진 (JPG, PNG, WebP, TIFF, BMP)|*.jpg;*.jpeg;*.png;*.webp;*.tif;*.tiff;*.bmp"
		};
		if (current.Length > 0)
		{
			string folder = File.Exists(current[0]) ? Path.GetDirectoryName(current[0]) : current[0];
			if (Directory.Exists(folder)) files.InitialDirectory = folder;
		}
		if (files.ShowDialog(this) != true) return;
		_settingTarget = true;
		ModeFile.IsChecked = true;
		TargetBox.Text = string.Join(Environment.NewLine, files.FileNames);
		_settingTarget = false;
		Invalidate();
		Scan();
	}

	private string[] Paths()
	{
		return (TargetBox.Text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim().Trim('"').Trim()).Where(p => p.Length > 0).ToArray();
	}

	private void Scan_Click(object sender, RoutedEventArgs e) => Scan();

	private void Scan()
	{
		string[] paths = Paths();
		bool folder = ModeFolder.IsChecked == true;
		if (paths.Length == 0)
		{
			ShowScanError(folder ? "사진 폴더를 선택해 주세요." : "배경을 지울 사진을 선택해 주세요.");
			return;
		}
		if (folder && paths.Length > 1)
		{
			ShowScanError("폴더는 하나만 지정해 주세요.");
			return;
		}
		ScanButton.IsEnabled = false;
		StatusTitle.Text = "대상을 확인하고 있습니다";
		StatusText.Text = "";
		Task.Run(() => CloriumBackgroundRemoval.Scan(paths, folder)).ContinueWith(t =>
		{
			ScanButton.IsEnabled = !_running;
			if (t.IsFaulted)
			{
				ShowScanError(t.Exception.GetBaseException().Message);
				StatusTitle.Text = "사진이나 폴더를 고른 뒤 확인을 눌러 주세요";
				return;
			}
			JsonElement plan = t.Result;
			_planId = plan.GetProperty("plan_id").GetString();
			_output = plan.GetProperty("output").GetString();
			int total = plan.GetProperty("total").GetInt32();
			int existing = plan.TryGetProperty("existing", out JsonElement ex) ? ex.GetInt32() : 0;
			int excluded = plan.TryGetProperty("excluded", out JsonElement exc) ? exc.GetInt32() : 0;
			StringBuilder summary = new StringBuilder("사진 " + total + "장");
			if (folder) summary.Append(" (하위 폴더 포함)");
			if (existing > 0) summary.Append(" · 이미 결과가 있는 " + existing + "장은 새 이름으로 저장");
			if (excluded > 0) summary.Append(" · 결과/연결 폴더 " + excluded + "개 제외");
			ScanSummary.Text = summary.ToString();
			ScanOutput.Text = "저장 위치: " + _output;
			ScanCard.Visibility = Visibility.Visible;
			ScanError.Visibility = Visibility.Collapsed;
			StatusTitle.Text = "준비됐습니다. 배경 제거 시작을 눌러 주세요";
			StatusText.Text = "처리할 사진 " + total + "장";
			CountText.Text = "0 / " + total;
			Progress.Value = 0;
			ErrorsCard.Visibility = Visibility.Collapsed;
			ShowFirstSource(plan, paths, folder);
			UpdateButtons();
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	private void ShowScanError(string message)
	{
		_planId = null;
		ScanCard.Visibility = Visibility.Collapsed;
		ScanError.Text = message;
		ScanError.Visibility = Visibility.Visible;
		UpdateButtons();
	}

	// Before processing: the first photo of the plan (full size) on the left, nothing on the right.
	private void ShowFirstSource(JsonElement plan, string[] paths, bool folder)
	{
		_scanPaths = paths;
		_scanFolder = folder;
		string first = null;
		if (!folder) first = paths[0];
		else if (plan.TryGetProperty("examples", out JsonElement examples) && examples.GetArrayLength() > 0) first = Path.Combine(paths[0], examples[0].GetProperty("source").GetString());
		SourceImage.Source = null;
		ResultImage.Source = null;
		ResultEmpty.Visibility = Visibility.Visible;
		_shownSource = _shownResult = _currentName = null;
		_resultFromFile = _resultLoading = false;
		Fit();
		if (first != null) LoadInto(SourceImage, first, () => _currentName == null);
	}

	// Full-size read off the UI thread; applied only while the same item is still on screen.
	private void LoadInto(Image target, string path, Func<bool> stillWanted, Action<bool> done = null)
	{
		Task.Run(() => File.Exists(path) ? LoadFile(path) : null).ContinueWith(t =>
		{
			bool ok = t.Result != null && stillWanted();
			if (ok)
			{
				target.Source = t.Result;
				if (target == ResultImage) ResultEmpty.Visibility = Visibility.Collapsed;
				UpdateZoomText();
			}
			done?.Invoke(ok);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	private string SourcePathOf(string name)
	{
		if (string.IsNullOrEmpty(name) || _scanPaths.Length == 0) return null;
		if (_scanFolder) return Path.Combine(_scanPaths[0], name);
		return _scanPaths.FirstOrDefault(p => p.EndsWith("\\" + name, StringComparison.OrdinalIgnoreCase) || string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
	}

	// Same rule as the engine (batch.output_path): every folder level gets _bgr, the file <stem>_bgr.png. A name the
	// engine had to change (front.jpg + front.png, an earlier result) is not found and the thumbnail stays.
	private string ResultPathOf(string name)
	{
		if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(_output)) return null;
		string[] parts = name.Split('\\', '/');
		string folder = _output;
		for (int i = 0; i < parts.Length - 1; i++) folder = Path.Combine(folder, parts[i] + "_bgr");
		return Path.Combine(folder, Path.GetFileNameWithoutExtension(parts[parts.Length - 1]) + "_bgr.png");
	}

	private void TryResultFile()
	{
		if (_resultFromFile || _resultLoading || _currentName == null) return;
		string name = _currentName, path = ResultPathOf(name);
		if (path == null || !File.Exists(path)) return;
		_resultLoading = true;
		LoadInto(ResultImage, path, () => _currentName == name, ok =>
		{
			_resultLoading = false;
			if (ok) _resultFromFile = true;
		});
	}

	private static BitmapImage LoadFile(string path)
	{
		try
		{
			BitmapImage image = new BitmapImage();
			image.BeginInit();
			image.CacheOption = BitmapCacheOption.OnLoad;
			image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
			image.UriSource = new Uri(path);
			image.EndInit();
			image.Freeze();
			return image;
		}
		catch (Exception ex) when (ex is IOException || ex is NotSupportedException || ex is UriFormatException || ex is ArgumentException || ex is InvalidOperationException)
		{
			return null;
		}
	}

	// "data:image/png;base64,..." from the engine's batch preview.
	private static BitmapImage LoadDataUrl(string url)
	{
		int comma = url?.IndexOf(',') ?? -1;
		if (comma < 0) return null;
		try
		{
			BitmapImage image = new BitmapImage();
			image.BeginInit();
			image.CacheOption = BitmapCacheOption.OnLoad;
			image.StreamSource = new MemoryStream(Convert.FromBase64String(url.Substring(comma + 1)));
			image.EndInit();
			image.Freeze();
			return image;
		}
		catch (Exception ex) when (ex is FormatException || ex is NotSupportedException || ex is IOException || ex is InvalidOperationException)
		{
			return null;
		}
	}

	// ---------- processing ----------

	private void Start_Click(object sender, RoutedEventArgs e)
	{
		if (_planId == null || _running) return;
		string plan = _planId;
		int resolution = Quality2048.IsChecked == true ? 2048 : 1024;
		int edge = Edge0.IsChecked == true ? 0 : Edge2.IsChecked == true ? 2 : 1;
		bool opaque = OpaqueSwitch.IsChecked == true, detail = DetailSwitch.IsChecked == true;
		StartButton.IsEnabled = false;
		Task.Run(() => CloriumBackgroundRemoval.StartBatch(plan, resolution, edge, opaque, detail)).ContinueWith(t =>
		{
			if (t.IsFaulted)
			{
				StatusTitle.Text = "시작하지 못했습니다";
				StatusText.Text = t.Exception.GetBaseException().Message;
				UpdateButtons();
				return;
			}
			_jobId = t.Result.GetProperty("id").GetString();
			_running = true;
			ErrorsCard.Visibility = Visibility.Collapsed;
			StatusTitle.Text = "배경을 지우고 있습니다";
			UpdateButtons();
			Apply(t.Result);
			_jobTimer.Start();
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	private void Stop_Click(object sender, RoutedEventArgs e)
	{
		string job = _jobId;
		if (job == null) return;
		StopButton.IsEnabled = false;
		Task.Run(() => CloriumBackgroundRemoval.CancelBatch(job)).ContinueWith(t =>
		{
			StopButton.IsEnabled = true;
			StatusText.Text = t.IsFaulted ? t.Exception.GetBaseException().Message : "현재 사진 저장을 마친 뒤 중지합니다.";
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	private void PollJob()
	{
		if (_polling || _jobId == null) return;
		_polling = true;
		string job = _jobId;
		Task.Run(() => CloriumBackgroundRemoval.BatchStatus(job)).ContinueWith(t =>
		{
			_polling = false;
			if (t.IsFaulted)
			{
				StatusText.Text = t.Exception.GetBaseException().Message;
				Log.Debug("BackgroundRemoval : status failed " + t.Exception.GetBaseException().Message);
				return;
			}
			Apply(t.Result);
		}, TaskScheduler.FromCurrentSynchronizationContext());
	}

	private void Apply(JsonElement job)
	{
		int total = Int(job, "total"), completed = Int(job, "completed"), success = Int(job, "success"), failed = Int(job, "failed");
		string status = Str(job, "status");
		Progress.Maximum = Math.Max(1, total);
		Progress.Value = completed;
		CountText.Text = completed + " / " + total;
		string current = Str(job, "current");
		StatusText.Text = string.IsNullOrEmpty(current) ? Str(job, "message") : current + " · " + Str(job, "message");
		if (job.TryGetProperty("output", out JsonElement output) && output.ValueKind == JsonValueKind.String) _output = output.GetString();
		if (job.TryGetProperty("preview", out JsonElement preview) && preview.ValueKind == JsonValueKind.Object)
		{
			// The engine only sends 400 px thumbnails: shown at once, then replaced by the photo and the saved PNG.
			string name = Str(preview, "name"), source = Str(preview, "source"), result = Str(preview, "result");
			if (name != _currentName)
			{
				_currentName = name;
				_sourceFromFile = _resultFromFile = _resultLoading = false;
				_shownSource = _shownResult = null;
				string sourcePath = SourcePathOf(name);
				if (sourcePath != null) LoadInto(SourceImage, sourcePath, () => _currentName == name, ok => _sourceFromFile |= ok);
			}
			if (source != _shownSource && !_sourceFromFile)
			{
				_shownSource = source;
				SourceImage.Source = LoadDataUrl(source);
			}
			if (result != _shownResult && !_resultFromFile)
			{
				_shownResult = result;
				ResultImage.Source = LoadDataUrl(result);
				ResultEmpty.Visibility = ResultImage.Source == null ? Visibility.Visible : Visibility.Collapsed;
			}
			if (!string.IsNullOrEmpty(result)) TryResultFile();
		}
		if (job.TryGetProperty("errors", out JsonElement errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
		{
			ErrorsText.Text = string.Join(Environment.NewLine, errors.EnumerateArray().Select(x => Str(x, "file") + " — " + Str(x, "error")));
			ErrorsCard.Visibility = Visibility.Visible;
		}
		if (status == "running") return;
		TryResultFile();
		_jobTimer.Stop();
		_running = false;
		_planId = null;
		StopButton.IsEnabled = true;
		switch (status)
		{
			case "done":
				StatusTitle.Text = failed > 0 ? "끝났습니다: " + success + "장 저장, " + failed + "장 실패" : "끝났습니다: " + success + "장 저장";
				break;
			case "cancelled":
				StatusTitle.Text = "중지했습니다: " + success + "장 저장";
				break;
			default:
				StatusTitle.Text = "처리하지 못했습니다";
				break;
		}
		StatusText.Text = Str(job, "message") + (job.TryGetProperty("seconds", out JsonElement s) && s.ValueKind == JsonValueKind.Number ? " · " + s.GetDouble().ToString("0.#") + "초" : "") + " · 다시 처리하려면 확인을 눌러 주세요.";
		ScanCard.Visibility = Visibility.Collapsed;
		UpdateButtons();
	}

	private static int Int(JsonElement e, string name) => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

	private static string Str(JsonElement e, string name) => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() : "";

	// ---------- zoom (both panes) ----------

	private const double MaxZoom = 40;

	private void View_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		ZoomAt(e.GetPosition((IInputElement)sender), _zoom.ScaleX * Math.Pow(1.25, e.Delta / 120.0));
		e.Handled = true;
	}

	private void View_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount == 2)
		{
			Fit();
			return;
		}
		if (_zoom.ScaleX <= 1) return;
		FrameworkElement view = (FrameworkElement)sender;
		_dragFrom = e.GetPosition(view);
		_panFrom = new Point(_pan.X, _pan.Y);
		view.CaptureMouse();
		view.Cursor = Cursors.SizeAll;
	}

	private void View_MouseMove(object sender, MouseEventArgs e)
	{
		if (_dragFrom == null) return;
		Point now = e.GetPosition((IInputElement)sender);
		_pan.X = _panFrom.X + now.X - _dragFrom.Value.X;
		_pan.Y = _panFrom.Y + now.Y - _dragFrom.Value.Y;
		ClampPan();
	}

	private void View_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		if (_dragFrom == null) return;
		_dragFrom = null;
		FrameworkElement view = (FrameworkElement)sender;
		view.ReleaseMouseCapture();
		view.Cursor = Cursors.Hand;
	}

	private void ZoomIn_Click(object sender, RoutedEventArgs e) => ZoomAt(ViewCenter(), _zoom.ScaleX * 1.5);

	private void ZoomOut_Click(object sender, RoutedEventArgs e) => ZoomAt(ViewCenter(), _zoom.ScaleX / 1.5);

	private void ZoomFit_Click(object sender, RoutedEventArgs e) => Fit();

	// 100% = one photo pixel per screen pixel.
	private void ZoomActual_Click(object sender, RoutedEventArgs e)
	{
		double actual = ActualPixelScale();
		if (actual > 0) ZoomAt(ViewCenter(), actual);
	}

	private Point ViewCenter() => new Point(SourceView.ActualWidth / 2, SourceView.ActualHeight / 2);

	private void Fit()
	{
		_zoom.ScaleX = _zoom.ScaleY = 1;
		_pan.X = _pan.Y = 0;
		UpdateZoomText();
	}

	// p is in pane coordinates; the point under the cursor stays put.
	private void ZoomAt(Point p, double scale)
	{
		scale = Math.Max(1, Math.Min(MaxZoom, scale));
		double old = _zoom.ScaleX;
		_pan.X = p.X - (p.X - _pan.X) * scale / old;
		_pan.Y = p.Y - (p.Y - _pan.Y) * scale / old;
		_zoom.ScaleX = _zoom.ScaleY = scale;
		ClampPan();
		UpdateZoomText();
	}

	private void ClampPan()
	{
		double w = SourceView.ActualWidth, h = SourceView.ActualHeight, s = _zoom.ScaleX;
		_pan.X = Math.Min(0, Math.Max(w - w * s, _pan.X));
		_pan.Y = Math.Min(0, Math.Max(h - h * s, _pan.Y));
	}

	// Zoom factor at which the shown photo is drawn 1:1 on screen (0 = nothing shown yet).
	private double ActualPixelScale()
	{
		Image image = SourceImage.Source != null ? SourceImage : ResultImage;
		if (!(image.Source is BitmapSource bitmap) || image.ActualWidth <= 0) return 0;
		double dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
		return bitmap.PixelWidth / (image.ActualWidth * dpi);
	}

	private void UpdateZoomText()
	{
		double s = _zoom.ScaleX, actual = ActualPixelScale();
		ZoomText.Text = s <= 1.001 ? "맞춤" + (actual > 0 ? " · " + Math.Round(100 / actual) + "%" : "") : actual > 0 ? Math.Round(100 * s / actual) + "%" : "×" + s.ToString("0.#");
		// At 200% and more show crisp photo pixels (edges of the cut-out) instead of smoothing them.
		BitmapScalingMode mode = actual > 0 && s / actual >= 2 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality;
		RenderOptions.SetBitmapScalingMode(SourceImage, mode);
		RenderOptions.SetBitmapScalingMode(ResultImage, mode);
	}

	private void OpenOutput_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(_output) || !Directory.Exists(_output)) return;
		try
		{
			Process.Start(new ProcessStartInfo("explorer.exe", "\"" + _output + "\"") { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			Log.Error("BackgroundRemoval : unable to open " + _output, ex);
		}
	}

	private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
	{
		if (!_running || _jobId == null) return;
		MessageBoxResult answer = MessageBox.Show(this, "배경 제거가 진행 중입니다.\n\n중지하고 닫을까요? (이미 저장된 결과는 유지됩니다)\n\n아니요를 누르면 계속 처리하고 창은 닫지 않습니다.", "배경제거", MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (answer != MessageBoxResult.Yes)
		{
			e.Cancel = true;
			return;
		}
		string job = _jobId;
		Task.Run(() =>
		{
			try { CloriumBackgroundRemoval.CancelBatch(job); }
			catch (CloriumBackgroundRemoval.ApiException ex) { Log.Debug("BackgroundRemoval : cancel on close failed " + ex.Message); }
		});
	}
}
