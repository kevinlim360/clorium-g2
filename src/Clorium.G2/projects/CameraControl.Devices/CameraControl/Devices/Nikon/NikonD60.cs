using System;
using System.Collections.Generic;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Nikon;

public class NikonD60 : NikonBase
{
	protected new Dictionary<int, string> _csTable = new Dictionary<int, string>
	{
		{ 0, "JPEG (BASIC)" },
		{ 1, "JPEG (NORMAL)" },
		{ 2, "JPEG (FINE)" },
		{ 3, "RAW" },
		{ 4, "RAW + JPEG (BASIC)" }
	};

	protected override void InitCompressionSetting()
	{
		try
		{
			byte b = 1;
			CompressionSetting = new PropertyValue<long>();
			CompressionSetting.ValueChanged += CompressionSetting_ValueChanged;
			MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, 20484u);
			byte b2 = mTPDataResponse.Data[b + 5];
			for (int i = 0; i < mTPDataResponse.Data.Length - (2 * b + 6 + 2); i += b)
			{
				byte b3 = mTPDataResponse.Data[2 * b + 6 + 2 + i];
				CompressionSetting.AddValues(_csTable.ContainsKey(b3) ? _csTable[b3] : b3.ToString(), b3);
			}
			CompressionSetting.ReloadValues();
			CompressionSetting.SetValue(b2);
		}
		catch (Exception)
		{
		}
	}

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		bool result = base.Init(deviceDescriptor);
		Capabilities.Clear();
		Capabilities.Add(CapabilityEnum.CaptureNoAf);
		Capabilities.Add(CapabilityEnum.CaptureInRam);
		HaveLiveView = false;
		CaptureInSdRam = false;
		PropertyChanged -= NikonBase_PropertyChanged;
		return result;
	}

	public override void ReadDeviceProperties(uint prop)
	{
		base.ReadDeviceProperties(prop);
		HaveLiveView = false;
	}
}
