using System;
using System.Collections.Generic;

namespace CameraControl.Devices.Nikon;

public class NikonZ7 : NikonD500
{
	public NikonZ7()
	{
		_isoTable = new Dictionary<uint, string>
		{
			{ 32u, "Lo 1.0" },
			{ 40u, "Lo 0.7" },
			{ 45u, "Lo 0.5" },
			{ 50u, "Lo 0.3" },
			{ 64u, "64" },
			{ 72u, "72" },
			{ 80u, "80" },
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
			{ 8000u, "8000" },
			{ 10000u, "10000" },
			{ 12800u, "12800" },
			{ 16000u, "16000" },
			{ 20000u, "20000" },
			{ 25600u, "25600" },
			{ 32000u, "Hi 0.3" },
			{ 40000u, "Hi 0.7" },
			{ 51200u, "Hi 1.0" },
			{ 102400u, "Hi 2.0" }
		};
		_autoIsoTable = new Dictionary<byte, string>
		{
			{ 0, "100" },
			{ 1, "125" },
			{ 2, "140" },
			{ 3, "160" },
			{ 4, "200" },
			{ 5, "250" },
			{ 6, "280" },
			{ 7, "320" },
			{ 8, "400" },
			{ 9, "500" },
			{ 10, "560" },
			{ 11, "640" },
			{ 12, "800" },
			{ 13, "1000" },
			{ 14, "1100" },
			{ 15, "1250" },
			{ 16, "1600" },
			{ 17, "2000" },
			{ 18, "2200" },
			{ 19, "2500" },
			{ 20, "3200" },
			{ 21, "4000" },
			{ 22, "4500" },
			{ 23, "5000" },
			{ 24, "6400" },
			{ 25, "8000" },
			{ 26, "9000" },
			{ 27, "10000" },
			{ 28, "12800" },
			{ 29, "16000" },
			{ 30, "18000" },
			{ 31, "20000" },
			{ 32, "25600" },
			{ 33, "Hi 0.3" },
			{ 34, "Hi 0.5" },
			{ 35, "Hi 0.7" },
			{ 36, "Hi 1" },
			{ 37, "Hi 2" }
		};
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
}
