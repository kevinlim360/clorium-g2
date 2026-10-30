using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using CameraControl.Devices.Classes;
using CameraControl.Devices.Xml;
using PortableDeviceLib;
using PortableDeviceLib.Model;

namespace CameraControl.Devices.Nikon;

public class NikonBase : BaseMTPCamera
{
	public const uint CONST_CMD_AfDrive = 37057u;

	public const uint CONST_CMD_StartLiveView = 37377u;

	public const uint CONST_CMD_EndLiveView = 37378u;

	public const uint CONST_CMD_GetLiveViewImage = 37379u;

	public const uint CONST_CMD_InitiateCaptureRecInMedia = 37383u;

	public const uint CONST_CMD_AfAndCaptureRecInSdram = 37067u;

	public const uint CONST_CMD_InitiateCaptureRecInSdram = 37056u;

	public const uint CONST_CMD_ChangeAfArea = 37381u;

	public const uint CONST_CMD_MfDrive = 37380u;

	public const uint CONST_CMD_GetLargeThumb = 37060u;

	public const uint CONST_CMD_GetEvent = 37063u;

	public const uint CONST_CMD_DeviceReady = 37064u;

	public const uint CONST_CMD_ChangeCameraMode = 37058u;

	public const uint CONST_CMD_StartMovieRecInCard = 37386u;

	public const uint CONST_CMD_EndMovieRec = 37387u;

	public const uint CONST_PROP_ImageSize = 20483u;

	public const uint CONST_PROP_Fnumber = 20487u;

	public const uint CONST_PROP_MovieFnumber = 53673u;

	public const uint CONST_PROP_ExposureIndex = 20495u;

	public const uint CONST_PROP_ExposureIndexEx = 53428u;

	public const uint CONST_PROP_MovieExposureIndex = 53674u;

	public const uint CONST_PROP_ExposureTime = 20493u;

	public const uint CONST_PROP_ShutterSpeed = 53504u;

	public const uint CONST_PROP_MovieShutterSpeed = 53672u;

	public const uint CONST_PROP_WhiteBalance = 20485u;

	public const uint CONST_PROP_ExposureProgramMode = 20494u;

	public const uint CONST_PROP_ExposureBiasCompensation = 20496u;

	public const uint CONST_PROP_MovieExposureBiasCompensation = 53675u;

	public const uint CONST_PROP_DateTime = 20497u;

	public const uint CONST_PROP_LiveViewImageZoomRatio = 53667u;

	public const uint CONST_PROP_AFModeSelect = 53601u;

	public const uint CONST_PROP_AfModeAtLiveView = 53345u;

	public const uint CONST_PROP_CompressionSetting = 20484u;

	public const uint CONST_PROP_ExposureMeteringMode = 20491u;

	public const uint CONST_PROP_FocusMode = 20490u;

	public const uint CONST_PROP_LiveViewStatus = 53666u;

	public const uint CONST_PROP_LiveViewSelector = 53670u;

	public const uint CONST_PROP_ExposureIndicateStatus = 53681u;

	public const uint CONST_PROP_RecordingMedia = 53515u;

	public const uint CONST_PROP_NoiseReduction = 53355u;

	public const uint CONST_PROP_ApplicationMode = 53744u;

	public const uint CONST_PROP_RawCompressionType = 53270u;

	public const uint CONST_PROP_RawCompressionBitMode = 53577u;

	public const uint CONST_PROP_ActivePicCtrlItem = 53760u;

	public const uint CONST_PROP_ColorSpace = 53298u;

	public const uint CONST_PROP_WbTuneFluorescentType = 53583u;

	public const uint CONST_PROP_WbColorTemp = 53278u;

	public const uint CONST_PROP_ISOAutoHighLimit = 53635u;

	public const uint CONST_PROP_FlashCompensation = 53540u;

	public const uint CONST_Event_DevicePropChanged = 16390u;

	public const uint CONST_Event_StoreFull = 16394u;

	public const uint CONST_Event_CaptureComplete = 16397u;

	public const uint CONST_Event_CaptureCompleteRecInSdram = 49410u;

	public const uint CONST_Event_ObsoleteEvent = 49412u;

	protected Dictionary<uint, string> _isoTable = new Dictionary<uint, string>
	{
		{ 100u, "100" },
		{ 125u, "125" },
		{ 160u, "160" },
		{ 200u, "200" },
		{ 250u, "250" },
		{ 320u, "320" },
		{ 400u, "400" },
		{ 500u, "500" },
		{ 640u, "640" },
		{ 800u, "800" },
		{ 1000u, "1000" },
		{ 1250u, "1250" },
		{ 1600u, "1600" },
		{ 2000u, "2000" },
		{ 2500u, "2500" },
		{ 3200u, "3200" },
		{ 4000u, "4000" },
		{ 5000u, "5000" },
		{ 6400u, "6400" },
		{ 8000u, "Hi 0.3" },
		{ 10000u, "Hi 0.7" },
		{ 12800u, "Hi 1" },
		{ 25600u, "Hi 2" }
	};

	protected Dictionary<byte, string> _autoIsoTable = new Dictionary<byte, string>
	{
		{ 0, "200" },
		{ 1, "400" },
		{ 2, "800" },
		{ 3, "1600" },
		{ 4, "3200" },
		{ 5, "6400" },
		{ 6, "Hi 1.7" },
		{ 7, "Hi 2" }
	};

	protected Dictionary<uint, string> _shutterTable = new Dictionary<uint, string>
	{
		{ 1u, "1/6400" },
		{ 2u, "1/4000" },
		{ 3u, "1/3200" },
		{ 4u, "1/2500" },
		{ 5u, "1/2000" },
		{ 6u, "1/1600" },
		{ 8u, "1/1250" },
		{ 10u, "1/1000" },
		{ 12u, "1/800" },
		{ 13u, "1/750" },
		{ 15u, "1/640" },
		{ 20u, "1/500" },
		{ 25u, "1/400" },
		{ 28u, "1/350" },
		{ 31u, "1/320" },
		{ 40u, "1/250" },
		{ 50u, "1/200" },
		{ 55u, "1/180" },
		{ 62u, "1/160" },
		{ 80u, "1/125" },
		{ 100u, "1/100" },
		{ 111u, "1/90" },
		{ 125u, "1/80" },
		{ 166u, "1/60" },
		{ 200u, "1/50" },
		{ 222u, "1/45" },
		{ 250u, "1/40" },
		{ 333u, "1/30" },
		{ 400u, "1/25" },
		{ 500u, "1/20" },
		{ 666u, "1/15" },
		{ 769u, "1/13" },
		{ 1000u, "1/10" },
		{ 1250u, "1/8" },
		{ 1666u, "1/6" },
		{ 2000u, "1/5" },
		{ 2500u, "1/4" },
		{ 3333u, "1/3" },
		{ 4000u, "1/2.5" },
		{ 5000u, "1/2" },
		{ 6250u, "1/1.6" },
		{ 6666u, "1/1.5" },
		{ 7692u, "1/1.3" },
		{ 10000u, "1s" },
		{ 13000u, "1.3s" },
		{ 15000u, "1.5s" },
		{ 16000u, "1.6s" },
		{ 20000u, "2s" },
		{ 25000u, "2.5s" },
		{ 30000u, "3s" },
		{ 40000u, "4s" },
		{ 50000u, "5s" },
		{ 60000u, "6s" },
		{ 80000u, "8s" },
		{ 100000u, "10s" },
		{ 130000u, "13s" },
		{ 150000u, "15s" },
		{ 200000u, "20s" },
		{ 250000u, "25s" },
		{ 300000u, "30s" },
		{
			uint.MaxValue,
			"Bulb"
		}
	};

	private Dictionary<int, string> _exposureModeTable = new Dictionary<int, string>
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

	private Dictionary<uint, string> _wbTable = new Dictionary<uint, string>
	{
		{ 2u, "Auto" },
		{ 4u, "Daylight" },
		{ 5u, "Fluorescent" },
		{ 6u, "Incandescent" },
		{ 7u, "Flash" },
		{ 32784u, "Cloudy" },
		{ 32785u, "Shade" },
		{ 32786u, "Kelvin" },
		{ 32787u, "Custom" },
		{ 32790u, "Natural Light Auto" }
	};

	protected Dictionary<int, string> _csTable = new Dictionary<int, string>
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

	private Dictionary<int, string> _emmTable = new Dictionary<int, string>
	{
		{ 2, "Center-weighted metering" },
		{ 3, "Multi-pattern metering" },
		{ 4, "Spot metering" },
		{ 32784, "Highlight-weighted" }
	};

	private Dictionary<uint, string> _fmTable = new Dictionary<uint, string>
	{
		{ 1u, "[M] Manual focus" },
		{ 32784u, "[S] Single AF servo" },
		{ 32785u, "[C] Continuous AF servo" },
		{ 32786u, "[A] AF servo mode automatic switching" },
		{ 32787u, "[F] Constant AF servo" }
	};

	private DateTime _dateTime;

	private bool _liveViewOn;

	private bool _liveViewMovieOn;

	public bool LiveViewOn
	{
		get
		{
			return _liveViewOn;
		}
		set
		{
			_liveViewOn = value;
			FocusMode = (LiveViewOn ? LiveViewFocusMode : NormalFocusMode);
			SetMovieMode();
		}
	}

	public bool LiveViewMovieOn
	{
		get
		{
			return _liveViewMovieOn;
		}
		set
		{
			_liveViewMovieOn = value;
			SetMovieMode();
		}
	}

	public PropertyValue<long> LiveViewFocusMode { get; set; }

	public PropertyValue<long> NormalFocusMode { get; set; }

	public PropertyValue<long> NormalFNumber { get; set; }

	public PropertyValue<long> MovieFNumber { get; set; }

