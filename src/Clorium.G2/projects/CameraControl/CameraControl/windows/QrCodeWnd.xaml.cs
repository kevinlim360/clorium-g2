using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;

namespace CameraControl.windows;

public partial class QrCodeWnd : Window, IComponentConnector
{
	public QrCodeWnd()
	{
		InitializeComponent();
		if (qrcode != null)
		{
			qrcode.Text = ServiceProvider.Settings.Webaddress;
		}
	}

	private void textBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (qrcode != null)
		{
			qrcode.Text = ServiceProvider.Settings.Webaddress;
		}
	}

	private void MetroWindow_Deactivated(object sender, EventArgs e)
	{
		try
		{
		}
		catch
		{
		}
	}

	private void qrcode_MouseDown(object sender, MouseButtonEventArgs e)
	{
		PhotoUtils.Run(ServiceProvider.Settings.Webaddress);
	}
}
