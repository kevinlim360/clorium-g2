using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Video.FFMPEG;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Others;

public class WebCameraDevice : BaseCameraDevice
{
	private ConcurrentQueue<Bitmap> _frameBuffer = new ConcurrentQueue<Bitmap>();

	private VideoFileWriter _videoWriter;

	public static TaskCompletionSource<bool> RecordTaskSignal;

	private readonly object _recordingLock = new object();

	private volatile bool _isRecording = false;

	public static int StaticPreSeconds = 5;

	public static int StaticPostSeconds = 5;

	private VideoCaptureDevice _captureDevice;

	private LiveViewData _liveViewData = new LiveViewData();

	private object _lock = new object();

	private volatile bool _operationInProgress;

	private volatile bool _processLiveView = false;

	public WebCameraDevice()
	{
		Capabilities.Add(CapabilityEnum.LiveView);
	}

	public void Init(string id)
	{
		_captureDevice = new VideoCaptureDevice(id);
		_captureDevice.NewFrame += _localWebCam_NewFrame;
		_captureDevice.SnapshotFrame += _localWebCam_SnapshotFrame;
		_captureDevice.ProvideSnapshots = true;
		IsConnected = true;
		PortName = id;
		if (_captureDevice.VideoCapabilities.Length != 0)
		{
			_captureDevice.VideoResolution = _captureDevice.VideoCapabilities[0];
		}
		LiveViewImageZoomRatio = new PropertyValue<long>();
		IsoNumber = new PropertyValue<long>
		{
			Available = false
		};
		FNumber = new PropertyValue<long>
		{
			Available = false
		};
		ExposureCompensation = new PropertyValue<long>
		{
			Available = false
		};
		FocusMode = new PropertyValue<long>
		{
			Available = false
		};
		ShutterSpeed = new PropertyValue<long>
		{
			Available = false
		};
		WhiteBalance = new PropertyValue<long>
		{
			Available = false
		};
		Mode = new PropertyValue<long>
		{
			Available = false
		};
		ExposureMeteringMode = new PropertyValue<long>
		{
			Available = false
		};
		CompressionSetting = new PropertyValue<long>();
		VideoCapabilities[] videoCapabilities = _captureDevice.VideoCapabilities;
		for (int i = 0; i < videoCapabilities.Length; i++)
		{
			CompressionSetting.AddValues(videoCapabilities[i].ToString(), i);
		}
		CompressionSetting.ReloadValues();
		StartLiveView();
		if (_captureDevice.VideoResolution != null)
		{
			CompressionSetting.Value = _captureDevice.VideoResolution.ToString();
		}
		else if (_captureDevice.VideoCapabilities.Length != 0)
		{
			_captureDevice.VideoResolution = _captureDevice.VideoCapabilities.First();
			CompressionSetting.Value = _captureDevice.VideoResolution.ToString();
		}
		CompressionSetting.ValueChanged += CompressionSetting_ValueChanged;
	}

	private void CompressionSetting_ValueChanged(object sender, string key, long val)
	{
		StopLiveView();
		VideoCapabilities[] videoCapabilities = _captureDevice.VideoCapabilities;
		_captureDevice.VideoResolution = videoCapabilities[val];
		StartLiveView();
	}

	private void _localWebCam_NewFrame(object sender, NewFrameEventArgs eventargs)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected Obj, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected Obj, but got Unknown
		if (_operationInProgress || !_processLiveView)
		{
			return;
		}
		try
		{
			_operationInProgress = true;
			Bitmap val = (Bitmap)((Image)eventargs.Frame).Clone();
			try
			{
				if (_isRecording)
				{
					lock (_recordingLock)
					{
						if (_videoWriter != null && _videoWriter.IsOpen)
						{
							_videoWriter.WriteVideoFrame(val);
						}
					}
				}
				else
				{
					_frameBuffer.Enqueue((Bitmap)((Image)val).Clone());
					if (_frameBuffer.Count > 30 * StaticPreSeconds && _frameBuffer.TryDequeue(out var result))
					{
						((Image)result).Dispose();
					}
				}
				using MemoryStream memoryStream = new MemoryStream();
				((Image)val).Save((Stream)memoryStream, ImageFormat.Jpeg);
				memoryStream.Seek(0L, SeekOrigin.Begin);
				lock (_lock)
				{
					_liveViewData.ImageData = memoryStream.ToArray();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch (Exception exception)
		{
			Log.Error("웹캠 동영상 프레임 에러 : ", exception);
		}
		finally
		{
			_operationInProgress = false;
		}
	}

	private void _localWebCam_SnapshotFrame(object sender, NewFrameEventArgs eventargs)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected Obj, but got Unknown
		try
		{
			Image val = (Image)(Bitmap)((Image)eventargs.Frame).Clone();
			using MemoryStream memoryStream = new MemoryStream();
			val.Save((Stream)memoryStream, ImageFormat.Jpeg);
			memoryStream.Seek(0L, SeekOrigin.Begin);
			PhotoCaptureEvent(memoryStream.ToArray());
		}
		catch (Exception exception)
		{
			Log.Error("웹캠 이미지 캡쳐 에러 : ", exception);
		}
	}

	public override void StartLiveView()
	{
		lock (_lock)
		{
			if (!_captureDevice.IsRunning)
			{
				_captureDevice.Start();
				Thread.Sleep(500);
			}
			_operationInProgress = false;
			_processLiveView = true;
		}
	}

	public override void StopLiveView()
	{
		if (_captureDevice == null)
		{
			return;
		}
		lock (_lock)
		{
			_captureDevice.SignalToStop();
			_captureDevice.WaitForStop();
			_operationInProgress = false;
			_processLiveView = false;
		}
	}

	public override LiveViewData GetLiveViewImage()
	{
		_operationInProgress = false;
		return _liveViewData;
	}

	public override void TransferFile(object o, string filename)
	{
		byte[] bytes = (byte[])o;
		File.WriteAllBytes(filename, bytes);
	}

	public override void Close()
	{
		if (_isRecording)
		{
			StopRecordMovie();
		}
		Bitmap result;
		while (_frameBuffer.TryDequeue(out result))
		{
			((Image)result).Dispose();
		}
		if (_captureDevice != null)
		{
			_captureDevice.SignalToStop();
			_captureDevice.SnapshotFrame -= _localWebCam_SnapshotFrame;
			_captureDevice.NewFrame -= _localWebCam_NewFrame;
			_captureDevice.WaitForStop();
			_captureDevice = null;
		}
	}

	public override void CapturePhotoNoAf()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		CapturePhoto();
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		StartLiveView();
		_processLiveView = true;
		GetLiveViewImage();
		if (_liveViewData.ImageData != null)
		{
			PhotoCaptureEvent(_liveViewData.ImageData);
			_liveViewData.ImageData = null;
			return;
		}
		throw new Exception("웹캠 촬영할 수 없습니다.");
	}

