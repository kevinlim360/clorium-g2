using System;
using System.Collections.Generic;
using System.Net;
using CameraControl.Devices.Classes;
using CameraControl.Devices.Others;

namespace CameraControl.Devices.Wifi;

public class PtzOpticsProvider : IWifiDeviceProvider
{
	public string Name { get; set; }

	public string DefaultIp { get; set; }

	public DeviceDescriptor Connect(string address)
	{
		Dictionary<string, string> data = GetData("http://" + address + "/cgi-bin/param.cgi?get_device_conf");
		if (!data.ContainsKey("serial_num"))
		{
			throw new Exception("Invalid connection data");
		}
		PtzOpticsCamera ptzOpticsCamera = new PtzOpticsCamera(address);
		ptzOpticsCamera.SerialNumber = data["serial_num"];
		ptzOpticsCamera.Manufacturer = "PTZOptics";
		ptzOpticsCamera.DeviceName = data["devname"];
		DeviceDescriptor deviceDescriptor = new DeviceDescriptor
		{
			WpdId = "PTZOptics"
		};
		deviceDescriptor.CameraDevice = ptzOpticsCamera;
		return deviceDescriptor;
	}

	public PtzOpticsProvider()
	{
		Name = "PTZOptics";
		DefaultIp = "192.168.1.1";
	}

	private Dictionary<string, string> GetData(string url)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		WebClient webClient = new WebClient();
		string text = webClient.DownloadString(url);
		string[] array = text.Split(new char[1] { '\n' });
		foreach (string text2 in array)
		{
			if (text2 != null && text2.Contains("="))
			{
				dictionary.Add(text2.Split(new char[1] { '=' })[0].Trim(), text2.Split(new char[1] { '=' })[1].Replace("\"", ""));
			}
		}
		return dictionary;
	}
}
