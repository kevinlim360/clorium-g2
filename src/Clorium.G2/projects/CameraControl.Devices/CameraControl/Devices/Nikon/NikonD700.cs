using System.IO;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Nikon;

public class NikonD700 : NikonBase
{
	public const int CONST_PROP_LiveViewMode = 53664;

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Clear();
		Capabilities.Add(CapabilityEnum.LiveView);
		Capabilities.Add(CapabilityEnum.RecordMovie);
		Capabilities.Add(CapabilityEnum.CaptureInRam);
		CaptureInSdRam = true;
		return result;
	}

	public override void StartLiveView()
	{
		SetProperty(4118u, new byte[1] { 1 }, 53515u);
		DeviceReady();
		SetProperty(4118u, new byte[1] { 1 }, 53664u);
		DeviceReady();
		base.StartLiveView();
	}

	public override void StopLiveView()
	{
		base.StopLiveView();
		DeviceReady();
		SetProperty(4118u, new byte[1], 53515u);
		DeviceReady();
	}

	public override void CapturePhotoNoAf()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		lock (Locker)
		{
			try
			{
				IsBusy = true;
				MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4117u, 53666u);
				ErrorCodes.GetException(mTPDataResponse.ErrorCode);
				if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0 && mTPDataResponse.Data[0] > 0)
				{
					if (CaptureInSdRam)
					{
						DeviceReady();
						ErrorCodes.GetException(ExecuteWithNoData(37056u, uint.MaxValue));
						return;
					}
					StopLiveView();
				}
				DeviceReady();
				ErrorCodes.GetException(ExecuteWithNoData(4110u));
			}
			catch
			{
				IsBusy = false;
				throw;
			}
		}
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
