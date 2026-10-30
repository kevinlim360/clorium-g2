using System;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Nikon;

public class NikonL830 : BaseMTPCamera
{
	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		base.Init(deviceDescriptor);
		Properties.Add(InitFocalLength());
		ExposureCompensation = new PropertyValue<long>
		{
			Available = false
		};
		ExposureMeteringMode = new PropertyValue<long>
		{
			Available = false
		};
		FNumber = new PropertyValue<long>
		{
			Available = false
		};
		IsoNumber = new PropertyValue<long>
		{
			Available = false
		};
		CompressionSetting = new PropertyValue<long>
		{
			Available = false
		};
		Mode = new PropertyValue<long>
		{
			Available = false
		};
		ShutterSpeed = new PropertyValue<long>
		{
			Available = false
		};
		WhiteBalance = new PropertyValue<long>
		{
			Available = false
		};
		FocusMode = new PropertyValue<long>
		{
			Available = false
		};
		if (StillImageDevice is StillImageDevice stillImageDevice)
		{
			stillImageDevice.DeviceEvent += StillImageDevice_DeviceEvent;
		}
		return true;
	}

	protected virtual PropertyValue<long> InitFocalLength()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Focal Length",
			IsEnabled = true,
			Code = 20488u,
			SubType = typeof(uint)
		};
		res.AddValues("3500", 3500L);
		res.AddValues("4600", 4600L);
		res.AddValues("5300", 5300L);
		res.AddValues("6100", 6100L);
		res.AddValues("7300", 7300L);
		res.AddValues("8600", 8600L);
		res.AddValues("10500", 10500L);
		res.SetValue(StillImageDevice.ExecuteReadData(4117u, res.Code), notifyValuChange: false);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	private void StillImageDevice_DeviceEvent(object sender, PortableDeviceEventArgs e)
	{
		if (e.EventType.EventGuid == PortableDeviceGuids.WPD_EVENT_OBJECT_ADDED)
		{
			string iD = e.EventType.DeviceObject.ID;
			PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
			{
				WiaImageItem = null,
				CameraDevice = this,
				FileName = e.EventType.DeviceObject.Name,
				Handle = e.EventType.DeviceObject.ID
			};
			OnPhotoCapture(this, args);
		}
	}
}
