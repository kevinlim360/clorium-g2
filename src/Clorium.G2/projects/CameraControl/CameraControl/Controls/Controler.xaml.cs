using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Translation;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.Controls;

public partial class Controler : UserControl, IComponentConnector
{
	private bool _loading = false;

	public Controler()
	{
		InitializeComponent();
		if (ServiceProvider.DeviceManager != null)
		{
			ServiceProvider.DeviceManager.PropertyChanged += DeviceManager_PropertyChanged;
		}
		RefreshItems();
	}

	private void DeviceManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (ServiceProvider.DeviceManager != null && ServiceProvider.DeviceManager.SelectedCameraDevice != null && e.PropertyName == "SelectedCameraDevice")
		{
			Dispatcher.Invoke(RefreshItems);
			if (ServiceProvider.DeviceManager.SelectedCameraDevice is BaseCameraDevice device)
			{
				device.PropertyChanged += device_PropertyChanged;
			}
		}
	}

	private void device_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (!_loading && sender == ServiceProvider.DeviceManager.SelectedCameraDevice && e.PropertyName == "CaptureInSdRam")
		{
			Dispatcher.BeginInvoke(new Action(RefreshItems));
		}
	}

	private void RefreshItems()
	{
		_loading = true;
		try
		{
			if (ServiceProvider.Settings == null || !ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
			{
				return;
			}
			CameraProperty property = ServiceProvider.DeviceManager.SelectedCameraDevice.LoadProperties();
			cmb_transfer.Items.Clear();
			if (ServiceProvider.DeviceManager.SelectedCameraDevice.GetCapability(CapabilityEnum.CaptureInRam))
			{
				cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem1);
				cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem2);
				cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem3);
				if (ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam)
				{
					cmb_transfer.SelectedItem = TranslationStrings.LabelTransferItem1;
				}
				else if (!ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam && property.NoDownload)
				{
					cmb_transfer.SelectedItem = TranslationStrings.LabelTransferItem2;
				}
				else
				{
					cmb_transfer.SelectedItem = TranslationStrings.LabelTransferItem3;
				}
			}
			else
			{
				cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem2);
				cmb_transfer.Items.Add(TranslationStrings.LabelTransferItem3);
				cmb_transfer.SelectedItem = (property.NoDownload ? TranslationStrings.LabelTransferItem2 : TranslationStrings.LabelTransferItem3);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Error relod list ", exception);
		}
		_loading = false;
	}

	private void cmb_shutter_GotFocus(object sender, RoutedEventArgs e)
	{
		if (!(sender is ComboBox { IsFocused: not false }))
		{
		}
	}

	private void cmb_transfer_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_loading && !ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
		{
			ServiceProvider.DeviceManager.SelectedCameraDevice.StopLiveView();
			CameraProperty property = ServiceProvider.DeviceManager.SelectedCameraDevice.LoadProperties();
			if ((string)cmb_transfer.SelectedItem == TranslationStrings.LabelTransferItem1 && !ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam)
			{
				ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = true;
			}
			if ((string)cmb_transfer.SelectedItem == TranslationStrings.LabelTransferItem2)
			{
				property.NoDownload = true;
				ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = false;
			}
			if ((string)cmb_transfer.SelectedItem == TranslationStrings.LabelTransferItem3)
			{
				property.NoDownload = false;
				ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = false;
			}
			property.CaptureInSdRam = ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam;
			ServiceProvider.DeviceManager.SelectedCameraDevice.StartLiveView();
		}
	}
}
