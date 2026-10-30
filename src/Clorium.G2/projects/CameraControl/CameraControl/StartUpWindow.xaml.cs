using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CameraControl.Core;
using CameraControl.Core.Classes;

namespace CameraControl;

public partial class StartUpWindow : Window, IComponentConnector
{
	public StartUpWindow()
	{
		InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		ServiceProvider.Settings.Version_Debug = "4.7.1.5";
		lbl_vers.Content = ServiceProvider.Settings.Version_Debug;
		if (!string.IsNullOrEmpty(ServiceProvider.Branding.StartupScreenImage) && File.Exists(ServiceProvider.Branding.StartupScreenImage))
		{
			BitmapImage bi = new BitmapImage();
			bi.BeginInit();
			bi.UriSource = new Uri(PhotoUtils.GetFullPath(ServiceProvider.Branding.StartupScreenImage));
			bi.EndInit();
			background.Source = bi;
		}
	}

	private void Window_ContentRendered(object sender, EventArgs e)
	{
		if (ServiceProvider.Branding.ShowStartupScreenAnimation && Resources["OnLoaded1"] is Storyboard storyBoard)
		{
			storyBoard.Begin(background);
		}
	}
}
