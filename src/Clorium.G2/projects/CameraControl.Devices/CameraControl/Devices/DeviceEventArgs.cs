using System;

namespace CameraControl.Devices;

public class DeviceEventArgs : EventArgs
{
	public uint Code { get; set; }
}
