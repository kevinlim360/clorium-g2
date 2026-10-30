using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CameraControl.Classes;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using ImageMagick;

namespace CameraControl.windows
{
	/// <summary>
	/// G2 색보정 창. 노출·명도·대비·채도·색온도·색조·채널별 레벨.
	/// 미리보기는 축소본으로, 저장은 원본 해상도로 같은 엔진(G2ImageAdjust)을 거친다. 원본 파일은 덮어쓰지 않는다.
	/// </summary>
	public partial class G2ColorAdjustWnd : Window
	{
		private const int PreviewEdge = 2000;

		/// <summary>이 실행 동안 마지막으로 저장한 보정값("이전 보정값").</summary>
		private static G2AdjustSettings _lastSaved;

		private readonly string _source;

		private readonly bool _addToSession;

		private readonly G2AdjustSettings _settings = new G2AdjustSettings();

		private readonly object _previewLock = new object();

		private readonly List<Row> _rows = new List<Row>();

		private readonly List<Row> _levelRows = new List<Row>();

		private IMagickImage<byte> _preview;

		private BitmapSource _originalBitmap;

		private BitmapSource _adjustedBitmap;

		private G2AdjustSettings _pending;

		private bool _rendering;

		private bool _busy = true;

		private bool _closed;

		private bool _holdCompare;

		private int _channel;

		private bool _syncing;

		private bool _saving;

		/// <summary>원본에 투명도가 있는지(없으면 미리보기를 항상 불투명으로 표시).</summary>
		private bool _sourceHasAlpha;

		/// <summary>템플릿 저장 파일. null이면 %APPDATA%\Clorium\G2\color-templates.xml (검사에서 임시 경로로 바꿀 수 있음).</summary>
		public static string TemplateStorePath { get; set; }

		private G2ColorTemplateStore _store;

		/// <summary>목록에서 마지막으로 적용한 템플릿(이름 기준).</summary>
		private string _appliedTemplate;

		private bool _templateRenaming;

		private bool _selectingTemplate;

		private bool _batching;

		private CancellationTokenSource _batchCts;

		/// <summary>마지막 일괄 적용 결과(검사·상태 표시용).</summary>
		public G2BatchResult LastBatch { get; private set; }

		/// <summary>배치 시작 전 확인 창을 띄울지(자동 검사에서 끔).</summary>
		public bool ConfirmBatch { get; set; } = true;

		/// <summary>저장에 성공한 마지막 파일 경로.</summary>
		public string SavedFile { get; private set; }

		public G2ColorAdjustWnd(string sourceFile, bool addToSession = true)
		{
			_source = sourceFile;
			_addToSession = addToSession;
			InitializeComponent();
			FileTitle.Text = Path.GetFileName(sourceFile);
			Title = "색보정 — " + Path.GetFileName(sourceFile);
			BuildRows();
			LoadTemplates();
			SetBusy(true, "사진을 불러오는 중…");
			PrevSettingsButton.IsEnabled = _lastSaved != null;
			EngineText.Text = G2ImageAdjust.EngineVersion;
			Loaded += async (s, e) => await LoadAsync();
			Closing += (s, e) =>
			{
				if (_saving)
				{
					e.Cancel = true;
				}
				else if (_batching)
				{
					e.Cancel = true;
					_batchCts?.Cancel();
					BusyText.Text = "현재 사진을 저장한 뒤 멈추는 중…";
				}
			};
			Closed += (s, e) =>
			{
				_closed = true;
				lock (_previewLock)
				{
					_preview?.Dispose();
					_preview = null;
				}
			};
			PreviewKeyDown += Window_PreviewKeyDown;
			PreviewKeyUp += Window_PreviewKeyUp;
		}

		// ---------------------------------------------------------------- rows

		private sealed class Row
		{
			public Slider Slider;
			public TextBox Box;
			public Func<double> Get;
			public Action<double> Set;
			public Func<double, double> ToSlider;
			public Func<double, double> FromSlider;
			public string Format;
			public double Min, Max, Default;
		}

