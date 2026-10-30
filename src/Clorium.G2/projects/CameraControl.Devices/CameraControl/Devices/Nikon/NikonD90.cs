using System;
using System.IO;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Nikon;

public class NikonD90 : NikonBase
{
	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Clear();
		Capabilities.Add(CapabilityEnum.LiveView);
		Capabilities.Add(CapabilityEnum.CaptureInRam);
		Capabilities.Add(CapabilityEnum.CaptureNoAf);
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
		if (mTPDataResponse.Data == null || mTPDataResponse.Data.Length <= 128)
		{
			return null;
		}
		int num = mTPDataResponse.Data.Length;
		GetAdditionalLiveViewData(liveViewData, mTPDataResponse.Data);
		MemoryStream memoryStream = new MemoryStream(num - 128);
		memoryStream.Write(mTPDataResponse.Data, 128, num - 128);
		memoryStream.Close();
		liveViewData.ImageData = memoryStream.GetBuffer();
		return liveViewData;
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
				LockCamera();
				byte b = 0;
				MTPDataResponse mTPDataResponse2 = StillImageDevice.ExecuteReadData(4117u, 53601u);
				if (mTPDataResponse2.Data != null && mTPDataResponse2.Data.Length != 0)
				{
					b = mTPDataResponse2.Data[0];
				}
				SetProperty(4118u, new byte[1] { 4 }, 53601u);
				ErrorCodes.GetException(CaptureInSdRam ? ExecuteWithNoData(37056u, uint.MaxValue) : ExecuteWithNoData(4110u));
				if (mTPDataResponse2.Data != null && mTPDataResponse2.Data.Length != 0)
				{
					SetProperty(4118u, new byte[1] { b }, 53601u);
				}
				UnLockCamera();
			}
			catch (Exception)
			{
				IsBusy = false;
				throw;
			}
		}
	}
}
