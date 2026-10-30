using System;
using System.Runtime.InteropServices;

namespace Canon.Eos.Framework.Internal.SDK;

public partial class Edsdk
{

	[DllImport("EDSDK.dll")]
	public static extern uint EdsSetPropertyData(IntPtr inRef, uint inPropertyId, int inParam, int inPropertySize, byte[] inPropertyData);

	[DllImport("EDSDK.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "EdsCreateEvfImageRef")]
	public static extern uint EdsCreateEvfImageRefCdecl(IntPtr inStreamRef, out IntPtr outEvfImageRef);

	[DllImport("EDSDK.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "EdsDownloadEvfImage")]
	public static extern uint EdsDownloadEvfImageCdecl(IntPtr inCameraRef, IntPtr outEvfImageRef);
}
