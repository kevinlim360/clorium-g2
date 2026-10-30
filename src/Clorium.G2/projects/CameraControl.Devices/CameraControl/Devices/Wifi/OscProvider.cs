using CameraControl.Devices.Classes;
using CameraControl.Devices.Others;

namespace CameraControl.Devices.Wifi;

public class OscProvider : IWifiDeviceProvider
{
	public string Name { get; set; }

	public string DefaultIp { get; set; }

	public DeviceDescriptor Connect(string address)
	{
		DeviceDescriptor deviceDescriptor = new DeviceDescriptor
		{
			WpdId = "OscCamera"
		};
		OscCamera oscCamera = (OscCamera)(deviceDescriptor.CameraDevice = new OscCamera());
		oscCamera.Init("http://" + address);
		return deviceDescriptor;
	}

	public OscProvider()
	{
		Name = "OSC Protocol";
		DefaultIp = "192.168.1.1";
	}
}
