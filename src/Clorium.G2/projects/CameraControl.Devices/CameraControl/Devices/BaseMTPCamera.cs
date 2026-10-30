using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using CameraControl.Devices.Classes;
using CameraControl.Devices.TransferProtocol;
using CameraControl.Devices.Xml;
using PortableDeviceLib;

namespace CameraControl.Devices;

public class BaseMTPCamera : BaseCameraDevice
{
	public delegate void DeviceEventHandler(object sender, DeviceEventArgs eventArgs);

	public const int TransferRetries = 10;

	protected const string AppName = "CameraControl";

	protected const int AppMajorVersionNumber = 1;

	protected const int AppMinorVersionNumber = 0;

	public const uint CONST_CMD_GetDevicePropValue = 4117u;

	public const uint CONST_CMD_SetDevicePropValue = 4118u;

	public const uint CONST_CMD_GetDevicePropDesc = 4116u;

	public const uint CONST_CMD_GetObject = 4105u;

	public const uint CONST_CMD_GetObjectHandles = 4103u;

	public const uint CONST_CMD_GetObjectInfo = 4104u;

	public const uint CONST_CMD_GetThumb = 4106u;

	public const uint CONST_CMD_DeleteObject = 4107u;

	public const uint CONST_CMD_InitiateCapture = 4110u;

	public const uint CONST_CMD_FormatStore = 4111u;

	public const uint CONST_CMD_GetStorageIDs = 4100u;

	public const uint CONST_Event_ObjectAdded = 16386u;

	public const uint CONST_Event_ObjectAddedInSdram = 49409u;

	public const uint CONST_PROP_BatteryLevel = 20481u;

	private const int CONST_READY_TIME = 1;

	private const int CONST_LOOP_TIME = 100;

	protected ITransferProtocol StillImageDevice = null;

	protected bool DeviceIsBusy = false;

	protected System.Timers.Timer _timer = new System.Timers.Timer(100.0);

	protected bool _eventIsbusy = false;

	public event DeviceEventHandler DeviceEvent;

