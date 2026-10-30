using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class AstroLiveViewWnd : Window, IWindow, IComponentConnector
{
	public ICameraDevice CameraDevice { get; set; }

	public LiveViewData LiveViewData { get; set; }

	public int Brightness { get; set; }

	public bool Freeze { get; set; }

	public Point CentralPoint { get; set; }

	public AstroLiveViewWnd()
	{
		Brightness = 0;
		Freeze = false;
		InitializeComponent();
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "AstroLiveViewWnd_Show":
			CameraDevice = param as ICameraDevice;
			if (CameraDevice != null)
			{
				Dispatcher.Invoke(() =>
				{
					DataContext = new AstroLiveViewViewModel(CameraDevice, this);
					Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
					Show();
					Activate();
					Focus();
				});
			}
			break;
		case "AstroLiveViewWnd_Hide":
			try
			{
				((AstroLiveViewViewModel)DataContext).UnInit();
			}
			catch (Exception exception)
			{
				Log.Error("Unable to stop live view", exception);
			}
			Hide();
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				Hide();
				Close();
			});
			break;
		}
	}

	private void MetroWindow_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("AstroLiveViewWnd_Hide");
		}
	}

	private void live_view_image_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left && ((AstroLiveViewViewModel)DataContext).Bitmap != null && ((AstroLiveViewViewModel)DataContext).ZoomFactor == 1)
		{
			Point point = e.MouseDevice.GetPosition(live_view_image);
			double dw = (double)((AstroLiveViewViewModel)DataContext).Bitmap.PixelWidth / live_view_image.ActualWidth;
			double hw = (double)((AstroLiveViewViewModel)DataContext).Bitmap.PixelHeight / live_view_image.ActualHeight;
			((AstroLiveViewViewModel)DataContext).CentralPoint = new Point(point.X * dw, point.Y * hw);
			if (((AstroLiveViewViewModel)DataContext).IsFullImage)
			{
				((LiveViewViewModel)DataContext).SetFocusPos(point, live_view_image.ActualWidth, live_view_image.ActualHeight);
			}
		}
	}

	private void img_preview_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left && ((AstroLiveViewViewModel)DataContext).Bitmap != null)
		{
			Point point = e.MouseDevice.GetPosition(img_preview);
			if (((AstroLiveViewViewModel)DataContext).IsFullImage)
			{
				double dw = (double)((AstroLiveViewViewModel)DataContext).Preview.PixelWidth / img_preview.ActualWidth;
				double hw = (double)((AstroLiveViewViewModel)DataContext).Preview.PixelHeight / img_preview.ActualHeight;
				((AstroLiveViewViewModel)DataContext).CentralPoint = new Point(point.X * dw, point.Y * hw);
			}
			try
			{
				((LiveViewViewModel)DataContext).SetFocusPos(point, img_preview.ActualWidth, img_preview.ActualHeight);
			}
			catch (Exception ex)
			{
				Log.Error("Focus Error", ex);
				StaticHelper.Instance.SystemMessage = "Focus error: " + ex.Message;
			}
		}
	}
}
