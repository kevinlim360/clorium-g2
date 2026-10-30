using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WIA;

[ComImport]
[CompilerGenerated]
[Guid("73856D9A-2720-487A-A584-21D5774E9D0F")]
[TypeIdentifier]
public interface IDeviceManager
{
	[DispId(1)]
	DeviceInfos DeviceInfos
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(2)]
	void RegisterEvent([In][MarshalAs(UnmanagedType.BStr)] string EventID, [In][MarshalAs(UnmanagedType.BStr)] string DeviceID = "*");
}