	public void OnDeviceEvent(DeviceEventArgs eventargs)
	{
		DeviceEvent?.Invoke(this, eventargs);
	}

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		StillImageDevice = deviceDescriptor.StillImageDevice;
		if (StillImageDevice is StillImageDevice stillImageDevice)
		{
			stillImageDevice.DeviceEvent += StillImageDevice_DeviceEvent;
		}
		IsConnected = true;
		DeviceName = StillImageDevice.Model;
		Manufacturer = StillImageDevice.Manufacturer;
		MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4117u, 20481u);
		if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0)
		{
			Battery = mTPDataResponse.Data[0];
		}
		return true;
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

	public void StopEventTimer()
	{
		while (_eventIsbusy)
		{
		}
		_timer.Stop();
	}

	public void StartEventTimer()
	{
		_timer.Start();
	}

	private void StillImageDevice_DeviceEvent(object sender, PortableDeviceEventArgs e)
	{
		if (e.EventType.EventGuid == PortableDeviceGuids.WPD_EVENT_DEVICE_REMOVED)
		{
			_timer.Stop();
			StillImageDevice.Disconnect();
			StillImageDevice.IsConnected = false;
			IsConnected = false;
			OnCameraDisconnected(this, new DisconnectCameraEventArgs
			{
				StillImageDevice = StillImageDevice
			});
		}
	}

	public override bool DeleteObject(DeviceObject deviceObject)
	{
		uint num = ExecuteWithNoData(4107u, (uint)deviceObject.Handle);
		return num == 0 || num == 8193;
	}

	public override AsyncObservableCollection<DeviceObject> GetObjects(object storageId, bool loadThumbs)
	{
		AsyncObservableCollection<DeviceObject> asyncObservableCollection = new AsyncObservableCollection<DeviceObject>();
		MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4103u, uint.MaxValue);
		if (mTPDataResponse.Data == null)
		{
			Log.Debug("Get object error :" + mTPDataResponse.ErrorCode.ToString("X"));
			ErrorCodes.GetException(mTPDataResponse.ErrorCode);
			return asyncObservableCollection;
		}
		int num = BitConverter.ToInt32(mTPDataResponse.Data, 0);
		for (int i = 0; i < num; i++)
		{
			DeviceObject deviceObject = new DeviceObject();
			uint num2 = BitConverter.ToUInt32(mTPDataResponse.Data, 4 * i + 4);
			deviceObject.Handle = num2;
			MTPDataResponse mTPDataResponse2 = ExecuteReadDataEx(4104u, num2);
			if (mTPDataResponse2.Data == null)
			{
				continue;
			}
			uint num3 = BitConverter.ToUInt16(mTPDataResponse2.Data, 4);
			if (num3 != 12288 && num3 != 14337 && num3 != 14336)
			{
				continue;
			}
			deviceObject.FileName = Encoding.Unicode.GetString(mTPDataResponse2.Data, 53, 24);
			if (deviceObject.FileName.Contains("\0"))
			{
				deviceObject.FileName = deviceObject.FileName.Split(new char[1])[0];
			}
			try
			{
				string s = Encoding.Unicode.GetString(mTPDataResponse2.Data, 80, 30);
				DateTime result = DateTime.MinValue;
				if (DateTime.TryParseExact(s, "yyyyMMddTHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
				{
					deviceObject.FileDate = result;
				}
			}
			catch (Exception)
			{
			}
			if (loadThumbs)
			{
				MTPDataResponse mTPDataResponse3 = ExecuteReadDataEx(4106u, num2);
				deviceObject.ThumbData = mTPDataResponse3.Data;
			}
			asyncObservableCollection.Add(deviceObject);
		}
		return asyncObservableCollection;
	}

	public override void TransferFile(object o, string filename)
	{
		using FileStream stream = File.Open(filename, FileMode.Create);
		TransferFile(o, stream);
	}

	public override void TransferFile(object o, Stream stream)
	{
		if (!stream.CanWrite)
		{
			throw new ArgumentException("Specified stream is not writable.", "stream");
		}
		lock (Locker)
		{
			if (o is string id)
			{
				((StillImageDevice)StillImageDevice).SaveFile(id, stream);
				return;
			}
			int num = 10;
			_timer.Stop();
			MTPDataResponse mTPDataResponse = new MTPDataResponse();
			do
			{
				try
				{
					mTPDataResponse = StillImageDevice.ExecuteReadBigData(4105u, stream, (uint total, uint current) =>
					{
						double num2 = (double)current / (double)total;
						TransferProgress = Convert.ToUInt32(num2 * 100.0);
					}, Convert.ToUInt32(o));
				}
				catch (COMException exception)
				{
					Log.Error("Transfer error", exception);
					Thread.Sleep(200);
					num--;
					continue;
				}
				break;
			}
			while (num > 0);
			_timer.Start();
			TransferProgress = 0u;
		}
	}

	public override void FormatStorage(object storageId)
	{
		MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4100u);
		if (mTPDataResponse.Data.Length > 4)
		{
			int num = BitConverter.ToInt32(mTPDataResponse.Data, 0);
			for (int i = 0; i < num; i++)
			{
				uint param = BitConverter.ToUInt32(mTPDataResponse.Data, 4 * i + 4);
				ErrorCodes.GetException(ExecuteWithNoData(4111u, param));
			}
		}
	}

	public MTPDataResponse ExecuteReadDataEx(uint code, uint param1, uint param2)
	{
		return ExecuteReadDataEx(code, param1, param2, 100, 0);
	}

	public uint ExecuteWithNoData(uint code, uint param1)
	{
		return ExecuteWithNoData(code, param1, 100, 0);
	}

	public uint ExecuteWithNoData(uint code, uint param1, uint param2, uint param3)
	{
		return ExecuteWithNoData(code, param1, param2, param3, 100, 0);
	}

	public uint ExecuteWithNoData(uint code)
	{
		return ExecuteWithNoData(code, 100, 0);
	}

	public uint ExecuteWithNoData(uint code, uint param1, int loop, int counter)
	{
		uint num = 0u;
		bool flag;
		do
		{
			flag = true;
			num = StillImageDevice.ExecuteWithNoData(code, param1);
			if ((num == 8217 || num == 2147942570u) && counter < loop)
			{
				Thread.Sleep(1);
				counter++;
				flag = false;
			}
		}
		while (!flag);
		return num;
	}

	public uint ExecuteWithNoData(uint code, uint param1, uint param2, uint param3, int loop, int counter)
	{
		uint num = 0u;
		bool flag;
		do
		{
			flag = true;
			num = StillImageDevice.ExecuteWithNoData(code, param1, param2, param3);
			if ((num == 8217 || num == 2147942570u) && counter < loop)
			{
				Thread.Sleep(1);
				counter++;
				flag = false;
			}
		}
		while (!flag);
		return num;
	}

	public uint ExecuteWithNoData(uint code, int loop, int counter)
	{
		uint num = 0u;
		bool flag;
		do
		{
			flag = true;
			num = StillImageDevice.ExecuteWithNoData(code);
			if ((num == 8217 || num == 2147942570u) && counter < loop)
			{
				Thread.Sleep(1);
				counter++;
				flag = false;
			}
		}
		while (!flag);
		return num;
	}

	public uint ExecuteWithNoData(uint code, uint param1, uint param2)
	{
		return StillImageDevice.ExecuteWithNoData(code, param1, param2);
	}

	public MTPDataResponse ExecuteReadDataEx(uint code, uint param1, uint param2, int loop, int counter)
	{
		DeviceIsBusy = true;
		MTPDataResponse mTPDataResponse = new MTPDataResponse();
		bool flag;
		do
		{
			flag = true;
			mTPDataResponse = StillImageDevice.ExecuteReadData(code, param1, param2);
			if ((mTPDataResponse.ErrorCode == 8217 || mTPDataResponse.ErrorCode == 2147942570u) && counter < loop)
			{
				Thread.Sleep(1);
				counter++;
				flag = false;
			}
		}
		while (!flag);
		DeviceIsBusy = false;
		return mTPDataResponse;
	}

	public MTPDataResponse ExecuteReadDataEx(uint code, uint param1, uint param2, uint param3, int loop, int counter)
	{
		DeviceIsBusy = true;
		MTPDataResponse mTPDataResponse = new MTPDataResponse();
		bool flag;
		do
		{
			flag = true;
			mTPDataResponse = StillImageDevice.ExecuteReadData(code, param1, param2, param3);
			if ((mTPDataResponse.ErrorCode == 8217 || mTPDataResponse.ErrorCode == 2147942570u) && counter < loop)
			{
				Thread.Sleep(1);
				counter++;
				flag = false;
			}
		}
		while (!flag);
		DeviceIsBusy = false;
		return mTPDataResponse;
	}

	public MTPDataResponse ExecuteReadDataEx(uint code, uint param1)
	{
		int num = 0;
		DeviceIsBusy = true;
		MTPDataResponse mTPDataResponse = new MTPDataResponse();
		bool flag;
		do
		{
			mTPDataResponse = StillImageDevice.ExecuteReadData(code, param1);
			flag = true;
			if ((mTPDataResponse.ErrorCode == 8217 || mTPDataResponse.ErrorCode == 2147942570u) && num < 100)
			{
				Thread.Sleep(1);
				num++;
				flag = false;
			}
		}
		while (!flag);
		DeviceIsBusy = false;
		return mTPDataResponse;
	}

	public MTPDataResponse ExecuteReadDataEx(uint code)
	{
		int num = 0;
		DeviceIsBusy = true;
		MTPDataResponse mTPDataResponse = new MTPDataResponse();
		bool flag;
		do
		{
			mTPDataResponse = StillImageDevice.ExecuteReadData(code);
			flag = true;
			if ((mTPDataResponse.ErrorCode == 8217 || mTPDataResponse.ErrorCode == 2147942570u) && num < 100)
			{
				Thread.Sleep(1);
				num++;
				flag = false;
			}
		}
		while (!flag);
		DeviceIsBusy = false;
		return mTPDataResponse;
	}

	public void SetProperty(uint code, byte[] data, uint param1)
	{
		bool enabled = _timer.Enabled;
		_timer.Stop();
		bool flag = false;
		int num = 0;
		do
		{
			if (num > 5)
			{
				return;
			}
			try
			{
				flag = false;
				uint num2 = StillImageDevice.ExecuteWriteData(code, data, param1);
				if (num2 != 0 || num2 != 8193)
				{
					if (num2 == 8217 || num2 == 2147942570u)
					{
						Thread.Sleep(50);
						flag = true;
						num++;
					}
					else
					{
						ErrorCodes.GetException(num2);
					}
				}
			}
			catch (Exception exception)
			{
				Log.Debug("Error set property :" + param1.ToString("X"), exception);
			}
		}
		while (flag);
		if (enabled)
		{
			_timer.Start();
		}
	}

	public void SetProperty(uint code, byte[] data, uint param1, uint param2)
	{
		bool enabled = _timer.Enabled;
		_timer.Stop();
		bool flag = false;
		int num = 0;
		do
		{
			if (num > 5)
			{
				return;
			}
			try
			{
				flag = false;
				uint num2 = StillImageDevice.ExecuteWriteData(code, data, param1, param2);
				if (num2 != 0 || num2 != 8193)
				{
					if (num2 == 8217 || num2 == 2147942570u)
					{
						Thread.Sleep(50);
						flag = true;
						num++;
					}
					else
					{
						ErrorCodes.GetException(num2);
					}
				}
			}
			catch (Exception exception)
			{
				Log.Debug("Error set property :" + param1.ToString("X"), exception);
			}
		}
		while (flag);
		if (enabled)
		{
			_timer.Start();
		}
	}

	public void SetProperty(uint code, byte[] data, uint param1, uint param2, uint param3)
	{
		bool enabled = _timer.Enabled;
		_timer.Stop();
		bool flag = false;
		int num = 0;
		do
		{
			if (num > 5)
			{
				return;
			}
			try
			{
				flag = false;
				uint num2 = StillImageDevice.ExecuteWriteData(code, data, param1, param2, param3);
				if (num2 != 0 || num2 != 8193)
				{
					if (num2 == 8217 || num2 == 2147942570u)
					{
						Thread.Sleep(100);
						flag = true;
						num++;
					}
					else
					{
						ErrorCodes.GetException(num2);
					}
				}
			}
			catch (Exception exception)
			{
				Log.Debug("Error set property :" + param1.ToString("X"), exception);
			}
		}
		while (flag);
		if (enabled)
		{
			_timer.Start();
		}
	}

	public static short ToInt16(byte[] value, int startIndex)
	{
		int num = (short)((value[startIndex] << 8) | value[startIndex + 1]);
		return (short)num;
	}

	public static ushort ToUInt16(byte[] value, int startIndex)
	{
		uint num = (uint)((value[startIndex] << 8) | value[startIndex + 1]);
		return (ushort)num;
	}

	public static int ToInt32(byte[] value, int startIndex)
	{
		return (value[startIndex] << 24) | (value[startIndex + 1] << 16) | (value[startIndex + 2] << 8) | value[startIndex + 3];
	}

	public decimal ToDeciaml(byte[] value, int startIndex)
	{
		int num = ToUInt16(value, startIndex);
		int num2 = ToUInt16(value, startIndex + 2);
		if (num2 == 0 && num == 0)
		{
			return 0m;
		}
		string value2 = num + "." + num2;
		return Convert.ToDecimal(value2, new CultureInfo("en-US"));
	}

	protected XmlDeviceData LoadDeviceData(MTPDataResponse res)
	{
		XmlDeviceData xmlDeviceData = new XmlDeviceData();
		ErrorCodes.GetException(res.ErrorCode);
		xmlDeviceData.Manufacturer = Manufacturer;
		int num = 8;
		int num2 = res.Data[num];
		num += num2 * 2;
		num += 3;
		int num3 = res.Data[num];
		num += 2;
		for (int i = 0; i < num3; i++)
		{
			num += 2;
			xmlDeviceData.AvaiableCommands.Add(new XmlCommandDescriptor
			{
				Code = BitConverter.ToUInt16(res.Data, num)
			});
		}
		num += 2;
		int num4 = res.Data[num];
		num += 2;
		for (int j = 0; j < num4; j++)
		{
			num += 2;
			xmlDeviceData.AvaiableEvents.Add(new XmlEventDescriptor
			{
				Code = BitConverter.ToUInt16(res.Data, num)
			});
		}
		num += 2;
		int num5 = res.Data[num];
		num += 2;
		for (int k = 0; k < num5; k++)
		{
			num += 2;
			xmlDeviceData.AvaiableProperties.Add(new XmlPropertyDescriptor
			{
				Code = BitConverter.ToUInt16(res.Data, num)
			});
		}
		try
		{
			MTPDataResponse mTPDataResponse = ExecuteReadDataEx(37066u);
			if (mTPDataResponse.Data.Length != 0)
			{
				num = 0;
				num5 = mTPDataResponse.Data[num];
				num += 2;
				for (int l = 0; l < num5; l++)
				{
					num += 2;
					xmlDeviceData.AvaiableProperties.Add(new XmlPropertyDescriptor
					{
						Code = BitConverter.ToUInt16(mTPDataResponse.Data, num)
					});
				}
			}
		}
		catch (Exception)
		{
		}
		return xmlDeviceData;
	}
}
