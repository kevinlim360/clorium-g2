using System.Windows.Media;
using System.Xml.Serialization;
using CameraControl.Devices.Classes;
using Newtonsoft.Json;

namespace CameraControl.Core.Classes;

public class LiveviewSettings : BaseFieldClass
{
	[XmlIgnore]
	[JsonIgnore]
	private double _canvasWidt;

	[XmlIgnore]
	[JsonIgnore]
	private double _canvasHeight;

	[XmlIgnore]
	[JsonIgnore]
	private double _gridVerticalMinSize;

	[XmlIgnore]
	[JsonIgnore]
	private double _gridVerticalMaxSize;

	private double _gridVerticalMin;

	private double _gridVerticalMax;

	[XmlIgnore]
	[JsonIgnore]
	private double _gridHorizontalMinSize;

	[XmlIgnore]
	[JsonIgnore]
	private double _gridHorizontalMaxSize;

	private double _gridHorizontalMin;

	private double _gridHorizontalMax;

	private bool _gridVisible;

	private int _snapshotCaptureCount;

	private int _snapshotTimeCapture;

	private Color _gridColor;

	public string SelectedOverlay { get; set; }

	public bool BlackAndWhite { get; set; }

	public bool Invert { get; set; }

	public bool EdgeDetection { get; set; }

	public bool HighlightOverExp { get; set; }

	public bool HighlightUnderExp { get; set; }

	public int RotationIndex { get; set; }

	public int MotionThreshold { get; set; }

	public int WaitForMotionSec { get; set; }

	public bool MotionAutofocusBeforCapture { get; set; }

	public bool DetectMotion { get; set; }

	public bool DetectMotionArea { get; set; }

	public int MotionAction { get; set; }

	public int MotionMovieLength { get; set; }

	public bool ShowFocusRect { get; set; }

	public bool ShowLeftTab { get; set; }

	public bool NoProcessing { get; set; }

	public int Brightness { get; set; }

	public int HorizontalMin { get; set; }

	public int HorizontalMax { get; set; }

	public int VerticalMin { get; set; }

	public int VerticalMax { get; set; }

	public bool ShowRuler { get; set; }

	public bool FlipImage { get; set; }

	public int PreviewTime { get; set; }

	public int CropRatio { get; set; }

	public int CaptureCount { get; set; }

	public int CaptureDelay { get; set; }

	public double CanvasWidt
	{
		get
		{
			return _canvasWidt;
		}
		set
		{
			_canvasWidt = value;
			GridHorizontalMinSize = CanvasWidt * _gridHorizontalMin / 100.0;
			GridHorizontalMaxSize = CanvasWidt * _gridHorizontalMax / 100.0;
		}
	}

	public double CanvasHeight
	{
		get
		{
			return _canvasHeight;
		}
		set
		{
			_canvasHeight = value;
			GridVerticalMaxSize = CanvasHeight * (100.0 - _gridVerticalMax) / 100.0;
			GridVerticalMinSize = CanvasHeight * (100.0 - _gridVerticalMin) / 100.0;
		}
	}

	public double GridVerticalMinSize
	{
		get
		{
			return _gridVerticalMinSize;
		}
		set
		{
			_gridVerticalMinSize = value;
			NotifyPropertyChanged("GridVerticalMinSize");
		}
	}

	public double GridVerticalMaxSize
	{
		get
		{
			return _gridVerticalMaxSize;
		}
		set
		{
			_gridVerticalMaxSize = value;
			NotifyPropertyChanged("GridVerticalMaxSize");
		}
	}

	public double GridVerticalMin
	{
		get
		{
			return _gridVerticalMin;
		}
		set
		{
			_gridVerticalMin = value;
			GridVerticalMinSize = CanvasHeight * (100.0 - _gridVerticalMin) / 100.0;
			NotifyPropertyChanged("GridVerticalMin");
		}
	}

	public double GridVerticalMax
	{
		get
		{
			return _gridVerticalMax;
		}
		set
		{
			_gridVerticalMax = value;
			GridVerticalMaxSize = CanvasHeight * (100.0 - _gridVerticalMax) / 100.0;
			NotifyPropertyChanged("GridVerticalMax");
		}
	}

	public double GridHorizontalMinSize
	{
		get
		{
			return _gridHorizontalMinSize;
		}
		set
		{
			_gridHorizontalMinSize = value;
			NotifyPropertyChanged("GridHorizontalMinSize");
		}
	}

	public double GridHorizontalMaxSize
	{
		get
		{
			return _gridHorizontalMaxSize;
		}
		set
		{
			_gridHorizontalMaxSize = value;
			NotifyPropertyChanged("GridHorizontalMaxSize");
		}
	}

	public double GridHorizontalMin
	{
		get
		{
			return _gridHorizontalMin;
		}
		set
		{
			_gridHorizontalMin = value;
			GridHorizontalMinSize = CanvasWidt * _gridHorizontalMin / 100.0;
			NotifyPropertyChanged("GridHorizontalMin");
		}
	}

	public double GridHorizontalMax
	{
		get
		{
			return _gridHorizontalMax;
		}
		set
		{
			_gridHorizontalMax = value;
			GridHorizontalMaxSize = CanvasWidt * _gridHorizontalMax / 100.0;
			NotifyPropertyChanged("GridHorizontalMax");
		}
	}

	public bool GridVisible
	{
		get
		{
			return _gridVisible;
		}
		set
		{
			_gridVisible = value;
			NotifyPropertyChanged("GridVisible");
		}
	}

	public int SnapshotCaptureCount
	{
		get
		{
			return _snapshotCaptureCount;
		}
		set
		{
			_snapshotCaptureCount = value;
			NotifyPropertyChanged("SnapshotCaptureCount");
		}
	}

	public int SnapshotCaptureTime
	{
		get
		{
			return _snapshotTimeCapture;
		}
		set
		{
			_snapshotTimeCapture = value;
			NotifyPropertyChanged("SnapshotCaptureTime");
		}
	}

	public Color GridColor
	{
		get
		{
			return _gridColor;
		}
		set
		{
			_gridColor = value;
			NotifyPropertyChanged("GridColor");
		}
	}

	public LiveviewSettings()
	{
		GridVisible = false;
		BlackAndWhite = false;
		EdgeDetection = false;
		HighlightOverExp = false;
		HighlightUnderExp = false;
		MotionThreshold = 20;
		ShowFocusRect = true;
		ShowLeftTab = true;
		NoProcessing = false;
		HorizontalMin = 0;
		HorizontalMax = 100;
		VerticalMin = 0;
		VerticalMax = 100;
		FlipImage = false;
		CropRatio = 0;
		MotionMovieLength = 30;
		CaptureCount = 1;
		SnapshotCaptureCount = 1;
		SnapshotCaptureTime = 500;
		GridColor = Colors.Gray;
	}
}
