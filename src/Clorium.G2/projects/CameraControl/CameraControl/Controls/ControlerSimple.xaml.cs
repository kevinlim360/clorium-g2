using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;

namespace CameraControl.Controls;

public partial class ControlerSimple : UserControl, IComponentConnector
{
	public ControlerSimple()
	{
		InitializeComponent();
	}

	private void btn_capture_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ICameraDevice camera = DataContext as ICameraDevice;
			ServiceProvider.DeviceManager.SelectedCameraDevice = camera;
			CameraHelper.StopLiveAll();
			CameraHelper.Capture(DataContext);
			CameraHelper.StartLiveAll();
		}
		catch (Exception ex)
		{
			StaticHelper.Instance.SystemMessage = ex.Message;
			Log.Error("Take photo", ex);
		}
	}
}
