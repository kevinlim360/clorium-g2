using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Interfaces;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class MultipleLiveView : Window, IWindow, IComponentConnector
{
	public MultipleLiveView()
	{
		InitializeComponent();
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "MultipleLiveViewWnd_Show":
			Dispatcher.Invoke(() =>
			{
				((MultipleLiveViewViewModel)DataContext).InitCameras();
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				Show();
				Activate();
				Focus();
			});
			((MultipleLiveViewViewModel)DataContext).StartLiveView();
			break;
		case "MultipleLiveViewWnd_Hide":
			Dispatcher.BeginInvoke(new Action(Hide));
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				Hide();
				Close();
			});
			break;
		case "MultipleLiveViewWnd_Maximize":
			Dispatcher.Invoke(() =>
			{
				WindowState = WindowState.Maximized;
			});
			break;
		}
	}

	private void MetroWindow_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("MultipleLiveViewWnd_Hide");
		}
	}
}
