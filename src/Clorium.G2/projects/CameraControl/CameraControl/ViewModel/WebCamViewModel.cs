using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.Devices.Others;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.ViewModel;

internal class WebCamViewModel : INotifyPropertyChanged
{
	private ICameraDevice _cameraDevice;

	private DispatcherTimer _liveViewTimer;

	private BitmapSource _webcambitmap;

	private bool _canRecord = true;

	private string _statusText = "대기 중";

	private int _preSeconds = 5;

	private int _postSeconds = 5;

	private bool _isManualFocusEnabled;

	private int _focusValue = 0;

	private object _selectedResolution;

	public RelayCommand RecordCommand => new RelayCommand(async () =>
	{
		await ExecuteRecord();
	}, () => CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled);

	public RelayCommand CaptureCommand => new RelayCommand(async () =>
	{
		await ExecuteCapture();
	});

	public bool CanRecord
	{
		get
		{
			return _canRecord && CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled;
		}
		set
		{
			_canRecord = value;
			OnPropertyChanged("CanRecord");
		}
	}

	public string StatusText
	{
		get
		{
			return _statusText;
		}
		set
		{
			_statusText = value;
			OnPropertyChanged("StatusText");
		}
	}

	public int PreSeconds
	{
		get
		{
			return _preSeconds;
		}
		set
		{
			_preSeconds = value;
			OnPropertyChanged("PreSeconds");
		}
	}

	public int PostSeconds
	{
		get
		{
			return _postSeconds;
		}
		set
		{
			_postSeconds = value;
			OnPropertyChanged("PostSeconds");
		}
	}

	public bool IsManualFocusEnabled
	{
		get
		{
			return _isManualFocusEnabled;
		}
		set
		{
			if (_isManualFocusEnabled != value)
			{
				_isManualFocusEnabled = value;
				OnPropertyChanged("IsManualFocusEnabled");
				SetFocusMode(value);
			}
		}
	}

	public int FocusValue
	{
		get
		{
			return _focusValue;
		}
		set
		{
			if (_focusValue != value)
			{
				_focusValue = value;
				OnPropertyChanged("FocusValue");
				SetFocusValue(value);
			}
		}
	}

	public BitmapSource WepCamBitmap
	{
		get
		{
			return _webcambitmap;
		}
		set
		{
			_webcambitmap = value;
			OnPropertyChanged("WepCamBitmap");
		}
	}

	public PropertyValue<long> CompressionSetting => ServiceProvider.DeviceManager.SelectedCameraDevice?.CompressionSetting;

	public object SelectedResolution
	{
		get
		{
			return _selectedResolution;
		}
		set
		{
			if (_selectedResolution != value)
			{
				_selectedResolution = value;
				OnPropertyChanged("SelectedResolution");
				SetResolution(value);
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	private void SetResolution(object val)
	{
		if (val == null || _cameraDevice == null)
		{
			return;
		}
		try
		{
			StopLiveView();
			CompressionSetting.Value = val.ToString();
			Task.Delay(100).ContinueWith((Task _) =>
			{
				StartLiveView();
			});
		}
		catch (Exception ex)
		{
			Log.Error("해상도 변경 실패: " + ex.Message);
		}
	}

	private void SetFocusMode(bool isManual)
	{
		if (_cameraDevice == null)
		{
			return;
		}
		try
		{
			if (isManual)
			{
				_cameraDevice.Focus(FocusValue, 0);
			}
			else
			{
				_cameraDevice.AutoFocus();
			}
			StatusText = (isManual ? "수동 포커스 모드" : "자동 포커스 모드");
		}
		catch (Exception ex)
		{
			Log.Error("포커스 모드 변경 실패: " + ex.Message);
		}
	}

	private void SetFocusValue(int value)
	{
		if (_cameraDevice == null || !IsManualFocusEnabled)
		{
			return;
		}
		try
		{
			_cameraDevice.Focus(value, 0);
		}
		catch (Exception ex)
		{
			Log.Error("포커스 값 변경 실패: " + ex.Message);
		}
	}

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public WebCamViewModel()
	{
		_liveViewTimer = new DispatcherTimer();
		_liveViewTimer.Interval = TimeSpan.FromMilliseconds(33.0);
		_liveViewTimer.Tick += LiveViewTimer_Tick;
	}

	public void InitWebcam()
	{
		ServiceProvider.DeviceManager.AddWebcameras(ServiceProvider.Settings.WebCam_DeviceName);
		_cameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
		if (_cameraDevice != null && CompressionSetting != null)
		{
			SelectedResolution = CompressionSetting.Value;
			OnPropertyChanged("CompressionSetting");
		}
		PreSeconds = 5;
		PostSeconds = 5;
		StartLiveView();
	}

	public void UnInitWebcam()
	{
		foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.IsConnected))
		{
			if (connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName)
			{
				StopLiveView();
				ServiceProvider.DeviceManager.DisconnectCamera(connectedDevice);
				break;
			}
		}
	}

	private void StartLiveView()
	{
		_liveViewTimer.Start();
	}

	private void StopLiveView()
	{
		_liveViewTimer.Stop();
	}

	private void LiveViewTimer_Tick(object sender, EventArgs e)
	{
		UpdateLiveView();
	}

	private void UpdateLiveView()
	{
		LiveViewData liveData = _cameraDevice.GetLiveViewImage();
		if (liveData?.ImageData == null)
		{
			return;
		}
		try
		{
			using MemoryStream ms = new MemoryStream(liveData.ImageData);
			BitmapImage bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.StreamSource = ms;
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();
			bitmap.Freeze();
			WepCamBitmap = bitmap;
		}
		catch
		{
		}
	}

	private async Task ExecuteRecord()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		try
		{
			CanRecord = false;
			StatusText = "녹화 처리 중 ...";
			WebCameraDevice.StaticPreSeconds = PreSeconds;
			WebCameraDevice.StaticPostSeconds = PostSeconds;
			_cameraDevice.StartRecordMovie();
			if (WebCameraDevice.RecordTaskSignal != null)
			{
				await WebCameraDevice.RecordTaskSignal.Task;
			}
			StatusText = "녹화 완료";
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			StatusText = "에러: " + ex2.Message;
		}
		finally
		{
			CanRecord = true;
		}
	}

	private async Task ExecuteCapture()
	{
		await CameraHelper.CaptureAsync();
	}
}
