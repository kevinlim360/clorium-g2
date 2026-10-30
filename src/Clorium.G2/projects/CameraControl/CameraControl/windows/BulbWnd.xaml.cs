using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Scripting;
using CameraControl.Core.Translation;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.Devices.Nikon;
using Microsoft.Win32;

namespace CameraControl.windows;

public partial class BulbWnd : Window, INotifyPropertyChanged, IWindow, IComponentConnector
{
	private System.Timers.Timer _captureTimer = new System.Timers.Timer(1000.0);

	private System.Timers.Timer _waitTimer = new System.Timers.Timer(1000.0);

	private int _captureSecs;

	private int _waitSecs;

	private int _photoCount = 0;

	private string _defaultScriptFile = "";

	private bool _noAutofocus;

	private int _captureTime;

	private int _expDelay;

	private int _waitDelay;

	private int _numOfPhotos;

	private int _waitTime;

	private string _message;

	private ScriptObject _defaultScript;

	private int _phdType;

	private int _phdWait;

	private int _countDown;

	private string _event;

	private int _photoLeft;

	private bool _automaticGuiding;

	public ICameraDevice CameraDevice { get; set; }

	public bool NoAutofocus
	{
		get
		{
			return _noAutofocus;
		}
		set
		{
			_noAutofocus = value;
			NotifyPropertyChanged("NoAutofocus");
		}
	}

	public int CaptureTime
	{
		get
		{
			return _captureTime;
		}
		set
		{
			_captureTime = value;
			NotifyPropertyChanged("CaptureTime");
		}
	}

	public int expDelay
	{
		get
		{
			return _expDelay;
		}
		set
		{
			_expDelay = value;
		}
	}

	public int waitDelay
	{
		get
		{
			return _waitDelay;
		}
		set
		{
			_waitDelay = value;
		}
	}

	public int NumOfPhotos
	{
		get
		{
			return _numOfPhotos;
		}
		set
		{
			_numOfPhotos = value;
			NotifyPropertyChanged("NumOfPhotos");
		}
	}

	public int WaitTime
	{
		get
		{
			return _waitTime;
		}
		set
		{
			_waitTime = value;
			NotifyPropertyChanged("WaitTime");
		}
	}

	public string Message
	{
		get
		{
			return _message;
		}
		set
		{
			_message = value;
			NotifyPropertyChanged("Message");
		}
	}

	public ScriptObject DefaultScript
	{
		get
		{
			return _defaultScript;
		}
		set
		{
			_defaultScript = value;
			NotifyPropertyChanged("DefaultScript");
		}
	}

	public int PhdType
	{
		get
		{
			return _phdType;
		}
		set
		{
			_phdType = value;
			NotifyPropertyChanged("PhdType");
		}
	}

	public int PhdWait
	{
		get
		{
			return _phdWait;
		}
		set
		{
			_phdWait = value;
			NotifyPropertyChanged("PhdWait");
		}
	}

	public int CountDown
	{
		get
		{
			return _countDown;
		}
		set
		{
			_countDown = value;
			NotifyPropertyChanged("CountDown");
		}
	}

	public string Event
	{
		get
		{
			return _event;
		}
		set
		{
			_event = value;
			NotifyPropertyChanged("Event");
		}
	}

	public int PhotoLeft
	{
		get
		{
			return _photoLeft;
		}
		set
		{
			_photoLeft = value;
			NotifyPropertyChanged("PhotoLeft");
		}
	}

	public bool AutomaticGuiding
	{
		get
		{
			return _automaticGuiding;
		}
		set
		{
			_automaticGuiding = value;
			if (_automaticGuiding && PhdType == 0)
			{
				PhdType = 1;
			}
			NotifyPropertyChanged("AutomaticGuiding");
		}
	}

	public RelayCommand<IScriptCommand> AddCommand { get; private set; }

	public RelayCommand<IScriptCommand> EditCommand { get; private set; }

	public RelayCommand<IScriptCommand> DelCommand { get; private set; }

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public BulbWnd()
	{
		CameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
		AddCommand = new RelayCommand<IScriptCommand>(AddCommandMethod);
		EditCommand = new RelayCommand<IScriptCommand>(EditCommandMethod);
		DelCommand = new RelayCommand<IScriptCommand>(DelCommandMethod);
		InitializeComponent();
		CaptureTime = 60;
		NumOfPhotos = 1;
		WaitTime = 0;
		PhdType = 0;
		PhdWait = 5;
		CountDown = 0;
		PhotoLeft = 0;
		AutomaticGuiding = false;
		_captureTimer.Elapsed += _captureTimer_Elapsed;
		_waitTimer.Elapsed += _waitTimer_Elapsed;
	}

