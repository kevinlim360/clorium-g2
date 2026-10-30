using System;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class WebCamWnd : Window, IComponentConnector
{
	public WebCamWnd()
	{
		InitializeComponent();
		DataContext = new WebCamViewModel();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		((WebCamViewModel)DataContext).InitWebcam();
	}

	private void Window_Closed(object sender, EventArgs e)
	{
	}

	private void btn_WebCpture_Click(object sender, RoutedEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("Capture");
	}
}
