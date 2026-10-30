using System;
using System.IO.Ports;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.Plugins.ExternalDevices;

public class SerialPortShutterRelease : IExternalDevice
{
	public string Name { get; set; }

	public SourceEnum DeviceType { get; set; }

	public bool Capture(CustomConfig config)
	{
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
			return (UserControl)(object)new SerialPortShutterReleaseConfig(config);
		}
		catch (Exception exception)
		{
			Log.Error("", exception);
		}
		return null;
	}

	public bool OpenShutter(CustomConfig config)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected Obj, but got Unknown
		try
		{
			if (config.AttachedObject != null)
			{
				CloseShutter(config);
			}
			SerialPort serialPort = new SerialPort(config.Get("Port"));
			serialPort.Open();
			serialPort.RtsEnable = true;
			config.AttachedObject = serialPort;
		}
		catch (Exception ex)
		{
			Log.Debug("Comm OpenShutter", ex);
			StaticHelper.Instance.SystemMessage = "Error Shutter " + ex.Message;
		}
		return true;
	}

	public bool CloseShutter(CustomConfig config)
	{
		try
		{
			if (config.AttachedObject == null)
			{
				return false;
			}
			object attachedObject = config.AttachedObject;
			SerialPort serialPort = (SerialPort)((attachedObject is SerialPort) ? attachedObject : null);
			if (serialPort == null)
			{
				throw new ArgumentNullException("serialPort");
			}
			serialPort.RtsEnable = false;
			serialPort.Close();
			config.AttachedObject = null;
		}
		catch (Exception ex)
		{
			Log.Debug("Comm CloseShutter", ex);
			StaticHelper.Instance.SystemMessage = "Error Shutter " + ex.Message;
		}
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

	public SerialPortShutterRelease()
	{
		Name = "Serial Port Shutter Release";
		DeviceType = SourceEnum.ExternaExternalShutterRelease;
	}
}
