using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.ViewModel;
using MahApps.Metro.Controls;

namespace CameraControl.windows;

public partial class LiveViewWnd : Window, IWindow, INotifyPropertyChanged, IComponentConnector
{
	private SuperfocusWindow _rangeWindow;
	private SuperfocusWindow RangeWindow()
	{
		var window = SuperfocusWindow.For(((LiveViewViewModel)DataContext).CameraDevice, this);
		if (_rangeWindow != window)
		{
			_rangeWindow = window;
			window.Closed += (s, e) => { if (_rangeWindow == window) _rangeWindow = null; };
		}
		return window;
	}
	public void ShowSuperfocus() { var window = RangeWindow(); window.Show(); window.Activate(); }
	private void SuperfocusOpen_Click(object sender, RoutedEventArgs e) { ShowSuperfocus(); }
	private ICameraDevice _selectedPortableDevice;

	private DateTime _focusMoveTime = DateTime.Now;

	private CameraProperty _cameraProperty;

	private Point _startPoint;

	private bool _isDrawing = false;

	public LiveViewData LiveViewData { get; set; }

	public ICameraDevice SelectedPortableDevice
	{
		get
		{
			return _selectedPortableDevice;
		}
		set
		{
			if (_selectedPortableDevice != value)
			{
				_selectedPortableDevice = value;
				NotifyPropertyChanged("SelectedPortableDevice");
			}
		}
	}

	public CameraProperty CameraProperty
	{
		get
		{
			return _cameraProperty;
		}
		set
		{
			_cameraProperty = value;
			NotifyPropertyChanged("CameraProperty");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public LiveViewWnd()
	{
		try
		{
			SelectedPortableDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
			LiveViewManager.PreviewLoaded += LiveViewManager_PreviewLoaded;
		}
		catch (Exception exception)
		{
			Log.Error("Live view init error ", exception);
		}
		Init();
	}

	public LiveViewWnd(ICameraDevice device)
	{
		try
		{
			SelectedPortableDevice = device;
		}
		catch (Exception exception)
		{
			Log.Error("Live view init error ", exception);
		}
		Init();
	}

	public void Init()
	{
		try
		{
			InitializeComponent();
			SuperfocusLauncher.Visibility = SelectedPortableDevice is CameraControl.Devices.Canon.CanonSDKBase ? Visibility.Visible : Visibility.Collapsed;
		}
		catch (Exception exception)
		{
			Log.Error("Live view init error ", exception);
		}
	}

	public virtual void NotifyPropertyChanged(string info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}

	public void ExecuteCommand(string cmd, object param)
	{
		Dispatcher.Invoke(() =>
		{
			try
			{
				if (DataContext != null)
				{
					((LiveViewViewModel)DataContext).WindowsManager_Event(cmd, param);
				}
			}
			catch (Exception)
			{
			}
		});
		switch (cmd)
		{
		case "LiveViewWnd_Show":
			Dispatcher.Invoke(() =>
			{
				try
				{
					ICameraDevice cameraDevice = param as ICameraDevice;
					CameraProperty cameraProperty = cameraDevice.LoadProperties();
					if (cameraProperty.SaveLiveViewWindow && cameraProperty.WindowRect.Width > 0.0 && cameraProperty.WindowRect.Height > 0.0)
					{
						Left = cameraProperty.WindowRect.Left;
						Top = cameraProperty.WindowRect.Top;
						Width = cameraProperty.WindowRect.Width;
						Height = cameraProperty.WindowRect.Height;
					}
					if (cameraDevice == SelectedPortableDevice && IsVisible)
					{
						Activate();
						Focus();
					}
					else
					{
						DataContext = new LiveViewViewModel(cameraDevice, this);
						SelectedPortableDevice = cameraDevice;
						Show();
						Activate();
						Focus();
						if (ServiceProvider.Settings.IsMaximized)
						{
							WindowState = WindowState.Maximized;
						}
					}
				}
				catch (Exception exception)
				{
					Log.Error("Error initialize live view window ", exception);
				}
			});
			break;
		case "LiveViewWnd_Hide":
			Dispatcher.Invoke(() =>
			{
				try
				{
					ICameraDevice cameraDevice = ((LiveViewViewModel)DataContext).CameraDevice;
					CameraProperty cameraProperty = cameraDevice.LoadProperties();
					if (cameraProperty.SaveLiveViewWindow)
					{
						cameraProperty.WindowRect = new Rect(Left, Top, Width, Height);
					}
					((LiveViewViewModel)DataContext).UnInit();
				}
				catch (Exception exception)
				{
					Log.Error("Unable to stop live view", exception);
				}
				Hide();
				Tilting.IsChecked = false;
				Drawing.IsChecked = false;
			});
			break;
		case "LiveViewWnd_Message":
			Dispatcher.Invoke(() =>
			{
				MessageBox.Show((string)param);
			});
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				if (DataContext != null)
				{
					ICameraDevice cameraDevice = ((LiveViewViewModel)DataContext).CameraDevice;
					CameraProperty cameraProperty = cameraDevice.LoadProperties();
					if (cameraProperty.SaveLiveViewWindow)
					{
						cameraProperty.WindowRect = new Rect(Left, Top, Width, Height);
					}
					((LiveViewViewModel)DataContext).UnInit();
					Hide();
					Close();
				}
			});
			break;
		case "All_Minimize":
			Dispatcher.Invoke(() =>
			{
				WindowState = WindowState.Minimized;
			});
			break;
		case "LiveViewWnd_Maximize":
			Dispatcher.Invoke(() =>
			{
				WindowState = WindowState.Maximized;
			});
			break;
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (_rangeWindow?.IsWorking == true) { e.Cancel = true; _rangeWindow.RequestStop(); return; }
		_rangeWindow?.InvalidateRange(); _rangeWindow?.Hide();
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Hide", SelectedPortableDevice);
		}
	}

