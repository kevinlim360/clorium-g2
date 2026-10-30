using System;
using CameraControl.Devices.Classes;
using PanonoTest;

namespace CameraControl.Devices.Wifi;

public class PanonoProvider : IWifiDeviceProvider
{
	public string Name { get; set; }

	public string DefaultIp { get; set; }

	public DeviceDescriptor Connect(string address)
	{
		if (string.IsNullOrEmpty(address) || address == DefaultIp)
		{
			PanonoDiscovery panonoDiscovery = new PanonoDiscovery();
			if (panonoDiscovery.UDPSocketSetup())
			{
				return ConnectCamera(panonoDiscovery.EndPoint);
			}
			throw new Exception("No camera was found !");
		}
		return ConnectCamera(address);
	}

	public DeviceDescriptor ConnectCamera(string endpoint)
	{
		PanonoCamera panonoCamera = new PanonoCamera();
		panonoCamera.Init(endpoint);
		Log.Debug("Camera found at " + endpoint);
		panonoCamera.DeviceName = "Panono";
		panonoCamera.Manufacturer = "Panono";
		return new DeviceDescriptor
		{
			WpdId = "PanonoCamera",
			CameraDevice = panonoCamera
		};
	}

	public PanonoProvider()
	{
		Name = "Panono";
		DefaultIp = "<Auto>";
	}
}
