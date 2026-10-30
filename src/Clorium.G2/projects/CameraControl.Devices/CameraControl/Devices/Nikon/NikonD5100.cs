using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Nikon;

public class NikonD5100 : NikonBase
{
	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Clear();
		Capabilities.Add(CapabilityEnum.LiveView);
		Capabilities.Add(CapabilityEnum.RecordMovie);
		Capabilities.Add(CapabilityEnum.CaptureInRam);
		Capabilities.Add(CapabilityEnum.CaptureNoAf);
		return result;
	}
}
