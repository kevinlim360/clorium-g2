using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices.Others;

public class PtzOpticsCamera : BaseCameraDevice
{
	private string _address = "";

	public PtzOpticsCamera(string ip)
	{
		_address = ip;
		IsBusy = false;
		IsConnected = true;
		HaveLiveView = false;
		ExposureStatus = 1;
		ExposureCompensation = new PropertyValue<long>
		{
			IsEnabled = false
		};
		Mode = new PropertyValue<long>
		{
			IsEnabled = false
		};
		FNumber = new PropertyValue<long>
		{
			IsEnabled = false
		};
		ShutterSpeed = new PropertyValue<long>
		{
			IsEnabled = false
		};
		WhiteBalance = new PropertyValue<long>
		{
			IsEnabled = false
		};
		FocusMode = new PropertyValue<long>
		{
			IsEnabled = false
		};
		IsoNumber = new PropertyValue<long>
		{
			IsEnabled = true
		};
		ExposureMeteringMode = new PropertyValue<long>
		{
			IsEnabled = false
		};
		Battery = 100;
		Capabilities.Add(CapabilityEnum.CaptureNoAf);
		Capabilities.Add(CapabilityEnum.LiveView);
		Capabilities.Add(CapabilityEnum.LiveViewStream);
		LiveViewImageZoomRatio = new PropertyValue<long>();
		LiveViewImageZoomRatio.AddValues("All", 0L);
		LiveViewImageZoomRatio.Value = "All";
	}

	public override string GetLiveViewStream()
	{
		return "rtsp://" + _address + "/1";
	}

	public override void StartLiveView()
	{
	}

	public override void StopLiveView()
	{
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		string text = "http://" + _address + "/snapshot.jpg";
		PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
		{
			WiaImageItem = null,
			EventArgs = new PortableDeviceEventArgs(),
			CameraDevice = this,
			FileName = text.Replace('/', '\\'),
			Handle = text
		};
		OnPhotoCapture(this, args);
	}

	public override void TransferFile(object o, string filename)
	{
		TransferProgress = 0u;
		HttpHelper.DownLoadFileByWebRequest((string)o, filename, this, 0L);
	}
}
