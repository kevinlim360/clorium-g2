using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using MaterialDesignColors;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace CameraControl.Core.Classes;

public partial class Settings : BaseFieldClass
{
	public delegate void SessionSelectedEventHandler(PhotoSession oldvalu, PhotoSession newvalue);

	public static string AppName = "Clorium";

	private string ConfigFile = "";

	private PhotoSession _defaultSession;

	private ObservableCollection<CameraPreset> _cameraPresets;

	private AsyncObservableCollection<WindowCommandItem> _actions;

	private BitmapFile _selectedBitmap;

	private bool _imageLoading;

	private string _webaddress;

	private CustomConfigEnumerator _deviceConfigs;

	private Color _fullScreenColor;

	private string _selectedLanguage;

	private bool _disableNativeDrivers;

	private string _currentThemeName;

	private string _currentThemeNameNew;

	private int _liveViewFreezeTimeOut;

	private bool _previewLiveViewImage;

	private bool _useFTPserver;

	private bool _useWebserver;

	private int _webserverPort;

	private bool _allowWebserverActions;

	private bool _publicWebserver;

	private bool _useplaySound;

	private bool _showFocusPoints;

	private string _defaultSessionName;

	private bool _preview;

	private string _fullScreenPassword;

	private bool _hideFullscreenControls;

	private int _previewSeconds;

	private bool _autoPreview;

	private int _smallFocusStepCanon;

	private int _mediumFocusStepCanon;

	private int _largeFocusStepCanon;

	private int _canonFocusStepWait;

	private int _smalFocusStep;

	private int _mediumFocusStep;

	private int _largeFocusStep;

	private int _focusMoveStep;

	private bool _dontLoadThumbnails;

	private string _externalViewer;

	private int _motionBlockSize;

	private int _detectionType;

	private bool _useExternalViewer;

	private string _externalViewerPath;

	private string _externalViewerArgs;

	private string _selectedMainForm;

	private bool _useParallelTransfer;

	private bool _showUntranslatedLabelId;

	private bool _highlightUnderExp;

	private bool _highlightOverExp;

	private bool _easyLiveViewControl;

	private bool _useAppFakeCamera;

	private bool _showThumbUpDown;

	private bool _autoPreviewJpgOnly;

	private string _clientId;

	private bool _loadThumbsDownload;

	private int _thumbHeigh;

	private bool _minimizeToTrayIcon;

	private bool _hideTrayNotifications;

	private bool _startMinimized;

	private bool _startupWithWindows;

	private bool _sendUsageStatistics;

	private bool _flipPreview;

	private bool _enhancedThumbs;

	private int _selectedWifi;

	private string _wifiIp;

	private bool _loadCanonTransferMode;

	private bool _newSessionOnStartUp;

	private bool _usewebcam;

	private bool _skipAddingFileToSession;

	private bool _showThumbInfo;

	private bool _disableHardwareAcceleration;

	private string _webcamdevicename;

	private string _arcodedevicename;

	private string _bgtargetcolor;

	private bool _usedebuglog;

	private bool _usedatabase;

	private bool _showdebugtest;

	private bool _usecomparedate;

	private string _metaScale;

	private double _diskSize;

	private bool _getMeasurePicture;

	private string _cloriumID = "M1_";

	private string _downloadfolder = "D:\\Clorium Images\\";

	// Supply the preset password through deployment configuration; do not embed a recovered default.
	private string _presetpassword = Environment.GetEnvironmentVariable("CLORIUM_PRESET_PASSWORD") ?? string.Empty;

	private string _sessionfile_keepdays = "30";

	private bool _resolutionchange;

	private int _resolutionpixelwidth = 1800;

	private int _resolutionpixelheight = 1500;

	private string _currentpresetname;

	private bool _usealwaysbackup = true;

	private bool _useapicommunication;

	private string _apihost = "";

	private int _rotateIndex;

	private bool _usemonstardeck;

	private bool _usebarcodescanner;

	private bool _useLEDdimmer;

	private int _todaysessioncounter = 0;

	private int _totalsessioncounter = 0;

	private bool _uselighting = false;

	private bool _modebgpng = false;

	private bool _modebgjpeg = false;

	private bool _modelighting = false;

	private bool _modebarcodesession = false;

	private bool _modebarcodecapture = false;

	private int _lighttemperaturemin = 2700;

	private int _lighttemperaturemax = 8500;

	private int _lightsavetime = 5;

	private bool _showpreset = true;

	private bool _showsremovebg = false;

	private bool _showfrontback = false;

	private bool _showstatistics = false;

	private bool _showliveview = false;

	private bool _usealarm = true;

	private bool _usedefectcapture = false;

	private bool _isdefect = false;

	private string _elapseddownloadfolder = "D:\\Clorium Statistics\\";

	private int _alarmwaittime = 180;

	private int _soundTime = 3;

	private int _alarmsoundindex = 0;

	public Stopwatch stopWatch = new Stopwatch();

	public long startTime = 0L;

	public long diffTime = 0L;

	[JsonIgnore]
	[XmlIgnore]
	public Branding Branding => ServiceProvider.Branding;

