using Windows.Devices.Enumeration;

namespace CameraControl.Devices.GoPro;

public class GDeviceInformation
{
	public DeviceInformation DeviceInfo { get; set; } = null;

	public bool IsPresent { get; set; } = false;

	public bool IsConnected { get; set; } = false;

	public bool IsVisible => IsPresent || IsConnected;

	public string DisplayName => DeviceInfo.Name ?? "";

	public GDeviceInformation(DeviceInformation inDeviceInformation, bool inPresent, bool inConnected)
	{
		DeviceInfo = inDeviceInformation;
		IsPresent = inPresent;
		IsConnected = inConnected;
	}

	private GDeviceInformation()
	{
	}
}
