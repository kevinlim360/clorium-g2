using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.windows;

public partial class LiveViewFullScreenWnd : Window, IComponentConnector
{
	public LiveViewFullScreenWnd()
	{
		InitializeComponent();
	}

	private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		TriggerClass.KeyDown(e);
	}

	private void MetroWindow_Closing(object sender, CancelEventArgs e)
	{
	}

	private void image1_MouseDown(object sender, MouseButtonEventArgs e)
	{
	}

	private void image1_KeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			Close();
		}
	}

	private void image1_KeyUp(object sender, KeyEventArgs e)
	{
	}

	private void MetroWindow_StateChanged(object sender, EventArgs e)
	{
	}
}