		private void BuildRows()
		{
			var s = _settings;
			AddRow(TonePanel, "노출", "EV", -3, 3, 0, "+0.00;-0.00;0.00", 0.05, () => s.Exposure, v => s.Exposure = v, null,
				"선형광 기준 노출 보정입니다. +1EV는 빛의 양 2배입니다.");
			AddRow(TonePanel, "명도", null, -100, 100, 0, "+0;-0;0", 1, () => s.Brightness, v => s.Brightness = v, null,
				"검정과 흰색은 그대로 두고 중간톤 밝기를 조절합니다.");
			AddRow(TonePanel, "대비", null, -100, 100, 0, "+0;-0;0", 1, () => s.Contrast, v => s.Contrast = v, null,
				"중간 회색을 기준으로 밝은 곳과 어두운 곳의 차이를 조절합니다.");
			AddRow(TonePanel, "채도", null, -100, 100, 0, "+0;-0;0", 1, () => s.Saturation, v => s.Saturation = v,
				Gradient(Color.FromRgb(0x9A, 0x9F, 0xA6), Color.FromRgb(0xE5, 0x48, 0x4D), Color.FromRgb(0x30, 0xA4, 0x6C), Color.FromRgb(0x3E, 0x7B, 0xFA)),
				"휘도(Rec.709)를 유지하며 색의 진하기를 조절합니다. −100은 흑백입니다.");

			AddRow(ColorPanel, "색온도", null, -100, 100, 0, "+0;-0;0", 1, () => s.Temperature, v => s.Temperature = v,
				Gradient(Color.FromRgb(0x5B, 0x8D, 0xEF), Color.FromRgb(0xEE, 0xEE, 0xEE), Color.FromRgb(0xF2, 0xA6, 0x3B)),
				"− 차갑게(파랑) · + 따뜻하게(주황). 밝기는 유지합니다.");
			AddRow(ColorPanel, "색조", null, -100, 100, 0, "+0;-0;0", 1, () => s.Tint, v => s.Tint = v,
				Gradient(Color.FromRgb(0x4C, 0xB0, 0x5A), Color.FromRgb(0xEE, 0xEE, 0xEE), Color.FromRgb(0xD0, 0x4C, 0xC4)),
				"− 녹색 · + 마젠타. 형광등 등의 색 틀어짐을 보정합니다.");

			_levelRows.Add(AddRow(LevelsPanel, "검정점", null, 0, 254, 0, "0", 1, () => CurrentLevels.Black, v => SetBlack(v), null,
				"이 값 이하를 검정으로 만듭니다."));
			// gamma: slider position p in [-1, 1] ↔ gamma = 3^p (0.33 … 3.00), 1.00 at the centre
			_levelRows.Add(AddRow(LevelsPanel, "감마", null, 0.1, 10, 1, "0.00", 0.01, () => CurrentLevels.Gamma, v => CurrentLevels.Gamma = v, null,
				"중간톤을 밝게(>1) 또는 어둡게(<1) 합니다.", g => Math.Log(Math.Max(0.1, g)) / Math.Log(3), p => Math.Round(Math.Pow(3, p), 2), -1, 1));
			_levelRows.Add(AddRow(LevelsPanel, "흰점", null, 1, 255, 255, "0", 1, () => CurrentLevels.White, v => SetWhite(v), null,
				"이 값 이상을 최대 밝기로 만듭니다."));
			UpdateLevelTracks();
		}

		private G2Levels CurrentLevels => _settings.Channel(_channel);

		private void SetBlack(double v)
		{
			G2Levels l = CurrentLevels;
			l.Black = (int)Math.Round(Math.Max(0, Math.Min(254, v)));
			if (l.White <= l.Black)
			{
				l.White = l.Black + 1;
			}
		}

		private void SetWhite(double v)
		{
			G2Levels l = CurrentLevels;
			l.White = (int)Math.Round(Math.Max(1, Math.Min(255, v)));
			if (l.Black >= l.White)
			{
				l.Black = l.White - 1;
			}
		}

		private static Brush Gradient(params Color[] colors)
		{
			var b = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
			for (int i = 0; i < colors.Length; i++)
			{
				b.GradientStops.Add(new GradientStop(colors[i], colors.Length == 1 ? 0 : (double)i / (colors.Length - 1)));
			}
			b.Freeze();
			return b;
		}

		private Row AddRow(Panel panel, string label, string unit, double min, double max, double def, string format, double step,
			Func<double> get, Action<double> set, Brush track, string hint,
			Func<double, double> toSlider = null, Func<double, double> fromSlider = null, double sliderMin = double.NaN, double sliderMax = double.NaN)
		{
			var row = new Row
			{
				Get = get,
				Set = set,
				Format = format,
				Min = min,
				Max = max,
				Default = def,
				ToSlider = toSlider ?? (v => v),
				FromSlider = fromSlider ?? (v => v)
			};
			var grid = new Grid { Margin = new Thickness(0, 6, 0, 0) };
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			var name = new TextBlock { Text = unit == null ? label : label + "  ", Style = (Style)FindResource("CcRowLabel"), ToolTip = hint + "\n두 번 누르면 기본값으로 돌아갑니다." };
			if (unit != null)
			{
				name.Inlines.Add(new System.Windows.Documents.Run(unit) { Foreground = (Brush)FindResource("G2Sub"), FontSize = 11 });
			}
			name.MouseLeftButtonDown += (s, e) =>
			{
				if (e.ClickCount == 2 && !_busy)
				{
					row.Set(row.Default);
					AfterChange(row);
					e.Handled = true;
				}
			};
			grid.Children.Add(name);

			row.Box = new TextBox { Style = (Style)FindResource("CcValueBox"), ToolTip = "숫자를 입력하고 Enter" };
			Grid.SetColumn(row.Box, 1);
			row.Box.KeyDown += (s, e) =>
			{
				if (e.Key == Key.Enter)
				{
					CommitBox(row);
					e.Handled = true;
				}
				else if (e.Key == Key.Escape)
				{
					ShowValue(row);
				}
			};
			row.Box.LostKeyboardFocus += (s, e) => CommitBox(row);
			row.Box.GotKeyboardFocus += (s, e) => row.Box.Dispatcher.BeginInvoke(new Action(() => row.Box.SelectAll()), System.Windows.Threading.DispatcherPriority.Input);
			grid.Children.Add(row.Box);

			row.Slider = new Slider
			{
				Style = (Style)FindResource(track == null ? "CcCenterSlider" : "CcGradientSlider"),
				Minimum = double.IsNaN(sliderMin) ? min : sliderMin,
				Maximum = double.IsNaN(sliderMax) ? max : sliderMax,
				SmallChange = step,
				LargeChange = step * 10,
				IsMoveToPointEnabled = true,
				Margin = new Thickness(0, 4, 0, 0),
				ToolTip = hint
			};
			if (track != null)
			{
				row.Slider.Background = track;
			}
			if (toSlider == null)
			{
				row.Slider.TickFrequency = step;
				row.Slider.IsSnapToTickEnabled = step >= 1;
			}
			row.Slider.ValueChanged += (s, e) =>
			{
				if (_syncing)
				{
					return;
				}
				double v = row.FromSlider(row.Slider.Value);
				if (step < 1 && toSlider == null)
				{
					v = Math.Round(v / step) * step;
				}
				row.Set(v);
				AfterChange(row);
			};
			Grid.SetRow(row.Slider, 1);
			Grid.SetColumnSpan(row.Slider, 2);
			grid.Children.Add(row.Slider);

			panel.Children.Add(grid);
			_rows.Add(row);
			ShowValue(row);
			return row;
		}

