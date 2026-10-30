using System;

namespace CameraControl.Devices.Classes;

public class DeviceObject
{
	public object Handle { get; set; }

	public string FileName { get; set; }

	public byte[] ThumbData { get; set; }

	public DateTime FileDate { get; set; }

	public long Size { get; set; }
}
