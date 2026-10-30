using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WIA;

[ComImport]
[CompilerGenerated]
[Guid("40571E58-A308-470A-80AA-FA10F88793A0")]
[TypeIdentifier]
	public interface IProperties : IEnumerable
{
	// The embedded WIA indexer has a by-ref VARIANT argument, which C# indexers
	// cannot express. Calling the accessor directly retains the COM method signature.
	[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
	[DispId(0)]
	[return: MarshalAs(UnmanagedType.Interface)]
	Property get_Item([In][MarshalAs(UnmanagedType.Struct)] ref object Index);
}
