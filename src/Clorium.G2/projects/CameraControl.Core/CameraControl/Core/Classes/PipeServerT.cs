#define DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading;
using CameraControl.Core.Scripting;
using CameraControl.Devices;
using Newtonsoft.Json;

namespace CameraControl.Core.Classes;

public class PipeServerT
{
	private string _pipeName;

	public void Listen(string PipeName)
	{
		try
		{
			_pipeName = PipeName;
			NamedPipeServerStream pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, pipeServer);
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex.Message);
		}
	}

	private void WaitForConnectionCallBack(IAsyncResult iar)
	{
		try
		{
			NamedPipeServerStream pipeServer = (NamedPipeServerStream)iar.AsyncState;
			pipeServer.EndWaitForConnection(iar);
			StreamReader sr = new StreamReader(pipeServer);
			StreamWriter sw = new StreamWriter(pipeServer);
			string response = ProccesQueries(sr.ReadLine());
			sw.WriteLine(response);
			sw.Flush();
			pipeServer.WaitForPipeDrain();
			pipeServer.Disconnect();
			pipeServer.Close();
			pipeServer = null;
			pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
			pipeServer.BeginWaitForConnection(WaitForConnectionCallBack, pipeServer);
		}
		catch (Exception exception)
		{
			Log.Debug("Pipe server error", exception);
		}
	}

	private string ProccesQueries(string query)
	{
		string res = ":;response:error;message:wrong query";
		try
		{
			Dictionary<string, string> lines = Pharse(query);
			if (lines.ContainsKey("request"))
			{
				string text = lines["request"];
				string text2 = text;
				if (!(text2 == "session"))
				{
					if (text2 == "cameras")
					{
						if (ServiceProvider.DeviceManager.ConnectedDevices.Count == 0)
						{
							return ":;response:error;message:no camera is connected";
						}
						return (lines.ContainsKey("format") && lines["format"] == "json") ? JsonConvert.SerializeObject(ServiceProvider.DeviceManager.ConnectedDevices, Formatting.Indented, new JsonSerializerSettings()) : GetCamerasData();
					}
					return ":;response:error;message:unknown request";
				}
				return (lines.ContainsKey("format") && lines["format"] == "json") ? JsonConvert.SerializeObject(ServiceProvider.Settings.DefaultSession, Formatting.Indented, new JsonSerializerSettings()) : GetSessionData();
			}
			if (lines.ContainsKey("command"))
			{
				ICameraDevice device = null;
				if (ServiceProvider.DeviceManager.ConnectedDevices.Count == 0)
				{
					return ":;response:error;message:no camera is connected";
				}
				if (lines.ContainsKey("serial"))
				{
					device = GetDevice(lines["serial"]);
				}
				if (device == null)
				{
					if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
					{
						return ":;response:error;message:No camera was found";
					}
					device = ServiceProvider.DeviceManager.SelectedCameraDevice;
				}
				switch (lines["command"])
				{
				case "capture":
				{
					Thread thread = new Thread(StartCapture);
					thread.Start(device);
					return ":;response:ok;";
				}
				case "starliveview":
					ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Show", device);
					return ":;response:ok;";
				case "captureliveview":
					ServiceProvider.WindowsManager.ExecuteCommand("LiveView_Capture", device);
					return ":;response:ok;";
				case "manualfocus":
				{
					DateTime time = DateTime.Now;
					ServiceProvider.WindowsManager.ExecuteCommand("LiveView_ManualFocus" + lines["step"], device);
					return ":;response:ok;executiontime:" + (DateTime.Now - time).ToString();
				}
				case "stopliveview":
					ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Hide", device);
					return ":;response:ok;";
				case "dcc":
					try
					{
						if (!lines.ContainsKey("param"))
						{
							return ":;response:error;No parameters are specified";
						}
						CommandLineProcessor processor = new CommandLineProcessor();
						object resp = processor.Pharse(lines["param"].Split(' '));
						return $":;response:{JsonConvert.SerializeObject(resp)};";
					}
					catch (Exception ex)
					{
						return ":;response:error;message:" + ex.Message;
					}
				}
			}
		}
		catch (Exception ex2)
		{
			res = ":;response:error;message:" + ex2.Message;
		}
		return res;
	}

	private ICameraDevice GetDevice(string serial)
	{
		return ServiceProvider.DeviceManager.ConnectedDevices.FirstOrDefault((ICameraDevice device) => device.SerialNumber == serial);
	}

	private void StartCapture(object o)
	{
		try
		{
			CameraHelper.Capture(o);
		}
		catch (Exception ex)
		{
			Log.Error("Error capture:", ex);
			StaticHelper.Instance.SystemMessage = ex.Message;
		}
	}

	public static Dictionary<string, string> Pharse(string data)
	{
		Dictionary<string, string> res = new Dictionary<string, string>();
		if (data.Length < 3)
		{
			return null;
		}
		string valueseparator = data.Substring(0, 1);
		char lineseparator = Convert.ToChar(data.Substring(1, 1));
		string[] lines = data.Substring(2).Split(lineseparator);
		if (lines.Length < 1)
		{
			return null;
		}
		string[] array = lines;
		foreach (string line in array)
		{
			if (line.Contains(valueseparator))
			{
				int seppos = line.IndexOf(valueseparator, StringComparison.Ordinal);
				res.Add(line.Substring(0, seppos), line.Substring(seppos + 1));
			}
			else
			{
				res.Add(valueseparator, string.Empty);
			}
		}
		return res;
	}

	private string GetSessionData()
	{
		string res = ":;response:session";
		IList<PropertyInfo> props = new List<PropertyInfo>(typeof(PhotoSession).GetProperties());
		foreach (PropertyInfo prop in props)
		{
			if (prop.PropertyType == typeof(string) || prop.PropertyType == typeof(int) || prop.PropertyType == typeof(bool))
			{
				object value = prop.GetValue(ServiceProvider.Settings.DefaultSession, null);
				res += $";{prop.Name.ToLower()}:{value}";
			}
		}
		return res;
	}

	private string GetCamerasData()
	{
		string res = ":;response:cameras;count:" + ServiceProvider.DeviceManager.ConnectedDevices.Count;
		for (int i = 0; i < ServiceProvider.DeviceManager.ConnectedDevices.Count; i++)
		{
			ICameraDevice device = ServiceProvider.DeviceManager.ConnectedDevices[i];
			if (!(device.DeviceName == "Z-App camera") && !(device.DeviceName == "Z-FTP camera") && !(device.DeviceName == ServiceProvider.Settings.WebCam_DeviceName))
			{
				CameraProperty property = ServiceProvider.Settings.CameraProperties.Get(ServiceProvider.DeviceManager.ConnectedDevices[i]);
				res += string.Format(";serial{0}:{1};model{0}:{2};name{0}:{3}", i + 1, ServiceProvider.DeviceManager.ConnectedDevices[i].SerialNumber, ServiceProvider.DeviceManager.ConnectedDevices[i].DeviceName, property.DeviceName);
			}
		}
		return res;
	}
}
