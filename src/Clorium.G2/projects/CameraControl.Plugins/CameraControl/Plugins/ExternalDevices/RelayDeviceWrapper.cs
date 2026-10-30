using System;
using System.Runtime.InteropServices;

namespace CameraControl.Plugins.ExternalDevices;

public class RelayDeviceWrapper
{
	[DllImport("usb_relay_device.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "usb_relay_device_enumerate")]
	private static extern IntPtr Pusb_relay_device_enumerate();

	public static usb_relay_device_info? usb_relay_device_enumerate()
	{
		IntPtr x = Pusb_relay_device_enumerate();
		if (x == IntPtr.Zero)
		{
			return null;
		}
		return (usb_relay_device_info)Marshal.PtrToStructure(x, typeof(usb_relay_device_info));
	}

	[DllImport("usb_relay_device.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	public static extern int usb_relay_init();

	[DllImport("usb_relay_device.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	public static extern int usb_relay_device_open_with_serial_number([MarshalAs(UnmanagedType.LPStr)] string serial_number, int len);

	[DllImport("usb_relay_device.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	public static extern void usb_relay_device_close(int hHandle);

	[DllImport("usb_relay_device.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	public static extern int usb_relay_device_open_all_relay_channel(int hHandle);

	[DllImport("usb_relay_device.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	public static extern int usb_relay_device_close_all_relay_channel(int hHandle);

	[DllImport("usb_relay_device.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	public static extern int usb_relay_device_close_one_relay_channel(int hHandle, int index);

	[DllImport("usb_relay_device.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
	public static extern int usb_relay_device_open_one_relay_channel(int hHandle, int index);
}
