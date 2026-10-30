using CameraControl.Devices.Classes;
using CameraControl.Devices.Others;
using CameraControl.Devices.TransferProtocol;

namespace CameraControl.Devices.Wifi;

public class YiCameraProvider : IWifiDeviceProvider
{
	public string Name { get; set; }

	public string DefaultIp { get; set; }

	public DeviceDescriptor Connect(string address)
	{
		int result = 7878;
		string address2 = address;
		if (address.Contains(":"))
		{
			address2 = address.Split(new char[1] { ':' })[0];
			int.TryParse(address.Split(new char[1] { ':' })[1], out result);
		}
		YiCameraProtocol yiCameraProtocol = new YiCameraProtocol();
		yiCameraProtocol.Connect(address2, result);
		DeviceDescriptor deviceDescriptor = new DeviceDescriptor
		{
			WpdId = "YiCamera"
		};
		YiCamera yiCamera = new YiCamera();
		deviceDescriptor.StillImageDevice = yiCameraProtocol;
		deviceDescriptor.CameraDevice = yiCamera;
		yiCamera.Init(deviceDescriptor);
		return deviceDescriptor;
	}

	public YiCameraProvider()
	{
		Name = "Xiaomi Yi action camera";
		DefaultIp = "192.168.42.1";
	}
}
