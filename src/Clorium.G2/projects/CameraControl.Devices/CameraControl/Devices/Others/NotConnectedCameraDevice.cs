using System.Text;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Others;

public class NotConnectedCameraDevice : BaseCameraDevice
{
	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		HaveLiveView = false;
		IsBusy = false;
		return true;
	}

	public NotConnectedCameraDevice()
	{
		IsConnected = false;
		HaveLiveView = false;
		ExposureStatus = 1;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder(base.ToString() + "\n\tType..................Simulator (No Camera)");
		return stringBuilder.ToString();
	}
}
