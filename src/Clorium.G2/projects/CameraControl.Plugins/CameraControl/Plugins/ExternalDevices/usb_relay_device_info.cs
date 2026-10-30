using System;
using System.Runtime.InteropServices;

namespace CameraControl.Plugins.ExternalDevices;

public struct usb_relay_device_info
{
	[MarshalAs(UnmanagedType.LPStr)]
	public string serial_number;

	public IntPtr device_path { get; set; }

	public usb_relay_device_type type { get; set; }

	public IntPtr next { get; set; }
}
