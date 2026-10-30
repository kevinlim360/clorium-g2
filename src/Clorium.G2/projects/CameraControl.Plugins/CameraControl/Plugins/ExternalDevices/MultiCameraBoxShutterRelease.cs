using System;
using System.IO.Ports;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.Plugins.ExternalDevices;

public class MultiCameraBoxShutterRelease : IExternalDevice
{
	private object _locker = new object();

	public string Name { get; set; }

	public SourceEnum DeviceType { get; set; }

	public bool Capture(CustomConfig config)
	{
		SendCommand("0", config.Get("Port"));
		SendCommand("5", config.Get("Port"));
		return true;
	}

	public bool Focus(CustomConfig config)
	{
		SendCommand("0", config.Get("Port"));
		SendCommand("6", config.Get("Port"));
		return true;
	}

	public bool CanExecute(CustomConfig config)
	{
		return true;
	}

	public UserControl GetConfig(CustomConfig config)
	{
		return (UserControl)(object)new SerialPortShutterReleaseConfig(config);
	}

	public bool OpenShutter(CustomConfig config)
	{
		SendCommand("2", config.Get("Port"));
		return true;
	}

	public bool CloseShutter(CustomConfig config)
	{
		SendCommand("3", config.Get("Port"));
		return true;
	}

	public bool AssertFocus(CustomConfig config)
	{
		SendCommand("1", config.Get("Port"));
		return true;
	}

	public bool DeassertFocus(CustomConfig config)
	{
		SendCommand("4", config.Get("Port"));
		return true;
	}

	public MultiCameraBoxShutterRelease()
	{
		Name = "Multi camera Shutter Release";
		DeviceType = SourceEnum.ExternaExternalShutterRelease;
	}

	private void SendCommand(string cmd, string port)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected Obj, but got Unknown
		lock (_locker)
		{
			try
			{
				SerialPort sp = new SerialPort();
				if (sp.IsOpen)
				{
					sp.Close();
				}
				sp.PortName = port;
				sp.BaudRate = 9600;
				sp.WriteTimeout = 3500;
				sp.Open();
				sp.Write(cmd);
				sp.Close();
			}
			catch (Exception ex)
			{
				Log.Error("Error sending serial command ", ex);
				StaticHelper.Instance.SystemMessage = "Error sending serial command " + ex.Message;
			}
		}
	}
}
