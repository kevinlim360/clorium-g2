using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using CameraControl.Devices.Classes;
using PortableDeviceLib;
using PortableDeviceLib.Model;

namespace CameraControl.Devices.Canon;

public class CanonBase : BaseMTPCamera
{
	public const int CONST_CMD_CANON_EOS_RemoteRelease = 37135;

	public const int CONST_CMD_CANON_EOS_BulbStart = 37157;

	public const int CONST_CMD_CANON_EOS_BulbEnd = 37158;

	public const int CONST_CMD_CANON_EOS_SetEventMode = 37141;

	public const int CONST_CMD_CANON_EOS_SetRemoteMode = 37140;

	public const int CONST_CMD_CANON_EOS_GetEvent = 37142;

	public const int CONST_CMD_CANON_EOS_DoAf = 37204;

	public const int CONST_CMD_CANON_EOS_GetViewFinderData = 37203;

	public const int CONST_CMD_CANON_EOS_GetObjectInfo = 37123;

	public const int CONST_CMD_CANON_EOS_SetDevicePropValueEx = 37136;

	public const int CONST_CMD_CANON_EOS_RequestDevicePropValue = 37129;

	public const int CONST_PROP_EOS_ShutterSpeed = 53506;

	public const int CONST_PROP_EOS_LiveView = 53680;

	public const int CONST_Event_CANON_EOS_PropValueChanged = 49545;

	public const int CONST_Event_CANON_EOS_ObjectAddedEx = 49537;

	protected Dictionary<uint, string> _shutterTable = new Dictionary<uint, string>
	{
		{ 0u, "30" },
		{ 1u, "25" },
		{ 2u, "20" },
		{ 3u, "15" },
		{ 4u, "13" },
		{ 5u, "10" },
		{ 6u, "8" },
		{ 7u, "6" },
		{ 8u, "5" },
		{ 9u, "4" },
		{ 10u, "3.2" },
		{ 11u, "2.5" },
		{ 12u, "2" },
		{ 13u, "1.6" },
		{ 14u, "1.3" },
		{ 15u, "1" },
		{ 16u, "0.8" },
		{ 17u, "0.6" },
		{ 18u, "0.5" },
		{ 19u, "0.4" },
		{ 20u, "0.3" },
		{ 21u, "1/4" },
		{ 22u, "1/5" },
		{ 23u, "1/6" },
		{ 24u, "1/8" },
		{ 25u, "1/10" },
		{ 26u, "1/13" },
		{ 27u, "1/15" },
		{ 28u, "1/20" },
		{ 29u, "1/25" },
		{ 30u, "1/30" },
		{ 31u, "1/40" },
		{ 32u, "1/50" },
		{ 33u, "1/60" },
		{ 34u, "1/80" },
		{ 35u, "1/100" },
		{ 36u, "1/125" },
		{ 37u, "1/160" },
		{ 38u, "1/200" },
		{ 39u, "1/250" },
		{ 40u, "1/320" },
		{ 41u, "1/400" },
		{ 42u, "1/500" },
		{ 43u, "1/640" },
		{ 44u, "1/800" },
		{ 45u, "1/1000" },
		{ 46u, "1/1250" },
		{ 47u, "1/1600" },
		{ 48u, "1/2000" },
		{ 49u, "1/2500" },
		{ 50u, "1/3200" },
		{ 51u, "1/4000" },
		{ 52u, "1/5000" },
		{ 53u, "1/6400" },
		{ 54u, "1/8000" }
	};

	public CanonBase()
	{
		_timer.AutoReset = true;
		_timer.Elapsed += _timer_Elapsed;
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		_timer.Stop();
		try
		{
			lock (Locker)
			{
				Thread thread = new Thread(getEvent);
				thread.Start();
			}
		}
		catch (Exception)
		{
		}
	}

