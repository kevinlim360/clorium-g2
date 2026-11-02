using System;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Nikon;

public class NikonD300 : NikonD3X
{
	public const int CONST_PROP_LiveViewMode = 53664;

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Remove(CapabilityEnum.CaptureNoAf);
		return result;
	}

	public override void StartLiveView()
	{
		if (!CaptureInSdRam)
		{
			SetProperty(4118u, new byte[1] { 1 }, 53515u);
		}
		SetProperty(4118u, new byte[1] { 1 }, 53664u);
		base.StartLiveView();
	}

	public override void StopLiveView()
	{
		base.StopLiveView();
		if (!CaptureInSdRam)
		{
			SetProperty(4118u, new byte[1], 53515u);
		}
		DeviceReady();
	}

	public override void CapturePhotoNoAf()
	{
		lock (Locker)
		{
			MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4117u, 53666u);
			ErrorCodes.GetException(mTPDataResponse.ErrorCode);
			if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0 && mTPDataResponse.Data[0] > 0)
			{
				if (CaptureInSdRam)
				{
					DeviceReady();
					ErrorCodes.GetException(StillImageDevice.ExecuteWithNoData(37056u, 4294967295u));
					return;
				}
				StopLiveView();
			}
			DeviceReady();
			byte b = 0;
			MTPDataResponse mTPDataResponse2 = StillImageDevice.ExecuteReadData(4117u, 53601u);
			if (mTPDataResponse2.Data != null && mTPDataResponse2.Data.Length != 0)
			{
				b = mTPDataResponse2.Data[0];
			}
			SetProperty(4118u, new byte[1] { 4 }, 53601u);
			DeviceReady();
			ErrorCodes.GetException(StillImageDevice.ExecuteWithNoData(4110u));
			if (mTPDataResponse2.Data != null && mTPDataResponse2.Data.Length != 0)
			{
				SetProperty(4118u, new byte[1] { b }, 53601u);
			}
		}
	}

	protected override PropertyValue<long> InitStillCaptureMode()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Still Capture Mode",
			IsEnabled = true,
			Code = 20499u,
			SubType = typeof(ushort)
		};
		res.AddValues("Single shot (single-frame shooting)", 1L);
		res.AddValues("Continuous high-speed shooting (CH)", 2L);
		res.AddValues("Continuous low-speed shooting (CL)", 32784L);
		res.AddValues("Self-timer", 32785L);
		res.AddValues("Mirror-up", 32786L);
		res.AddValues("Quiet shooting", 32790L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected override void InitFocusMode()
	{
		base.InitFocusMode();
		FocusMode.IsEnabled = false;
	}
}
