using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using CameraControl.Core;
using CameraControl.Core.Classes;

namespace CameraControl;

// Shown while G2 loads (Branding.ShowStartupScreen). Plays the Clorium intro (Intro/CloriumIntro, 4.4 s, then a slow
// 90-degree tick while loading continues); when G2 is ready CloseWhenReady() lets the intro finish, fades the logo out and
// closes. Branding.StartupScreenImage (a customer image) still replaces the logo; ShowStartupScreenAnimation=false shows the
// finished logo without animation.
public partial class StartUpWindow : Window, IComponentConnector
{
	private bool _closing;

	public StartUpWindow()
	{
		InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		ServiceProvider.Settings.Version_Debug = "4.7.1.5";
		VersionText.Text = ServiceProvider.Settings.Version_Debug;
		if (!string.IsNullOrEmpty(ServiceProvider.Branding.StartupScreenImage) && File.Exists(ServiceProvider.Branding.StartupScreenImage))
		{
			BitmapImage bi = new BitmapImage();
			bi.BeginInit();
			bi.UriSource = new Uri(PhotoUtils.GetFullPath(ServiceProvider.Branding.StartupScreenImage));
			bi.EndInit();
			BrandImage.Source = bi;
			BrandImage.Visibility = Visibility.Visible;
			Intro.Visibility = Visibility.Collapsed;
			Intro.ShowFinal();
			return;
		}
		if (ServiceProvider.Branding.ShowStartupScreenAnimation) Intro.Play();
		else Intro.ShowFinal();
	}

	// Called by App when G2 is ready (instead of Close): waits for the rest of the intro, fades the logo out, closes.
	public async void CloseWhenReady()
	{
		if (_closing) return;
		_closing = true;
		await Intro.WaitForIntroAsync();
		if (Intro.Visibility == Visibility.Visible) await Intro.HideAsync();
		Close();
	}
}
