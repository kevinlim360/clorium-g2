using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CameraControl.Core;
using CameraControl.Core.Classes;

namespace CameraControl;

// Shown while G2 loads (Branding.ShowStartupScreen). The Clorium logo animation plays once (about 3 s) and then keeps a slow
// 90-degree tick while loading continues; when G2 is ready CloseWhenReady() lets the intro finish and fades the window out.
// Branding.StartupScreenImage (a customer image) still replaces the logo; ShowStartupScreenAnimation=false shows it still.
public partial class StartUpWindow : Window, IComponentConnector
{
	// Intro length (pinwheel + CLORIUM wipe + AI PHOTO STATION letters) before the window may close.
	private static readonly TimeSpan IntroLength = TimeSpan.FromSeconds(3.0);

	private static readonly TimeSpan FadeLength = TimeSpan.FromMilliseconds(450);

	private readonly Stopwatch _shown = new Stopwatch();

	private bool _animated;

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
			Logo.Visibility = Visibility.Collapsed;
		}
	}

	private void Window_ContentRendered(object sender, EventArgs e)
	{
		_shown.Start();
		if (Logo.Visibility != Visibility.Visible) return;
		Storyboard intro = (Storyboard)Resources["Intro"];
		if (!ServiceProvider.Branding.ShowStartupScreenAnimation)
		{
			// Still logo: the last frame of the intro.
			intro.Begin(this, true);
			intro.SkipToFill(this);
			return;
		}
		_animated = true;
		intro.Completed += (s, a) =>
		{
			if (!_closing) ((Storyboard)Resources["Tick"]).Begin(this, true);
		};
		intro.Begin(this, true);
	}

	// Called by App when G2 is ready (instead of Close): lets the intro finish, then fades out and closes.
	public void CloseWhenReady()
	{
		if (_closing) return;
		_closing = true;
		TimeSpan left = _animated ? IntroLength - _shown.Elapsed : TimeSpan.Zero;
		if (left <= TimeSpan.Zero)
		{
			FadeOut();
			return;
		}
		DispatcherTimer wait = new DispatcherTimer { Interval = left };
		wait.Tick += (s, e) =>
		{
			wait.Stop();
			FadeOut();
		};
		wait.Start();
	}

	private void FadeOut()
	{
		DoubleAnimation fade = new DoubleAnimation(0, FadeLength) { EasingFunction = new QuadraticEase() };
		fade.Completed += (s, e) => Close();
		DoubleAnimation grow = new DoubleAnimation(1.03, FadeLength);
		CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
		CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
		BeginAnimation(OpacityProperty, fade);
	}
}
