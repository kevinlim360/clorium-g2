using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.windows;

public partial class AboutWnd : Window, IComponentConnector
{
	public AboutWnd()
	{
		InitializeComponent();
		Title = "About " + System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
		string file = Path.Combine(Settings.ApplicationFolder, "about.txt");
		if (File.Exists(file))
		{
			textBlock2.Visibility = Visibility.Hidden;
			textBlock1.Text = File.ReadAllText(file);
			btn_donate.Visibility = Visibility.Collapsed;
			button1.Visibility = Visibility.Collapsed;
		}
	}

	private void button2_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void button1_Click(object sender, RoutedEventArgs e)
	{
		PhotoUtils.Run("http://www.digicamcontrol.com/");
	}

	private void button3_Click(object sender, RoutedEventArgs e)
	{
		PhotoUtils.Run("http://www.gnu.org/licenses/gpl-3.0.txt");
	}

	private void btn_donate_Click(object sender, RoutedEventArgs e)
	{
		PhotoUtils.Donate();
	}
}
