using System.Runtime.InteropServices;
using System.Threading;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Nikon;

public class NikonD40 : NikonBase
{
	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Clear();
		HaveLiveView = false;
		CaptureInSdRam = false;
		return result;
	}

	public override void ReadDeviceProperties(uint prop)
	{
		base.ReadDeviceProperties(prop);
		HaveLiveView = false;
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		Monitor.Enter(Locker);
		try
		{
			IsBusy = true;
			DeviceReady();
			ErrorCodes.GetException(ExecuteWithNoData(4110u));
		}
		catch (COMException exception)
		{
			IsBusy = false;
			ErrorCodes.GetException(exception);
		}
		catch
		{
			IsBusy = false;
			throw;
		}
		finally
		{
			Monitor.Exit(Locker);
		}
	}

	protected override PropertyValue<long> InitPictControl()
	{
		PropertyValue<long> propertyValue = new PropertyValue<long>();
		propertyValue.Name = "Picture control";
		propertyValue.IsEnabled = false;
		propertyValue.Code = 53760u;
		propertyValue.SubType = typeof(ushort);
		return propertyValue;
	}
}