	private void Init()
	{
		DefaultScript = new ScriptObject();
		_defaultScriptFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Settings.AppName, "default.dccscript");
		btn_astrolv.IsEnabled = CameraDevice.GetCapability(CapabilityEnum.LiveView);
		try
		{
			if (File.Exists(_defaultScriptFile))
			{
				DefaultScript = ServiceProvider.ScriptManager.Load(_defaultScriptFile);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Error loading default scrip", exception);
		}
	}

	public void AddCommandMethod(IScriptCommand command)
	{
		IScriptCommand scriptCommand = command.Create();
		EditCommandMethod(scriptCommand);
		DefaultScript.Commands.Add(scriptCommand);
		lst_commands.SelectedItem = scriptCommand;
	}

	public void EditCommandMethod(IScriptCommand command)
	{
		if (command != null)
		{
			if (!command.HaveEditControl)
			{
				MessageBox.Show("Use script editor to edit this script");
				return;
			}
			ScriptCommandEdit dlg = new ScriptCommandEdit(command.GetConfig());
			dlg.ShowDialog();
		}
	}

	public void DelCommandMethod(IScriptCommand command)
	{
		if (command != null)
		{
			int ind = DefaultScript.Commands.IndexOf(command);
			DefaultScript.Commands.Remove(command);
			if (ind >= DefaultScript.Commands.Count)
			{
				ind = DefaultScript.Commands.Count - 1;
			}
			if (ind > 0)
			{
				lst_commands.SelectedItem = DefaultScript.Commands[ind];
			}
		}
	}

