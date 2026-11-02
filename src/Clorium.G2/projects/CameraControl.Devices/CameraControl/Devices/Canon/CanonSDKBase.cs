using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Timers;
using CameraControl.Devices.Classes;
using Canon.Eos.Framework;
using Canon.Eos.Framework.Eventing;
using Canon.Eos.Framework.Internal;
using Canon.Eos.Framework.Internal.SDK;

namespace CameraControl.Devices.Canon;

public class CanonSDKBase : BaseMTPCamera
{
	private EosLiveImageEventArgs _liveViewImageData = null;

	private bool _recording = false;

	private bool debugMode = false;

	public EosCamera Camera = null;

	private System.Timers.Timer _shutdownTimer = new System.Timers.Timer(60000.0);

	protected Dictionary<uint, string> _shutterTable = new Dictionary<uint, string>
	{
		{ 12u, "Bulb" },
		{ 16u, "30" },
		{ 19u, "25" },
		{ 20u, "20" },
		{ 21u, "20" },
		{ 24u, "15" },
		{ 27u, "13" },
		{ 28u, "10" },
		{ 29u, "10" },
		{ 32u, "8" },
		{ 35u, "6" },
		{ 36u, "6" },
		{ 37u, "5" },
		{ 40u, "4" },
		{ 43u, "3.2" },
		{ 44u, "3" },
		{ 45u, "2.5" },
		{ 48u, "2" },
		{ 51u, "1.6" },
		{ 52u, "1.5" },
		{ 53u, "1.3" },
		{ 56u, "1" },
		{ 59u, "0.8" },
		{ 60u, "0.7" },
		{ 61u, "0.6" },
		{ 64u, "0.5" },
		{ 67u, "0.4" },
		{ 68u, "0.3" },
		{ 69u, "0.3" },
		{ 72u, "1/4" },
		{ 75u, "1/5" },
		{ 76u, "1/6" },
		{ 77u, "1/5" },
		{ 80u, "1/8" },
		{ 83u, "1/10" },
		{ 84u, "1/10" },
		{ 85u, "1/13" },
		{ 88u, "1/15" },
		{ 91u, "1/20" },
		{ 92u, "1/20" },
		{ 93u, "1/25" },
		{ 96u, "1/30" },
		{ 99u, "1/40" },
		{ 100u, "1/45" },
		{ 101u, "1/50" },
		{ 104u, "1/60" },
		{ 107u, "1/80" },
		{ 108u, "1/90" },
		{ 109u, "1/100" },
		{ 112u, "1/125" },
		{ 115u, "1/160" },
		{ 116u, "1/180" },
		{ 117u, "1/200" },
		{ 120u, "1/250" },
		{ 123u, "1/320" },
		{ 124u, "1/350" },
		{ 125u, "1/400" },
		{ 128u, "1/500" },
		{ 131u, "1/640" },
		{ 132u, "1/750" },
		{ 133u, "1/800" },
		{ 136u, "1/1000" },
		{ 139u, "1/1250" },
		{ 140u, "1/1500" },
		{ 141u, "1/1600" },
		{ 144u, "1/2000" },
		{ 147u, "1/2500" },
		{ 148u, "1/3000" },
		{ 149u, "1/3200" },
		{ 152u, "1/4000" },
		{ 155u, "1/5000" },
		{ 156u, "1/6000" },
		{ 157u, "1/6400" },
		{ 160u, "1/8000" }
	};

	protected Dictionary<int, string> _apertureTable = new Dictionary<int, string>
	{
		{ 8, "1.0" },
		{ 11, "1.1" },
		{ 12, "1.2" },
		{ 13, "1.0" },
		{ 16, "1.4" },
		{ 19, "1.6" },
		{ 20, "1.8" },
		{ 21, "1.8" },
		{ 24, "2.0" },
		{ 27, "2.2" },
		{ 28, "2.5" },
		{ 29, "2.5" },
		{ 32, "2.8" },
		{ 35, "3.2" },
		{ 36, "3.5" },
		{ 37, "3.5" },
		{ 40, "4.0" },
		{ 43, "4.5" },
		{ 44, "4.5" },
		{ 45, "5.0" },
		{ 48, "5.6" },
		{ 51, "6.3" },
		{ 52, "6.7" },
		{ 53, "7.1" },
		{ 56, "8.0" },
		{ 59, "9.0" },
		{ 60, "9.5" },
		{ 61, "10.0" },
		{ 64, "11.0" },
		{ 67, "13.0" },
		{ 68, "13.0" },
		{ 69, "14.0" },
		{ 72, "16.0" },
		{ 75, "18.0" },
		{ 76, "19.0" },
		{ 77, "20.0" },
		{ 80, "22.0" },
		{ 83, "25.0" },
		{ 84, "27.0" },
		{ 85, "29.0" },
		{ 88, "32.0" },
		{ 91, "36.0" },
		{ 92, "38.0" },
		{ 93, "40.0" },
		{ 96, "45.0" },
		{ 99, "51.0" },
		{ 100, "54.0" },
		{ 101, "57.0" },
		{ 104, "64.0" },
		{ 107, "72.0" },
		{ 108, "76.0" },
		{ 109, "80.0" },
		{ 112, "91.0" }
	};

	protected Dictionary<long, string> _exposureModeTable = new Dictionary<long, string>
	{
		{ 0L, "P" },
		{ 1L, "Tv" },
		{ 2L, "Av" },
		{ 3L, "M" },
		{ 4L, "Bulb" },
		{ 5L, "A-DEP" },
		{ 6L, "DEP" },
		{ 7L, "Camera settings registered" },
		{ 8L, "Lock" },
		{ 9L, "Auto" },
		{ 10L, "Night scene Portrait" },
		{ 11L, "Sport" },
		{ 12L, "Portrait" },
		{ 13L, "Landscape" },
		{ 14L, "Close-Up" },
		{ 15L, "Flash Off" },
		{ 19L, "Creative Auto" },
		{ 21L, "Photo in Movie" }
	};

	protected Dictionary<uint, string> _isoTable = new Dictionary<uint, string>
	{
		{ 0u, "Auto" },
		{ 40u, "6" },
		{ 48u, "12" },
		{ 56u, "25" },
		{ 64u, "50" },
		{ 72u, "100" },
		{ 75u, "125" },
		{ 77u, "160" },
		{ 80u, "200" },
		{ 83u, "250" },
		{ 85u, "320" },
		{ 88u, "400" },
		{ 91u, "500" },
		{ 93u, "640" },
		{ 96u, "800" },
		{ 99u, "1000" },
		{ 101u, "1250" },
		{ 104u, "1600" },
		{ 112u, "3200" },
		{ 120u, "6400" },
		{ 128u, "12800" },
		{ 136u, "25600" },
		{ 144u, "51200" },
		{ 152u, "102400" }
	};

	protected Dictionary<long, string> _ecTable = new Dictionary<long, string>
	{
		{ 40L, "+5.0" },
		{ 37L, "+4 2/3" },
		{ 36L, "+4.5" },
		{ 35L, "+4 1/3" },
		{ 32L, "+4.0" },
		{ 29L, "+3 2/3" },
		{ 28L, "+3.5" },
		{ 27L, "+3 1/3" },
		{ 24L, "+3.0" },
		{ 21L, "+2 2/3" },
		{ 20L, "+2.5" },
		{ 19L, "+2 1/3" },
		{ 16L, "+2.0" },
		{ 13L, "+1 2/3" },
		{ 12L, "+1.5" },
		{ 11L, "+1 1/3" },
		{ 8L, "+1.0" },
		{ 5L, "+2/3" },
		{ 4L, "+0.5" },
		{ 3L, "+1/3" },
		{ 0L, "0.0" },
		{ 4294967293L, "-1/3" },
		{ 4294967292L, "-0.5" },
		{ 4294967291L, "-2/3" },
		{ 4294967288L, "-1.0" },
		{ 4294967285L, "-1 1/3" },
		{ 4294967284L, "-1.5" },
		{ 4294967283L, "-1 2/3" },
		{ 4294967280L, "-2.0" },
		{ 4294967277L, "-2 1/3" },
		{ 4294967276L, "-2.5" },
		{ 4294967275L, "-2 2/3" },
		{ 4294967272L, "-3.0" }
	};

