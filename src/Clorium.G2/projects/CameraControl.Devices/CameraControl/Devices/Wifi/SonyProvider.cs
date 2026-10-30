using System;
using CameraControl.Devices.Classes;
using CameraControl.Devices.Sony;
using Kazyx.DeviceDiscovery;
using SonyCameraCommunication;

namespace CameraControl.Devices.Wifi;

public class SonyProvider : IWifiDeviceProvider
{
	public string Name { get; set; }

	public string DefaultIp { get; set; }

	public DeviceDescriptor Connect(string address)
	{
		CameraDiscovery cameraDiscovery = new CameraDiscovery();
		if (cameraDiscovery.UDPSocketSetup() && cameraDiscovery.MSearch())
		{
			string response = cameraDiscovery.DeviceDescription();
			SonyCameraDeviceInfo sonyCameraDeviceInfo = cameraDiscovery.AnalyzeDescription(response);
			SonyWifiCamera sonyWifiCamera = new SonyWifiCamera();
			sonyWifiCamera.Init(sonyCameraDeviceInfo.Endpoints["camera"]);
			sonyWifiCamera.DeviceName = sonyCameraDeviceInfo.FriendlyName;
			sonyWifiCamera.SerialNumber = sonyCameraDeviceInfo.UDN;
			DeviceDescriptor deviceDescriptor = new DeviceDescriptor
			{
				WpdId = "SonyWifiCamera"
			};
			deviceDescriptor.CameraDevice = sonyWifiCamera;
			return deviceDescriptor;
		}
		throw new Exception("No camera was found !");
	}

	public SonyProvider()
	{
		Name = "Sony";
		DefaultIp = "<Auto>";
	}
}
