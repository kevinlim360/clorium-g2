using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CameraControl.Core;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.ViewModel;

public class MultipleLiveViewViewModel : ViewModelBase
{
	private System.Timers.Timer _timer = new System.Timers.Timer(66.0);

	private bool _operInProgress;

	private int _rows;

	private int _cols;

	private ObservableCollection<SimpleLiveViewViewModel> _cameras;

	public ObservableCollection<SimpleLiveViewViewModel> Cameras
	{
		get
		{
			return _cameras;
		}
		set
		{
			_cameras = value;
			RaisePropertyChanged(() => Cameras);
		}
	}

	public RelayCommand StartLiveViewCommand { get; set; }

	public RelayCommand StopLiveViewCommand { get; set; }

	public RelayCommand AutoFocusCommand { get; set; }

	public RelayCommand StartRecordCommand { get; set; }

	public RelayCommand StopRecordCommand { get; set; }

	public bool OperInProgress
	{
		get
		{
			return _operInProgress;
		}
		set
		{
			_operInProgress = value;
			RaisePropertyChanged(() => OperInProgress);
		}
	}

	public int Rows
	{
		get
		{
			return _rows;
		}
		set
		{
			_rows = value;
			RaisePropertyChanged(() => Rows);
		}
	}

	public int Cols
	{
		get
		{
			return _cols;
		}
		set
		{
			_cols = value;
			RaisePropertyChanged(() => Cols);
		}
	}

	public MultipleLiveViewViewModel()
	{
		Rows = 2;
		Cols = 2;
		StartLiveViewCommand = new RelayCommand(StartLiveView);
		StopLiveViewCommand = new RelayCommand(StopLiveView);
		AutoFocusCommand = new RelayCommand(AutoFocus);
		StartRecordCommand = new RelayCommand(StartRecord, () => CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled);
		StopRecordCommand = new RelayCommand(StopRecord);
		_timer.Elapsed += _timer_Elapsed;
		InitCameras();
	}

	public void InitCameras()
	{
		Cameras = new ObservableCollection<SimpleLiveViewViewModel>();
		if (ServiceProvider.DeviceManager == null)
		{
			return;
		}
		ServiceProvider.WindowsManager.Event += WindowsManager_Event;
		foreach (ICameraDevice device in ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.GetCapability(CapabilityEnum.LiveView)))
		{
			if (!(device.DeviceName == "Z-App camera"))
			{
				if (device.DeviceName == "Z-FTP camera")
				{
					break;
				}
				if (!(device.DeviceName == ServiceProvider.Settings.WebCam_DeviceName))
				{
					Cameras.Add(new SimpleLiveViewViewModel(device));
				}
			}
		}
	}

	private void WindowsManager_Event(string cmd, object o)
	{
		if (!(cmd == "MultipleLiveView_Start"))
		{
			if (cmd == "MultipleLiveView_Stop")
			{
				StopLiveView();
			}
		}
		else
		{
			StartLiveView();
		}
		if (cmd.StartsWith("MultipleLiveView_Row") && cmd.Contains("_"))
		{
			string[] vals = cmd.Split('_');
			if (vals.Count() > 2 && int.TryParse(vals[2], out var x))
			{
				Rows = x;
			}
		}
		if (cmd.StartsWith("MultipleLiveView_Col") && cmd.Contains("_"))
		{
			string[] vals2 = cmd.Split('_');
			if (vals2.Count() > 2 && int.TryParse(vals2[2], out var x2))
			{
				Cols = x2;
			}
		}
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		_timer.Stop();
		Task.Factory.StartNew(GetLiveViewThread);
	}

	private void AutoFocus()
	{
		foreach (SimpleLiveViewViewModel camera in Cameras)
		{
			camera.Focus();
		}
	}

	private void GetLiveViewThread()
	{
		foreach (SimpleLiveViewViewModel camera in Cameras)
		{
			camera.Get();
		}
		_timer.Start();
	}

	public void StartLiveView()
	{
		OperInProgress = true;
		Thread thread = new Thread(StartLiveViewThread);
		thread.Start();
	}

	private void StartLiveViewThread()
	{
		foreach (SimpleLiveViewViewModel camera in Cameras)
		{
			camera.Start();
		}
		OperInProgress = false;
		_timer.Start();
	}

	private void StopLiveView()
	{
		OperInProgress = true;
		Thread thread = new Thread(StopLiveViewThread);
		thread.Start();
	}

	private void StopLiveViewThread()
	{
		_timer.Stop();
		foreach (SimpleLiveViewViewModel camera in Cameras)
		{
			camera.Stop();
		}
		OperInProgress = false;
	}

	private void StartRecord()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		OperInProgress = true;
		Thread thread = new Thread(StartRecordThread);
		thread.Start();
	}

	private void StartRecordThread()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		foreach (SimpleLiveViewViewModel camera in Cameras)
		{
			camera.RecordMovie();
		}
		OperInProgress = false;
		_timer.Start();
	}

	private void StopRecord()
	{
		OperInProgress = true;
		Thread thread = new Thread(StopRecordThread);
		thread.Start();
	}

	private void StopRecordThread()
	{
		_timer.Stop();
		foreach (SimpleLiveViewViewModel camera in Cameras)
		{
			camera.StopRecordMovie();
		}
		OperInProgress = false;
	}
}
