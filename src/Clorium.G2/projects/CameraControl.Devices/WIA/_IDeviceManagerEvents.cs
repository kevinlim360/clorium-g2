using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WIA;

[ComImport]
[CompilerGenerated]
[Guid("2E9A5206-2360-49DF-9D9B-1762B4BEAE77")]
[InterfaceType(2)]
[TypeIdentifier]
public interface _IDeviceManagerEvents
{
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(1)]
	void OnEvent([In][MarshalAs(UnmanagedType.BStr)] string EventID, [In][MarshalAs(UnmanagedType.BStr)] string DeviceID, [In][MarshalAs(UnmanagedType.BStr)] string ItemID);
}
