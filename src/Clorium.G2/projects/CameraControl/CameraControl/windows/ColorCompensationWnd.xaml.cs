using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class ColorCompensationWnd : Window, IComponentConnector
{
	public string pathfilename = "";

	private bool _isSliderBusy = false;

	private CancellationTokenSource cts;

	private ColorCompensationViewModel ViewModel => (ColorCompensationViewModel)DataContext;

	public ColorCompensationWnd()
	{
		InitializeComponent();
		Loaded += Window_Loaded;
		Closing += (object s, CancelEventArgs e) =>
		{
			ViewModel?.SaveCheckCondition();
		};
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		DataContext = new ColorCompensationViewModel(pathfilename);
		ViewModel.LoadCheckCondition();
		UpdateTextBlockValue(ViewModel.filters);
		UpdateTrkBarValue(ViewModel.filters);
	}

	private void UpdateTrkBarValue(FilterSettings current)
	{
		trkBrightness.Value = current.Brightness;
		trkContrast.Value = current.Contrast;
		trkGamma.Value = current.Gamma;
		trkSharpness.Value = current.Sharpness;
		trkSaturation.Value = current.Saturation;
		trkHue.Value = current.Hue;
	}

	private void UpdateTextBlockValue(FilterSettings current)
	{
		trkBrightness.Value = current.Brightness;
		trkContrast.Value = current.Contrast;
		trkGamma.Value = current.Gamma;
		trkSharpness.Value = current.Sharpness;
		trkSaturation.Value = current.Saturation;
		trkHue.Value = current.Hue;
	}

	private FilterSettings UpdateFiltersFromTrkBar(FilterSettings current)
	{
		current = new FilterSettings
		{
			Brightness = (int)trkBrightness.Value,
			Contrast = (int)trkContrast.Value,
			Gamma = (int)trkGamma.Value,
			Sharpness = (int)trkSharpness.Value,
			Saturation = (int)trkSaturation.Value,
			Hue = (int)trkHue.Value
		};
		return current;
	}

	private FilterSettings ResetFilterSettings(FilterSettings current)
	{
		ViewModel.history.Clear();
		current = new FilterSettings
		{
			Brightness = 0,
			Contrast = 100,
			Gamma = 100,
			Sharpness = 100,
			Saturation = 100,
			Hue = 0
		};
		return current;
	}

	private async void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (ViewModel?.SourceImage == null || _isSliderBusy || !(await PreventAccumulativeEvent()))
		{
			return;
		}
		try
		{
			_isSliderBusy = true;
			ViewModel.filters = UpdateFiltersFromTrkBar(ViewModel.filters);
			UpdateTextBlockValue(ViewModel.filters);
			ColorCompensationViewModel vm = ViewModel;
			FilterSettings filters = ViewModel.filters;
			WriteableBitmap orgImage = ViewModel.OrgsourceImage;
			int width = orgImage.PixelWidth;
			int height = orgImage.PixelHeight;
			_ = orgImage.DpiX;
			_ = orgImage.DpiY;
			int stride = width * 4;
			byte[] rawPixels = new byte[stride * height];
			orgImage.CopyPixels(rawPixels, stride, 0);
			byte[] resultBuffer = await Task.Run(() => vm.ApplyFiltersToBuffer(rawPixels, filters, width, height));
			ViewModel.SourceImage.WritePixels(new Int32Rect(0, 0, width, height), resultBuffer, stride, 0);
		}
		finally
		{
			_isSliderBusy = false;
		}
	}

	private async Task<bool> PreventAccumulativeEvent()
	{
		CancellationTokenSource oldCts = cts;
		cts = new CancellationTokenSource();
		oldCts?.Cancel();
		oldCts?.Dispose();
		CancellationToken token = cts.Token;
		try
		{
			await Task.Delay(100, token);
			if (token.IsCancellationRequested)
			{
				return false;
			}
		}
		catch (TaskCanceledException)
		{
			return false;
		}
		return true;
	}

	private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
	{
		ViewModel.history.Push(ViewModel.filters);
	}

	private void Undo_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.filters = ViewModel.history.Undo();
		if (ViewModel.filters != null)
		{
			UpdateTextBlockValue(ViewModel.filters);
			UpdateTrkBarValue(ViewModel.filters);
		}
	}

	private void Redo_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.filters = ViewModel.history.Redo();
		UpdateTextBlockValue(ViewModel.filters);
		UpdateTrkBarValue(ViewModel.filters);
	}

	private void Reset_Click(object sender, RoutedEventArgs e)
	{
		ViewModel.filters = ResetFilterSettings(ViewModel.filters);
		UpdateTextBlockValue(ViewModel.filters);
		UpdateTrkBarValue(ViewModel.filters);
	}

	private async void Save_Click(object sender, RoutedEventArgs e)
	{
		PhotoSession session = ServiceProvider.Settings.DefaultSession;
		session.GetNewFileItem(pathfilename);
		ServiceProvider.WindowsManager.ExecuteCommand("Del_Image", false);
		int timeout = 0;
		while (File.Exists(pathfilename) && timeout < 30)
		{
			await Task.Delay(100);
			timeout++;
		}
		if (File.Exists(pathfilename))
		{
			MessageBox.Show("파일 삭제에 실패하여 중단합니다.");
			return;
		}
		ViewModel.SaveImage(pathfilename);
		FileItem SelectedItem = session.GetNewFileItem(pathfilename);
		SelectedItem.RemoveThumbs();
		session.Add(SelectedItem);
		ServiceProvider.OnFileTransfered(SelectedItem);
		ServiceProvider.WindowsManager.ExecuteCommand("Select_Image", SelectedItem);
		MessageBox.Show("\ud83d\udce3 저장이 완료되었습니다.");
		DialogResult = true;
	}
}
