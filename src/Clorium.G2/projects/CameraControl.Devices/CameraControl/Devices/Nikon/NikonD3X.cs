using System.IO;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Nikon;

public class NikonD3X : NikonBase
{
	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Add(CapabilityEnum.LiveView);
		return result;
	}

	public override LiveViewData GetLiveViewImage()
	{
		LiveViewData liveViewData = new LiveViewData();
		liveViewData.HaveFocusData = true;
		MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(37379u);
		if (mTPDataResponse.ErrorCode == 40971)
		{
			_timer.Start();
			liveViewData.IsLiveViewRunning = false;
			liveViewData.ImageData = null;
			return liveViewData;
		}
		if (mTPDataResponse.Data == null || mTPDataResponse.Data.Length <= 64)
		{
			return null;
		}
		int num = mTPDataResponse.Data.Length;
		GetAdditionalLiveViewData(liveViewData, mTPDataResponse.Data);
		MemoryStream memoryStream = new MemoryStream(num - 64);
		memoryStream.Write(mTPDataResponse.Data, 64, num - 64);
		memoryStream.Close();
		liveViewData.ImageData = memoryStream.GetBuffer();
		return liveViewData;
	}

	protected override void InitFocusMode()
	{
		base.InitFocusMode();
		FocusMode.IsEnabled = false;
	}
}
