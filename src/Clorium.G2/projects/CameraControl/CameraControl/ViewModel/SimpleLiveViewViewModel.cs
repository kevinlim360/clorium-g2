using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.ViewModel;

public class SimpleLiveViewViewModel : ViewModelBase
{
	private ICameraDevice _cameraDevice;

	private BitmapSource _bitmap;

	private bool _isMovieRecording;

	private CameraProperty _cameraProperty;

	private int _rotation = 0;

	public ICameraDevice CameraDevice
	{
		get
		{
			return _cameraDevice;
		}
		set
		{
			_cameraDevice = value;
			RaisePropertyChanged(() => CameraDevice);
		}
	}

	public BitmapSource Bitmap
	{
		get
		{
			return _bitmap;
		}
		set
		{
			_bitmap = value;
			RaisePropertyChanged(() => Bitmap);
		}
	}

	public bool IsMovieRecording
	{
		get
		{
			return _isMovieRecording;
		}
		set
		{
			_isMovieRecording = value;
			RaisePropertyChanged(() => IsMovieRecording);
		}
	}

	public CameraProperty CameraProperty
	{
		get
		{
			return _cameraProperty;
		}
		set
		{
			_cameraProperty = value;
			RaisePropertyChanged(() => CameraProperty);
		}
	}

	public int Rotation
	{
		get
		{
			return _rotation;
		}
		set
		{
			_rotation = value;
			RaisePropertyChanged(() => Rotation);
		}
	}

	public RelayCommand FocusCommand { get; set; }

	public SimpleLiveViewViewModel()
	{
	}

	public SimpleLiveViewViewModel(ICameraDevice device)
	{
		CameraDevice = device;
		FocusCommand = new RelayCommand(Focus);
		CameraProperty = device.LoadProperties();
		switch (CameraProperty.LiveviewSettings.RotationIndex)
		{
		case 0:
			Rotation = 0;
			break;
		case 1:
			Rotation = 90;
			break;
		case 2:
			Rotation = 180;
			break;
		case 3:
			Rotation = 270;
			break;
		}
	}

	public void Start()
	{
		bool retry = false;
		int retryNum = 0;
		do
		{
			try
			{
				LiveViewManager.StartLiveView(CameraDevice);
			}
			catch (DeviceException ex)
			{
				if (ex.ErrorCode == 2147942570u || ex.ErrorCode == 8217)
				{
					Thread.Sleep(100);
					retry = true;
					retryNum++;
					continue;
				}
				throw;
			}
		}
		while (retry && retryNum < 35);
	}

	public void Stop()
	{
		bool retry = false;
		int retryNum = 0;
		do
		{
			try
			{
				LiveViewManager.StopLiveView(CameraDevice);
			}
			catch (DeviceException ex)
			{
				if (ex.ErrorCode == 2147942570u || ex.ErrorCode == 8217)
				{
					Thread.Sleep(500);
					retry = true;
					retryNum++;
					continue;
				}
				throw;
			}
		}
		while (retry && retryNum < 35);
	}

	public void Get()
	{
		try
		{
			LiveViewData LiveViewData = LiveViewManager.GetLiveViewImage(CameraDevice);
			if (LiveViewData != null && LiveViewData.ImageData != null)
			{
				MemoryStream stream = new MemoryStream(LiveViewData.ImageData, LiveViewData.ImageDataPosition, LiveViewData.ImageData.Length - LiveViewData.ImageDataPosition);
				using Bitmap res = new Bitmap(stream);
				WriteableBitmap writeableBitmap = BitmapFactory.ConvertToPbgra32Format(BitmapSourceConvert.ToBitmapSource(res));
				writeableBitmap.Freeze();
				Bitmap = writeableBitmap;
				IsMovieRecording = LiveViewData.MovieIsRecording;
				return;
			}
		}
		catch (Exception)
		{
		}
	}

	public void Focus()
	{
		Thread thread = new Thread(AutoFocusThread);
		thread.Start();
	}

	private void AutoFocusThread()
	{
		try
		{
			CameraDevice.AutoFocus();
		}
		catch (Exception ex)
		{
			Log.Error("Unable to autofocus", ex);
			StaticHelper.Instance.SystemMessage = ex.Message;
		}
	}

	public void RecordMovie()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		string resp = CameraDevice.GetProhibitionCondition(OperationEnum.RecordMovie);
		if (string.IsNullOrEmpty(resp))
		{
			Thread thread = new Thread(RecordMovieThread);
			thread.Start();
		}
	}

	private void RecordMovieThread()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		try
		{
			CameraDevice.StartRecordMovie();
		}
		catch (Exception ex)
		{
			StaticHelper.Instance.SystemMessage = ex.Message;
			Log.Error("Recording error", ex);
		}
	}

	public void StopRecordMovie()
	{
		Thread thread = new Thread(StopRecordMovieThread);
		thread.Start();
	}

	private void StopRecordMovieThread()
	{
		try
		{
			CameraDevice.StopRecordMovie();
		}
		catch (Exception ex)
		{
			StaticHelper.Instance.SystemMessage = ex.Message;
			Log.Error("Recording error", ex);
		}
	}
}
