using System;
using System.Collections.Generic;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Nikon;

public class NikonD500 : NikonD500Base
{
	public NikonD500()
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
			{ 204800u, "Hi 2" },
			{ 409600u, "Hi 3" },
			{ 820000u, "Hi 4" },
			{ 1640000u, "Hi 5" }
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
			{ 37, "Hi 2" },
			{ 38, "Hi 3" },
			{ 39, "Hi 4" },
			{ 40, "Hi 5" }
		};
	}

	protected override void InitCompressionSetting()
	{
		_csTable = new Dictionary<int, string>
		{
			{ 0, "JPEG (BASIC)" },
			{ 1, "JPEG (BASIC*)" },
			{ 2, "JPEG (NORMAL)" },
			{ 3, "JPEG (NORMAL*)" },
			{ 4, "JPEG (FINE)" },
			{ 5, "JPEG (FINE*)" },
			{ 6, "TIFF (RGB)" },
			{ 7, "RAW" },
			{ 8, "RAW + JPEG (BASIC)" },
			{ 9, "RAW + JPEG (BASIC*)" },
			{ 10, "RAW + JPEG (NORMAL)" },
			{ 11, "RAW + JPEG (NORMAL*)" },
			{ 12, "RAW + JPEG (FINE)" },
			{ 13, "RAW + JPEG (FINE*)" }
		};
		base.InitCompressionSetting();
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
		res.AddValues("Single frame (S)", 1L);
		res.AddValues("Continuous low speed (CL)", 32784L);
		res.AddValues("Continuous high speed (CH)", 2L);
		res.AddValues("Quiet shutter-release (Q)", 32790L);
		res.AddValues("Quiet continuous shutter-release (QC)", 32792L);
		res.AddValues("Self-timer", 32785L);
		res.AddValues("Mirror up", 32786L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}
}
