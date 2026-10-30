#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CameraControl.Core;
using Microsoft.Win32;

namespace CameraControl.windows;

public partial class RemoveBackgroundWnd : Window, IComponentConnector
{
	private WriteableBitmap _originalBitmap;

	private System.Windows.Media.Color _targetColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFDBE9FF");

	private string selectedFile;

	public string InitDirectory;

	public RemoveBackgroundWnd()
	{
		InitializeComponent();
		ResultImage.MouseDown += ResultImage_MouseDown;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		ThresholdSlider.Value = ServiceProvider.Settings.BGThresHold;
		SoftRangeSlider.Value = ServiceProvider.Settings.BGSoftRange;
		if (ServiceProvider.Settings.BGAlgorithm)
		{
			RadioSingle.IsChecked = true;
		}
		else
		{
			RadioDiff.IsChecked = true;
		}
		_targetColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(ServiceProvider.Settings.BGTargetColor);
	}

	private void Window_Closed(object sender, EventArgs e)
	{
		ServiceProvider.Settings.BGThresHold = (int)ThresholdSlider.Value;
		ServiceProvider.Settings.BGSoftRange = (int)SoftRangeSlider.Value;
		ServiceProvider.Settings.BGAlgorithm = ((RadioSingle.IsChecked == true) ? true : false);
		ServiceProvider.Settings.Save();
	}

	public async Task PrecessingOneImage(string filePath)
	{
		if (ThresholdSlider.Value == 0.0 || SoftRangeSlider.Value == 1.0)
		{
			ThresholdSlider.Value = ServiceProvider.Settings.BGThresHold;
			SoftRangeSlider.Value = ServiceProvider.Settings.BGSoftRange;
		}
		if (ServiceProvider.Settings.ModeBgPng || ServiceProvider.Settings.ModeBgJpeg)
		{
			await ProcessOneImage(filePath, (int)ThresholdSlider.Value, (int)SoftRangeSlider.Value, save: true);
		}
	}

	public async Task PrecessingTwoImages(string filePath)
	{
		if (ThresholdSlider.Value == 0.0 || SoftRangeSlider.Value == 1.0)
		{
			ThresholdSlider.Value = ServiceProvider.Settings.BGThresHold;
			SoftRangeSlider.Value = ServiceProvider.Settings.BGSoftRange;
		}
		if (ServiceProvider.Settings.ModeBgPng || ServiceProvider.Settings.ModeBgJpeg)
		{
			await ProcessTwoImages(filePath, (int)ThresholdSlider.Value, (int)SoftRangeSlider.Value, save: true);
		}
	}

