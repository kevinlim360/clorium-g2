using System;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Nikon;

public class NikonD4 : NikonBase
{
	public const int CONST_CMD_TerminateCapture = 37388;

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Clear();
		Capabilities.Add(CapabilityEnum.LiveView);
		Capabilities.Add(CapabilityEnum.Bulb);
		Capabilities.Add(CapabilityEnum.RecordMovie);
		Capabilities.Add(CapabilityEnum.CaptureInRam);
		Capabilities.Add(CapabilityEnum.CaptureNoAf);
		return result;
	}

	public override void StartBulbMode()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		DeviceReady();
		StillImageDevice.ExecuteWithNoData(37058u, 1u);
		SetProperty(4118u, BitConverter.GetBytes((ushort)1), 20494u);
		SetProperty(4118u, BitConverter.GetBytes(uint.MaxValue), 20493u);
		ErrorCodes.GetException(CaptureInSdRam ? StillImageDevice.ExecuteWithNoData(37383u, 4294967295u, 1u) : StillImageDevice.ExecuteWithNoData(37383u, 4294967295u, 0u));
	}

	public override void EndBulbMode()
	{
		lock (Locker)
		{
			DeviceReady();
			ErrorCodes.GetException(StillImageDevice.ExecuteWithNoData(37388u, default(uint), default(uint)));
			DeviceReady();
			StillImageDevice.ExecuteWithNoData(37058u, default(uint));
		}
	}

	public override void LockCamera()
	{
		StillImageDevice.ExecuteWithNoData(37058u, 1u);
	}

	public override void UnLockCamera()
	{
		StillImageDevice.ExecuteWithNoData(37058u, default(uint));
	}

	protected override PropertyValue<long> InitExposureDelay()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Exposure delay mode",
			IsEnabled = true,
			Code = 53354u
		};
		res.AddValues("3 sec", 0L);
		res.AddValues("2 sec", 1L);
		res.AddValues("One sec", 1L);
		res.AddValues("OFF", 1L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, new byte[1] { (byte)val }, res.Code);
		};
		return res;
	}

	public override void StartRecordMovie()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		CameraControl.Devices.LegacyCapturePolicy.RequireMovieRecording();

		SetProperty(4118u, new byte[1] { 1 }, 53744u);
		base.StartRecordMovie();
	}

	public override void StopRecordMovie()
	{
		base.StopRecordMovie();
		SetProperty(4118u, new byte[1], 53744u);
	}
}