		private void CommitBox(Row row)
		{
			string text = (row.Box.Text ?? "").Trim().Replace(',', '.');
			if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && !double.IsNaN(v))
			{
				v = Math.Max(row.Min, Math.Min(row.Max, v));
				if (Math.Abs(v - row.Get()) > 1e-9)
				{
					row.Set(v);
					AfterChange(row);
					return;
				}
			}
			ShowValue(row);
		}

		private void ShowValue(Row row)
		{
			_syncing = true;
			try
			{
				double v = row.Get();
				row.Box.Text = v.ToString(row.Format, CultureInfo.InvariantCulture);
				row.Slider.Value = Math.Max(row.Slider.Minimum, Math.Min(row.Slider.Maximum, row.ToSlider(v)));
			}
			finally
			{
				_syncing = false;
			}
		}

		private void ShowAllValues()
		{
			foreach (Row r in _rows)
			{
				ShowValue(r);
			}
			UpdateChannelSummary();
		}

		private void AfterChange(Row changed)
		{
			// levels rows influence each other (black < white)
			if (_levelRows.Contains(changed))
			{
				foreach (Row r in _levelRows)
				{
					ShowValue(r);
				}
				UpdateChannelSummary();
			}
			else
			{
				ShowValue(changed);
			}
			UpdateTemplateState();
			RequestRender();
		}

