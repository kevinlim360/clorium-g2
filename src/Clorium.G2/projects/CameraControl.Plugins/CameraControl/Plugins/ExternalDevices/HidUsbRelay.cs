using System;
using System.Threading;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.Plugins.ExternalDevices;

internal class HidUsbRelay : IExternalDevice
{
	private int Hd = 0;

	public string Name { get; set; }

	public SourceEnum DeviceType { get; set; }

	public HidUsbRelay()
	{
		Name = "HID USB Relay Release";
		DeviceType = SourceEnum.ExternaExternalShutterRelease;
	}

	public bool Capture(CustomConfig config)
	{
		Connect();
		RelayDeviceWrapper.usb_relay_device_close_all_relay_channel(Hd);
		Thread.Sleep(500);
		RelayDeviceWrapper.usb_relay_device_open_all_relay_channel(Hd);
		RelayDeviceWrapper.usb_relay_device_close(Hd);
		Hd = 0;
		return true;
	}

	public bool Focus(CustomConfig config)
	{
		return true;
	}

	public bool CanExecute(CustomConfig config)
	{
		return true;
	}

	public UserControl GetConfig(CustomConfig config)
	{
		try
		{
			return (UserControl)(object)new HidUsbRelayConfig(config);
		}
		catch (Exception exception)
		{
			Log.Error("", exception);
		}
		return null;
	}

	public bool OpenShutter(CustomConfig config)
	{
		Capture(config);
		return true;
	}

	public bool CloseShutter(CustomConfig config)
	{
		Connect();
		RelayDeviceWrapper.usb_relay_device_close_all_relay_channel(Hd);
		RelayDeviceWrapper.usb_relay_device_close(Hd);
		Hd = 0;
		return true;
	}

	public bool AssertFocus(CustomConfig config)
	{
		return true;
	}

	public bool DeassertFocus(CustomConfig config)
	{
		return true;
	}

	private void Connect()
	{
		if (Hd == 0)
		{
			usb_relay_device_info? dev = RelayDeviceWrapper.usb_relay_device_enumerate();
			if (!dev.HasValue)
			{
				throw new Exception("No usb relay device found !");
			}
			Hd = RelayDeviceWrapper.usb_relay_device_open_with_serial_number(dev.Value.serial_number, dev.Value.serial_number.Length);
		}
	}
}
