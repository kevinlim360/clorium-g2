using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WIA;

[ComImport]
[CompilerGenerated]
[Guid("F4243B65-3F63-4D99-93CD-86B6D62C5EB2")]
[TypeIdentifier]
public interface IImageFile
{
	void _VtblGap1_1();

	[DispId(2)]
	string FileExtension
	{
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		[DispId(2)]
		[return: MarshalAs(UnmanagedType.BStr)]
		get;
	}

	void _VtblGap2_16();

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(18)]
	void SaveFile([In][MarshalAs(UnmanagedType.BStr)] string Filename);
}