		private void Channel_Checked(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement fe && fe.Tag != null && int.TryParse(fe.Tag.ToString(), out int ch))
			{
				_channel = ch;
				if (_levelRows.Count > 0)
				{
					foreach (Row r in _levelRows)
					{
						ShowValue(r);
					}
					UpdateLevelTracks();
					UpdateChannelSummary();
				}
			}
		}

		private void UpdateLevelTracks()
		{
			Color c;
			switch (_channel)
			{
				case 1: c = Color.FromRgb(0xE5, 0x48, 0x4D); break;
				case 2: c = Color.FromRgb(0x30, 0xA4, 0x6C); break;
				case 3: c = Color.FromRgb(0x3E, 0x7B, 0xFA); break;
				default: c = Color.FromRgb(0xFF, 0xFF, 0xFF); break;
			}
			Brush ramp = Gradient(Color.FromRgb(0x16, 0x18, 0x1C), c);
			if (_levelRows.Count == 3)
			{
				_levelRows[0].Slider.Background = ramp;
				_levelRows[2].Slider.Background = ramp;
				_levelRows[1].Slider.Background = (Brush)FindResource("G2Line");
			}
		}

		private void UpdateChannelSummary()
		{
			string[] names = { "전체", "R", "G", "B" };
			var parts = new List<string>();
			for (int i = 0; i < 4; i++)
			{
				G2Levels l = _settings.Channel(i);
				if (!l.IsIdentity)
				{
					parts.Add(names[i] + " " + l.Black + " / " + l.Gamma.ToString("0.00", CultureInfo.InvariantCulture) + " / " + l.White);
				}
			}
			ChannelSummary.Text = parts.Count == 0 ? "변경 없음" : string.Join("  ·  ", parts);
		}

		private void ResetChannel_Click(object sender, RoutedEventArgs e)
		{
			if (_busy) return;
			CurrentLevels.Reset();
			foreach (Row r in _levelRows)
			{
				ShowValue(r);
			}
			UpdateChannelSummary();
			RequestRender();
		}

		// ---------------------------------------------------------------- load / render

		private async Task LoadAsync()
		{
			try
			{
				var info = await Task.Run(() =>
				{
					using (MagickImage original = G2ImageAdjust.Load(_source))
					{
						IColorProfile icc = original.GetColorProfile();
						string desc = original.Width + "×" + original.Height + " · " + original.Format.ToString().ToUpperInvariant() +
							" · " + original.Depth + "bit · " + (icc != null ? "ICC " + (string.IsNullOrEmpty(icc.Description) ? icc.ColorSpace.ToString() : icc.Description) : "ICC 없음(sRGB로 간주)");
						IMagickImage<byte> preview = G2ImageAdjust.CreatePreview(original, PreviewEdge);
						_sourceHasAlpha = original.HasAlpha;
						byte[] bgra = G2ImageAdjust.ToBgra(preview, !_sourceHasAlpha);
						return Tuple.Create(preview, bgra, desc, original.Depth > 8, (int)original.Width, (int)original.Height);
					}
				});
				if (_closed)
				{
					info.Item1.Dispose();
					return;
				}
				lock (_previewLock)
				{
					_preview = info.Item1;
				}
				int w = (int)_preview.Width, h = (int)_preview.Height;
				_originalBitmap = Frozen(info.Item2, w, h);
				_adjustedBitmap = _originalBitmap;
				FileInfoText.Text = info.Item3;
				if (info.Item4)
				{
					StatusText.Text = "16비트 원본입니다. 이 버전의 엔진(Q8)은 8비트 정밀도로 처리해 저장합니다. 원본은 그대로 둡니다.";
				}
				ProxyBadgeText.Text = w == info.Item5 && h == info.Item6 ? "원본 크기 미리보기" : "미리보기 " + w + "×" + h + " · 저장 " + info.Item5 + "×" + info.Item6;
				ProxyBadge.Visibility = Visibility.Visible;
				ShowHistogram(G2ImageAdjust.Histogram(info.Item2));
				UpdateStage();
				SetBusy(false, null);
				UpdateChannelSummary();
			}
			catch (Exception ex)
			{
				Log.Error("G2 color adjust load", ex);
				BusyText.Text = "사진을 열 수 없습니다.\n" + ex.GetBaseException().Message;
				SaveButton.IsEnabled = SaveAsButton.IsEnabled = ResetButton.IsEnabled = PrevSettingsButton.IsEnabled = false;
				ControlsPanel.IsEnabled = false;
				_busy = true;
			}
		}

		private void RequestRender()
		{
			UpdateTemplateState();
			if (_preview == null)
			{
				return;
			}
			_pending = _settings.Clone();
			if (!_rendering)
			{
				_ = RenderLoopAsync();
			}
		}

		private async Task RenderLoopAsync()
		{
			_rendering = true;
			try
			{
				while (_pending != null && !_closed)
				{
					G2AdjustSettings s = _pending;
					_pending = null;
					Tuple<BitmapSource, G2Histogram> result;
					try
					{
						result = await Task.Run(() =>
						{
							lock (_previewLock)
							{
								if (_preview == null) return null;
								using (IMagickImage<byte> work = _preview.Clone())
								{
									G2ImageAdjust.Apply(work, s);
									byte[] bgra = G2ImageAdjust.ToBgra(work, !_sourceHasAlpha);
									return Tuple.Create(Frozen(bgra, (int)work.Width, (int)work.Height), G2ImageAdjust.Histogram(bgra));
								}
							}
						});
					}
					catch (Exception ex)
					{
						Log.Error("G2 color adjust preview", ex);
						StatusText.Text = "미리보기 오류: " + ex.GetBaseException().Message;
						continue;
					}
					if (result == null || _closed)
					{
						return;
					}
					_adjustedBitmap = result.Item1;
					ShowHistogram(result.Item2);
					UpdateStage();
				}
			}
			finally
			{
				_rendering = false;
			}
		}

		/// <summary>Immutable, thread-safe preview frame (created off the UI thread).</summary>
		private static BitmapSource Frozen(byte[] bgra, int w, int h)
		{
			BitmapSource b = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, bgra, w * 4);
			b.Freeze();
			return b;
		}

		/// <summary>보기 방식: 0 보정 후, 1 원본, 2 나란히(보정 전/후).</summary>
		private int ViewMode => ViewSide.IsChecked == true ? 2 : (ViewOriginal.IsChecked == true ? 1 : 0);

		private void UpdateStage()
		{
			if (StageImage == null || SidePanel == null)
			{
				return;
			}
			bool side = ViewMode == 2 && !_holdCompare;
			SidePanel.Visibility = side ? Visibility.Visible : Visibility.Collapsed;
			StageImage.Visibility = side ? Visibility.Collapsed : Visibility.Visible;
			if (side)
			{
				SideBefore.Source = _originalBitmap;
				SideAfter.Source = _adjustedBitmap;
				SideAfterText.Text = _settings.IsIdentity ? "보정 없음" : "보정 후";
				StateBadge.Visibility = Visibility.Collapsed;
				return;
			}
			bool original = ViewMode == 1 || _holdCompare;
			StageImage.Source = original ? _originalBitmap : _adjustedBitmap;
			StateBadge.Visibility = _originalBitmap == null ? Visibility.Collapsed : Visibility.Visible;
			StateBadgeText.Text = original ? "원본" : (_settings.IsIdentity ? "보정 없음" : "보정 후");
			StateBadge.Background = original ? (Brush)FindResource("G2Pr") : new SolidColorBrush(Color.FromArgb(0xCC, 0x16, 0x18, 0x1C));
		}

		private void ShowHistogram(G2Histogram h)
		{
			long max = 1;
			for (int i = 1; i < 255; i++)
			{
				max = Math.Max(max, Math.Max(h.Luma[i], Math.Max(h.Red[i], Math.Max(h.Green[i], h.Blue[i]))));
			}
			HistRed.Data = Area(h.Red, max);
			HistGreen.Data = Area(h.Green, max);
			HistBlue.Data = Area(h.Blue, max);
			HistLuma.Data = Line(h.Luma, max);
			ClipText.Text = "그림자 " + h.ShadowClipPercent.ToString("0.0", CultureInfo.InvariantCulture) + "% · 하이라이트 " + h.HighlightClipPercent.ToString("0.0", CultureInfo.InvariantCulture) + "%";
			ClipText.Foreground = (Brush)FindResource(h.HighlightClipPercent >= 1.0 || h.ShadowClipPercent >= 1.0 ? "G2Pr" : "G2Sub");
		}

		private static double BinHeight(long count, long max) => 100.0 * Math.Min(1.0, Math.Sqrt((double)count / max));

		private static Geometry Area(long[] bins, long max)
		{
			var g = new StreamGeometry();
			using (StreamGeometryContext c = g.Open())
			{
				c.BeginFigure(new Point(0, 100), true, true);
				for (int i = 0; i < 256; i++)
				{
					double y = 100 - BinHeight(bins[i], max);
					c.LineTo(new Point(i, y), false, false);
					c.LineTo(new Point(i + 1, y), false, false);
				}
				c.LineTo(new Point(256, 100), false, false);
			}
			g.Freeze();
			return g;
		}

		private static Geometry Line(long[] bins, long max)
		{
			var g = new StreamGeometry();
			using (StreamGeometryContext c = g.Open())
			{
				c.BeginFigure(new Point(0.5, 100 - BinHeight(bins[0], max)), false, false);
				for (int i = 1; i < 256; i++)
				{
					c.LineTo(new Point(i + 0.5, 100 - BinHeight(bins[i], max)), true, true);
				}
			}
			g.Freeze();
			return g;
		}

		private void SetBusy(bool busy, string text)
		{
			_busy = busy;
			BusyOverlay.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
			if (text != null)
			{
				BusyText.Text = text;
			}
			SaveButton.IsEnabled = SaveAsButton.IsEnabled = ResetButton.IsEnabled = !busy;
			PrevSettingsButton.IsEnabled = !busy && _lastSaved != null;
			ControlsPanel.IsEnabled = !busy;
			ViewAdjusted.IsEnabled = ViewOriginal.IsEnabled = ViewSide.IsEnabled = !busy;
			if (!busy)
			{
				UpdateTemplateButtons();
			}
		}

		// ---------------------------------------------------------------- compare

		private void View_Checked(object sender, RoutedEventArgs e) => UpdateStage();

		private void Stage_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (_busy) return;
			_holdCompare = true;
			UpdateStage();
		}

		private void Stage_MouseUp(object sender, MouseButtonEventArgs e)
		{
			_holdCompare = false;
			UpdateStage();
		}

		private void Stage_MouseLeave(object sender, MouseEventArgs e)
		{
			if (_holdCompare)
			{
				_holdCompare = false;
				UpdateStage();
			}
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape && !(Keyboard.FocusedElement is TextBox))
			{
				if (!_saving && !_batching) Close();
				e.Handled = true;
			}
			else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
			{
				if (!_busy) Save_Click(this, null);
				e.Handled = true;
			}
			else if (e.Key == Key.OemBackslash || e.Key == Key.Oem5)
			{
				if (!(Keyboard.FocusedElement is TextBox) && !_busy)
				{
					_holdCompare = true;
					UpdateStage();
					e.Handled = true;
				}
			}
		}

		private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.OemBackslash || e.Key == Key.Oem5) && _holdCompare)
			{
				_holdCompare = false;
				UpdateStage();
			}
		}

		// ---------------------------------------------------------------- actions

		private void Reset_Click(object sender, RoutedEventArgs e)
		{
			if (_busy) return;
			CopySettings(new G2AdjustSettings());
		}

		private void PrevSettings_Click(object sender, RoutedEventArgs e)
		{
			if (_busy || _lastSaved == null) return;
			CopySettings(_lastSaved);
		}

		private void CopySettings(G2AdjustSettings from)
		{
			G2AdjustSettings c = from.Clone();
			_settings.Exposure = c.Exposure;
			_settings.Brightness = c.Brightness;
			_settings.Contrast = c.Contrast;
			_settings.Saturation = c.Saturation;
			_settings.Temperature = c.Temperature;
			_settings.Tint = c.Tint;
			_settings.Master = c.Master;
			_settings.Red = c.Red;
			_settings.Green = c.Green;
			_settings.Blue = c.Blue;
			ShowAllValues();
			UpdateTemplateState();
			RequestRender();
		}

		private async void Save_Click(object sender, RoutedEventArgs e)
		{
			if (_busy) return;
			string dest;
			try
			{
				dest = G2ImageAdjust.NextAdjustedName(_source);
			}
			catch (Exception ex)
			{
				StatusText.Text = "저장 위치를 정할 수 없습니다: " + ex.Message;
				return;
			}
			await SaveToAsync(dest, false, _addToSession);
		}

		private async void SaveAs_Click(object sender, RoutedEventArgs e)
		{
			if (_busy) return;
			string ext = Path.GetExtension(_source);
			var dialog = new Microsoft.Win32.SaveFileDialog
			{
				Title = "보정본 저장",
				InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(_source)),
				FileName = Path.GetFileName(G2ImageAdjust.NextAdjustedName(_source)),
				DefaultExt = ext,
				Filter = "원본과 같은 형식 (*" + ext + ")|*" + ext + "|JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png|TIFF (*.tif)|*.tif",
				OverwritePrompt = true,
				AddExtension = true
			};
			if (dialog.ShowDialog(this) != true)
			{
				return;
			}
			if (string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(_source), StringComparison.OrdinalIgnoreCase))
			{
				StatusText.Text = "원본과 다른 이름을 지정해 주세요. 원본은 덮어쓰지 않습니다.";
				return;
			}
			bool sameFolder = string.Equals(Path.GetDirectoryName(Path.GetFullPath(dialog.FileName)), Path.GetDirectoryName(Path.GetFullPath(_source)), StringComparison.OrdinalIgnoreCase);
			await SaveToAsync(dialog.FileName, true, _addToSession && sameFolder);
		}

		private async Task SaveToAsync(string dest, bool overwrite, bool addToSession)
		{
			G2AdjustSettings s = _settings.Clone();
			_saving = true;
			SetBusy(true, "저장하는 중… (원본 해상도)");
			try
			{
				await Task.Run(() => G2ImageAdjust.SaveAdjusted(_source, dest, s, overwrite));
				SavedFile = dest;
				_lastSaved = s;
				Log.Debug("G2 color adjust saved " + dest + " : " + s.Describe());
				string note = "";
				if (addToSession)
				{
					note = AddToSession(dest) ? " · 세션에 추가됨" : " · 세션 목록 갱신 실패(파일은 저장됨)";
				}
				StatusText.Text = "저장됨: " + Path.GetFileName(dest) + note + "\n" + s.Describe();
				StatusText.Foreground = (Brush)FindResource("G2Ok");
			}
			catch (Exception ex)
			{
				Log.Error("G2 color adjust save", ex);
				StatusText.Text = "저장하지 못했습니다. 원본은 그대로입니다.\n" + ex.GetBaseException().Message;
				StatusText.Foreground = (Brush)FindResource("G2Err");
			}
			finally
			{
				_saving = false;
				if (!_closed)
				{
					SetBusy(false, null);
				}
			}
		}

		// ---------------------------------------------------------------- templates

		private void LoadTemplates()
		{
			try
			{
				_store = new G2ColorTemplateStore(string.IsNullOrEmpty(TemplateStorePath) ? G2ColorTemplateStore.DefaultPath : TemplateStorePath);
				_store.Load();
				if (_store.QuarantinedFile != null)
				{
					StatusText.Text = "템플릿 파일을 읽을 수 없어 따로 보관했습니다: " + Path.GetFileName(_store.QuarantinedFile);
				}
			}
			catch (Exception ex)
			{
				Log.Error("G2 color templates load", ex);
				StatusText.Text = "템플릿을 불러오지 못했습니다: " + ex.GetBaseException().Message;
			}
			RefreshTemplateList(null);
		}

		private G2ColorTemplate SelectedTemplate => TemplateList.SelectedItem as G2ColorTemplate;

		private void RefreshTemplateList(string select)
		{
			_selectingTemplate = true;
			try
			{
				List<G2ColorTemplate> items = _store == null ? new List<G2ColorTemplate>() : _store.Items.ToList();
				TemplateList.ItemsSource = items;
				TemplateList.SelectedItem = select == null ? null : items.FirstOrDefault(t => string.Equals(t.Name, select, StringComparison.OrdinalIgnoreCase));
			}
			finally
			{
				_selectingTemplate = false;
			}
			int n = _store == null ? 0 : _store.Items.Count;
			TemplateCount.Text = n == 0 ? "" : n + "개";
			TemplateEmpty.Visibility = n == 0 ? Visibility.Visible : Visibility.Collapsed;
			TemplateList.Visibility = n == 0 ? Visibility.Collapsed : Visibility.Visible;
			UpdateTemplateButtons();
			UpdateTemplateState();
		}

		private void UpdateTemplateButtons()
		{
			bool has = SelectedTemplate != null;
			UpdateTemplateButton.IsEnabled = has;
			DeleteTemplateButton.IsEnabled = has;
			BatchButton.IsEnabled = has;
		}

		private void UpdateTemplateState()
		{
			if (TemplateState == null)
			{
				return;
			}
			G2ColorTemplate t = _store?.Find(_appliedTemplate);
			if (t == null)
			{
				TemplateState.Text = SelectedTemplate == null && (_store?.Items.Count ?? 0) > 0 ? "템플릿을 누르면 이 사진에 바로 적용됩니다." : "";
				TemplateState.Foreground = (Brush)FindResource("G2Sub");
				return;
			}
			bool same = t.Settings.SameAs(_settings);
			TemplateState.Text = same ? "적용됨: " + t.Name : "적용됨: " + t.Name + " · 이후 값 변경됨 (덮어쓰기로 저장 가능)";
			TemplateState.Foreground = (Brush)FindResource(same ? "G2Ok" : "G2Pr");
		}

		/// <summary>목록에서 템플릿을 고르면 바로 이 사진에 적용한다.</summary>
		private void TemplateList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateTemplateButtons();
			if (_selectingTemplate || _busy)
			{
				return;
			}
			G2ColorTemplate t = SelectedTemplate;
			if (t != null)
			{
				ApplyTemplate(t.Name);
			}
		}

		/// <summary>템플릿 값을 현재 사진에 적용(미리보기 갱신). 저장은 하지 않는다.</summary>
		public bool ApplyTemplate(string name)
		{
			G2ColorTemplate t = _store?.Find(name);
			if (t == null)
			{
				return false;
			}
			_appliedTemplate = t.Name;
			CopySettings(t.Settings);
			StatusText.Text = "템플릿 '" + t.Name + "' 적용 · 저장하려면 '새 파일로 저장'";
			StatusText.Foreground = (Brush)FindResource("G2Sub");
			return true;
		}

		private void TemplateList_DoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (_busy || SelectedTemplate == null)
			{
				return;
			}
			ShowNameRow(true, SelectedTemplate.Name);
		}

		private void NewTemplate_Click(object sender, RoutedEventArgs e)
		{
			if (_busy) return;
			ShowNameRow(false, SuggestName());
		}

		private string SuggestName()
		{
			for (int i = (_store?.Items.Count ?? 0) + 1; ; i++)
			{
				string n = "템플릿 " + i;
				if (_store?.Find(n) == null) return n;
			}
		}

		private void ShowNameRow(bool rename, string text)
		{
			_templateRenaming = rename;
			TemplateNameOk.Content = rename ? "이름 변경" : "저장";
			TemplateNameRow.Visibility = Visibility.Visible;
			TemplateButtons.Visibility = Visibility.Collapsed;
			TemplateNameBox.Text = text ?? "";
			TemplateNameBox.Focus();
			TemplateNameBox.SelectAll();
		}

		private void HideNameRow()
		{
			TemplateNameRow.Visibility = Visibility.Collapsed;
			TemplateButtons.Visibility = Visibility.Visible;
		}

		private void TemplateNameBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				TemplateNameOk_Click(sender, null);
				e.Handled = true;
			}
			else if (e.Key == Key.Escape)
			{
				HideNameRow();
				e.Handled = true;
			}
		}

		private void TemplateNameCancel_Click(object sender, RoutedEventArgs e) => HideNameRow();

		private void TemplateNameOk_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				G2ColorTemplate t;
				if (_templateRenaming)
				{
					string old = SelectedTemplate?.Name;
					t = _store.Rename(old, TemplateNameBox.Text);
					if (string.Equals(_appliedTemplate, old, StringComparison.OrdinalIgnoreCase))
					{
						_appliedTemplate = t.Name;
					}
					StatusText.Text = "템플릿 이름을 바꿨습니다: " + t.Name;
				}
				else
				{
					t = _store.Add(TemplateNameBox.Text, _settings);
					_appliedTemplate = t.Name;
					StatusText.Text = "템플릿 저장: " + t.Name + " · " + t.Summary;
				}
				StatusText.Foreground = (Brush)FindResource("G2Ok");
				HideNameRow();
				RefreshTemplateList(t.Name);
			}
			catch (Exception ex)
			{
				StatusText.Text = ex.GetBaseException().Message;
				StatusText.Foreground = (Brush)FindResource("G2Err");
				TemplateNameBox.Focus();
			}
		}

		private void UpdateTemplate_Click(object sender, RoutedEventArgs e)
		{
			G2ColorTemplate sel = SelectedTemplate;
			if (_busy || sel == null) return;
			if (System.Windows.MessageBox.Show(this, "템플릿 '" + sel.Name + "'을(를) 지금 보정값으로 바꿀까요?\n\n" + _settings.Describe(), "템플릿 덮어쓰기", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
			{
				return;
			}
			try
			{
				G2ColorTemplate t = _store.Update(sel.Name, _settings);
				_appliedTemplate = t.Name;
				StatusText.Text = "템플릿 덮어씀: " + t.Name;
				StatusText.Foreground = (Brush)FindResource("G2Ok");
				RefreshTemplateList(t.Name);
			}
			catch (Exception ex)
			{
				StatusText.Text = ex.GetBaseException().Message;
				StatusText.Foreground = (Brush)FindResource("G2Err");
			}
		}

		private void DeleteTemplate_Click(object sender, RoutedEventArgs e)
		{
			G2ColorTemplate sel = SelectedTemplate;
			if (_busy || sel == null) return;
			if (System.Windows.MessageBox.Show(this, "템플릿 '" + sel.Name + "'을(를) 삭제할까요?\n사진의 현재 보정값은 그대로 둡니다.", "템플릿 삭제", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
			{
				return;
			}
			try
			{
				_store.Remove(sel.Name);
				if (string.Equals(_appliedTemplate, sel.Name, StringComparison.OrdinalIgnoreCase))
				{
					_appliedTemplate = null;
				}
				StatusText.Text = "템플릿 삭제: " + sel.Name;
				StatusText.Foreground = (Brush)FindResource("G2Sub");
				RefreshTemplateList(null);
			}
			catch (Exception ex)
			{
				StatusText.Text = ex.GetBaseException().Message;
				StatusText.Foreground = (Brush)FindResource("G2Err");
			}
		}

		// ---------------------------------------------------------------- batch

		private sealed class Win32Owner : System.Windows.Forms.IWin32Window
		{
			public Win32Owner(IntPtr handle) { Handle = handle; }

			public IntPtr Handle { get; }
		}

		private async void Batch_Click(object sender, RoutedEventArgs e)
		{
			G2ColorTemplate t = SelectedTemplate;
			if (_busy || t == null)
			{
				StatusText.Text = "일괄 적용할 템플릿을 먼저 선택해 주세요.";
				return;
			}
			string folder;
			using (var dialog = new System.Windows.Forms.FolderBrowserDialog
			{
				Description = "템플릿 '" + t.Name + "'을(를) 적용할 사진 폴더를 고르세요. (하위 폴더 제외, 결과는 폴더\\_cc\\)",
				ShowNewFolderButton = false,
				SelectedPath = Path.GetDirectoryName(Path.GetFullPath(_source))
			})
			{
				var owner = new Win32Owner(new System.Windows.Interop.WindowInteropHelper(this).Handle);
				if (dialog.ShowDialog(owner) != System.Windows.Forms.DialogResult.OK)
				{
					return;
				}
				folder = dialog.SelectedPath;
			}
			await RunBatchAsync(folder, t.Name);
		}

		/// <summary>폴더의 모든 사진에 템플릿을 적용한다(결과: 폴더\_cc\). 검사에서도 직접 호출한다.</summary>
		public async Task<G2BatchResult> RunBatchAsync(string folder, string templateName)
		{
			G2ColorTemplate t = _store?.Find(templateName);
			if (t == null || _busy)
			{
				return null;
			}
			int count;
			try
			{
				count = G2ColorBatch.FindImages(folder).Count;
			}
			catch (Exception ex)
			{
				StatusText.Text = ex.GetBaseException().Message;
				StatusText.Foreground = (Brush)FindResource("G2Err");
				return null;
			}
			if (count == 0)
			{
				StatusText.Text = "이 폴더에는 보정할 사진(JPEG·PNG·TIFF·BMP)이 없습니다: " + folder;
				StatusText.Foreground = (Brush)FindResource("G2Err");
				return null;
			}
			if (ConfirmBatch && System.Windows.MessageBox.Show(this,
				"템플릿 '" + t.Name + "'을(를) " + count + "장에 적용합니다.\n\n" + t.Summary + "\n\n폴더: " + folder + "\n저장: " + Path.Combine(folder, G2ColorBatch.OutputFolderName) + "\n원본은 바꾸지 않습니다.",
				"일괄 적용", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
			{
				return null;
			}
			_batching = true;
			_batchCts = new CancellationTokenSource();
			SetBusy(true, "일괄 적용 중… 템플릿 '" + t.Name + "'");
			BusyProgress.IsIndeterminate = false;
			BusyProgress.Minimum = 0;
			BusyProgress.Maximum = count;
			BusyProgress.Value = 0;
			BusyDetail.Text = "";
			BatchStopButton.Visibility = Visibility.Visible;
			BatchStopButton.IsEnabled = true;
			var progress = new Progress<G2BatchProgress>(p =>
			{
				BusyProgress.Value = p.Index;
				BusyDetail.Text = p.File == null ? "" : (p.Index + 1) + " / " + p.Total + "  ·  " + p.File;
			});
			G2BatchResult result = null;
			try
			{
				G2AdjustSettings s = t.Settings.Clone();
				CancellationToken token = _batchCts.Token;
				result = await Task.Run(() => G2ColorBatch.Run(folder, s, t.Name, progress, token));
				LastBatch = result;
				string summary = "일괄 적용 " + (result.Cancelled ? "중지" : "완료") + ": " + result.Saved + " / " + result.Total + "장 저장" +
					(result.Errors.Count > 0 ? " · 실패 " + result.Errors.Count + "장" : "") + "\n" + result.OutputFolder;
				StatusText.Text = summary;
				StatusText.Foreground = (Brush)FindResource(result.Errors.Count > 0 || result.Cancelled ? "G2Pr" : "G2Ok");
				Log.Debug("G2 color batch " + folder + " template=" + t.Name + " saved=" + result.Saved + "/" + result.Total + " errors=" + result.Errors.Count);
			}
			catch (Exception ex)
			{
				Log.Error("G2 color batch", ex);
				StatusText.Text = "일괄 적용 실패: " + ex.GetBaseException().Message;
				StatusText.Foreground = (Brush)FindResource("G2Err");
			}
			finally
			{
				_batching = false;
				_batchCts.Dispose();
				_batchCts = null;
				BatchStopButton.Visibility = Visibility.Collapsed;
				BusyProgress.IsIndeterminate = true;
				BusyDetail.Text = "";
				if (!_closed)
				{
					SetBusy(false, null);
				}
			}
			if (result != null && ConfirmBatch && result.Saved > 0 && !_closed)
			{
				string text = StatusText.Text + (result.Errors.Count > 0 ? "\n\n실패:\n" + string.Join("\n", result.Errors.Take(8)) + (result.Errors.Count > 8 ? "\n…" : "") : "") + "\n\n결과 폴더를 열까요?";
				if (System.Windows.MessageBox.Show(this, text, "일괄 적용", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
				{
					try { System.Diagnostics.Process.Start("explorer.exe", "\"" + result.OutputFolder + "\""); } catch (Exception) { }
				}
			}
			return result;
		}

		private void BatchStop_Click(object sender, RoutedEventArgs e)
		{
			_batchCts?.Cancel();
			BatchStopButton.IsEnabled = false;
			BusyText.Text = "현재 사진을 저장한 뒤 멈추는 중…";
		}

		/// <summary>기존 색보정과 같은 방식으로 세션 목록에 추가하고 선택한다. 자동 내보내기(FileTransfered)는 호출하지 않는다.</summary>
		private static bool AddToSession(string file)
		{
			try
			{
				PhotoSession session = ServiceProvider.Settings?.DefaultSession;
				if (session == null)
				{
					return false;
				}
				FileItem item = session.GetNewFileItem(file);
				item.RemoveThumbs();
				session.Add(item);
				ServiceProvider.WindowsManager.ExecuteCommand("Select_Image", item);
				return true;
			}
			catch (Exception ex)
			{
				Log.Error("G2 color adjust add to session", ex);
				return false;
			}
		}
	}
}