	protected Dictionary<uint, string> _wbTable = new Dictionary<uint, string>
	{
		{ 0u, "Auto" },
		{ 1u, "Daylight" },
		{ 2u, "Cloudy" },
		{ 3u, "Tungsten" },
		{ 4u, "Fluorescent" },
		{ 5u, "Flash" },
		{ 6u, "Manual" },
		{ 8u, "Shade" },
		{ 9u, "Color temperature" },
		{ 10u, "Custom white balance: PC-1" },
		{ 11u, "Custom white balance: PC-2" },
		{ 12u, "Custom white balance: PC-3" },
		{ 15u, "Manual 2" },
		{ 16u, "Manual 3" },
		{ 18u, "Manual 4" },
		{ 19u, "Manual 5" },
		{ 20u, "Custom white balance: PC-4" },
		{ 21u, "Custom white balance: PC-5" }
	};

	protected Dictionary<uint, string> _meteringTable = new Dictionary<uint, string>
	{
		{ 1u, "Spot metering" },
		{ 3u, "Evaluative metering" },
		{ 4u, "Partial metering" },
		{ 5u, "Center-weighted averaging metering" },
		{
			uint.MaxValue,
			"Not valid/no settings changes"
		}
	};

	protected Dictionary<uint, string> _focusModeTable = new Dictionary<uint, string>
	{
		{ 0u, "One-Shot AF" },
		{ 1u, "AI Servo AF" },
		{ 2u, "AI Focus AF" },
		{ 3u, "Manual Focus" },
		{
			uint.MaxValue,
			"Not valid/no settings changes"
		}
	};

	protected Dictionary<int, string> _colorTemperatureTable = new Dictionary<int, string>
	{
		{ 2500, "2500" },
		{ 2600, "2600" },
		{ 2700, "2700" },
		{ 2800, "2800" },
		{ 2900, "2900" },
		{ 3000, "3000" },
		{ 3100, "3100" },
		{ 3200, "3200" },
		{ 3300, "3300" },
		{ 3400, "3400" },
		{ 3500, "3500" },
		{ 3600, "3600" },
		{ 3700, "3700" },
		{ 3800, "3800" },
		{ 3900, "3900" },
		{ 4000, "4000" },
		{ 4100, "4100" },
		{ 4200, "4200" },
		{ 4300, "4300" },
		{ 4400, "4400" },
		{ 4500, "4500" },
		{ 4600, "4600" },
		{ 4700, "4700" },
		{ 4800, "4800" },
		{ 4900, "4900" },
		{ 5000, "5000" },
		{ 5100, "5100" },
		{ 5200, "5200" },
		{ 5300, "5300" },
		{ 5400, "5400" },
		{ 5500, "5500" },
		{ 5600, "5600" },
		{ 5700, "5700" },
		{ 5800, "5800" },
		{ 5900, "5900" },
		{ 6000, "6000" },
		{ 6100, "6100" },
		{ 6200, "6200" },
		{ 6300, "6300" },
		{ 6400, "6400" },
		{ 6500, "6500" },
		{ 6600, "6600" },
		{ 6700, "6700" },
		{ 6800, "6800" },
		{ 6900, "6900" },
		{ 7000, "7000" },
		{ 7100, "7100" },
		{ 7200, "7200" },
		{ 7300, "7300" },
		{ 7400, "7400" },
		{ 7500, "7500" },
		{ 7600, "7600" },
		{ 7700, "7700" },
		{ 7800, "7800" },
		{ 7900, "7900" },
		{ 8000, "8000" },
		{ 8100, "8100" },
		{ 8200, "8200" },
		{ 8300, "8300" },
		{ 8400, "8400" },
		{ 8500, "8500" },
		{ 8600, "8600" },
		{ 8700, "8700" },
		{ 8800, "8800" },
		{ 8900, "8900" },
		{ 9000, "9000" },
		{ 9100, "9100" },
		{ 9200, "9200" },
		{ 9300, "9300" },
		{ 9400, "9400" },
		{ 9500, "9500" },
		{ 9600, "9600" },
		{ 9700, "9700" },
		{ 9800, "9800" },
		{ 9900, "9900" },
		{ 10000, "10000" }
	};

	public override bool CaptureInSdRam
	{
		get
		{
			return base.CaptureInSdRam;
		}
		set
		{
			base.CaptureInSdRam = value;
			if (IsConnected && Camera != null)
			{
				if (!base.CaptureInSdRam)
				{
					Camera.SavePicturesToCamera();
				}
				else
				{
					Camera.SavePicturesToHost(Path.GetTempPath());
				}
			}
		}
	}

	public override DateTime DateTime
	{
		get
		{
			try
			{
				return Camera.GetDate();
			}
			catch (Exception exception)
			{
				Log.Error("CanonSDK : Unable to get time", exception);
			}
			return DateTime.MinValue;
		}
		set
		{
			try
			{
				Camera.SetDate(value);
				NotifyPropertyChanged("DateTime");
			}
			catch (Exception exception)
			{
				Log.Error("CanonSDK : Unable to set time", exception);
			}
		}
	}

