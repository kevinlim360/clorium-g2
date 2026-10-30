using System;
using CameraControl.Devices.Classes;
using CameraControl.Devices.TransferProtocol;
using CameraControl.Devices.TransferProtocol.PtpIp;

namespace CameraControl.Devices.Wifi;

public class PtpIpProvider : IWifiDeviceProvider
{
	public string Name { get; set; }

	public string DefaultIp { get; set; }

	public DeviceDescriptor Connect(string address)
	{
		string ip = address;
		if (address.Contains(":"))
		{
			ip = address.Split(new char[1] { ':' })[0];
			int.TryParse(address.Split(new char[1] { ':' })[1], out var _);
		}
		PtpIpClient ptpIpClient = new PtpIpClient();
		if (!ptpIpClient.Open(ip, 15740))
		{
			throw new Exception("No server was found!");
		}
		PtpIpProtocol ptpIpProtocol = new PtpIpProtocol(ptpIpClient);
		ptpIpProtocol.ExecuteWithNoData(4098u, 1u);
		if (CameraDeviceManager.GetNativeDriver(ptpIpProtocol.Model) != null)
		{
			DeviceDescriptor deviceDescriptor = new DeviceDescriptor
			{
				WpdId = "ptpip"
			};
			ICameraDevice cameraDevice = (ICameraDevice)Activator.CreateInstance(CameraDeviceManager.GetNativeDriver(ptpIpProtocol.Model));
			deviceDescriptor.StillImageDevice = ptpIpProtocol;
			deviceDescriptor.CameraDevice = cameraDevice;
			cameraDevice.Init(deviceDescriptor);
			return deviceDescriptor;
		}
		throw new Exception("Not Supported device " + ptpIpProtocol.Model);
	}

	public PtpIpProvider()
	{
		Name = "WU 1a/1b,Nikon camera";
		DefaultIp = "192.168.1.1";
	}
}