	public PropertyValue<long> NormalIsoNumber { get; set; }

	public PropertyValue<long> MovieIsoNumber { get; set; }

	public PropertyValue<long> NormalShutterSpeed { get; set; }

	public PropertyValue<long> MovieShutterSpeed { get; set; }

	public PropertyValue<long> NormalExposureCompensation { get; set; }

	public PropertyValue<long> MovieExposureCompensation { get; set; }

	public override DateTime DateTime
	{
		get
		{
			return _dateTime;
		}
		set
		{
			_dateTime = value;
			try
			{
				string s = _dateTime.ToString("yyyyMMddTHHmmss");
				List<byte> list = new List<byte>();
				List<byte> list2 = new List<byte>();
				list2.Add(16);
				list.AddRange(Encoding.UTF8.GetBytes(s));
				foreach (byte item in list)
				{
					list2.Add(item);
					list2.Add(0);
				}
				list2.Add(0);
				list2.Add(0);
				SetProperty(4118u, list2.ToArray(), 20497u);
			}
			catch (Exception exception)
			{
				Log.Error("Error set camera DateTime", exception);
			}
			NotifyPropertyChanged("DateTime");
		}
	}

	public NikonBase()
	{
		_timer.AutoReset = true;
		_timer.Elapsed += _timer_Elapsed;
		SlowDownEventTimer();
	}

	private void SetMovieMode()
	{
		if (LiveViewOn && LiveViewMovieOn)
		{
			FNumber = MovieFNumber;
			IsoNumber = MovieIsoNumber;
			ShutterSpeed = MovieShutterSpeed;
			ExposureCompensation = MovieExposureCompensation;
		}
		else
		{
			FNumber = NormalFNumber;
			IsoNumber = NormalIsoNumber;
			ShutterSpeed = NormalShutterSpeed;
			ExposureCompensation = NormalExposureCompensation;
		}
	}

	private void SlowDownEventTimer()
	{
		_timer.Interval = 100.0;
	}

	private void SpeedUpEventTimer()
	{
		_timer.Interval = 100.0;
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		try
		{
			_timer.Stop();
			lock (Locker)
			{
				ThreadPool.QueueUserWorkItem(GetEvent);
			}
		}
		catch (Exception)
		{
		}
	}

