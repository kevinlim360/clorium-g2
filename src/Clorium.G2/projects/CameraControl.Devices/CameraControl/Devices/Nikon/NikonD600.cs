using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Nikon;

public class NikonD600 : NikonD600Base
{
	public NikonD600()
	{
		_isoTable = new Dictionary<uint, string>
		{
			{ 50u, "Lo 1.0" },
			{ 64u, "Lo 0.7" },
			{ 72u, "Lo 0.5" },
			{ 80u, "Lo 0.3" },
			{ 100u, "100" },
			{ 125u, "125" },
			{ 140u, "140" },
			{ 160u, "160" },
			{ 200u, "200" },
			{ 250u, "250" },
			{ 280u, "280" },
			{ 320u, "320" },
			{ 400u, "400" },
			{ 500u, "500" },
			{ 560u, "560" },
			{ 640u, "640" },
			{ 800u, "800" },
			{ 1000u, "1000" },
			{ 1250u, "1250" },
			{ 1600u, "1600" },
			{ 2000u, "2000" },
			{ 2200u, "2200" },
			{ 2500u, "2500" },
			{ 3200u, "3200" },
			{ 4000u, "4000" },
			{ 4500u, "4500" },
			{ 5000u, "5000" },
			{ 6400u, "6400" },
			{ 9000u, "Hi 0.5" },
			{ 12800u, "Hi 1" },
			{ 25600u, "Hi 2" }
		};
		_autoIsoTable = new Dictionary<byte, string>
		{
			{ 0, "200" },
			{ 1, "250" },
			{ 2, "280" },
			{ 3, "320" },
			{ 4, "400" },
			{ 5, "500" },
			{ 6, "560" },
			{ 7, "640" },
			{ 8, "800" },
			{ 9, "1000" },
			{ 10, "1100" },
			{ 11, "1250" },
			{ 12, "1600" },
			{ 13, "2000" },
			{ 14, "2200" },
			{ 15, "2500" },
			{ 16, "3200" },
			{ 17, "4000" },
			{ 18, "4500" },
			{ 19, "5000" },
			{ 20, "6400" },
			{ 21, "Hi 0.3" },
			{ 22, "Hi 0.5" },
			{ 23, "Hi 0.7" },
			{ 24, "Hi 1" },
			{ 25, "Hi 2" }
		};
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

	protected override PropertyValue<long> InitExposureDelay()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Exposure delay mode",
			IsEnabled = true,
			Code = 53354u
		};
		res.AddValues("3 sec", 3L);
		res.AddValues("2 sec", 2L);
		res.AddValues("1 sec", 1L);
		res.AddValues("OFF", 0L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, new byte[1] { (byte)val }, res.Code);
		};
		return res;
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
			case 20495u:
				NormalIsoNumber.SetValue(StillImageDevice.ExecuteReadData(4117u, 20495u), notifyValuChange: false);
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
