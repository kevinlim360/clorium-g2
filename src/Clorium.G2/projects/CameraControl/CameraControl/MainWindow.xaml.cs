using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CameraControl.Classes;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Database;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Canon;
using CameraControl.Devices.Classes;
using CameraControl.Layouts;
using CameraControl.ViewModel;
using CameraControl.windows;
using Hardcodet.Wpf.TaskbarNotification;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MaterialDesignThemes.Wpf;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using WMPLib;

namespace CameraControl;

public partial class MainWindow : MetroWindow, IMainWindowPlugin, INotifyPropertyChanged, IComponentConnector
{
	private struct LASTINPUTINFO
	{
		public uint cbSize;

		public uint dwTime;
	}

	public enum EXIFOrientation
	{
		Deg_0 = 1,
		Deg_90 = 6,
		Deg_180 = 3,
		Deg_270 = 8
	}

	private object _locker = new object();

	private FileItem _selectedItem = null;

	private System.Timers.Timer _selectiontimer = new System.Timers.Timer(4000.0);

	private DateTime _lastLoadTime = DateTime.Now;

	private double _usingMemory = 0.0;

	private int _capturecount = 0;

	private string _barcodevalue = "None";

	public SignalRServerHost SignalRWebServer = new SignalRServerHost();

	public HttpServerAndroid AndroidAppServer = new HttpServerAndroid();

	public FTPServerWatcher WatcherFtpServer = new FTPServerWatcher();

	private readonly MainMenuViewModel mainModel;

	private readonly Arduino arduino;

	private readonly SerialPort serialPort1;

	private DispatcherTimer _ManagementTimer;

	private bool isScreenHidden = false;

	private List<ICameraDevice> activeCamerasInLiveWnd = new List<ICameraDevice>();

	private DateTime lastWarningTime = DateTime.MinValue;

	private DateTime lastPowerSaveTime = DateTime.Now;

	private string[] CameraSerial = new string[7];

	private string[] CameraAlias = new string[7];

	private bool isTestStop = false;

	private ushort Exif_Rotate = 1;

	private System.Timers.Timer SessionTimer = new System.Timers.Timer(500.0);

	private DateTime SessionStartTime;

	private long elapsedTicks = 0L;

	private bool isBlinking = false;

	private bool isAlarmPlayed = false;

	private WindowsMediaPlayer wmpPlayer;

	private System.Timers.Timer stopTimer;

	private Storyboard _blink;

	private bool isToggleRot = false;

	private bool is270CaptureMode = false;

	private bool _isInternalChange = false;

	private DateTime lastEventTime = DateTime.MinValue;

	private bool isReadOnly = false;

	private bool isCalibrationMode = false;

	private string filename_Calibration = "D:\\Clorium Data\\Clorium_Calibration.jpg";

	private readonly SerialPort serialPortBarcode = new SerialPort();

	private ManagementEventWatcher pnpWatcher;

	private SolidColorBrush _connectStatus = System.Windows.Media.Brushes.Red;

	public string DisplayName { get; set; }

	public RelayCommand<AutoExportPluginConfig> ConfigurePluginCommand { get; set; }

	public RelayCommand<IAutoExportPlugin> AddPluginCommand { get; set; }

	public RelayCommand<CameraPreset> SelectPresetCommand { get; private set; }

	public RelayCommand<CameraPreset> DeletePresetCommand { get; private set; }

	public RelayCommand<CameraPreset> LoadInAllPresetCommand { get; private set; }

	public RelayCommand<CameraPreset> VerifyPresetCommand { get; private set; }

	public double UsingMemory
	{
		get
		{
			return _usingMemory;
		}
		set
		{
			if (_usingMemory != value)
			{
				_usingMemory = value;
				NotifyPropertyChanged("UsingMemory");
			}
		}
	}

	public int CaptureCount
	{
		get
		{
			return _capturecount;
		}
		set
		{
			if (_capturecount != value)
			{
				_capturecount = value;
				NotifyPropertyChanged("CaptureCount");
			}
		}
	}

	public string BarCodeValue
	{
		get
		{
			return _barcodevalue;
		}
		set
		{
			if (_barcodevalue != value)
			{
				_barcodevalue = value;
				NotifyPropertyChanged("BarCodeValue");
			}
		}
	}

