namespace CameraControl.Devices.Classes;

public class LiveViewData
{
	public int LiveViewImageWidth { get; set; }

	public int LiveViewImageHeight { get; set; }

	public int ImageWidth { get; set; }

	public int ImageHeight { get; set; }

	public int FocusX { get; set; }

	public int FocusY { get; set; }

	public int FocusFrameXSize { get; set; }

	public int FocusFrameYSize { get; set; }

	public bool HaveFocusData { get; set; }

	public byte[] ImageData { get; set; }

	public bool Focused { get; set; }

	public int ImageDataPosition { get; set; }

	public int Rotation { get; set; }

	public bool MovieIsRecording { get; set; }

	public bool IsLiveViewRunning { get; set; }

	public bool HaveLevelAngleData { get; set; }

	public decimal LevelAngleRolling { get; set; }

	public decimal LevelAnglePitching { get; set; }

	public decimal LevelAngleYawing { get; set; }

	public decimal MovieTimeRemain { get; set; }

	public int SoundL { get; set; }

	public int SoundR { get; set; }

	public int PeakSoundL { get; set; }

	public int PeakSoundR { get; set; }

	public bool HaveSoundData { get; set; }

	public LiveViewData()
	{
		IsLiveViewRunning = true;
		HaveLevelAngleData = false;
		SoundL = 0;
		SoundR = 0;
		HaveSoundData = false;
	}
}
