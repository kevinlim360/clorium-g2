using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Nikon;

public class NikonD500Base : NikonD800
{
	public NikonD500Base()
	{
		_shutterTable = new Dictionary<uint, string>
		{
			{ 73536u, "1/8000" },
			{ 71936u, "1/6400" },
			{ 71536u, "1/6000" },
			{ 70536u, "1/5000" },
			{ 69536u, "1/4000" },
			{ 68736u, "1/3200" },
			{ 68036u, "1/2500" },
			{ 67536u, "1/2000" },
			{ 67136u, "1/1600" },
			{ 66786u, "1/1250" },
			{ 66536u, "1/1000" },
			{ 66336u, "1/800" },
			{ 66286u, "1/750" },
			{ 66176u, "1/640" },
			{ 66036u, "1/500" },
			{ 65936u, "1/400" },
			{ 65886u, "1/350" },
			{ 65856u, "1/320" },
			{ 65786u, "1/250" },
			{ 65736u, "1/200" },
			{ 65716u, "1/180" },
			{ 65696u, "1/160" },
			{ 65661u, "1/125" },
			{ 65636u, "1/100" },
			{ 65626u, "1/90" },
			{ 65616u, "1/80" },
			{ 65596u, "1/60" },
			{ 65586u, "1/50" },
			{ 65581u, "1/45" },
			{ 65576u, "1/40" },
			{ 65566u, "1/30" },
			{ 65561u, "1/25" },
			{ 65556u, "1/20" },
			{ 65551u, "1/15" },
			{ 65549u, "1/13" },
			{ 65546u, "1/10" },
			{ 65544u, "1/8" },
			{ 65542u, "1/6" },
			{ 65541u, "1/5" },
			{ 65540u, "1/4" },
			{ 65539u, "1/3" },
			{ 655385u, "1/2.5" },
			{ 65538u, "1/2" },
			{ 655376u, "1/1.6" },
			{ 655375u, "1/1.5" },
			{ 655373u, "1/1.3" },
			{ 65537u, "1s" },
			{ 851978u, "1.3s" },
			{ 983050u, "1.5s" },
			{ 1048586u, "1.6s" },
			{ 131073u, "2s" },
			{ 1638410u, "2.5s" },
			{ 196609u, "3s" },
			{ 262145u, "4s" },
			{ 327681u, "5s" },
			{ 393217u, "6s" },
			{ 524289u, "8s" },
			{ 655361u, "10s" },
			{ 851969u, "13s" },
			{ 983041u, "15s" },
			{ 1310721u, "20s" },
			{ 1638401u, "25s" },
			{ 1966081u, "30s" },
			{
				uint.MaxValue,
				"Bulb"
			},
			{ 4294967293u, "Time" },
			{ 4294967294u, "Flash Sync" }
		};
	}

