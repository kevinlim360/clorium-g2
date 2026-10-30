using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Custom;

public class CustomDevice : BaseMTPCamera
{
	public bool Init(DeviceDescriptor deviceDescriptor, DeviceDescription description)
	{
		base.Init(deviceDescriptor);
		if (StillImageDevice is StillImageDevice stillImageDevice)
		{
			stillImageDevice.DeviceEvent += StillImageDevice_DeviceEvent;
		}
		foreach (DeviceProperty property in description.Properties)
		{
			if (string.IsNullOrEmpty(property.Name))
			{
				continue;
			}
			try
			{
				MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, property.Code);
				ErrorCodes.GetException(mTPDataResponse.ErrorCode);
				uint dataType = BitConverter.ToUInt16(mTPDataResponse.Data, 2);
				int dataLength = StaticHelper.GetDataLength(dataType);
				PropertyValue<long> propertyValue = new PropertyValue<long>
				{
					Code = property.Code,
					Name = property.Name
				};
				foreach (DevicePropertyValue value in property.Values)
				{
					propertyValue.AddValues(value.Name, value.Value);
				}
				propertyValue.ValueChanged += value_ValueChanged;
				AdvancedProperties.Add(propertyValue);
			}
			catch (Exception exception)
			{
				Log.Error("Error ger property ", exception);
			}
		}
		return true;
	}

	private void value_ValueChanged(object sender, string key, long val)
	{
		if (sender is PropertyValue<long> propertyValue)
		{
			SetProperty(4118u, BitConverter.GetBytes(val), propertyValue.Code);
		}
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

	public override void TransferFile(object o, Stream stream)
	{
		((StillImageDevice)StillImageDevice).SaveFile((string)o, stream);
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		Monitor.Enter(Locker);
		try
		{
			IsBusy = true;
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
}
