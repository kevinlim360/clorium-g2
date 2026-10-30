using System;
using System.Collections.Generic;
using CameraControl.Devices.Classes;
using CameraControl.Devices.TransferProtocol;
using CameraControl.Devices.TransferProtocol.DDServer;

namespace CameraControl.Devices.Wifi;

public class DDServerProvider : IWifiDeviceProvider
{
	public string Name { get; set; }

	public string DefaultIp { get; set; }

	public DeviceDescriptor Connect(string address)
	{
		int result = 4757;
		string ip = address;
		if (address.Contains(":"))
		{
			ip = address.Split(new char[1] { ':' })[0];
			int.TryParse(address.Split(new char[1] { ':' })[1], out result);
		}
		DdClient ddClient = new DdClient();
		if (!ddClient.Open(ip, result))
		{
			throw new Exception("No server was found!");
		}
		List<DdServerDevice> devices = ddClient.GetDevices();
		if (devices.Count == 0)
		{
			throw new Exception("No connected device was found!");
		}
		ddClient.Connect(devices[0]);
		DdServerProtocol ddServerProtocol = new DdServerProtocol(ddClient);
		if (CameraDeviceManager.GetNativeDriver(ddServerProtocol.Model) != null)
		{
			DeviceDescriptor deviceDescriptor = new DeviceDescriptor
			{
				WpdId = "ddserver"
			};
			ICameraDevice cameraDevice = (ICameraDevice)Activator.CreateInstance(CameraDeviceManager.GetNativeDriver(ddServerProtocol.Model));
			deviceDescriptor.StillImageDevice = ddServerProtocol;
			cameraDevice.Init(deviceDescriptor);
			deviceDescriptor.CameraDevice = cameraDevice;
			return deviceDescriptor;
		}
		throw new Exception("Not Supported device " + ddServerProtocol.Model);
	}

	public DDServerProvider()
	{
		Name = "DSLRDASHBOARDSERVER (Nikon only)";
		DefaultIp = "192.168.1.1";
	}
}
