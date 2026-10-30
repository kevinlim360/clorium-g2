using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WMPLib;

[ComImport]
[CompilerGenerated]
[Guid("9104D1AB-80C9-4FED-ABF0-2E6417A6DF14")]
[TypeIdentifier]
public interface IWMPSettings
{
	void _VtblGap1_20();

	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(111)]
	void setMode([In][MarshalAs(UnmanagedType.BStr)] string bstrMode, [In] bool varfMode);
}
