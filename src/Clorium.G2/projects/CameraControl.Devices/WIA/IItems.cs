using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WIA;

[ComImport]
[CompilerGenerated]
[Guid("46102071-60B4-4E58-8620-397D17B0BB5B")]
[TypeIdentifier]
public interface IItems : IEnumerable
{
	[DispId(0)]
	Item this[[In] int Index]
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}

	[DispId(1)]
	int Count
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(1)]
		get;
	}

	void _VtblGap1_2();

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(3)]
	void Remove([In] int Index);
}
