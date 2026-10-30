using System.Windows;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.windows;

public partial class Welcome : Window, IComponentConnector
{
	public Welcome()
	{
		InitializeComponent();
	}

	private void btn_facebook_Click(object sender, RoutedEventArgs e)
	{
		PhotoUtils.Run("http://www.clorium.com");
		Close();
	}

	private void btn_google_Click(object sender, RoutedEventArgs e)
	{
		PhotoUtils.Run("http://www.clorium.com");
		Close();
	}

	private void btn_donate_Click(object sender, RoutedEventArgs e)
	{
		PhotoUtils.Run("http://www.clorium.com");
		Close();
	}

	private void btn_twitter_Click(object sender, RoutedEventArgs e)
	{
		PhotoUtils.Run("http://www.clorium.com");
		Close();
	}

	private void btn_flickr_Click(object sender, RoutedEventArgs e)
	{
		PhotoUtils.Run("http://www.clorium.com");
		Close();
	}

	private void btn_site_Click(object sender, RoutedEventArgs e)
	{
		PhotoUtils.Run("http://www.clorium.com");
	}
}
