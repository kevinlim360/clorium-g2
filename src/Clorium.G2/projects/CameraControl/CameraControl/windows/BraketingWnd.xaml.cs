using System.Windows;
using System.Windows.Markup;
using CameraControl.Core.Classes;
using CameraControl.Devices;

namespace CameraControl.windows;

public partial class BraketingWnd : Window, IComponentConnector
{
	public BraketingWnd(ICameraDevice device, PhotoSession session)
	{
		InitializeComponent();
	}

	private void btn_help_Click(object sender, RoutedEventArgs e)
	{
		HelpProvider.Run(HelpSections.Bracketig);
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
