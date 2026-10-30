using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Nikon;

public class NikonD5200 : NikonD600Base
{
	protected override PropertyValue<long> InitExposureDelay()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Exposure delay mode",
			IsEnabled = true,
			Code = 53354u
		};
		res.AddValues("OFF", 0L);
		res.AddValues("1 sec", 1L);
		res.ReloadValues();
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, new byte[1] { (byte)val }, res.Code);
		};
		return res;
	}
}