	public PictureControl GetPictureControl(byte slotnum)
	{
		PictureControl pictureControl = new PictureControl();
		MTPDataResponse mTPDataResponse = ExecuteReadDataEx(37068u, slotnum, 0u);
		if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length > 30)
		{
			pictureControl.IsLoaded = true;
			pictureControl.ItemNumber = slotnum;
			pictureControl.Monocrome = mTPDataResponse.Data[1] == 1;
			pictureControl.CustomFlag = mTPDataResponse.Data[2];
			string text = Encoding.ASCII.GetString(mTPDataResponse.Data, 3, 20);
			pictureControl.RegistrationName = (text.Contains("\0") ? text.Split(new char[1])[0] : text);
			if (!pictureControl.Monocrome)
			{
				pictureControl.QuickAdjustFlag = mTPDataResponse.Data[23];
				pictureControl.QuickAdjust = (sbyte)mTPDataResponse.Data[24];
				pictureControl.Saturation = (sbyte)mTPDataResponse.Data[25];
				pictureControl.Hue = (sbyte)mTPDataResponse.Data[26];
			}
			else
			{
				pictureControl.FilterEffects = mTPDataResponse.Data[23];
				pictureControl.Toning = mTPDataResponse.Data[24];
				pictureControl.ToningDensity = mTPDataResponse.Data[25];
			}
			pictureControl.Sharpening = (sbyte)mTPDataResponse.Data[27];
			pictureControl.Contrast = (sbyte)mTPDataResponse.Data[28];
			pictureControl.Brightness = (sbyte)mTPDataResponse.Data[29];
			pictureControl.CustomCurveFlag = mTPDataResponse.Data[30];
			if (pictureControl.CustomCurveFlag == 1)
			{
				mTPDataResponse.Data.CopyTo(pictureControl.CustomCurveData, 31);
			}
			return pictureControl;
		}
		return null;
	}

	public void SetPictureControl(PictureControl control)
	{
	}

	protected virtual PropertyValue<long> InitHostMode()
	{
		PropertyValue<long> propertyValue = new PropertyValue<long>
		{
			Name = "Lock",
			IsEnabled = true
		};
		propertyValue.AddValues("OFF", 0L);
		propertyValue.AddValues("ON", 1L);
		propertyValue.ReloadValues();
		propertyValue.Value = "OFF";
		propertyValue.ValueChanged += (object sender, string key, long val) =>
		{
			ExecuteWithNoData(37058u, (!(key == "OFF")) ? 1u : 0u);
			Mode.IsEnabled = key == "ON";
			GetEvent(null);
			ReadDeviceProperties(20494u);
		};
		return propertyValue;
	}

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		try
		{
			IsBusy = true;
			Capabilities.Add(CapabilityEnum.CaptureInRam);
			Capabilities.Add(CapabilityEnum.CaptureNoAf);
			StillImageDevice = deviceDescriptor.StillImageDevice;
			if (StillImageDevice is StillImageDevice stillImageDevice)
			{
				stillImageDevice.DeviceEvent += _stillImageDevice_DeviceEvent;
			}
			HaveLiveView = true;
			DeviceReady();
			DeviceName = StillImageDevice.Model;
			Manufacturer = StillImageDevice.Manufacturer;
			IsConnected = true;
			CaptureInSdRam = true;
			PropertyChanged += NikonBase_PropertyChanged;
			string serialNumber = StillImageDevice.SerialNumber;
			Log.Debug("Serial number" + serialNumber);
			if (serialNumber != null && serialNumber.Length >= 7)
			{
				SerialNumber = serialNumber.Substring(0, 7);
				if (SerialNumber == "0000000")
				{
					SerialNumber = serialNumber.Substring(serialNumber.Length - 7, 7);
				}
			}
			Thread thread = new Thread(LoadProperties)
			{
				Priority = ThreadPriority.Lowest
			};
			thread.Start();
		}
		catch (Exception exception)
		{
			Log.Error("Error initialize device", exception);
		}
		return true;
	}

	public void LoadProperties()
	{
		try
		{
			InitIso();
			InitShutterSpeed();
			InitFNumber();
			ReadDeviceProperties(53670u);
			InitMode();
			InitWhiteBalance();
			InitExposureCompensation();
			InitCompressionSetting();
			InitExposureMeteringMode();
			InitFocusMode();
			InitOther();
			ReadDeviceProperties(20481u);
			ReadDeviceProperties(53681u);
			AddAditionalProps();
			ReInitShutterSpeed();
			ReadDeviceProperties(53666u);
			_timer.Start();
			OnCameraInitDone();
		}
		catch (Exception exception)
		{
			Log.Error("Error load device properties", exception);
		}
		IsBusy = false;
	}

	public virtual void AddAditionalProps()
	{
		AdvancedProperties.Add(InitImageSize());
		AdvancedProperties.Add(InitRawQuality());
		AdvancedProperties.Add(InitBurstNumber());
		AdvancedProperties.Add(InitStillCaptureMode());
		AdvancedProperties.Add(InitOnOffProperty("Auto Iso", 53332u));
		AdvancedProperties.Add(InitFlash());
		AdvancedProperties.Add(InitOnOffProperty("Long exp NR", 53355u));
		AdvancedProperties.Add(InitNRHiIso());
		AdvancedProperties.Add(InitExposureDelay());
		AdvancedProperties.Add(InitLock());
		AdvancedProperties.Add(InitPictControl());
		AdvancedProperties.Add(InitRawBit());
		AdvancedProperties.Add(InitColorSpace());
		AdvancedProperties.Add(InitWbTuneFluorescentType());
		AdvancedProperties.Add(InitWbColorTemp());
		AdvancedProperties.Add(InitOnOffProperty("Application mode", 53744u));
		AdvancedProperties.Add(InitAutoIsoHight());
		AdvancedProperties.Add(CenterWeightedExRange());
		AdvancedProperties.Add(FlashCompensation());
		AdvancedProperties.Add(FlashSyncSpeed());
		AdvancedProperties.Add(CaptureAreaCrop());
		AdvancedProperties.Add(ActiveDLighting());
		AdvancedProperties.Add(HDRMode());
		AdvancedProperties.Add(HDREv());
		AdvancedProperties.Add(HDRSmoothing());
		AdvancedProperties.Add(ActiveSlot());
		AdvancedProperties.Add(LensSort());
		AdvancedProperties.Add(InitHostMode());
		try
		{
			XmlDeviceData xmlDeviceData = LoadDeviceData(ExecuteReadDataEx(4097u));
			foreach (PropertyValue<long> advancedProperty in AdvancedProperties)
			{
				if (!xmlDeviceData.PropertyExist(advancedProperty.Code))
				{
					advancedProperty.Available = false;
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("Unable to check advanced proprties", exception);
		}
		foreach (PropertyValue<long> advancedProperty2 in AdvancedProperties)
		{
			ReadDeviceProperties(advancedProperty2.Code);
		}
	}

	protected virtual PropertyValue<long> LensSort()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Lens Sort",
			IsEnabled = false,
			Code = 53473u,
			SubType = typeof(sbyte)
		};
		res.AddValues("Not mounted", 0L);
		res.AddValues("CPU lens mounted", 1L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> ActiveSlot()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Active Slot",
			IsEnabled = false,
			Code = 53746u,
			SubType = typeof(sbyte)
		};
		res.AddValues("Card not inserted", 0L);
		res.AddValues("CF slot", 1L);
		res.AddValues("SD slot", 2L);
		res.AddValues("CF slot & SD slot", 3L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> FlashSyncSpeed()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Flash sync speed",
			IsEnabled = true,
			Code = 53364u,
			SubType = typeof(sbyte)
		};
		res.AddValues("1/320 sec. (auto FP)", 0L);
		res.AddValues("1/250 sec. (auto FP)", 1L);
		res.AddValues("1/250 sec.", 2L);
		res.AddValues("1/200 sec.", 3L);
		res.AddValues("1/160 sec.", 4L);
		res.AddValues("1/125 sec.", 5L);
		res.AddValues("1/100 sec.", 6L);
		res.AddValues("1/80 sec.", 7L);
		res.AddValues("1/60 sec.", 8L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> HDRSmoothing()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "HDR intensity",
			IsEnabled = true,
			Code = 53554u,
			SubType = typeof(sbyte)
		};
		res.AddValues("High", 0L);
		res.AddValues("Normal", 1L);
		res.AddValues("Low", 2L);
		res.AddValues("Auto", 3L);
		res.AddValues("Extra high", 4L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> HDREv()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "HDR Exposure deviation",
			IsEnabled = true,
			Code = 53553u,
			SubType = typeof(sbyte)
		};
		res.AddValues("Auto", 0L);
		res.AddValues("1 EV", 1L);
		res.AddValues("2 EV", 2L);
		res.AddValues("3 EV", 3L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> HDRMode()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "HDRMode",
			IsEnabled = true,
			Code = 53552u,
			SubType = typeof(sbyte)
		};
		res.AddValues("OFF", 0L);
		res.AddValues("ON (single)", 1L);
		res.AddValues("ON (sequence)", 2L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> ActiveDLighting()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Active D-Lighting",
			IsEnabled = true,
			Code = 53582u,
			SubType = typeof(sbyte)
		};
		res.AddValues("Extra high", 0L);
		res.AddValues("High", 1L);
		res.AddValues("Normal", 2L);
		res.AddValues("Low", 3L);
		res.AddValues("Not performed", 4L);
		res.AddValues("Auto", 5L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> CaptureAreaCrop()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Capture area crop",
			IsEnabled = true,
			Code = 53296u,
			SubType = typeof(sbyte)
		};
		res.AddValues("FX format (36x24)", 0L);
		res.AddValues("DX format (24x16)", 1L);
		res.AddValues("5:4 (30x24)", 2L);
		res.AddValues("1.2x (30x20)", 3L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> FlashCompensation()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Ext. flash compensation",
			IsEnabled = true,
			Code = 53540u,
			SubType = typeof(sbyte)
		};
		for (decimal num = -18m; num <= 18m; ++num)
		{
			if (num > 0m)
			{
				res.AddValues("+" + decimal.Round(num / 6m, 1).ToString("0.0", CultureInfo.CreateSpecificCulture("en-US")), (long)num);
			}
			else
			{
				res.AddValues(decimal.Round(num / 6m, 1).ToString("0.0", CultureInfo.CreateSpecificCulture("en-US")), (long)num);
			}
		}
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes((short)(sbyte)val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> CenterWeightedExRange()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Center-weighted area",
			IsEnabled = true,
			Code = 53337u,
			SubType = typeof(byte)
		};
		res.AddValues("6 mm", 0L);
		res.AddValues("8 mm", 1L);
		res.AddValues("10 mm", 2L);
		res.AddValues("10 mm", 3L);
		res.AddValues("Average on the entire screen", 4L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	public virtual PropertyValue<long> InitAutoIsoHight()
	{
		PropertyValue<long> res = new PropertyValue<long>();
		try
		{
			DeviceReady();
			byte b = 1;
			res.Name = "ISO Auto High Limit";
			res.SubType = typeof(byte);
			res.Code = 53635u;
			res.ValueChanged += (object sender, string key, long val) =>
			{
				SetProperty(4118u, new byte[1] { (byte)val }, res.Code);
			};
			MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, 53635u);
			int num = BitConverter.ToInt16(mTPDataResponse.Data, 2);
			byte b2 = mTPDataResponse.Data[2 * b + 5];
			byte b3 = mTPDataResponse.Data[b + 5];
			foreach (KeyValuePair<byte, string> item in _autoIsoTable)
			{
				res.AddValues(item.Value, item.Key);
			}
			res.ReloadValues();
			res.SetValue(b3, notifyValuChange: false);
		}
		catch (Exception)
		{
		}
		return res;
	}

	protected virtual PropertyValue<long> InitColorSpace()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Color space",
			IsEnabled = true,
			Code = 53298u,
			SubType = typeof(byte)
		};
		res.AddValues("sRGB", 0L);
		res.AddValues("Adobe RGB", 1L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitWbTuneFluorescentType()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Fluorescent light type",
			IsEnabled = (WhiteBalance.NumericValue == 5),
			Code = 53583u,
			SubType = typeof(byte),
			DisableIfWrongValue = false
		};
		res.AddValues("Sodium lamp mixed light", 0L);
		res.AddValues("Cool white fluorescent lamp", 1L);
		res.AddValues("Warm white fluorescent lamp", 2L);
		res.AddValues("White fluorescent lamp", 3L);
		res.AddValues("Day white fluorescent lamp", 4L);
		res.AddValues("Daylight fluorescent lamp", 5L);
		res.AddValues("High color-temperature mercury lamp", 6L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitWbColorTemp()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Temperature",
			IsEnabled = (WhiteBalance.NumericValue == 32786),
			Code = 53278u,
			SubType = typeof(byte),
			DisableIfWrongValue = false
		};
		res.AddValues("2500 K", 0L);
		res.AddValues("2560 K", 1L);
		res.AddValues("2630 K", 2L);
		res.AddValues("2700 K", 3L);
		res.AddValues("2780 K", 4L);
		res.AddValues("2860 K", 5L);
		res.AddValues("2940 K", 6L);
		res.AddValues("3030 K", 7L);
		res.AddValues("3130 K", 8L);
		res.AddValues("3230 K", 9L);
		res.AddValues("3330 K", 10L);
		res.AddValues("3450 K", 11L);
		res.AddValues("3570 K", 12L);
		res.AddValues("3700 K", 13L);
		res.AddValues("3850 K", 14L);
		res.AddValues("4000 K", 15L);
		res.AddValues("4170 K", 16L);
		res.AddValues("4350 K", 17L);
		res.AddValues("4550 K", 18L);
		res.AddValues("4760 K", 19L);
		res.AddValues("5000 K", 20L);
		res.AddValues("5260 K", 21L);
		res.AddValues("5560 K", 22L);
		res.AddValues("5880 K", 23L);
		res.AddValues("6250 K", 24L);
		res.AddValues("6670 K", 25L);
		res.AddValues("7140 K", 26L);
		res.AddValues("7690 K", 27L);
		res.AddValues("8330 K", 28L);
		res.AddValues("9090 K", 29L);
		res.AddValues("10000 K", 30L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitRawBit()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Raw Recording bit mode",
			IsEnabled = true,
			Code = 53577u,
			SubType = typeof(byte),
			DisableIfWrongValue = true
		};
		res.AddValues("12-bit recording", 0L);
		res.AddValues("14-bit recording", 1L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitPictControl()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Picture control",
			IsEnabled = true,
			Code = 53760u,
			SubType = typeof(ushort)
		};
		res.AddValues("Standard", 1L);
		res.AddValues("Neutral", 2L);
		res.AddValues("Vivid", 3L);
		res.AddValues("Monochrome", 4L);
		res.AddValues("Portrait", 5L);
		res.AddValues("Landscape", 6L);
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		try
		{
			for (byte b = 201; b < 210; b++)
			{
				PictureControl pictureControl = GetPictureControl(b);
				if (pictureControl != null && pictureControl.IsLoaded && !string.IsNullOrWhiteSpace(pictureControl.RegistrationName))
				{
					res.AddValues(pictureControl.RegistrationName, b);
				}
				else
				{
					res.AddValues("Custom picture control " + (b - 200), b);
				}
			}
		}
		catch (Exception)
		{
		}
		res.ReloadValues();
		return res;
	}

	protected virtual PropertyValue<long> InitImageSize()
	{
		PropertyValue<long> propertyValue = new PropertyValue<long>
		{
			Name = "Image Size",
			IsEnabled = true,
			Code = 20483u
		};
		propertyValue.ValueChanged += ImageSize_ValueChanged;
		MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4116u, propertyValue.Code);
		if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length > 112)
		{
			propertyValue.AddValues(Encoding.Unicode.GetString(mTPDataResponse.Data, 51, 20), 0L);
			propertyValue.AddValues(Encoding.Unicode.GetString(mTPDataResponse.Data, 72, 20), 0L);
			propertyValue.AddValues(Encoding.Unicode.GetString(mTPDataResponse.Data, 93, 20), 0L);
			propertyValue.SetValue(Encoding.Unicode.GetString(mTPDataResponse.Data, 27, 20), notifyValuChange: false);
		}
		propertyValue.ReloadValues();
		return propertyValue;
	}

	private void ImageSize_ValueChanged(object sender, string key, long val)
	{
		if (CompressionSetting == null || CompressionSetting.Value == null || !CompressionSetting.Value.Contains("RAW"))
		{
			List<byte> list = new List<byte> { 10 };
			list.AddRange(Encoding.Unicode.GetBytes(key));
			SetProperty(4118u, list.ToArray(), 20483u);
		}
	}

	protected virtual PropertyValue<long> InitRawQuality()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Raw Compression",
			IsEnabled = true,
			Code = 53270u,
			SubType = typeof(byte),
			DisableIfWrongValue = true
		};
		res.AddValues("Lossless compressed RAW", 0L);
		res.AddValues("Compressed RAW", 1L);
		res.AddValues("Uncompressed RAW", 2L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitOnOffProperty(string name, uint code)
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = name,
			IsEnabled = true,
			Code = code
		};
		res.AddValues("OFF", 0L);
		res.AddValues("ON", 1L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, new byte[1] { (byte)val }, res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitLock()
	{
		PropertyValue<long> propertyValue = new PropertyValue<long>
		{
			Name = "Lock",
			IsEnabled = true
		};
		propertyValue.AddValues("OFF", 0L);
		propertyValue.AddValues("ON", 1L);
		propertyValue.ReloadValues();
		propertyValue.Value = "OFF";
		propertyValue.ValueChanged += (object sender, string key, long val) =>
		{
			if (key == "OFF")
			{
				UnLockCamera();
			}
			else
			{
				LockCamera();
			}
		};
		return propertyValue;
	}

	protected virtual PropertyValue<long> InitFlash()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Flash",
			IsEnabled = true,
			Code = 20492u,
			SubType = typeof(ushort)
		};
		res.AddValues("Flash prohibited", 2L);
		res.AddValues("Red-eye reduction", 4L);
		res.AddValues("Normal synchronization", 32784L);
		res.AddValues("Slow synchronization", 32785L);
		res.AddValues("Rear synchronization", 32786L);
		res.AddValues("Red-eye reduction slow synchronization", 32787L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitStillCaptureMode()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Still Capture Mode",
			IsEnabled = true,
			Code = 20499u,
			SubType = typeof(ushort)
		};
		res.AddValues("Single shot (single-frame shooting)", 1L);
		res.AddValues("Continuous shot (continuous shooting)", 2L);
		res.AddValues("Self-timer", 32785L);
		res.AddValues("Quick-response remote", 32788L);
		res.AddValues("2s delayed remote", 32789L);
		res.AddValues("Quiet shooting", 32790L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitBurstNumber()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Burst Number",
			IsEnabled = true,
			Code = 20504u,
			SubType = typeof(ushort)
		};
		for (int i = 1; i < 100; i++)
		{
			res.AddValues(i.ToString(), i);
		}
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitNRHiIso()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "High ISO NR",
			IsEnabled = true,
			Code = 53360u
		};
		res.AddValues("Not performed", 0L);
		res.AddValues("Low", 1L);
		res.AddValues("Normal", 2L);
		res.AddValues("High", 3L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, new byte[1] { (byte)val }, res.Code);
		};
		return res;
	}

	protected virtual PropertyValue<long> InitExposureDelay()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Exposure delay mode",
			IsEnabled = true,
			Code = 53354u
		};
		res.AddValues("OFF", 0L);
		res.AddValues("ON", 1L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, new byte[1] { (byte)val }, res.Code);
		};
		return res;
	}

	protected void NikonBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "CaptureInSdRam")
		{
			SetProperty(4118u, (!CaptureInSdRam) ? new byte[1] : new byte[1] { 1 }, 53515u);
			ReadDeviceProperties(53515u);
		}
	}

	private void _stillImageDevice_DeviceEvent(object sender, PortableDeviceEventArgs e)
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
		else
		{
			Thread thread = new Thread(GetEvent);
			thread.Start();
		}
	}

	protected virtual void InitOther()
	{
		LiveViewImageZoomRatio = new PropertyValue<long>
		{
			Name = "LiveViewImageZoomRatio"
		};
		LiveViewImageZoomRatio.SubType = typeof(int);
		LiveViewImageZoomRatio.AddValues("All", 0L);
		LiveViewImageZoomRatio.AddValues("25%", 1L);
		LiveViewImageZoomRatio.AddValues("33%", 2L);
		LiveViewImageZoomRatio.AddValues("50%", 3L);
		LiveViewImageZoomRatio.AddValues("66%", 4L);
		LiveViewImageZoomRatio.AddValues("100%", 5L);
		LiveViewImageZoomRatio.AddValues("200%", 6L);
		LiveViewImageZoomRatio.SetValue("All");
		LiveViewImageZoomRatio.ReloadValues();
		LiveViewImageZoomRatio.ValueChanged += LiveViewImageZoomRatio_ValueChanged;
	}

	protected virtual void LiveViewImageZoomRatio_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			SetProperty(4118u, BitConverter.GetBytes(val), 53667u);
		}
	}

	protected virtual void InitIso()
	{
		lock (Locker)
		{
			NormalIsoNumber = new PropertyValue<long>();
			NormalIsoNumber.Name = "IsoNumber";
			NormalIsoNumber.SubType = typeof(int);
			NormalIsoNumber.ValueChanged += IsoNumber_ValueChanged;
			NormalIsoNumber.Clear();
			try
			{
				DeviceReady();
				MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4116u, 20495u);
				ushort num = BitConverter.ToUInt16(mTPDataResponse.Data, 7);
				for (int i = 0; i < mTPDataResponse.Data.Length - 12; i += 2)
				{
					ushort num2 = BitConverter.ToUInt16(mTPDataResponse.Data, 12 + i);
					NormalIsoNumber.AddValues(_isoTable.ContainsKey(num2) ? _isoTable[num2] : num2.ToString(), num2);
				}
				NormalIsoNumber.ReloadValues();
				NormalIsoNumber.SetValue(num, notifyValuChange: false);
				IsoNumber = NormalIsoNumber;
			}
			catch (Exception)
			{
				NormalIsoNumber.IsEnabled = false;
			}
			MovieIsoNumber = new PropertyValue<long>();
			MovieIsoNumber.Name = "IsoNumber";
			MovieIsoNumber.SubType = typeof(int);
			MovieIsoNumber.ValueChanged += MovieIsoNumber_ValueChanged;
			MovieIsoNumber.Clear();
			try
			{
				MTPDataResponse mTPDataResponse2 = ExecuteReadDataEx(4116u, 53674u);
				ushort num3 = BitConverter.ToUInt16(mTPDataResponse2.Data, 7);
				for (int j = 0; j < mTPDataResponse2.Data.Length - 12; j += 2)
				{
					ushort num4 = BitConverter.ToUInt16(mTPDataResponse2.Data, 12 + j);
					MovieIsoNumber.AddValues(_isoTable.ContainsKey(num4) ? _isoTable[num4] : num4.ToString(CultureInfo.InvariantCulture), num4);
				}
				MovieIsoNumber.ReloadValues();
				MovieIsoNumber.SetValue(num3, notifyValuChange: false);
			}
			catch (Exception)
			{
				MovieIsoNumber.IsEnabled = false;
			}
		}
	}

	private void IsoNumber_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			SetProperty(4118u, BitConverter.GetBytes((int)val), 20495u);
		}
	}

	private void MovieIsoNumber_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			SetProperty(4118u, BitConverter.GetBytes((int)val), 53674u);
		}
	}

	protected virtual void InitShutterSpeed()
	{
		NormalShutterSpeed = new PropertyValue<long>();
		NormalShutterSpeed.Name = "ShutterSpeed";
		NormalShutterSpeed.ValueChanged += ShutterSpeed_ValueChanged;
		MovieShutterSpeed = new PropertyValue<long>();
		MovieShutterSpeed.Name = "ShutterSpeed";
		MovieShutterSpeed.ValueChanged += MovieShutterSpeed_ValueChanged;
		ReInitShutterSpeed();
	}

	private void ShutterSpeed_ValueChanged(object sender, string key, long val)
	{
		if (Mode != null && (Mode.Value == "M" || Mode.Value == "S"))
		{
			if (key == "Bulb")
			{
				SetProperty(4118u, BitConverter.GetBytes(uint.MaxValue), 53504u);
			}
			else
			{
				SetProperty(4118u, BitConverter.GetBytes(val), 20493u);
			}
		}
	}

	private void MovieShutterSpeed_ValueChanged(object sender, string key, long val)
	{
		if (Mode != null && (Mode.Value == "M" || Mode.Value == "S"))
		{
			if (key == "Bulb")
			{
				SetProperty(4118u, BitConverter.GetBytes(uint.MaxValue), 53672u);
			}
			else
			{
				SetProperty(4118u, BitConverter.GetBytes(val), 53672u);
			}
		}
	}

	protected virtual void ReInitShutterSpeed()
	{
		lock (Locker)
		{
			DeviceReady();
			try
			{
				byte b = 4;
				MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, 20493u);
				if (mTPDataResponse.Data == null)
				{
					return;
				}
				NormalShutterSpeed.Clear();
				int num = BitConverter.ToInt16(mTPDataResponse.Data, 2);
				byte b2 = mTPDataResponse.Data[2 * b + 5];
				uint num2 = BitConverter.ToUInt32(mTPDataResponse.Data, b + 5);
				for (int i = 0; i < mTPDataResponse.Data.Length - (2 * b + 6 + 2); i += b)
				{
					uint num3 = BitConverter.ToUInt32(mTPDataResponse.Data, 2 * b + 6 + 2 + i);
					NormalShutterSpeed.AddValues(_shutterTable.ContainsKey(num3) ? _shutterTable[num3] : num3.ToString(), num3);
				}
				if (Mode != null && (Mode.Value == "S" || Mode.Value == "M") && !NormalShutterSpeed.Values.Contains("Bulb"))
				{
					NormalShutterSpeed.AddValues("Bulb", 4294967295L);
				}
				NormalShutterSpeed.ReloadValues();
				NormalShutterSpeed.SetValue(num2, notifyValuChange: false);
				ShutterSpeed = NormalShutterSpeed;
			}
			catch (Exception)
			{
			}
			try
			{
				byte b3 = 4;
				MTPDataResponse mTPDataResponse2 = StillImageDevice.ExecuteReadData(4116u, 53672u);
				if (mTPDataResponse2.Data != null && mTPDataResponse2.Data.Length != 0)
				{
					MovieShutterSpeed.Clear();
					int num4 = BitConverter.ToInt16(mTPDataResponse2.Data, 2);
					byte b4 = mTPDataResponse2.Data[2 * b3 + 5];
					uint num5 = BitConverter.ToUInt32(mTPDataResponse2.Data, b3 + 5);
					for (int j = 0; j < mTPDataResponse2.Data.Length - (2 * b3 + 6 + 2); j += b3)
					{
						uint num6 = BitConverter.ToUInt32(mTPDataResponse2.Data, 2 * b3 + 6 + 2 + j);
						MovieShutterSpeed.AddValues("1/" + (num6 - 65536), num6);
					}
					if (Mode != null && (Mode.Value == "S" || Mode.Value == "M") && !MovieShutterSpeed.Values.Contains("Bulb"))
					{
						MovieShutterSpeed.AddValues("Bulb", 4294967295L);
					}
					MovieShutterSpeed.SetValue(num5, notifyValuChange: false);
				}
			}
			catch (Exception)
			{
			}
		}
	}

	private void InitMode()
	{
		try
		{
			DeviceReady();
			byte b = 2;
			Mode = new PropertyValue<long>();
			Mode.SubType = typeof(uint);
			Mode.Name = "Mode";
			Mode.IsEnabled = false;
			Mode.ValueChanged += Mode_ValueChanged;
			MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, 20494u);
			int num = BitConverter.ToInt16(mTPDataResponse.Data, 2);
			byte b2 = mTPDataResponse.Data[2 * b + 5];
			ushort num2 = BitConverter.ToUInt16(mTPDataResponse.Data, b + 5);
			for (int i = 0; i < mTPDataResponse.Data.Length - (2 * b + 6 + 2); i += b)
			{
				ushort num3 = BitConverter.ToUInt16(mTPDataResponse.Data, 2 * b + 6 + 2 + i);
				Mode.AddValues(_exposureModeTable.ContainsKey(num3) ? _exposureModeTable[num3] : num3.ToString(), num3);
			}
			Mode.ReloadValues();
			Mode.SetValue(num2);
		}
		catch (Exception)
		{
		}
	}

	private void Mode_ValueChanged(object sender, string key, long val)
	{
		try
		{
			switch (key)
			{
			case "M":
				ShutterSpeed.IsEnabled = true;
				NormalFNumber.IsEnabled = true;
				break;
			case "P":
				ShutterSpeed.IsEnabled = false;
				FNumber.IsEnabled = false;
				break;
			case "A":
				ShutterSpeed.IsEnabled = false;
				NormalFNumber.IsEnabled = true;
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
			if (Mode.IsEnabled)
			{
				SetProperty(4118u, BitConverter.GetBytes(val), 20494u);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Usable to set camera mode", exception);
		}
	}

	protected virtual void InitFNumber()
	{
		NormalFNumber = new PropertyValue<long>
		{
			IsEnabled = true,
			Name = "FNumber"
		};
		NormalFNumber.ValueChanged += NormalFNumber_ValueChanged;
		NormalFNumber.SubType = typeof(int);
		MovieFNumber = new PropertyValue<long>
		{
			IsEnabled = true,
			Name = "FNumber"
		};
		MovieFNumber.ValueChanged += MovieFNumber_ValueChanged;
		MovieFNumber.SubType = typeof(int);
		ReInitFNumber(trigervaluchange: false);
	}

	private void MovieFNumber_ValueChanged(object sender, string key, long val)
	{
		if (Mode != null && (Mode.Value == "A" || Mode.Value == "M"))
		{
			SetProperty(4118u, BitConverter.GetBytes((int)val), 53673u);
		}
	}

	private void NormalFNumber_ValueChanged(object sender, string key, long val)
	{
		if (Mode != null && (Mode.Value == "A" || Mode.Value == "M"))
		{
			SetProperty(4118u, BitConverter.GetBytes((int)val), 20487u);
		}
	}

	protected void ReInitFNumber(bool trigervaluchange)
	{
		try
		{
			DeviceReady();
			MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, 20487u);
			if (mTPDataResponse.Data != null)
			{
				int num = BitConverter.ToInt16(mTPDataResponse.Data, 2);
				byte b = mTPDataResponse.Data[9];
				ushort num2 = BitConverter.ToUInt16(mTPDataResponse.Data, 7);
				NormalFNumber.Clear();
				for (int i = 0; i < mTPDataResponse.Data.Length - 12; i += 2)
				{
					ushort num3 = BitConverter.ToUInt16(mTPDataResponse.Data, 12 + i);
					string key = ((double)(int)num3 / 100.0).ToString("0.0", CultureInfo.CreateSpecificCulture("en-US"));
					NormalFNumber.AddValues(key, num3);
				}
				NormalFNumber.ReloadValues();
				NormalFNumber.SetValue(num2, trigervaluchange);
				FNumber = NormalFNumber;
			}
			mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, 53673u);
			if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0)
			{
				MovieFNumber.Clear();
				int num4 = BitConverter.ToInt16(mTPDataResponse.Data, 2);
				byte b2 = mTPDataResponse.Data[9];
				ushort num5 = BitConverter.ToUInt16(mTPDataResponse.Data, 7);
				for (int j = 0; j < mTPDataResponse.Data.Length - 12; j += 2)
				{
					ushort num6 = BitConverter.ToUInt16(mTPDataResponse.Data, 12 + j);
					string key2 = ((double)(int)num6 / 100.0).ToString("0.0", CultureInfo.CreateSpecificCulture("en-US"));
					MovieFNumber.AddValues(key2, num6);
				}
				MovieFNumber.ReloadValues();
				MovieFNumber.SetValue(num5, trigervaluchange);
			}
		}
		catch (Exception)
		{
		}
	}

	private void InitWhiteBalance()
	{
		try
		{
			DeviceReady();
			byte b = 2;
			WhiteBalance = new PropertyValue<long>();
			WhiteBalance.Name = "WhiteBalance";
			WhiteBalance.ValueChanged += WhiteBalance_ValueChanged;
			MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4116u, 20485u);
			if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0)
			{
				int num = BitConverter.ToInt16(mTPDataResponse.Data, 2);
				byte b2 = mTPDataResponse.Data[2 * b + 5];
				ushort num2 = BitConverter.ToUInt16(mTPDataResponse.Data, b + 5);
				for (int i = 0; i < mTPDataResponse.Data.Length - (2 * b + 6 + 2); i += b)
				{
					ushort num3 = BitConverter.ToUInt16(mTPDataResponse.Data, 2 * b + 6 + 2 + i);
					WhiteBalance.AddValues(_wbTable.ContainsKey(num3) ? _wbTable[num3] : num3.ToString(), num3);
				}
				WhiteBalance.ReloadValues();
				WhiteBalance.SetValue(num2, notifyValuChange: false);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Error init white balance", exception);
		}
	}

	private void WhiteBalance_ValueChanged(object sender, string key, long val)
	{
		SetProperty(4118u, BitConverter.GetBytes((ushort)val), 20485u);
		if (AdvancedProperties.Count > 13)
		{
			AdvancedProperties[13].IsEnabled = val == 5;
		}
		if (AdvancedProperties.Count > 14)
		{
			AdvancedProperties[14].IsEnabled = val == 32786;
		}
	}

	public void InitExposureCompensation()
	{
		try
		{
			DeviceReady();
			byte b = 2;
			NormalExposureCompensation = new PropertyValue<long>();
			NormalExposureCompensation.SubType = typeof(int);
			NormalExposureCompensation.Name = "ExposureCompensation";
			NormalExposureCompensation.ValueChanged += ExposureCompensation_ValueChanged;
			MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4116u, 20496u);
			short num = BitConverter.ToInt16(mTPDataResponse.Data, b + 5);
			for (int i = 0; i < mTPDataResponse.Data.Length - (2 * b + 6 + 2); i += b)
			{
				short num2 = BitConverter.ToInt16(mTPDataResponse.Data, 2 * b + 6 + 2 + i);
				decimal num3 = num2;
				string text = decimal.Round(num3 / 1000m, 1).ToString("0.0", CultureInfo.CreateSpecificCulture("en-US"));
				if (num3 > 0m)
				{
					text = "+" + text;
				}
				NormalExposureCompensation.AddValues(text, num2);
			}
			NormalExposureCompensation.ReloadValues();
			NormalExposureCompensation.SetValue(num, notifyValuChange: false);
			ExposureCompensation = NormalExposureCompensation;
		}
		catch (Exception)
		{
		}
		try
		{
			DeviceReady();
			byte b2 = 2;
			MovieExposureCompensation = new PropertyValue<long>();
			MovieExposureCompensation.SubType = typeof(int);
			MovieExposureCompensation.Name = "ExposureCompensation";
			MovieExposureCompensation.ValueChanged += MovieExposureCompensation_ValueChanged;
			MTPDataResponse mTPDataResponse2 = ExecuteReadDataEx(4116u, 53675u);
			short num4 = BitConverter.ToInt16(mTPDataResponse2.Data, b2 + 5);
			for (int j = 0; j < mTPDataResponse2.Data.Length - (2 * b2 + 6 + 2); j += b2)
			{
				short num5 = BitConverter.ToInt16(mTPDataResponse2.Data, 2 * b2 + 6 + 2 + j);
				decimal num6 = num5;
				string text2 = decimal.Round(num6 / 1000m, 1).ToString("0.0", CultureInfo.CreateSpecificCulture("en-US"));
				if (num6 > 0m)
				{
					text2 = "+" + text2;
				}
				MovieExposureCompensation.AddValues(text2, num5);
			}
			MovieExposureCompensation.ReloadValues();
			MovieExposureCompensation.SetValue(num4, notifyValuChange: false);
		}
		catch (Exception)
		{
		}
	}

	private void MovieExposureCompensation_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			SetProperty(4118u, BitConverter.GetBytes(val), 53675u);
		}
	}

	private void ExposureCompensation_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			SetProperty(4118u, BitConverter.GetBytes(val), 20496u);
		}
	}

	protected virtual void InitCompressionSetting()
	{
		try
		{
			DeviceReady();
			byte b = 1;
			CompressionSetting = new PropertyValue<long>();
			CompressionSetting.SubType = typeof(int);
			CompressionSetting.Name = "CompressionSetting ";
			CompressionSetting.ValueChanged += CompressionSetting_ValueChanged;
			MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, 20484u);
			if (mTPDataResponse.Data.Length != 0)
			{
				int num = BitConverter.ToInt16(mTPDataResponse.Data, 2);
				byte b2 = mTPDataResponse.Data[2 * b + 5];
				byte b3 = mTPDataResponse.Data[b + 5];
				for (int i = 0; i < mTPDataResponse.Data.Length - (2 * b + 6 + 2); i += b)
				{
					byte b4 = mTPDataResponse.Data[2 * b + 6 + 2 + i];
					CompressionSetting.AddValues(_csTable.ContainsKey(b4) ? _csTable[b4] : b4.ToString(), b4);
				}
				CompressionSetting.ReloadValues();
				CompressionSetting.SetValue(b3, notifyValuChange: false);
			}
		}
		catch (Exception)
		{
		}
	}

	protected void CompressionSetting_ValueChanged(object sender, string key, long val)
	{
		SetProperty(4118u, BitConverter.GetBytes((short)(byte)val), 20484u);
	}

	public void InitExposureMeteringMode()
	{
		try
		{
			DeviceReady();
			byte b = 2;
			ExposureMeteringMode = new PropertyValue<long>();
			ExposureMeteringMode.SubType = typeof(int);
			ExposureMeteringMode.Name = "ExposureMeteringMode";
			ExposureMeteringMode.ValueChanged += ExposureMeteringMode_ValueChanged;
			MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4116u, 20491u);
			ushort num = BitConverter.ToUInt16(mTPDataResponse.Data, b + 5);
			for (int i = 0; i < mTPDataResponse.Data.Length - (2 * b + 6 + 2); i += b)
			{
				ushort num2 = BitConverter.ToUInt16(mTPDataResponse.Data, 2 * b + 6 + 2 + i);
				ExposureMeteringMode.AddValues(_emmTable.ContainsKey(num2) ? _emmTable[num2] : num2.ToString(), num2);
			}
			ExposureMeteringMode.ReloadValues();
			ExposureMeteringMode.SetValue(num, notifyValuChange: false);
		}
		catch (Exception)
		{
		}
	}

	private void ExposureMeteringMode_ValueChanged(object sender, string key, long val)
	{
		SetProperty(4118u, BitConverter.GetBytes((ushort)val), 20491u);
	}

	protected virtual void InitFocusMode()
	{
		try
		{
			Log.Debug("InitFocusMode 1");
			DeviceReady();
			NormalFocusMode = new PropertyValue<long>();
			NormalFocusMode.Name = "FocusMode";
			NormalFocusMode.Code = 53601u;
			NormalFocusMode.IsEnabled = true;
			NormalFocusMode.AddValues("AF-S", 0L);
			NormalFocusMode.AddValues("AF-C", 1L);
			NormalFocusMode.AddValues("AF-A", 2L);
			NormalFocusMode.AddValues("MF (hard)", 3L);
			NormalFocusMode.AddValues("MF (soft)", 4L);
			NormalFocusMode.ReloadValues();
			NormalFocusMode.ValueChanged += NormalFocusMode_ValueChanged;
			FocusMode = NormalFocusMode;
			ReadDeviceProperties(NormalFocusMode.Code);
			Log.Debug("InitFocusMode 2");
			LiveViewFocusMode = new PropertyValue<long>();
			LiveViewFocusMode.Name = "FocusMode";
			LiveViewFocusMode.Code = 53345u;
			LiveViewFocusMode.IsEnabled = true;
			LiveViewFocusMode.AddValues("AF-S", 0L);
			LiveViewFocusMode.AddValues("AF-F", 2L);
			LiveViewFocusMode.AddValues("MF (hard)", 3L);
			LiveViewFocusMode.AddValues("MF (soft)", 4L);
			LiveViewFocusMode.ReloadValues();
			LiveViewFocusMode.ValueChanged += LiveViewFocusMode_ValueChanged;
			ReadDeviceProperties(LiveViewFocusMode.Code);
			Log.Debug("InitFocusMode 3");
		}
		catch (Exception exception)
		{
			Log.Error("Unable to init focus mode property", exception);
		}
	}

	private void LiveViewFocusMode_ValueChanged(object sender, string key, long val)
	{
		SetProperty(4118u, BitConverter.GetBytes((short)(sbyte)val), 53345u);
	}

	private void NormalFocusMode_ValueChanged(object sender, string key, long val)
	{
		SetProperty(4118u, BitConverter.GetBytes((short)(sbyte)val), 53601u);
	}

	public override void StartLiveView()
	{
		lock (Locker)
		{
			MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4117u, 53666u);
			if (mTPDataResponse.Data == null || mTPDataResponse.Data.Length == 0 || mTPDataResponse.Data[0] <= 0)
			{
				DeviceReady();
				ErrorCodes.GetException(ExecuteWithNoData(37377u));
				DeviceReady();
				LiveViewImageZoomRatio.SetValue();
			}
		}
	}

	public override void StopLiveView()
	{
		lock (Locker)
		{
			DeviceReady();
			ErrorCodes.GetException(ExecuteWithNoData(37378u));
			DeviceReady();
		}
	}

	public override LiveViewData GetLiveViewImage()
	{
		_timer.Stop();
		LiveViewData liveViewData = new LiveViewData();
		if (Monitor.TryEnter(Locker, 10))
		{
			try
			{
				liveViewData.HaveFocusData = true;
				MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(37379u);
				if (mTPDataResponse.ErrorCode == 40971)
				{
					_timer.Start();
					liveViewData.IsLiveViewRunning = false;
					liveViewData.ImageData = null;
					return liveViewData;
				}
				if (mTPDataResponse.Data == null || mTPDataResponse.Data.Length <= 384)
				{
					_timer.Start();
					liveViewData.ImageData = null;
					return liveViewData;
				}
				int num = mTPDataResponse.Data.Length;
				GetAdditionalLiveViewData(liveViewData, mTPDataResponse.Data);
				liveViewData.ImageDataPosition = 384;
				liveViewData.ImageData = mTPDataResponse.Data;
			}
			finally
			{
				Monitor.Exit(Locker);
			}
		}
		_timer.Start();
		return liveViewData;
	}

	[Obsolete("Use GetAdditionalLiveViewData instead.", false)]
	protected virtual void GetAditionalLIveViewData(LiveViewData viewData, byte[] result)
	{
		GetAdditionalLiveViewData(viewData, result);
	}

	protected virtual void GetAdditionalLiveViewData(LiveViewData viewData, byte[] result)
	{
		viewData.LiveViewImageWidth = BaseMTPCamera.ToInt16(result, 0);
		viewData.LiveViewImageHeight = BaseMTPCamera.ToInt16(result, 2);
		viewData.ImageWidth = BaseMTPCamera.ToInt16(result, 4);
		viewData.ImageHeight = BaseMTPCamera.ToInt16(result, 6);
		viewData.FocusFrameXSize = BaseMTPCamera.ToInt16(result, 16);
		viewData.FocusFrameYSize = BaseMTPCamera.ToInt16(result, 18);
		viewData.FocusX = BaseMTPCamera.ToInt16(result, 20);
		viewData.FocusY = BaseMTPCamera.ToInt16(result, 22);
		viewData.MovieTimeRemain = ToDeciaml(result, 56);
		viewData.Focused = result[40] != 1;
		if (result[29] == 1)
		{
			viewData.Rotation = -90;
		}
		if (result[29] == 2)
		{
			viewData.Rotation = 90;
		}
		viewData.MovieIsRecording = result[60] == 1;
	}

	public override int Focus(int step)
	{
		if (step == 0)
		{
			return 0;
		}
		DeviceReady();
		uint code = ((step > 0) ? ExecuteWithNoData(37380u, 2u, (uint)step) : ExecuteWithNoData(37380u, 1u, (uint)(-step)));
		ErrorCodes.GetException(code);
		DeviceReady();
		return step;
	}

	public override void Focus(FocusDirection direction, FocusAmount amount)
	{
		switch (direction)
		{
		case FocusDirection.Far:
			switch (amount)
			{
			case FocusAmount.Small:
				Focus(10);
				break;
			case FocusAmount.Medium:
				Focus(100);
				break;
			case FocusAmount.Large:
				Focus(500);
				break;
			default:
				throw new ArgumentOutOfRangeException("amount", amount, null);
			}
			break;
		case FocusDirection.Near:
			switch (amount)
			{
			case FocusAmount.Small:
				Focus(-10);
				break;
			case FocusAmount.Medium:
				Focus(-100);
				break;
			case FocusAmount.Large:
				Focus(-500);
				break;
			default:
				throw new ArgumentOutOfRangeException("amount", amount, null);
			}
			break;
		}
	}

	public override void AutoFocus()
	{
		lock (Locker)
		{
			DeviceReady();
			ErrorCodes.GetException(ExecuteWithNoData(37057u));
		}
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		SpeedUpEventTimer();
		Monitor.Enter(Locker);
		try
		{
			IsBusy = true;
			DeviceReady();
			ErrorCodes.GetException(CaptureInSdRam ? ExecuteWithNoData(37067u) : ExecuteWithNoData(4110u));
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

	public override void CapturePhotoNoAf()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		SpeedUpEventTimer();
		lock (Locker)
		{
			MTPDataResponse mTPDataResponse = null;
			byte b = 0;
			try
			{
				IsBusy = true;
				DeviceReady();
				MTPDataResponse mTPDataResponse2 = ExecuteReadDataEx(4117u, 53666u);
				if (mTPDataResponse2.Data != null && mTPDataResponse2.Data.Length != 0 && mTPDataResponse2.Data[0] > 0)
				{
					if (CaptureInSdRam)
					{
						ErrorCodes.GetException(ExecuteWithNoData(37056u, uint.MaxValue));
					}
					else
					{
						ErrorCodes.GetException(ExecuteWithNoData(37383u, uint.MaxValue, 0u));
					}
					return;
				}
				mTPDataResponse = StillImageDevice.ExecuteReadData(4117u, 53601u);
				if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0)
				{
					b = mTPDataResponse.Data[0];
				}
				SetProperty(4118u, new byte[1] { 4 }, 53601u);
				DeviceReady();
				ErrorCodes.GetException(CaptureInSdRam ? ExecuteWithNoData(37056u, uint.MaxValue) : ExecuteWithNoData(4110u));
			}
			catch
			{
				IsBusy = false;
				throw;
			}
			finally
			{
				if (mTPDataResponse != null && mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0)
				{
					SetProperty(4118u, new byte[1] { b }, 53601u);
				}
			}
		}
	}

	public override void Focus(int x, int y)
	{
		lock (Locker)
		{
			ErrorCodes.GetException(ExecuteWithNoData(37381u, (uint)x, (uint)y));
		}
	}

	public override void Close()
	{
		lock (Locker)
		{
			try
			{
				IsConnected = false;
				_timer.Stop();
				HaveLiveView = false;
				StillImageDevice.Disconnect();
			}
			catch (Exception exception)
			{
				Log.Error("Close camera error", exception);
			}
		}
	}

	public override void ReadDeviceProperties(uint prop)
	{
		try
		{
			HaveLiveView = true;
			switch (prop)
			{
			case 20487u:
				ReInitFNumber(trigervaluchange: false);
				return;
			case 53673u:
				ReInitFNumber(trigervaluchange: false);
				return;
			case 20495u:
				NormalIsoNumber.SetValue(StillImageDevice.ExecuteReadData(4117u, 20495u), notifyValuChange: false);
				return;
			case 53428u:
				NormalIsoNumber.SetValue(StillImageDevice.ExecuteReadData(4117u, 53428u), notifyValuChange: false);
				return;
			case 53674u:
				MovieFNumber.SetValue(StillImageDevice.ExecuteReadData(4117u, 53674u), notifyValuChange: false);
				return;
			case 20493u:
				NormalShutterSpeed.SetValue(StillImageDevice.ExecuteReadData(4117u, 20493u), notifyValuChange: false);
				return;
			case 53672u:
				MovieShutterSpeed.SetValue(StillImageDevice.ExecuteReadData(4117u, 53672u), notifyValuChange: false);
				return;
			case 20485u:
				WhiteBalance.SetValue(StillImageDevice.ExecuteReadData(4117u, 20485u), notifyValuChange: false);
				return;
			case 20494u:
				Mode.SetValue(StillImageDevice.ExecuteReadData(4117u, 20494u), notifyValuChange: true);
				return;
			case 20496u:
				NormalExposureCompensation.SetValue(StillImageDevice.ExecuteReadData(4117u, 20496u), notifyValuChange: false);
				return;
			case 53675u:
				MovieExposureCompensation.SetValue(StillImageDevice.ExecuteReadData(4117u, 53675u), notifyValuChange: false);
				return;
			case 20484u:
				CompressionSetting.SetValue(StillImageDevice.ExecuteReadData(4117u, 20484u), notifyValuChange: false);
				return;
			case 20491u:
				ExposureMeteringMode.SetValue(StillImageDevice.ExecuteReadData(4117u, 20491u), notifyValuChange: false);
				return;
			case 53601u:
				NormalFocusMode.SetValue(StillImageDevice.ExecuteReadData(4117u, 53601u), notifyValuChange: false);
				NormalFocusMode.IsEnabled = NormalFocusMode.NumericValue != 3;
				return;
			case 53345u:
				LiveViewFocusMode.SetValue(StillImageDevice.ExecuteReadData(4117u, 53345u), notifyValuChange: false);
				LiveViewFocusMode.IsEnabled = LiveViewFocusMode.NumericValue != 3;
				return;
			case 20481u:
			{
				MTPDataResponse mTPDataResponse4 = StillImageDevice.ExecuteReadData(4117u, 20481u);
				if (mTPDataResponse4.Data != null && mTPDataResponse4.Data.Length != 0)
				{
					Battery = mTPDataResponse4.Data[0];
				}
				return;
			}
			case 53681u:
			{
				MTPDataResponse mTPDataResponse3 = StillImageDevice.ExecuteReadData(4117u, 53681u);
				if (mTPDataResponse3.Data != null && mTPDataResponse3.Data.Length != 0)
				{
					sbyte value = (sbyte)mTPDataResponse3.Data[0];
					ExposureStatus = Convert.ToInt32(value);
				}
				return;
			}
			case 53666u:
			{
				MTPDataResponse mTPDataResponse2 = ExecuteReadDataEx(4117u, 53666u);
				if (mTPDataResponse2.Data != null && mTPDataResponse2.Data.Length != 0)
				{
					LiveViewOn = mTPDataResponse2.Data[0] == 1;
				}
				else
				{
					LiveViewOn = false;
				}
				return;
			}
			case 53670u:
			{
				MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4117u, 53670u);
				if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0)
				{
					LiveViewMovieOn = mTPDataResponse.Data[0] == 1;
				}
				else
				{
					LiveViewMovieOn = false;
				}
				return;
			}
			}
			foreach (PropertyValue<long> item in AdvancedProperties.Where((PropertyValue<long> advancedProperty) => advancedProperty.Code == prop))
			{
				if (item.Name == "Image Size")
				{
					MTPDataResponse mTPDataResponse5 = StillImageDevice.ExecuteReadData(4117u, item.Code);
					if (mTPDataResponse5.Data != null && mTPDataResponse5.Data.Length != 0)
					{
						item.SetValue(Encoding.Unicode.GetString(mTPDataResponse5.Data, 1, 20), notifyValuChange: false);
					}
				}
				else
				{
					item.SetValue(StillImageDevice.ExecuteReadData(4117u, item.Code), notifyValuChange: false);
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("ReadDeviceProperties error", exception);
		}
	}

	public override void LockCamera()
	{
		ExecuteWithNoData(37058u, 1u);
		Mode.IsEnabled = true;
	}

	public override void UnLockCamera()
	{
		Mode.IsEnabled = false;
		ExecuteWithNoData(37058u, 0u);
	}

	public override void StartRecordMovie()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		CameraControl.Devices.LegacyCapturePolicy.RequireMovieRecording();

		base.StartRecordMovie();
		DeviceReady();
		ErrorCodes.GetException(ExecuteWithNoData(37386u));
	}

	public override void StopRecordMovie()
	{
		base.StopRecordMovie();
		DeviceReady();
		ErrorCodes.GetException(ExecuteWithNoData(37387u));
	}

	public override void SetCameraField(CameraFieldType cameraFieldType, string comment)
	{
		switch (cameraFieldType)
		{
		case CameraFieldType.Comment:
		{
			if (string.IsNullOrEmpty(comment))
			{
				SetProperty(4118u, new byte[1], 53393u);
				break;
			}
			comment = ((comment.Length > 36) ? comment.Substring(0, 36) : comment.PadRight(36));
			List<byte> list3 = new List<byte>();
			List<byte> list4 = new List<byte>();
			list4.Add(36);
			list3.AddRange(Encoding.UTF8.GetBytes(comment));
			foreach (byte item in list3)
			{
				list4.Add(item);
				list4.Add(0);
			}
			list4.Add(0);
			list4.Add(0);
			SetProperty(4118u, list4.ToArray(), 53392u);
			SetProperty(4118u, new byte[1] { 1 }, 53393u);
			break;
		}
		case CameraFieldType.Artist:
		{
			if (string.IsNullOrEmpty(comment))
			{
				SetProperty(4118u, new byte[3], 53362u);
				break;
			}
			comment = ((comment.Length > 36) ? comment.Substring(0, 36) : comment.PadRight(36));
			List<byte> list5 = new List<byte>();
			List<byte> list6 = new List<byte>();
			list6.Add(36);
			list5.AddRange(Encoding.UTF8.GetBytes(comment));
			foreach (byte item2 in list5)
			{
				list6.Add(item2);
				list6.Add(0);
			}
			list6.Add(0);
			list6.Add(0);
			SetProperty(4118u, list6.ToArray(), 53362u);
			break;
		}
		case CameraFieldType.Copyright:
		{
			if (string.IsNullOrEmpty(comment))
			{
				SetProperty(4118u, new byte[3], 53363u);
				break;
			}
			comment = ((comment.Length > 53) ? comment.Substring(0, 53) : comment.PadRight(53));
			List<byte> list = new List<byte>();
			List<byte> list2 = new List<byte>();
			list2.Add(54);
			list.AddRange(Encoding.UTF8.GetBytes(comment));
			foreach (byte item3 in list)
			{
				list2.Add(item3);
				list2.Add(0);
			}
			list2.Add(0);
			list2.Add(0);
			SetProperty(4118u, list2.ToArray(), 53363u);
			break;
		}
		}
	}

	private void GetEvent(object state)
	{
		try
		{
			if (_eventIsbusy)
			{
				return;
			}
			_eventIsbusy = true;
			MTPDataResponse mTPDataResponse = ExecuteReadDataEx(37063u);
			if (mTPDataResponse.Data == null || mTPDataResponse.Data.Length == 0)
			{
				Log.Debug("Get event error :" + mTPDataResponse.ErrorCode.ToString("X"));
				_eventIsbusy = false;
				return;
			}
			int num = BitConverter.ToInt16(mTPDataResponse.Data, 0);
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					try
					{
						uint num2 = BitConverter.ToUInt16(mTPDataResponse.Data, 6 * i + 2);
						ushort prop = BitConverter.ToUInt16(mTPDataResponse.Data, 6 * i + 4);
						int num3 = BitConverter.ToInt32(mTPDataResponse.Data, 6 * i + 4);
						switch (num2)
						{
						case 16390u:
							ReadDeviceProperties(prop);
							break;
						case 16386u:
						case 49409u:
						{
							Log.Debug("CONST_Event_ObjectAddedInSdram" + num2.ToString("X") + " L:");
							MTPDataResponse mTPDataResponse2 = ExecuteReadDataEx(4104u, (uint)num3);
							string text = "DSC_0000.JPG";
							if (mTPDataResponse2.Data != null && mTPDataResponse2.Data.Length != 0)
							{
								Log.Debug("L:" + mTPDataResponse2.Data.Length);
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
							Log.Debug("File name" + text);
							PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
							{
								WiaImageItem = null,
								EventArgs = new PortableDeviceEventArgs(new PortableDeviceEventType
								{
									ObjectHandle = (uint)num3
								}),
								CameraDevice = this,
								FileName = text,
								Handle = (uint)num3
							};
							OnPhotoCapture(this, args);
							break;
						}
						case 16397u:
						case 49410u:
							SlowDownEventTimer();
							OnCaptureCompleted(this, new EventArgs());
							break;
						case 49412u:
							break;
						default:
							Log.Debug("Unknown event code :" + num2.ToString("X") + "|" + num3.ToString("X"));
							break;
						}
					}
					catch (Exception exception)
					{
						Log.Error("Event queue processing error ", exception);
					}
				}
			}
		}
		catch (InvalidComObjectException)
		{
		}
		catch (Exception)
		{
		}
		_eventIsbusy = false;
		_timer.Start();
	}

	public void DeviceReady()
	{
		DeviceReady(0);
	}

	public void ResetTimer()
	{
		_timer.Stop();
		_timer.Start();
	}

	public void DeviceReady(int retrynum)
	{
		bool flag = true;
		if (retrynum > 50)
		{
			return;
		}
		ulong num = ExecuteWithNoData(37064u);
		if (num != 0L && num != 8193)
		{
			if (num == 8217 || num == 2147942570u)
			{
				Console.WriteLine("Device not ready");
				Thread.Sleep(5);
				retrynum++;
			}
			else
			{
				Console.WriteLine("Device ready code #0" + num.ToString("X"));
			}
		}
	}

	public override void FormatStorage(object storageId)
	{
		DeviceReady();
		base.FormatStorage(storageId);
	}

	public override void ResetDevice()
	{
		if (StillImageDevice is StillImageDevice stillImageDevice)
		{
			Console.WriteLine(stillImageDevice.ResetDevice().ToString("X"));
		}
	}

	public override string GetProhibitionCondition(OperationEnum operationEnum)
	{
		switch (operationEnum)
		{
		case OperationEnum.Capture:
			return "";
		case OperationEnum.RecordMovie:
		{
			MTPDataResponse mTPDataResponse2 = ExecuteReadDataEx(4117u, 53412u);
			if (mTPDataResponse2.Data != null && mTPDataResponse2.Data.Length != 0)
			{
				int num2 = BitConverter.ToInt32(mTPDataResponse2.Data, 0);
				if (num2 == 0)
				{
					return string.Empty;
				}
				if (StaticHelper.GetBit(num2, 0))
				{
					return "LabelNoCardInserted";
				}
				if (StaticHelper.GetBit(num2, 1))
				{
					return "LabelCardError";
				}
				if (StaticHelper.GetBit(num2, 2))
				{
					return "LabelCardNotFormatted";
				}
				if (StaticHelper.GetBit(num2, 3))
				{
					return "LabelNoFreeAreaInCard";
				}
				if (StaticHelper.GetBit(num2, 7))
				{
					return "LabelCardBufferNotEmpty";
				}
				if (StaticHelper.GetBit(num2, 8))
				{
					return "LabelPcBufferNotEmpty";
				}
				if (StaticHelper.GetBit(num2, 9))
				{
					return "LabelBufferNotEmpty";
				}
				if (StaticHelper.GetBit(num2, 10))
				{
					return "LabelRecordInProgres";
				}
				if (StaticHelper.GetBit(num2, 11))
				{
					return "LabelCardProtected";
				}
				if (StaticHelper.GetBit(num2, 12))
				{
					return "LabelDuringEnlargedDisplayLiveView";
				}
				if (StaticHelper.GetBit(num2, 13))
				{
					return "LabelWrongLiveViewType";
				}
				if (StaticHelper.GetBit(num2, 14))
				{
					return "";
				}
			}
			return "";
		}
		case OperationEnum.AutoFocus:
			if (FocusMode != null && FocusMode.Value != null && FocusMode.Value.Contains("[M]"))
			{
				return "LabelMFError";
			}
			return "";
		case OperationEnum.ManualFocus:
		{
			if (FocusMode != null && FocusMode.Value != null && FocusMode.Value.Contains("[M]"))
			{
				return "LabelMFError";
			}
			MTPDataResponse mTPDataResponse3 = ExecuteReadDataEx(4117u, 53345u);
			if (mTPDataResponse3 != null && mTPDataResponse3.Data != null && mTPDataResponse3.Data.Length != 0)
			{
				byte b = mTPDataResponse3.Data[0];
				if (b == 2)
				{
					return "LabelNotAFSError";
				}
			}
			return "";
		}
		case OperationEnum.LiveView:
		{
			MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4117u, 53668u);
			if (mTPDataResponse.Data != null && mTPDataResponse.Data.Length != 0)
			{
				int num = BitConverter.ToInt32(mTPDataResponse.Data, 0);
				if (num == 0)
				{
					return string.Empty;
				}
				if (StaticHelper.GetBit(num, 0))
				{
					return "LabelDestinationCardError";
				}
				if (StaticHelper.GetBit(num, 2))
				{
					return "LabelSequenceError";
				}
				if (StaticHelper.GetBit(num, 4))
				{
					return "LabelFullyPressedButtonError";
				}
				if (StaticHelper.GetBit(num, 5))
				{
					return "LabelApertureValueError";
				}
				if (StaticHelper.GetBit(num, 6))
				{
					return "LabelBulbError";
				}
				if (StaticHelper.GetBit(num, 7))
				{
					return "LabelDuringCleaningMirror";
				}
				if (StaticHelper.GetBit(num, 8))
				{
					return "LabelDuringInsufficiencyBattery";
				}
				if (StaticHelper.GetBit(num, 9))
				{
					return "LabelTTLError";
				}
				if (StaticHelper.GetBit(num, 11))
				{
					return "LabelNonCPULEnseError";
				}
				if (StaticHelper.GetBit(num, 12))
				{
					return "LabelImageInRAM";
				}
				if (StaticHelper.GetBit(num, 13))
				{
					return "LabelMirrorUpError2";
				}
				if (StaticHelper.GetBit(num, 14))
				{
					return "LabelNoCardInsertedError";
				}
				if (StaticHelper.GetBit(num, 15))
				{
					return "LabelCommandProcesingError";
				}
				if (StaticHelper.GetBit(num, 16))
				{
					return "LabelShoutingInProgress";
				}
				if (StaticHelper.GetBit(num, 17))
				{
					return "LabelOverHeatedError";
				}
				if (StaticHelper.GetBit(num, 18))
				{
					return "LabelCardProtectedError";
				}
				if (StaticHelper.GetBit(num, 19))
				{
					return "LabelCardError";
				}
				if (StaticHelper.GetBit(num, 20))
				{
					return "LabelCardNotFormatted";
				}
				if (StaticHelper.GetBit(num, 21))
				{
					return "LabelBulbError";
				}
				if (StaticHelper.GetBit(num, 22))
				{
					return "LabelMirrorUpError";
				}
			}
			return "";
		}
		default:
			throw new ArgumentOutOfRangeException("operationEnum");
		}
	}

	public override AsyncObservableCollection<DeviceObject> GetObjects(object storageId, bool loadThumbs)
	{
		DeviceReady();
		return base.GetObjects(storageId, loadThumbs);
	}

	public override void TransferFileThumb(object o, string filename)
	{
		lock (Locker)
		{
			using (FileStream fileStream = File.Open(filename, FileMode.Create))
			{
				MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadBigData(37060u, fileStream, (uint total, uint current) =>
				{
					double num = (double)current / (double)total;
					TransferProgress = Convert.ToUInt32(num * 100.0);
				}, Convert.ToUInt32(o));
				if (mTPDataResponse.Data != null)
				{
					fileStream.Write(mTPDataResponse.Data, 0, mTPDataResponse.Data.Length);
				}
			}
			_timer.Start();
			TransferProgress = 0u;
		}
	}

	public override string ToStringCameraData()
	{
		StringBuilder stringBuilder = new StringBuilder(base.ToString() + "\n\tType..................Nikon()");
		stringBuilder.AppendFormat("\n\tLiveView:", Array.Empty<object>());
		stringBuilder.AppendFormat("\n\t  On..................{0}", LiveViewOn ? "Yes" : "No");
		stringBuilder.AppendFormat("\n\t  Focus Mode..........{0}", LiveViewFocusMode);
		stringBuilder.AppendFormat("\n\tNormal:", Array.Empty<object>());
		stringBuilder.AppendFormat("\n\t  Focus Mode..........{0}", NormalFocusMode);
		stringBuilder.AppendFormat("\n\texposure..............{0} f{1} +/-{2}, ISO{3}", new object[4] { NormalShutterSpeed, NormalFNumber, NormalExposureCompensation, NormalIsoNumber });
		return stringBuilder.ToString();
	}
}
