using System;
using System.Collections.Generic;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Nikon;

public class NikonZ6 : NikonD500
{
	public const uint CONST_CMD_LiveViewZoomArea = 53693u;

	public NikonZ6()
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
			{ 8000u, "8000" },
			{ 10000u, "10000" },
			{ 12800u, "12800" },
			{ 16000u, "16000" },
			{ 18000u, "18000" },
			{ 20000u, "20000" },
			{ 25600u, "25600" },
			{ 32000u, "32000" },
			{ 36000u, "36000" },
			{ 40000u, "40000" },
			{ 51200u, "51200" },
			{ 64000u, "Hi 0.3" },
			{ 72000u, "Hi 0.5" },
			{ 81200u, "Hi 0.7" },
			{ 102400u, "Hi 1" },
			{ 204800u, "Hi 2" }
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
			{ 21, "8000" },
			{ 22, "9000" },
			{ 23, "10000" },
			{ 24, "12800" },
			{ 25, "16000" },
			{ 26, "18000" },
			{ 27, "20000" },
			{ 28, "25600" },
			{ 29, "32000" },
			{ 30, "36000" },
			{ 31, "40000" },
			{ 32, "51200" },
			{ 33, "Hi 0.3" },
			{ 34, "Hi 0.5" },
			{ 35, "Hi 0.7" },
			{ 36, "Hi 1" },
			{ 37, "Hi 2" }
		};
	}

	protected override void InitFNumber()
	{
		NormalFNumber = new PropertyValue<long>
		{
			IsEnabled = true,
			Name = "FNumber"
		};
		NormalFNumber.ValueChanged += NormalFNumber_ValueChanged;
		NormalFNumber.SubType = typeof(ushort);
		MovieFNumber = new PropertyValue<long>
		{
			IsEnabled = true,
			Name = "FNumber"
		};
		MovieFNumber.ValueChanged += MovieFNumber_ValueChanged;
		MovieFNumber.SubType = typeof(ushort);
		ReInitFNumber(trigervaluchange: false);
	}

	private void MovieFNumber_ValueChanged(object sender, string key, long val)
	{
		if (Mode != null && (Mode.Value == "A" || Mode.Value == "M"))
		{
			SetProperty(4118u, BitConverter.GetBytes((ushort)val), 53673u);
		}
	}

	private void NormalFNumber_ValueChanged(object sender, string key, long val)
	{
		if (Mode != null && (Mode.Value == "A" || Mode.Value == "M"))
		{
			SetProperty(4118u, BitConverter.GetBytes((ushort)val), 20487u);
		}
	}

	protected override void InitOther()
	{
		LiveViewImageZoomRatio = new PropertyValue<long>
		{
			Name = "LiveViewImageZoomRatio"
		};
		LiveViewImageZoomRatio.SubType = typeof(ushort);
		LiveViewImageZoomRatio.AddValues("All", 0L);
		LiveViewImageZoomRatio.AddValues("50%", 2048L);
		LiveViewImageZoomRatio.AddValues("100%", 1024L);
		LiveViewImageZoomRatio.AddValues("200%", 512L);
		LiveViewImageZoomRatio.SetValue("All");
		LiveViewImageZoomRatio.ReloadValues();
		LiveViewImageZoomRatio.ValueChanged += LiveViewImageZoomRatio_ValueChanged;
	}

	protected override void LiveViewImageZoomRatio_ValueChanged(object sender, string key, long val)
	{
		lock (Locker)
		{
			SetProperty(4118u, BitConverter.GetBytes((uint)val), 53693u);
		}
	}

	public override void StartRecordMovie()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		CameraControl.Devices.LegacyCapturePolicy.RequireMovieRecording();

		SetProperty(4118u, new byte[1] { 1 }, 53670u);
		base.StartRecordMovie();
		DeviceReady();
		ErrorCodes.GetException(ExecuteWithNoData(37386u));
	}
}
