using System;
using System.IO;
using System.Threading.Tasks;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Others;

public class FakeCameraDevice : BaseCameraDevice
{
	private LiveViewData _liveViewData = new LiveViewData();

	public byte[] LatestRawData { get; set; }

	public string LatestFileName { get; set; }

	public byte[] LiveViewImageData { get; set; }

	public FakeCameraDevice()
	{
		DeviceName = "Z-App camera";
		SerialNumber = "No1";
		HaveLiveView = true;
		IsBusy = false;
		IsConnected = true;
		ExposureStatus = 1;
		Battery = 100;
		Capabilities.Add(CapabilityEnum.CaptureNoAf);
		Capabilities.Add(CapabilityEnum.LiveView);
		LiveViewImageZoomRatio = new PropertyValue<long>();
		LiveViewImageZoomRatio.AddValues("All", 0L);
		LiveViewImageZoomRatio.Value = "All";
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
		ExposureMeteringMode = new PropertyValue<long>
		{
			IsEnabled = false
		};
		CompressionSetting = new PropertyValue<long>
		{
			IsEnabled = true
		};
		CompressionSetting.AddValues("jpg", 0L);
		CompressionSetting.Value = "jpg";
		CompressionSetting.ReloadValues();
		IsoNumber = new PropertyValue<long>
		{
			IsEnabled = true
		};
		IsoNumber.AddValues("100", 100L);
		IsoNumber.AddValues("200", 200L);
		IsoNumber.AddValues("300", 300L);
		IsoNumber.Value = "100";
		IsoNumber.ReloadValues();
		PropertyValue<long> propertyValue = new PropertyValue<long>
		{
			Name = "Test Item"
		};
		propertyValue.AddValues("Val 1", 1L);
		propertyValue.AddValues("Val 2", 2L);
		propertyValue.AddValues("Val 3", 3L);
		propertyValue.Value = "Val 1";
		propertyValue.ReloadValues();
		AdvancedProperties.Add(propertyValue);
	}

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		return true;
	}

	public override void CapturePhoto()
	{
		if (LatestRawData != null)
		{
			string text = Path.GetExtension(LatestFileName ?? "").ToLower();
			PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
			{
				WiaImageItem = null,
				EventArgs = null,
				CameraDevice = this,
				FileName = ((text == ".mp4") ? "EventVideo.mp4" : "img0000.jpg"),
				Handle = LatestRawData
			};
			Task.Factory.StartNew(() =>
			{
				OnPhotoCapture(this, args);
			});
			LatestRawData = null;
			LatestFileName = null;
		}
	}

	public override void CapturePhotoNoAf()
	{
		CapturePhoto();
	}

	public override void TransferFile(object o, Stream stream)
	{
		byte[] array = File.ReadAllBytes((string)o);
		stream.Write(array, 0, array.Length);
	}

	public override void TransferFile(object o, string filename)
	{
		try
		{
			if (o is byte[] bytes)
			{
				File.WriteAllBytes(filename, bytes);
				return;
			}
			if (o is string text)
			{
				if (File.Exists(text))
				{
					File.Copy(text, filename, overwrite: true);
				}
				return;
			}
			throw new InvalidCastException("지원하지 않는 데이터 형식입니다: " + o.GetType().Name);
		}
		catch (Exception exception)
		{
			Log.Error("파일 전송(TransferFile) 중 오류 발생: ", exception);
		}
	}

	public override void StartLiveView()
	{
		_liveViewData.IsLiveViewRunning = true;
	}

	public override void StopLiveView()
	{
		_liveViewData.IsLiveViewRunning = false;
		LiveViewImageData = null;
	}

	public override string GetLiveViewStream()
	{
		return "rtsp://freja.hiof.no:1935/rtplive/definst/hessdalen03.stream";
	}

	public override LiveViewData GetLiveViewImage()
	{
		if (LiveViewImageData != null)
		{
			lock (_liveViewData)
			{
				_liveViewData.ImageData = LiveViewImageData;
				_liveViewData.IsLiveViewRunning = true;
				_liveViewData.ImageHeight = 480;
				_liveViewData.ImageWidth = 640;
			}
		}
		return _liveViewData;
	}

	public override void Focus(int x, int y)
	{
		_liveViewData.FocusX = x;
		_liveViewData.FocusY = y;
	}

	public override int Focus(int step)
	{
		return step;
	}

	public override bool DeleteObject(DeviceObject deviceObject)
	{
		return true;
	}
}
