using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WIA;

[ComImport]
[CompilerGenerated]
[DefaultMember("Value")]
[Guid("706038DC-9F4B-4E45-88E2-5EB7D665B815")]
[TypeIdentifier]
public interface IProperty
{
	[DispId(0)]
	object Value
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(0)]
		[return: MarshalAs(UnmanagedType.Struct)]
		get;
	}

	// WIA exposes a by-ref VARIANT setter; a C# property setter cannot express it.
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(0)]
	void set_Value([In][MarshalAs(UnmanagedType.Struct)] ref object pvResult);

	void _VtblGap1_1();

	void _VtblGap2_7();

	[DispId(8)]
	Vector SubTypeValues
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(8)]
		[return: MarshalAs(UnmanagedType.Interface)]
		get;
	}
}