	public override LiveViewData GetLiveViewImage()
	{
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
					return null;
				}
				GetAdditionalLiveViewData(liveViewData, mTPDataResponse.Data);
				liveViewData.ImageDataPosition = 384;
				liveViewData.ImageData = mTPDataResponse.Data;
			}
			finally
			{
				Monitor.Exit(Locker);
			}
		}
		return liveViewData;
	}

	protected override void GetAdditionalLiveViewData(LiveViewData viewData, byte[] result)
	{
		viewData.LiveViewImageWidth = BaseMTPCamera.ToInt16(result, 8);
		viewData.LiveViewImageHeight = BaseMTPCamera.ToInt16(result, 10);
		viewData.ImageWidth = BaseMTPCamera.ToInt16(result, 12);
		viewData.ImageHeight = BaseMTPCamera.ToInt16(result, 14);
		viewData.FocusFrameXSize = BaseMTPCamera.ToInt16(result, 24);
		viewData.FocusFrameYSize = BaseMTPCamera.ToInt16(result, 26);
		viewData.FocusX = BaseMTPCamera.ToInt16(result, 28);
		viewData.FocusY = BaseMTPCamera.ToInt16(result, 30);
		viewData.Focused = result[48] != 1;
		viewData.MovieIsRecording = result[68] == 1;
		viewData.MovieTimeRemain = ToDeciaml(result, 64);
		if (result[37] == 1)
		{
			viewData.Rotation = -90;
		}
		if (result[37] == 2)
		{
			viewData.Rotation = 90;
		}
		viewData.HaveLevelAngleData = true;
		viewData.LevelAngleRolling = BaseMTPCamera.ToInt16(result, 52);
		viewData.LevelAnglePitching = BaseMTPCamera.ToInt16(result, 56);
		viewData.LevelAngleYawing = BaseMTPCamera.ToInt16(result, 60);
		viewData.PeakSoundL = (int)((double)(int)result[352] / 14.0 * 100.0);
		viewData.PeakSoundR = (int)((double)(int)result[353] / 14.0 * 100.0);
		viewData.SoundL = (int)((double)(int)result[354] / 14.0 * 100.0);
		viewData.SoundR = (int)((double)(int)result[355] / 14.0 * 100.0);
		viewData.HaveSoundData = true;
	}

	protected override PropertyValue<long> InitStillCaptureMode()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Still Capture Mode",
			IsEnabled = true,
			Code = 20499u,
			SubType = typeof(ushort)
		};
		res.AddValues("Single shot (single-frame shooting)", 1L);
		res.AddValues("Continuous high-speed shooting (CH)", 2L);
		res.AddValues("Continuous low-speed shooting (CL)", 32784L);
		res.AddValues("Self-timer", 32785L);
		res.AddValues("Mirror-up", 32786L);
		res.AddValues("Quiet shooting", 32790L);
		res.AddValues("Remote control", 32791L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
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
		res.AddValues("1 sec", 2L);
		res.AddValues("OFF", 3L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, new byte[1] { (byte)val }, res.Code);
		};
		return res;
	}

	protected override void InitIso()
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
				MTPDataResponse mTPDataResponse = ExecuteReadDataEx(4116u, 53428u);
				uint num = BitConverter.ToUInt32(mTPDataResponse.Data, 9);
				for (int i = 0; i < mTPDataResponse.Data.Length - 16; i += 4)
				{
					uint num2 = BitConverter.ToUInt32(mTPDataResponse.Data, 16 + i);
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
				uint num3 = BitConverter.ToUInt32(mTPDataResponse2.Data, 9);
				for (int j = 0; j < mTPDataResponse2.Data.Length - 16; j += 4)
				{
					uint num4 = BitConverter.ToUInt32(mTPDataResponse2.Data, 16 + j);
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
			SetProperty(4118u, BitConverter.GetBytes((int)val), 53428u);
		}
	}

	private void MovieIsoNumber_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			SetProperty(4118u, BitConverter.GetBytes((int)val), 53674u);
		}
	}

	protected override void InitShutterSpeed()
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
		uint value = Convert.ToUInt32(val & 0xFFFFFFFFu);
		if (Mode != null && (Mode.Value == "M" || Mode.Value == "S"))
		{
			if (key == "Bulb")
			{
				SetProperty(4118u, BitConverter.GetBytes(uint.MaxValue), 53504u);
			}
			else
			{
				SetProperty(4118u, BitConverter.GetBytes(value), 53504u);
			}
		}
	}

	private void MovieShutterSpeed_ValueChanged(object sender, string key, long val)
	{
		uint value = Convert.ToUInt32(val & 0xFFFFFFFFu);
		if (Mode != null && (Mode.Value == "M" || Mode.Value == "S"))
		{
			if (key == "Bulb")
			{
				SetProperty(4118u, BitConverter.GetBytes(uint.MaxValue), 53672u);
			}
			else
			{
				SetProperty(4118u, BitConverter.GetBytes(value), 53672u);
			}
		}
	}

	protected override void ReInitShutterSpeed()
	{
		lock (Locker)
		{
			DeviceReady();
			try
			{
				byte b = 4;
				MTPDataResponse mTPDataResponse = StillImageDevice.ExecuteReadData(4116u, 53504u);
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
			case 53428u:
				NormalIsoNumber.SetValue(StillImageDevice.ExecuteReadData(4117u, 53428u), notifyValuChange: false);
				return;
			case 53674u:
				MovieFNumber.SetValue(StillImageDevice.ExecuteReadData(4117u, 53674u), notifyValuChange: false);
				return;
			case 53504u:
				NormalShutterSpeed.SetValue(StillImageDevice.ExecuteReadData(4117u, 53504u), notifyValuChange: false);
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
}
