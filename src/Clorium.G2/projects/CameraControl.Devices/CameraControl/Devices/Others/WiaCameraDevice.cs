using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CameraControl.Devices.Classes;
using WIA;

namespace CameraControl.Devices.Others;

public class WiaCameraDevice : BaseCameraDevice
{
	private Dictionary<int, string> ShutterTable = new Dictionary<int, string>
	{
		{ 1, "1/6400" },
		{ 2, "1/4000" },
		{ 3, "1/3200" },
		{ 4, "1/2500" },
		{ 5, "1/2000" },
		{ 6, "1/1600" },
		{ 8, "1/1250" },
		{ 10, "1/1000" },
		{ 12, "1/800" },
		{ 15, "1/640" },
		{ 20, "1/500" },
		{ 25, "1/400" },
		{ 31, "1/320" },
		{ 40, "1/250" },
		{ 50, "1/200" },
		{ 62, "1/160" },
		{ 80, "1/125" },
		{ 100, "1/100" },
		{ 125, "1/80" },
		{ 166, "1/60" },
		{ 200, "1/50" },
		{ 250, "1/40" },
		{ 333, "1/30" },
		{ 400, "1/25" },
		{ 500, "1/20" },
		{ 666, "1/15" },
		{ 769, "1/13" },
		{ 1000, "1/10" },
		{ 1250, "1/8" },
		{ 1666, "1/6" },
		{ 2000, "1/5" },
		{ 2500, "1/4" },
		{ 3333, "1/3" },
		{ 4000, "1/2.5" },
		{ 5000, "1/2" },
		{ 6250, "1/1.6" },
		{ 7692, "1/1.3" },
		{ 10000, "1s" },
		{ 13000, "1.3s" },
		{ 16000, "1.6s" },
		{ 20000, "2s" },
		{ 25000, "2.5s" },
		{ 30000, "3s" },
		{ 40000, "4s" },
		{ 50000, "5s" },
		{ 60000, "6s" },
		{ 80000, "8s" },
		{ 100000, "10s" },
		{ 130000, "13s" },
		{ 150000, "15s" },
		{ 200000, "20s" },
		{ 250000, "25s" },
		{ 300000, "30s" },
		{ -1, "Bulb" }
	};

	private Dictionary<int, string> ExposureModeTable = new Dictionary<int, string>
	{
		{ 1, "M" },
		{ 2, "P" },
		{ 3, "A" },
		{ 4, "S" },
		{ 32784, "[Scene mode] AUTO" },
		{ 32785, "[Scene mode] Portrait" },
		{ 32786, "[Scene mode] Landscape" },
		{ 32787, "[Scene mode] Close up" },
		{ 32788, "[Scene mode] Sports" },
		{ 32790, "[Scene mode] Flash prohibition AUTO" },
		{ 32791, "[Scene mode] Child" },
		{ 32792, "[Scene mode] SCENE" },
		{ 32793, "[EffectMode] EFFECTS" }
	};

	private Dictionary<int, string> WbTable = new Dictionary<int, string>
	{
		{ 2, "Auto" },
		{ 4, "Daylight" },
		{ 5, "Fluorescent " },
		{ 6, "Incandescent" },
		{ 7, "Flash" },
		{ 32784, "Cloudy" },
		{ 32785, "Shade" },
		{ 32786, "Kelvin" },
		{ 32787, "Custom" }
	};

	private Dictionary<int, string> CSTable = new Dictionary<int, string>
	{
		{ 0, "JPEG (BASIC)" },
		{ 1, "JPEG (NORMAL)" },
		{ 2, "JPEG (FINE)" },
		{ 3, "TIFF (RGB)" },
		{ 4, "RAW" },
		{ 5, "RAW + JPEG (BASIC)" },
		{ 6, "RAW + JPEG (NORMAL)" },
		{ 7, "RAW + JPEG (FINE)" }
	};

	private Dictionary<int, string> EMMTable = new Dictionary<int, string>
	{
		{ 2, "Center-weighted metering" },
		{ 3, "Multi-pattern metering" },
		{ 4, "Spot metering" }
	};

