using System;
using System.Collections.Generic;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Nikon;

public class NikonD80 : NikonBase
{
	protected new Dictionary<int, string> _csTable = new Dictionary<int, string>
	{
		{ 0, "JPEG (BASIC)" },
		{ 1, "JPEG (NORMAL)" },
		{ 2, "JPEG (FINE)" },
		{ 3, "RAW" },
		{ 4, "RAW + JPEG (BASIC)" },
		{ 5, "RAW + JPEG (NORMAL)" },
		{ 6, "RAW + JPEG (FINE)" }
	};

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Clear();
		Capabilities.Add(CapabilityEnum.CaptureNoAf);
		HaveLiveView = false;
		CaptureInSdRam = false;
		return result;
	}

	protected override void InitCompressionSetting()
	{
		try
		{
			byte b = 1;
			CompressionSetting = new PropertyValue<long>();
			CompressionSetting.ValueChanged += CompressionSetting_ValueChanged;
			MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, 20484u);
			int num = BitConverter.ToInt16(mTPDataResponse.Data, 2);
			byte b2 = mTPDataResponse.Data[2 * b + 5];
			byte b3 = mTPDataResponse.Data[b + 5];
			for (int i = 0; i < mTPDataResponse.Data.Length - (2 * b + 6 + 2); i += b)
			{
				byte b4 = mTPDataResponse.Data[2 * b + 6 + 2 + i];
				CompressionSetting.AddValues(_csTable.ContainsKey(b4) ? _csTable[b4] : b4.ToString(), b4);
			}
			CompressionSetting.ReloadValues();
			CompressionSetting.SetValue(b3);
		}
		catch (Exception)
		{
		}
	}

	public override void ReadDeviceProperties(uint prop)
	{
		base.ReadDeviceProperties(prop);
		HaveLiveView = false;
	}

	public override void CapturePhotoNoAf()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		lock (Locker)
		{
			byte b = 0;
			MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4117u, 53601u);
			if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0)
			{
				b = mTPDataResponse.Data[0];
			}
			ErrorCodes.GetException(StillImageDevice.ExecuteWriteData(4118u, new byte[1] { 4 }, 53601u));
			ErrorCodes.GetException(StillImageDevice.ExecuteWithNoData(4110u));
			if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0)
			{
				ErrorCodes.GetException(StillImageDevice.ExecuteWriteData(4118u, new byte[1] { b }, 53601u));
			}
		}
	}
}
