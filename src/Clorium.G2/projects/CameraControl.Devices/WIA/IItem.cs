using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WIA;

[ComImport]
[CompilerGenerated]
[Guid("68F2BF12-A755-4E2B-9BCD-37A22587D078")]
[TypeIdentifier]
public interface IItem
{
	[DispId(1)]
	string ItemID
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	void _VtblGap1_5();

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(7)]
	[return: MarshalAs(UnmanagedType.Struct)]
	object Transfer([In][MarshalAs(UnmanagedType.BStr)] string FormatID = "{00000000-0000-0000-0000-000000000000}");
}