	private Dictionary<uint, string> FMTable = new Dictionary<uint, string>
	{
		{ 1u, "[M] Manual focus" },
		{ 32784u, "[S] Single AF servo" },
		{ 32785u, "[C] Continuous AF servo" },
		{ 32786u, "[A] AF servo mode automatic switching" },
		{ 32787u, "[F] Constant AF servo" }
	};

	private string _displayName;

	internal new object Locker = new object();

	public override string DisplayName
	{
		get
		{
			if (string.IsNullOrEmpty(_displayName))
			{
				return DeviceName + " (" + SerialNumber + ")(WIA)";
			}
			return _displayName;
		}
		set
		{
			_displayName = value;
			NotifyPropertyChanged("DisplayName");
		}
	}

	private Device Device { get; set; }

	public DeviceManager DeviceManager { get; set; }

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		IsBusy = false;
		try
		{
			ConnectToWiaDevice(deviceDescriptor);
		}
		catch (Exception exception)
		{
			Log.Error("Unable to connect camera using wia driver", exception);
			return false;
		}
		DeviceManager = (DeviceManager)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("E1C5D730-7E97-4D8A-9E42-BBAE87C2059F")));
		DeviceManager.RegisterEvent("{4C8F4EF5-E14F-11D2-B326-00C04F68CE61}", deviceDescriptor.WiaId);
		new ComAwareEventInfo(typeof(_IDeviceManagerEvents_Event), "OnEvent").AddEventHandler(DeviceManager, new _IDeviceManagerEvents_OnEventEventHandler(DeviceManager_OnEvent));
		try
		{
			Device = deviceDescriptor.WiaDevice;
			Properties properties = Device.Properties;
			object Index = "Description";
			DeviceName = (dynamic)properties.get_Item(ref Index).Value;
			Properties properties2 = Device.Properties;
			Index = "Manufacturer";
			Manufacturer = (dynamic)properties2.get_Item(ref Index).Value;
			Properties properties3 = Device.Properties;
			Index = "PnP ID String";
			SerialNumber = StaticHelper.GetSerial((dynamic)properties3.get_Item(ref Index).Value);
		}
		catch (Exception exception2)
		{
			Log.Debug("Init error", exception2);
		}
		IsConnected = true;
		try
		{
			try
			{
				Properties properties4 = Device.Properties;
				object Index = "F Number";
				Property property = properties4.get_Item(ref Index);
				if (property != null)
				{
					foreach (object subTypeValue in property.SubTypeValues)
					{
						double num = (int)subTypeValue;
						string key = (num / 100.0).ToString("0.0");
						FNumber.AddValues(key, (int)num);
						FNumber.ReloadValues();
						if ((int)subTypeValue == (int)(dynamic)property.Value)
						{
							FNumber.SetValue((int)num);
						}
					}
				}
			}
			catch (COMException)
			{
				FNumber.IsEnabled = false;
			}
			try
			{
				Properties properties5 = Device.Properties;
				object Index = "Exposure Index";
				Property property2 = properties5.get_Item(ref Index);
				if (property2 != null)
				{
					foreach (object subTypeValue2 in property2.SubTypeValues)
					{
						IsoNumber.AddValues(subTypeValue2.ToString(), (int)subTypeValue2);
						IsoNumber.ReloadValues();
						if ((int)subTypeValue2 == (int)(dynamic)property2.Value)
						{
							IsoNumber.SetValue((int)subTypeValue2);
						}
					}
				}
			}
			catch (COMException)
			{
				IsoNumber.IsEnabled = false;
			}
			try
			{
				Properties properties6 = Device.Properties;
				object Index = "Exposure Time";
				Property property3 = properties6.get_Item(ref Index);
				if (property3 != null)
				{
					foreach (int subTypeValue3 in property3.SubTypeValues)
					{
						if (ShutterTable.ContainsKey(subTypeValue3))
						{
							ShutterSpeed.AddValues(ShutterTable[subTypeValue3], subTypeValue3);
						}
					}
					ShutterSpeed.ReloadValues();
					ShutterSpeed.SetValue((dynamic)property3.Value);
				}
			}
			catch (COMException)
			{
				ShutterSpeed.IsEnabled = false;
			}
			try
			{
				Properties properties7 = Device.Properties;
				object Index = "White Balance";
				Property property4 = properties7.get_Item(ref Index);
				if (property4 != null)
				{
					foreach (object subTypeValue4 in property4.SubTypeValues)
					{
						if (WbTable.ContainsKey((int)subTypeValue4))
						{
							WhiteBalance.AddValues(WbTable[(int)subTypeValue4], (int)subTypeValue4);
						}
					}
					WhiteBalance.ReloadValues();
					WhiteBalance.SetValue((dynamic)property4.Value);
				}
			}
			catch (COMException)
			{
				WhiteBalance.IsEnabled = false;
			}
			try
			{
				Properties properties8 = Device.Properties;
				object Index = "Exposure Mode";
				Property property5 = properties8.get_Item(ref Index);
				if (property5 != null)
				{
					foreach (object subTypeValue5 in property5.SubTypeValues)
					{
						if (ExposureModeTable.ContainsKey((int)subTypeValue5))
						{
							Mode.AddValues(ExposureModeTable[(int)subTypeValue5], Convert.ToUInt32(subTypeValue5));
						}
					}
					Mode.ReloadValues();
					Mode.SetValue(Convert.ToUInt32((dynamic)property5.Value));
				}
				Mode.IsEnabled = false;
			}
			catch (COMException)
			{
				Mode.IsEnabled = false;
			}
			try
			{
				Properties properties9 = Device.Properties;
				object Index = "Exposure Compensation";
				Property property6 = properties9.get_Item(ref Index);
				if (property6 != null)
				{
					foreach (object subTypeValue6 in property6.SubTypeValues)
					{
						decimal num3 = (int)subTypeValue6;
						string text = decimal.Round(num3 / 1000m, 1).ToString();
						if (num3 > 0m)
						{
							text = "+" + text;
						}
						ExposureCompensation.AddValues(text, (int)subTypeValue6);
					}
					ExposureCompensation.ReloadValues();
					ExposureCompensation.SetValue((dynamic)property6.Value);
				}
			}
			catch (COMException)
			{
				ExposureCompensation.IsEnabled = false;
			}
			try
			{
				Properties properties10 = Device.Properties;
				object Index = "Compression Setting";
				Property property7 = properties10.get_Item(ref Index);
				if (property7 != null)
				{
					foreach (object subTypeValue7 in property7.SubTypeValues)
					{
						if (CSTable.ContainsKey((int)subTypeValue7))
						{
							CompressionSetting.AddValues(CSTable[(int)subTypeValue7], (int)subTypeValue7);
						}
					}
					CompressionSetting.ReloadValues();
					CompressionSetting.SetValue((dynamic)property7.Value);
				}
			}
			catch (COMException)
			{
				CompressionSetting.IsEnabled = false;
			}
			try
			{
				Properties properties11 = Device.Properties;
				object Index = "Exposure Metering Mode";
				Property property8 = properties11.get_Item(ref Index);
				if (property8 != null)
				{
					foreach (object subTypeValue8 in property8.SubTypeValues)
					{
						if (EMMTable.ContainsKey((int)subTypeValue8))
						{
							ExposureMeteringMode.AddValues(EMMTable[(int)subTypeValue8], (int)subTypeValue8);
						}
					}
					ExposureMeteringMode.ReloadValues();
					ExposureMeteringMode.SetValue((dynamic)property8.Value);
				}
			}
			catch (COMException)
			{
				CompressionSetting.IsEnabled = false;
			}
			try
			{
				Properties properties12 = Device.Properties;
				object Index = "Focus Mode";
				Property property9 = properties12.get_Item(ref Index);
				if (property9 != null)
				{
					foreach (int subTypeValue9 in property9.SubTypeValues)
					{
						uint num4 = Convert.ToUInt16(subTypeValue9);
						if (FMTable.ContainsKey(num4))
						{
							FocusMode.AddValues(FMTable[num4], num4);
						}
					}
					FocusMode.ReloadValues();
					FocusMode.SetValue(Convert.ToUInt16((int)(dynamic)property9.Value));
				}
			}
			catch (COMException)
			{
				FocusMode.IsEnabled = false;
			}
			try
			{
				Properties properties13 = Device.Properties;
				object Index = "Battery Status";
				Battery = (dynamic)properties13.get_Item(ref Index).Value;
			}
			catch (COMException)
			{
				Battery = 0;
			}
			IsConnected = true;
		}
		catch (Exception message)
		{
			Log.Error(message);
			IsConnected = false;
		}
		HaveLiveView = true;
		return true;
	}

	private void ConnectToWiaDevice(DeviceDescriptor deviceDescriptor, int retries_left = 6)
	{
		if (deviceDescriptor.WiaDevice != null)
		{
			return;
		}
		Thread.Sleep(500);
		try
		{
			deviceDescriptor.WiaDevice = deviceDescriptor.WiaDeviceInfo.Connect();
			deviceDescriptor.CameraDevice = this;
			Thread.Sleep(250);
		}
		catch (COMException ex)
		{
			if (ex.ErrorCode == -2145320954 && retries_left > 0)
			{
				int num = 2 * (7 - retries_left);
				Thread.Sleep(1000 * num);
				Log.Debug("Connection to wia failed, Retrying to connect in " + num + " seconds");
				ConnectToWiaDevice(deviceDescriptor, retries_left - 1);
				return;
			}
			Log.Error("Could not connect to wia device.", ex);
			throw ex;
		}
	}

	private void DeviceManager_OnEvent(string eventId, string deviceId, string itemId)
	{
		Item item = Device.GetItem(itemId);
		ImageFile imageFile = (ImageFile)(dynamic)item.Transfer("{B96B3CAE-0728-11D3-9D7B-0000F81EF32E}");
		PhotoCapturedEventArgs e = new PhotoCapturedEventArgs();
		e.EventArgs = imageFile;
		e.CameraDevice = this;
		e.FileName = "00000." + imageFile.FileExtension;
		e.Handle = new object[2] { imageFile, itemId };
		PhotoCapturedEventArgs args = e;
		OnPhotoCapture(this, args);
		OnCaptureCompleted(this, new EventArgs());
	}

	public WiaCameraDevice()
	{
		FNumber = new PropertyValue<long>();
		FNumber.ValueChanged += FNumber_ValueChanged;
		IsoNumber = new PropertyValue<long>();
		IsoNumber.ValueChanged += IsoNumber_ValueChanged;
		ShutterSpeed = new PropertyValue<long>();
		ShutterSpeed.ValueChanged += ShutterSpeed_ValueChanged;
		WhiteBalance = new PropertyValue<long>();
		WhiteBalance.ValueChanged += WhiteBalance_ValueChanged;
		Mode = new PropertyValue<long>();
		Mode.ValueChanged += Mode_ValueChanged;
		CompressionSetting = new PropertyValue<long>();
		CompressionSetting.ValueChanged += CompressionSetting_ValueChanged;
		ExposureCompensation = new PropertyValue<long>();
		ExposureCompensation.ValueChanged += ExposureCompensation_ValueChanged;
		ExposureMeteringMode = new PropertyValue<long>();
		ExposureMeteringMode.ValueChanged += ExposureMeteringMode_ValueChanged;
		FocusMode = new PropertyValue<long>();
		FocusMode.IsEnabled = false;
	}

	private void ExposureMeteringMode_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			try
			{
				Properties properties = Device.Properties;
				object Index = "Exposure Metering Mode";
				Property property = properties.get_Item(ref Index);
				object pvResult = val;
				property.set_Value(ref pvResult);
			}
			catch (Exception)
			{
			}
		}
	}

	private void CompressionSetting_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			try
			{
				Properties properties = Device.Properties;
				object Index = "Compression Setting";
				Property property = properties.get_Item(ref Index);
				object pvResult = val;
				property.set_Value(ref pvResult);
			}
			catch (Exception)
			{
			}
		}
	}

	private void ExposureCompensation_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			try
			{
				Properties properties = Device.Properties;
				object Index = "Exposure Compensation";
				Property property = properties.get_Item(ref Index);
				object pvResult = val;
				property.set_Value(ref pvResult);
			}
			catch (Exception)
			{
			}
		}
	}

	private void Mode_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			switch (key)
			{
			case "M":
				ShutterSpeed.IsEnabled = true;
				FNumber.IsEnabled = true;
				break;
			case "P":
				ShutterSpeed.IsEnabled = false;
				FNumber.IsEnabled = false;
				break;
			case "A":
				ShutterSpeed.IsEnabled = false;
				FNumber.IsEnabled = true;
				break;
			case "S":
				ShutterSpeed.IsEnabled = true;
				FNumber.IsEnabled = false;
				break;
			default:
				ShutterSpeed.IsEnabled = false;
				FNumber.IsEnabled = false;
				break;
			}
			Properties properties = Device.Properties;
			object Index = "Exposure Mode";
			Property property = properties.get_Item(ref Index);
			object pvResult = val;
			property.set_Value(ref pvResult);
		}
	}

	private void WhiteBalance_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			try
			{
				Properties properties = Device.Properties;
				object Index = "White Balance";
				Property property = properties.get_Item(ref Index);
				object pvResult = val;
				property.set_Value(ref pvResult);
			}
			catch (Exception)
			{
			}
		}
	}

	private void ShutterSpeed_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			try
			{
				Properties properties = Device.Properties;
				object Index = "Exposure Time";
				Property property = properties.get_Item(ref Index);
				object pvResult = val;
				property.set_Value(ref pvResult);
			}
			catch (Exception)
			{
			}
		}
	}

	private void IsoNumber_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			try
			{
				Properties properties = Device.Properties;
				object Index = "Exposure Index";
				Property property = properties.get_Item(ref Index);
				object pvResult = val;
				property.set_Value(ref pvResult);
			}
			catch (Exception)
			{
			}
		}
	}

	private void FNumber_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			try
			{
				Properties properties = Device.Properties;
				object Index = "F Number";
				Property property = properties.get_Item(ref Index);
				object pvResult = val;
				property.set_Value(ref pvResult);
			}
			catch (Exception)
			{
			}
		}
	}

	public override void StartLiveView()
	{
	}

	public override void StopLiveView()
	{
	}

	public override LiveViewData GetLiveViewImage()
	{
		return new LiveViewData();
	}

	public override void AutoFocus()
	{
	}

	public override int Focus(int step)
	{
		return 0;
	}

	public override void Focus(int x, int y)
	{
	}

	public override void CapturePhotoNoAf()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		lock (Locker)
		{
			CapturePhoto();
		}
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		Monitor.Enter(Locker);
		try
		{
			IsBusy = true;
			Device.ExecuteCommand("{AF933CAC-ACAD-11D2-A093-00C04F72DC3C}");
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

	public override void Close()
	{
		if (Device != null)
		{
			Marshal.ReleaseComObject(Device);
		}
		Device = null;
		HaveLiveView = false;
		if (DeviceName != null)
		{
			new ComAwareEventInfo(typeof(_IDeviceManagerEvents_Event), "OnEvent").RemoveEventHandler(DeviceManager, new _IDeviceManagerEvents_OnEventEventHandler(DeviceManager_OnEvent));
		}
	}

	public override void ReadDeviceProperties(uint o)
	{
		HaveLiveView = true;
	}

	public override void TransferFile(object o, string filename)
	{
		if (((object[])o)[0] is ImageFile imageFile)
		{
			imageFile.SaveFile(filename);
		}
	}

	public override bool DeleteObject(DeviceObject deviceObject)
	{
		string text = (string)((object[])deviceObject.Handle)[1];
		for (int i = 1; i <= Device.Items.Count; i++)
		{
			if (Device.Items[i].ItemID == text)
			{
				Device.Items.Remove(i);
				break;
			}
		}
		return true;
	}

	public override string ToStringCameraData()
	{
		StringBuilder stringBuilder = new StringBuilder(base.ToString() + "\n\tType..................WIA");
		if (Device != null)
		{
			stringBuilder.AppendFormat("\n\tDevice ID.............{0}", Device.DeviceID);
		}
		return stringBuilder.ToString();
	}
}
