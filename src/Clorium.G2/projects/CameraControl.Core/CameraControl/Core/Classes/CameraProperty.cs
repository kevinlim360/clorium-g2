using System.Windows;
using System.Xml.Serialization;
using CameraControl.Devices.Classes;
using Newtonsoft.Json;

namespace CameraControl.Core.Classes;

public class CameraProperty : BaseFieldClass
{
	private string _serialNumber;

	private string _deviceName;

	private string _profileNmae;

	private string _defaultPresetName;

	private bool _noDownload;

	private int _counterInc;

	private bool _captureInSdRam;

	private int _counter;

	private bool _useExternalShutter;

	[XmlIgnore]
	[JsonIgnore]
	private CustomConfig _selectedConfig;

	private string _selectedConfigName;

	private LiveviewSettings _liveviewSettings;

	private int _sortOrder;

	private WindowCommandItem _keyTrigger;

	private WindowCommandItem _keyTriggerNoAF;

	private int _delay;

	private bool _saveLiveViewWindow;

	public Rect WindowRect { get; set; }

	public string SerialNumber
	{
		get
		{
			return _serialNumber;
		}
		set
		{
			_serialNumber = value;
			NotifyPropertyChanged("SerialNumber");
		}
	}

	public string DeviceName
	{
		get
		{
			return _deviceName;
		}
		set
		{
			_deviceName = value;
			NotifyPropertyChanged("DeviceName");
		}
	}

	public string PhotoSessionName
	{
		get
		{
			return _profileNmae;
		}
		set
		{
			_profileNmae = value;
			NotifyPropertyChanged("PhotoSessionName");
		}
	}

	public string DefaultPresetName
	{
		get
		{
			return _defaultPresetName;
		}
		set
		{
			_defaultPresetName = value;
			NotifyPropertyChanged("PhotoSessionName");
		}
	}

	[XmlIgnore]
	[JsonIgnore]
	public PhotoSession PhotoSession { get; set; }

	public bool NoDownload
	{
		get
		{
			return _noDownload;
		}
		set
		{
			_noDownload = value;
			NotifyPropertyChanged("NoDownload");
		}
	}

	public int CounterInc
	{
		get
		{
			if (_counterInc < 1)
			{
				_counterInc = 1;
			}
			return _counterInc;
		}
		set
		{
			_counterInc = value;
			NotifyPropertyChanged("CounterInc");
		}
	}

	public bool CaptureInSdRam
	{
		get
		{
			return _captureInSdRam;
		}
		set
		{
			_captureInSdRam = value;
			NotifyPropertyChanged("CaptureInSdRam");
		}
	}

	public int Counter
	{
		get
		{
			return _counter;
		}
		set
		{
			_counter = value;
			NotifyPropertyChanged("Counter");
		}
	}

	public bool UseExternalShutter
	{
		get
		{
			return _useExternalShutter;
		}
		set
		{
			_useExternalShutter = value;
			NotifyPropertyChanged("UseExternalShutter");
		}
	}

	public CustomConfig SelectedConfig
	{
		get
		{
			if (_selectedConfig == null)
			{
				return _selectedConfig;
			}
			foreach (CustomConfig config in ServiceProvider.ExternalDeviceManager.ExternalShutters)
			{
				if (config.Name == SelectedConfigName)
				{
					_selectedConfig = config;
				}
			}
			return _selectedConfig;
		}
		set
		{
			_selectedConfig = value;
			SelectedConfigName = ((_selectedConfig == null) ? "" : value.Name);
			NotifyPropertyChanged("SelectedConfig");
		}
	}

	public string SelectedConfigName
	{
		get
		{
			return _selectedConfigName;
		}
		set
		{
			_selectedConfigName = value;
			NotifyPropertyChanged("SelectedConfigName");
		}
	}

	public LiveviewSettings LiveviewSettings
	{
		get
		{
			return _liveviewSettings;
		}
		set
		{
			_liveviewSettings = value;
			NotifyPropertyChanged("LiveviewSettings");
		}
	}

	public int SortOrder
	{
		get
		{
			return _sortOrder;
		}
		set
		{
			_sortOrder = value;
			NotifyPropertyChanged("SortOrder");
		}
	}

	public int Delay
	{
		get
		{
			return _delay;
		}
		set
		{
			_delay = value;
			NotifyPropertyChanged("Delay");
		}
	}

	public WindowCommandItem KeyTrigger
	{
		get
		{
			return _keyTrigger;
		}
		set
		{
			_keyTrigger = value;
			NotifyPropertyChanged("KeyTrigger");
		}
	}

	public WindowCommandItem KeyTriggerNoAF
	{
		get
		{
			return _keyTriggerNoAF;
		}
		set
		{
			_keyTriggerNoAF = value;
			NotifyPropertyChanged("KeyTriggerNoAF");
		}
	}

	public bool SaveLiveViewWindow
	{
		get
		{
			return _saveLiveViewWindow;
		}
		set
		{
			_saveLiveViewWindow = value;
			NotifyPropertyChanged("SaveLiveViewWindow");
		}
	}

	public CameraProperty()
	{
		NoDownload = false;
		CaptureInSdRam = true;
		Counter = 0;
		LiveviewSettings = new LiveviewSettings();
		KeyTrigger = new WindowCommandItem();
		KeyTriggerNoAF = new WindowCommandItem();
		SaveLiveViewWindow = true;
		WindowRect = default;
	}
}