	private async void ResultImage_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (_originalBitmap != null && RadioDiff.IsChecked != true)
		{
			System.Windows.Point p = e.GetPosition(ResultImage);
			int x = (int)(p.X * ((double)_originalBitmap.PixelWidth / ResultImage.ActualWidth));
			int y = (int)(p.Y * ((double)_originalBitmap.PixelHeight / ResultImage.ActualHeight));
			if (x >= 0 && x < _originalBitmap.PixelWidth && y >= 0 && y < _originalBitmap.PixelHeight)
			{
				byte[] pixel = new byte[4];
				_originalBitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
				_targetColor = System.Windows.Media.Color.FromRgb(pixel[2], pixel[1], pixel[0]);
				TargetColor.Text = $"* 선택된 배경색: RGB({_targetColor.R}, {_targetColor.G}, {_targetColor.B})  - 값:({_targetColor})";
				await ProcessOneImage(selectedFile, (int)ThresholdSlider.Value, (int)SoftRangeSlider.Value, save: false);
			}
		}
	}

	private async void LoadImages_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			InitialDirectory = InitDirectory,
			Multiselect = false,
			Filter = "Image Files|*.jpg",
			Title = "변경하고 싶은 이미지 선택하세요."
		};
		if (openFileDialog.ShowDialog() == true)
		{
			selectedFile = openFileDialog.FileName;
			BitmapImage imgOn = LoadBitmap(selectedFile);
			_originalBitmap = new WriteableBitmap(new FormatConvertedBitmap(imgOn, PixelFormats.Bgra32, null, 0.0));
			_originalBitmap.Freeze();
			ResultImage.Source = _originalBitmap;
			if (RadioSingle.IsChecked == true)
			{
				await ProcessOneImage(selectedFile, (int)ThresholdSlider.Value, (int)SoftRangeSlider.Value, save: false);
			}
			else
			{
				await ProcessTwoImages(selectedFile, (int)ThresholdSlider.Value, (int)SoftRangeSlider.Value, save: false);
			}
		}
	}

	private async void Algorithm_Checked(object sender, RoutedEventArgs e)
	{
		if (GuideText1 != null)
		{
			if (RadioSingle.IsChecked == true)
			{
				GuideText1.Text = " * 단일 모드: 제거할 바탕색 이미지를 클릭하세요.";
			}
			else
			{
				GuideText1.Text = " * 차분 모드: 조명 On/Off 2장의 사진이 필요합니다.";
			}
			ServiceProvider.Settings.BGAlgorithm = RadioSingle.IsChecked.Value;
			await RefreshDisplay();
		}
	}

	private async void SubAlgorithm_Checked(object sender, RoutedEventArgs e)
	{
		if (GuideText2 != null)
		{
			if (RadioGeneral.IsChecked == true)
			{
				GuideText2.Text = "* 일반 의류: 외곽 배경만 지우고 옷 내부의 비슷한 색상은 보호합니다.";
			}
			else
			{
				GuideText2.Text = "* 씨스루 의류: 옷 전체에서 배경색을 찾아 투명하게 만듭니다. (레이스 등)";
			}
			await RefreshDisplay();
		}
	}

	private async Task RefreshDisplay()
	{
		if (_originalBitmap != null)
		{
			int threshold = (int)ThresholdSlider.Value;
			int softRange = (int)SoftRangeSlider.Value;
			if (RadioSingle.IsChecked == true)
			{
				await ProcessOneImage(selectedFile, threshold, softRange, save: false);
			}
			else
			{
				await ProcessTwoImages(selectedFile, threshold, softRange, save: false);
			}
		}
	}

	private void BackgroundCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (DisplayBorder != null)
		{
			ComboBoxItem selectedItem = (ComboBoxItem)BackgroundCombo.SelectedItem;
			switch (selectedItem.Content.ToString())
			{
			case "체크보드 (투명)":
				DisplayBorder.Background = ((VisualBrush)Resources["CheckerboardBrush"]) ?? CreateCheckerboardBrush();
				break;
			case "흰색 (White)":
				DisplayBorder.Background = System.Windows.Media.Brushes.White;
				break;
			case "검은색 (Black)":
				DisplayBorder.Background = System.Windows.Media.Brushes.Black;
				break;
			case "회색 (Gray)":
				DisplayBorder.Background = System.Windows.Media.Brushes.Gray;
				break;
			case "베이지 (Beige)":
				DisplayBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 220));
				break;
			}
		}
	}

	private VisualBrush CreateCheckerboardBrush()
	{
		VisualBrush vb = new VisualBrush();
		vb.TileMode = TileMode.Tile;
		vb.Viewport = new Rect(0.0, 0.0, 20.0, 20.0);
		vb.ViewportUnits = BrushMappingMode.Absolute;
		Grid grid = new Grid
		{
			Width = 20.0,
			Height = 20.0
		};
		grid.Children.Add(new System.Windows.Shapes.Rectangle
		{
			Fill = System.Windows.Media.Brushes.White,
			Width = 20.0,
			Height = 20.0
		});
		grid.Children.Add(new System.Windows.Shapes.Rectangle
		{
			Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 238, 238)),
			Width = 10.0,
			Height = 10.0,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left
		});
		grid.Children.Add(new System.Windows.Shapes.Rectangle
		{
			Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 238, 238)),
			Width = 10.0,
			Height = 10.0,
			VerticalAlignment = VerticalAlignment.Bottom,
			HorizontalAlignment = HorizontalAlignment.Right
		});
		vb.Visual = grid;
		return vb;
	}

	private async void Slider_DragCompleted(object sender, DragCompletedEventArgs e)
	{
		if (!string.IsNullOrEmpty(selectedFile))
		{
			if (RadioSingle.IsChecked == true)
			{
				await ProcessOneImage(selectedFile, (int)ThresholdSlider.Value, (int)SoftRangeSlider.Value, save: false);
			}
			else
			{
				await ProcessTwoImages(selectedFile, (int)ThresholdSlider.Value, (int)SoftRangeSlider.Value, save: false);
			}
		}
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		if (ResultImage.Source is BitmapSource bitmapSource1)
		{
			SaveToPng(bitmapSource1, selectedFile);
		}
		System.Windows.Media.Brush currentBrush = Dispatcher.Invoke(() => DisplayBorder.Background);
		if (currentBrush is VisualBrush)
		{
			currentBrush = System.Windows.Media.Brushes.White;
		}
		if (ResultImage.Source is BitmapSource bitmapSource2)
		{
			SaveWithBackgroundAsJpg(bitmapSource2, currentBrush, selectedFile);
		}
		MessageBox.Show("이미지 저장이 완료되었습니다.");
	}

	private async void BatchProcess_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			InitialDirectory = InitDirectory,
			Multiselect = true,
			Filter = "Image Files|*.jpg",
			Title = "변경하고 싶은 다수 이미지들을 선택하세요."
		};
		if (openFileDialog.ShowDialog() == true)
		{
			string[] selectedFiles = openFileDialog.FileNames;
			int currentThreshold = (int)ThresholdSlider.Value;
			int currentSoftRange = (int)SoftRangeSlider.Value;
			await ProcessBatch(selectedFiles, currentThreshold, currentSoftRange);
		}
	}

	private void SetBusyState(bool isBusy, string message = "처리 중...")
	{
		if (isBusy)
		{
			LoadingOverlay.Visibility = Visibility.Visible;
			OverlayText.Text = message;
			StatusText.Text = "▶ " + message;
			StatusText.Foreground = System.Windows.Media.Brushes.DeepSkyBlue;
			StatusText.FontWeight = FontWeights.Bold;
			Cursor = Cursors.Wait;
		}
		else
		{
			LoadingOverlay.Visibility = Visibility.Collapsed;
			StatusText.Text = "✔ 처리 완료";
			StatusText.Foreground = System.Windows.Media.Brushes.Green;
			StatusText.FontWeight = FontWeights.Normal;
			Cursor = Cursors.Arrow;
		}
	}

	private void SaveToPng(BitmapSource bitmap, string filePath)
	{
		if (bitmap == null)
		{
			return;
		}
		string newDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath), "Result");
		string filename = System.IO.Path.GetFileNameWithoutExtension(filePath) + ".png";
		string newfilePath = System.IO.Path.Combine(newDir, filename);
		if (!Directory.Exists(newDir))
		{
			Directory.CreateDirectory(newDir);
		}
		PngBitmapEncoder encoder = new PngBitmapEncoder();
		encoder.Frames.Add(BitmapFrame.Create(bitmap));
		using FileStream stream = new FileStream(newfilePath, FileMode.Create);
		encoder.Save(stream);
	}

	private void SaveWithBackgroundAsJpg(BitmapSource foreground, System.Windows.Media.Brush backgroundBrush, string filePath)
	{
		if (foreground == null)
		{
			return;
		}
		string newDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filePath), "Result");
		string filename = System.IO.Path.GetFileNameWithoutExtension(filePath) + "_new.jpeg";
		string newfilePath = System.IO.Path.Combine(newDir, filename);
		if (!Directory.Exists(newDir))
		{
			Directory.CreateDirectory(newDir);
		}
		double widthInDiu = (double)foreground.PixelWidth * 96.0 / foreground.DpiX;
		double heightInDiu = (double)foreground.PixelHeight * 96.0 / foreground.DpiY;
		DrawingVisual drawingVisual = new DrawingVisual();
		RenderOptions.SetBitmapScalingMode(drawingVisual, BitmapScalingMode.HighQuality);
		RenderOptions.SetEdgeMode(drawingVisual, EdgeMode.Aliased);
		using (DrawingContext dc = drawingVisual.RenderOpen())
		{
			Rect rect = new Rect(0.0, 0.0, widthInDiu, heightInDiu);
			dc.DrawRectangle(backgroundBrush, null, rect);
			dc.DrawImage(foreground, rect);
		}
		RenderTargetBitmap renderTarget = new RenderTargetBitmap(foreground.PixelWidth, foreground.PixelHeight, foreground.DpiX, foreground.DpiY, PixelFormats.Pbgra32);
		renderTarget.Render(drawingVisual);
		JpegBitmapEncoder encoder = new JpegBitmapEncoder
		{
			QualityLevel = 95
		};
		encoder.Frames.Add(BitmapFrame.Create(renderTarget));
		using FileStream stream = new FileStream(newfilePath, FileMode.Create);
		encoder.Save(stream);
	}

	private BitmapImage LoadBitmap(string path)
	{
		using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
		BitmapDecoder decoder = BitmapDecoder.Create(fs, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
		BitmapFrame frame = decoder.Frames[0];
		BitmapMetadata metadata = frame.Metadata as BitmapMetadata;
		BitmapImage bi = new BitmapImage();
		bi.BeginInit();
		bi.UriSource = new Uri(path);
		bi.CacheOption = BitmapCacheOption.OnLoad;
		if (metadata != null && metadata.ContainsQuery("/app1/ifd/{ushort=274}"))
		{
			object orientation = metadata.GetQuery("/app1/ifd/{ushort=274}");
			if (orientation != null)
			{
				switch ((ushort)orientation)
				{
				case 6:
					bi.Rotation = Rotation.Rotate90;
					break;
				case 3:
					bi.Rotation = Rotation.Rotate180;
					break;
				case 8:
					bi.Rotation = Rotation.Rotate270;
					break;
				}
			}
		}
		bi.EndInit();
		bi.Freeze();
		return bi;
	}

	private async Task ProcessOneImage(string onPath, int threshold, int softRange, bool save)
	{
		BitmapImage imgOn = LoadBitmap(onPath);
		bool isSeeThrough = RadioSeeThrough.IsChecked == true;
		_ = _targetColor;
		SetBusyState(isBusy: true);
		await Task.Run(() =>
		{
			WriteableBitmap result = null;
			if (isSeeThrough)
			{
				result = ProcessOneImageAlgorithm_SeeThrough(imgOn, _targetColor, threshold, softRange);
			}
			else
			{
				result = ProcessOneImageAlgorithm_BoundaryOnly(imgOn, _targetColor, threshold, softRange);
			}
			Dispatcher.Invoke(() =>
			{
				ResultImage.Source = result;
				SetBusyState(isBusy: false);
			});
			if (save && result != null)
			{
				SaveToPng(result, onPath);
			}
		});
	}

	private async Task ProcessTwoImages(string onPath, int threshold, int softRange, bool save)
	{
		bool isSeeThrough = await Dispatcher.InvokeAsync(() => RadioSeeThrough.IsChecked == true);
		System.Windows.Media.Brush bgBrush = await Dispatcher.InvokeAsync(() =>
		{
			System.Windows.Media.Brush background = DisplayBorder.Background;
			return (background is VisualBrush) ? System.Windows.Media.Brushes.White : background;
		});
		string folder = System.IO.Path.GetDirectoryName(onPath);
		string fileNameOnly = System.IO.Path.GetFileNameWithoutExtension(onPath);
		string offPath = System.IO.Path.Combine(folder, fileNameOnly + "_BG.jpeg");
		SetBusyState(isBusy: true);
		if (!File.Exists(offPath))
		{
			StatusText.Text = "배경 이미지(_BG)를 찾을 수 없습니다.";
			return;
		}
		BitmapImage imgOn = LoadBitmap(onPath);
		BitmapImage imgOff = LoadBitmap(offPath);
		await Task.Run(() =>
		{
			WriteableBitmap result = null;
			if (isSeeThrough)
			{
				result = ProcessTwoImageAlgorithm_SeeThrough(imgOn, imgOff, threshold, softRange);
			}
			else
			{
				result = ProcessTwoImageAlgorithm_BoundaryOnly(imgOn, imgOff, threshold, softRange);
			}
			Dispatcher.Invoke(() =>
			{
				ResultImage.Source = result;
				SetBusyState(isBusy: false);
			});
			if (save && result != null)
			{
				if (ServiceProvider.Settings.ModeBgPng)
				{
					SaveToPng(result, onPath);
				}
				if (ServiceProvider.Settings.ModeBgJpeg)
				{
					SaveWithBackgroundAsJpg(result, bgBrush, onPath);
				}
			}
		});
	}

	private async Task ProcessBatch(string[] selectedFiles, int threshold, int softRange)
	{
		bool isSeeThrough = RadioSeeThrough.IsChecked == true;
		bool isSingleMode = RadioSingle.IsChecked == true;
		System.Windows.Media.Color targetColor = _targetColor;
		System.Windows.Media.Brush bgBrush = DisplayBorder.Background;
		if (bgBrush is VisualBrush)
		{
			bgBrush = System.Windows.Media.Brushes.White;
		}
		BatchProgress.Maximum = selectedFiles.Length;
		BatchProgress.Value = 0.0;
		BatchProgress.Visibility = Visibility.Visible;
		SetBusyState(isBusy: true, "일괄 처리 시작...");
		await Task.Run(() =>
		{
			string[] array = selectedFiles;
			foreach (string text in array)
			{
				try
				{
					string fileNameOnly = System.IO.Path.GetFileNameWithoutExtension(text);
					Dispatcher.Invoke(() => StatusText.Text = $"⏳ 처리 중: {fileNameOnly} ({BatchProgress.Value + 1.0}/{selectedFiles.Length})");
					WriteableBitmap result = null;
					if (isSingleMode)
					{
						BitmapImage srcImage = LoadBitmap(text);
						if (isSeeThrough)
						{
							result = ProcessOneImageAlgorithm_SeeThrough(srcImage, targetColor, threshold, softRange);
						}
						else
						{
							result = ProcessOneImageAlgorithm_BoundaryOnly(srcImage, targetColor, threshold, softRange);
						}
						goto IL_0179;
					}
					string directoryName = System.IO.Path.GetDirectoryName(text);
					string path = System.IO.Path.Combine(directoryName, fileNameOnly + "_BG.jpeg");
					if (File.Exists(path))
					{
						BitmapImage imgOn = LoadBitmap(text);
						BitmapImage imgOff = LoadBitmap(path);
						if (isSeeThrough)
						{
							result = ProcessTwoImageAlgorithm_SeeThrough(imgOn, imgOff, threshold, softRange);
						}
						else
						{
							result = ProcessTwoImageAlgorithm_BoundaryOnly(imgOn, imgOff, threshold, softRange);
						}
						goto IL_0179;
					}
					goto end_IL_0015;
					IL_0179:
					if (result != null)
					{
						Dispatcher.Invoke(() => ResultImage.Source = result);
						if (ServiceProvider.Settings.ModeBgPng)
						{
							SaveToPng(result, text);
						}
						if (ServiceProvider.Settings.ModeBgJpeg)
						{
							SaveWithBackgroundAsJpg(result, bgBrush, text);
						}
					}
					Dispatcher.Invoke(() => BatchProgress.Value++);
					end_IL_0015:;
				}
				catch (Exception ex)
				{
					Debug.WriteLine("파일 처리 오류 (" + text + "): " + ex.Message);
				}
			}
		});
		SetBusyState(isBusy: false);
		BatchProgress.Visibility = Visibility.Collapsed;
		MessageBox.Show($"{selectedFiles.Length}개 파일 세트에 대한 처리가 완료되었습니다.");
	}

	private unsafe WriteableBitmap ProcessOneImageAlgorithm_SeeThrough(BitmapSource srcImage, System.Windows.Media.Color targetColor, float threshold, float softRange)
	{
		int width = srcImage.PixelWidth;
		int height = srcImage.PixelHeight;
		int stride = width * 4;
		BitmapSource bmp = ((srcImage.Format == PixelFormats.Bgra32) ? srcImage : new FormatConvertedBitmap(srcImage, PixelFormats.Bgra32, null, 0.0));
		WriteableBitmap result = new WriteableBitmap(width, height, srcImage.DpiX, srcImage.DpiY, PixelFormats.Bgra32, null);
		byte[] pixels = new byte[height * stride];
		bmp.CopyPixels(pixels, stride, 0);
		result.Lock();
		fixed (byte* pSrc = pixels)
		{
			byte* localSrc = pSrc;
			byte* pBackBuffer = (byte*)(void*)result.BackBuffer;
			float invSoftRange = ((softRange <= 0f) ? 1f : (1f / softRange));
			byte targetR = targetColor.R;
			byte targetG = targetColor.G;
			byte targetB = targetColor.B;
			Parallel.For(0, height, (int y) =>
			{
				int num = y * stride;
				byte* ptr = localSrc + num;
				byte* ptr2 = pBackBuffer + num;
				for (int i = 0; i < width; i++)
				{
					int num2 = i << 2;
					float num3 = ptr[num2] - targetB;
					float num4 = ptr[num2 + 1] - targetG;
					float num5 = ptr[num2 + 2] - targetR;
					float num6 = (float)Math.Sqrt(num5 * num5 + num4 * num4 + num3 * num3);
					float num7 = (num6 - threshold) * invSoftRange * 255f;
					byte b = (byte)((!(num7 < 0f)) ? ((num7 > 255f) ? byte.MaxValue : ((byte)num7)) : 0);
					if (b > 0)
					{
						ptr2[num2] = (byte)(ptr[num2] * b / 255);
						ptr2[num2 + 1] = (byte)(ptr[num2 + 1] * b / 255);
						ptr2[num2 + 2] = (byte)(ptr[num2 + 2] * b / 255);
						ptr2[num2 + 3] = b;
					}
					else
					{
						*(int*)(ptr2 + num2) = 0;
					}
				}
			});
		}
		result.AddDirtyRect(new Int32Rect(0, 0, width, height));
		result.Unlock();
		result.Freeze();
		return result;
	}

	private unsafe WriteableBitmap ProcessOneImageAlgorithm_BoundaryOnly(BitmapSource srcImage, System.Windows.Media.Color targetColor, float threshold, float softRange)
	{
		int width = srcImage.PixelWidth;
		int height = srcImage.PixelHeight;
		int stride = width * 4;
		BitmapSource bmp = ((srcImage.Format == PixelFormats.Bgra32) ? srcImage : new FormatConvertedBitmap(srcImage, PixelFormats.Bgra32, null, 0.0));
		byte[] srcPixels = new byte[height * stride];
		bmp.CopyPixels(srcPixels, stride, 0);
		byte[] mask = new byte[width * height];
		DetermineBackgroundArea(srcPixels, mask, width, height, targetColor, threshold);
		WriteableBitmap result = new WriteableBitmap(width, height, srcImage.DpiX, srcImage.DpiY, PixelFormats.Bgra32, null);
		result.Lock();
		fixed (byte* pSrc = srcPixels)
		{
			fixed (byte* pMask = mask)
			{
				byte* localSrc = pSrc;
				byte* localMask = pMask;
				byte* pBackBuffer = (byte*)(void*)result.BackBuffer;
				float invSoftRange = ((softRange <= 0f) ? 1f : (1f / softRange));
				Parallel.For(0, height, (int y) =>
				{
					int num = y * stride;
					int num2 = y * width;
					byte* ptr = localSrc + num;
					byte* ptr2 = pBackBuffer + num;
					for (int i = 0; i < width; i++)
					{
						int num3 = i << 2;
						int num4 = num2 + i;
						if (localMask[num4] == 0)
						{
							ptr2[num3] = ptr[num3];
							ptr2[num3 + 1] = ptr[num3 + 1];
							ptr2[num3 + 2] = ptr[num3 + 2];
							ptr2[num3 + 3] = byte.MaxValue;
						}
						else
						{
							float num5 = ptr[num3] - targetColor.B;
							float num6 = ptr[num3 + 1] - targetColor.G;
							float num7 = ptr[num3 + 2] - targetColor.R;
							float num8 = (float)Math.Sqrt(num7 * num7 + num6 * num6 + num5 * num5);
							float num9 = (num8 - threshold) * invSoftRange * 255f;
							byte b = (byte)((!(num9 < 0f)) ? ((num9 > 255f) ? byte.MaxValue : ((byte)num9)) : 0);
							if (b > 0)
							{
								ptr2[num3] = (byte)(ptr[num3] * b / 255);
								ptr2[num3 + 1] = (byte)(ptr[num3 + 1] * b / 255);
								ptr2[num3 + 2] = (byte)(ptr[num3 + 2] * b / 255);
								ptr2[num3 + 3] = b;
							}
							else
							{
								*(int*)(ptr2 + num3) = 0;
							}
						}
					}
				});
			}
		}
		result.AddDirtyRect(new Int32Rect(0, 0, width, height));
		result.Unlock();
		result.Freeze();
		return result;
	}

	private void DetermineBackgroundArea(byte[] pixels, byte[] mask, int width, int height, System.Windows.Media.Color target, float threshold)
	{
		Queue<System.Drawing.Point> queue = new Queue<System.Drawing.Point>();
		queue.Enqueue(new System.Drawing.Point(0, 0));
		mask[0] = 1;
		while (queue.Count > 0)
		{
			System.Drawing.Point p = queue.Dequeue();
			int[] dx = new int[4] { 0, 0, 1, -1 };
			int[] dy = new int[4] { 1, -1, 0, 0 };
			for (int i = 0; i < 4; i++)
			{
				int nx = p.X + dx[i];
				int ny = p.Y + dy[i];
				if (nx >= 0 && nx < width && ny >= 0 && ny < height && mask[ny * width + nx] == 0)
				{
					int pos = (ny * width + nx) * 4;
					float dist = (float)Math.Sqrt(Math.Pow(pixels[pos + 2] - target.R, 2.0) + Math.Pow(pixels[pos + 1] - target.G, 2.0) + Math.Pow(pixels[pos] - target.B, 2.0));
					if (dist < threshold + 20f)
					{
						mask[ny * width + nx] = 1;
						queue.Enqueue(new System.Drawing.Point(nx, ny));
					}
				}
			}
		}
	}

	private unsafe WriteableBitmap ProcessTwoImageAlgorithm_SeeThrough(BitmapSource imgOn, BitmapSource imgOff, float threshold, float softRange)
	{
		int width = imgOn.PixelWidth;
		int height = imgOn.PixelHeight;
		int stride = width * 4;
		WriteableBitmap result = new WriteableBitmap(width, height, imgOn.DpiX, imgOn.DpiY, PixelFormats.Bgra32, null);
		BitmapSource srcOn = ((imgOn.Format == PixelFormats.Bgra32) ? imgOn : new FormatConvertedBitmap(imgOn, PixelFormats.Bgra32, null, 0.0));
		BitmapSource srcOff = ((imgOff.Format == PixelFormats.Bgra32) ? imgOff : new FormatConvertedBitmap(imgOff, PixelFormats.Bgra32, null, 0.0));
		result.Lock();
		byte[] pixelsOn = new byte[height * stride];
		byte[] pixelsOff = new byte[height * stride];
		srcOn.CopyPixels(pixelsOn, stride, 0);
		srcOff.CopyPixels(pixelsOff, stride, 0);
		fixed (byte* pOn = pixelsOn)
		{
			fixed (byte* pOff = pixelsOff)
			{
				byte* localOn = pOn;
				byte* localOff = pOff;
				byte* pBackBuffer = (byte*)(void*)result.BackBuffer;
				float invSoftRange = ((softRange <= 0f) ? 1f : (1f / softRange));
				Parallel.For(0, height, (int y) =>
				{
					int num = y * stride;
					byte* ptr = localOn + num;
					byte* ptr2 = localOff + num;
					byte* ptr3 = pBackBuffer + num;
					for (int i = 0; i < width; i++)
					{
						int num2 = i << 2;
						float value = ptr[num2] - ptr2[num2];
						float value2 = ptr[num2 + 1] - ptr2[num2 + 1];
						float value3 = ptr[num2 + 2] - ptr2[num2 + 2];
						float num3 = Math.Abs(value) * 0.11f + Math.Abs(value2) * 0.59f + Math.Abs(value3) * 0.3f;
						float num4 = (num3 - threshold) * invSoftRange * 255f;
						float num5 = 255f - num4;
						if (num5 < 50f)
						{
							num5 = 0f;
						}
						byte b = (byte)((!(num5 < 0f)) ? ((num5 > 255f) ? byte.MaxValue : ((byte)num5)) : 0);
						if (b > 0)
						{
							ptr3[num2] = (byte)(ptr[num2] * b / 255);
							ptr3[num2 + 1] = (byte)(ptr[num2 + 1] * b / 255);
							ptr3[num2 + 2] = (byte)(ptr[num2 + 2] * b / 255);
							ptr3[num2 + 3] = b;
						}
						else
						{
							*(int*)(ptr3 + num2) = 0;
						}
					}
				});
			}
		}
		result.AddDirtyRect(new Int32Rect(0, 0, width, height));
		result.Unlock();
		result.Freeze();
		return result;
	}

	private unsafe WriteableBitmap ProcessTwoImageAlgorithm_BoundaryOnly(BitmapSource imgOn, BitmapSource imgOff, float threshold, float softRange)
	{
		int width = imgOn.PixelWidth;
		int height = imgOn.PixelHeight;
		int stride = width * 4;
		BitmapSource srcOn = ((imgOn.Format == PixelFormats.Bgra32) ? imgOn : new FormatConvertedBitmap(imgOn, PixelFormats.Bgra32, null, 0.0));
		BitmapSource srcOff = ((imgOff.Format == PixelFormats.Bgra32) ? imgOff : new FormatConvertedBitmap(imgOff, PixelFormats.Bgra32, null, 0.0));
		byte[] pixelsOn = new byte[height * stride];
		byte[] pixelsOff = new byte[height * stride];
		srcOn.CopyPixels(pixelsOn, stride, 0);
		srcOff.CopyPixels(pixelsOff, stride, 0);
		byte[] mask = new byte[width * height];
		DetermineTwoImageBackgroundArea(pixelsOn, pixelsOff, mask, width, height, stride, threshold);
		WriteableBitmap result = new WriteableBitmap(width, height, imgOn.DpiX, imgOn.DpiY, PixelFormats.Bgra32, null);
		result.Lock();
		fixed (byte* pOn = pixelsOn)
		{
			fixed (byte* pOff = pixelsOff)
			{
				fixed (byte* pMask = mask)
				{
					byte* localOn = pOn;
					byte* localOff = pOff;
					byte* localMask = pMask;
					byte* pBackBuffer = (byte*)(void*)result.BackBuffer;
					float invSoftRange = ((softRange <= 0f) ? 1f : (1f / softRange));
					Parallel.For(0, height, (int y) =>
					{
						int num = y * stride;
						int num2 = y * width;
						byte* ptr = localOn + num;
						byte* ptr2 = localOff + num;
						byte* ptr3 = pBackBuffer + num;
						for (int i = 0; i < width; i++)
						{
							int num3 = i << 2;
							int num4 = num2 + i;
							if (localMask[num4] == 0)
							{
								ptr3[num3] = ptr[num3];
								ptr3[num3 + 1] = ptr[num3 + 1];
								ptr3[num3 + 2] = ptr[num3 + 2];
								ptr3[num3 + 3] = byte.MaxValue;
							}
							else
							{
								float value = ptr[num3] - ptr2[num3];
								float value2 = ptr[num3 + 1] - ptr2[num3 + 1];
								float value3 = ptr[num3 + 2] - ptr2[num3 + 2];
								float num5 = Math.Abs(value) * 0.11f + Math.Abs(value2) * 0.59f + Math.Abs(value3) * 0.3f;
								float num6 = (num5 - threshold) * invSoftRange * 255f;
								float num7 = 255f - num6;
								if (num7 < 50f)
								{
									num7 = 0f;
								}
								byte b = (byte)((!(num7 < 0f)) ? ((num7 > 255f) ? byte.MaxValue : ((byte)num7)) : 0);
								if (b > 0)
								{
									ptr3[num3] = (byte)(ptr[num3] * b / 255);
									ptr3[num3 + 1] = (byte)(ptr[num3 + 1] * b / 255);
									ptr3[num3 + 2] = (byte)(ptr[num3 + 2] * b / 255);
									ptr3[num3 + 3] = b;
								}
								else
								{
									*(int*)(ptr3 + num3) = 0;
								}
							}
						}
					});
				}
			}
		}
		result.AddDirtyRect(new Int32Rect(0, 0, width, height));
		result.Unlock();
		result.Freeze();
		return result;
	}

	private void DetermineTwoImageBackgroundArea(byte[] on, byte[] off, byte[] mask, int width, int height, int stride, float threshold)
	{
		Queue<System.Drawing.Point> queue = new Queue<System.Drawing.Point>();
		for (int x = 0; x < width; x += 10)
		{
			queue.Enqueue(new System.Drawing.Point(x, 0));
			queue.Enqueue(new System.Drawing.Point(x, height - 1));
		}
		for (int y = 0; y < height; y += 10)
		{
			queue.Enqueue(new System.Drawing.Point(0, y));
			queue.Enqueue(new System.Drawing.Point(width - 1, y));
		}
		while (queue.Count > 0)
		{
			System.Drawing.Point p = queue.Dequeue();
			if (p.X < 0 || p.X >= width || p.Y < 0 || p.Y >= height || mask[p.Y * width + p.X] == 1)
			{
				continue;
			}
			int pos = p.Y * stride + p.X * 4;
			float d0 = on[pos] - off[pos];
			float d1 = on[pos + 1] - off[pos + 1];
			float d2 = on[pos + 2] - off[pos + 2];
			float diff = Math.Abs(d0) * 0.11f + Math.Abs(d1) * 0.59f + Math.Abs(d2) * 0.3f;
			if (diff < threshold + 15f)
			{
				mask[p.Y * width + p.X] = 1;
				if (p.X + 1 < width && mask[p.Y * width + p.X + 1] == 0)
				{
					queue.Enqueue(new System.Drawing.Point(p.X + 1, p.Y));
				}
				if (p.X - 1 >= 0 && mask[p.Y * width + p.X - 1] == 0)
				{
					queue.Enqueue(new System.Drawing.Point(p.X - 1, p.Y));
				}
				if (p.Y + 1 < height && mask[(p.Y + 1) * width + p.X] == 0)
				{
					queue.Enqueue(new System.Drawing.Point(p.X, p.Y + 1));
				}
				if (p.Y - 1 >= 0 && mask[(p.Y - 1) * width + p.X] == 0)
				{
					queue.Enqueue(new System.Drawing.Point(p.X, p.Y - 1));
				}
			}
		}
	}
}
