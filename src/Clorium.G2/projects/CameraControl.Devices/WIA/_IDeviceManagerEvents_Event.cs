using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WIA;

[ComImport]
[CompilerGenerated]
[Guid("94a0e92d-43c0-494e-ac29-fd45948a5221")]
[ComEventInterface(typeof(_IDeviceManagerEvents), typeof(_IDeviceManagerEvents))]
[TypeIdentifier("94a0e92d-43c0-494e-ac29-fd45948a5221", "WIA._IDeviceManagerEvents_Event")]
public interface _IDeviceManagerEvents_Event
{
	event _IDeviceManagerEvents_OnEventEventHandler OnEvent;
}