	private void image1_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left && _selectedPortableDevice.LiveViewImageZoomRatio.Value == "All")
		{
			try
			{
				((LiveViewViewModel)DataContext).SetFocusPos(e.MouseDevice.GetPosition(_image), _image.ActualWidth, _image.ActualHeight);
			}
			catch (Exception ex)
			{
				Log.Error("Focus Error", ex);
				StaticHelper.Instance.SystemMessage = "Focus error: " + ex.Message;
			}
		}
	}

	private void _image_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (e.Delta > 0)
		{
			((LiveViewViewModel)DataContext).CameraDevice.LiveViewImageZoomRatio.NextValue();
		}
		else
		{
			((LiveViewViewModel)DataContext).CameraDevice.LiveViewImageZoomRatio.PrevValue();
		}
	}

	private void canvas_image_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ButtonState == MouseButtonState.Pressed && e.ChangedButton == MouseButton.Left)
		{
			try
			{
				((LiveViewViewModel)DataContext).SetFocusPos(e.MouseDevice.GetPosition(_previeImage), _previeImage.ActualWidth, _previeImage.ActualHeight);
			}
			catch (Exception ex)
			{
				Log.Error("Focus Error", ex);
				StaticHelper.Instance.SystemMessage = "Focus error: " + ex.Message;
			}
		}
	}

	private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (!((DateTime.Now - _focusMoveTime).TotalMilliseconds < 200.0))
		{
			_focusMoveTime = DateTime.Now;
			TriggerClass.KeyDown(e);
		}
	}

	private void MetroWindow_StateChanged(object sender, EventArgs e)
	{
		((LiveViewViewModel)DataContext).IsMinized = WindowState == WindowState.Minimized;
		if (WindowState == WindowState.Maximized)
		{
			ServiceProvider.Settings.IsMaximized = true;
		}
		else if (WindowState == WindowState.Normal)
		{
			ServiceProvider.Settings.IsMaximized = false;
		}
	}

	private void MetroWindow_Drop(object sender, DragEventArgs e)
	{
		if (e.Data.GetDataPresent(DataFormats.FileDrop))
		{
			string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
			((LiveViewViewModel)DataContext).SetOverlay(files[0]);
			((LiveViewViewModel)DataContext).OverlayActivated = true;
		}
	}

	private void LiveViewManager_PreviewLoaded(ICameraDevice cameraDevice, string file)
	{
		Task.Factory.StartNew(() =>
		{
			Thread.Sleep(500);
			Application.Current.BeginInvoke(zoomAndPanControl.ScaleToFit);
		});
	}

	private void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		e.Handled = true;
		Point curContentMousePoint = e.GetPosition(PreviewBitmap);
		if (e.Delta > 0)
		{
			zoomAndPanControl.ZoomIn(curContentMousePoint);
		}
		else if (e.Delta < 0)
		{
			if (zoomAndPanControl.ContentScale - 0.2 > zoomAndPanControl.FitScale())
			{
				zoomAndPanControl.ZoomOut(curContentMousePoint);
			}
			else
			{
				zoomAndPanControl.ScaleToFit();
			}
		}
	}

	private void zoomAndPanControl_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		zoomAndPanControl.ScaleToFit();
	}

	private void zoomAndPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		Point curContentMousePoint = e.GetPosition(PreviewBitmap);
		if (zoomAndPanControl.ContentScale <= zoomAndPanControl.FitScale())
		{
			zoomAndPanControl.ZoomAboutPoint(4.0, curContentMousePoint);
		}
		else
		{
			zoomAndPanControl.ScaleToFit();
		}
	}

	private void ButtonZoomMinus_MouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			((LiveViewViewModel)DataContext).CameraDevice.StartZoom(ZoomDirection.Out);
		}
		catch (Exception exception)
		{
			Log.Debug("Zoom error", exception);
		}
	}

	private void ButtonZoomMinus_MouseUp(object sender, MouseButtonEventArgs e)
	{
		try
		{
			((LiveViewViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.Out);
		}
		catch (Exception exception)
		{
			Log.Debug("Zoom error", exception);
		}
	}

	private void ButtonZoomMinus_MouseLeave(object sender, MouseEventArgs e)
	{
		try
		{
			((LiveViewViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.Out);
		}
		catch (Exception exception)
		{
			Log.Debug("Zoom error", exception);
		}
	}

	private void ButtonZoomPlus_MouseDown(object sender, MouseButtonEventArgs e)
	{
		try
		{
			((LiveViewViewModel)DataContext).CameraDevice.StartZoom(ZoomDirection.In);
		}
		catch (Exception exception)
		{
			Log.Debug("Zoom error", exception);
		}
	}

	private void ButtonZoomPlus_MouseLeave(object sender, MouseEventArgs e)
	{
		try
		{
			((LiveViewViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.In);
		}
		catch (Exception exception)
		{
			Log.Debug("Zoom error", exception);
		}
	}

	private void ButtonZoomPlus_MouseUp(object sender, MouseButtonEventArgs e)
	{
		try
		{
			((LiveViewViewModel)DataContext).CameraDevice.StopZoom(ZoomDirection.In);
		}
		catch (Exception exception)
		{
			Log.Debug("Zoom error", exception);
		}
	}

	private void btn_help_Click(object sender, RoutedEventArgs e)
	{
		HelpProvider.Run(HelpSections.LiveView);
	}

	private void Expander_Click(object sender, RoutedEventArgs e)
	{
		if (!(sender is Expander currentExpander))
		{
			return;
		}
		if (currentExpander.Name == "ExpanderCalibration" && !ServiceProvider.Settings.CheckPassword())
		{
			currentExpander.IsExpanded = false;
			return;
		}
		DependencyObject parent = VisualTreeHelper.GetParent(currentExpander);
		while (parent != null && !(parent is StackPanel))
		{
			parent = VisualTreeHelper.GetParent(parent);
		}
		if (!(parent is StackPanel stackPanel))
		{
			return;
		}
		foreach (object child in stackPanel.Children)
		{
			if (child is ContentControl container)
			{
				if (container.Content is Expander otherExpander && otherExpander != currentExpander)
				{
					otherExpander.IsExpanded = false;
				}
			}
			else if (child is Expander directExpander && directExpander != currentExpander)
			{
				directExpander.IsExpanded = false;
			}
		}
	}

	private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (Drawing.IsChecked != false)
		{
			_isDrawing = true;
			_startPoint = e.GetPosition(DrawingCanvas);
			Canvas.SetLeft(StartThumb, _startPoint.X - StartThumb.Width / 2.0);
			Canvas.SetTop(StartThumb, _startPoint.Y - StartThumb.Height / 2.0);
			GuideLine.X1 = _startPoint.X;
			GuideLine.Y1 = _startPoint.Y;
			GuideLine.X2 = _startPoint.X;
			GuideLine.Y2 = _startPoint.Y;
		}
	}

	private void Canvas_MouseMove(object sender, MouseEventArgs e)
	{
		if (_isDrawing)
		{
			Point currentPoint = e.GetPosition(DrawingCanvas);
			GuideLine.X2 = currentPoint.X;
			GuideLine.Y2 = currentPoint.Y;
			Canvas.SetLeft(EndThumb, currentPoint.X - EndThumb.Width / 2.0);
			Canvas.SetTop(EndThumb, currentPoint.Y - EndThumb.Height / 2.0);
			UpdateMeasureLine(_startPoint, currentPoint);
		}
	}

	private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
	{
		_isDrawing = false;
	}

	private void UpdateMeasureLine(Point p1, Point p2)
	{
		int x1 = ((LiveViewViewModel)DataContext).writeableBitmapPixelWidth * (int)p1.X / 1000;
		int y1 = ((LiveViewViewModel)DataContext).writeableBitmapPixelHeight * (int)p1.Y / 1000;
		int width = (int)(p2.X - p1.X);
		int height = (int)(p2.Y - p1.Y);
		int x2 = ((LiveViewViewModel)DataContext).writeableBitmapPixelWidth * (int)(p1.X + (double)width) / 1000;
		int y2 = ((LiveViewViewModel)DataContext).writeableBitmapPixelHeight * (int)(p1.Y + (double)height) / 1000;
		double ratio = ((LiveViewViewModel)DataContext).Bitmap.Width / (double)((LiveViewViewModel)DataContext).orgImageWidth;
		double viewScale = double.Parse(ServiceProvider.Settings.MetaScale) / ratio;
		double dx = x2 - x1;
		double dy = y2 - y1;
		double distPx = Math.Sqrt(dx * dx + dy * dy);
		double distMm = distPx * viewScale;
		Canvas.SetLeft(DistanceText, (p1.X + p2.X) / 2.0);
		Canvas.SetTop(DistanceText, (p1.Y + p2.Y) / 2.0);
		DistanceText.Text = $"{distMm:F1} mm";
	}

	private void MoveLine_DragDelta(object sender, DragDeltaEventArgs e)
	{
		double dx = e.HorizontalChange;
		double dy = e.VerticalChange;
		GuideLine.X1 += dx;
		GuideLine.Y1 += dy;
		GuideLine.X2 += dx;
		GuideLine.Y2 += dy;
		Canvas.SetLeft(StartThumb, Canvas.GetLeft(StartThumb) + dx);
		Canvas.SetTop(StartThumb, Canvas.GetTop(StartThumb) + dy);
		Canvas.SetLeft(EndThumb, Canvas.GetLeft(EndThumb) + dx);
		Canvas.SetTop(EndThumb, Canvas.GetTop(EndThumb) + dy);
		Canvas.SetLeft(DistanceText, (GuideLine.X1 + GuideLine.X2) / 2.0);
		Canvas.SetTop(DistanceText, (GuideLine.Y1 + GuideLine.Y2) / 2.0);
	}

	private void CheckedDrawing_Click(object sender, RoutedEventArgs e)
	{
		((LiveViewViewModel)DataContext).SettingArea = true;
		BoxSize.Visibility = Visibility.Visible;
		Tilting.IsChecked = false;
		Centering.IsChecked = false;
	}

	private void UncheckedDrawing_Click(object sender, RoutedEventArgs e)
	{
		((LiveViewViewModel)DataContext).SettingArea = false;
		BoxSize.Visibility = Visibility.Collapsed;
	}

	private void CheckedTilting_Click(object sender, RoutedEventArgs e)
	{
		((LiveViewViewModel)DataContext).MeasureTilt = true;
		Result_Tilt.Visibility = Visibility.Visible;
		Centering.IsChecked = false;
		Drawing.IsChecked = false;
	}

	private void UncheckedTilting_Click(object sender, RoutedEventArgs e)
	{
		((LiveViewViewModel)DataContext).MeasureTilt = false;
		Result_Tilt.Visibility = Visibility.Collapsed;
	}

	private void CheckedCentering_Click(object sender, RoutedEventArgs e)
	{
		((LiveViewViewModel)DataContext).MeasureCenter = true;
		Result_Center.Visibility = Visibility.Visible;
		Tilting.IsChecked = false;
		Drawing.IsChecked = false;
	}

	private void UncheckedCentering_Click(object sender, RoutedEventArgs e)
	{
		((LiveViewViewModel)DataContext).MeasureCenter = false;
		Result_Center.Visibility = Visibility.Collapsed;
	}
}
