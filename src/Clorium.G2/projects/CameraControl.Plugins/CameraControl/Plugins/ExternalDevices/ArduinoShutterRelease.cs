using System;
using System.IO.Ports;
using System.Linq;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.Plugins.ExternalDevices;

public class ArduinoShutterRelease : IExternalDevice
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
			return (UserControl)(object)new ArduinoShutterReleaseConfig(config);
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
			serialPort.BaudRate = 9600;
			serialPort.WriteTimeout = 3500;
			if (config.Get("IsHexa") == "True")
			{
				byte[] data = StringToByteArray(config.Get("CaptureOn"));
				serialPort.Write(data, 0, data.Length);
			}
			else
			{
				serialPort.WriteLine(config.Get("CaptureOn"));
			}
			config.AttachedObject = serialPort;
		}
		catch (Exception ex)
		{
			Log.Debug("ArduinoShutterRelease", ex);
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
			if (config.Get("IsHexa") == "True")
			{
				byte[] data = StringToByteArray(config.Get("CaptureOff"));
				serialPort.Write(data, 0, data.Length);
			}
			else
			{
				serialPort.WriteLine(config.Get("CaptureOff"));
			}
			serialPort.Close();
			config.AttachedObject = null;
		}
		catch (Exception ex)
		{
			Log.Debug("ArduinoShutterRelease", ex);
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

	public ArduinoShutterRelease()
	{
		Name = "Arduino Shutter Release (Serial)";
		DeviceType = SourceEnum.ExternaExternalShutterRelease;
	}

	private static byte[] StringToByteArray(string hex)
	{
		return (from x in Enumerable.Range(0, hex.Length)
			where x % 2 == 0
			select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
	}
}