	public void Init(EosCamera camera, bool debug)
	{
		debugMode = debug;
		try
		{
			PreventShutDown = true;
			IsBusy = true;
			Camera = camera;
			Camera.IsErrorTolerantMode = false;
			DeviceName = Camera.DeviceDescription;
			PortName = camera.PortName;
			Manufacturer = "Canon Inc.";
			Log.Debug("Canon SDK : Init 1", debugMode);
			Camera.SetEventHandlers();
			Log.Debug("Canon SDK : Init 2", debugMode);
			Camera.EnsureOpenSession();
			Log.Debug("Canon SDK : Init 3", debugMode);
			Camera.Error += _camera_Error;
			Camera.Shutdown += _camera_Shutdown;
			Camera.LiveViewPaused += Camera_LiveViewPaused;
			Camera.LiveViewUpdate += Camera_LiveViewUpdate;
			Camera.PictureTaken += Camera_PictureTaken;
			Capabilities.Add(CapabilityEnum.Bulb);
			Capabilities.Add(CapabilityEnum.LiveView);
			Capabilities.Add(CapabilityEnum.CaptureInRam);
			Capabilities.Add(CapabilityEnum.RecordMovie);
			Capabilities.Add(CapabilityEnum.SimpleManualFocus);
			Log.Debug("Canon SDK : Init 4", debugMode);
			IsConnected = true;
			ResetShutterButton();
			Thread.Sleep(200);
			LoadProperties();
			Log.Debug("Canon SDK : Init 5", debugMode);
			Thread thread = new Thread(() =>
			{
				Thread.Sleep(200);
				OnCameraInitDone();
			});
			thread.Start();
			_shutdownTimer.Elapsed += _shutdownTimer_Elapsed;
		}
		catch (Exception exception)
		{
			Log.Error("CanonSDK : 카메라 초기화 실패", exception);
			throw;
		}
	}

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		Capabilities.Add(CapabilityEnum.Bulb);
		Capabilities.Add(CapabilityEnum.LiveView);
		IsConnected = true;
		return true;
	}

	public override void Close()
	{
		HaveLiveView = false;
		Camera.Error -= _camera_Error;
		Camera.Shutdown -= _camera_Shutdown;
		Camera.LiveViewPaused -= Camera_LiveViewPaused;
		Camera.LiveViewUpdate -= Camera_LiveViewUpdate;
		Camera.PictureTaken -= Camera_PictureTaken;
		Camera.PropertyChanged -= Camera_PropertyChanged;
		Camera.WillShutdown -= Camera_WillShutdown;
		Camera = null;
	}

	public void LoadProperties()
	{
		Log.Debug("Canon SDK : LoadProperties 4-1", debugMode);
		InitMode();
		Log.Debug("Canon SDK : LoadProperties 4-2", debugMode);
		InitShutterSpeed();
		Log.Debug("Canon SDK : LoadProperties 4-3", debugMode);
		InitFNumber();
		Log.Debug("Canon SDK : LoadProperties 4-4", debugMode);
		InitIso();
		Log.Debug("Canon SDK : LoadProperties 4-5", debugMode);
		InitEc();
		Log.Debug("Canon SDK : LoadProperties 4-6", debugMode);
		InitWb();
		Log.Debug("Canon SDK : LoadProperties 4-7", debugMode);
		InitColorTemperature();
		Log.Debug("Canon SDK : LoadProperties 4-8", debugMode);
		InitMetering();
		Log.Debug("Canon SDK : LoadProperties 4-9", debugMode);
		InitFocus();
		Log.Debug("Canon SDK : LoadProperties 4-10", debugMode);
		InitOther();
		Log.Debug("Canon SDK : LoadProperties 4-11", debugMode);
		InitCompression();
		Log.Debug("Canon SDK : LoadProperties 4-12", debugMode);
		Battery = (int)Camera.BatteryLevel + 20;
		IsBusy = false;
		Log.Debug("Canon SDK : LoadProperties 4-12-1", debugMode);
		CaptureInSdRam = true;
		Log.Debug("Canon SDK : LoadProperties 4-12-2", debugMode);
		Camera.PropertyChanged += Camera_PropertyChanged;
		Camera.WillShutdown += Camera_WillShutdown;
		SerialNumber = Camera.SerialNumber;
		Log.Debug("Canon SDK : LoadProperties 4-13", debugMode);
		AddAditionalProps();
		Log.Debug("Canon SDK : LoadProperties 4-14", debugMode);
	}

	private void InitMode()
	{
		Mode = new PropertyValue<long>();
		Mode.ValueChanged += Mode_ValueChanged;
		InitAEMode();
	}

	private void InitAEMode()
	{
		lock (Locker)
		{
			try
			{
				List<int> settingsList = GetSettingsList(1024u);
				foreach (KeyValuePair<long, string> item in _exposureModeTable)
				{
					if (settingsList.Count > 0)
					{
						if (settingsList.Contains((int)item.Key))
						{
							Mode.AddValues(item.Value, item.Key);
						}
					}
					else
					{
						Mode.AddValues(item.Value, item.Key);
					}
				}
				Mode.ReloadValues();
				Mode.SetValue((uint)Camera.GetProperty(1024u), notifyValuChange: false);
				Mode.IsEnabled = true;
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : [Error] InitAEMode()", exception);
			}
		}
	}

	private void Mode_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.SetProperty(1078u, val);
			Log.Debug("CanonSDK : Edsdk.PropID_AEModeSelect : 0x" + val.ToString("X"), debugMode);
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Error set EC to camera", exception);
		}
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
				List<int> settingsList = GetSettingsList(1030u);
				foreach (KeyValuePair<uint, string> item in _shutterTable)
				{
					if (settingsList.Count > 0)
					{
						if (settingsList.Contains((int)item.Key))
						{
							ShutterSpeed.AddValues(item.Value, item.Key);
						}
					}
					else
					{
						ShutterSpeed.AddValues(item.Value, item.Key);
					}
				}
				ShutterSpeed.ReloadValues();
				long property = Camera.GetProperty(1030u);
				if (property == 0)
				{
					ShutterSpeed.IsEnabled = false;
					return;
				}
				ShutterSpeed.IsEnabled = true;
				ShutterSpeed.SetValue(Camera.GetProperty(1030u), notifyValuChange: false);
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : [Error] ReInitShutterSpeed()", exception);
			}
		}
	}

	private void ShutterSpeed_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.SetProperty(1030u, val);
		}
		catch (Exception exception)
		{
			Log.Error("CanonSDK : Error set property sP", exception);
		}
	}

	private void InitFNumber()
	{
		FNumber = new PropertyValue<long>
		{
			IsEnabled = true,
			Name = "FNumber"
		};
		FNumber.ValueChanged += FNumber_ValueChanged;
		ReInitFNumber(trigervaluchange: true);
	}

	private void FNumber_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.SetProperty(1029u, val);
		}
		catch (Exception exception)
		{
			Log.Error("CanonSDK : Error set property FNumber", exception);
		}
	}

	private void ReInitFNumber(bool trigervaluchange)
	{
		try
		{
			List<int> data = GetSettingsList(1029u);
			long property = Camera.GetProperty(1029u);
			bool flag = FNumber.Values.Count == 0;
			if (data.Count > 0)
			{
				FNumber.Clear();
			}
			if (flag && data.Count == 0)
			{
				foreach (KeyValuePair<int, string> item in _apertureTable)
				{
					FNumber.AddValues(item.Value, item.Key);
				}
			}
			else
			{
				foreach (KeyValuePair<int, string> item2 in from keyValuePair in _apertureTable
					where data.Count > 0
					where data.Contains(keyValuePair.Key)
					select keyValuePair)
				{
					FNumber.AddValues(item2.Value, item2.Key);
				}
			}
			FNumber.ReloadValues();
			if (property == 0)
			{
				FNumber.IsEnabled = false;
				return;
			}
			FNumber.SetValue((int)property, notifyValuChange: false);
			FNumber.IsEnabled = true;
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : [Error] ReInitFNumber()", exception);
		}
	}

	private void InitIso()
	{
		IsoNumber = new PropertyValue<long>();
		IsoNumber.ValueChanged += IsoNumber_ValueChanged;
		ReInitIso();
	}

	private void IsoNumber_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.PauseLiveview();
			Camera.SetProperty(1026u, val);
			Camera.ResumeLiveview();
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Error set ISO to camera", exception);
		}
	}

	private void ReInitIso()
	{
		try
		{
			List<int> data = GetSettingsList(1026u);
			long property = Camera.GetProperty(1026u);
			bool flag = IsoNumber.Values.Count == 0;
			if (data.Count > 0)
			{
				IsoNumber.Clear();
			}
			if (flag && data.Count == 0)
			{
				foreach (KeyValuePair<uint, string> item in _isoTable)
				{
					IsoNumber.AddValues(item.Value, (int)item.Key);
				}
			}
			else
			{
				foreach (KeyValuePair<uint, string> item2 in from keyValuePair in _isoTable
					where data.Count > 0
					where data.Contains((int)keyValuePair.Key)
					select keyValuePair)
				{
					IsoNumber.AddValues(item2.Value, (int)item2.Key);
				}
			}
			IsoNumber.ReloadValues();
			IsoNumber.SetValue((int)property, notifyValuChange: false);
			IsoNumber.IsEnabled = true;
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Error set iso ", exception);
		}
	}

	private void InitWb()
	{
		List<int> settingsList = GetSettingsList(262u);
		WhiteBalance = new PropertyValue<long>();
		WhiteBalance.ValueChanged += WhiteBalance_ValueChanged;
		try
		{
			foreach (KeyValuePair<uint, string> item in _wbTable)
			{
				if (settingsList.Count > 0 && settingsList.Contains((int)item.Key))
				{
					WhiteBalance.AddValues(item.Value, (int)item.Key);
				}
			}
			WhiteBalance.ReloadValues();
			WhiteBalance.IsEnabled = true;
			WhiteBalance.SetValue(Camera.GetProperty(262u), notifyValuChange: false);
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Error get WB", exception);
		}
	}

	private void WhiteBalance_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.SetProperty(262u, val);
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Error set WB to camera", exception);
		}
	}

	private void InitMetering()
	{
		List<int> settingsList = GetSettingsList(1027u);
		ExposureMeteringMode = new PropertyValue<long>();
		ExposureMeteringMode.ValueChanged += ExposureMeteringMode_ValueChanged;
		try
		{
			foreach (KeyValuePair<uint, string> item in _meteringTable)
			{
				if (settingsList.Contains((int)item.Key))
				{
					ExposureMeteringMode.AddValues(item.Value, (int)item.Key);
				}
			}
			ExposureMeteringMode.ReloadValues();
			ExposureMeteringMode.IsEnabled = true;
			ExposureMeteringMode.SetValue((int)Camera.GetProperty(1027u), notifyValuChange: false);
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Error get metering", exception);
		}
	}

	private void ExposureMeteringMode_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.SetProperty(1027u, val);
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Error set metering mode to camera", exception);
		}
	}

	private void InitOther()
	{
		LiveViewImageZoomRatio = new PropertyValue<long>
		{
			Name = "LiveViewImageZoomRatio"
		};
		LiveViewImageZoomRatio.AddValues("All", 1L);
		LiveViewImageZoomRatio.AddValues("5x", 5L);
		LiveViewImageZoomRatio.SetValue("All");
		LiveViewImageZoomRatio.ReloadValues();
		LiveViewImageZoomRatio.ValueChanged += LiveViewImageZoomRatio_ValueChanged;
	}

	private void LiveViewImageZoomRatio_ValueChanged(object sender, string key, long val)
	{
		Camera.SetProperty(1287u, val);
	}

	public virtual void AddAditionalProps()
	{
		Log.Debug("Canon SDK : LoadProperties 4-13-1", debugMode);
		AdvancedProperties.Add(InitDriveMode());
		Log.Debug("Canon SDK : LoadProperties 4-13-2", debugMode);
		AdvancedProperties.Add(InitBracket());
		Log.Debug("Canon SDK : LoadProperties 4-13-3", debugMode);
		AdvancedProperties.Add(InitAEBracket());
		Log.Debug("Canon SDK : LoadProperties 4-13-4", debugMode);
		foreach (PropertyValue<long> advancedProperty in AdvancedProperties)
		{
			advancedProperty.SetValue(Camera.GetProperty(advancedProperty.Code), notifyValuChange: false);
		}
	}

	private PropertyValue<long> InitDriveMode()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Drive Mode",
			IsEnabled = true,
			Code = 1025u,
			SubType = typeof(uint),
			DisableIfWrongValue = true
		};
		Edsdk.EdsPropertyDesc propertyDescription = Camera.GetPropertyDescription(1025u);
		int[] source = propertyDescription.PropDesc.Take(propertyDescription.NumElements).ToArray();
		if (Enumerable.Contains(source, 0))
		{
			res.AddValues("Single-Frame Shooting", 0L);
		}
		if (Enumerable.Contains(source, 1))
		{
			res.AddValues("Continuous Shooting", 1L);
		}
		if (Enumerable.Contains(source, 2))
		{
			res.AddValues("Video", 2L);
		}
		if (Enumerable.Contains(source, 4))
		{
			res.AddValues("High-Speed Continuous Shooting", 4L);
		}
		if (Enumerable.Contains(source, 5))
		{
			res.AddValues("Low-Speed Continuous Shooting", 5L);
		}
		if (Enumerable.Contains(source, 6))
		{
			res.AddValues("Silent single shooting", 6L);
		}
		if (Enumerable.Contains(source, 7))
		{
			res.AddValues("10-Sec Self-Timer plus continuous shots", 7L);
		}
		if (Enumerable.Contains(source, 10))
		{
			res.AddValues("10-Sec Self-Timer", 10L);
		}
		if (Enumerable.Contains(source, 11))
		{
			res.AddValues("2-Sec Self-Timer", 11L);
		}
		if (Enumerable.Contains(source, 12))
		{
			res.AddValues("14fps super high speed", 12L);
		}
		if (Enumerable.Contains(source, 13))
		{
			res.AddValues("Silent single shooting", 13L);
		}
		if (Enumerable.Contains(source, 14))
		{
			res.AddValues("Silent contin shooting", 14L);
		}
		if (Enumerable.Contains(source, 15))
		{
			res.AddValues("Silent HS continuous", 15L);
		}
		if (Enumerable.Contains(source, 16))
		{
			res.AddValues("Silent LS continuous", 16L);
		}
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			Camera.SetProperty(res.Code, val);
		};
		return res;
	}

	private PropertyValue<long> InitBracket()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Bracketing",
			IsEnabled = false,
			Code = 1035u,
			SubType = typeof(uint),
			DisableIfWrongValue = true
		};
		res.AddValues("Bracket off", 4294967295L);
		res.AddValues("AE bracket", 1L);
		res.AddValues("ISO bracket", 2L);
		res.AddValues("WB bracket", 3L);
		res.AddValues("FE bracket", 4L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			Camera.SetProperty(res.Code, val);
		};
		return res;
	}

	private PropertyValue<long> InitAEBracket()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "AE Bracketing",
			IsEnabled = true,
			Code = 1038u,
			SubType = typeof(uint),
			DisableIfWrongValue = true
		};
		Edsdk.EdsPropertyDesc propertyDescription = Camera.GetPropertyDescription(1038u);
		res.AddValues("Bracket off", 0L);
		for (int i = 1; i < propertyDescription.NumElements; i++)
		{
			res.AddValues("Step " + i, propertyDescription.PropDesc[i]);
		}
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			Camera.SetProperty(res.Code, val);
		};
		return res;
	}

	private void InitCompression()
	{
		CompressionSetting = new PropertyValue<long>();
		CompressionSetting.Name = "CompressionSetting";
		CompressionSetting.ValueChanged += CompressionSetting_ValueChanged;
		ReInitCompression();
	}

	private void ReInitCompression()
	{
		lock (Locker)
		{
			try
			{
				CompressionSetting.Clear();
				List<int> settingsList = GetSettingsList(256u);
				if (settingsList != null && settingsList.Count > 0)
				{
					foreach (int item in settingsList)
					{
						long num = item;
						EosImageQuality q = EosImageQuality.Create(num);
						string qualityName = GetQualityName(q);
						if (!qualityName.Contains("Cr2"))
						{
							CompressionSetting.AddValues(qualityName, (int)num);
						}
					}
				}
				CompressionSetting.ReloadValues();
				long num2 = Camera.ImageQuality.ToBitMask();
				if (num2 == 0)
				{
					CompressionSetting.IsEnabled = false;
					return;
				}
				CompressionSetting.IsEnabled = true;
				CompressionSetting.SetValue(num2, notifyValuChange: false);
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : [Error] ReInitCompression()", exception);
			}
		}
	}

	private string GetQualityName(EosImageQuality q)
	{
		if (q.PrimaryImageFormat == EosImageFormat.Cr2)
		{
			return "Cr2";
		}
		if (q.PrimaryImageFormat.ToString().ToUpper().Contains("RAW"))
		{
			return "RAW";
		}
		string text = q.PrimaryImageSize.ToString().Replace("Middle", "Medium").Replace("Small2", "Small")
			.Replace("Small3", "Smaller")
			.Replace("Small4", "Tiny");
		string text2 = q.PrimaryCompressLevel.ToString();
		string text3 = "JPEG";
		return text + " " + text2 + " " + text3;
	}

	private void CompressionSetting_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.SetProperty(256u, (uint)val);
		}
		catch (Exception exception)
		{
			Log.Error("CanonSDK : Error set property Compression", exception);
		}
	}

	private void InitEc()
	{
		ExposureCompensation = new PropertyValue<long>();
		ExposureCompensation.Name = "ExposureCompensation";
		ExposureCompensation.ValueChanged += ExposureCompensation_ValueChanged;
		ReInitEc();
	}

	private void ReInitEc()
	{
		lock (Locker)
		{
			try
			{
				ExposureCompensation.Clear();
				List<int> settingsList = GetSettingsList(1031u);
				if (settingsList != null && settingsList.Count > 1)
				{
					foreach (int item in settingsList)
					{
						long num = Convert.ToInt64(item);
						ExposureCompensation.AddValues(GetExposureName(num), (int)num);
					}
				}
				else
				{
					foreach (KeyValuePair<long, string> item2 in _ecTable)
					{
						ExposureCompensation.AddValues(item2.Value, (int)item2.Key);
					}
				}
				ExposureCompensation.ReloadValues();
				long property = Camera.GetProperty(1031u);
				ExposureCompensation.IsEnabled = true;
				ExposureCompensation.SetValue(property, notifyValuChange: false);
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : [Error] ReInitEc()", exception);
			}
		}
	}

	private string GetExposureName(long val)
	{
		if (_ecTable.ContainsKey(val))
		{
			return _ecTable[val];
		}
		long key = val & 0xFF;
		if (_ecTable.ContainsKey(key))
		{
			return _ecTable[key];
		}
		return $"Value(0x{val:X})";
	}

	private void ExposureCompensation_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.SetProperty(1031u, val);
		}
		catch (Exception exception)
		{
			Log.Error("CanonSDK : Error set EC to camera", exception);
		}
	}

	private void InitFocus()
	{
		List<int> settingsList = GetSettingsList(1028u);
		FocusMode = new PropertyValue<long>();
		FocusMode.ValueChanged += FocusMode_ValueChanged;
		ReInitFocus();
	}

	private void ReInitFocus()
	{
		lock (Locker)
		{
			try
			{
				FocusMode.Clear();
				List<int> settingsList = GetSettingsList(1028u);
				if (settingsList != null && settingsList.Count > 0)
				{
					foreach (int item in settingsList)
					{
						uint num = (uint)Convert.ToInt32(item);
						if (_focusModeTable.ContainsKey(num))
						{
							FocusMode.AddValues(_focusModeTable[num], (int)num);
						}
					}
				}
				else
				{
					foreach (KeyValuePair<uint, string> item2 in _focusModeTable)
					{
						if (item2.Key != uint.MaxValue)
						{
							FocusMode.AddValues(item2.Value, (int)item2.Key);
						}
					}
				}
				FocusMode.ReloadValues();
				FocusMode.SetValue(Camera.GetProperty(1028u), notifyValuChange: false);
			}
			catch (Exception exception)
			{
				Log.Debug("Focus Error", exception);
			}
		}
	}

	private void FocusMode_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.SetProperty(1028u, val);
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Error set focus mode to camera", exception);
		}
	}

	private PropertyValue<long> InitFlash()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Flash",
			IsEnabled = true,
			Code = 1042u,
			SubType = typeof(uint),
			DisableIfWrongValue = true
		};
		res.AddValues("No flash", 0L);
		res.AddValues("Flash", 1L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			Camera.SetProperty(res.Code, val);
		};
		return res;
	}

	private PropertyValue<long> InitFlahEc()
	{
		PropertyValue<long> propertyValue = new PropertyValue<long>();
		propertyValue.Name = "Flash Compensation";
		propertyValue.IsEnabled = true;
		propertyValue.Code = 1032u;
		propertyValue.SubType = typeof(uint);
		propertyValue.DisableIfWrongValue = true;
		PropertyValue<long> propertyValue2 = propertyValue;
		List<int> settingsList = GetSettingsList(1032u);
		propertyValue2.ValueChanged += FlashExposureCompensation_ValueChanged;
		try
		{
			foreach (KeyValuePair<long, string> item in _ecTable)
			{
				if (settingsList.Count == 0)
				{
					propertyValue2.AddValues(item.Value, (int)item.Key);
				}
				else if (settingsList.Contains((int)item.Key))
				{
					propertyValue2.AddValues(item.Value, (int)item.Key);
				}
			}
			propertyValue2.ReloadValues();
			propertyValue2.IsEnabled = true;
			propertyValue2.SetValue((int)Camera.GetProperty(1032u), notifyValuChange: false);
		}
		catch (Exception exception)
		{
			Log.Debug("Error get EC", exception);
		}
		return propertyValue2;
	}

	private void FlashExposureCompensation_ValueChanged(object sender, string key, long val)
	{
		try
		{
			Camera.SetProperty(1032u, val);
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Error set EC to camera", exception);
		}
	}

	public List<int> GetSettingsList(uint PropID)
	{
		if (Camera.Handle != IntPtr.Zero)
		{
			Edsdk.EdsPropertyDesc outPropertyDesc = default;
			uint num = Edsdk.EdsGetPropertyDesc(Camera.Handle, PropID, out outPropertyDesc);
			return outPropertyDesc.PropDesc.Take(outPropertyDesc.NumElements).ToList();
		}
		return new List<int>();
	}

	private void _camera_Error(object sender, EosExceptionEventArgs e)
	{
		try
		{
			Log.Error("CanonSDK : Canon error", e.Exception);
			StaticHelper.Instance.SystemMessage = e.Exception.Message;
		}
		catch (Exception ex)
		{
			Log.Error("CanonSDK : Error get camera error", ex);
			StaticHelper.Instance.SystemMessage = ex.Message;
		}
	}

	private void _camera_Shutdown(object sender, EventArgs e)
	{
		IsConnected = false;
		OnCameraDisconnected(this, new DisconnectCameraEventArgs
		{
			StillImageDevice = null,
			EosCamera = Camera
		});
	}

	private void _shutdownTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		if (IsBusy || !Monitor.TryEnter(Locker, 10))
		{
			return;
		}
		try
		{
			Camera_WillShutdown(null, null);
		}
		finally
		{
			Monitor.Exit(Locker);
		}
	}

	private void Camera_WillShutdown(object sender, EventArgs e)
	{
		try
		{
			if (PreventShutDown)
			{
				Camera.SendCommand(1u);
				Log.Debug("카메라 잠수모드 연장");
			}
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : Camera_WillShutdown() 에러가 발생했습니다.", exception);
		}
	}

	private void Camera_LiveViewUpdate(object sender, EosLiveImageEventArgs e)
	{
		_liveViewImageData = e;
	}

	private void Camera_LiveViewPaused(object sender, EventArgs e)
	{
		try
		{
		}
		catch (Exception exception)
		{
			IsBusy = false;
			Log.Debug("CanonSDK :Camera_LiveViewPaused() 에러가 발생했습니다.", exception);
		}
	}

	private void Camera_PropertyChanged(object sender, EosPropertyEventArgs e)
	{
		if (IsBusy)
		{
			return;
		}
		lock (Locker)
		{
			try
			{
				switch (e.PropertyId)
				{
				case 1031u:
					ExposureCompensation.SetValue((int)Camera.GetProperty(1031u), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_ExposureCompensation : 0x" + Camera.GetProperty(1031u).ToString("X"), debugMode);
					break;
				case 1024u:
					ReInitFNumber(trigervaluchange: true);
					ReInitShutterSpeed();
					Mode.SetValue((uint)Camera.GetProperty(1024u), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_AEMode : 0x" + Camera.GetProperty(1024u).ToString("X"), debugMode);
					break;
				case 262u:
					WhiteBalance.SetValue(Camera.GetProperty(262u), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_WhiteBalance : 0x" + Camera.GetProperty(262u).ToString("X"), debugMode);
					break;
				case 263u:
					ColorTemperature.SetValue(Camera.GetProperty(263u), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_ColorTemperature : 0x" + Camera.GetProperty(263u).ToString("X"), debugMode);
					break;
				case 1026u:
					IsoNumber.SetValue(Camera.GetProperty(1026u), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_ISOSpeed : 0x" + Camera.GetProperty(1026u).ToString("X"), debugMode);
					break;
				case 1030u:
					ShutterSpeed.SetValue(Camera.GetProperty(1030u), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_Tv : 0x" + Camera.GetProperty(1030u).ToString("X"), debugMode);
					break;
				case 1029u:
					FNumber.SetValue((int)Camera.GetProperty(1029u), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_Av : 0x" + Camera.GetProperty(1029u).ToString("X"), debugMode);
					break;
				case 1027u:
					ExposureMeteringMode.SetValue((int)Camera.GetProperty(1027u), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_MeteringMode : 0x" + Camera.GetProperty(1027u).ToString("X"), debugMode);
					break;
				case 1028u:
					FocusMode.SetValue((int)Camera.GetProperty(1028u), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_AFMode : 0x" + Camera.GetProperty(1028u).ToString("X"), debugMode);
					break;
				case 256u:
				{
					int num3 = (int)Camera.GetProperty(256u);
					CompressionSetting.SetValue((int)Camera.ImageQuality.ToBitMask(), notifyValuChange: false);
					Log.Debug("CanonSDK : PropID_ImageQuality : 0x" + Camera.GetProperty(256u).ToString("X"), debugMode);
					break;
				}
				case 8u:
					Battery = (int)Camera.BatteryLevel + 20;
					break;
				case 16778300u:
				{
					int num2 = (int)Camera.GetProperty(16778300u);
					Log.Debug("CanonSDK : PropID_AEBracketType : 0x" + Camera.GetProperty(16778300u).ToString("X"), debugMode);
					break;
				}
				case 1035u:
				{
					int num = (int)Camera.GetProperty(1035u);
					Log.Debug("CanonSDK : PropID_Bracket : 0x" + Camera.GetProperty(1035u).ToString("X"), debugMode);
					break;
				}
				}
				foreach (PropertyValue<long> item in AdvancedProperties.Where((PropertyValue<long> advancedProperty) => advancedProperty.Code == e.PropertyId))
				{
					item.SetValue(Camera.GetProperty(item.Code), notifyValuChange: false);
				}
			}
			catch (Exception exception)
			{
				Log.Error("CanonSDK : Camera_PropertyChanged() 에러가 발생했습니다. " + e.PropertyId.ToString("X"), exception);
			}
		}
	}

	private void Camera_PictureTaken(object sender, EosImageEventArgs e)
	{
		try
		{
			Log.Debug("CanonSDK : Event - Finish Picture taken");
			PhotoCapturedEventArgs e2 = new PhotoCapturedEventArgs
			{
				WiaImageItem = null,
				CameraDevice = this,
				FileName = "IMG0000.jpg",
				Handle = ((e.Pointer != IntPtr.Zero) ? ((object)e.Pointer) : e)
			};
			if (e is EosFileImageEventArgs e3)
			{
				e2.FileName = Path.GetFileName(e3.ImageFilePath);
			}
			if (e is EosMemoryImageEventArgs e4 && !string.IsNullOrEmpty(e4.FileName))
			{
				e2.FileName = Path.GetFileName(e4.FileName);
			}
			OnPhotoCapture(this, e2);
			OnCaptureCompleted(this, new EventArgs());
		}
		catch (Exception exception)
		{
			Log.Error("CanonSDK : Camera_PictureTaken() 에러가 발생했습니다.", exception);
		}
	}

	public override void CapturePhoto()
	{
		Log.Debug("CanonSDK : CapturePhotoAF() - start");
		Monitor.Enter(Locker);
		try
		{
			IsBusy = true;
			if (Camera.IsInHostLiveViewMode)
			{
				if (Camera.ImageQuality.SecondaryImageFormat != EosImageFormat.Unknown)
				{
					throw new Exception("RAW+JPG capture in live view not supported");
				}
				Camera.PauseLiveview();
				ErrorCodes.GetCanonException(Camera.SendCommand(4u, 1));
				Thread.Sleep(20);
				ErrorCodes.GetCanonException(Camera.SendCommand(4u, 3));
				Thread.Sleep(20);
				ErrorCodes.GetCanonException(Camera.SendCommand(4u));
			}
			else
			{
				Camera.TakePicture();
			}
		}
		catch (Exception exception)
		{
			IsBusy = false;
			PlayWave("C:\\Windows\\Media\\Windows Notify Messaging.wav");
			Log.Debug("CanonSDK : 포커스가 맞지 않습니다", exception);
			throw;
		}
		finally
		{
			Camera.ResumeLiveview();
			Monitor.Exit(Locker);
			Log.Debug("CanonSDK : CapturePhotoAF() - end");
		}
	}

	public override void CapturePhotoNoAf()
	{
		Log.Debug("CanonSDK : CapturePhotoNoAF() - start");
		Monitor.Enter(Locker);
		try
		{
			if (Camera.IsInHostLiveViewMode && Camera.ImageQuality.SecondaryImageFormat != EosImageFormat.Unknown)
			{
				throw new Exception("RAW+JPG capture in live view not supported");
			}
			IsBusy = true;
			Camera.PauseLiveview();
			ErrorCodes.GetCanonException(Camera.SendCommand(4u, 65539));
			Thread.Sleep(20);
			ErrorCodes.GetCanonException(Camera.SendCommand(4u));
		}
		catch (Exception exception)
		{
			IsBusy = false;
			PlayWave("C:\\Windows\\Media\\Windows Notify Messaging.wav");
			Log.Debug("CanonSDK : CapturePhotoNoAF() 에러가 발생했습니다.", exception);
			throw;
		}
		finally
		{
			Camera.ResumeLiveview();
			Monitor.Exit(Locker);
			Log.Debug("CanonSDK : CapturePhotoNoAF() - end");
		}
	}

	private void HalfShutter()
	{
		PressHalfButton();
		ErrorCodes.GetCanonException(Camera.SendCommand(4u, 3));
	}

	public override void StartLiveView()
	{
		_recording = false;
		ResetShutterButton();
		Camera.StartLiveView(EosLiveViewAutoFocus.LiveMode);
	}

	public void StartLiveViewCamera()
	{
		_recording = false;
		ResetShutterButton();
		Camera.StartLiveViewCamera();
	}

	public override void StopLiveView()
	{
		if (Camera != null)
		{
			Camera.StopLiveView();
		}
	}

	public override void StartRecordMovie()
	{
		CameraControl.Devices.LegacyCapturePolicy.RequireMovieRecording();

		lock (Locker)
		{
			_recording = true;
			ResetShutterButton();
			Camera.StartRecord();
		}
	}

	public override void StopRecordMovie()
	{
		lock (Locker)
		{
			Camera.StopRecord();
			_recording = false;
		}
	}

	public override void StartBulbMode()
	{
		Camera.BulbStart();
	}

	public override void EndBulbMode()
	{
		Camera.BulbEnd();
	}

	public override void AutoFocus()
	{
		ResetShutterButton();
		Camera.PauseLiveview();
		try
		{
			ErrorCodes.GetCanonException(Camera.SendCommand(258u, 1));
		}
		finally
		{
			Camera.ResumeLiveview();
		}
	}

	public override void Focus(int x, int y)
	{
		lock (Locker)
		{
			ResetShutterButton();
			if (_liveViewImageData != null)
			{
				x -= _liveViewImageData.ZommBounds.Width / 2;
				y -= _liveViewImageData.ZommBounds.Height / 2;
			}
			if (x < 0)
			{
				x = 0;
			}
			if (y < 0)
			{
				y = 0;
			}
			Camera.LiveViewqueue.Enqueue(() =>
			{
				Camera.SetPropertyIntegerArrayData(1288u, new uint[2]
				{
					(uint)x,
					(uint)y
				});
			});
		}
	}

	public override int Focus(int step)
	{
		ResetShutterButton();
		int num = 0;
		if (Camera.SendCommand(259u, (step < 0) ? 1 : 32769) == 0)
		{
			num += ((step >= 0) ? 1 : (-1));
		}
		return num;
	}

	public override void Focus(FocusDirection direction, FocusAmount amount)
	{
		ResetShutterButton();
		switch (direction)
		{
		case FocusDirection.Far:
			switch (amount)
			{
			case FocusAmount.Small:
				Camera.SendCommand(259u, 32769);
				break;
			case FocusAmount.Medium:
				Camera.SendCommand(259u, 32770);
				break;
			case FocusAmount.Large:
				Camera.SendCommand(259u, 32771);
				break;
			default:
				throw new ArgumentOutOfRangeException("amount", amount, null);
			}
			break;
		case FocusDirection.Near:
			switch (amount)
			{
			case FocusAmount.Small:
				Camera.SendCommand(259u, 1);
				break;
			case FocusAmount.Medium:
				Camera.SendCommand(259u, 2);
				break;
			case FocusAmount.Large:
				Camera.SendCommand(259u, 3);
				break;
			default:
				throw new ArgumentOutOfRangeException("amount", amount, null);
			}
			break;
		}
	}

	// Checked SDK path for the new sequencer; legacy manual-focus behavior is preserved.
	public void PrepareFocusStack()
	{
		lock (Locker)
		{
			ErrorCodes.GetCanonException(Camera.SendCommand(Edsdk.CameraCommand_DoEvfAf, (int)Edsdk.EdsEvfAf.CameraCommand_EvfAf_OFF));
			ErrorCodes.GetCanonException(Camera.SendCommand(Edsdk.CameraCommand_PressShutterButton, (int)Edsdk.EdsShutterButton.CameraCommand_ShutterButton_OFF));
		}
	}

	public void FocusForStack(FocusDirection direction, FocusAmount amount)
	{
		if (!Enum.IsDefined(typeof(FocusDirection), direction) || !Enum.IsDefined(typeof(FocusAmount), amount))
			throw new ArgumentOutOfRangeException("direction/amount");
		lock (Locker)
		{
			if (!IsConnected || Camera == null || !Camera.IsInHostLiveViewMode)
				throw new InvalidOperationException("수퍼포커스 초점 이동에는 연결된 카메라의 라이브뷰가 필요합니다.");
			PrepareFocusStack();
			uint lens = direction == FocusDirection.Near
				? (amount == FocusAmount.Small ? Edsdk.EvfDriveLens_Near1 : amount == FocusAmount.Medium ? Edsdk.EvfDriveLens_Near2 : Edsdk.EvfDriveLens_Near3)
				: (amount == FocusAmount.Small ? Edsdk.EvfDriveLens_Far1 : amount == FocusAmount.Medium ? Edsdk.EvfDriveLens_Far2 : Edsdk.EvfDriveLens_Far3);
			ErrorCodes.GetCanonException(Camera.SendCommand(Edsdk.CameraCommand_DriveLensEvf, (int)lens));
		}
	}

	public void ReleaseButton()
	{
		Camera.SendCommand(4u);
	}

	private uint ResetShutterButton()
	{
		Camera.SendCommand(258u);
		return Camera.SendCommand(4u);
	}

	public void PressButton()
	{
		ResetShutterButton();
		ErrorCodes.GetCanonException(Camera.SendCommand(4u, 3));
	}

	public void PressHalfButton()
	{
		ErrorCodes.GetCanonException(Camera.SendCommand(4u, 1));
	}

	public override bool DeleteObject(DeviceObject deviceObject)
	{
		if (deviceObject.Handle is IntPtr)
		{
			Camera.DeleteItem((IntPtr)deviceObject.Handle);
		}
		return true;
	}

	public override void ReleaseResurce(object o)
	{
		if (o is IntPtr)
		{
			Edsdk.EdsRelease((IntPtr)o);
		}
	}

	public override void TransferFileThumb(object o, string filename)
	{
		base.TransferFileThumb(o, filename);
	}

	public override void TransferFile(object o, string filename)
	{
		lock (Locker)
		{
			IsBusy = true;
			if (o is IntPtr)
			{
				try
				{
					IsBusy = true;
					Camera.PauseLiveview();
					EosImageTransporter eosImageTransporter = new EosImageTransporter();
					eosImageTransporter.ProgressEvent += (int i) =>
					{
						TransferProgress = (uint)i;
					};
					eosImageTransporter.TransportAsFileName((IntPtr)o, filename, Camera.Handle);
					Camera.ResumeLiveview();
				}
				catch (Exception exception)
				{
					IsBusy = false;
					Camera.ResumeLiveview();
					if (File.Exists(filename))
					{
						FileInfo fileInfo = new FileInfo(filename);
						if (fileInfo.Length > 10000)
						{
							File.Delete(filename);
						}
					}
					Log.Error("CanonSDK : 카메라로부터 파일 전송받기 실패했습니다.", exception);
					throw;
				}
			}
			if (o is EosFileImageEventArgs e)
			{
				Log.Debug("File transfer started");
				try
				{
					if (File.Exists(e.ImageFilePath))
					{
						File.Copy(e.ImageFilePath, filename, overwrite: true);
						File.Delete(e.ImageFilePath);
					}
					else
					{
						Log.Error("CanonSDK : Base file not found " + e.ImageFilePath);
					}
				}
				catch (Exception exception2)
				{
					IsBusy = false;
					Log.Error("CanonSDK : Transfer error ", exception2);
					throw;
				}
			}
			if (o is EosMemoryImageEventArgs e2)
			{
				Log.Debug("Memory file transfer started");
				try
				{
					using FileStream fileStream = File.Create(filename, e2.ImageData.Length);
					fileStream.Write(e2.ImageData, 0, e2.ImageData.Length);
				}
				catch (Exception exception3)
				{
					IsBusy = false;
					Log.Error("CanonSDK : Error transfer memory file 2", exception3);
					throw;
				}
			}
			if (o is string fileName)
			{
				GetFile(fileName, filename);
			}
		}
	}

	// Original-preserving transfer for the new focus service, including SDK file callbacks.
	public void TransferFocusFile(object handle, string filename)
	{
		lock (Locker)
		{
			if (File.Exists(filename)) throw new IOException("원본 저장 경로에 이미 파일이 있습니다.");
			IsBusy = true;
			Camera.PauseLiveview();
			try
			{
				if (handle is IntPtr pointer && pointer != IntPtr.Zero)
				{
					var transporter = new EosImageTransporter();
					transporter.ProgressEvent += i => TransferProgress = (uint)i;
					try { transporter.TransportAsFileName(pointer, filename, Camera.Handle); }
					catch
					{
						// Complete or cancel the SDK transfer before releasing its item handle.
						// Cancellation does not delete the camera original or partial local file.
						uint cancelResult = Edsdk.EdsDownloadCancel(pointer);
						if (cancelResult != 0) Log.Debug("Focus download cancellation SDK code: " + cancelResult.ToString("X"));
						throw;
					}
				}
				else if (handle is EosFileImageEventArgs file)
					File.Copy(file.ImageFilePath, filename, overwrite: false);
				else if (handle is EosMemoryImageEventArgs memory)
				{
					using (var stream = new FileStream(filename, FileMode.CreateNew, FileAccess.Write))
						stream.Write(memory.ImageData, 0, memory.ImageData.Length);
				}
				else throw new InvalidOperationException("지원하지 않는 Canon 다운로드 핸들입니다.");
			}
			finally { Camera.ResumeLiveview(); }
		}
	}

	public override string GetProhibitionCondition(OperationEnum operationEnum)
	{
		switch (operationEnum)
		{
		case OperationEnum.RecordMovie:
			if (Camera.GetProperty(1296u) == 4)
			{
				return "LabelRecordInProgres";
			}
			if (Camera.LiveViewDevice == EosLiveViewDevice.None)
			{
				return "LabelWrongLiveViewType";
			}
			break;
		default:
			throw new ArgumentOutOfRangeException("operationEnum", operationEnum, null);
		case OperationEnum.Capture:
		case OperationEnum.AutoFocus:
		case OperationEnum.ManualFocus:
		case OperationEnum.LiveView:
			break;
		}
		return "";
	}

	public override AsyncObservableCollection<DeviceObject> GetObjects(object storageId, bool loadThumbs)
	{
		List<DeviceObject> list = new List<DeviceObject>();
		int outCount = 0;
		Edsdk.EdsGetChildCount(Camera.Handle, out outCount);
		for (int i = 0; i < outCount; i++)
		{
			Edsdk.EdsGetChildAtIndex(Camera.Handle, i, out var outRef);
			Edsdk.EdsGetVolumeInfo(outRef, out var outVolumeInfo);
			if (outVolumeInfo.StorageType == 0)
			{
				throw new Exception("No memory card inserted");
			}
			if (outVolumeInfo.szVolumeLabel != "HDD")
			{
				list.AddRange(GetChildren(outRef, loadThumbs));
			}
			Edsdk.EdsRelease(outRef);
		}
		return new AsyncObservableCollection<DeviceObject>(list);
	}

	private List<DeviceObject> GetChildren(IntPtr ptr, bool loadThumbs)
	{
		List<DeviceObject> list = new List<DeviceObject>();
		Edsdk.EdsGetChildCount(ptr, out var outCount);
		if (outCount > 0)
		{
			for (int i = 0; i < outCount; i++)
			{
				Edsdk.EdsGetChildAtIndex(ptr, i, out var outRef);
				Edsdk.EdsGetDirectoryItemInfo(outRef, out var outDirItemInfo);
				if (outDirItemInfo.isFolder == 0)
				{
					if (loadThumbs)
					{
						Edsdk.EdsCreateMemoryStream(0uL, out var outStream);
						Edsdk.EdsDownloadThumbnail(outRef, outStream);
						list.Add(new DeviceObject
						{
							FileName = outDirItemInfo.szFileName,
							ThumbData = GetImage(outStream, Edsdk.EdsImageSource.Thumbnail),
							Handle = outDirItemInfo.szFileName
						});
						if (outStream != IntPtr.Zero)
						{
							Edsdk.EdsRelease(outStream);
						}
					}
					else
					{
						list.Add(new DeviceObject
						{
							FileName = outDirItemInfo.szFileName,
							Handle = outRef
						});
					}
				}
				else
				{
					list.AddRange(GetChildren(outRef, loadThumbs));
					Edsdk.EdsRelease(outRef);
				}
			}
		}
		return list;
	}

	public void GetFile(string fileName, string outFileName)
	{
		int outCount = 0;
		Edsdk.EdsGetChildCount(Camera.Handle, out outCount);
		for (int i = 0; i < outCount; i++)
		{
			Edsdk.EdsGetChildAtIndex(Camera.Handle, i, out var outRef);
			Edsdk.EdsGetVolumeInfo(outRef, out var outVolumeInfo);
			if (outVolumeInfo.szVolumeLabel != "HDD")
			{
				GetFile(outRef, fileName, outFileName);
			}
			Edsdk.EdsRelease(outRef);
		}
	}

	private void GetFile(IntPtr ptr, string fileName, string outfileName)
	{
		List<DeviceObject> list = new List<DeviceObject>();
		Edsdk.EdsGetChildCount(ptr, out var outCount);
		if (outCount <= 0)
		{
			return;
		}
		for (int i = 0; i < outCount; i++)
		{
			Edsdk.EdsGetChildAtIndex(ptr, i, out var outRef);
			Edsdk.EdsGetDirectoryItemInfo(outRef, out var outDirItemInfo);
			if (outDirItemInfo.isFolder == 0)
			{
				if (outDirItemInfo.szFileName == fileName)
				{
					Camera._transporter.TransportAsFileName(outRef, outfileName, Camera.Handle);
				}
			}
			else
			{
				GetFile(outRef, fileName, outfileName);
			}
			Edsdk.EdsRelease(outRef);
		}
	}

	private byte[] GetImage(IntPtr img_stream, Edsdk.EdsImageSource imageSource)
	{
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Expected Obj, but got Unknown
		IntPtr outStream = IntPtr.Zero;
		IntPtr outImageRef = IntPtr.Zero;
		IntPtr outPointer = IntPtr.Zero;
		Edsdk.EdsSize inDstSize = default;
		Edsdk.EdsCreateImageRef(img_stream, out outImageRef);
		Edsdk.EdsGetImageInfo(outImageRef, imageSource, out var outImageInfo);
		inDstSize.width = outImageInfo.EffectiveRect.width;
		inDstSize.height = outImageInfo.EffectiveRect.height;
		int num = (inDstSize.width * 3 + 3) & -4;
		uint num2 = (uint)(inDstSize.height * num);
		byte[] array = new byte[num2];
		Edsdk.EdsCreateMemoryStreamFromPointer(array, num2, out outStream);
		Edsdk.EdsGetImage(outImageRef, imageSource, Edsdk.EdsTargetImageType.RGB, outImageInfo.EffectiveRect, inDstSize, outStream);
		if (inDstSize.width % 4 == 0)
		{
			for (int i = 0; i < num2; i += 3)
			{
				byte b = array[i];
				array[i] = array[i + 2];
				array[i + 2] = b;
			}
		}
		else
		{
			int num3 = num - inDstSize.width * 3;
			for (int num4 = inDstSize.height - 1; num4 > -1; num4--)
			{
				int sourceIndex = inDstSize.width * 3 * num4;
				int num5 = num * num4;
				Array.Copy(array, sourceIndex, array, num5, inDstSize.width * 3);
				for (int j = num5; j < num5 + inDstSize.width * 3; j += 3)
				{
					byte b = array[j];
					array[j] = array[j + 2];
					array[j + 2] = b;
				}
			}
		}
		Edsdk.EdsGetPointer(outStream, out outPointer);
		Edsdk.EdsRelease(img_stream);
		Edsdk.EdsRelease(outImageRef);
		Edsdk.EdsRelease(outStream);
		try
		{
			Bitmap val = new Bitmap(inDstSize.width, inDstSize.height, num, (PixelFormat)137224, outPointer);
			using MemoryStream memoryStream = new MemoryStream();
			((Image)val).Save((Stream)memoryStream, ImageFormat.Bmp);
			memoryStream.Close();
			return memoryStream.ToArray();
		}
		catch (Exception exception)
		{
			Log.Error("CanonSDK : GetImage() 에러가 발생했습니다.", exception);
		}
		return new byte[0];
	}

	public override LiveViewData GetLiveViewImage()
	{
		LiveViewData liveViewData = new LiveViewData();
		if (Monitor.TryEnter(Locker, 10))
		{
			try
			{
				if (Camera == null)
				{
					return liveViewData;
				}
				Camera.DownloadEvf();
				if (_liveViewImageData != null)
				{
					liveViewData.HaveFocusData = true;
					liveViewData.ImageDataPosition = 0;
					liveViewData.ImageData = _liveViewImageData.ImageData;
					liveViewData.ImageHeight = _liveViewImageData.ImageSize.Height;
					liveViewData.ImageWidth = _liveViewImageData.ImageSize.Width;
					liveViewData.LiveViewImageHeight = 100;
					liveViewData.LiveViewImageWidth = 100;
					liveViewData.FocusX = _liveViewImageData.ZommBounds.X + _liveViewImageData.ZommBounds.Width / 2;
					liveViewData.FocusY = _liveViewImageData.ZommBounds.Y + _liveViewImageData.ZommBounds.Height / 2;
					liveViewData.FocusFrameXSize = _liveViewImageData.ZommBounds.Width;
					liveViewData.FocusFrameYSize = _liveViewImageData.ZommBounds.Height;
					liveViewData.MovieIsRecording = _recording;
				}
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : GetLiveViewImage() 에러가 발생했습니다.", exception);
				throw;
			}
			finally
			{
				Monitor.Exit(Locker);
			}
		}
		return liveViewData;
	}

	public override void FormatStorage(object storageId)
	{
		try
		{
			lock (Locker)
			{
				Camera.Lock();
				int outCount = 0;
				Log.Debug("EdsGetChildCount");
				if (Edsdk.EdsGetChildCount(Camera.Handle, out outCount) != 0)
				{
					throw new DeviceException("Error EdsGetChildCount");
				}
				for (int i = 0; i < outCount; i++)
				{
					Log.Debug("EdsGetChildAtIndex");
					if (Edsdk.EdsGetChildAtIndex(Camera.Handle, i, out var outRef) != 0)
					{
						throw new DeviceException("Error EdsGetChildAtIndex");
					}
					Log.Debug("EdsGetVolumeInfo");
					if (Edsdk.EdsGetVolumeInfo(outRef, out var outVolumeInfo) != 0)
					{
						throw new DeviceException("Error EdsGetVolumeInfo");
					}
					if (outVolumeInfo.szVolumeLabel != "HDD")
					{
						Log.Debug("EdsFormatVolume");
						if (Edsdk.EdsFormatVolume(outRef) != 0)
						{
							throw new DeviceException("Error EdsFormatVolume");
						}
					}
					Log.Debug("EdsRelease");
					if (Edsdk.EdsRelease(outRef) != 0)
					{
						throw new DeviceException("Error EdsRelease");
					}
				}
			}
		}
		finally
		{
			Camera.Unlock();
		}
	}

	public override string ToStringCameraData()
	{
		StringBuilder stringBuilder = new StringBuilder(base.ToStringCameraData() + "\n\tType..................Canon (SDK)");
		if (Camera != null)
		{
			stringBuilder.AppendFormat("\n\tFirmware version......{0}", Camera.FirmwareVersion);
			stringBuilder.AppendFormat("\n\tDate/Time.............{0}", DateTime.ToString());
			stringBuilder.AppendFormat("\n\tBattery...............{0,3}%, quality {1}, (note base class value is this + 20)", Camera.BatteryLevel, Camera.BatteryQuality);
			stringBuilder.AppendFormat("\n\tIs in LiveView Mode...{0}", Camera.IsInLiveViewMode ? "Yes" : "No");
			stringBuilder.Append("\n\tImage Quality - Primary");
			stringBuilder.AppendFormat("\n\t  Format..............{0}", Camera.ImageQuality.PrimaryImageFormat);
			stringBuilder.AppendFormat("\n\t  Size................{0}", Camera.ImageQuality.PrimaryImageSize);
			stringBuilder.AppendFormat("\n\t  Compression.........{0}", Camera.ImageQuality.PrimaryCompressLevel);
			if (!Camera.ImageQuality.SecondaryImageFormat.Equals(EosImageFormat.Unknown))
			{
				stringBuilder.Append("\n\tImage Quality - Secondary");
				stringBuilder.AppendFormat("\n\t  Format..............{0}", Camera.ImageQuality.SecondaryImageFormat);
				stringBuilder.AppendFormat("\n\t  Size................{0}", Camera.ImageQuality.SecondaryImageSize);
				stringBuilder.AppendFormat("\n\t  Compression.........{0}", Camera.ImageQuality.SecondaryCompressLevel);
			}
			stringBuilder.AppendFormat("\n\tArtist................{0}", Camera.Artist);
			stringBuilder.AppendFormat("\n\tCopyright (c).........{0}", Camera.Copyright);
			stringBuilder.AppendFormat("\n\tDepth Preview.........{0}", Camera.DepthOfFieldPreview ? "Yes" : "No");
			stringBuilder.AppendFormat("\n\tWhite Balance.........{0}", Camera.WhiteBalance);
		}
		return stringBuilder.ToString();
	}

	private void PlayWave(string filePath)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected Obj, but got Unknown
		if (!string.IsNullOrWhiteSpace(filePath))
		{
			SoundPlayer val = new SoundPlayer(filePath);
			val.Play();
		}
	}

	private void InitColorTemperature()
	{
		List<int> settingsList = GetSettingsList(263u);
		ColorTemperature = new PropertyValue<long>();
		ColorTemperature.ValueChanged += ColorTemperature_ValueChanged;
		try
		{
			foreach (KeyValuePair<int, string> item in _colorTemperatureTable)
			{
				if (settingsList.Contains(item.Key))
				{
					ColorTemperature.AddValues(item.Value, item.Key);
				}
			}
			ColorTemperature.ReloadValues();
			ColorTemperature.IsEnabled = true;
			ColorTemperature.SetValue((int)Camera.GetProperty(263u), notifyValuChange: false);
		}
		catch (Exception exception)
		{
			Log.Debug("CanonSDK : InitColorTemperature() 에러가 발생했습니다.", exception);
		}
	}

	private void ColorTemperature_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			try
			{
				Camera.SetProperty(263u, val);
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : ColorTemperature_ValueChanged() 에러가 발생했습니다.", exception);
			}
		}
	}

	public override void SetColorTemperature(long val)
	{
		lock (Locker)
		{
			try
			{
				Camera.SetProperty(263u, val);
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : SetColorTemperature() 에러가 발생했습니다.", exception);
			}
		}
	}

	public override long GetColorTemperature()
	{
		lock (Locker)
		{
			try
			{
				return Camera.GetProperty(263u);
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : GetColorTemperature() 에러가 발생했습니다.", exception);
				return 0L;
			}
		}
	}

	public override bool IsModeColorTemperature()
	{
		lock (Locker)
		{
			try
			{
				long property = Camera.GetProperty(262u);
				if (property == 9)
				{
					return true;
				}
				return false;
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : IsModeColorTemperature() 에러가 발생했습니다.", exception);
				return false;
			}
		}
	}

	public override void StopFocus()
	{
		lock (Locker)
		{
			try
			{
				Camera.SendCommand(258u);
				Camera.SendCommand(4u);
			}
			catch (Exception exception)
			{
				Log.Debug("CanonSDK : StopFocus() 에러가 발생했습니다.", exception);
			}
		}
	}
}