	public SolidColorBrush ConnectStatus
	{
		get
		{
			return _connectStatus;
		}
		set
		{
			_connectStatus = value;
			NotifyPropertyChanged("ConnectStatus");
		}
	}

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public virtual void NotifyPropertyChanged(string propertyName)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public MainWindow()
	{
		DisplayName = "Default";
		CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (object sender1, ExecutedRoutedEventArgs args) =>
		{
			Close();
		}));
		ConfigurePluginCommand = new RelayCommand<AutoExportPluginConfig>(ConfigurePlugin);
		AddPluginCommand = new RelayCommand<IAutoExportPlugin>(AddPlugin);
		SelectPresetCommand = new RelayCommand<CameraPreset>(SelectPreset);
		DeletePresetCommand = new RelayCommand<CameraPreset>(DeletePreset, (CameraPreset o) => ServiceProvider.Settings.CameraPresets.Count > 0);
		LoadInAllPresetCommand = new RelayCommand<CameraPreset>(LoadInAllPreset);
		VerifyPresetCommand = new RelayCommand<CameraPreset>(VerifyPreset);
		InitializeComponent();
		if (!string.IsNullOrEmpty(ServiceProvider.Branding.ApplicationTitle))
		{
			Title = ServiceProvider.Branding.ApplicationTitle;
		}
		if (!string.IsNullOrEmpty(ServiceProvider.Branding.LogoImage) && File.Exists(ServiceProvider.Branding.LogoImage))
		{
			BitmapImage bi = new BitmapImage();
			bi.BeginInit();
			bi.UriSource = new Uri(PhotoUtils.GetFullPath(ServiceProvider.Branding.LogoImage));
			bi.EndInit();
			Icon = bi;
		}
		_selectiontimer.Elapsed += Selectiontimer_Elapsed;
		_selectiontimer.AutoReset = false;
		ServiceProvider.WindowsManager.Event += WindowsManager_Event;
		_ManagementTimer = new DispatcherTimer();
		_ManagementTimer.Interval = TimeSpan.FromSeconds(1.0);
		_ManagementTimer.Tick += ManagementTimer_Tick;
		_ManagementTimer.Start();
		LightControlWnd.Instance.AllLightOn();
		ServiceProvider.Settings.PrevTime = default;
		ServiceProvider.Settings.PauseTime = default;
		ServiceProvider.Settings.IsTimerRunning = false;
		SessionTimer.Elapsed += SessionTimer_Elapsed;
		if (ServiceProvider.Settings.UseWebserver)
		{
			SignalRWebServer.StartSignalRWebServer();
			AndroidAppServer.StartAndroidServer();
		}
		if (ServiceProvider.Settings.UseFTPserver)
		{
			WatcherFtpServer.StartFTPServer();
		}
		if (ServiceProvider.Settings.UseLEDDimmer)
		{
			serialPort1 = new SerialPort();
			arduino = new Arduino(serialPort1, 57600, "CH340");
			arduino.ConnectStatusChanged += (System.Windows.Media.Brush status) =>
			{
				System.Windows.Application.Current.Dispatcher.Invoke(() =>
				{
					ConnectStatus = (SolidColorBrush)status;
				});
			};
		}
		if (ServiceProvider.Settings.UseAlarm)
		{
			mainModel = MainMenuViewModel.Instance;
			mainModel.BarcodeChanged += OnBarcodeChanged;
		}
	}

	private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
	{
		DataContext = ServiceProvider.Settings;
		ServiceProvider.DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
		ServiceProvider.DeviceManager.CameraSelected += DeviceManager_CameraSelected;
		ServiceProvider.DeviceManager.CameraConnected += DeviceManager_CameraConnected;
		ServiceProvider.DeviceManager.CameraDisconnected += DeviceManager_CameraDisconnected;
		SetLayout(ServiceProvider.Settings.SelectedLayout);
		if (ServiceProvider.Settings.StartMinimized)
		{
			WindowState = WindowState.Minimized;
		}
		if (ServiceProvider.Settings.DefaultSession.TimeLapseSettings.Started)
		{
			ServiceProvider.WindowsManager.ExecuteCommand("TimeLapseWnd_Show");
			ServiceProvider.WindowsManager.ExecuteCommand("TimeLapse_Start");
		}
		Thread thread = new Thread(StartupThread);
		thread.Start();
		thread.Join();
		SortCameras();
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (SuperfocusWindow.AnyWorking) { e.Cancel = true; SuperfocusWindow.StopAll(); return; }
		if (ServiceProvider.Settings.MinimizeToTrayIcon)
		{
			WindowState = WindowState.Minimized;
			e.Cancel = true;
		}
		if (!e.Cancel) SuperfocusWindow.CloseAll();
		USBDisconnect();
		AndroidAppServer.StopAndroidServer();
		SignalRWebServer.Dispose();
		WatcherFtpServer.StopFTPServer();
		DisconnectBarcodeScanner();
		LightControlWnd.Instance.AllLightOff();
		ExternProcess_Close();
		_ManagementTimer.Stop();
		_ManagementTimer.Tick -= ManagementTimer_Tick;
		SessionTimer.Elapsed -= SessionTimer_Elapsed;
	}

	private void Window_Closed(object sender, EventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("All_Close");
	}

	private void Window_StateChanged(object sender, EventArgs e)
	{
		if (WindowState == WindowState.Minimized && ServiceProvider.Settings.MinimizeToTrayIcon && !ServiceProvider.Settings.HideTrayNotifications)
		{
			Hide();
			Dispatcher.BeginInvoke((Action)(() =>
			{
				MyNotifyIcon.HideBalloonTip();
				MyNotifyIcon.ShowBalloonTip(ServiceProvider.Branding.ApplicationTitle, "Application was minimized \nDouble click to restore", BalloonIcon.Info);
			}));
		}
	}

	private void MetroWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (!txt_CaptureName.IsFocused)
		{
			TriggerClass.KeyDown(e);
		}
	}

	private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Delete)
		{
			ServiceProvider.WindowsManager.ExecuteCommand("Del_Image");
			e.Handled = true;
		}
	}

	private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand((e.Delta > 0) ? "Next_Image" : "Prev_Image");
	}

	private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("RefreshDisplay");
	}

	private void WindowsManager_Event(string cmd, object o)
	{
		switch (cmd)
		{
		case "ResetTimer":
			ResetTimer();
			break;
		case "SortCameras":
			SortCameras();
			break;
		case "MainWnd_Message":
			StaticHelper.Instance.SystemMessage = o.ToString();
			break;
		case "SetLayout":
			SetLayout(o.ToString());
			break;
		case "Restore":
			Dispatcher.BeginInvoke((Action)(() =>
			{
				try
				{
					Show();
					WindowState = WindowState.Normal;
					Activate();
					Focus();
				}
				catch (Exception exception)
				{
					Log.Debug("Unable to restore window", exception);
				}
			}));
			break;
		case "All_Minimize":
			Dispatcher.Invoke(() =>
			{
				WindowState = WindowState.Minimized;
			});
			break;
		}
	}

	private void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
	{
		Dispatcher.BeginInvoke((Action)(() =>
		{
			if (!ServiceProvider.Settings.HideTrayNotifications)
			{
				MyNotifyIcon.HideBalloonTip();
				MyNotifyIcon.ShowBalloonTip("Camera connected", cameraDevice.LoadProperties().DeviceName, BalloonIcon.Info);
			}
			SortCameras();
		}));
		if (!(cameraDevice.DeviceName == "Z-App camera") && !(cameraDevice.DeviceName == "Z-FTP camera") && !(cameraDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName))
		{
			string cameraSerial = cameraDevice.SerialNumber;
			RegistAlias(cameraSerial);
			if (cameraDevice.Manufacturer.Contains("Canon"))
			{
				ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Hide");
				Thread.Sleep(500);
			}
			Log.Debug("Main : DeviceManager_CameraConnected", ServiceProvider.Settings.UseDebugLog);
		}
	}

	private void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
	{
		Dispatcher.BeginInvoke((Action)(() =>
		{
			Title = (ServiceProvider.Branding.ApplicationTitle ?? "Clorium") + " - " + ((newcameraDevice == null) ? "" : newcameraDevice.DisplayName);
		}));
		if (newcameraDevice.DeviceName == null || newcameraDevice.DeviceName == "Z-App camera" || newcameraDevice.DeviceName == "Z-FTP camera" || newcameraDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName)
		{
			return;
		}
		List<ICameraDevice> allEosCameras = ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice x) => x is CanonSDKBase).ToList();
		if (allEosCameras.Count != 0)
		{
			if (ServiceProvider.Settings.ShowLiveView && newcameraDevice.Manufacturer.Contains("Canon") && allEosCameras.Count == 1)
			{
				Thread.Sleep(500);
				ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Show", newcameraDevice);
			}
			Log.Debug("Main : DeviceManager_CameraSelected", ServiceProvider.Settings.UseDebugLog);
		}
	}

	private void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
	{
		Dispatcher.BeginInvoke((Action)(() =>
		{
			if (!ServiceProvider.Settings.HideTrayNotifications)
			{
				MyNotifyIcon.HideBalloonTip();
				MyNotifyIcon.ShowBalloonTip("Camera disconnected", cameraDevice.LoadProperties().DeviceName, BalloonIcon.Info);
			}
		}));
		RemoveAlias(cameraDevice.SerialNumber);
		Log.Debug("Main : DeviceManager_CameraDisconnected", ServiceProvider.Settings.UseDebugLog);
	}

	[DllImport("user32.dll")]
	private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

	private void ManagementTimer_Tick(object sender, EventArgs e)
	{
		ComputerInfo ci = new ComputerInfo();
		ulong totalMemory = ci.TotalPhysicalMemory;
		ulong availableMemory = ci.AvailablePhysicalMemory;
		double usedMemory = totalMemory - availableMemory;
		double usagePercent = (UsingMemory = usedMemory / (double)totalMemory * 100.0);
		if (usagePercent > 90.0 && (DateTime.Now - lastWarningTime).TotalMinutes >= 10.0)
		{
			_ManagementTimer.Stop();
			if (!ServiceProvider.Settings.ShowDebugTest)
			{
				Log.Debug("⚠\ufe0f 메모리 사용량이 90%가 넘었습니다.");
				System.Windows.MessageBox.Show("⚠\ufe0f 메모리 사용량이 90%가 넘었습니다.\n⚠\ufe0f 프로그램 사용에는 지장없습니다.\n⚠\ufe0f 실행이 느려지면 재시작하는 것을 권장합니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			lastWarningTime = DateTime.Now;
			_ManagementTimer.Start();
		}
		if ((DateTime.Now - lastPowerSaveTime).TotalMinutes >= (double)ServiceProvider.Settings.LightPowerSaveTime)
		{
			LightControlWnd.Instance.AllLightOff();
			Send_Dimming("0");
			lastPowerSaveTime = DateTime.Now;
		}
		LASTINPUTINFO lastInputInfo = default;
		lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
		if (!GetLastInputInfo(ref lastInputInfo))
		{
			return;
		}
		uint idleMilliseconds = (uint)Environment.TickCount - lastInputInfo.dwTime;
		double idleMinutes = (double)idleMilliseconds / 1000.0 / 60.0;
		if (idleMinutes >= 60.0)
		{
			if (isScreenHidden || !(ServiceProvider.WindowsManager.Get(typeof(LiveViewManager)) is LiveViewManager lvManager) || lvManager.ActiveDeviceList.Count <= 0)
			{
				return;
			}
			isScreenHidden = true;
			activeCamerasInLiveWnd.Clear();
			List<ICameraDevice> currentActive = lvManager.ActiveDeviceList;
			foreach (ICameraDevice device in currentActive)
			{
				if (device != null && device.IsConnected)
				{
					activeCamerasInLiveWnd.Add(device);
					ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Hide", device);
					Thread.Sleep(300);
				}
			}
			Log.Debug($"\ud83d\udca4 라이브 뷰 유휴 모드 진입: {activeCamerasInLiveWnd.Count}개 숨김입니다.");
		}
		else
		{
			if (!isScreenHidden)
			{
				return;
			}
			isScreenHidden = false;
			foreach (ICameraDevice device2 in activeCamerasInLiveWnd)
			{
				if (device2 != null && device2.IsConnected)
				{
					ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Show", device2);
					Thread.Sleep(300);
				}
			}
			Log.Debug($"⚡ 라이브 뷰 활동 감지: {activeCamerasInLiveWnd.Count}개 복구했습니다.");
			activeCamerasInLiveWnd.Clear();
		}
	}

	private void Selectiontimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		if (_selectedItem != null)
		{
			ServiceProvider.WindowsManager.ExecuteCommand("Select_Image", _selectedItem);
		}
	}

	private void InitialzeProgram()
	{
		ServiceProvider.Settings.isBtnPressed = false;
		ServiceProvider.Settings.GetMeasurePicture = false;
		ServiceProvider.Settings.ThumbailLoaded = true;
		ServiceProvider.Settings.TurnTableShot = false;
		ServiceProvider.Settings.FrontBackMode = 0;
		ServiceProvider.Settings.ShowAPICommunication = false;
		ServiceProvider.Settings.IsDefect = false;
	}

	private void InitialzeDeivce()
	{
		if (ServiceProvider.Settings.UseBarcodeScanner && !ServiceProvider.Settings.D_DayBefore)
		{
			ConnectBarcodeScanner();
		}
		if (ServiceProvider.Settings.UseMonstarDeck)
		{
			ExeMonsterDeck();
		}
		if (ServiceProvider.Settings.UseLEDDimmer)
		{
			USBConnect();
			Send_Dimming(ServiceProvider.Settings.LED_Dimming);
		}
	}

	private void InitialzeCameraDevice(ICameraDevice cameraDevice)
	{
		CameraProperty property = cameraDevice.LoadProperties();
		if (property.DefaultPresetName != "(None)" && property.DefaultPresetName != "")
		{
			ServiceProvider.Settings.CurrentPresetName = property.DefaultPresetName;
		}
		if (property.LiveviewSettings.RotationIndex == 3)
		{
			but_ToggleRot_Click(this, null);
		}
		UpdateSliderValue(GetColorTemperature());
		if (!(cameraDevice.DeviceName == "Z-App camera") && !(cameraDevice.DeviceName == "Z-FTP camera"))
		{
			List<ICameraDevice> allEosCameras = ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice x) => x is CanonSDKBase).ToList();
			if (allEosCameras.Count != 0 && ServiceProvider.Settings.ShowLiveView && cameraDevice.Manufacturer.Contains("Canon") && allEosCameras.Count == 1)
			{
				Thread.Sleep(300);
				ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Show", cameraDevice);
			}
		}
	}

	private void StartupThread()
	{
		Log.Debug("Main : StartupThread()");
		InitialzeProgram();
		InitialzeDeivce();
		foreach (ICameraDevice cameraDevice in ServiceProvider.DeviceManager.ConnectedDevices)
		{
			if (cameraDevice.DeviceName == "")
			{
				continue;
			}
			string cameraSerial = cameraDevice.SerialNumber;
			RegistAlias(cameraSerial);
			Dispatcher.BeginInvoke((Action)(() =>
			{
				InitialzeCameraDevice(cameraDevice);
			}));
			CameraProperty property = cameraDevice.LoadProperties();
			CameraPreset preset = ServiceProvider.Settings.GetPreset(property.DefaultPresetName);
			if (preset != null)
			{
				try
				{
					Thread.Sleep(500);
					cameraDevice.WaitForCamera(5000);
					preset.Set(cameraDevice);
				}
				catch (Exception exception)
				{
					Log.Error("Main : 프리셋 로드 실패 -> ", exception);
				}
				Log.Debug("Main : StartupThread() Preset");
			}
			try
			{
				if ((cameraDevice is CanonSDKBase && ServiceProvider.Settings.LoadCanonTransferMode) || !(cameraDevice is CanonSDKBase))
				{
					cameraDevice.CaptureInSdRam = property.CaptureInSdRam;
					Log.Debug("Main : CaptureInSdRam", ServiceProvider.Settings.UseDebugLog);
				}
			}
			catch (Exception exception2)
			{
				Log.Error("Main : Unable to CaptureInSdRam or DateTime", exception2);
			}
		}
	}

	private void ExeMonsterDeck()
	{
		Log.Debug("Main : Monster Deck Run");
		Process[] myProc = Process.GetProcessesByName("MONSTAR DECK");
		if (myProc.Length == 0)
		{
			string processname = "C:\\Program Files (x86)\\MONSTAR DECK\\MONSTAR DECK.exe";
			Process.Start(processname);
		}
	}

	private void but_qr_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : QRCode Click");
		QrCodeWnd wnd = new QrCodeWnd();
		wnd.Owner = this;
		wnd.Show();
	}

	private void but_AIRemoveBg_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : RemoveBg Click");
		Process[] myProc = Process.GetProcessesByName("PhotoScissors");
		if (myProc.Length < 1)
		{
			string processname = "C:\\\\Program Files (x86)\\\\Clorium\\PhotoScissors.exe";
			Process.Start(processname);
		}
	}

	private void but_Ruler_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : AutoSizing Click");
		Process[] myProc = Process.GetProcessesByName("Clorium AutoSizing");
		if (myProc.Length < 1)
		{
			string processname = "C:\\\\Program Files (x86)\\\\Clorium\\Clorium AutoSizing.exe";
			Process.Start(processname);
		}
	}

	private void but_Lighting_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Lighting Click");
		LightControlWnd wnd = new LightControlWnd(this);
		wnd.Owner = this;
		if (wnd.ShowDialog() != true)
		{
		}
	}

	private void but_TurnTable_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : TurnTable Click");
		ServiceProvider.WindowsManager.ExecuteCommand("TurnTableWnd_Show");
	}

	private void but_multicamera_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : MultipleCamera Click");
		ServiceProvider.WindowsManager.ExecuteCommand("MultipleCameraWnd_Show");
	}

	private void but_Enfuse_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Enfuse Click");
		foreach (IToolPlugin item in ServiceProvider.PluginManager.ToolPlugins)
		{
			if (item.Title == "Enfuse")
			{
				item.Execute();
			}
		}
	}

	private void but_ErrorInform_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : ErrorInform Click");
		string folderPath = "D:\\Clorium Data\\Error 정보\\";
		string date = DateTime.Now.ToString("yyyy-MM-dd HH시mm분");
		if (!Directory.Exists(folderPath))
		{
			Directory.CreateDirectory(folderPath);
		}
		string src = Path.Combine(Settings.DataFolder, "settings.json");
		string dst = folderPath + date + " settings.json";
		File.Copy(src, dst);
		src = Path.Combine(Settings.DataFolder + "\\Log", "app.log");
		dst = folderPath + date + " app.log";
		File.Copy(src, dst);
		dst = folderPath + date + " MiainView.jpg";
		CaptureScreenToJpg(0, dst);
		dst = folderPath + date + " LiveView.jpg";
		CaptureScreenToJpg(1, dst);
		System.Windows.MessageBox.Show("\ud83d\udccd 에러 관련 정보 저장 완료했습니다.");
	}

	private void Version_DoubleClick(object sender, RoutedEventArgs e)
	{
		string filePath = "D:\\Clorium Data\\Clorium_Version.md";
		ProcessStartInfo psi = new ProcessStartInfo("notepad.exe", "\"" + filePath + "\"");
		psi.UseShellExecute = false;
		try
		{
			Process.Start(psi);
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show("\ud83d\udccd 파일을 열 수 없습니다: " + ex.Message);
		}
	}

	private void but_LightingOn_Click(object sender, RoutedEventArgs e)
	{
		LightControlWnd.Instance.AllLightOn();
		Send_Dimming(ServiceProvider.Settings.LED_Dimming);
		lastPowerSaveTime = DateTime.Now;
	}

	private void but_LightingOff_Click(object sender, RoutedEventArgs e)
	{
		LightControlWnd.Instance.AllLightOff();
		Send_Dimming("0");
		lastPowerSaveTime = DateTime.Now;
	}

	private void but_ColorCompensation_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("OpenColorCompensation Click");
		// G2: new colour-correction window (Magick.NET engine). Saves a new file next to the original; never overwrites it.
		string file = ServiceProvider.Settings?.SelectedBitmap?.FileItem?.FileName;
		if (string.IsNullOrEmpty(file) || !File.Exists(file))
		{
			System.Windows.MessageBox.Show("색보정할 사진을 먼저 선택해 주세요.", "색보정");
			return;
		}
		if (!G2ImageAdjust.IsSupportedFile(file))
		{
			System.Windows.MessageBox.Show("JPEG, PNG, TIFF, BMP 사진만 색보정할 수 있습니다. RAW 파일은 지원하지 않습니다.", "색보정");
			return;
		}
		G2ColorAdjustWnd wnd = new G2ColorAdjustWnd(file) { Owner = this };
		wnd.ShowDialog();
	}

	private void ExternProcess_Close()
	{
		Process[] processList1 = Process.GetProcessesByName("Clorium AutoSizing");
		if (processList1.Length != 0)
		{
			Process[] array = processList1;
			foreach (Process proc in array)
			{
				if (!proc.HasExited)
				{
					proc.Kill();
				}
			}
		}
		Process[] processList2 = Process.GetProcessesByName("Clorium Centering");
		if (processList2.Length != 0)
		{
			Process[] array2 = processList2;
			foreach (Process proc2 in array2)
			{
				if (!proc2.HasExited)
				{
					proc2.Kill();
				}
			}
		}
		Process[] processList3 = Process.GetProcessesByName("PhotoScissors");
		if (processList3.Length == 0)
		{
			return;
		}
		Process[] array3 = processList3;
		foreach (Process proc3 in array3)
		{
			if (!proc3.HasExited)
			{
				proc3.Kill();
			}
		}
	}

	private void but_timelapse_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Timelapse Click");
		ServiceProvider.WindowsManager.ExecuteCommand("TimeLapseWnd_Show");
	}

	private void but_fullscreen_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : FullScreen Click");
		ServiceProvider.WindowsManager.ExecuteCommand("FullScreenWnd_Show");
	}

	private void btn_br_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Braketing Click");
		BraketingWnd wnd = new BraketingWnd(ServiceProvider.DeviceManager.SelectedCameraDevice, ServiceProvider.Settings.DefaultSession);
		wnd.ShowDialog();
	}

	private void btn_browse_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Browse Click");
		ServiceProvider.WindowsManager.ExecuteCommand("BrowseWnd_Show");
	}

	private void but_download_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Download Click");
		ServiceProvider.WindowsManager.ExecuteCommand("DownloadPhotosWnd_Show", ServiceProvider.DeviceManager.SelectedCameraDevice);
	}

	private void but_star_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Star Click");
		ServiceProvider.WindowsManager.ExecuteCommand("BulbWnd_Show", ServiceProvider.DeviceManager.SelectedCameraDevice);
	}

	private void but_print_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Print Click");
		ServiceProvider.WindowsManager.ExecuteCommand("PrintWnd_Show");
	}

	private void but_bt_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Bluetooth Click");
		try
		{
			BluetoothView dlg = new BluetoothView();
			dlg.Owner = this;
			dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			dlg.ShowDialog();
		}
		catch (Exception ex)
		{
			Log.Error("Unable to connect to Bluetooth device", ex);
			this.ShowMessageAsync("Error", "Unable to connect to Bluetooth device " + ex.Message);
		}
	}

	private void but_barcode_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Barcode Click");
		ServiceProvider.WindowsManager.ExecuteCommand("BarcodeWnd_Show");
	}

	private void but_sequencer_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Sequencer Click");
		foreach (IToolPlugin item in ServiceProvider.PluginManager.ToolPlugins)
		{
			if (item.Title == TranslationStrings.LabelImageSequencer)
			{
				item.Execute();
			}
		}
	}

	private void but_wifi_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Wifi Click");
		try
		{
			if (string.IsNullOrEmpty(ServiceProvider.Settings.WifiIp))
			{
				ServiceProvider.Settings.WifiIp = "192.168.1.1";
			}
			GetIpWnd wnd = new GetIpWnd();
			wnd.Owner = this;
			if (wnd.ShowDialog() == true)
			{
				ServiceProvider.DeviceManager.AddDevice(wnd.WifiDeviceProvider.Connect(wnd.Ip));
			}
		}
		catch (Exception ex)
		{
			Log.Error("Unable to connect to WiFi device", ex);
			StaticHelper.Instance.SystemMessage = "Unable to connect to WiFi device " + ex.Message;
		}
	}

	private void MyNotifyIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
	{
		Show();
		WindowState = WindowState.Normal;
	}

	private void btn_sort_Click(object sender, RoutedEventArgs e)
	{
		SortCameras(asc: true);
	}

	private void btn_sort_desc_Click(object sender, RoutedEventArgs e)
	{
		SortCameras(asc: false);
	}

	private void btn_NewPreset_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : New Preset Click");
		if (IsCameraBusy())
		{
			return;
		}
		CameraPreset cameraPreset = new CameraPreset();
		SavePresetWnd wnd = new SavePresetWnd(cameraPreset);
		wnd.Owner = this;
		if (wnd.ShowDialog() != true)
		{
			return;
		}
		foreach (CameraPreset preset in ServiceProvider.Settings.CameraPresets)
		{
			if (preset.Name == cameraPreset.Name)
			{
				cameraPreset = preset;
				break;
			}
		}
		cameraPreset.Get(ServiceProvider.DeviceManager.SelectedCameraDevice);
		if (!ServiceProvider.Settings.CameraPresets.Contains(cameraPreset))
		{
			ServiceProvider.Settings.CameraPresets.Add(cameraPreset);
		}
		ServiceProvider.Settings.Save();
		ServiceProvider.Settings.CurrentPresetName = cameraPreset.Name;
		ServiceProvider.Settings.CurrentColorTemperature = (int)slider.Value;
	}

	private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		Log.Debug("Main ICON : Load Preset ListBox Double Click");
		if (!IsCameraBusy() && PresetListBox.SelectedItem != null)
		{
			CameraPreset preset = PresetListBox.SelectedItem as CameraPreset;
			SelectPreset(preset);
		}
	}

	private void SelectPreset(CameraPreset preset)
	{
		Log.Debug("Main ICON : Load Preset Click");
		if (IsCameraBusy() || preset == null)
		{
			return;
		}
		try
		{
			preset.Set(ServiceProvider.DeviceManager.SelectedCameraDevice);
			Thread.Sleep(300);
			UpdateSliderValue(GetColorTemperature());
			ServiceProvider.Settings.CurrentPresetName = preset.Name;
			ServiceProvider.Settings.CurrentColorTemperature = (int)slider.Value;
		}
		catch (Exception exception)
		{
			Log.Error("Unable to set PresetLoad", exception);
		}
	}

	private void btn_ShowPreset_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : Edit Preset Click");
		PresetEditWnd wnd = new PresetEditWnd();
		wnd.Owner = this;
		wnd.ShowDialog();
	}

	private void DeletePreset(CameraPreset obj)
	{
		if (!ServiceProvider.Settings.CheckPassword())
		{
			return;
		}
		Log.Debug("Main ICON : Delete Preset Click");
		if (IsCameraBusy() || obj == null)
		{
			return;
		}
		ServiceProvider.Settings.CameraPresets.Remove(obj);
		try
		{
			File.Delete(obj.FileName);
		}
		catch (Exception exception)
		{
			Log.Error("Unable to delete Preset", exception);
		}
	}

	private void LoadInAllPreset(CameraPreset preset)
	{
		if (IsCameraBusy() || preset == null)
		{
			return;
		}
		ProgressWindow dlg = new ProgressWindow();
		dlg.Show();
		try
		{
			int i = 0;
			dlg.MaxValue = ServiceProvider.DeviceManager.ConnectedDevices.Count;
			foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices)
			{
				if (!(connectedDevice.DeviceName == "Z-App camera") && !(connectedDevice.DeviceName == "Z-FTP camera") && !(connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName) && connectedDevice != null && connectedDevice.IsConnected)
				{
					try
					{
						dlg.Label = connectedDevice.DisplayName;
						dlg.Progress = i;
						i++;
						preset.Set(connectedDevice);
					}
					catch (Exception exception)
					{
						Log.Error("Unable to set AllPreset Load 1", exception);
					}
					Thread.Sleep(250);
				}
			}
		}
		catch (Exception exception2)
		{
			Log.Error("Unable to set AllPreset Load 2", exception2);
		}
		dlg.Hide();
	}

	private void VerifyPreset(CameraPreset preset)
	{
		if (IsCameraBusy() || preset == null)
		{
			return;
		}
		ProgressWindow dlg = new ProgressWindow();
		dlg.Show();
		try
		{
			int i = 0;
			dlg.MaxValue = ServiceProvider.DeviceManager.ConnectedDevices.Count;
			foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices)
			{
				if (!(connectedDevice.DeviceName == "Z-App camera") && !(connectedDevice.DeviceName == "Z-FTP camera") && !(connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName) && connectedDevice != null && connectedDevice.IsConnected)
				{
					try
					{
						dlg.Label = connectedDevice.DisplayName;
						dlg.Progress = i;
						i++;
						preset.Verify(connectedDevice);
					}
					catch (Exception exception)
					{
						Log.Error("Unable to set PresetVerify 1", exception);
					}
					Thread.Sleep(250);
				}
			}
		}
		catch (Exception exception2)
		{
			Log.Error("Unable to set PresetVerify 2", exception2);
		}
		dlg.Hide();
	}

	private bool IsCameraBusy()
	{
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
		{
			Log.Debug("카메라가 Busy 상태입니다.");
			return true;
		}
		return false;
	}

	private void Button_EditSession_Click(object sender, RoutedEventArgs e)
	{
		EditSession();
	}

	private void EditSession()
	{
		try
		{
			EditSession editSession = new EditSession(ServiceProvider.Settings.DefaultSession);
			editSession.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
			editSession.ShowDialog();
			ServiceProvider.Settings.Save(ServiceProvider.Settings.DefaultSession);
		}
		catch (Exception ex)
		{
			Log.Error("Error refresh session ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", ex.Message);
		}
	}

	private void AddPlugin(IAutoExportPlugin obj)
	{
		ConfigurePlugin(ServiceProvider.Settings.DefaultSession.AddPlugin(obj));
	}

	private void ConfigurePlugin(AutoExportPluginConfig plugin)
	{
		AutoExportPluginEdit pluginEdit = new AutoExportPluginEdit
		{
			DataContext = new AutoExportPluginEditViewModel(plugin),
			Owner = this
		};
		pluginEdit.ShowDialog();
	}

	private void ExecuteAutoexportPlugins(ICameraDevice cameraDevice, FileItem fileItem, bool runOnTransfered)
	{
		foreach (AutoExportPluginConfig plugin in ServiceProvider.Settings.DefaultSession.AutoExportPluginConfigs)
		{
			if (plugin.RunAfterTransfer == runOnTransfered && plugin.IsEnabled && plugin.Evaluate(cameraDevice))
			{
				IAutoExportPlugin pl = ServiceProvider.PluginManager.GetAutoExportPlugin(plugin.Type);
				try
				{
					pl.Execute(fileItem, plugin);
					ServiceProvider.Analytics.PluginExecute(plugin.Type);
					Log.Debug("AutoexportPlugin executed " + plugin.Type);
				}
				catch (Exception ex)
				{
					plugin.IsError = true;
					plugin.Error = ex.Message;
					plugin.IsRedy = true;
					Log.Error("Error to apply plugin", ex);
				}
			}
		}
	}

	private void SetLayout(string enumname)
	{
		if (Enum.TryParse<LayoutTypeEnum>(enumname, ignoreCase: true, out var type))
		{
			SetLayout(type);
		}
	}

	private void SetLayout(LayoutTypeEnum type)
	{
		ServiceProvider.Settings.SelectedLayout = type.ToString();
		if (StackLayout.Children.Count > 0 && StackLayout.Children[0] is LayoutBase cnt)
		{
			cnt.UnInit();
		}
		switch (type)
		{
		case LayoutTypeEnum.Normal:
		{
			StackLayout.Children.Clear();
			LayoutNormal control3 = new LayoutNormal();
			PreviewPanel.DataContext = control3.ZoomAndPanControl;
			StackLayout.Children.Add(control3);
			break;
		}
		case LayoutTypeEnum.Grid:
		{
			StackLayout.Children.Clear();
			LayoutGrid control2 = new LayoutGrid();
			PreviewPanel.DataContext = control2.ZoomAndPanControl;
			StackLayout.Children.Add(control2);
			break;
		}
		case LayoutTypeEnum.GridRight:
		{
			StackLayout.Children.Clear();
			LayoutGridRight control = new LayoutGridRight();
			PreviewPanel.DataContext = control.ZoomAndPanControl;
			StackLayout.Children.Add(control);
			break;
		}
		}
	}

	private void SortCameras()
	{
		SortCameras(asc: true);
	}

	private void SortCameras(bool asc)
	{
		foreach (ICameraDevice device in ServiceProvider.DeviceManager.ConnectedDevices)
		{
			device.LoadProperties();
		}
		if (asc)
		{
			ServiceProvider.DeviceManager.ConnectedDevices = new AsyncObservableCollection<ICameraDevice>(from x in ServiceProvider.DeviceManager.ConnectedDevices
				orderby x.LoadProperties().SortOrder, x.DisplayName
				select x);
		}
		else
		{
			ServiceProvider.DeviceManager.ConnectedDevices = new AsyncObservableCollection<ICameraDevice>(from x in ServiceProvider.DeviceManager.ConnectedDevices
				orderby x.LoadProperties().SortOrder descending, x.DisplayName descending
				select x);
		}
	}

	private void RegistAlias(string cameraSerial)
	{
		if (cameraSerial == "No1")
		{
			CameraSerial[5] = cameraSerial;
			CameraAlias[5] = "-z";
			return;
		}
		if (cameraSerial == "No2")
		{
			CameraSerial[6] = cameraSerial;
			CameraAlias[6] = "-y";
			return;
		}
		for (int i = 0; i < 5; i++)
		{
			if (CameraSerial[i] == cameraSerial)
			{
				return;
			}
		}
		for (int j = 0; j < 5; j++)
		{
			string camAlias = "";
			switch (j)
			{
			case 0:
				camAlias = "-a";
				break;
			case 1:
				camAlias = "-b";
				break;
			case 2:
				camAlias = "-c";
				break;
			case 3:
				camAlias = "-d";
				break;
			case 4:
				camAlias = "-e";
				break;
			}
			if (string.IsNullOrEmpty(CameraSerial[j]))
			{
				CameraSerial[j] = cameraSerial;
				CameraAlias[j] = camAlias;
				break;
			}
		}
	}

	private void RemoveAlias(string cameraSerial)
	{
		if (cameraSerial == "No1")
		{
			CameraSerial[5] = null;
			CameraAlias[5] = null;
			return;
		}
		if (cameraSerial == "No2")
		{
			CameraSerial[6] = null;
			CameraAlias[6] = null;
			return;
		}
		for (int i = 0; i < 5; i++)
		{
			if (CameraSerial[i] == cameraSerial)
			{
				CameraSerial[i] = null;
				CameraAlias[i] = null;
				break;
			}
		}
	}

	private string FindAlias(string cameraSerial)
	{
		string alias = "";
		if (cameraSerial == "No1")
		{
			return CameraAlias[5];
		}
		if (cameraSerial == "No2")
		{
			return CameraAlias[6];
		}
		for (int i = 0; i < 5; i++)
		{
			if (CameraSerial[i] == cameraSerial)
			{
				alias = CameraAlias[i];
				break;
			}
		}
		return alias;
	}

	public void CaptureScreenToJpg(int monitorIndex, string filePath)
	{
		Screen[] screens = Screen.AllScreens;
		if (monitorIndex >= screens.Length)
		{
			Console.WriteLine("해당 모니터 인덱스가 존재하지 않습니다.");
			return;
		}
		Rectangle bounds = screens[monitorIndex].Bounds;
		using Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
		using (Graphics g = Graphics.FromImage(bitmap))
		{
			g.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);
		}
		bitmap.Save(filePath, ImageFormat.Jpeg);
	}

	private void PlayWave(string filePath)
	{
		if (!string.IsNullOrWhiteSpace(filePath))
		{
			SoundPlayer soundPlayer = new SoundPlayer(filePath);
			soundPlayer.Play();
		}
	}

	private void but_TestStart_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : TestStart Click");
		isTestStop = false;
		Thread thread = new Thread(() =>
		{
			TestCapture();
		});
		thread.Start();
	}

	private void but_TestStop_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : TesStop Click");
		isTestStop = true;
	}

	private void TestCapture()
	{
		bool remember = ServiceProvider.Settings.UseAlarm;
		ServiceProvider.Settings.UseAlarm = false;
		int count = 0;
		string presetName = string.Empty;
		do
		{
			string barcode = DateTime.Now.ToString("HH-mm-ss");
			string testSessionFolder = null;
			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				testSessionFolder = MainMenuViewModel.Instance.CreateNewSessionFolder(barcode);
			});
			if (string.IsNullOrEmpty(testSessionFolder)) break;
			Thread.Sleep(500);
			if (isTestStop)
			{
				break;
			}
			MainWindow mainWindow = ServiceProvider.PluginManager.SelectedWindow as MainWindow;
			for (int i = 0; i < 3; i++)
			{
				if (isTestStop)
				{
					break;
				}
				mainWindow?.Capture();
				if (isTestStop)
				{
					break;
				}
				Thread.Sleep(3000);
			}
			if (isTestStop)
			{
				break;
			}
			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				if (count++ % 2 == 0)
				{
					presetName = "4)  어두운";
					CameraPreset preset = ServiceProvider.Settings.GetPreset(presetName);
					SelectPreset(preset);
					NextAperture();
				}
				else
				{
					presetName = "5)  블랙";
					CameraPreset preset2 = ServiceProvider.Settings.GetPreset(presetName);
					SelectPreset(preset2);
					PrevAperture();
				}
			});
			if (isTestStop)
			{
				break;
			}
			Thread.Sleep(3000);
		}
		while (!isTestStop);
		ServiceProvider.Settings.UseAlarm = remember;
	}

	private void setExifOrientation(bool rotate270)
	{
		if (rotate270)
		{
			switch (ServiceProvider.Settings.RotateIndex)
			{
			case 0:
				Exif_Rotate = 8;
				break;
			case 1:
				Exif_Rotate = 1;
				break;
			case 2:
				Exif_Rotate = 6;
				break;
			case 3:
				Exif_Rotate = 3;
				break;
			}
		}
	}

	private bool IsMusinsaOK()
	{
		if (ServiceProvider.Settings.UseAlarm && !ServiceProvider.Settings.IsTimerRunning)
		{
			System.Windows.MessageBox.Show("\ud83d\udce2일시 정지 중입니다.\ud83d\udce2\r\n촬영을 시작하려면 작업시작 버튼을 누르세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return false;
		}
		return true;
	}

	private void btn_TodayReset_Click(object sender, RoutedEventArgs e)
	{
		if (ServiceProvider.Settings.CheckPassword())
		{
			Log.Debug("Main : TodayReset Click");
			ServiceProvider.Settings.TodaySessionCounter = 0;
		}
	}

	private void SessionTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		elapsedTicks = DateTime.Now.Ticks - SessionStartTime.Ticks;
		TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
		this.Invoke(() =>
		{
			ElapsedTime.Content = elapsedSpan.Hours.ToString("00") + ":" + elapsedSpan.Minutes.ToString("00") + ":" + elapsedSpan.Seconds.ToString("00");
			if ((double)ServiceProvider.Settings.AlarmWaitTime < elapsedSpan.TotalSeconds)
			{
				if (!isAlarmPlayed)
				{
					Thread thread = new Thread(() =>
					{
						PlayAlarm(ServiceProvider.Settings.SoundTime);
					});
					thread.Start();
					isAlarmPlayed = true;
				}
				isBlinking = !isBlinking;
				if (isBlinking)
				{
					ElapsedTime.Background = new SolidColorBrush(Colors.Red);
				}
				else
				{
					ElapsedTime.Background = new SolidColorBrush(Colors.Transparent);
				}
			}
			else
			{
				ElapsedTime.Background = new SolidColorBrush(Colors.Transparent);
				isBlinking = false;
				isAlarmPlayed = false;
			}
		});
	}

	private void but_ToggleStartStop_Click(object sender, RoutedEventArgs e)
	{
		if (ServiceProvider.Settings.UseAlarm)
		{
			if (but_ToggleStartStop.Kind == PackIconKind.Restart)
			{
				but_ToggleStartStop.Kind = PackIconKind.PauseOctagonOutline;
				ToggleStartStopText.Text = "일시정지";
				RestartTimer();
			}
			else
			{
				but_ToggleStartStop.Kind = PackIconKind.Restart;
				ToggleStartStopText.Text = "작업시작";
				PauseTimer();
			}
		}
	}

	private void RestartTimer()
	{
		SessionTimer.Start();
		if (ServiceProvider.Settings.PauseTime != default(DateTime))
		{
			TimeSpan pausedDuration = DateTime.Now - ServiceProvider.Settings.PauseTime;
			SessionStartTime = SessionStartTime.Add(pausedDuration);
			ServiceProvider.Settings.PrevTime = ServiceProvider.Settings.PrevTime.Add(pausedDuration);
		}
		else
		{
			SessionStartTime = DateTime.Now;
			ServiceProvider.Settings.PrevTime = DateTime.Now;
		}
		ServiceProvider.Settings.IsTimerRunning = true;
	}

	private void PauseTimer()
	{
		SessionTimer.Stop();
		elapsedTicks = DateTime.Now.Ticks - SessionStartTime.Ticks;
		TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
		if ((double)ServiceProvider.Settings.AlarmWaitTime < elapsedSpan.TotalSeconds)
		{
			ElapsedTime.Background = new SolidColorBrush(Colors.Red);
		}
		ServiceProvider.Settings.PauseTime = DateTime.Now;
		ServiceProvider.Settings.IsTimerRunning = false;
	}

	private void btn_TimerReset_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main : TimerReset Click");
		SessionTimer.Stop();
		ElapsedTime.Content = "00:00:00";
		ElapsedTime.Background = new SolidColorBrush(Colors.Transparent);
		but_ToggleStartStop.Kind = PackIconKind.Restart;
		ToggleStartStopText.Text = "작업시작";
		ServiceProvider.Settings.PauseTime = default;
		ServiceProvider.Settings.IsTimerRunning = false;
	}

	private void ResetTimer()
	{
		SessionTimer.Start();
		ElapsedTime.Content = "00:00:00";
		ElapsedTime.Background = new SolidColorBrush(Colors.Transparent);
		SessionStartTime = DateTime.Now;
		StopAlarm();
		ServiceProvider.Settings.PrevTime = DateTime.Now;
		ServiceProvider.Settings.IsTimerRunning = true;
	}

	private void PlayAlarm(int seconds)
	{
		if (wmpPlayer == null || wmpPlayer.playState != WMPPlayState.wmppsPlaying)
		{
			string filePath = string.Empty;
			switch (ServiceProvider.Settings.AlarmSoundIndex)
			{
			default:
				return;
			case 0:
				filePath = "D:\\Clorium Data\\Alarm01.mp3";
				break;
			case 1:
				filePath = "D:\\Clorium Data\\Alarm02.mp3";
				break;
			case 2:
				filePath = "D:\\Clorium Data\\Alarm03.mp3";
				break;
			case 3:
				filePath = "D:\\Clorium Data\\Alarm04.mp3";
				break;
			}
			wmpPlayer = (WindowsMediaPlayer)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("6BF52A52-394A-11D3-B153-00C04F79FAA6")));
			wmpPlayer.URL = filePath;
			wmpPlayer.settings.setMode("loop", varfMode: true);
			wmpPlayer.controls.play();
			stopTimer = new System.Timers.Timer(seconds * 1000);
			stopTimer.Elapsed += (object s, ElapsedEventArgs e) =>
			{
				StopAlarm();
				stopTimer.Stop();
				stopTimer.Dispose();
			};
			stopTimer.AutoReset = false;
			stopTimer.Start();
		}
	}

	private void StopAlarm()
	{
		if (wmpPlayer != null)
		{
			wmpPlayer.controls.stop();
			wmpPlayer.close();
			wmpPlayer = null;
		}
	}

	private void but_AlarmOff_Click(object sender, RoutedEventArgs e)
	{
		StopAlarm();
	}

	private void but_Statistics_Click(object sender, RoutedEventArgs e)
	{
		if (ServiceProvider.Settings.UseAlarm)
		{
			Log.Debug("Main : Statistics Click");
			StatisticsCSV();
		}
	}

	private void StatisticsCSV()
	{
		if (!MainMenuViewModel.Instance.API_PostMeasureInform())
		{
			return;
		}
		MainMenuViewModel.Data_FolderInform.cameraUnit = "";
		string directory = Path.Combine(ServiceProvider.Settings.ElapsedDownloadFolder, DateTime.Now.ToString("yyyy년MM월dd일"));
		string filename = Path.Combine(directory, ServiceProvider.Settings.CloriumID + DateTime.Now.ToString("yyMMdd") + ".csv");
		if (!File.Exists(filename))
		{
			System.Windows.MessageBox.Show("\ud83d\udea8 CSV 파일이 존재하지 않습니다.");
			return;
		}
		try
		{
			if (!File.Exists(filename) || new System.IO.FileInfo(filename).Length <= 1)
			{
				return;
			}
			string[] lines = File.ReadAllLines(filename, Encoding.UTF8);
			if (lines.Length >= 7)
			{
				string avgLine = lines[lines.Length - 4];
				string totalLine = lines[lines.Length - 3];
				if (avgLine.Contains("평균시간(초)") || totalLine.Contains("누적시간(초)"))
				{
					System.Windows.MessageBox.Show("\ud83d\udea8 이미 통계 데이터가 작성된 파일입니다. 작업을 종료합니다.");
					return;
				}
			}
			List<int> elapsedTimes = (from line in lines.Skip(1)
				select line.Split(',') into parts
				where parts.Length > 2 && int.TryParse(parts[2], out var _)
				select int.Parse(parts[2])).ToList();
			if (elapsedTimes.Count > 0)
			{
				int totalElapsedTime = elapsedTimes.Sum();
				double averageElapsedTime = elapsedTimes.Average();
				using StreamWriter sw = new StreamWriter(filename, append: true, Encoding.UTF8);
				sw.WriteLine();
				sw.WriteLine($",평균시간(초),{averageElapsedTime:F1},");
				sw.WriteLine($",누적시간(초),{totalElapsedTime},");
				sw.WriteLine(",촬영 날짜," + DateTime.Now.ToString("yyyy년MM월dd일") + ",");
				sw.WriteLine(",장비 ID," + ServiceProvider.Settings.CloriumID + ",");
			}
			else
			{
				System.Windows.MessageBox.Show("\ud83d\udea8 통계를 계산할 데이터가 없습니다.");
			}
			System.Windows.MessageBox.Show("\ud83d\udc4d\ud83c\udffb 오늘도 많은 작업하시느라 수고하셨습니다. \ud83d\udc4f\ud83c\udffb");
		}
		catch (IOException)
		{
			System.Windows.MessageBox.Show("\ud83d\udea8 csv 파일이 열려있습니다. 닫고 다시 시도해 주세요", "경고", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
	}

	private void OnBarcodeChanged(string code)
	{
		Dispatcher.Invoke(() =>
		{
			BarCodeValue = code;
			BarcodeText.Visibility = Visibility.Visible;
			StartBlink();
		});
	}

	private void StartBlink()
	{
		StopBlink();
		_blink = new Storyboard();
		DoubleAnimation anim = new DoubleAnimation
		{
			From = 1.0,
			To = 0.0,
			Duration = TimeSpan.FromMilliseconds(1000.0),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever
		};
		Storyboard.SetTarget(anim, BarcodeText);
		Storyboard.SetTargetProperty(anim, new PropertyPath("Opacity"));
		_blink.Children.Add(anim);
		_blink.Begin();
	}

	private void StopBlink()
	{
		if (_blink != null)
		{
			_blink.Stop();
			_blink.Remove();
			_blink = null;
		}
		BarcodeText.Opacity = 1.0;
	}

	private async void CaptureOfIcon_Click(object sender, RoutedEventArgs e)
	{
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.DeviceName == "Z-FTP camera")
		{
			System.Windows.MessageBox.Show("⚠\ufe0f FTP Fake Camera는 촬영은 지원하지 않습니다.");
		}
		else
		{
			if (ServiceProvider.Settings.isBtnPressed)
			{
				return;
			}
			if (!ServiceProvider.Settings.ThumbailLoaded)
			{
				System.Windows.MessageBox.Show("썸네일 생성중입니다. 다시 캡쳐 시도하세요.");
				ServiceProvider.Settings.ThumbailLoaded = true;
				return;
			}
			Log.Debug("아이콘 캡쳐 Click");
			ServiceProvider.Settings.FrontBackMode = 0;
			if (ServiceProvider.DeviceManager.SelectedCameraDevice.DeviceName == "Z-App camera")
			{
				SignalRServerHub.SendTakePhoto();
			}
			else
			{
				await CaptureAsync(shouldWait: true);
			}
		}
	}

	private void But_FrontCapture_Click(object sender, RoutedEventArgs e)
	{
		if (IsMusinsaOK())
		{
			if (!isToggleRot)
			{
				CaptureOfFrontView(rotate270: false, removeBG: false);
			}
			else
			{
				CaptureOfFrontView(rotate270: true, removeBG: false);
			}
		}
	}

	private void But_BackCapture_Click(object sender, RoutedEventArgs e)
	{
		if (IsMusinsaOK())
		{
			if (!isToggleRot)
			{
				CaptureOfBackView(rotate270: false, removeBG: false);
			}
			else
			{
				CaptureOfBackView(rotate270: true, removeBG: false);
			}
			StopBlink();
			BarcodeText.Visibility = Visibility.Collapsed;
		}
	}

	private void But_FrontCaptureBG_Click(object sender, RoutedEventArgs e)
	{
		if (IsMusinsaOK())
		{
			if (!isToggleRot)
			{
				CaptureOfFrontView(rotate270: false, removeBG: true);
			}
			else
			{
				CaptureOfFrontView(rotate270: true, removeBG: true);
			}
		}
	}

	private void But_BackCaptureBG_Click(object sender, RoutedEventArgs e)
	{
		if (IsMusinsaOK())
		{
			if (!isToggleRot)
			{
				CaptureOfBackView(rotate270: false, removeBG: true);
			}
			else
			{
				CaptureOfBackView(rotate270: true, removeBG: true);
			}
			StopBlink();
			BarcodeText.Visibility = Visibility.Collapsed;
		}
	}

	private void But_CreatRemoveBG_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Batch Remove Background Click");
		RemoveBackgroundWnd wnd = new RemoveBackgroundWnd();
		wnd.InitDirectory = ServiceProvider.Settings.DefaultSession.Folder;
		if (wnd.ShowDialog() != true)
		{
		}
	}

	private void but_ToggleRot_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main : ToggleRot Click");
		if (but_ToggleRot.Content is PackIcon icon)
		{
			isToggleRot = icon.Kind == PackIconKind.ToggleSwitchOutline;
			icon.Kind = (isToggleRot ? PackIconKind.ToggleSwitch : PackIconKind.ToggleSwitchOutline);
			double angle = (isToggleRot ? 270 : 0);
			icon.RenderTransform = new RotateTransform(angle);
			icon.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_ToggleRotation270", isToggleRot);
		}
	}

	private void Capture()
	{
		if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
		{
			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				System.Windows.MessageBox.Show("⚠\ufe0f 연결된 카메라가 없습니다..");
			});
			return;
		}
		ServiceProvider.Settings.isBtnPressed = true;
		ServiceProvider.Settings.ThumbailLoaded = false;
		try
		{
			if (ServiceProvider.DeviceManager.SelectedCameraDevice.ShutterSpeed != null && ServiceProvider.DeviceManager.SelectedCameraDevice.ShutterSpeed.Value == "Bulb")
			{
				if (ServiceProvider.DeviceManager.SelectedCameraDevice.GetCapability(CapabilityEnum.Bulb))
				{
					ServiceProvider.WindowsManager.ExecuteCommand("BulbWnd_Show", ServiceProvider.DeviceManager.SelectedCameraDevice);
				}
				else
				{
					StaticHelper.Instance.SystemMessage = TranslationStrings.MsgBulbModeNotSupported;
				}
				return;
			}
			if ((DateTime.Now - lastPowerSaveTime).TotalMinutes >= (double)ServiceProvider.Settings.LightPowerSaveTime)
			{
				System.Windows.Application.Current.Dispatcher.Invoke(() =>
				{
					LightControlWnd.Instance.AllLightOn();
					Thread.Sleep(200);
				});
			}
			lastPowerSaveTime = DateTime.Now;
			if (ServiceProvider.Branding.ControlLighting && ServiceProvider.Settings.ModeLighting)
			{
				System.Windows.Application.Current.Dispatcher.Invoke(() =>
				{
					LightControlWnd.Instance.AllLightOn();
					Thread.Sleep(200);
				});
			}
			Thread thread = new Thread((ThreadStart)CameraHelper.Capture);
			thread.Start();
		}
		catch (Exception ex)
		{
			ServiceProvider.Settings.isBtnPressed = false;
			StaticHelper.Instance.SystemMessage = ex.Message;
			Log.Error("Error : Capture()", ex);
		}
	}

	private void ResetFailedCaptureState()
	{
		// Restore the purpose before re-enabling capture after a start failure.
		ServiceProvider.Settings.FrontBackMode = 0;
		ServiceProvider.Settings.GetMeasurePicture = false;
		ServiceProvider.Settings.Background = false;
		is270CaptureMode = false;
		isCalibrationMode = false;
		ServiceProvider.Settings.ThumbailLoaded = true;
		ServiceProvider.Settings.isBtnPressed = false;
	}

	private async Task<bool> CaptureAsync(bool shouldWait)
	{
		if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
		{
			ResetFailedCaptureState();
			System.Windows.MessageBox.Show("⚠\ufe0f 연결된 카메라가 없습니다..");
			return false;
		}
		ServiceProvider.Settings.isBtnPressed = true;
		ServiceProvider.Settings.ThumbailLoaded = false;
		try
		{
			if ((DateTime.Now - lastPowerSaveTime).TotalMinutes >= (double)ServiceProvider.Settings.LightPowerSaveTime || (ServiceProvider.Branding.ControlLighting && ServiceProvider.Settings.ModeLighting))
			{
				LightControlWnd.Instance.AllLightOn();
				Thread.Sleep(200);
			}
			lastPowerSaveTime = DateTime.Now;
			if (!await CameraHelper.CaptureAsync())
			{
				ResetFailedCaptureState();
				return false;
			}
			if (!shouldWait)
			{
				return true;
			}
			return await WaitForCaptureComplete(30);
		}
		catch (Exception ex)
		{
			ResetFailedCaptureState();
			StaticHelper.Instance.SystemMessage = ex.Message;
			Log.Error("Error : Capture()", ex);
			return false;
		}
	}

	private async Task<bool> WaitForCaptureComplete(int maxRetries)
	{
		int timer = 0;
		while (ServiceProvider.Settings.isBtnPressed)
		{
			if (timer++ > maxRetries)
			{
				return false;
			}
			await Task.Delay(100);
		}
		return true;
	}

	private void CaptureOfAndroid()
	{
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.DeviceName == "Z-App camera")
		{
			SignalRServerHub.SendTakePhoto();
		}
	}

	private void VideoToggleOfAndroid()
	{
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.DeviceName == "Z-App camera")
		{
			SignalRServerHub.SendToggleVideo();
		}
	}

	private async void CaptureOfBarCodeReader(string Barcode)
	{
		Log.Debug("바코드 리더기 Click [바코드 : " + Barcode + "]");
		if (ServiceProvider.Settings.UseBarcodeScanner && ServiceProvider.Settings.ModeBarcodeSession)
		{
			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				MainMenuViewModel.Instance.CreateNewSessionFolder(Barcode);
			});
		}
		if (ServiceProvider.Settings.UseBarcodeScanner && ServiceProvider.Settings.ModeBarcodeCapture && !ServiceProvider.Settings.isBtnPressed && ServiceProvider.Settings.ThumbailLoaded)
		{
			await CaptureAsync(shouldWait: true);
		}
	}

	private async void CaptureOfFrontView(bool rotate270, bool removeBG)
	{
		if (ServiceProvider.Settings.isBtnPressed || !ServiceProvider.Settings.ThumbailLoaded)
		{
			return;
		}
		Log.Debug("전면 촬영 Click : " + (rotate270 ? "270 회전" : "무 회전"));
		is270CaptureMode = rotate270;
		setExifOrientation(rotate270);
		if (ServiceProvider.Settings.IsDefect)
		{
			ServiceProvider.Settings.FrontBackMode = 0;
			ServiceProvider.Settings.GetMeasurePicture = false;
			ServiceProvider.Settings.Background = false;
			await CaptureAsync(shouldWait: true);
			return;
		}
		ServiceProvider.Settings.FrontBackMode = 1;
		ServiceProvider.Settings.GetMeasurePicture = true;
		if (!removeBG)
		{
			if (!await CaptureAsync(shouldWait: true))
			{
				return;
			}
		}
		else
		{
			if (!await CaptureBackground())
			{
				return;
			}
		}
		MainMenuViewModel.IsMusinsaFrontOK = true;
		ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_OverlayActive", true);
		ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_OverlayLastCapture", true);
	}

	private async void CaptureOfBackView(bool rotate270, bool removeBG)
	{
		if (!ServiceProvider.Settings.isBtnPressed && ServiceProvider.Settings.ThumbailLoaded)
		{
			Log.Debug("후면 촬영 Click : " + (rotate270 ? "270 회전" : "무 회전"));
			is270CaptureMode = rotate270;
			setExifOrientation(rotate270);
			ServiceProvider.Settings.FrontBackMode = 2;
			if (!removeBG)
			{
				if (!await CaptureAsync(shouldWait: true))
				{
					return;
				}
			}
			else
			{
				if (!await CaptureBackground())
				{
					return;
				}
			}
			MainMenuViewModel.IsMusinsaBackOK = true;
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_OverlayActive", false);
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_OverlayLastCapture", false);
		}
	}

	private async Task<bool> CaptureBackground()
	{
		ServiceProvider.Settings.Background = false;
		if (!(await CaptureAsync(shouldWait: true)))
		{
			System.Windows.MessageBox.Show("\ud83d\udea8조명[ON] 타임오버", "경고", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return false;
		}
		if (ModeA.IsChecked == false)
		{
			ServiceProvider.Settings.Background = true;
			Backgroud_LightOff();
			await Task.Delay(300);
			bool success = await CaptureAsync(shouldWait: true);
			Backgroud_LightOn();
			if (!success)
			{
				System.Windows.MessageBox.Show("\ud83d\udea8조명[OFF] 타임오버", "경고", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return false;
			}
		}
		if (Bg_Auto.IsChecked == false)
		{
			return true;
		}
		string filename = ServiceProvider.DeviceManager.LastCapturedImage[ServiceProvider.DeviceManager.SelectedCameraDevice];
		if (filename != null)
		{
			RemoveBackgroundWnd wnd = new RemoveBackgroundWnd();
			if (ModeA.IsChecked == true)
			{
				await wnd.PrecessingOneImage(filename);
			}
			else
			{
				await wnd.PrecessingTwoImages(filename);
			}
		}
		return true;
	}

	private void Backgroud_LightOn()
	{
	}

	private void Backgroud_LightOff()
	{
	}

	private void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
	{
		if (G2FocusCamera.TryHandle(eventArgs)) return;
		if (ServiceProvider.Settings.UseParallelTransfer)
		{
			PhotoCaptured(eventArgs);
			return;
		}
		lock (_locker)
		{
			PhotoCaptured(eventArgs);
		}
	}

	// G2 UI: the main menu (파일 · 화면 · 세션 · 카메라 …) opens from the top-left logo.
	private void G2LogoMenu_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement fe && fe.ContextMenu != null)
		{
			fe.ContextMenu.PlacementTarget = fe;
			fe.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
			fe.ContextMenu.IsOpen = true;
		}
	}

	// G2 UI: 파일 > 열기 — open the current session folder in Windows Explorer.
	private void G2OpenSessionFolder_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			string folder = ServiceProvider.Settings?.DefaultSession?.Folder;
			if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
			{
				System.Windows.MessageBox.Show("현재 세션 폴더를 찾을 수 없습니다.\n" + folder, "열기");
				return;
			}
			Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"") { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			Log.Error("G2 open session folder", ex);
		}
	}

	// G2 UI: 파일 > 다른 이름으로 저장 — copy the selected (already saved) photo to a new name. The original file is left untouched.
	private void G2SaveAs_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			FileItem item = ServiceProvider.Settings?.SelectedBitmap?.FileItem;
			if (item == null || string.IsNullOrEmpty(item.FileName) || !File.Exists(item.FileName))
			{
				System.Windows.MessageBox.Show("저장할 사진을 먼저 선택해 주세요.", "다른 이름으로 저장");
				return;
			}
			string ext = Path.GetExtension(item.FileName);
			var dialog = new Microsoft.Win32.SaveFileDialog
			{
				Title = "다른 이름으로 저장",
				InitialDirectory = Path.GetDirectoryName(item.FileName),
				FileName = Path.GetFileNameWithoutExtension(item.FileName) + "_copy" + ext,
				DefaultExt = ext,
				Filter = "사진 (*" + ext + ")|*" + ext + "|모든 파일 (*.*)|*.*",
				OverwritePrompt = true,
				AddExtension = true
			};
			if (dialog.ShowDialog(this) != true)
			{
				return;
			}
			if (string.Equals(Path.GetFullPath(dialog.FileName), Path.GetFullPath(item.FileName), StringComparison.OrdinalIgnoreCase))
			{
				System.Windows.MessageBox.Show("원본과 다른 이름을 지정해 주세요.", "다른 이름으로 저장");
				return;
			}
			File.Copy(item.FileName, dialog.FileName, overwrite: true);
		}
		catch (Exception ex)
		{
			Log.Error("G2 save as", ex);
			System.Windows.MessageBox.Show("저장하지 못했습니다.\n" + ex.Message, "다른 이름으로 저장");
		}
	}

	// G2 UI: floating camera-settings panel — close button and drag-to-move header.
	private void G2CloseCameraPanel_Click(object sender, RoutedEventArgs e)
	{
		G2CamToggle.IsChecked = false;
	}

	private void G2CamPanel_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
	{
		double maxX = Math.Max(0, (G2CamPanel.Parent as FrameworkElement)?.ActualWidth - G2CamPanel.ActualWidth - 32 ?? 0);
		double maxY = Math.Max(0, (G2CamPanel.Parent as FrameworkElement)?.ActualHeight - 80 ?? 0);
		G2CamPanelOffset.X = Math.Min(0, Math.Max(-maxX, G2CamPanelOffset.X + e.HorizontalChange));
		G2CamPanelOffset.Y = Math.Max(0, Math.Min(maxY, G2CamPanelOffset.Y + e.VerticalChange));
	}

	// G2 UI: close the "add auto-export plugin" list after a plugin is picked.
	private void G2ClosePluginPopup_Click(object sender, RoutedEventArgs e)
	{
		DownButton.IsChecked = false;
	}

	private void OpenManagedLicense_Click(object sender, RoutedEventArgs e)
	{
		ManagedLicenseRuntime.Show();
	}

	private void OpenSuperfocus_Click(object sender, RoutedEventArgs e)
	{
		var camera = ServiceProvider.DeviceManager.SelectedCameraDevice;
		if (camera == null) { System.Windows.MessageBox.Show("카메라를 연결해 주세요.", "수퍼포커스"); return; }
		Window owner = System.Windows.Application.Current.Windows.OfType<LiveViewWnd>().FirstOrDefault(w => ReferenceEquals(w.SelectedPortableDevice, camera));
		if (owner == null || !owner.IsVisible) {
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Show", camera);
			owner = System.Windows.Application.Current.Windows.OfType<LiveViewWnd>().FirstOrDefault(w => ReferenceEquals(w.SelectedPortableDevice, camera));
		}
		if (owner is LiveViewWnd live) { live.ShowSuperfocus(); return; }
		var window = SuperfocusWindow.For(camera, owner ?? this);
		window.Show(); window.Activate();
	}

	private void PhotoCaptured(object o)
	{
		PhotoCapturedEventArgs eventArgs = o as PhotoCapturedEventArgs;
		if (eventArgs == null)
		{
			return;
		}
		try
		{
			CaptureCount++;
			Log.Debug("[" + CaptureCount + "] ===== 캡쳐 이벤트 시작 =====");
			eventArgs.CameraDevice.IsBusy = true;
			string extension = Path.GetExtension(eventArgs.FileName);
			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				if (ServiceProvider.Branding.ControlLighting && ServiceProvider.Settings.ModeLighting)
				{
					LightControlWnd.Instance.AllLightOff();
				}
			});
			if (LiveViewManager.PreviewRequest.ContainsKey(eventArgs.CameraDevice) && LiveViewManager.PreviewRequest[eventArgs.CameraDevice])
			{
				LiveViewManager.PreviewRequest[eventArgs.CameraDevice] = false;
				string file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + extension);
				eventArgs.CameraDevice.TransferFile(eventArgs.Handle, file);
				eventArgs.CameraDevice.TransferProgress = 0u;
				eventArgs.CameraDevice.ReleaseResurce(eventArgs.Handle);
				LiveViewManager.Preview[eventArgs.CameraDevice] = file;
				LiveViewManager.OnPreviewCaptured(eventArgs.CameraDevice, file);
				return;
			}
			CameraProperty property = eventArgs.CameraDevice.LoadProperties();
			PhotoSession session = ServiceProvider.Settings.DefaultSession;
			StaticHelper.Instance.SystemMessage = "";
			if (!eventArgs.CameraDevice.CaptureInSdRam || PhotoUtils.IsMovie(eventArgs.FileName))
			{
				if (property.NoDownload)
				{
					eventArgs.CameraDevice.ReleaseResurce(eventArgs.Handle);
					StaticHelper.Instance.SystemMessage = "File transfer disabled";
					return;
				}
				if (extension != null && session.DownloadOnlyJpg && extension.ToLower() != ".jpg")
				{
					eventArgs.CameraDevice.ReleaseResurce(eventArgs.Handle);
					return;
				}
			}
			StaticHelper.Instance.SystemMessage = TranslationStrings.MsgPhotoTransferBegin;
			string tempFile = Path.GetTempFileName();
			if (File.Exists(tempFile))
			{
				File.Delete(tempFile);
			}
			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();
			if (!eventArgs.CameraDevice.CaptureInSdRam && session.DownloadThumbOnly)
			{
				eventArgs.CameraDevice.TransferFileThumb(eventArgs.Handle, tempFile);
			}
			else
			{
				eventArgs.CameraDevice.TransferFile(eventArgs.Handle, tempFile);
			}
			eventArgs.CameraDevice.TransferProgress = 0u;
			stopWatch.Stop();
			string strTransfer = "Transfer time : " + stopWatch.Elapsed.TotalSeconds.ToString("##.###") + "sec     Speed : " + Math.Round((double)new System.IO.FileInfo(tempFile).Length / 1024.0 / 1024.0 / stopWatch.Elapsed.TotalSeconds, 2) + " Mb/s";
			if (ServiceProvider.Settings.UsePlaySound)
			{
				PhotoUtils.PlayCaptureSound();
			}
			Log.Debug(strTransfer);
			if (isCalibrationMode)
			{
				File.Copy(tempFile, filename_Calibration, overwrite: true);
				if (File.Exists(tempFile))
				{
					PhotoUtils.WaitForFile(tempFile);
					File.Delete(tempFile);
				}
				Log.Debug("캘리브레이션 촬영");
				return;
			}
			if (ServiceProvider.Branding.ControlMultiCamera)
			{
				string author = GetMetadataAuthor(tempFile);
				if (author == "")
				{
					ServiceProvider.Settings.CameraAlias = FindAlias(eventArgs.CameraDevice.SerialNumber);
				}
				else
				{
					ServiceProvider.Settings.CameraAlias = "_" + author;
				}
				Log.Debug("카메라 별명 : " + ServiceProvider.Settings.CameraAlias);
			}
			string fileName = "";
			if (!session.UseOriginalFilename || eventArgs.CameraDevice.CaptureInSdRam)
			{
				fileName = session.GetNextFileName(eventArgs.FileName, eventArgs.CameraDevice, tempFile);
			}
			else
			{
				fileName = Path.Combine(session.Folder, eventArgs.FileName);
				if (File.Exists(fileName) && !session.AllowOverWrite)
				{
					fileName = StaticHelper.GetUniqueFilename(Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_", 0, Path.GetExtension(fileName));
				}
			}
			if (session.AllowOverWrite && File.Exists(fileName))
			{
				PhotoUtils.WaitForFile(fileName);
				File.Delete(fileName);
			}
			if (session.LowerCaseExtension && !string.IsNullOrEmpty(Path.GetExtension(fileName)))
			{
				fileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + Path.GetExtension(fileName).ToLower());
			}
			if (session.AskSavePath)
			{
				Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
				dialog.Filter = "All files|*.*";
				dialog.Title = "Save captured photo";
				dialog.FileName = fileName;
				dialog.InitialDirectory = Path.GetDirectoryName(fileName);
				if (dialog.ShowDialog() != true)
				{
					if (File.Exists(tempFile))
					{
						File.Delete(tempFile);
					}
					return;
				}
				fileName = dialog.FileName;
			}
			if (!Directory.Exists(Path.GetDirectoryName(fileName)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(fileName));
			}
			string backupfile = null;
			if (session.BackUp)
			{
				backupfile = session.CopyBackUp(tempFile, fileName);
				if (string.IsNullOrEmpty(backupfile))
				{
					StaticHelper.Instance.SystemMessage = "Unable to save the backup";
				}
			}
			if (ServiceProvider.Settings.DefaultSession.AutoExportPluginConfigs.Count((AutoExportPluginConfig x) => !x.RunAfterTransfer) > 0)
			{
				FileItem tempitem = new FileItem(tempFile);
				tempitem.Name = Path.GetFileName(fileName);
				tempitem.BackupFileName = backupfile;
				tempitem.Series = session.Series;
				tempitem.AddTemplates(eventArgs.CameraDevice, session);
				ExecuteAutoexportPlugins(eventArgs.CameraDevice, tempitem, runOnTransfered: false);
			}
			if ((!eventArgs.CameraDevice.CaptureInSdRam || PhotoUtils.IsMovie(fileName)) && session.DeleteFileAfterTransfer)
			{
				eventArgs.CameraDevice.DeleteObject(new DeviceObject
				{
					Handle = eventArgs.Handle
				});
			}
			if (extension.ToLower() != ".mp4" && extension.ToLower() != ".mov")
			{
				if (is270CaptureMode)
				{
					SetExifOrientation(tempFile, Exif_Rotate);
				}
				else
				{
					switch (ServiceProvider.Settings.RotateIndex)
					{
					case 0:
						Exif_Rotate = 1;
						break;
					case 1:
						Exif_Rotate = 6;
						break;
					case 2:
						Exif_Rotate = 3;
						break;
					case 3:
						Exif_Rotate = 8;
						break;
					}
					if (ServiceProvider.Settings.RotateIndex != 0)
					{
						SetExifOrientation(tempFile, Exif_Rotate);
					}
				}
				ImageProcess(tempFile, fileName);
			}
			else
			{
				File.Copy(tempFile, fileName);
			}
			if (File.Exists(tempFile))
			{
				PhotoUtils.WaitForFile(tempFile);
				File.Delete(tempFile);
			}
			Log.Debug("File name : " + fileName);
			if (ServiceProvider.Settings.Background)
			{
				return;
			}
			if (session.WriteComment)
			{
				if (!string.IsNullOrEmpty(session.Comment))
				{
					Exiv2Helper.SaveComment(fileName, session.Comment);
				}
				if (session.SelectedTag1 != null && !string.IsNullOrEmpty(session.SelectedTag1.Value))
				{
					Exiv2Helper.AddKeyword(fileName, session.SelectedTag1.Value);
				}
				if (session.SelectedTag2 != null && !string.IsNullOrEmpty(session.SelectedTag2.Value))
				{
					Exiv2Helper.AddKeyword(fileName, session.SelectedTag2.Value);
				}
				if (session.SelectedTag3 != null && !string.IsNullOrEmpty(session.SelectedTag3.Value))
				{
					Exiv2Helper.AddKeyword(fileName, session.SelectedTag3.Value);
				}
				if (session.SelectedTag4 != null && !string.IsNullOrEmpty(session.SelectedTag4.Value))
				{
					Exiv2Helper.AddKeyword(fileName, session.SelectedTag4.Value);
				}
			}
			if (session.ExternalData != null)
			{
				session.ExternalData.FileName = fileName;
			}
			if (ServiceProvider.Settings.SkipAddingFileToSession)
			{
				return;
			}
			Dispatcher.Invoke(() =>
			{
				try
				{
					_selectedItem = session.GetNewFileItem(fileName);
					_selectedItem.BackupFileName = backupfile;
					_selectedItem.Series = session.Series;
					_selectedItem.AddTemplates(eventArgs.CameraDevice, session);
					if (ServiceProvider.Settings.UseDatabase)
					{
						ServiceProvider.Database.Add(new DbFile(_selectedItem, eventArgs.CameraDevice.SerialNumber, eventArgs.CameraDevice.DisplayName, session.Name));
					}
				}
				catch (Exception exception2)
				{
					Log.Error("Unable to add file to database", exception2);
				}
			});
			ExecuteAutoexportPlugins(eventArgs.CameraDevice, _selectedItem, runOnTransfered: true);
			Dispatcher.Invoke(() =>
			{
				_selectedItem.RemoveThumbs();
				session.Add(_selectedItem);
				ServiceProvider.OnFileTransfered(_selectedItem);
			});
			if (ServiceProvider.Settings.MinimizeToTrayIcon && !IsVisible && !ServiceProvider.Settings.HideTrayNotifications)
			{
				MyNotifyIcon.HideBalloonTip();
				MyNotifyIcon.ShowBalloonTip("Photo transfered", fileName, BalloonIcon.Info);
			}
			ServiceProvider.DeviceManager.LastCapturedImage[eventArgs.CameraDevice] = fileName;
			Log.Debug("Thumbnail count : " + session.FilesCount);
			if (ServiceProvider.Settings.AutoPreview && !ServiceProvider.WindowsManager.Get(typeof(MultipleCameraWnd)).IsVisible && !ServiceProvider.Settings.UseExternalViewer && ((Path.GetExtension(fileName).ToLower() == ".jpg" && ServiceProvider.Settings.AutoPreviewJpgOnly) || !ServiceProvider.Settings.AutoPreviewJpgOnly))
			{
				if ((DateTime.Now - _lastLoadTime).TotalSeconds < 4.0)
				{
					_selectiontimer.Stop();
					_selectiontimer.Start();
				}
				else
				{
					ServiceProvider.WindowsManager.ExecuteCommand("Select_Image", _selectedItem);
				}
			}
			_lastLoadTime = DateTime.Now;
			StaticHelper.Instance.SystemMessage = TranslationStrings.MsgPhotoTransferDone + " " + strTransfer;
			if (ServiceProvider.Settings.UseExternalViewer && File.Exists(ServiceProvider.Settings.ExternalViewerPath))
			{
				string arg = ServiceProvider.Settings.ExternalViewerArgs;
				arg = (arg.Contains("%1") ? arg.Replace("%1", fileName) : (arg + " " + fileName));
				PhotoUtils.Run(ServiceProvider.Settings.ExternalViewerPath, arg, ProcessWindowStyle.Normal);
			}
			eventArgs.CameraDevice.ReleaseResurce(eventArgs.Handle);
			if (ServiceProvider.Settings.Preview && !ServiceProvider.WindowsManager.Get(typeof(MultipleCameraWnd)).IsVisible && !ServiceProvider.Settings.UseExternalViewer)
			{
				ServiceProvider.WindowsManager.ExecuteCommand("FullScreenWnd_ShowTimed");
			}
			Log.Debug("     --------- 캡쳐 이벤트 완료 ---------", ServiceProvider.Settings.UseDebugLog);
		}
		catch (Exception exception)
		{
			ServiceProvider.Settings.ThumbailLoaded = true;
			PlayWave("C:\\Windows\\Media\\Windows Notify Messaging.wav");
			Log.Error("다운로드 실패", exception);
			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				System.Windows.MessageBox.Show("\ud83d\udea8다운로드하지 못했습니다.\ud83d\udea8\r\n다시 촬영해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			});
		}
		finally
		{
			eventArgs.CameraDevice.IsBusy = false;
			ServiceProvider.Settings.GetMeasurePicture = false;
			ServiceProvider.Settings.FrontBackMode = 0;
			is270CaptureMode = false;
			isCalibrationMode = false;
			ServiceProvider.Settings.Background = false;
			ServiceProvider.Settings.IsDefect = false;
			ServiceProvider.Settings.isBtnPressed = false;
		}
	}

	private void ImageProcess(string inputfilename, string outfilename)
	{
		try
		{
			string pathWithoutExtension = Path.Combine(Path.GetDirectoryName(inputfilename), Path.GetFileNameWithoutExtension(inputfilename));
			string ext = Path.GetExtension(inputfilename);
			string resolutionOutFile = pathWithoutExtension + "Res" + ext;
			string[] array = new string[1] { resolutionOutFile };
			foreach (string file in array)
			{
				if (File.Exists(file))
				{
					File.Delete(file);
				}
			}
			string qualityInputFile = inputfilename;
			if (ServiceProvider.Settings.ResolutionChange)
			{
				int width = ServiceProvider.Settings.ResolutionPixelWidth;
				int height = ServiceProvider.Settings.ResolutionPixelHeight;
				ImageResolution(inputfilename, resolutionOutFile, width, height);
				qualityInputFile = resolutionOutFile;
			}
			File.Copy(qualityInputFile, outfilename, overwrite: true);
			string metafilename = string.Empty;
			if (ServiceProvider.Settings.GetMeasurePicture)
			{
				metafilename = WriteMetadata(outfilename, ServiceProvider.Settings.CloriumID, ServiceProvider.Settings.MetaScale, ServiceProvider.Settings.APIHost);
			}
			if (ServiceProvider.Settings.UseAlwaysBackup)
			{
				Backup(outfilename);
			}
			if (ServiceProvider.Settings.UseAlwaysBackup && ServiceProvider.Settings.GetMeasurePicture)
			{
				Backup(metafilename);
			}
			string[] array2 = new string[1] { resolutionOutFile };
			foreach (string file2 in array2)
			{
				if (File.Exists(file2))
				{
					File.Delete(file2);
				}
			}
		}
		catch (Exception innerException)
		{
			throw new Exception("이미지 처리 에러 발생", innerException);
		}
	}

	private void SetExifOrientation(string jpgPath, ushort orientationValue)
	{
		string tempFile = jpgPath + ".tmp";
		using (FileStream fs = new FileStream(jpgPath, FileMode.Open, FileAccess.Read))
		{
			using Image img = Image.FromStream(fs, useEmbeddedColorManagement: true, validateImageData: false);
			PropertyItem prop = img.PropertyItems.FirstOrDefault((PropertyItem p) => p.Id == 274);
			if (prop == null)
			{
				if (img.PropertyItems.Length == 0)
				{
					throw new InvalidOperationException("이미지에 PropertyItems가 없습니다.");
				}
				prop = img.PropertyItems[0];
				prop.Id = 274;
				prop.Type = 3;
				prop.Len = 2;
				prop.Value = new byte[2];
			}
			prop.Value[0] = (byte)(orientationValue & 0xFF);
			prop.Value[1] = (byte)((orientationValue >> 8) & 0xFF);
			img.SetPropertyItem(prop);
			img.Save(tempFile, ImageFormat.Jpeg);
		}
		File.Delete(jpgPath);
		File.Move(tempFile, jpgPath);
	}

	private ushort GetExifOrientation(string jpgPath)
	{
		using (Image img = Image.FromFile(jpgPath))
		{
			PropertyItem prop = img.PropertyItems.FirstOrDefault((PropertyItem p) => p.Id == 274);
			if (prop != null && prop.Value.Length >= 2)
			{
				return BitConverter.ToUInt16(prop.Value, 0);
			}
		}
		return 1;
	}

	private void ImageRotate(string filename, string outname, int angle_Index)
	{
		RotateFlipType angle = RotateFlipType.RotateNoneFlipNone;
		switch (angle_Index)
		{
		case 0:
			angle = RotateFlipType.RotateNoneFlipNone;
			break;
		case 1:
			angle = RotateFlipType.Rotate90FlipNone;
			break;
		case 2:
			angle = RotateFlipType.Rotate180FlipNone;
			break;
		case 3:
			angle = RotateFlipType.Rotate270FlipNone;
			break;
		}
		try
		{
			PhotoUtils.WaitForFile(filename);
			if (!File.Exists(filename))
			{
				System.Windows.MessageBox.Show("파일이 존재하지 않습니다.\r\n" + filename, "파일 오류", MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			using Bitmap bitmap = new Bitmap(filename);
			if (angle_Index == 0)
			{
				File.Copy(filename, outname, overwrite: true);
				return;
			}
			bitmap.RotateFlip(angle);
			bitmap.Save(outname, ImageFormat.Jpeg);
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	private void ImageResolution(string filename, string outname, int pixelWidth, int pixelHeight)
	{
		try
		{
			using Bitmap bitmap = new Bitmap(filename);
			int originalWidth = bitmap.Width;
			int originalHeight = bitmap.Height;
			double targetRatio = (double)pixelWidth / (double)pixelHeight;
			int targetWidth = originalWidth;
			int targetHeight = (int)((double)originalWidth / targetRatio);
			if (targetHeight > originalHeight)
			{
				targetHeight = originalHeight;
				targetWidth = (int)((double)originalHeight * targetRatio);
			}
			int x = (originalWidth - targetWidth) / 2;
			int y = (originalHeight - targetHeight) / 2;
			Rectangle cropRect = new Rectangle(x, y, targetWidth, targetHeight);
			using Bitmap croppedImage = bitmap.Clone(cropRect, bitmap.PixelFormat);
			using Bitmap resizedBitmap = new Bitmap(croppedImage, pixelWidth, pixelHeight);
			resizedBitmap.SetResolution(resizedBitmap.HorizontalResolution, resizedBitmap.VerticalResolution);
			PropertyItem[] propertyItems = bitmap.PropertyItems;
			foreach (PropertyItem prop in propertyItems)
			{
				resizedBitmap.SetPropertyItem(prop);
			}
			resizedBitmap.Save(outname, ImageFormat.Jpeg);
		}
		catch (Exception ex)
		{
			Log.Error(ex);
			string errorMessage = $"ImageResolution: 이미지 비율 변경 중 오류 발생 ({pixelWidth} X {pixelHeight}){ex.Message}";
			throw new Exception(errorMessage, ex);
		}
	}

	private string WriteMetadata(string fullpathfilename, string cloriumID, string scale, string host)
	{
		if (scale == null)
		{
			scale = "0";
		}
		try
		{
			BitmapFrame bitmapFrame;
			using (Stream filestream = File.Open(fullpathfilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				BitmapDecoder decoder = BitmapDecoder.Create(filestream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
				bitmapFrame = decoder.Frames[0];
			}
			if (!(bitmapFrame.Metadata is BitmapMetadata metadata))
			{
				return null;
			}
			BitmapMetadata metadata2 = metadata.Clone();
			metadata2.Title = cloriumID;
			metadata2.Subject = scale;
			metadata2.Comment = host;
			string measurePath = Path.Combine(Path.GetDirectoryName(fullpathfilename), "치수측정");
			if (!Directory.Exists(measurePath))
			{
				Directory.CreateDirectory(measurePath);
			}
			string metapathfile = Path.Combine(measurePath, System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(fullpathfilename), "_PR(?=_[FB]$)", "") + "_M.jpeg");
			using (Stream outputFile = File.Open(metapathfile, FileMode.Create, FileAccess.Write))
			{
				JpegBitmapEncoder encoder = new JpegBitmapEncoder
				{
					QualityLevel = 30
				};
				encoder.Frames.Add(BitmapFrame.Create(bitmapFrame, bitmapFrame.Thumbnail, metadata2, bitmapFrame.ColorContexts));
				encoder.Save(outputFile);
			}
			return metapathfile;
		}
		catch (Exception message)
		{
			Log.Error(message);
			throw;
		}
	}

	private string GetMetadataAuthor(string filename)
	{
		string extension = Path.GetExtension(filename).ToLower();
		if (extension == ".mp4" || extension == ".mov" || extension == ".avi")
		{
			return GetVideoAuthor(filename);
		}
		try
		{
			using Stream filestream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			BitmapDecoder decoder = BitmapDecoder.Create(filestream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
			BitmapFrame bitmapFrame = decoder.Frames[0];
			if (!(bitmapFrame.Metadata is BitmapMetadata metadata))
			{
				return "";
			}
			if (metadata.Author != null && metadata.Author.Count > 0)
			{
				return metadata.Author[0] ?? "";
			}
			return "";
		}
		catch (Exception ex)
		{
			Log.Error("이미지 메타데이터 읽기 실패: " + ex.Message);
			return "";
		}
	}

	private string GetVideoAuthor(string filename)
	{
		try
		{
			Type shellType = Type.GetTypeFromProgID("Shell.Application");
			dynamic shell = Activator.CreateInstance(shellType);
			dynamic folder = shell.NameSpace(Path.GetDirectoryName(filename));
			dynamic file = folder.ParseName(Path.GetFileName(filename));
			return folder.GetDetailsOf(file, 20);
		}
		catch (Exception ex)
		{
			Log.Error("영상 메타데이터 읽기 실패: " + ex.Message);
			return "";
		}
	}

	private void Backup(string filename)
	{
		try
		{
			string directory = Path.GetDirectoryName(filename);
			string tempName = Path.GetFileName(filename);
			string topFolder = directory.Split(Path.DirectorySeparatorChar)[1];
			string oldFolderName = new DirectoryInfo(topFolder).Name;
			string newFolderName = oldFolderName + " Backup";
			string backupDirectory = directory.Replace(topFolder, newFolderName);
			string backupfileName = Path.Combine(backupDirectory, tempName);
			backupfileName = "D:" + backupfileName.Substring(2);
			if (!Directory.Exists(Path.GetDirectoryName(backupfileName)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(backupfileName));
			}
			if (File.Exists(backupfileName))
			{
				File.Delete(backupfileName);
			}
			File.Copy(filename, backupfileName);
		}
		catch (Exception ex)
		{
			Log.Error(ex);
			string errorMessage = "파일 백업 실패 " + filename + ex.Message;
			throw new Exception(errorMessage, ex);
		}
	}

	private void SetColorTemperature(long val)
	{
		ServiceProvider.DeviceManager.SelectedCameraDevice.SetColorTemperature(val);
	}

	private long GetColorTemperature()
	{
		return ServiceProvider.DeviceManager.SelectedCameraDevice.GetColorTemperature();
	}

	private void NextColorTemperature()
	{
		ServiceProvider.DeviceManager.SelectedCameraDevice.ColorTemperature?.NextValue();
	}

	private void PrevColorTemperature()
	{
		ServiceProvider.DeviceManager.SelectedCameraDevice.ColorTemperature?.PrevValue();
	}

	private bool checkStatus()
	{
		if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsModeColorTemperature())
		{
			System.Windows.MessageBox.Show("⚠\ufe0f ColorTemperature 모드가 아닙니다.");
			return false;
		}
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
		{
			System.Windows.MessageBox.Show("⚠\ufe0f 색보정 슬라이더 동작시 카메라가 Busy 상태입니다.");
			return false;
		}
		return true;
	}

	private void btn_sliderLeft_Click(object sender, RoutedEventArgs e)
	{
		if (checkStatus())
		{
			PrevColorTemperature();
			long val = long.Parse(ServiceProvider.DeviceManager.SelectedCameraDevice.ColorTemperature.Value);
			UpdateSliderValue(val);
		}
	}

	private void btn_sliderRight_Click(object sender, RoutedEventArgs e)
	{
		if (checkStatus())
		{
			NextColorTemperature();
			long val = long.Parse(ServiceProvider.DeviceManager.SelectedCameraDevice.ColorTemperature.Value);
			UpdateSliderValue(val);
		}
	}

	private void ColorTemperature_SlideValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
	{
		if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected || ServiceProvider.DeviceManager.SelectedCameraDevice.DeviceName == null || ServiceProvider.DeviceManager.SelectedCameraDevice.DeviceName == "Z-App camera" || ServiceProvider.DeviceManager.SelectedCameraDevice.DeviceName == "Z-FTP camera" || ServiceProvider.DeviceManager.SelectedCameraDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName || _isInternalChange)
		{
			return;
		}
		if (isReadOnly)
		{
			isReadOnly = false;
			return;
		}
		if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsModeColorTemperature())
		{
			_isInternalChange = true;
			slider.Value = e.OldValue;
			_isInternalChange = false;
			System.Windows.MessageBox.Show("⚠\ufe0f 색온도 모드가 아닙니다.");
			return;
		}
		DateTime currentTime = DateTime.Now;
		if (!((currentTime - lastEventTime).TotalMilliseconds < 200.0))
		{
			lastEventTime = currentTime;
			long val = (long)e.NewValue;
			SetColorTemperature(val);
		}
	}

	private void UpdateSliderValue(long val)
	{
		isReadOnly = true;
		slider.Value = val;
	}

	private void Slider_MouseUp(object sender, MouseButtonEventArgs e)
	{
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsModeColorTemperature())
		{
			UpdateSliderValue(GetColorTemperature());
			Log.Debug("Set ColorTemperature(Mouse Up) : " + slider.Value);
		}
	}

	private void SetAperture(long val)
	{
		ServiceProvider.DeviceManager.SelectedCameraDevice.FNumber.SetValue(val.ToString());
	}

	private long GetAperture()
	{
		ServiceProvider.DeviceManager.SelectedCameraDevice.FNumber.GetValues();
		return long.Parse(ServiceProvider.DeviceManager.SelectedCameraDevice.FNumber.Value);
	}

	private void NextAperture()
	{
		ServiceProvider.DeviceManager.SelectedCameraDevice.FNumber?.NextValue();
	}

	private void PrevAperture()
	{
		ServiceProvider.DeviceManager.SelectedCameraDevice.FNumber?.PrevValue();
	}

	private void btn_ApertureLeft_Click(object sender, RoutedEventArgs e)
	{
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
		{
			Log.Debug("카메라가 Busy 상태입니다.");
		}
		else
		{
			PrevAperture();
		}
	}

	private void btn_ApertureRight_Click(object sender, RoutedEventArgs e)
	{
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
		{
			Log.Debug("카메라가 Busy 상태입니다.");
		}
		else
		{
			NextAperture();
		}
	}

	public async void but_Calibration_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("캘리브레이션 Click");
		isCalibrationMode = true;
		await CaptureAsync(shouldWait: true);
		ServiceProvider.Settings.ThumbailLoaded = true;
		if (File.Exists(filename_Calibration))
		{
			double scale = GetScaleWithSquareROI(filename_Calibration, ServiceProvider.Settings.DiskSize);
			MessageBoxResult ret = System.Windows.MessageBox.Show("스케일 값 : " + scale, "알림", MessageBoxButton.YesNo);
			if (ret == MessageBoxResult.Yes && scale > 0.0)
			{
				ServiceProvider.Settings.MetaScale = scale.ToString();
				Log.Debug("자동 캘리브레이션 실행 : " + ServiceProvider.Settings.MetaScale);
			}
		}
	}

	public unsafe double GetScaleWithSquareROI(string filePath, double realDiameterMm, int blackThreshold = 100)
	{
		BitmapImage bitmap = new BitmapImage();
		bitmap.BeginInit();
		bitmap.UriSource = new Uri(Path.GetFullPath(filePath));
		bitmap.CacheOption = BitmapCacheOption.OnLoad;
		bitmap.EndInit();
		if (bitmap.CanFreeze)
		{
			bitmap.Freeze();
		}
		int width = bitmap.PixelWidth;
		int height = bitmap.PixelHeight;
		FormatConvertedBitmap formatted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0.0);
		int stride = width * 4;
		byte[] pixels = new byte[height * stride];
		formatted.CopyPixels(pixels, stride, 0);
		int centerX = width / 2;
		int centerY = height / 2;
		int shortSide = Math.Min(width, height);
		int roiHalfSize = (int)((double)shortSide * 0.4);
		int roiMinX = Math.Max(0, centerX - roiHalfSize);
		int roiMaxX = Math.Min(width - 1, centerX + roiHalfSize);
		int roiMinY = Math.Max(0, centerY - roiHalfSize);
		int roiMaxY = Math.Min(height - 1, centerY + roiHalfSize);
		int minX = width;
		int maxX = 0;
		int minY = height;
		int maxY = 0;
		object lockObj = new object();
		fixed (byte* pFixed = pixels)
		{
			byte* pBase = pFixed;
			Parallel.For(roiMinY, roiMaxY + 1, (int y) =>
			{
				byte* ptr = pBase + y * stride;
				int num = width;
				int num2 = 0;
				bool flag = false;
				for (int i = roiMinX; i <= roiMaxX; i++)
				{
					int num3 = i << 2;
					if (ptr[num3] + ptr[num3 + 1] + ptr[num3 + 2] < blackThreshold)
					{
						if (i < num)
						{
							num = i;
						}
						if (i > num2)
						{
							num2 = i;
						}
						flag = true;
					}
				}
				if (flag)
				{
					lock (lockObj)
					{
						if (num < minX)
						{
							minX = num;
						}
						if (num2 > maxX)
						{
							maxX = num2;
						}
						if (y < minY)
						{
							minY = y;
						}
						if (y > maxY)
						{
							maxY = y;
						}
					}
				}
			});
		}
		double avgPixelDiameter = 0.0;
		double scale = -1.0;
		if (maxX > minX && maxY > minY)
		{
			avgPixelDiameter = (double)(maxX - minX + (maxY - minY)) / 2.0;
			scale = realDiameterMm / avgPixelDiameter;
		}
		DrawingVisual visual = new DrawingVisual();
		using (DrawingContext dc = visual.RenderOpen())
		{
			dc.DrawImage(bitmap, new Rect(0.0, 0.0, width, height));
			dc.DrawRectangle(null, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 5.0), new Rect(roiMinX, roiMinY, roiMaxX - roiMinX, roiMaxY - roiMinY));
			if (maxX > minX && maxY > minY)
			{
				string infoText = $"Scale: {scale:F5} mm/px\nPixel Dia: {avgPixelDiameter:F1} px\nReal Dia: {realDiameterMm} mm";
				FormattedText ft = new FormattedText(infoText, CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Arial"), (double)width * 0.03, System.Windows.Media.Brushes.Yellow, VisualTreeHelper.GetDpi(visual).PixelsPerDip);
				dc.DrawText(ft, new System.Windows.Point(30.0, 30.0));
				dc.DrawRectangle(null, new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 5.0), new Rect(minX, minY, maxX - minX, maxY - minY));
			}
		}
		string outPath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "_ret.jpg");
		RenderTargetBitmap renderTarget = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
		renderTarget.Render(visual);
		JpegBitmapEncoder encoder = new JpegBitmapEncoder
		{
			QualityLevel = 90
		};
		encoder.Frames.Add(BitmapFrame.Create(renderTarget));
		using (FileStream fs = new FileStream(outPath, FileMode.Create))
		{
			encoder.Save(fs);
		}
		Process.Start(new ProcessStartInfo
		{
			FileName = outPath,
			UseShellExecute = true
		});
		return scale;
	}

	private void but_webcam_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Main ICON : WebCam Click");
		WebCamWnd wnd = new WebCamWnd();
		wnd.Owner = this;
		if (wnd.ShowDialog() != true)
		{
		}
	}

	public bool ConnectBarcodeScanner()
	{
		string comPort = ServiceProvider.Settings.GetPortByKeywords("VID_26F1", "VID_1A86");
		if (comPort != null)
		{
			DisconnectBarcodeScanner();
			ConnectSerial(comPort);
			StartBarcodeUSBWatcher();
			return true;
		}
		System.Windows.MessageBox.Show("\ud83d\udea8 바코드 USB 드라이브를 찾을 수 없습니다.\ud83d\udea8\n\n⚠\ufe0f 바코드 USB 포트 확인 바랍니다.⚠\ufe0f", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		return false;
	}

	public void DisconnectBarcodeScanner()
	{
		serialPortBarcode.DataReceived -= SerialPort_DataReceived;
		if (serialPortBarcode.IsOpen)
		{
			serialPortBarcode.Close();
		}
	}

	public void ConnectSerial(string comPort)
	{
		serialPortBarcode.PortName = comPort;
		serialPortBarcode.BaudRate = 9600;
		serialPortBarcode.DataBits = 8;
		serialPortBarcode.Parity = Parity.None;
		serialPortBarcode.StopBits = StopBits.One;
		serialPortBarcode.ReadTimeout = 1000;
		serialPortBarcode.Open();
		serialPortBarcode.DataReceived += SerialPort_DataReceived;
		serialPortBarcode.Encoding = Encoding.UTF8;
	}

	public void StartBarcodeUSBWatcher()
	{
		string wmiQuery = "SELECT * FROM __InstanceOperationEvent WITHIN 5 WHERE TargetInstance ISA 'Win32_PnPEntity'";
		pnpWatcher = new ManagementEventWatcher(wmiQuery);
		pnpWatcher.EventArrived += DeviceChangedEvent_PnPEntity;
		pnpWatcher.Start();
	}

	private void DeviceChangedEvent_PnPEntity(object sender, EventArrivedEventArgs e)
	{
		if (!ServiceProvider.Settings.UseBarcodeScanner)
		{
			return;
		}
		string eventType = e.NewEvent.ClassPath.ClassName;
		if (!(e.NewEvent["TargetInstance"] is ManagementBaseObject targetInstance))
		{
			return;
		}
		string deviceId = targetInstance["DeviceID"]?.ToString();
		if (string.IsNullOrEmpty(deviceId) || (!deviceId.ToUpper().Contains("VID_26F1") && !deviceId.ToUpper().Contains("VID_1A86")))
		{
			return;
		}
		if (eventType == "__InstanceCreationEvent")
		{
			if (serialPortBarcode == null || !serialPortBarcode.IsOpen)
			{
				ConnectBarcodeScanner();
			}
		}
		else if (eventType == "__InstanceDeletionEvent" && serialPortBarcode != null && serialPortBarcode.IsOpen)
		{
			serialPortBarcode.Close();
		}
	}

	private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
	{
		SerialPort sp = (SerialPort)sender;
		try
		{
			BarCodeValue = sp.ReadExisting().Trim();
			if (string.IsNullOrEmpty(BarCodeValue) || !ServiceProvider.Settings.UseBarcodeScanner)
			{
				return;
			}
			if (ServiceProvider.Settings.ModeBarcodeSession || ServiceProvider.Settings.ModeBarcodeCapture)
			{
				Thread thread = new Thread(() =>
				{
					CaptureOfBarCodeReader(BarCodeValue);
				});
				thread.Start();
				return;
			}
			System.Windows.Application.Current.Dispatcher.Invoke(() =>
			{
				EditSession edit_Session = MainMenuViewModel.Edit_Session;
				if (edit_Session != null && edit_Session.IsVisible)
				{
					edit_Session.Session.Name = BarCodeValue;
					try
					{
						edit_Session.DialogResult = true;
					}
					catch (InvalidOperationException)
					{
						edit_Session.Close();
					}
				}
			});
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			System.Windows.Application.Current.Dispatcher.BeginInvoke((Action)(() =>
			{
				System.Windows.MessageBox.Show("\ud83d\udd25 시리얼 수신 중 예외 발생: " + ex3.Message, "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}));
		}
	}

	public void ExeWinCommand(string cmd, object data)
	{
		try
		{
			TurnTableWnd turnTableWnd = System.Windows.Application.Current.Windows.OfType<TurnTableWnd>().FirstOrDefault();
			if (turnTableWnd == null)
			{
				return;
			}
			string dataString = data?.ToString() ?? "";
			string[] dataParts = dataString.Split('|');
			switch (cmd)
			{
			case "Cmd_Capture":
				CaptureOfIcon_Click(this, null);
				break;
			case "Cmd_Laser":
				if (dataString.Equals("true", StringComparison.OrdinalIgnoreCase))
				{
					turnTableWnd.RemoteLaserOn();
				}
				else
				{
					turnTableWnd.RemoteLaserOff();
				}
				Log.Debug("OK : " + cmd);
				break;
			case "Cmd_Cycle":
				turnTableWnd.RemoteMoveOneCycle();
				Log.Debug("OK : " + cmd);
				break;
			case "Cmd_Stop":
				turnTableWnd.RemoteMoveStop();
				Log.Debug("OK : " + cmd);
				break;
			case "Cmd_ContCW":
				turnTableWnd.RemoteMoveContinueCW();
				Log.Debug("OK : " + cmd);
				break;
			case "Cmd_ContCCW":
				turnTableWnd.RemoteMoveContinueCCW();
				Log.Debug("OK : " + cmd);
				break;
			case "Cmd_StepCW":
				turnTableWnd.RemoteMoveStepCW();
				Log.Debug("OK : " + cmd);
				break;
			case "Cmd_StepCCW":
				turnTableWnd.RemoteMoveStepCCW();
				Log.Debug("OK : " + cmd);
				break;
			case "Cmd_Sample":
				if (dataParts.Length >= 2)
				{
					string speed = dataParts[0];
					string angle = dataParts[1];
					Log.Debug("OK : " + cmd + "param1 : " + speed.ToString() + "param2 : " + angle.ToString());
				}
				break;
			}
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show("Command : " + cmd + "\nData Error: " + ex.Message + "\nRaw Data: " + data);
		}
	}

	private void SwitchToRemoveBg_Click(object sender, RoutedEventArgs e)
	{
		Settings settings = ServiceProvider.Settings;
		settings.ShowRemoveBg = true;
	}

	private void SwitchToNormal_Click(object sender, RoutedEventArgs e)
	{
		Settings settings = ServiceProvider.Settings;
		settings.ShowRemoveBg = false;
		settings.ShowFrontBack = true;
	}

	public void USBConnect()
	{
		if (ServiceProvider.Settings.UseLEDDimmer && !arduino.Init())
		{
			System.Windows.MessageBox.Show("\ud83d\udea8 LED 조명 USB 드라이브를 찾을 수 없습니다.\ud83d\udea8\n\n⚠\ufe0f USB 포트 확인 바랍니다.⚠\ufe0f", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
	}

	public void USBDisconnect()
	{
		if (ServiceProvider.Settings.UseLEDDimmer)
		{
			Send_Dimming("0");
			arduino.Uninit();
		}
	}

	public void Send_Dimming(string bright)
	{
		if (arduino != null && arduino.bIsConnected)
		{
			string packet = "Cmd_DIM 0 " + bright;
			arduino.CommandSend(packet);
		}
	}

	private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (ServiceProvider.Settings.UseLEDDimmer && e.ClickCount == 2)
		{
			if (arduino != null && !arduino.bIsConnected)
			{
				arduino.Uninit();
			}
			arduino.Init();
		}
	}

	void IMainWindowPlugin.Show()
	{
		Show();
	}
}
