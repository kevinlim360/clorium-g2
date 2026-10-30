using System;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Nikon;

public class NikonD7100 : NikonD600Base
{
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
		viewData.PeakSoundL = (int)((double)(int)result[352] / 14.0 * 100.0);
		viewData.PeakSoundR = (int)((double)(int)result[353] / 14.0 * 100.0);
		viewData.SoundL = (int)((double)(int)result[354] / 14.0 * 100.0);
		viewData.SoundR = (int)((double)(int)result[355] / 14.0 * 100.0);
		viewData.HaveSoundData = true;
	}

	protected override void InitFNumber()
	{
		base.InitFNumber();
		MovieFNumber.IsEnabled = false;
	}

	protected override PropertyValue<long> CaptureAreaCrop()
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Name = "Capture area crop",
			IsEnabled = true,
			Code = 53296u,
			SubType = typeof(sbyte)
		};
		res.AddValues("DX", 0L);
		res.AddValues("1.3x", 1L);
		res.ValueChanged += (object sender, string key, long val) =>
		{
			SetProperty(4118u, BitConverter.GetBytes(val), res.Code);
		};
		return res;
	}
}