	private void PhotoCaptureEvent(byte[] data)
	{
		try
		{
			PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
			{
				WiaImageItem = null,
				EventArgs = null,
				CameraDevice = this,
				FileName = "img0000.jpg",
				Handle = data
			};
			OnPhotoCapture(this, args);
		}
		catch (Exception exception)
		{
			Log.Error("웹캠 이미지 캡쳐 이벤트 에러 : ", exception);
		}
	}

	public override void StartRecordMovie()
	{
		global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		StartRecordMovieLicensed();
	}

	private async void StartRecordMovieLicensed()
	{
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		if (_isRecording)
		{
			return;
		}
		RecordTaskSignal = new TaskCompletionSource<bool>();
		string tempPath = Path.Combine(Path.GetTempPath(), $"Event_{DateTime.Now:HHmmss}.mp4");
		try
		{
			lock (_recordingLock)
			{
				_videoWriter = new VideoFileWriter();
				VideoCapabilities res = _captureDevice.VideoResolution;
				VideoFileWriter videoWriter = _videoWriter;
				Size frameSize = res.FrameSize;
				int width = frameSize.Width;
				frameSize = res.FrameSize;
				videoWriter.Open(tempPath, width, frameSize.Height, 25, VideoCodec.MPEG4, 2000000);
				Bitmap old;
				while (_frameBuffer.TryDequeue(out old))
				{
					Bitmap val = old;
					try
					{
						_videoWriter.WriteVideoFrame(old);
					}
					finally
					{
						((IDisposable)val)?.Dispose();
					}
				}
				_isRecording = true;
			}
			await Task.Delay(StaticPostSeconds * 1000);
			lock (_recordingLock)
			{
				_isRecording = false;
				if (_videoWriter != null)
				{
					_videoWriter.Close();
					_videoWriter.Dispose();
					_videoWriter = null;
				}
			}
			VideoCaptureEvent(tempPath);
			RecordTaskSignal.SetResult(result: true);
		}
		catch (Exception exception)
		{
			_isRecording = false;
			RecordTaskSignal.SetException(exception);
			Log.Error("녹화 중 에러", exception);
			lock (_recordingLock)
			{
				if (_videoWriter != null)
				{
					_videoWriter.Dispose();
					_videoWriter = null;
				}
			}
		}
	}

	public override void StopRecordMovie()
	{
		lock (_recordingLock)
		{
			if (!_isRecording)
			{
				return;
			}
			_isRecording = false;
			try
			{
				if (_videoWriter != null)
				{
					_videoWriter.Close();
					_videoWriter.Dispose();
					_videoWriter = null;
				}
			}
			catch (Exception exception)
			{
				Log.Error("녹화 중지 에러: ", exception);
			}
		}
	}

	private void VideoCaptureEvent(string filePath)
	{
		try
		{
			byte[] handle = File.ReadAllBytes(filePath);
			PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
			{
				WiaImageItem = null,
				EventArgs = null,
				CameraDevice = this,
				FileName = "EventVideo.mp4",
				Handle = handle
			};
			OnPhotoCapture(this, args);
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
		}
		catch (Exception exception)
		{
			Log.Error("녹화 이벤트 에러 : ", exception);
		}
	}

	public override void AutoFocus()
	{
		_captureDevice.SetCameraProperty(CameraControlProperty.Focus, 0, CameraControlFlags.Auto);
	}

	public override void Focus(int x, int y)
	{
		_captureDevice.SetCameraProperty(CameraControlProperty.Focus, x, CameraControlFlags.Manual);
	}
}