	private void getEvent()
	{
		try
		{
			if (_eventIsbusy)
			{
				return;
			}
			_eventIsbusy = true;
			MTPDataResponse mTPDataResponse = ExecuteReadDataEx(37142u);
			if (mTPDataResponse.Data == null)
			{
				Log.Debug("Get event error :" + mTPDataResponse.ErrorCode.ToString("X"));
				return;
			}
			int num = BitConverter.ToInt16(mTPDataResponse.Data, 0);
			Log.Debug("Number of events " + num);
			if (num > 0)
			{
				Console.WriteLine("Event queue length " + num);
				for (int i = 0; i < num; i++)
				{
					try
					{
						uint num2 = BitConverter.ToUInt16(mTPDataResponse.Data, 6 * i + 2);
						ushort num3 = BitConverter.ToUInt16(mTPDataResponse.Data, 6 * i + 4);
						int num4 = BitConverter.ToInt32(mTPDataResponse.Data, 6 * i + 4);
						switch (num2)
						{
						case 49545u:
							Log.Debug("EOS property changed " + num3.ToString("X"));
							break;
						case 16386u:
						case 49409u:
						case 49537u:
							try
							{
								MTPDataResponse mTPDataResponse2 = ExecuteReadDataEx(37123u, (uint)num4);
								string text = "DSC_0000.JPG";
								if (mTPDataResponse2.Data != null)
								{
									text = Encoding.Unicode.GetString(mTPDataResponse2.Data, 53, 24);
									if (text.Contains("\0"))
									{
										text = text.Split(new char[1])[0];
									}
								}
								else
								{
									Log.Error("Error getting file name");
								}
								Log.Debug("File transfer " + text);
								PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
								{
									WiaImageItem = null,
									EventArgs = new PortableDeviceEventArgs(new PortableDeviceEventType
									{
										ObjectHandle = (uint)num4
									}),
									CameraDevice = this,
									FileName = text,
									Handle = (uint)num4
								};
								OnPhotoCapture(this, args);
							}
							catch (Exception exception)
							{
								Log.Error("Object added error", exception);
							}
							break;
						default:
							Log.Debug("Unknown event code :" + num2.ToString("X") + "|" + num4.ToString("X"));
							break;
						}
					}
					catch (Exception exception2)
					{
						Log.Error("Event queue processing error ", exception2);
					}
				}
			}
		}
		catch (InvalidComObjectException)
		{
			return;
		}
		catch (Exception)
		{
		}
		_eventIsbusy = false;
		_timer.Start();
	}

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		StillImageDevice = deviceDescriptor.StillImageDevice;
		DeviceName = StillImageDevice.Model;
		Manufacturer = StillImageDevice.Manufacturer;
		Capabilities.Add(CapabilityEnum.Bulb);
		Capabilities.Add(CapabilityEnum.LiveView);
		InitShutterSpeed();
		IsConnected = true;
		ExecuteWithNoData(37140u, 1u);
		ExecuteWithNoData(37141u, 1u);
		_timer.Start();
		return true;
	}

	private void InitShutterSpeed()
	{
		ShutterSpeed = new PropertyValue<long>();
		ShutterSpeed.Name = "ShutterSpeed";
		ShutterSpeed.ValueChanged += ShutterSpeed_ValueChanged;
		ReInitShutterSpeed();
	}

	private void ReInitShutterSpeed()
	{
		lock (Locker)
		{
			try
			{
				ShutterSpeed.Clear();
				foreach (KeyValuePair<uint, string> item in _shutterTable)
				{
					ShutterSpeed.AddValues(item.Value, item.Key);
				}
			}
			catch (Exception exception)
			{
				Log.Debug("EOS Shutter speed init", exception);
			}
		}
	}

	private void ShutterSpeed_ValueChanged(object sender, string key, long val)
	{
		SetProperty(37136u, BitConverter.GetBytes(val), 53506u);
		SetEOSProperty(53506u, (uint)val);
	}

	public override void CapturePhoto()
	{
		Monitor.Enter(Locker);
		try
		{
			IsBusy = true;
			ErrorCodes.GetException(ExecuteWithNoData(37135u));
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

	private void _stillImageDevice_DeviceEvent(object sender, PortableDeviceEventArgs e)
	{
		if (e.EventType.EventGuid == PortableDeviceGuids.WPD_EVENT_DEVICE_REMOVED)
		{
			StillImageDevice.Disconnect();
			StillImageDevice.IsConnected = false;
			IsConnected = false;
			OnCameraDisconnected(this, new DisconnectCameraEventArgs
			{
				StillImageDevice = StillImageDevice
			});
		}
		else
		{
			getEvent();
		}
	}

	public override void StartBulbMode()
	{
		ErrorCodes.GetException(ExecuteWithNoData(37157u));
	}

	public override void EndBulbMode()
	{
		ErrorCodes.GetException(ExecuteWithNoData(37158u));
	}

	public override LiveViewData GetLiveViewImage()
	{
		_timer.Stop();
		LiveViewData liveViewData = new LiveViewData();
		if (Monitor.TryEnter(Locker, 10))
		{
			try
			{
				liveViewData.HaveFocusData = false;
				MTPDataResponse mTPDataResponse = ExecuteReadDataEx(37203u, 1048576u);
				ErrorCodes.GetException(mTPDataResponse.ErrorCode);
				if (mTPDataResponse.Data == null)
				{
					_timer.Start();
					return null;
				}
				liveViewData.ImageDataPosition = 0;
				liveViewData.ImageData = mTPDataResponse.Data;
			}
			catch (Exception exception)
			{
				Log.Error("Error get live view image ", exception);
			}
			finally
			{
				Monitor.Exit(Locker);
			}
		}
		_timer.Start();
		return liveViewData;
	}

	public override void StartLiveView()
	{
		SetProperty(37136u, BitConverter.GetBytes(2), 53680u);
		SetEOSProperty(53680u, 2u);
	}

	public override void StopLiveView()
	{
		SetProperty(37136u, BitConverter.GetBytes(0), 53680u);
		SetEOSProperty(53680u, 0u);
	}

	public void SetEOSProperty(uint prop, uint val)
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
				uint num2 = ExecuteWithNoData(37136u, 12u, (int)prop, (int)val);
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
				Log.Debug("Error EOS set property :" + prop.ToString("X"), exception);
			}
		}
		while (flag);
		if (enabled)
		{
			_timer.Start();
		}
	}

	public override string ToString()
	{
		return base.ToString() + "\n\tType..................Canon (base)";
	}
}