	private void _waitTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		_waitSecs++;
		Message = $"Waiting for next capture {_waitSecs} sec. Photo done {_photoCount}/{NumOfPhotos}";
		if (CountDown > 0)
		{
			CountDown--;
		}
		else if (_waitSecs >= WaitTime)
		{
			if (CameraDevice.IsBusy)
			{
				Message = "Device is busy !Waiting for device to be ready !";
				return;
			}
			_waitTimer.Stop();
			StartCapture();
		}
	}

	private void _captureTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		_captureSecs++;
		if (_captureSecs >= CaptureTime + expDelay)
		{
			_captureTimer.Stop();
			StopCapture();
			_photoCount++;
			_waitSecs = 0;
			if (_photoCount < NumOfPhotos)
			{
				if (PhdType > 0)
				{
					PhdGuiding(PhdType);
				}
				Event = "Waiting";
				CountDown = WaitTime + waitDelay;
				_waitTimer.Start();
			}
		}
		CountDown--;
	}

	private void btn_start_Click(object sender, RoutedEventArgs e)
	{
		_photoCount = 0;
		AutomaticGuiding = false;
		PhotoLeft = NumOfPhotos;
		StartCapture();
	}

	private void StartCapture()
	{
		Thread thread = new Thread(StartCaptureThread);
		thread.Start();
	}

	private void StartCaptureThread()
	{
		Message = "";
		bool retry;
		do
		{
			retry = false;
			try
			{
				Log.Debug("Bulb capture started");
				CountDown = CaptureTime;
				Event = "Capture";
				PhotoLeft--;
				if (DefaultScript.UseExternal)
				{
					if (DefaultScript.SelectedConfig != null)
					{
						if (CameraDevice is NikonBase nikonBase)
						{
							nikonBase.StopEventTimer();
						}
						ServiceProvider.ExternalDeviceManager.OpenShutter(DefaultScript.SelectedConfig);
					}
					else
					{
						MessageBox.Show(TranslationStrings.LabelNoExternalDeviceSelected);
					}
				}
				else if (CameraDevice.GetCapability(CapabilityEnum.Bulb))
				{
					expDelay = CameraDevice.GetExposureDelay();
					CountDown += expDelay;
					waitDelay = ((CameraDevice.GetPropertyValue(53355u) == "ON") ? CaptureTime : 0);
					ServiceProvider.DeviceManager.LastCapturedImage[CameraDevice] = "-";
					CameraDevice.IsBusy = true;
					CameraDevice.LockCamera();
					CameraDevice.StartBulbMode();
				}
				else
				{
					StaticHelper.Instance.SystemMessage = TranslationStrings.MsgBulbModeNotSupported;
				}
			}
			catch (DeviceException ex)
			{
				if (ex.ErrorCode == 2147942570u)
				{
					retry = true;
					continue;
				}
				StaticHelper.Instance.SystemMessage = ex.Message;
				Log.Error("Bulb start", ex);
				CameraDevice.IsBusy = false;
			}
			catch (Exception ex2)
			{
				StaticHelper.Instance.SystemMessage = ex2.Message;
				Log.Error("Bulb start", ex2);
				Event = "Error";
				CountDown = 0;
				CameraDevice.IsBusy = false;
				return;
			}
		}
		while (retry);
		_waitSecs = 0;
		_captureSecs = 0;
		_captureTimer.Start();
	}

	public virtual void NotifyPropertyChanged(string info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}

	private void btn_stop_Click(object sender, RoutedEventArgs e)
	{
		StopCapture();
		CountDown = 0;
	}

	private void StopCapture()
	{
		_captureTimer.Stop();
		_waitTimer.Stop();
		Thread thread = new Thread(StopCaptureThread);
		thread.Start();
		StaticHelper.Instance.SystemMessage = "Capture stopped";
		Log.Debug("Bulb capture stopped");
		Event = "Done";
	}

	private void StopCaptureThread()
	{
		bool retry;
		do
		{
			retry = false;
			try
			{
				if (DefaultScript.UseExternal)
				{
					if (DefaultScript.SelectedConfig != null)
					{
						ServiceProvider.ExternalDeviceManager.CloseShutter(DefaultScript.SelectedConfig);
						if (CameraDevice is NikonBase nikonBase)
						{
							nikonBase.StartEventTimer();
						}
					}
					else
					{
						MessageBox.Show(TranslationStrings.LabelNoExternalDeviceSelected);
					}
				}
				else if (CameraDevice.GetCapability(CapabilityEnum.Bulb))
				{
					CameraDevice.EndBulbMode();
				}
				else
				{
					StaticHelper.Instance.SystemMessage = TranslationStrings.MsgBulbModeNotSupported;
				}
				StaticHelper.Instance.SystemMessage = "Capture done";
				Log.Debug("Bulb capture done");
			}
			catch (DeviceException ex)
			{
				if (ex.ErrorCode == 2147942570u)
				{
					retry = true;
					continue;
				}
				StaticHelper.Instance.SystemMessage = ex.Message;
				Log.Error("Bulb done", ex);
			}
			catch (Exception ex2)
			{
				StaticHelper.Instance.SystemMessage = ex2.Message;
				Log.Error("Bulb done", ex2);
			}
		}
		while (retry);
	}

	private void Window_Closed(object sender, EventArgs e)
	{
	}

	private void btn_help_Click(object sender, RoutedEventArgs e)
	{
		HelpProvider.Run(HelpSections.Bulb);
	}

	private void MenuItem_Click(object sender, RoutedEventArgs e)
	{
		SaveFileDialog dlg = new SaveFileDialog();
		dlg.Filter = "Script file(*.dccscript)|*.dccscript|All files|*.*";
		if (dlg.ShowDialog() == true)
		{
			try
			{
				ServiceProvider.ScriptManager.Save(DefaultScript, dlg.FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error saving script file" + ex.Message);
				Log.Error("Error saving script file", ex);
			}
		}
	}

	private void MenuItem_Click_1(object sender, RoutedEventArgs e)
	{
		OpenFileDialog dlg = new OpenFileDialog();
		dlg.Filter = "Script file(*.dccscript)|*.dccscript|All files|*.*";
		if (dlg.ShowDialog() == true)
		{
			try
			{
				DefaultScript = ServiceProvider.ScriptManager.Load(dlg.FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading script file" + ex.Message);
				Log.Error("Error loading script file", ex);
			}
		}
	}

	private void button1_Click(object sender, RoutedEventArgs e)
	{
		DefaultScript.CameraDevice = CameraDevice;
		ServiceProvider.ScriptManager.Execute(DefaultScript);
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "BulbWnd_Show":
			CameraDevice = param as ICameraDevice;
			if (CameraDevice == null)
			{
				CameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
			}
			if (CameraDevice == null)
			{
				Log.Debug("No camera for Bulb window !");
				return;
			}
			Dispatcher.Invoke(() =>
			{
				try
				{
					Init();
					CameraDevice.PhotoCaptured += CameraDevice_PhotoCaptured;
					ServiceProvider.ScriptManager.OutPutMessageReceived += ScriptManager_OutPutMessageReceived;
					Show();
					Activate();
					Focus();
				}
				catch (Exception exception)
				{
					Log.Error("Bulb window initialization error ", exception);
				}
			});
			break;
		case "BulbWnd_Hide":
			if (_captureTimer.Enabled)
			{
				StopCaptureThread();
				CameraDevice.UnLockCamera();
			}
			_captureTimer.Stop();
			_waitTimer.Stop();
			ServiceProvider.ScriptManager.OutPutMessageReceived -= ScriptManager_OutPutMessageReceived;
			CameraDevice.PhotoCaptured -= CameraDevice_PhotoCaptured;
			ServiceProvider.ScriptManager.Save(DefaultScript, _defaultScriptFile);
			Dispatcher.Invoke(Hide);
			break;
		case "All_Close":
			ServiceProvider.ScriptManager.OutPutMessageReceived -= ScriptManager_OutPutMessageReceived;
			CameraDevice.PhotoCaptured -= CameraDevice_PhotoCaptured;
			Dispatcher.Invoke(() =>
			{
				Hide();
				Close();
			});
			break;
		case "BulbCaptureStart":
			if (IsVisible)
			{
				btn_start_Click(null, null);
			}
			break;
		case "BulbCaptureStop":
			if (IsVisible)
			{
				btn_stop_Click(null, null);
			}
			break;
		case "BulbScriptStart":
			if (IsVisible)
			{
				button1_Click(null, null);
			}
			break;
		case "BulbScriptStop":
			if (IsVisible)
			{
				btn_stop_script_Click(null, null);
			}
			break;
		}
		if (!cmd.StartsWith("Bulb_"))
		{
			return;
		}
		string[] vals = cmd.Split('_');
		if (vals.Count() > 2)
		{
			switch (vals[1])
			{
			case "CaptureTime":
				CaptureTime = GetValue(vals, CaptureTime);
				break;
			case "NumOfPhotos":
				NumOfPhotos = GetValue(vals, NumOfPhotos);
				break;
			case "WaitTime":
				WaitTime = GetValue(vals, WaitTime);
				break;
			case "PhdWait":
				PhdWait = GetValue(vals, PhdWait);
				break;
			case "AutomaticGuiding":
				AutomaticGuiding = GetValue(vals, 0) == 1;
				break;
			case "PhdType":
				PhdType = GetValue(vals, PhdType);
				break;
			}
		}
	}

	private int GetValue(string[] cmd, int defVal)
	{
		if (int.TryParse(cmd[2], out var x))
		{
			return x;
		}
		return defVal;
	}

	private void CameraDevice_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
	{
		if (AutomaticGuiding && PhdType > 0)
		{
			Thread thread = new Thread(() =>
			{
				PhdGuiding(PhdType);
			});
			thread.Start();
		}
	}

	private void ScriptManager_OutPutMessageReceived(object sender, MessageEventArgs e)
	{
		AddOutput(e.Message);
	}

	private void MetroWindow_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("BulbWnd_Hide");
		}
	}

	private void btn_stop_script_Click(object sender, RoutedEventArgs e)
	{
		ServiceProvider.ScriptManager.Stop();
	}

	private void btn_astrolv_Click(object sender, RoutedEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("AstroLiveViewWnd_Show", CameraDevice);
	}

	public void PhdGuiding(int movetype)
	{
		try
		{
			Event = "PhdGuiding";
			TcpClient socket = new TcpClient("localhost", 4300);
			Thread.Sleep(200);
			switch (movetype)
			{
			case 1:
				SendReceiveTest2(socket, 3);
				break;
			case 2:
				SendReceiveTest2(socket, 4);
				break;
			case 3:
				SendReceiveTest2(socket, 5);
				break;
			case 4:
				SendReceiveTest2(socket, 12);
				break;
			case 5:
				SendReceiveTest2(socket, 13);
				break;
			}
			socket.Close();
			CountDown = PhdWait;
			for (int i = 1; i < PhdWait + 1; i++)
			{
				CountDown--;
				Thread.Sleep(1000);
			}
			Event = "";
		}
		catch (Exception ex)
		{
			StaticHelper.Instance.SystemMessage = "PHDGuiding error " + ex.Message;
			Log.Error("PHDGuiding error", ex);
		}
	}

	public static int SendReceiveTest2(TcpClient server, byte opersEnum)
	{
		byte[] bytes = new byte[256];
		try
		{
			int byteCount = server.Client.Send(new byte[1] { opersEnum }, SocketFlags.None);
			byteCount = server.Client.Receive(bytes, SocketFlags.None);
		}
		catch (SocketException ex)
		{
			return ex.ErrorCode;
		}
		return 0;
	}

	public void AddOutput(string msg)
	{
		Dispatcher.Invoke(() =>
		{
			Message = msg;
		});
	}
}
