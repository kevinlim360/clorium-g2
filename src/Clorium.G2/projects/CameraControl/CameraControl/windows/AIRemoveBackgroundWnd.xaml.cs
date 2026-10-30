using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Interfaces;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class AIRemoveBackgroundWnd : Window, IWindow, IComponentConnector
{
	public AIRemoveBackgroundWnd()
	{
		InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		zoomAndPanControl.ScaleToFit();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("AIRemoveBgWnd_Hide");
		}
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "AIRemoveBgWnd_Show":
			Dispatcher.Invoke(() =>
			{
				DataContext = new AIRemoveBackgroundViewModel(this);
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				Show();
				Activate();
				Focus();
				((AIRemoveBackgroundViewModel)DataContext).Init();
			});
			break;
		case "AIRemoveBgWnd_Hide":
			((AIRemoveBackgroundViewModel)DataContext).window_Closing();
			Dispatcher.Invoke(Hide);
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

	private void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		e.Handled = true;
		Point curContentMousePoint = e.GetPosition(Image);
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
		Point curContentMousePoint = e.GetPosition(Image);
		if (zoomAndPanControl.ContentScale <= zoomAndPanControl.FitScale())
		{
			zoomAndPanControl.ZoomAboutPoint(4.0, curContentMousePoint);
		}
		else
		{
			zoomAndPanControl.ScaleToFit();
		}
	}
}