	[XmlIgnore]
	[JsonIgnore]
	public PhotoSession DefaultSession
	{
		get
		{
			return _defaultSession;
		}
		set
		{
			if (value == null || _defaultSession == value)
			{
				return;
			}
			if (_defaultSession != null)
			{
				foreach (FileItem fileItem in _defaultSession.Files.ToList())
				{
					fileItem.Dispose();
				}
				_defaultSession.Files.Clear();
			}
			PhotoSession oldvalue = _defaultSession;
			_defaultSession = value;
			DefaultSessionName = _defaultSession.Name;
			Task.Run(() =>
			{
				if (SessionSelected != null)
				{
					SessionSelected(oldvalue, value);
				}
				LoadData(_defaultSession);
				Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, (Action)(() =>
				{
					NotifyPropertyChanged("DefaultSession");
				}));
			});
		}
	}

	[XmlIgnore]
	[JsonIgnore]
	public ObservableCollection<CameraPreset> CameraPresets
	{
		get
		{
			return _cameraPresets;
		}
		set
		{
			_cameraPresets = value;
			NotifyPropertyChanged("CameraPresets");
		}
	}

	public AsyncObservableCollection<WindowCommandItem> Actions
	{
		get
		{
			return _actions;
		}
		set
		{
			_actions = value;
			NotifyPropertyChanged("Actions");
		}
	}

	[XmlIgnore]
	[JsonIgnore]
	public BitmapFile SelectedBitmap
	{
		get
		{
			return _selectedBitmap;
		}
		set
		{
			_selectedBitmap = value;
			NotifyPropertyChanged("SelectedBitmap");
		}
	}

	[XmlIgnore]
	[JsonIgnore]
	public bool ImageLoading
	{
		get
		{
			return _imageLoading;
		}
		set
		{
			_imageLoading = value;
			NotifyPropertyChanged("ImageLoading");
		}
	}

	[XmlIgnore]
	[JsonIgnore]
	public ObservableCollection<PhotoSession> PhotoSessions { get; set; }

	public ObservableCollection<PluginSetting> PluginSettings { get; set; }

	public CameraPropertyEnumerator CameraProperties { get; set; }

	public string SelectedLayout { get; set; }

	public bool FullScreenInSecondaryMonitor { get; set; }

	public bool LoadFullRespreview { get; set; }

	public bool WiaDeviceSupport { get; set; }

	public bool Autorotate { get; set; }

	public int ExternalDeviceWaitForFocus { get; set; }

	public int ExternalDeviceWaitForCapture { get; set; }

	public static string ApplicationFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

	public static string BrandingFolder => Path.Combine(ApplicationFolder, "Branding");

	public static string DataFolder => (ServiceProvider.Branding != null && !string.IsNullOrEmpty(ServiceProvider.Branding.ApplicationDataFolder)) ? Path.GetFullPath(ServiceProvider.Branding.ApplicationDataFolder) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppName);

	public static string SessionFolder => Path.Combine(DataFolder, "Sessions");

	public static string CacheFolder => Path.Combine(DataFolder, "Cache");

	public static string PresetFolder => Path.Combine(DataFolder, "Presets");

	public string OverlayFolder => Path.Combine(DataFolder, "LiveViewOverlay");

	public static string WebServerFolder => Path.Combine(ApplicationFolder, "WebServer\\");

	public static string BrandingWebServerFolder => Path.Combine(BrandingFolder, "WebServer\\");

	public List<string> AvaiableWebAddresses
	{
		get
		{
			List<string> res = new List<string>();
			try
			{
				string sHostName = Dns.GetHostName();
				IPAddress[] ipA = (from ip in Dns.GetHostAddresses(sHostName)
					where ip.AddressFamily == AddressFamily.InterNetwork
					select ip).ToArray();
				res.AddRange(ipA.Select((IPAddress ip) => $"http://{ip}:{WebserverPort}/Remote.html"));
			}
			catch (Exception exception)
			{
				Log.Error("Error get web address ", exception);
			}
			return res;
		}
	}

	public string PublicWebAdress => "http://clorium.com/" + ClientId;

	public string Webaddress
	{
		get
		{
			if ((_webaddress == null || !AvaiableWebAddresses.Contains(_webaddress)) && AvaiableWebAddresses.Count > 0)
			{
				return AvaiableWebAddresses[0];
			}
			return _webaddress;
		}
		set
		{
			_webaddress = value;
			NotifyPropertyChanged("Webaddress");
		}
	}

	public CustomConfigEnumerator DeviceConfigs
	{
		get
		{
			return _deviceConfigs;
		}
		set
		{
			_deviceConfigs = value;
			NotifyPropertyChanged("DeviceConfigs");
		}
	}

	public Color FullScreenColor
	{
		get
		{
			return _fullScreenColor;
		}
		set
		{
			_fullScreenColor = value;
			NotifyPropertyChanged("FullScreenColor");
		}
	}

	public string SelectedLanguage
	{
		get
		{
			return _selectedLanguage;
		}
		set
		{
			_selectedLanguage = value;
			NotifyPropertyChanged("SelectedLanguage");
		}
	}

	public bool DisableNativeDrivers
	{
		get
		{
			return _disableNativeDrivers;
		}
		set
		{
			_disableNativeDrivers = value;
			NotifyPropertyChanged("DisableNativeDrivers");
		}
	}

	public string CurrentThemeName
	{
		get
		{
			if (!ServiceProvider.Branding.UseThemeSelector)
			{
				return ServiceProvider.Branding.DefaultTheme;
			}
			return _currentThemeName;
		}
		set
		{
			_currentThemeName = value;
			NotifyPropertyChanged("CurrentThemeName");
			if (ServiceProvider.WindowsManager != null)
			{
				ServiceProvider.WindowsManager.ApplyTheme();
			}
		}
	}

	public string CurrentThemeNameNew
	{
		get
		{
			if (!ServiceProvider.Branding.UseThemeSelector)
			{
				return ServiceProvider.Branding.DefaultTheme;
			}
			return _currentThemeNameNew;
		}
		set
		{
			_currentThemeNameNew = value;
			NotifyPropertyChanged("CurrentThemeNameNew");
		}
	}

	public int LiveViewFreezeTimeOut
	{
		get
		{
			return _liveViewFreezeTimeOut;
		}
		set
		{
			_liveViewFreezeTimeOut = value;
			NotifyPropertyChanged("LiveViewFreezeTimeOut");
		}
	}

	public bool PreviewLiveViewImage
	{
		get
		{
			return _previewLiveViewImage;
		}
		set
		{
			_previewLiveViewImage = value;
			NotifyPropertyChanged("PreviewLiveViewImage");
		}
	}

	public bool UseFTPserver
	{
		get
		{
			return _useFTPserver;
		}
		set
		{
			_useFTPserver = value;
			if (value)
			{
				UseWebserver = false;
			}
			NotifyPropertyChanged("UseFTPserver");
		}
	}

	public bool UseWebserver
	{
		get
		{
			return _useWebserver;
		}
		set
		{
			_useWebserver = value;
			if (value)
			{
				UseFTPserver = false;
			}
			NotifyPropertyChanged("UseWebserver");
		}
	}

	public int WebserverPort
	{
		get
		{
			return _webserverPort;
		}
		set
		{
			_webserverPort = value;
			NotifyPropertyChanged("WebserverPort");
			NotifyPropertyChanged("Webaddress");
		}
	}

	public bool AllowWebserverActions
	{
		get
		{
			return _allowWebserverActions;
		}
		set
		{
			_allowWebserverActions = value;
			NotifyPropertyChanged("AllowWebserverActions");
		}
	}

	public bool PublicWebserver
	{
		get
		{
			return _publicWebserver;
		}
		set
		{
			_publicWebserver = value;
			NotifyPropertyChanged("PublicWebserver");
		}
	}

	public bool UsePlaySound
	{
		get
		{
			return _useplaySound;
		}
		set
		{
			_useplaySound = value;
			NotifyPropertyChanged("UsePlaySound");
		}
	}

	public bool ShowFocusPoints
	{
		get
		{
			return _showFocusPoints;
		}
		set
		{
			_showFocusPoints = value;
			NotifyPropertyChanged("ShowFocusPoints");
		}
	}

	public string DefaultSessionName
	{
		get
		{
			return _defaultSessionName;
		}
		set
		{
			_defaultSessionName = value;
			NotifyPropertyChanged("DefaultSessionName");
		}
	}

	public bool Preview
	{
		get
		{
			return _preview;
		}
		set
		{
			_preview = value;
			NotifyPropertyChanged("Preview");
		}
	}

	public string FullScreenPassword
	{
		get
		{
			return _fullScreenPassword;
		}
		set
		{
			_fullScreenPassword = value;
			NotifyPropertyChanged("FullScreenPassword");
		}
	}

	public bool ShowFullscreenControls
	{
		get
		{
			return _hideFullscreenControls;
		}
		set
		{
			_hideFullscreenControls = value;
			NotifyPropertyChanged("ShowFullscreenControls");
		}
	}

	public int PreviewSeconds
	{
		get
		{
			return _previewSeconds;
		}
		set
		{
			_previewSeconds = value;
			NotifyPropertyChanged("PreviewSeconds");
		}
	}

	public bool AutoPreview
	{
		get
		{
			return _autoPreview;
		}
		set
		{
			_autoPreview = value;
			NotifyPropertyChanged("AutoPreview");
		}
	}

	public int SmallFocusStepCanon
	{
		get
		{
			return _smallFocusStepCanon;
		}
		set
		{
			_smallFocusStepCanon = value;
			NotifyPropertyChanged("SmallFocusStepCanon");
		}
	}

	public int MediumFocusStepCanon
	{
		get
		{
			return _mediumFocusStepCanon;
		}
		set
		{
			_mediumFocusStepCanon = value;
			NotifyPropertyChanged("MediumFocusStepCanon");
		}
	}

	public int LargeFocusStepCanon
	{
		get
		{
			return _largeFocusStepCanon;
		}
		set
		{
			_largeFocusStepCanon = value;
			NotifyPropertyChanged("LargeFocusStepCanon");
		}
	}

	public int CanonFocusStepWait
	{
		get
		{
			return _canonFocusStepWait;
		}
		set
		{
			_canonFocusStepWait = value;
			NotifyPropertyChanged("CanonFocusStepWait");
		}
	}

	public int SmalFocusStep
	{
		get
		{
			return _smalFocusStep;
		}
		set
		{
			_smalFocusStep = value;
			NotifyPropertyChanged("SmalFocusStep");
		}
	}

	public int MediumFocusStep
	{
		get
		{
			return _mediumFocusStep;
		}
		set
		{
			_mediumFocusStep = value;
			NotifyPropertyChanged("MediumFocusStep");
		}
	}

	public int LargeFocusStep
	{
		get
		{
			return _largeFocusStep;
		}
		set
		{
			_largeFocusStep = value;
			NotifyPropertyChanged("LargeFocusStep");
		}
	}

	public int FocusMoveStep
	{
		get
		{
			return _focusMoveStep;
		}
		set
		{
			_focusMoveStep = value;
			NotifyPropertyChanged("FocusMoveStep");
		}
	}

	public bool DontLoadThumbnails
	{
		get
		{
			return _dontLoadThumbnails;
		}
		set
		{
			_dontLoadThumbnails = value;
			NotifyPropertyChanged("DontLoadThumbnails");
		}
	}

	public string ExternalViewer
	{
		get
		{
			return _externalViewer;
		}
		set
		{
			_externalViewer = value;
			NotifyPropertyChanged("ExternalViewer");
		}
	}

	public int MotionBlockSize
	{
		get
		{
			return _motionBlockSize;
		}
		set
		{
			_motionBlockSize = value;
			NotifyPropertyChanged("MotionBlockSize");
		}
	}

	public int DetectionType
	{
		get
		{
			return _detectionType;
		}
		set
		{
			_detectionType = value;
			NotifyPropertyChanged("DetectionType");
		}
	}

	public bool UseExternalViewer
	{
		get
		{
			return _useExternalViewer;
		}
		set
		{
			_useExternalViewer = value;
			NotifyPropertyChanged("UseExternalViewer");
		}
	}

	public string ExternalViewerPath
	{
		get
		{
			return _externalViewerPath;
		}
		set
		{
			_externalViewerPath = value;
			NotifyPropertyChanged("ExternalViewerPath");
		}
	}

	public string ExternalViewerArgs
	{
		get
		{
			return _externalViewerArgs;
		}
		set
		{
			_externalViewerArgs = value;
			NotifyPropertyChanged("ExternalViewerArgs");
		}
	}

	public string SelectedMainForm
	{
		get
		{
			return _selectedMainForm;
		}
		set
		{
			_selectedMainForm = value;
			NotifyPropertyChanged("SelectedMainForm");
		}
	}

	public bool UseParallelTransfer
	{
		get
		{
			return _useParallelTransfer;
		}
		set
		{
			_useParallelTransfer = value;
			NotifyPropertyChanged("UseParallelTransfer");
		}
	}

	public bool ShowUntranslatedLabelId
	{
		get
		{
			return _showUntranslatedLabelId;
		}
		set
		{
			_showUntranslatedLabelId = value;
			NotifyPropertyChanged("ShowUntranslatedLabelId");
		}
	}

	public bool HighlightUnderExp
	{
		get
		{
			return _highlightUnderExp;
		}
		set
		{
			_highlightUnderExp = value;
			NotifyPropertyChanged("HighlightUnderExp");
		}
	}

	public bool HighlightOverExp
	{
		get
		{
			return _highlightOverExp;
		}
		set
		{
			_highlightOverExp = value;
			NotifyPropertyChanged("HighlightOverExp");
		}
	}

	public bool EasyLiveViewControl
	{
		get
		{
			return _easyLiveViewControl;
		}
		set
		{
			_easyLiveViewControl = value;
			NotifyPropertyChanged("EasyLiveViewControl");
		}
	}

	public bool UseAppFakeCamera
	{
		get
		{
			return _useAppFakeCamera;
		}
		set
		{
			_useAppFakeCamera = value;
			NotifyPropertyChanged("UseAppFakeCamera");
		}
	}

	public bool ShowThumbUpDown
	{
		get
		{
			return _showThumbUpDown;
		}
		set
		{
			_showThumbUpDown = value;
			NotifyPropertyChanged("ShowThumbUpDown");
		}
	}

	public bool AutoPreviewJpgOnly
	{
		get
		{
			return _autoPreviewJpgOnly;
		}
		set
		{
			_autoPreviewJpgOnly = value;
			NotifyPropertyChanged("AutoPreviewJpgOnly");
		}
	}

	public string ClientId
	{
		get
		{
			return _clientId;
		}
		set
		{
			_clientId = value;
			NotifyPropertyChanged("ClientId");
		}
	}

	public bool LoadThumbsDownload
	{
		get
		{
			return _loadThumbsDownload;
		}
		set
		{
			_loadThumbsDownload = value;
			NotifyPropertyChanged("LoadThumbsDownload");
		}
	}

	public int ThumbHeigh
	{
		get
		{
			return _thumbHeigh;
		}
		set
		{
			_thumbHeigh = value;
			NotifyPropertyChanged("ThumbHeigh");
		}
	}

	public bool MinimizeToTrayIcon
	{
		get
		{
			return _minimizeToTrayIcon;
		}
		set
		{
			_minimizeToTrayIcon = value;
			NotifyPropertyChanged("MinimizeToTrayIcon");
		}
	}

	public bool HideTrayNotifications
	{
		get
		{
			return _hideTrayNotifications;
		}
		set
		{
			_hideTrayNotifications = value;
			NotifyPropertyChanged("HideTrayNotifications");
		}
	}

	public bool StartMinimized
	{
		get
		{
			return _startMinimized;
		}
		set
		{
			_startMinimized = value;
			NotifyPropertyChanged("StartMinimized");
		}
	}

	public bool StartupWithWindows
	{
		get
		{
			return _startupWithWindows;
		}
		set
		{
			_startupWithWindows = value;
			NotifyPropertyChanged("StartupWithWindows");
		}
	}

	public bool SendUsageStatistics
	{
		get
		{
			return _sendUsageStatistics;
		}
		set
		{
			_sendUsageStatistics = value;
			NotifyPropertyChanged("SendUsageStatistics");
		}
	}

	public bool FlipPreview
	{
		get
		{
			return _flipPreview;
		}
		set
		{
			_flipPreview = value;
			NotifyPropertyChanged("FlipPreview");
		}
	}

	public bool EnhancedThumbs
	{
		get
		{
			return _enhancedThumbs;
		}
		set
		{
			_enhancedThumbs = value;
			NotifyPropertyChanged("EnhancedThumbs");
		}
	}

	public int SelectedWifi
	{
		get
		{
			return _selectedWifi;
		}
		set
		{
			_selectedWifi = value;
			NotifyPropertyChanged("SelectedWifi");
		}
	}

	public string WifiIp
	{
		get
		{
			return _wifiIp;
		}
		set
		{
			_wifiIp = value;
			NotifyPropertyChanged("WifiIp");
		}
	}

	public bool LoadCanonTransferMode
	{
		get
		{
			return _loadCanonTransferMode;
		}
		set
		{
			_loadCanonTransferMode = value;
			NotifyPropertyChanged("LoadCanonTransferMode");
		}
	}

	public bool NewSessionOnStartUp
	{
		get
		{
			return _newSessionOnStartUp;
		}
		set
		{
			_newSessionOnStartUp = value;
			NotifyPropertyChanged("NewSessionOnStartUp");
		}
	}

	public bool UseWebcam
	{
		get
		{
			return _usewebcam;
		}
		set
		{
			_usewebcam = value;
			NotifyPropertyChanged("UseWebcam");
		}
	}

	public bool SkipAddingFileToSession
	{
		get
		{
			return _skipAddingFileToSession;
		}
		set
		{
			_skipAddingFileToSession = value;
		}
	}

	public bool ShowThumbInfo
	{
		get
		{
			return _showThumbInfo;
		}
		set
		{
			_showThumbInfo = value;
			NotifyPropertyChanged("ShowThumbInfo");
		}
	}

	public bool DisableHardwareAccelerationNew
	{
		get
		{
			return _disableHardwareAcceleration;
		}
		set
		{
			_disableHardwareAcceleration = value;
			NotifyPropertyChanged("DisableHardwareAccelerationNew");
		}
	}

	public string Version_Debug { get; set; }

	public string CameraAlias { get; set; }

	public string LightPresetName { get; set; }

	public string TurnTableShot_PathName { get; set; }

	public string TurnTableShot_FileName { get; set; }

	public int ProcessCounter { get; set; }

	public int BGThresHold { get; set; }

	public int BGSoftRange { get; set; }

	public bool ShowAPICommunication { get; set; }

	public bool BGAlgorithm { get; set; }

	public bool Background { get; set; }

	public bool D_DayBefore { get; set; }

	private bool _turnTableShot;
    public bool TurnTableShot
    {
        get => _turnTableShot;
        set
        {
            if (_turnTableShot == value) return;
            _turnTableShot = value;
            NotifyPropertyChanged("TurnTableShot");
            NotifyPropertyChanged("CanChangeAestheticImprovement");
        }
    }

	public int CurrentColorTemperature { get; set; }

	private bool _isBtnPressed;

	public bool isBtnPressed
	{
		get => _isBtnPressed;
		set
		{
			if (_isBtnPressed == value) return;
			_isBtnPressed = value;
			NotifyPropertyChanged("isBtnPressed");
            NotifyPropertyChanged("CanChangeAestheticImprovement");
		}
	}

	private bool _thumbailLoaded;

	public bool ThumbailLoaded
	{
		get => _thumbailLoaded;
		set
		{
			if (_thumbailLoaded == value) return;
			_thumbailLoaded = value;
			NotifyPropertyChanged("ThumbailLoaded");
            NotifyPropertyChanged("CanChangeAestheticImprovement");
		}
	}

	public int FrontBackMode { get; set; } = 0;

	public bool UseHistogram { get; set; }

	public bool UseHighlight { get; set; }

	public string WebCam_DeviceName
	{
		get
		{
			return _webcamdevicename;
		}
		set
		{
			_webcamdevicename = value;
			NotifyPropertyChanged("WebCam_DeviceName");
		}
	}

	public string Barcode_DeviceName
	{
		get
		{
			return _arcodedevicename;
		}
		set
		{
			_arcodedevicename = value;
			NotifyPropertyChanged("Barcode_DeviceName");
		}
	}

	public string BGTargetColor
	{
		get
		{
			return _bgtargetcolor;
		}
		set
		{
			_bgtargetcolor = value;
			NotifyPropertyChanged("BGTargetColor");
		}
	}

	public bool UseDebugLog
	{
		get
		{
			return _usedebuglog;
		}
		set
		{
			_usedebuglog = value;
			NotifyPropertyChanged("UseDebugLog");
		}
	}

	public bool UseDatabase
	{
		get
		{
			return _usedatabase;
		}
		set
		{
			_usedatabase = value;
			NotifyPropertyChanged("UseDatabase");
		}
	}

	public bool ShowDebugTest
	{
		get
		{
			return _showdebugtest;
		}
		set
		{
			_showdebugtest = value;
			NotifyPropertyChanged("ShowDebugTest");
		}
	}

	public bool UseCompareDateMode
	{
		get
		{
			return _usecomparedate;
		}
		set
		{
			_usecomparedate = value;
			NotifyPropertyChanged("UseCompareDateMode");
		}
	}

	public string MetaScale
	{
		get
		{
			return _metaScale;
		}
		set
		{
			_metaScale = value;
			NotifyPropertyChanged("MetaScale");
		}
	}

	public double DiskSize
	{
		get
		{
			return _diskSize;
		}
		set
		{
			_diskSize = value;
			NotifyPropertyChanged("DiskSize");
		}
	}

	public bool GetMeasurePicture
	{
		get
		{
			return _getMeasurePicture;
		}
		set
		{
			_getMeasurePicture = value;
			NotifyPropertyChanged("GetMeasurePicture");
		}
	}

	public string CloriumID
	{
		get
		{
			return _cloriumID;
		}
		set
		{
			_cloriumID = value;
			NotifyPropertyChanged("CloriumID");
		}
	}

	public string DownloadFolder
	{
		get
		{
			return _downloadfolder;
		}
		set
		{
			_downloadfolder = value;
			NotifyPropertyChanged("DownloadFolder");
		}
	}

	public string PresetPassword
	{
		get
		{
			return _presetpassword;
		}
		set
		{
			_presetpassword = value;
			NotifyPropertyChanged("PresetPassword");
		}
	}

	public string SessionFileKeepDays
	{
		get
		{
			return _sessionfile_keepdays;
		}
		set
		{
			if (int.Parse(value) < 5)
			{
				value = "4";
			}
			_sessionfile_keepdays = value;
			NotifyPropertyChanged("SessionFileKeepDays");
		}
	}

	public bool ResolutionChange
	{
		get
		{
			return _resolutionchange;
		}
		set
		{
			_resolutionchange = value;
			NotifyPropertyChanged("ResolutionChange");
		}
	}

	public int ResolutionPixelWidth
	{
		get
		{
			return _resolutionpixelwidth;
		}
		set
		{
			_resolutionpixelwidth = value;
			NotifyPropertyChanged("ResolutionPixelWidth");
		}
	}

	public int ResolutionPixelHeight
	{
		get
		{
			return _resolutionpixelheight;
		}
		set
		{
			_resolutionpixelheight = value;
			NotifyPropertyChanged("ResolutionPixelHeight");
		}
	}

	public string CurrentPresetName
	{
		get
		{
			return _currentpresetname;
		}
		set
		{
			_currentpresetname = value;
			NotifyPropertyChanged("CurrentPresetName");
		}
	}

	public bool UseAlwaysBackup
	{
		get
		{
			return _usealwaysbackup;
		}
		set
		{
			_usealwaysbackup = value;
			NotifyPropertyChanged("UseAlwaysBackup");
		}
	}

	public bool UseAPICommunication
	{
		get
		{
			return _useapicommunication;
		}
		set
		{
			_useapicommunication = value;
			NotifyPropertyChanged("UseAPICommunication");
		}
	}

	public string APIHost
	{
		get
		{
			return _apihost;
		}
		set
		{
			_apihost = value;
			NotifyPropertyChanged("APIHost");
		}
	}

	public int RotateIndex
	{
		get
		{
			return _rotateIndex;
		}
		set
		{
			_rotateIndex = value;
			NotifyPropertyChanged("RotateIndex");
		}
	}

	public bool UseMonstarDeck
	{
		get
		{
			return _usemonstardeck;
		}
		set
		{
			_usemonstardeck = value;
			NotifyPropertyChanged("UseMonstarDeck");
		}
	}

	public bool UseBarcodeScanner
	{
		get
		{
			return _usebarcodescanner;
		}
		set
		{
			_usebarcodescanner = value;
			NotifyPropertyChanged("UseBarcodeScanner");
		}
	}

	public bool UseLEDDimmer
	{
		get
		{
			return _useLEDdimmer;
		}
		set
		{
			_useLEDdimmer = value;
			NotifyPropertyChanged("UseLEDDimmer");
		}
	}

	public int TodaySessionCounter
	{
		get
		{
			return _todaysessioncounter;
		}
		set
		{
			_todaysessioncounter = value;
			NotifyPropertyChanged("TodaySessionCounter");
		}
	}

	public int TotalSessionCounter
	{
		get
		{
			return _totalsessioncounter;
		}
		set
		{
			_totalsessioncounter = value;
			NotifyPropertyChanged("TotalSessionCounter");
		}
	}

	public bool UseLighting
	{
		get
		{
			return _uselighting;
		}
		set
		{
			_uselighting = value;
			NotifyPropertyChanged("UseLighting");
		}
	}

	public bool ModeBgPng
	{
		get
		{
			return _modebgpng;
		}
		set
		{
			_modebgpng = value;
			NotifyPropertyChanged("ModeBgPng");
		}
	}

	public bool ModeBgJpeg
	{
		get
		{
			return _modebgjpeg;
		}
		set
		{
			_modebgjpeg = value;
			NotifyPropertyChanged("ModeBgJpeg");
		}
	}

	public bool ModeLighting
	{
		get
		{
			return _modelighting;
		}
		set
		{
			_modelighting = value;
			NotifyPropertyChanged("ModeLighting");
		}
	}

	public bool ModeBarcodeSession
	{
		get
		{
			return _modebarcodesession;
		}
		set
		{
			_modebarcodesession = value;
			NotifyPropertyChanged("ModeBarcodeSession");
		}
	}

	public bool ModeBarcodeCapture
	{
		get
		{
			return _modebarcodecapture;
		}
		set
		{
			_modebarcodecapture = value;
			NotifyPropertyChanged("ModeBarcodeCapture");
		}
	}

	public int LightTemperatureMin
	{
		get
		{
			return _lighttemperaturemin;
		}
		set
		{
			_lighttemperaturemin = value;
			NotifyPropertyChanged("LightTemperatureMin");
		}
	}

	public int LightTemperatureMax
	{
		get
		{
			return _lighttemperaturemax;
		}
		set
		{
			_lighttemperaturemax = value;
			NotifyPropertyChanged("LightTemperatureMax");
		}
	}

	public int LightPowerSaveTime
	{
		get
		{
			return _lightsavetime;
		}
		set
		{
			_lightsavetime = value;
			NotifyPropertyChanged("LightPowerSaveTime");
		}
	}

	public bool ShowPreset
	{
		get
		{
			return _showpreset;
		}
		set
		{
			_showpreset = value;
			NotifyPropertyChanged("ShowPreset");
		}
	}

	public bool ShowRemoveBg
	{
		get
		{
			return _showsremovebg;
		}
		set
		{
			if (_showsremovebg != value)
			{
				_showsremovebg = value;
				if (value)
				{
					ShowFrontBack = false;
				}
				NotifyPropertyChanged("ShowRemoveBg");
			}
		}
	}

	public bool ShowFrontBack
	{
		get
		{
			return _showfrontback;
		}
		set
		{
			_showfrontback = value;
			if (value)
			{
				ShowRemoveBg = false;
			}
			NotifyPropertyChanged("ShowFrontBack");
		}
	}

	public bool ShowStatistics
	{
		get
		{
			return _showstatistics;
		}
		set
		{
			_showstatistics = value;
			NotifyPropertyChanged("ShowStatistics");
		}
	}

	public bool ShowLiveView
	{
		get
		{
			return _showliveview;
		}
		set
		{
			_showliveview = value;
			NotifyPropertyChanged("ShowLiveView");
		}
	}

	public DateTime PrevTime { get; set; }

	public DateTime PauseTime { get; set; }

	public bool IsTimerRunning { get; set; }

	public bool IsMaximized { get; set; }

	public string LED_Dimming { get; set; } = "0";

	public bool UseAlarm
	{
		get
		{
			return _usealarm;
		}
		set
		{
			_usealarm = value;
			NotifyPropertyChanged("UseAlarm");
		}
	}

	public bool UseDefectCapture
	{
		get
		{
			return _usedefectcapture;
		}
		set
		{
			_usedefectcapture = value;
			NotifyPropertyChanged("UseDefectCapture");
		}
	}

	public bool IsDefect
	{
		get
		{
			return _isdefect;
		}
		set
		{
			_isdefect = value;
			NotifyPropertyChanged("IsDefect");
		}
	}

	public string ElapsedDownloadFolder
	{
		get
		{
			return _elapseddownloadfolder;
		}
		set
		{
			_elapseddownloadfolder = value;
			NotifyPropertyChanged("ElapsedDownloadFolder");
		}
	}

	public int AlarmWaitTime
	{
		get
		{
			return _alarmwaittime;
		}
		set
		{
			_alarmwaittime = value;
			NotifyPropertyChanged("AlarmWaitTime");
		}
	}

	public int SoundTime
	{
		get
		{
			return _soundTime;
		}
		set
		{
			_soundTime = value;
			NotifyPropertyChanged("SoundTime");
		}
	}

	public int AlarmSoundIndex
	{
		get
		{
			return _alarmsoundindex;
		}
		set
		{
			_alarmsoundindex = value;
			NotifyPropertyChanged("AlarmSoundIndex");
		}
	}

	public PluginSetting this[string name]
	{
		get
		{
			foreach (PluginSetting pluginSetting in PluginSettings)
			{
				if (pluginSetting.Name == name)
				{
					return pluginSetting;
				}
			}
			PluginSetting pl = new PluginSetting
			{
				Name = name
			};
			PluginSettings.Add(pl);
			return pl;
		}
	}

	public List<string> Themes
	{
		get
		{
			try
			{
				List<string> themes = new SwatchesProvider().Swatches.Select((Swatch accent) => "Light\\" + accent.Name).ToList();
				themes.AddRange(new SwatchesProvider().Swatches.Select((Swatch accent) => "Dark\\" + accent.Name));
				return themes;
			}
			catch (Exception)
			{
				return new List<string>();
			}
		}
	}

	public event SessionSelectedEventHandler SessionSelected;

	public Settings()
	{
		ConfigFile = Path.Combine(DataFolder, "settings.json");
		CameraPresets = new AsyncObservableCollection<CameraPreset>();
		DefaultSession = new PhotoSession();
		PhotoSessions = new ObservableCollection<PhotoSession>();
		SelectedBitmap = new BitmapFile();
		CameraProperties = new CameraPropertyEnumerator();
		DeviceConfigs = new CustomConfigEnumerator();
		PluginSettings = new ObservableCollection<PluginSetting>();
		ResetSettings();
	}

	public void ResetSettings()
	{
		Actions = new AsyncObservableCollection<WindowCommandItem>();
		ClientId = Guid.NewGuid().ToString();
		if (ServiceProvider.WindowsManager != null)
		{
			SyncActions(ServiceProvider.WindowsManager.WindowCommands);
		}
		DisableNativeDrivers = false;
		WiaDeviceSupport = false;
		PublicWebserver = false;
		AllowWebserverActions = true;
		ImageLoading = false;
		Preview = false;
		PreviewSeconds = 3;
		PreviewLiveViewImage = true;
		LiveViewFreezeTimeOut = 3;
		SmalFocusStep = 10;
		MediumFocusStep = 100;
		LargeFocusStep = 500;
		SmallFocusStepCanon = 1;
		MediumFocusStepCanon = 4;
		LargeFocusStepCanon = 25;
		CanonFocusStepWait = 400;
		FocusMoveStep = 50;
		RotateIndex = 0;
		MotionBlockSize = 40;
		HighlightOverExp = false;
		HighlightUnderExp = false;
		ShowUntranslatedLabelId = false;
		ShowFocusPoints = true;
		ShowThumbUpDown = false;
		ShowThumbInfo = true;
		ShowFullscreenControls = true;
		AutoPreview = true;
		AutoPreviewJpgOnly = false;
		Autorotate = true;
		LoadThumbsDownload = true;
		EnhancedThumbs = true;
		ThumbHeigh = 100;
		FullScreenInSecondaryMonitor = false;
		LoadCanonTransferMode = true;
		SelectedLayout = "Normal";
		DisableHardwareAccelerationNew = true;
		ExternalDeviceWaitForCapture = 1000;
		ExternalDeviceWaitForFocus = 1000;
		SendUsageStatistics = false;
		MinimizeToTrayIcon = false;
		StartMinimized = false;
		ExternalViewerArgs = string.Empty;
		UseExternalViewer = false;
		FullScreenColor = Colors.Black;
		UseAppFakeCamera = false;
		UseFTPserver = false;
		UseWebserver = false;
		WebserverPort = 8080;
		UseCompareDateMode = true;
		UseAlwaysBackup = true;
		UseMonstarDeck = false;
		UseBarcodeScanner = false;
		UseLEDDimmer = false;
		UsePlaySound = true;
		UseDatabase = false;
		UseDebugLog = false;
		UseParallelTransfer = false;
		UseAPICommunication = false;
		UseAlarm = false;
		UseDefectCapture = false;
        UseAestheticImprovement = false;
		ShowAPICommunication = false;
		ShowDebugTest = false;
		ShowFrontBack = false;
		ShowStatistics = false;
		ShowLiveView = true;
		ShowDebugTest = false;
		ModeBarcodeSession = false;
		ModeBarcodeCapture = false;
		ModeLighting = false;
		ModeBgPng = false;
		ModeBgJpeg = false;
		StartupWithWindows = false;
		SelectedLanguage = "en_US";
		CurrentThemeNameNew = "Dark\\grey";
		LightTemperatureMin = 2700;
		LightTemperatureMax = 8500;
		LightPowerSaveTime = 5;
		BGTargetColor = "#FFDBE9FF";
		BGThresHold = 30;
		BGSoftRange = 35;
		BGAlgorithm = false;
		CloriumID = "M1_";
		MetaScale = "0.222222";
		DiskSize = 500.0;
		APIHost = "http://localhost:5000";
		LightPresetName = "Default";
		WebCam_DeviceName = "";
		Barcode_DeviceName = "";
		IsDefect = false;
	}

	public void SyncActions(IEnumerable<WindowCommandItem> items)
	{
		List<WindowCommandItem> itemsToAdd = (from windowCommandItem in items
			let found = Actions.Any((WindowCommandItem commandItem) => windowCommandItem.Name == commandItem.Name)
			where !found
			select windowCommandItem).ToList();
		foreach (WindowCommandItem item in itemsToAdd)
		{
			Actions.Add(item);
		}
	}

	public void Add(PhotoSession session)
	{
		Save(session);
		PhotoSessions.Add(session);
	}

	public void LoadData(PhotoSession session)
	{
		try
		{
			if (session == null)
			{
				return;
			}
			if (Directory.Exists(session.Folder))
			{
				if (ServiceProvider.Settings != null && ServiceProvider.Settings.SkipAddingFileToSession)
				{
					session.Files.Clear();
					return;
				}
				if (session.AlowFolderChange && session.ReloadOnFolderChange)
				{
					session.Files.Clear();
				}
				FileItem[] fileItems = new FileItem[session.Files.Count];
				session.Files.CopyTo(fileItems, 0);
				string[] files = Directory.GetFiles(session.Folder);
				string[] array = files;
				foreach (string file in array)
				{
					if (session.SupportedExtensions.Contains(Path.GetExtension(file).ToLower()) && !string.IsNullOrEmpty(file) && !session.ContainFile(file))
					{
						session.AddFile(file);
					}
				}
			}
			List<FileItem> removedItems = session.Files.Where((FileItem fileItem) => !File.Exists(fileItem.FileName)).ToList();
			foreach (FileItem removedItem in removedItems)
			{
				removedItem.Visible = false;
			}
			Log.Debug("세션 이름 : " + session.Name + "      썸네일 개수 : " + session.Files.Count((FileItem x) => x.Visible));
		}
		catch (Exception exception)
		{
			Log.Error("세션 로드 실패 ", exception);
		}
	}

	public void Save(PhotoSession session)
	{
		if (session == null)
		{
			return;
		}
		try
		{
			string filename = Path.Combine(SessionFolder, session.Name + ".json");
			SaveSession(session, filename);
			session.ConfigFile = filename;
		}
		catch (Exception exception)
		{
			Log.Error("포토 세션 저장 실패 : " + session.Name, exception);
		}
	}

	public void SaveSession(PhotoSession session, string filename)
	{
		string json = JsonConvert.SerializeObject(session);
		File.WriteAllText(filename, json);
	}

	public void Save(CameraPreset preset)
	{
		if (preset == null)
		{
			return;
		}
		try
		{
			if (!Directory.Exists(PresetFolder))
			{
				Directory.CreateDirectory(PresetFolder);
			}
			preset.Save(preset.FileName);
		}
		catch (Exception exception)
		{
			Log.Error("프리셋 파일 저장 실패 : " + preset.Name, exception);
		}
	}

	public void Save(Branding branding)
	{
		if (branding == null)
		{
			return;
		}
		try
		{
			string filename = Path.Combine(DataFolder, "Branding.xml");
			XmlSerializer serializer = new XmlSerializer(typeof(Branding));
			Stream writer = new FileStream(filename, FileMode.Create);
			serializer.Serialize(writer, branding);
			writer.Close();
		}
		catch (Exception)
		{
			Log.Error("브랜딩 파일 저장 실패");
		}
	}

	public void Save()
	{
		try
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(ConfigFile, json);
			foreach (CameraPreset cameraPreset in CameraPresets)
			{
				Save(cameraPreset);
			}
		}
		catch (Exception exception)
		{
			Log.Error("설정 저장 실패", exception);
			MessageBox.Show("쓰기 권한이 없습니다.");
		}
	}

	public PhotoSession LoadSession(string filename)
	{
		PhotoSession photoSession = new PhotoSession();
		try
		{
			if (File.Exists(filename))
			{
				if (Path.GetExtension(filename) == ".json")
				{
					photoSession = JsonConvert.DeserializeObject<PhotoSession>(File.ReadAllText(filename));
				}
				else
				{
					XmlSerializer mySerializer = new XmlSerializer(typeof(PhotoSession));
					FileStream myFileStream = new FileStream(filename, FileMode.Open);
					photoSession = (PhotoSession)mySerializer.Deserialize(myFileStream);
					myFileStream.Close();
					photoSession.ConfigFile = filename;
					string s = photoSession.FileNameTemplate;
					s = s.Replace("$C", "[Counter 4 digit]");
					s = s.Replace("$N", "[Session Name]");
					s = s.Replace("$E", "[Exposure Compensation]");
					s = s.Replace("$D", "[Date yyyy-MM-dd]");
					s = s.Replace("$B", "[Barcode]");
					s = s.Replace("$Type", "[File format]");
					s = s.Replace("$X", "[Camera Name]");
					s = s.Replace("$Tag1", "[Selected Tag1]");
					s = s.Replace("$Tag2", "[Selected Tag2]");
					s = s.Replace("$Tag3", "[Selected Tag3]");
					s = s.Replace("$Tag4", "[Selected Tag4]");
					photoSession.FileNameTemplate = s;
				}
			}
		}
		catch (Exception message)
		{
			Log.Error(message);
		}
		if (photoSession == null)
		{
			photoSession = new PhotoSession();
		}
		return photoSession;
	}

	public Settings Load()
	{
		Settings settings = new Settings();
		if (File.Exists(ServiceProvider.Branding.DefaultSettings))
		{
			settings = LoadSettings(ServiceProvider.Branding.DefaultSettings, settings);
		}
		if (ServiceProvider.Branding.ResetSettingsOnLoad)
		{
			return settings;
		}
		try
		{
			if (!Directory.Exists(Path.GetDirectoryName(ConfigFile)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile));
			}
			if (File.Exists(ConfigFile) || File.Exists(ConfigFile.Replace(".json", ".xml")))
			{
				settings = LoadSettings(ConfigFile, settings);
			}
			else
			{
				settings.Save();
			}
			if (settings == null)
			{
				settings = new Settings();
				settings.Save();
			}
			settings.LoadPresetData();
		}
		catch (Exception exception)
		{
			Log.Error("Settings 로딩 실패", exception);
		}
		return settings;
	}

	public Settings LoadSettings(string fileName, Settings defaultSettings)
	{
		try
		{
			if (File.Exists(fileName))
			{
				if (Path.GetExtension(fileName) == ".json")
				{
					defaultSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(fileName));
				}
				else
				{
					XmlSerializer mySerializer = new XmlSerializer(typeof(Settings));
					FileStream myFileStream = new FileStream(fileName, FileMode.Open);
					defaultSettings = (Settings)mySerializer.Deserialize(myFileStream);
					myFileStream.Close();
				}
			}
			else
			{
				defaultSettings = LoadSettings(fileName.Replace(".json", ".xml"), defaultSettings);
			}
		}
		catch
		{
			throw;
		}
		if (defaultSettings == null)
		{
			defaultSettings = new Settings();
		}
		return defaultSettings;
	}

	public PhotoSession GetSession(string name)
	{
		try
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			if (!string.IsNullOrEmpty(name))
			{
				return PhotoSessions.FirstOrDefault((PhotoSession photoSession) => photoSession.Name == name);
			}
		}
		catch (Exception exception)
		{
			Log.Debug("포토 세션 찾기 실패 : ", exception);
		}
		return null;
	}

	public CameraPreset GetPreset(string name)
	{
		return string.IsNullOrEmpty(name) ? null : CameraPresets.FirstOrDefault((CameraPreset cameraPreset) => cameraPreset.Name == name);
	}

	public void LoadSessionData()
	{
		string sesionFolder = SessionFolder;
		if (!Directory.Exists(sesionFolder))
		{
			Directory.CreateDirectory(sesionFolder);
		}
		string cacheFolder = CacheFolder;
		if (!Directory.Exists(CacheFolder))
		{
			Directory.CreateDirectory(cacheFolder);
		}
		string[] sesions = Directory.GetFiles(sesionFolder, "*.json");
		string[] array = sesions;
		foreach (string sesion in array)
		{
			try
			{
				PhotoSessions.Add(LoadSession(sesion));
			}
			catch (Exception exception)
			{
				Log.Error("Error loading session :" + sesion, exception);
			}
		}
		if (PhotoSessions.Count == 0)
		{
			sesions = Directory.GetFiles(sesionFolder, "*.xml");
			string[] array2 = sesions;
			foreach (string sesion2 in array2)
			{
				try
				{
					PhotoSession sesiondata = LoadSession(sesion2);
					Add(sesiondata);
					File.Delete(sesion2);
					Save(sesiondata);
				}
				catch (Exception exception2)
				{
					Log.Error("세션 데이터 로드 실패 :" + sesion2, exception2);
				}
			}
		}
		if (NewSessionOnStartUp)
		{
			DefaultSession = GenerateNewSession();
			Add(DefaultSession);
			return;
		}
		if (PhotoSessions.Count > 0)
		{
			DefaultSession = GetSession(DefaultSessionName) ?? PhotoSessions[0];
		}
		if (PhotoSessions.Count == 0)
		{
			Add(DefaultSession);
		}
	}

	private PhotoSession GenerateNewSession()
	{
		PhotoSession session = new PhotoSession();
		session.Name = "Session";
		string defaultsessionfile = Path.Combine(SessionFolder, "Default.json");
		if (File.Exists(defaultsessionfile))
		{
			session = ServiceProvider.Settings.LoadSession(defaultsessionfile);
			session.Files.Clear();
		}
		int i = 1;
		string sessiomname = session.Name;
		while (PhotoSessions.Any((PhotoSession x) => x.Name.ToLower() == (sessiomname + i).ToLower() && x.Files.Count > 0))
		{
			i++;
		}
		sessiomname += i;
		session.Name = sessiomname;
		return session;
	}

	public void LoadPresetData()
	{
		if (!Directory.Exists(PresetFolder))
		{
			Directory.CreateDirectory(PresetFolder);
		}
		string[] presets = Directory.GetFiles(PresetFolder, "*.xml");
		string[] array = presets;
		foreach (string presetname in array)
		{
			try
			{
				CameraPreset preset = CameraPreset.Load(presetname);
				if (preset != null)
				{
					CameraPresets.Add(preset);
				}
			}
			catch (Exception exception)
			{
				Log.Error("프리셋 데이터 로드 실패 :" + presetname, exception);
			}
		}
	}

	public void RemoveSessionFile(string keepdays)
	{
		// Isolated development runs must not clean shared installed-app sessions
		// or redirect the user to the hard-coded installed executable below.
		if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "g2-development-runtime.marker"))) return;
		if (int.Parse(keepdays) <= ProcessCounter++)
		{
			DeleteSession("C:\\ProgramData\\Clorium\\Sessions", keepdays);
			DeleteCache("C:\\ProgramData\\Clorium\\Cache\\InfoFile", keepdays);
			DeleteCache("C:\\ProgramData\\Clorium\\Cache\\Large", keepdays);
			DeleteCache("C:\\ProgramData\\Clorium\\Cache\\Small", keepdays);
			if (int.Parse(keepdays) > 4)
			{
				ProcessCounter = 0;
				Save();
				MessageBox.Show("\ud83d\udea8 프로그램 최적화 작업중으로 최대 1~2분 소요됩니다.\n\n\ud83d\udea8 만일 프로그램이 자동으로 실행되지 않으면 1~2분 후 직접 재 시작해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				Process.Start(new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = "/C timeout /t 3 & start \"\" \"C:\\Program Files (x86)\\Clorium\\CameraControl.exe\"",
					WindowStyle = ProcessWindowStyle.Hidden
				});
				Environment.Exit(0);
			}
		}
	}

	private void DeleteSession(string folder, string keepdays)
	{
		try
		{
			DirectoryInfo dir_session = new DirectoryInfo(folder);
			System.IO.FileInfo[] files = dir_session.GetFiles();
			foreach (System.IO.FileInfo File_Session in files)
			{
				DeleteOverTerm(File_Session, keepdays);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("\ud83d\udea8 세션 파일 오류 발생: " + ex.Message);
		}
	}

	private void DeleteCache(string folder, string keepdays)
	{
		try
		{
			DirectoryInfo dir_folder = new DirectoryInfo(folder);
			DirectoryInfo[] directories = dir_folder.GetDirectories();
			foreach (DirectoryInfo subfolder in directories)
			{
				System.IO.FileInfo[] files = subfolder.GetFiles();
				foreach (System.IO.FileInfo subFile in files)
				{
					DeleteOverTerm(subFile, keepdays);
				}
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("\ud83d\udea8 캐시 파일 오류 발생: " + ex.Message);
		}
	}

	private void DeleteOverTerm(System.IO.FileInfo File, string keepdays)
	{
		int calday = (DateTime.Today - File.LastWriteTime).Days;
		if (calday > int.Parse(keepdays))
		{
			File.Delete();
		}
	}

	public void StartElapse()
	{
		stopWatch.Start();
		startTime = stopWatch.ElapsedMilliseconds;
	}

	public long EndElapse()
	{
		long nowTime = stopWatch.ElapsedMilliseconds;
		diffTime = nowTime - startTime;
		return diffTime;
	}

	public bool CheckPassword()
	{
		if (!ServiceProvider.Settings.Branding.ControlPassword)
		{
			return true;
		}
		string inputPassword = Interaction.InputBox("비밀번호를 입력하세요:", "비밀번호 확인");
		if (string.IsNullOrEmpty(inputPassword))
		{
			return false;
		}
		if (ServiceProvider.Settings.PresetPassword != inputPassword)
		{
			MessageBox.Show("\ud83e\udd14올바른 비밀번호를 입력하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return false;
		}
		return true;
	}

	public string GetPortByKeywords(params string[] keywords)
	{
		using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
		{
			foreach (ManagementBaseObject obj in searcher.Get())
			{
				string name = obj["Name"]?.ToString();
				string deviceId = obj["DeviceID"]?.ToString();
				List<string> targets = new List<string>();
				if (!string.IsNullOrEmpty(name))
				{
					targets.Add(name);
				}
				if (!string.IsNullOrEmpty(deviceId))
				{
					targets.Add(deviceId);
				}
				if (!targets.Any((string t) => keywords.Any((string k) => t.ToUpper().Contains(k.ToUpper()))))
				{
					continue;
				}
				int start = name.IndexOf("(COM");
				if (start >= 0)
				{
					int end = name.IndexOf(")", start);
					if (end > start)
					{
						return name.Substring(start + 1, end - start - 1);
					}
				}
			}
		}
		return null;
	}
}
