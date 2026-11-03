using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Accord;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Vision.Motion;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.Devices.Others;
using CameraControl.windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using WebEye;

namespace CameraControl.ViewModel;

public class LiveViewViewModel : ViewModelBase
{
	private Window _window;

	private LiveViewFullScreenWnd FullScreenWnd;

	private LiveViewVideoSource _liveViewVideoSource = null;

	private StreamPlayerControl _videoSource = new StreamPlayerControl();

	private MotionDetector _detector;

	private ICameraDevice _cameraDevice;

	private static CameraProperty _cameraProperty;

	private DateTime _framestart;

	private DateTime _photoCapturedTime = DateTime.MinValue;

	private DateTime _restartTimerStartTime;

	private DateTime _recordStartTime;

	private System.Timers.Timer _timer = new System.Timers.Timer(50.0);

	private System.Timers.Timer _restartTimer = new System.Timers.Timer(1000.0);

	private System.Timers.Timer _freezeTimer = new System.Timers.Timer();

	private const int DesiredFrameRate = 20;

	private int _totalframes = 0;

	private int _fps;

	private int _recordLength = 0;

	private bool _recording;

	private string _recButtonText;

	private AsyncObservableCollection<ValuePair> _overlays;

	private WriteableBitmap _overlayImage = null;

	private string _lastOverlay = string.Empty;

	private bool _overlayUseLastCaptured;

	private bool _overlayActivated;

	private int _overlayScale;

	private int _overlayHorizontal;

	private int _overlayVertical;

	private int _overlayTransparency;

	private string _overlayloadedFilePath = string.Empty;

	private string _localSelectedOverlay = null;

	private bool _focusIProgress = false;

	private int _focusValue;

	private int _focusCounter;

	private int _focusProgressMax;

	private int _focusProgressValue;

	private bool _focusProgressVisible;

	private int _selectedFocusValue;

	private bool _simpleManualFocus;

	private bool _autoFocusBeforCapture;

	private BitmapSource _bitmap;

	private BitmapSource _preview;

	private BitmapSource _previewBitmap;

	private bool _previewBitmapVisible;

	private bool _freezeImage;

	private AsyncObservableCollection<string> _grids;

	private int _gridType;

	private bool _triggerOnMotion;

	private double _currentMotionIndex;

	private PointCollection _luminanceHistogramPoints = null;

	private PointCollection _redColorHistogramPoints;

	private PointCollection _greenColorHistogramPoints;

	private PointCollection _blueColorHistogramPoints;

	private string _title;

	private string _counterMessage;

	private bool _isMinized;

	private bool _stayOnTop;

	private bool _operInProgress = false;

	private bool _lockA;

	private bool _lockB;

	private int _rotation;

	private bool _settingArea;

	private int _countDown;

	private bool _countDownVisible;

	private bool _captureInProgress;

	private bool _delayedStart;

	private int _levelAngle;

	private string _levelAngleColor;

	private decimal _movieTimeRemain;

	private int _soundL;

	private int _soundR;

	private int _peakSoundL;

	private int _peakSoundR;

	private bool _haveSoundData;

	private WriteableBitmap writeableBitmap;

	private int _timer_Elapsed_count = 0;

	private int _frame_count = 0;

	public int writeableBitmapPixelWidth = 0;

	public int writeableBitmapPixelHeight = 0;

	public int orgImageWidth = 0;

	public int orgImageHeight = 0;

	public bool MeasureTilt = false;

	public bool MeasureCenter = false;

	private string _calibrate;

	private string _boxwidth_mm;

	private string _boxheight_mm;

	private string _tilt_deg;

	private string _center_disp;

	private string _direction_deg;

	private string _scale_mm;

	private double _lastAvgX = -1.0;

	private double _lastAvgY = -1.0;

	private double _lastBView = -1.0;

	public double _centerX = 0.0;

	public double _centerY = 0.0;

	public LiveViewData LiveViewData { get; set; }

	public bool ShowHistogram { get; set; }

	public bool CaptureCancelRequested { get; set; }

	public bool IsActive { get; set; }

	public bool ZoomSupported => CameraDevice.GetCapability(CapabilityEnum.Zoom);

	public RelayCommand AutoFocusCommand { get; set; }

	public RelayCommand RecordMovieCommand { get; set; }

	public RelayCommand CaptureCommand { get; set; }

	public RelayCommand CancelCaptureCommand { get; set; }

	public RelayCommand PreviewCommand { get; set; }

	public RelayCommand FocusPCommand { get; set; }

	public RelayCommand FocusPPCommand { get; set; }

	public RelayCommand FocusPPPCommand { get; set; }

	public RelayCommand FocusMCommand { get; set; }

	public RelayCommand FocusMMCommand { get; set; }

	public RelayCommand FocusMMMCommand { get; set; }

	public RelayCommand StartLiveViewCommand { get; set; }

	public RelayCommand StopLiveViewCommand { get; set; }

	public RelayCommand ResetBrigthnessCommand { get; set; }

	public RelayCommand BrowseOverlayCommand { get; set; }

	public RelayCommand ResetOverlayCommand { get; set; }

	public RelayCommand ZoomOutCommand { get; set; }

	public RelayCommand ZoomInCommand { get; set; }

	public RelayCommand ZoomIn100 { get; set; }

	public RelayCommand ToggleGridCommand { get; set; }

	public RelayCommand LockCurrentNearCommand { get; set; }

	public RelayCommand LockCurrentFarCommand { get; set; }

	public RelayCommand ResetConditionCommand { get; set; }

	public RelayCommand FullScreenCommand { get; set; }

	public RelayCommand ClosePreviewCommand { get; set; }

	public RelayCommand CaptureSnapshotCommand { get; set; }

	public RelayCommand CalibrateCommand { get; set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<object> DeleteOverlayItemCommand { get; set; }

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
			RaisePropertyChanged(() => ZoomSupported);
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

	public BitmapSource PreviewBitmap
	{
		get
		{
			return _previewBitmap;
		}
		set
		{
			_previewBitmap = value;
			RaisePropertyChanged(() => PreviewBitmap);
			PreviewBitmapVisible = true;
		}
	}

	public bool PreviewBitmapVisible
	{
		get
		{
			return _previewBitmapVisible;
		}
		set
		{
			_previewBitmapVisible = value;
			RaisePropertyChanged(() => PreviewBitmapVisible);
		}
	}

	public int LevelAngle
	{
		get
		{
			return _levelAngle;
		}
		set
		{
			_levelAngle = value;
			RaisePropertyChanged(() => LevelAngle);
			LevelAngleColor = ((_levelAngle % 90 <= 1 || _levelAngle % 90 >= 89) ? "Green" : "Red");
		}
	}

	public string LevelAngleColor
	{
		get
		{
			return _levelAngleColor;
		}
		set
		{
			_levelAngleColor = value;
			RaisePropertyChanged(() => LevelAngleColor);
		}
	}

	public bool IsMinized
	{
		get
		{
			return _isMinized;
		}
		set
		{
			_isMinized = value;
			RaisePropertyChanged(() => IsMinized);
		}
	}

	public int GridType
	{
		get
		{
			return _gridType;
		}
		set
		{
			_gridType = value;
			RaisePropertyChanged(() => GridType);
		}
	}

	public AsyncObservableCollection<string> Grids
	{
		get
		{
			return _grids;
		}
		set
		{
			_grids = value;
			RaisePropertyChanged(() => Grids);
		}
	}

	public AsyncObservableCollection<ValuePair> Overlays
	{
		get
		{
			return _overlays;
		}
		set
		{
			_overlays = value;
			RaisePropertyChanged(() => Overlays);
		}
	}

	public string SelectedOverlay
	{
		get
		{
			if (_localSelectedOverlay == null)
			{
				_localSelectedOverlay = CameraProperty.LiveviewSettings.SelectedOverlay;
			}
			return _localSelectedOverlay;
		}
		set
		{
			CameraProperty.LiveviewSettings.SelectedOverlay = value;
			_localSelectedOverlay = value;
			_overlayImage = null;
			_overlayloadedFilePath = string.Empty;
			RaisePropertyChanged(() => SelectedOverlay);
		}
	}

	public bool OverlayActivated
	{
		get
		{
			return _overlayActivated;
		}
		set
		{
			_overlayActivated = value;
			RaisePropertyChanged(() => OverlayActivated);
		}
	}

	public int OverlayScale
	{
		get
		{
			return _overlayScale;
		}
		set
		{
			_overlayScale = value;
			RaisePropertyChanged(() => OverlayScale);
		}
	}

	public int OverlayHorizontal
	{
		get
		{
			return _overlayHorizontal;
		}
		set
		{
			_overlayHorizontal = value;
			RaisePropertyChanged(() => OverlayHorizontal);
		}
	}

	public int OverlayVertical
	{
		get
		{
			return _overlayVertical;
		}
		set
		{
			_overlayVertical = value;
			RaisePropertyChanged(() => OverlayVertical);
		}
	}

	public int OverlayTransparency
	{
		get
		{
			return _overlayTransparency;
		}
		set
		{
			_overlayTransparency = value;
			RaisePropertyChanged(() => OverlayTransparency);
		}
	}

	public bool OverlayUseLastCaptured
	{
		get
		{
			return _overlayUseLastCaptured;
		}
		set
		{
			_overlayUseLastCaptured = value;
			RaisePropertyChanged(() => OverlayUseLastCaptured);
		}
	}

	public bool BlackAndWhite
	{
		get
		{
			return CameraProperty.LiveviewSettings.BlackAndWhite;
		}
		set
		{
			CameraProperty.LiveviewSettings.BlackAndWhite = value;
			RaisePropertyChanged(() => BlackAndWhite);
		}
	}

	public bool Invert
	{
		get
		{
			return CameraProperty.LiveviewSettings.Invert;
		}
		set
		{
			CameraProperty.LiveviewSettings.Invert = value;
			RaisePropertyChanged(() => Invert);
		}
	}

	public bool EdgeDetection
	{
		get
		{
			return CameraProperty.LiveviewSettings.EdgeDetection;
		}
		set
		{
			CameraProperty.LiveviewSettings.EdgeDetection = value;
			RaisePropertyChanged(() => EdgeDetection);
		}
	}

	public int RotationIndex
	{
		get
		{
			return CameraProperty.LiveviewSettings.RotationIndex;
		}
		set
		{
			CameraProperty.LiveviewSettings.RotationIndex = value;
			RaisePropertyChanged(() => RotationIndex);
		}
	}

	public bool FreezeImage
	{
		get
		{
			return _freezeImage;
		}
		set
		{
			_freezeImage = value;
			if (_freezeImage)
			{
				_freezeTimer.Stop();
				_freezeTimer.Start();
			}
			RaisePropertyChanged(() => FreezeImage);
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

	public int PreviewTime
	{
		get
		{
			return CameraProperty.LiveviewSettings.PreviewTime;
		}
		set
		{
			CameraProperty.LiveviewSettings.PreviewTime = value;
			RaisePropertyChanged(() => PreviewTime);
		}
	}

	public int Brightness
	{
		get
		{
			return CameraProperty.LiveviewSettings.Brightness;
		}
		set
		{
			CameraProperty.LiveviewSettings.Brightness = value;
			RaisePropertyChanged(() => Brightness);
		}
	}

	public int CropRatio
	{
		get
		{
			return CameraProperty.LiveviewSettings.CropRatio;
		}
		set
		{
			CameraProperty.LiveviewSettings.CropRatio = value;
		}
	}

	public int CropOffsetX { get; set; }

	public int CropOffsetY { get; set; }

	public BitmapSource Preview
	{
		get
		{
			return _preview;
		}
		set
		{
			_preview = value;
			RaisePropertyChanged(() => Preview);
		}
	}

	public double CurrentMotionIndex
	{
		get
		{
			return _currentMotionIndex;
		}
		set
		{
			_currentMotionIndex = value;
			RaisePropertyChanged(() => CurrentMotionIndex);
		}
	}

	public int MotionThreshold
	{
		get
		{
			return CameraProperty.LiveviewSettings.MotionThreshold;
		}
		set
		{
			CameraProperty.LiveviewSettings.MotionThreshold = value;
			RaisePropertyChanged(() => MotionThreshold);
		}
	}

	public bool TriggerOnMotion
	{
		get
		{
			return _triggerOnMotion;
		}
		set
		{
			_triggerOnMotion = value;
			RaisePropertyChanged(() => TriggerOnMotion);
		}
	}

	public int WaitForMotionSec
	{
		get
		{
			return CameraProperty.LiveviewSettings.WaitForMotionSec;
		}
		set
		{
			CameraProperty.LiveviewSettings.WaitForMotionSec = value;
		}
	}

	public bool MotionAutofocusBeforCapture
	{
		get
		{
			return CameraProperty.LiveviewSettings.MotionAutofocusBeforCapture;
		}
		set
		{
			CameraProperty.LiveviewSettings.MotionAutofocusBeforCapture = value;
		}
	}

	public bool DetectMotion
	{
		get
		{
			return CameraProperty.LiveviewSettings.DetectMotion;
		}
		set
		{
			CameraProperty.LiveviewSettings.DetectMotion = value;
			RaisePropertyChanged(() => DetectMotion);
		}
	}

	public bool DetectMotionArea
	{
		get
		{
			return CameraProperty.LiveviewSettings.DetectMotionArea;
		}
		set
		{
			CameraProperty.LiveviewSettings.DetectMotionArea = value;
			if (_detector != null)
			{
				_detector.Reset();
			}
			if (value)
			{
				ShowRuler = true;
			}
			else
			{
				ShowRuler = false;
			}
		}
	}

	public int MotionAction
	{
		get
		{
			return CameraProperty.LiveviewSettings.MotionAction;
		}
		set
		{
			CameraProperty.LiveviewSettings.MotionAction = value;
			RaisePropertyChanged(() => MotionAction);
		}
	}

	public int MotionMovieLength
	{
		get
		{
			return CameraProperty.LiveviewSettings.MotionMovieLength;
		}
		set
		{
			CameraProperty.LiveviewSettings.MotionMovieLength = value;
			RaisePropertyChanged(() => MotionMovieLength);
		}
	}

	public bool FlipImage
	{
		get
		{
			return CameraProperty.LiveviewSettings.FlipImage;
		}
		set
		{
			CameraProperty.LiveviewSettings.FlipImage = value;
		}
	}

	public PointCollection LuminanceHistogramPoints
	{
		get
		{
			return _luminanceHistogramPoints;
		}
		set
		{
			if (_luminanceHistogramPoints != value)
			{
				_luminanceHistogramPoints = value;
				RaisePropertyChanged(() => LuminanceHistogramPoints);
			}
		}
	}

	public PointCollection RedColorHistogramPoints
	{
		get
		{
			return _redColorHistogramPoints;
		}
		set
		{
			_redColorHistogramPoints = value;
			RaisePropertyChanged(() => RedColorHistogramPoints);
		}
	}

	public PointCollection GreenColorHistogramPoints
	{
		get
		{
			return _greenColorHistogramPoints;
		}
		set
		{
			_greenColorHistogramPoints = value;
			RaisePropertyChanged(() => GreenColorHistogramPoints);
		}
	}

	public PointCollection BlueColorHistogramPoints
	{
		get
		{
			return _blueColorHistogramPoints;
		}
		set
		{
			_blueColorHistogramPoints = value;
			RaisePropertyChanged(() => BlueColorHistogramPoints);
		}
	}

	public bool HighlightOverExp
	{
		get
		{
			return CameraProperty.LiveviewSettings.HighlightOverExp;
		}
		set
		{
			CameraProperty.LiveviewSettings.HighlightOverExp = value;
			RaisePropertyChanged(() => HighlightOverExp);
		}
	}

	public bool HighlightUnderExp
	{
		get
		{
			return CameraProperty.LiveviewSettings.HighlightUnderExp;
		}
		set
		{
			CameraProperty.LiveviewSettings.HighlightUnderExp = value;
			RaisePropertyChanged(() => HighlightUnderExp);
		}
	}

	public bool IsFree => !CaptureInProgress;

	public int FocusValue
	{
		get
		{
			return _focusValue;
		}
		set
		{
			_focusValue = value;
			RaisePropertyChanged(() => FocusValue);
			RaisePropertyChanged(() => CounterMessage);
		}
	}

	public int SelectedFocusValue
	{
		get
		{
			return _selectedFocusValue;
		}
		set
		{
			if (_selectedFocusValue != value)
			{
				SetFocus(value - _selectedFocusValue);
				_selectedFocusValue = value;
				RaisePropertyChanged(() => SelectedFocusValue);
			}
		}
	}

	public int FocusCounter
	{
		get
		{
			return _focusCounter;
		}
		set
		{
			_selectedFocusValue = value;
			_focusCounter = value;
			RaisePropertyChanged(() => FocusCounter);
			RaisePropertyChanged(() => CounterMessage);
			RaisePropertyChanged(() => SelectedFocusValue);
		}
	}

	public string CounterMessage
	{
		get
		{
			if (!LockA && !LockB)
			{
				return "?";
			}
			if (LockA && !LockB)
			{
				return FocusCounter.ToString();
			}
			if (LockB)
			{
				return FocusCounter + "/" + FocusValue;
			}
			return _counterMessage;
		}
		set
		{
			_counterMessage = value;
			RaisePropertyChanged(() => CounterMessage);
		}
	}

	public bool FocusingEnabled => !_focusIProgress;

	public bool LockA
	{
		get
		{
			return _lockA;
		}
		set
		{
			_lockA = value;
			if (_lockA && !LockB)
			{
				FocusCounter = 0;
				FocusValue = 0;
				LockB = false;
			}
			RaisePropertyChanged(() => LockA);
			RaisePropertyChanged(() => CounterMessage);
		}
	}

	public bool LockB
	{
		get
		{
			return _lockB;
		}
		set
		{
			_lockB = value;
			if (_lockB && LockA)
			{
				FocusValue = FocusCounter;
			}
			if (!_lockA && LockB)
			{
				FocusCounter = 0;
				FocusValue = 0;
			}
			RaisePropertyChanged(() => LockB);
			RaisePropertyChanged(() => CounterMessage);
		}
	}

	public bool ShowFocusRect
	{
		get
		{
			return CameraProperty.LiveviewSettings.ShowFocusRect;
		}
		set
		{
			CameraProperty.LiveviewSettings.ShowFocusRect = value;
			RaisePropertyChanged(() => ShowFocusRect);
		}
	}

	public bool ShowLeftTab
	{
		get
		{
			return CameraProperty.LiveviewSettings.ShowLeftTab;
		}
		set
		{
			CameraProperty.LiveviewSettings.ShowLeftTab = value;
			RaisePropertyChanged(() => ShowLeftTab);
		}
	}

	public bool NoProcessing
	{
		get
		{
			return CameraProperty.LiveviewSettings.NoProcessing;
		}
		set
		{
			CameraProperty.LiveviewSettings.NoProcessing = value;
			RaisePropertyChanged(() => NoProcessing);
			RaisePropertyChanged(() => NoProcessingWarnColor);
		}
	}

	public string NoProcessingWarnColor => CameraProperty.LiveviewSettings.NoProcessing ? "Red" : "Transparent";

	public bool DelayedStart
	{
		get
		{
			return _delayedStart;
		}
		set
		{
			_delayedStart = value;
			RaisePropertyChanged(() => DelayedStart);
		}
	}

	public int SoundL
	{
		get
		{
			return _soundL;
		}
		set
		{
			_soundL = value;
			RaisePropertyChanged(() => SoundL);
		}
	}

	public int SoundR
	{
		get
		{
			return _soundR;
		}
		set
		{
			_soundR = value;
			RaisePropertyChanged(() => SoundR);
		}
	}

	public int PeakSoundL
	{
		get
		{
			return _peakSoundL;
		}
		set
		{
			_peakSoundL = value;
			RaisePropertyChanged(() => PeakSoundL);
		}
	}

	public int PeakSoundR
	{
		get
		{
			return _peakSoundR;
		}
		set
		{
			_peakSoundR = value;
			RaisePropertyChanged(() => PeakSoundR);
		}
	}

	public bool HaveSoundData
	{
		get
		{
			return _haveSoundData;
		}
		set
		{
			_haveSoundData = value;
			RaisePropertyChanged(() => HaveSoundData);
		}
	}

	public int Fps
	{
		get
		{
			return _fps;
		}
		set
		{
			_fps = value;
			RaisePropertyChanged(() => Fps);
		}
	}

	public string RecButtonText
	{
		get
		{
			return _recButtonText;
		}
		set
		{
			_recButtonText = value;
			RaisePropertyChanged(() => RecButtonText);
		}
	}

	public bool Recording
	{
		get
		{
			return _recording;
		}
		set
		{
			_recording = value;
			RecButtonText = (_recording ? TranslationStrings.ButtonRecordStopMovie : TranslationStrings.ButtonRecordMovie);
			RaisePropertyChanged(() => Recording);
		}
	}

	public decimal MovieTimeRemain
	{
		get
		{
			return _movieTimeRemain;
		}
		set
		{
			_movieTimeRemain = value;
			RaisePropertyChanged(() => MovieTimeRemain);
		}
	}

	public bool CountDownVisible
	{
		get
		{
			return _countDownVisible;
		}
		set
		{
			_countDownVisible = value;
			RaisePropertyChanged(() => CountDownVisible);
		}
	}

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			RaisePropertyChanged(() => Title);
		}
	}

	public bool CaptureInProgress
	{
		get
		{
			return _captureInProgress;
		}
		set
		{
			_captureInProgress = value;
			RaisePropertyChanged(() => CaptureInProgress);
			RaisePropertyChanged(() => IsFree);
		}
	}

	public int SnapshotCaptureTime
	{
		get
		{
			return CameraProperty.LiveviewSettings.SnapshotCaptureTime;
		}
		set
		{
			CameraProperty.LiveviewSettings.SnapshotCaptureTime = value;
			RaisePropertyChanged(() => SnapshotCaptureTime);
		}
	}

	public int SnapshotCaptureCount
	{
		get
		{
			return CameraProperty.LiveviewSettings.SnapshotCaptureCount;
		}
		set
		{
			CameraProperty.LiveviewSettings.SnapshotCaptureCount = value;
			RaisePropertyChanged(() => SnapshotCaptureCount);
		}
	}

	public Rect RullerRect => new Rect(HorizontalMin, VerticalMin, HorizontalMax, VerticalMax);

	public int HorizontalMin
	{
		get
		{
			return CameraProperty.LiveviewSettings.HorizontalMin;
		}
		set
		{
			CameraProperty.LiveviewSettings.HorizontalMin = value;
			RaisePropertyChanged(() => RullerRect);
		}
	}

	public int HorizontalMax
	{
		get
		{
			return CameraProperty.LiveviewSettings.HorizontalMax;
		}
		set
		{
			CameraProperty.LiveviewSettings.HorizontalMax = value;
			RaisePropertyChanged(() => RullerRect);
		}
	}

	public int VerticalMin
	{
		get
		{
			return CameraProperty.LiveviewSettings.VerticalMin;
		}
		set
		{
			CameraProperty.LiveviewSettings.VerticalMin = value;
			RaisePropertyChanged(() => RullerRect);
		}
	}

	public int VerticalMax
	{
		get
		{
			return CameraProperty.LiveviewSettings.VerticalMax;
		}
		set
		{
			CameraProperty.LiveviewSettings.VerticalMax = value;
			RaisePropertyChanged(() => RullerRect);
		}
	}

	public bool ShowRuler
	{
		get
		{
			return CameraProperty.LiveviewSettings.ShowRuler;
		}
		set
		{
			CameraProperty.LiveviewSettings.ShowRuler = value;
			RaisePropertyChanged(() => ShowRuler);
		}
	}

	public bool SettingArea
	{
		get
		{
			return _settingArea;
		}
		set
		{
			_settingArea = value;
			RaisePropertyChanged(() => SettingArea);
			RaisePropertyChanged(() => NoSettingArea);
		}
	}

	public System.Windows.Media.Color GridColor
	{
		get
		{
			return CameraProperty.LiveviewSettings.GridColor;
		}
		set
		{
			CameraProperty.LiveviewSettings.GridColor = value;
			RaisePropertyChanged(() => GridColor);
		}
	}

	public bool NoSettingArea => !SettingArea;

	public bool SimpleManualFocus
	{
		get
		{
			return _simpleManualFocus;
		}
		set
		{
			_simpleManualFocus = value;
			RaisePropertyChanged(() => SimpleManualFocus);
		}
	}

	public bool StayOnTop
	{
		get
		{
			return _stayOnTop;
		}
		set
		{
			_stayOnTop = value;
			RaisePropertyChanged(() => StayOnTop);
		}
	}

	public int CaptureDelay
	{
		get
		{
			return CameraProperty.LiveviewSettings.CaptureDelay;
		}
		set
		{
			CameraProperty.LiveviewSettings.CaptureDelay = value;
			RaisePropertyChanged(() => CaptureDelay);
		}
	}

	public int CaptureCount
	{
		get
		{
			return CameraProperty.LiveviewSettings.CaptureCount;
		}
		set
		{
			CameraProperty.LiveviewSettings.CaptureCount = value;
			RaisePropertyChanged(() => CaptureCount);
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
			RaisePropertyChanged(() => CountDown);
		}
	}

	public bool AutoFocusBeforCapture
	{
		get
		{
			return _autoFocusBeforCapture;
		}
		set
		{
			_autoFocusBeforCapture = value;
			RaisePropertyChanged(() => AutoFocusBeforCapture);
		}
	}

	public bool FocusProgressVisible
	{
		get
		{
			return _focusProgressVisible;
		}
		set
		{
			_focusProgressVisible = value;
			RaisePropertyChanged(() => FocusProgressVisible);
		}
	}

	public int FocusProgressMax
	{
		get
		{
			return _focusProgressMax;
		}
		set
		{
			_focusProgressMax = value;
			RaisePropertyChanged(() => FocusProgressMax);
		}
	}

	public int FocusProgressValue
	{
		get
		{
			return _focusProgressValue;
		}
		set
		{
			_focusProgressValue = value;
			RaisePropertyChanged(() => FocusProgressValue);
		}
	}

	public string CalibrateBase
	{
		get
		{
			return _calibrate;
		}
		set
		{
			_calibrate = value;
			ServiceProvider.Settings.DiskSize = double.Parse(value);
			RaisePropertyChanged(() => CalibrateBase);
		}
	}

	public string BoxWidth_mm
	{
		get
		{
			return _boxwidth_mm;
		}
		set
		{
			_boxwidth_mm = value;
			RaisePropertyChanged(() => BoxWidth_mm);
		}
	}

	public string BoxHeight_mm
	{
		get
		{
			return _boxheight_mm;
		}
		set
		{
			_boxheight_mm = value;
			RaisePropertyChanged(() => BoxHeight_mm);
		}
	}

	public string Tilt_Deg
	{
		get
		{
			return _tilt_deg;
		}
		set
		{
			_tilt_deg = value;
			RaisePropertyChanged(() => Tilt_Deg);
		}
	}

	public string Center_Disp
	{
		get
		{
			return _center_disp;
		}
		set
		{
			_center_disp = value;
			RaisePropertyChanged(() => Center_Disp);
		}
	}

	public string Direction_Deg
	{
		get
		{
			return _direction_deg;
		}
		set
		{
			_direction_deg = value;
			RaisePropertyChanged(() => Direction_Deg);
		}
	}

	public string Scale_mm
	{
		get
		{
			return _scale_mm;
		}
		set
		{
			_scale_mm = value;
			RaisePropertyChanged(() => Scale_mm);
		}
	}

	public static event EventHandler FocuseDone;

	public LiveViewViewModel()
	{
		CameraProperty = new CameraProperty();
		CameraDevice = new NotConnectedCameraDevice();
		InitCommands();
		PreviewBitmapVisible = true;
		_timer.Stop();
		_timer.AutoReset = true;
		_timer.Elapsed += Timer_Elapsed;
		IsActive = true;
	}

	public LiveViewViewModel(ICameraDevice device, Window window)
	{
		CameraDevice = device;
		_window = window;
		CameraProperty = device.LoadProperties();
		SimpleManualFocus = CameraDevice.GetCapability(CapabilityEnum.SimpleManualFocus);
		Title = TranslationStrings.LiveViewWindowTitle + " - " + CameraProperty.DeviceName;
		InitOverlay();
		InitCommands();
		if (CameraProperty.LiveviewSettings != null)
		{
			SelectedOverlay = CameraProperty.LiveviewSettings.SelectedOverlay;
		}
		if (ServiceProvider.Settings.DetectionType == 0)
		{
			_detector = new MotionDetector(new TwoFramesDifferenceDetector(suppressNoise: true), new BlobCountingObjectsProcessing(ServiceProvider.Settings.MotionBlockSize, ServiceProvider.Settings.MotionBlockSize, highlightMotionRegions: true));
		}
		else
		{
			_detector = new MotionDetector(new SimpleBackgroundModelingDetector(suppressNoise: true, keepObjectEdges: true), new BlobCountingObjectsProcessing(ServiceProvider.Settings.MotionBlockSize, ServiceProvider.Settings.MotionBlockSize, highlightMotionRegions: true));
		}
		TriggerOnMotion = false;
		ShowHistogram = false;
		PreviewBitmapVisible = false;
		Init();
		ServiceProvider.WindowsManager.Event += WindowsManagerEvent;
	}

	private void InitCommands()
	{
		StartLiveViewCommand = new RelayCommand(StartLiveView);
		StopLiveViewCommand = new RelayCommand(StopLiveView);
		CaptureCommand = new RelayCommand(CaptureInThread);
		PreviewCommand = new RelayCommand(CapturePreview);
		CancelCaptureCommand = new RelayCommand(() =>
		{
			CaptureCancelRequested = true;
		});
		RecordMovieCommand = new RelayCommand(() =>
		{
			PreviewBitmapVisible = false;
			if (Recording)
			{
				StopRecordMovie();
			}
			else
			{
				RecordMovie();
			}
		}, () => CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled && CameraDevice.GetCapability(CapabilityEnum.RecordMovie));
		AutoFocusCommand = new RelayCommand(AutoFocus);
		FocusMCommand = new RelayCommand(() =>
		{
			SetFocus(SimpleManualFocus ? (-ServiceProvider.Settings.SmallFocusStepCanon) : (-ServiceProvider.Settings.SmalFocusStep));
		});
		FocusMMCommand = new RelayCommand(() =>
		{
			SetFocus(SimpleManualFocus ? (-ServiceProvider.Settings.MediumFocusStepCanon) : (-ServiceProvider.Settings.MediumFocusStep));
		});
		FocusMMMCommand = new RelayCommand(() =>
		{
			SetFocus(SimpleManualFocus ? (-ServiceProvider.Settings.LargeFocusStepCanon) : (-ServiceProvider.Settings.LargeFocusStep));
		});
		FocusPCommand = new RelayCommand(() =>
		{
			SetFocus(SimpleManualFocus ? ServiceProvider.Settings.SmallFocusStepCanon : ServiceProvider.Settings.SmalFocusStep);
		});
		FocusPPCommand = new RelayCommand(() =>
		{
			SetFocus(SimpleManualFocus ? ServiceProvider.Settings.MediumFocusStepCanon : ServiceProvider.Settings.MediumFocusStep);
		});
		FocusPPPCommand = new RelayCommand(() =>
		{
			SetFocus(SimpleManualFocus ? ServiceProvider.Settings.LargeFocusStepCanon : ServiceProvider.Settings.LargeFocusStep);
		});
		LockCurrentNearCommand = new RelayCommand(() =>
		{
			if (LockB)
			{
				FocusValue -= FocusCounter;
				FocusCounter = 0;
			}
			LockA = true;
		});
		LockCurrentFarCommand = new RelayCommand(() =>
		{
			if (LockB || LockA)
			{
				FocusValue = FocusCounter;
			}
			LockB = true;
		});
		ResetBrigthnessCommand = new RelayCommand(() =>
		{
			Brightness = 0;
		});
		BrowseOverlayCommand = new RelayCommand(BrowseOverlay);
		ResetOverlayCommand = new RelayCommand(ResetOverlay);
		ZoomInCommand = new RelayCommand(() =>
		{
			CameraDevice.LiveViewImageZoomRatio.NextValue();
		});
		ZoomOutCommand = new RelayCommand(() =>
		{
			CameraDevice.LiveViewImageZoomRatio.PrevValue();
		});
		ZoomIn100 = new RelayCommand(ToggleZoom);
		ToggleGridCommand = new RelayCommand(ToggleGrid);
		CaptureSnapshotCommand = new RelayCommand(CaptureSnapshot);
		FullScreenCommand = new RelayCommand(FullScreen);
		ClosePreviewCommand = new RelayCommand(() =>
		{
			PreviewBitmap = null;
			PreviewBitmapVisible = false;
		});
		CalibrateCommand = new RelayCommand(Calibrate);
		ResetConditionCommand = new RelayCommand(ResetCondition);
		DeleteOverlayItemCommand = new GalaSoft.MvvmLight.Command.RelayCommand<object>(DeleteOverlayItem);
	}

	private void Init()
	{
		IsActive = true;
		DelayedStart = false;
		RecButtonText = TranslationStrings.ButtonRecordMovie;
		CalibrateBase = ServiceProvider.Settings.DiskSize.ToString();
		_videoSource.StreamStarted += VideoSource_StreamStarted;
		_videoSource.StreamFailed += VideoSource_StreamFailed;
		_timer.Stop();
		_timer.AutoReset = true;
		CameraDevice.CameraDisconnected += CameraDeviceCameraDisconnected;
		_photoCapturedTime = DateTime.MinValue;
		CameraDevice.PhotoCaptured += CameraDevicePhotoCaptured;
		StartLiveView();
		_freezeTimer.Interval = ServiceProvider.Settings.LiveViewFreezeTimeOut * 1000;
		_freezeTimer.Elapsed += FreezeTimer_Elapsed;
		_timer.Elapsed += Timer_Elapsed;
		_restartTimer.AutoReset = true;
		_restartTimer.Elapsed += RestartTimer_Elapsed;
		LiveViewManager.PreviewCaptured += LiveViewManager_PreviewCaptured;
		if (!IsInDesignMode)
		{
			ServiceProvider.WindowsManager.Event += WindowsManager_Event1;
		}
	}

	public void UnInit()
	{
		_timer.Stop();
		_restartTimer.Stop();
		CameraDevice.PhotoCaptured -= CameraDevicePhotoCaptured;
		LiveViewManager.PreviewCaptured -= LiveViewManager_PreviewCaptured;
		ServiceProvider.WindowsManager.Event -= WindowsManager_Event1;
		ServiceProvider.WindowsManager.Event -= WindowsManagerEvent;
		Thread.Sleep(300);
		StopLiveView();
		Recording = false;
		LockA = false;
		LockB = false;
		LiveViewData = null;
		IsActive = false;
	}

	private void InitOverlay()
	{
		Overlays = new AsyncObservableCollection<ValuePair>();
		Grids = new AsyncObservableCollection<string>
		{
			TranslationStrings.LabelNone,
			TranslationStrings.LabelRuleOfThirds,
			TranslationStrings.LabelComboGrid,
			TranslationStrings.LabelDiagonal,
			TranslationStrings.LabelSplit,
			TranslationStrings.LabelCircle
		};
		if (!Directory.Exists(ServiceProvider.Settings.OverlayFolder))
		{
			Directory.CreateDirectory(ServiceProvider.Settings.OverlayFolder);
		}
		else
		{
			string[] files = Directory.GetFiles(ServiceProvider.Settings.OverlayFolder, "*.png");
			string[] array = files;
			foreach (string file in array)
			{
				Overlays.Add(new ValuePair
				{
					Name = Path.GetFileNameWithoutExtension(file),
					Value = file
				});
			}
		}
		OverlayTransparency = 50;
		OverlayUseLastCaptured = false;
	}

	private void WindowsManagerEvent(string cmd, object o)
	{
		ICameraDevice device = (o as ICameraDevice) ?? ServiceProvider.DeviceManager.SelectedCameraDevice;
		if (device != CameraDevice)
		{
			return;
		}
		if (cmd.StartsWith("LiveView_ManualFocus"))
		{
			try
			{
				string step = cmd.Substring("LiveView_ManualFocus".Length);
				SetFocus(Convert.ToInt32(step));
				while (_focusIProgress)
				{
					Thread.Sleep(5);
				}
			}
			catch (Exception)
			{
			}
		}
		switch (cmd)
		{
		case "LiveViewWnd_StartMotionDetection":
			DetectMotion = true;
			TriggerOnMotion = true;
			MotionAction = 1;
			break;
		case "LiveViewWnd_StopMotionDetection":
			DetectMotion = false;
			break;
		case "LiveViewWnd_Maximize":
			ShowLeftTab = false;
			break;
		case "LiveViewWnd_StartRecord":
			RecordMovie();
			break;
		case "LiveViewWnd_StopRecord":
			StopRecordMovie();
			break;
		case "LiveViewWnd_StartView":
			StartLiveView();
			break;
		case "LiveViewWnd_StopView":
			StopLiveView();
			break;
		case "LiveViewFullScreen_Show":
			Application.Current.Dispatcher.BeginInvoke(new Action(FullScreen));
			break;
		case "LiveViewFullScreen_Hide":
			Application.Current.Dispatcher.BeginInvoke((Action)(() =>
			{
				if (FullScreenWnd != null && FullScreenWnd.IsVisible)
				{
					FullScreenWnd.Close();
				}
			}));
			break;
		case "LiveViewWnd_OverlayActive":
			OverlayActivated = (bool)o;
			break;
		case "LiveViewWnd_OverlayLastCapture":
			OverlayUseLastCaptured = (bool)o;
			break;
		case "LiveViewWnd_ToggleRotation270":
			if ((bool)o)
			{
				switch (ServiceProvider.Settings.RotateIndex)
				{
				case 0:
					RotationIndex = 3;
					break;
				case 1:
					RotationIndex = 0;
					break;
				case 2:
					RotationIndex = 1;
					break;
				case 3:
					RotationIndex = 2;
					break;
				}
			}
			else
			{
				RotationIndex = ServiceProvider.Settings.RotateIndex;
			}
			break;
		}
	}

	public void WindowsManager_Event(string cmd, object o)
	{
		ICameraDevice device = (o as ICameraDevice) ?? ServiceProvider.DeviceManager.SelectedCameraDevice;
		if (device != CameraDevice)
		{
			return;
		}
		switch (cmd)
		{
		case "LiveView_Capture":
			CaptureInThread();
			break;
		case "LiveView_CaptureSnapShot":
			CaptureSnapshot();
			break;
		case "LiveView_Focus_Move_Right":
			if (LiveViewData != null && LiveViewData.ImageData != null)
			{
				SetFocusPos(LiveViewData.FocusX + ServiceProvider.Settings.FocusMoveStep, LiveViewData.FocusY);
			}
			break;
		case "LiveView_Focus_Move_Left":
			if (LiveViewData != null && LiveViewData.ImageData != null)
			{
				SetFocusPos(LiveViewData.FocusX - ServiceProvider.Settings.FocusMoveStep, LiveViewData.FocusY);
			}
			break;
		case "LiveView_Focus_Move_Up":
			if (LiveViewData != null && LiveViewData.ImageData != null)
			{
				SetFocusPos(LiveViewData.FocusX, LiveViewData.FocusY - ServiceProvider.Settings.FocusMoveStep);
			}
			break;
		case "LiveView_Focus_Move_Down":
			if (LiveViewData != null && LiveViewData.ImageData != null)
			{
				SetFocusPos(LiveViewData.FocusX, LiveViewData.FocusY + ServiceProvider.Settings.FocusMoveStep);
			}
			break;
		case "LiveView_Zoom_All":
			CameraDevice.LiveViewImageZoomRatio.SetValue(0L);
			break;
		case "LiveView_Zoom_25":
			CameraDevice.LiveViewImageZoomRatio.SetValue(1L);
			break;
		case "LiveView_Zoom_33":
			CameraDevice.LiveViewImageZoomRatio.SetValue(2L);
			break;
		case "LiveView_Zoom_50":
			CameraDevice.LiveViewImageZoomRatio.SetValue(3L);
			break;
		case "LiveView_Zoom_66":
			CameraDevice.LiveViewImageZoomRatio.SetValue(4L);
			break;
		case "LiveView_Zoom_100":
			CameraDevice.LiveViewImageZoomRatio.SetValue(5L);
			break;
		case "LiveView_Focus_M":
			FocusMCommand.Execute(null);
			break;
		case "LiveView_Focus_P":
			FocusPCommand.Execute(null);
			break;
		case "LiveView_Focus_MM":
			FocusMMCommand.Execute(null);
			break;
		case "LiveView_Focus_PP":
			FocusPPCommand.Execute(null);
			break;
		case "LiveView_Focus_MMM":
			FocusMMMCommand.Execute(null);
			break;
		case "LiveView_Focus_PPP":
			FocusPPPCommand.Execute(null);
			break;
		case "LiveView_Focus":
			AutoFocus();
			break;
		case "LiveView_Preview":
			PreviewCommand.Execute(null);
			break;
		case "LiveView_NoProcess":
			NoProcessing = true;
			break;
		}
	}

	private void WindowsManager_Event1(string cmd, object o)
	{
		if (cmd.StartsWith("LiveWnd_Overlay"))
		{
			try
			{
				if (cmd.Contains("_"))
				{
					string[] vals = cmd.Split('_');
					if (vals.Count() > 3)
					{
						CameraProperty property = ServiceProvider.DeviceManager.SelectedCameraDevice.LoadProperties();
						int y = 1;
						if (int.TryParse(vals[2], out y))
						{
							OverlayTransparency = y;
						}
						SelectedOverlay = ((vals[3].ToLower() == "null") ? "" : vals[3]);
						OverlayActivated = vals[3].ToLower() != "null";
					}
				}
			}
			catch (Exception exception)
			{
				Log.Error("Unable to load overlay", exception);
			}
		}
		if (!cmd.StartsWith("Snapshot_"))
		{
			return;
		}
		string[] vals2 = cmd.Split('_');
		if (vals2.Count() <= 2)
		{
			return;
		}
		string text = vals2[1];
		string text2 = text;
		if (!(text2 == "CaptureTime"))
		{
			if (text2 == "NumOfPhotos")
			{
				SnapshotCaptureCount = GetValue(vals2, SnapshotCaptureCount);
			}
		}
		else
		{
			SnapshotCaptureTime = GetValue(vals2, SnapshotCaptureTime);
		}
	}

	private void CameraDeviceCameraDisconnected(object sender, DisconnectCameraEventArgs eventArgs)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Hide", CameraDevice);
	}

	private void LiveViewManager_PreviewCaptured(ICameraDevice cameraDevice, string file)
	{
		if (cameraDevice != CameraDevice)
		{
			return;
		}
		try
		{
			if (File.Exists(file))
			{
				PreviewBitmap = BitmapLoader.Instance.LoadImage(file, 2048, 0);
				LiveViewManager.OnPreviewLoaded(CameraDevice, file);
				File.Delete(file);
			}
		}
		catch (Exception exception)
		{
			Log.Debug("Preview werror", exception);
		}
	}

	private void CameraDevicePhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
	{
		_detector.Reset();
		_photoCapturedTime = DateTime.Now;
		_timer.Start();
	}

	private void VideoSource_StreamFailed(object sender, StreamFailedEventArgs e)
	{
		StaticHelper.Instance.SystemMessage = "Error start live view streem :" + e.Error;
	}

	private void VideoSource_StreamStarted(object sender, EventArgs e)
	{
		StaticHelper.Instance.SystemMessage = "Frame processing started";
		_timer.Start();
	}

	private void Timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		if (!_operInProgress)
		{
			if (++_timer_Elapsed_count >= 20)
			{
				Fps = _frame_count;
				_frame_count = 0;
				_timer_Elapsed_count = 0;
			}
			Task.Run(() =>
			{
				GetLiveImage();
			});
		}
	}

	private void RestartTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		if ((DateTime.Now - _restartTimerStartTime).TotalSeconds > 2.0)
		{
			_restartTimer.Stop();
			DelayedStart = false;
			StartLiveView();
		}
	}

	private void FreezeTimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		FreezeImage = false;
	}

	public virtual void GetLiveImage()
	{
		if (!FreezeImage && !_operInProgress)
		{
			_frame_count++;
			_totalframes++;
			_operInProgress = true;
			if (CameraDevice.GetCapability(CapabilityEnum.LiveViewStream))
			{
				GetLiveImageStream();
			}
			else
			{
				GetLiveImageData();
			}
			_operInProgress = false;
		}
	}

	public virtual void GetLiveImageStream()
	{
		try
		{
			if (!_videoSource.IsPlaying)
			{
				return;
			}
			using Bitmap image = _videoSource.GetCurrentFrame();
			ProcessLiveView(image);
		}
		catch (Exception)
		{
		}
	}

	public virtual void GetLiveImageData()
	{
		try
		{
			if (DelayedStart)
			{
				Log.Error("Live View is delayed to start");
				return;
			}
			try
			{
				LiveViewData = LiveViewManager.GetLiveViewImage(CameraDevice);
			}
			catch (Exception)
			{
				_timer.Stop();
				_operInProgress = false;
				Application.Current.Dispatcher.Invoke(() =>
				{
					Log.Error("라이브 뷰 에러 발생");
					MessageBox.Show("⛔ 라이브 뷰가 정지되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				});
				return;
			}
			if (LiveViewData == null)
			{
				return;
			}
			if (!LiveViewData.IsLiveViewRunning)
			{
				DelayedStart = true;
				_restartTimerStartTime = DateTime.Now;
				_restartTimer.Start();
				_operInProgress = false;
			}
			else
			{
				if (LiveViewData.ImageData == null || LiveViewData.ImageData.Length < 50)
				{
					return;
				}
				bool record = LiveViewData.MovieIsRecording;
				if (LiveViewData == null || LiveViewData.ImageData == null)
				{
					return;
				}
				using MemoryStream stream = new MemoryStream(LiveViewData.ImageData, LiveViewData.ImageDataPosition, LiveViewData.ImageData.Length - LiveViewData.ImageDataPosition);
				LevelAngle = (int)LiveViewData.LevelAngleRolling;
				SoundL = LiveViewData.SoundL;
				SoundR = LiveViewData.SoundR;
				PeakSoundL = LiveViewData.PeakSoundL;
				PeakSoundR = LiveViewData.PeakSoundR;
				HaveSoundData = LiveViewData.HaveSoundData;
				MovieTimeRemain = decimal.Round(LiveViewData.MovieTimeRemain, 2);
				if (record && _recordLength > 0 && (DateTime.Now - _recordStartTime).TotalSeconds > (double)_recordLength)
				{
					StopRecordMovie();
				}
				if (NoProcessing)
				{
					if (!IsMinized)
					{
						BitmapImage bi = new BitmapImage();
						bi.BeginInit();
						bi.CacheOption = BitmapCacheOption.OnLoad;
						bi.StreamSource = stream;
						bi.EndInit();
						bi.Freeze();
						Bitmap = bi;
					}
					_liveViewVideoSource?.OnImageGrabbed(new Bitmap(stream));
					ServiceProvider.DeviceManager.LiveViewImage[CameraDevice] = stream.ToArray();
					_operInProgress = false;
				}
				else
				{
					using (Bitmap res = new Bitmap(stream))
					{
						ProcessLiveView(res);
					}
					stream.Close();
				}
				return;
			}
		}
		catch (Exception exception)
		{
			Log.Error("라이브 뷰 에러", exception);
		}
		finally
		{
			_operInProgress = false;
		}
	}

	public void ProcessLiveView(Bitmap bmp)
	{
		using (Bitmap cloned = new Bitmap(bmp))
		{
			_liveViewVideoSource?.OnImageGrabbed(cloned);
		}
		if (PreviewTime > 0 && (DateTime.Now - _photoCapturedTime).TotalSeconds <= (double)PreviewTime)
		{
			WriteableBitmap bitmap = ServiceProvider.Settings.SelectedBitmap.DisplayImage.Clone();
			if (FlipImage && !ServiceProvider.Settings.FlipPreview)
			{
				bitmap = bitmap.Flip(WriteableBitmapExtensions.FlipMode.Vertical);
			}
			bitmap.Freeze();
			ServiceProvider.DeviceManager.LiveViewImage[CameraDevice] = SaveJpeg(bitmap);
			Bitmap = bitmap;
			return;
		}
		if (DetectMotion)
		{
			ProcessMotionDetection(bmp);
		}
		if (_totalframes % 20 == 0 && ShowHistogram)
		{
			ImageStatisticsHSL hslStatistics = new ImageStatisticsHSL(bmp);
			LuminanceHistogramPoints = ConvertToPointCollection(hslStatistics.Luminance.Values);
			ImageStatistics statistics = new ImageStatistics(bmp);
			RedColorHistogramPoints = ConvertToPointCollection(statistics.Red.Values);
			GreenColorHistogramPoints = ConvertToPointCollection(statistics.Green.Values);
			BlueColorHistogramPoints = ConvertToPointCollection(statistics.Blue.Values);
		}
		if (HighlightUnderExp)
		{
			ColorFiltering filtering = new ColorFiltering();
			filtering.Blue = new IntRange(0, 5);
			filtering.Red = new IntRange(0, 5);
			filtering.Green = new IntRange(0, 5);
			filtering.FillOutsideRange = false;
			filtering.FillColor = new RGB(System.Drawing.Color.Blue);
			filtering.ApplyInPlace(bmp);
		}
		if (HighlightOverExp)
		{
			ColorFiltering filtering2 = new ColorFiltering();
			filtering2.Blue = new IntRange(250, 255);
			filtering2.Red = new IntRange(250, 255);
			filtering2.Green = new IntRange(250, 255);
			filtering2.FillOutsideRange = false;
			filtering2.FillColor = new RGB(System.Drawing.Color.Red);
			filtering2.ApplyInPlace(bmp);
		}
		if (Invert)
		{
			Invert invertFilter = new Invert();
			invertFilter.ApplyInPlace(bmp);
		}
		if (Brightness != 0)
		{
			BrightnessCorrection filter = new BrightnessCorrection(Brightness);
			Bitmap oldBmp = bmp;
			bmp = filter.Apply(bmp);
			oldBmp?.Dispose();
		}
		Bitmap newbmp = bmp;
		if (EdgeDetection)
		{
			FiltersSequence filter2 = new FiltersSequence(Grayscale.CommonAlgorithms.BT709, new HomogenityEdgeDetector());
			newbmp = filter2.Apply(bmp);
		}
		if (writeableBitmap == null || writeableBitmap.PixelWidth != bmp.Width || writeableBitmap.PixelHeight != bmp.Height)
		{
			writeableBitmap = new WriteableBitmap(bmp.Width, bmp.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
		}
		if (BlackAndWhite)
		{
			Grayscale filter3 = new Grayscale(0.299, 0.587, 0.114);
			writeableBitmap = BitmapFactory.ConvertToPbgra32Format(BitmapSourceConvert.ToBitmapSource(filter3.Apply(newbmp)));
		}
		else
		{
			writeableBitmap = BitmapFactory.ConvertToPbgra32Format(BitmapSourceConvert.ToBitmapSource(newbmp));
		}
		WriteableBitmap preview = BitmapFactory.ConvertToPbgra32Format(BitmapSourceConvert.ToBitmapSource(bmp));
		DrawFocusPoint(preview, fill: true);
		if (CameraDevice.LiveViewImageZoomRatio.Value == "All" || CameraDevice.LiveViewImageZoomRatio.Value == "0")
		{
			preview.Freeze();
			Preview = preview;
			if (ShowFocusRect)
			{
				DrawFocusPoint(writeableBitmap);
			}
		}
		DrawGrid(writeableBitmap);
		if (OverlayActivated)
		{
			ShowOverlay(writeableBitmap);
		}
		if (ShowRuler && NoSettingArea)
		{
			ShowDetectMotionArea(writeableBitmap);
		}
		if (SettingArea)
		{
			ShowMeasureBoxSize(writeableBitmap, double.Parse(ServiceProvider.Settings.MetaScale));
		}
		if (MeasureTilt)
		{
			ShowMeasureTilt(writeableBitmap, ServiceProvider.Settings.DiskSize);
		}
		if (MeasureCenter)
		{
			ShowMeasureCenter(writeableBitmap);
		}
		if (FlipImage)
		{
			writeableBitmap = writeableBitmap.Flip(WriteableBitmapExtensions.FlipMode.Vertical);
		}
		if (CropRatio > 0)
		{
			CropOffsetX = (int)((double)writeableBitmap.PixelWidth / 2.0 * (double)CropRatio / 100.0);
			CropOffsetY = (int)((double)writeableBitmap.PixelHeight / 2.0 * (double)CropRatio / 100.0);
			writeableBitmap = writeableBitmap.Crop(CropOffsetX, CropOffsetY, writeableBitmap.PixelWidth - 2 * CropOffsetX, writeableBitmap.PixelHeight - 2 * CropOffsetY);
		}
		switch (RotationIndex)
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
		case 4:
			Rotation = LiveViewData.Rotation;
			break;
		}
		writeableBitmap.Freeze();
		Bitmap = writeableBitmap;
		ServiceProvider.DeviceManager.LiveViewImage[CameraDevice] = SaveJpeg(writeableBitmap);
		writeableBitmapPixelWidth = writeableBitmap.PixelWidth;
		writeableBitmapPixelHeight = writeableBitmap.PixelHeight;
		if (LiveViewData != null)
		{
			orgImageWidth = LiveViewData.ImageWidth;
		}
		if (LiveViewData != null && LiveViewData.ImageData != null)
		{
			LiveViewData.ImageData = null;
		}
	}

	public void StartLiveView()
	{
		try
		{
			if (!IsActive)
			{
				return;
			}
			string resp = CameraDevice.GetProhibitionCondition(OperationEnum.LiveView);
			if (!string.IsNullOrEmpty(resp))
			{
				Log.Error("Error starting live view " + resp);
				if (resp != "LabelImageInRAM" && resp != "LabelCommandProcesingError")
				{
					ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Message", TranslationStrings.LabelLiveViewError + "\n" + TranslationManager.GetTranslation(resp));
				}
				_timer.Stop();
				return;
			}
			Task.Run(async () =>
			{
				try
				{
					await StartLiveViewThreadAsync();
				}
				catch (Exception ex2)
				{
					Exception ex3 = ex2;
					Log.Error("Error starting live view in background", ex3);
					await Application.Current.Dispatcher.BeginInvoke((Action)(() =>
					{
						ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Message", TranslationStrings.LabelLiveViewError + "\n" + ex3.Message);
						_timer.Stop();
					}));
				}
			});
		}
		catch (Exception ex)
		{
			Log.Error("Error starting live view ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Message", TranslationStrings.LabelLiveViewError + "\n" + ex.Message);
			_timer.Stop();
		}
	}

	private async Task StartLiveViewThreadAsync(CancellationToken ct = default(CancellationToken))
	{
		try
		{
			_totalframes = 0;
			_framestart = DateTime.Now;
			Log.Debug("LiveView : Start Liveview (async)");
			int attempt = 0;
			int baseDelayMs = 100;
			bool started = false;
			while (!started && attempt < 10)
			{
				ct.ThrowIfCancellationRequested();
				try
				{
					LiveViewManager.StartLiveView(CameraDevice);
					started = true;
				}
				catch (DeviceException ex)
				{
					DeviceException deviceException = ex;
					if (deviceException.ErrorCode == 2147942570u || deviceException.ErrorCode == 8217)
					{
						attempt++;
						int delay = Math.Min(2000, baseDelayMs * (1 << attempt - 1));
						Log.Debug(string.Format("Retry live view (attempt {0}) after {1} ms : {2}", attempt, delay, deviceException.ErrorCode.ToString("X")));
						await Task.Delay(delay, ct).ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						if (!(ex is Exception source))
						{
							throw ex;
						}
						ExceptionDispatchInfo.Capture(source).Throw();
					}
					continue;
				}
				break;
			}
			if (!started)
			{
				throw new Exception("Unable to start live view after retries.");
			}
			if (CameraDevice.GetCapability(CapabilityEnum.LiveViewStream))
			{
				Recording = false;
				await Application.Current.Dispatcher.BeginInvoke((Action)(() =>
				{
					try
					{
						_videoSource.StartPlay(new Uri(CameraDevice.GetLiveViewStream()));
						Log.Error("Waiting for live view stream");
						StaticHelper.Instance.SystemMessage = "Waiting for live view stream...";
					}
					catch (Exception exception2)
					{
						Log.Error("Failed to start live view stream", exception2);
					}
				}));
			}
			else
			{
				_timer.Start();
			}
			_operInProgress = false;
		}
		catch (OperationCanceledException)
		{
			Log.Debug("StartLiveViewThreadAsync cancelled");
			_operInProgress = false;
		}
		catch (Exception ex3)
		{
			Exception exception = ex3;
			Log.Error("LiveView : StartLiveView() 실패 (async)", exception);
			StaticHelper.Instance.SystemMessage = "Unable to start liveview ! " + exception.Message;
			_operInProgress = false;
		}
	}

	public void StopLiveView()
	{
		if (CameraDevice.IsChecked)
		{
			Thread thread = new Thread(StopLiveViewThread);
			thread.Start();
			thread.Join();
		}
	}

	private void StopLiveViewThread()
	{
		try
		{
			_totalframes = 0;
			_framestart = DateTime.Now;
			bool retry = false;
			int retryNum = 0;
			Log.Debug("LiveView : Stop Liveview");
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
						Log.Debug("Retry live view stop:" + ex.ErrorCode.ToString("X"));
						retry = true;
						retryNum++;
						continue;
					}
					throw;
				}
			}
			while (retry && retryNum < 35);
			if (_videoSource != null && _videoSource.IsPlaying)
			{
				_videoSource.Stop();
			}
		}
		catch (Exception ex2)
		{
			Log.Error("Unable to stop liveview !", ex2);
			StaticHelper.Instance.SystemMessage = "Unable to stop liveview ! " + ex2.Message;
		}
	}

	private void CapturePreview()
	{
		Thread thread = new Thread(() =>
		{
			Capture(preview: true);
		});
		thread.Start();
	}

	private void CaptureInThread()
	{
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
		{
			MessageBox.Show("이미지 다운로드 중입니다. 다시 시도하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return;
		}
		Thread thread = new Thread(() =>
		{
			Capture();
		});
		thread.Start();
	}

	private void Capture(bool preview = false)
	{
		if (G2FocusCamera.IsOwned(CameraDevice)) return;
		if (CameraDevice.ShutterSpeed != null && CameraDevice.ShutterSpeed.Value == "Bulb")
		{
			StaticHelper.Instance.SystemMessage = TranslationStrings.MsgBulbModeNotSupported;
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Message", TranslationStrings.MsgBulbModeNotSupported);
			CaptureInProgress = false;
			return;
		}
		if (preview)
		{
			_timer.Stop();
			Thread.Sleep(300);
			try
			{
				LiveViewManager.PreviewRequest[CameraDevice] = true;
				CameraDevice.CapturePhotoNoAf();
				Log.Debug("LiveView : Capture preview");
				return;
			}
			catch (Exception ex)
			{
				StaticHelper.Instance.SystemMessage = ex.Message;
				Log.Error("Unable to take preview picture with no af", ex);
			}
		}
		CaptureInProgress = true;
		CaptureCancelRequested = false;
		if (CaptureCount == 0)
		{
			CaptureCount = 1;
		}
		for (int i = 0; i < CaptureCount; i++)
		{
			Log.Debug("LiveView : Capture started");
			if (CaptureDelay > 0)
			{
				Log.Debug("LiveView : Capture delayed");
				CountDown = CaptureDelay;
				CountDownVisible = true;
				while (CountDown > 0 && !CaptureCancelRequested)
				{
					Thread.Sleep(1000);
					CountDown--;
				}
				CountDownVisible = false;
			}
			if (AutoFocusBeforCapture)
			{
				AutoFocusThread();
			}
			if (CaptureCancelRequested)
			{
				break;
			}
			_timer.Stop();
			Thread.Sleep(300);
			try
			{
				CameraDevice.CapturePhotoNoAf();
				Log.Debug("LiveView : Capture Initialization Done");
			}
			catch (Exception ex2)
			{
				StaticHelper.Instance.SystemMessage = ex2.Message;
				Log.Error("Unable to take picture with no af", ex2);
				break;
			}
			if (CaptureCount <= 1)
			{
				continue;
			}
			CameraDevice.WaitForCamera(2000);
			for (int j = 0; j < PreviewTime; j++)
			{
				Thread.Sleep(1000);
				if (CaptureCancelRequested)
				{
					break;
				}
			}
		}
		CaptureInProgress = false;
	}

	private void CaptureSnapshot()
	{
		Task.Factory.StartNew(CaptureSnapshotThread);
	}

	private void CaptureSnapshotThread()
	{
		try
		{
			for (int i = 0; i < SnapshotCaptureCount; i++)
			{
				MemoryStream stream = new MemoryStream(LiveViewData.ImageData, LiveViewData.ImageDataPosition, LiveViewData.ImageData.Length - LiveViewData.ImageDataPosition);
				ServiceProvider.DeviceManager.OnPhotoCaptured(null, new PhotoCapturedEventArgs
				{
					CameraDevice = new FileTransferDevice(),
					FileName = "file.jpg",
					Handle = stream.ToArray()
				});
				if (i < SnapshotCaptureCount)
				{
					Thread.Sleep(SnapshotCaptureTime);
				}
			}
		}
		catch (Exception exception)
		{
			Log.Debug("Error capture snapshot", exception);
		}
	}

	private void RecordMovie()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		try
		{
			string resp = (Recording ? "" : CameraDevice.GetProhibitionCondition(OperationEnum.RecordMovie));
			if (string.IsNullOrEmpty(resp))
			{
				_recordLength = 0;
				Thread thread = new Thread(RecordMovieThread);
				thread.Start();
			}
			else
			{
				ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Message", TranslationStrings.LabelErrorRecordMovie + "\n" + TranslationManager.GetTranslation(resp));
			}
		}
		catch (Exception ex)
		{
			Log.Debug("Start movie record error", ex);
			StaticHelper.Instance.SystemMessage = "Start movie record error " + ex.Message;
		}
	}

	private void RecordMovieThread()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		try
		{
			CameraDevice.StartRecordMovie();
			Recording = true;
			_recordStartTime = DateTime.Now;
		}
		catch (Exception ex)
		{
			StaticHelper.Instance.SystemMessage = ex.Message;
			Log.Error("Start Recording error", ex);
		}
	}

	private void StopRecordMovie()
	{
		Thread thread = new Thread(StopRecordMovieThread);
		thread.Start();
	}

	private void StopRecordMovieThread()
	{
		try
		{
			Recording = false;
			CameraDevice.StopRecordMovie();
		}
		catch (Exception ex)
		{
			StaticHelper.Instance.SystemMessage = ex.Message;
			Log.Error("Stop Recording error", ex);
		}
	}

	public void RecordMovie_Turntable()
	{
		// Legacy capture is disabled in G2; replacement uses a separate service.
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return;

		PreviewBitmapVisible = false;
		RecordMovie();
		CameraDevice.GetCapability(CapabilityEnum.RecordMovie);
	}

	public void StopRecordMovie_Turntable()
	{
		PreviewBitmapVisible = false;
		StopRecordMovie();
	}

	private void SetFocus(int step)
	{
		if (G2FocusCamera.IsOwned(CameraDevice)) return;
		Application.Current.Dispatcher.Invoke(() => CameraControl.windows.SuperfocusWindow.InvalidateFor(CameraDevice));
		PreviewBitmapVisible = false;
		if (_focusIProgress)
		{
			return;
		}
		_focusIProgress = true;
		RaisePropertyChanged(() => FocusingEnabled);
		try
		{
			string resp = CameraDevice.GetProhibitionCondition(OperationEnum.ManualFocus);
			if (string.IsNullOrEmpty(resp))
			{
				Thread thread = new Thread(SetFocusThread);
				thread.Start(step);
				return;
			}
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Message", TranslationStrings.LabelErrorUnableFocus + "\n" + TranslationManager.GetTranslation(resp));
			_focusIProgress = false;
			RaisePropertyChanged(() => FocusingEnabled);
			SelectedFocusValue = FocusCounter;
		}
		catch (Exception exception)
		{
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Message", TranslationStrings.LabelErrorUnableFocus);
			Log.Error("Unable to focus ", exception);
			_focusIProgress = false;
			RaisePropertyChanged(() => FocusingEnabled);
			SelectedFocusValue = FocusCounter;
		}
	}

	private void SetFocusThread(object ostep)
	{
		int step = (int)ostep;
		if (step != 0)
		{
			if (LockA)
			{
				if (FocusCounter == 0 && step < 0)
				{
					_focusIProgress = false;
					_selectedFocusValue = FocusCounter;
					OnFocuseDone();
					RaisePropertyChanged(() => FocusingEnabled);
					RaisePropertyChanged(() => SelectedFocusValue);
					return;
				}
				if (FocusCounter + step < 0)
				{
					step = -FocusCounter;
				}
			}
			if (LockB && FocusCounter + step > FocusValue)
			{
				step = FocusValue - FocusCounter;
			}
			int focusStep = 0;
			_timer.Stop();
			int retryCount = 10;
			do
			{
				try
				{
					CameraDevice.StartLiveView();
					if (SimpleManualFocus)
					{
						FocusStepMove(step);
						GetLiveImage();
						focusStep = step;
						FocusCounter += focusStep;
					}
					else
					{
						FocusCounter += CameraDevice.Focus(step);
					}
					if (!LockA && LockB && FocusCounter < 0)
					{
						FocusValue += FocusCounter * -1;
						FocusCounter = 0;
					}
					StaticHelper.Instance.SystemMessage = "Move focus " + step;
				}
				catch (DeviceException ex)
				{
					Log.Debug("Unable to focus", ex);
					StaticHelper.Instance.SystemMessage = TranslationStrings.LabelErrorUnableFocus + " " + ex.Message;
					retryCount--;
					goto IL_0300;
				}
				catch (Exception exception)
				{
					Log.Debug("Unable to focus", exception);
					StaticHelper.Instance.SystemMessage = TranslationStrings.LabelErrorUnableFocus;
					retryCount--;
					goto IL_0300;
				}
				break;
				IL_0300:
				Thread.Sleep(50);
			}
			while (retryCount > 0);
		}
		_focusIProgress = false;
		_selectedFocusValue = FocusCounter;
		OnFocuseDone();
		RaisePropertyChanged(() => FocusingEnabled);
		RaisePropertyChanged(() => SelectedFocusValue);
		_timer.Start();
	}

	private void FocusStepMove(int step)
	{
		int large = Math.Abs(step) / ServiceProvider.Settings.LargeFocusStepCanon;
		int large_left = Math.Abs(step) % ServiceProvider.Settings.LargeFocusStepCanon;
		int medium = large_left / ServiceProvider.Settings.MediumFocusStepCanon;
		int meduim_left = large_left % ServiceProvider.Settings.MediumFocusStepCanon;
		FocusDirection dir = ((step > 0) ? FocusDirection.Far : FocusDirection.Near);
		for (int i = 0; i < large; i++)
		{
			CameraDevice.Focus(dir, FocusAmount.Large);
			Thread.Sleep(ServiceProvider.Settings.CanonFocusStepWait);
		}
		for (int j = 0; j < medium; j++)
		{
			CameraDevice.Focus(dir, FocusAmount.Medium);
			Thread.Sleep(ServiceProvider.Settings.CanonFocusStepWait);
		}
		for (int k = 0; k < meduim_left; k++)
		{
			CameraDevice.Focus(dir, FocusAmount.Small);
			Thread.Sleep(ServiceProvider.Settings.CanonFocusStepWait);
		}
	}

	private static void OnFocuseDone()
	{
		FocuseDone?.Invoke(null, EventArgs.Empty);
	}

	public void SetFocusPos(System.Windows.Point initialPoint, double refWidth, double refHeight)
	{
		if (LiveViewData != null)
		{
			double offsetX = (refWidth * 100.0 / (double)(100 - CropRatio) - refWidth) / 2.0;
			double offsety = (refHeight * 100.0 / (double)(100 - CropRatio) - refHeight) / 2.0;
			double xt = (double)LiveViewData.ImageWidth / (refWidth + offsetX * 2.0);
			double yt = (double)LiveViewData.ImageHeight / (refHeight + offsety * 2.0);
			int posx = (int)((offsetX + initialPoint.X) * xt);
			if (FlipImage)
			{
				posx = (int)((refWidth - initialPoint.X + offsetX) * xt);
			}
			int posy = (int)((initialPoint.Y + offsety) * yt);
			Task.Factory.StartNew(() =>
			{
				SetFocusPos(posx, posy);
			});
		}
	}

	public virtual void SetFocusPos(int x, int y)
	{
        if (CameraControl.windows.SuperfocusWindow.TrySelectPreviewBox(CameraDevice, LiveViewData, x, y)) return;
		if (G2FocusCamera.IsOwned(CameraDevice)) return;
		try
		{
			CameraDevice.Focus(x, y);
		}
		catch (Exception exception)
		{
			Log.Error("Error set focus pos :", exception);
			StaticHelper.Instance.SystemMessage = TranslationStrings.LabelErrorSetFocusPos;
		}
	}

	private void DrawFocusPoint(WriteableBitmap bitmap, bool fill = false)
	{
		try
		{
			if (LiveViewData != null)
			{
				double xt = bitmap.Width / (double)LiveViewData.ImageWidth;
				double yt = bitmap.Height / (double)LiveViewData.ImageHeight;
                var selected=CameraControl.windows.SuperfocusWindow.PreviewBoxFor(CameraDevice);
                double focusX=selected==null?LiveViewData.FocusX:selected.X*LiveViewData.ImageWidth;
                double focusY=selected==null?LiveViewData.FocusY:selected.Y*LiveViewData.ImageHeight;
				if (fill)
				{
					bitmap.FillRectangle2((int)(focusX * xt - (double)LiveViewData.FocusFrameXSize * xt / 2.0), (int)(focusY * yt - (double)LiveViewData.FocusFrameYSize * yt / 2.0), (int)(focusX * xt + (double)LiveViewData.FocusFrameXSize * xt / 2.0), (int)(focusY * yt + (double)LiveViewData.FocusFrameYSize * yt / 2.0), LiveViewData.HaveFocusData ? System.Windows.Media.Color.FromArgb(96, 0, byte.MaxValue, 0) : System.Windows.Media.Color.FromArgb(96, byte.MaxValue, 0, 0));
				}
				else
				{
					bitmap.DrawRectangle((int)(focusX * xt - (double)LiveViewData.FocusFrameXSize * xt / 2.0), (int)(focusY * yt - (double)LiveViewData.FocusFrameYSize * yt / 2.0), (int)(focusX * xt + (double)LiveViewData.FocusFrameXSize * xt / 2.0), (int)(focusY * yt + (double)LiveViewData.FocusFrameYSize * yt / 2.0), LiveViewData.HaveFocusData ? Colors.Green : Colors.Red);
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("Error draw helper lines", exception);
		}
	}

	private void AutoFocus()
	{
		PreviewBitmapVisible = false;
		if (LockA || LockB)
		{
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Message", TranslationStrings.LabelErrorAutoFocusLock);
			return;
		}
		string resp = CameraDevice.GetProhibitionCondition(OperationEnum.AutoFocus);
		if (string.IsNullOrEmpty(resp))
		{
			Thread thread = new Thread(AutoFocusThread);
			thread.Start();
		}
		else
		{
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Message", TranslationStrings.LabelErrorUnableFocus + "\n" + TranslationManager.GetTranslation(resp));
		}
	}

	private void AutoFocusThread()
	{
		if (G2FocusCamera.IsOwned(CameraDevice)) return;
		if (Application.Current.Dispatcher.Invoke(() => CameraControl.windows.SuperfocusWindow.TryFocusSelectedBox(CameraDevice))) return;
		Application.Current.Dispatcher.Invoke(() => CameraControl.windows.SuperfocusWindow.InvalidateFor(CameraDevice));
		try
		{
			LockB = false;
			LockA = false;
			CameraDevice.AutoFocus();
		}
		catch (Exception ex)
		{
			Log.Error("Unable to auto focus", ex);
			StaticHelper.Instance.SystemMessage = ex.Message;
		}
	}

	private void ProcessMotionDetection(Bitmap bmp)
	{
		if (CountDownVisible)
		{
			return;
		}
		try
		{
			float movement = 0f;
			if (DetectMotionArea)
			{
				int x1 = bmp.Width * HorizontalMin / 1000;
				int x2 = bmp.Width * (HorizontalMin + HorizontalMax) / 1000;
				int y2 = bmp.Height * (VerticalMin + VerticalMax) / 1000;
				int y3 = bmp.Height * VerticalMin / 1000;
				using Bitmap cropbmp = new Bitmap(bmp.Clone(new Rectangle(x1, y3, x2 - x1, y2 - y3), bmp.PixelFormat));
				cropbmp.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);
				movement = _detector.ProcessFrame(cropbmp);
				using Graphics currentTileGraphics = Graphics.FromImage(bmp);
				currentTileGraphics.DrawImage(cropbmp, x1, y3);
			}
			else
			{
				movement = _detector.ProcessFrame(bmp);
			}
			CurrentMotionIndex = Math.Round(movement * 100f, 2);
			if (movement > (float)MotionThreshold / 100f && MotionAction > 0 && !Recording && (DateTime.Now - _photoCapturedTime).TotalSeconds > (double)WaitForMotionSec && _totalframes > 10)
			{
				if (MotionAction == 1)
				{
					FocusOnMovment(bmp);
					CaptureInThread();
				}
				if (MotionAction == 2 && CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled)
				{
					FocusOnMovment(bmp);
					RecordMovie();
					_recordLength = MotionMovieLength;
				}
				_detector.Reset();
				_photoCapturedTime = DateTime.Now;
				_totalframes = 0;
			}
		}
		catch (Exception exception)
		{
			Log.Error("Motion detection error ", exception);
		}
	}

	private void FocusOnMovment(Bitmap bmp)
	{
		if (!MotionAutofocusBeforCapture)
		{
			return;
		}
		if (_detector.MotionProcessingAlgorithm is BlobCountingObjectsProcessing { ObjectRectangles: not null } processing && processing.ObjectRectangles.Length != 0 && LiveViewData.ImageData != null)
		{
			Rectangle rectangle = default;
			int surface = 0;
			Rectangle[] objectRectangles = processing.ObjectRectangles;
			for (int i = 0; i < objectRectangles.Length; i++)
			{
				Rectangle objectRectangle = objectRectangles[i];
				if (surface < objectRectangle.Width * objectRectangle.Height)
				{
					surface = objectRectangle.Width * objectRectangle.Height;
					rectangle = objectRectangle;
				}
			}
			double xt = (double)LiveViewData.ImageWidth / (double)bmp.Width;
			double yt = (double)LiveViewData.ImageHeight / (double)bmp.Height;
			int posx = (int)((double)(rectangle.X + rectangle.Width / 2) * xt);
			int posy = (int)((double)(rectangle.Y + rectangle.Height / 2) * yt);
			CameraDevice.Focus(posx, posy);
		}
		AutoFocusThread();
	}

	private void FullScreen()
	{
		if (FullScreenWnd != null)
		{
			FullScreenWnd.Close();
			FullScreenWnd.DataContext = null;
			FullScreenWnd = null;
		}
		FullScreenWnd = new LiveViewFullScreenWnd();
		FullScreenWnd.DataContext = this;
		FullScreenWnd.Title = Title;
		FullScreenWnd.Closed += FullScreenWnd_Closed;
		FullScreenWnd.Show();
		_window.Hide();
	}

	private void FullScreenWnd_Closed(object sender, EventArgs e)
	{
		try
		{
			FullScreenWnd.Closed -= FullScreenWnd_Closed;
			_window.Show();
		}
		catch (Exception exception)
		{
			Log.Debug("Unable to show main live view window", exception);
		}
	}

	private void ResetOverlay()
	{
		OverlayTransparency = 50;
		OverlayHorizontal = 0;
		OverlayScale = 0;
		OverlayVertical = 0;
	}

	public void BrowseOverlay()
	{
		OpenFileDialog dlg = new OpenFileDialog();
		dlg.Filter = "Supported Images|*.jpg;*.png|Jpeg file(*.jpg)|*.jpg|Png file(*.png)|*.png|All files|*.*";
		dlg.FileName = SelectedOverlay;
		if (dlg.ShowDialog() == true)
		{
			string filename = Path.GetFileNameWithoutExtension(dlg.FileName);
			filename += ".png";
			BitmapLoader.Instance.GenerateOverlayFile(dlg.FileName);
			SetOverlay(ServiceProvider.Settings.OverlayFolder + "\\" + filename);
		}
	}

	public void SetOverlay(string file)
	{
		Overlays.Add(new ValuePair
		{
			Name = Path.GetFileNameWithoutExtension(file),
			Value = file
		});
		SelectedOverlay = file;
	}

	private void DeleteOverlayItem(object param)
	{
		if (!(param is ValuePair itemToRemove))
		{
			return;
		}
		string message = "'" + itemToRemove.Name + "' 정말 삭제하시겠습니까?";
		MessageBoxResult result = MessageBox.Show(message, "파일 삭제 확인", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
		if (result != MessageBoxResult.OK)
		{
			return;
		}
		try
		{
			string filePath = itemToRemove.Value.ToString();
			if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
			{
				File.Delete(filePath);
			}
			if (Overlays.Contains(itemToRemove))
			{
				Overlays.Remove(itemToRemove);
			}
			if (SelectedOverlay == itemToRemove.Value)
			{
				SelectedOverlay = null;
			}
		}
		catch (IOException ex)
		{
			MessageBox.Show("파일을 삭제중 오류가 발생했습니다:\n" + ex.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
		catch (Exception ex2)
		{
			MessageBox.Show("알 수 없는 오류가 발생했습니다:\n" + ex2.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void ToggleGrid()
	{
		int i = GridType;
		i++;
		if (i >= Grids.Count)
		{
			i = 0;
		}
		GridType = i;
	}

	private void ToggleZoom()
	{
		try
		{
			if (CameraDevice.LiveViewImageZoomRatio != null && CameraDevice.LiveViewImageZoomRatio.Values != null)
			{
				if (CameraDevice.LiveViewImageZoomRatio.Values.Count == 2)
				{
					CameraDevice.LiveViewImageZoomRatio.Value = ((CameraDevice.LiveViewImageZoomRatio.Value == CameraDevice.LiveViewImageZoomRatio.Values[0]) ? CameraDevice.LiveViewImageZoomRatio.Values[1] : CameraDevice.LiveViewImageZoomRatio.Values[0]);
				}
				else
				{
					CameraDevice.LiveViewImageZoomRatio.Value = ((CameraDevice.LiveViewImageZoomRatio.Value == CameraDevice.LiveViewImageZoomRatio.Values[0]) ? CameraDevice.LiveViewImageZoomRatio.Values[CameraDevice.LiveViewImageZoomRatio.Values.Count - 2] : CameraDevice.LiveViewImageZoomRatio.Values[0]);
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("Unable to set zoom", exception);
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

	public byte[] SaveJpeg(WriteableBitmap image)
	{
		JpegBitmapEncoder enc = new JpegBitmapEncoder();
		enc.QualityLevel = 50;
		enc.Frames.Add(BitmapFrame.Create(image));
		using MemoryStream stm = new MemoryStream();
		enc.Save(stm);
		return stm.ToArray();
	}

	private void DrawGrid(WriteableBitmap writeableBitmap)
	{
		System.Windows.Media.Color color = Colors.White;
		color.A = 50;
		color = GridColor;
		int GridLineNumber = 0;
		switch (GridType)
		{
		case 1:
			GridLineNumber = 4;
			break;
		case 2:
			GridLineNumber = 6;
			break;
		case 3:
			GridLineNumber = 10;
			break;
		case 4:
			GridLineNumber = 20;
			break;
		}
		for (int i = 1; i < GridLineNumber; i++)
		{
			writeableBitmap.DrawLine(0, (int)(writeableBitmap.Height / (double)GridLineNumber * (double)i), (int)writeableBitmap.Width, (int)(writeableBitmap.Height / (double)GridLineNumber * (double)i), color);
			writeableBitmap.DrawLine((int)(writeableBitmap.Width / (double)GridLineNumber * (double)i), 0, (int)(writeableBitmap.Width / (double)GridLineNumber * (double)i), (int)writeableBitmap.Height, color);
		}
		writeableBitmap.DrawLineDDA(0, (int)(writeableBitmap.Height / 2.0), (int)writeableBitmap.Width, (int)(writeableBitmap.Height / 2.0), Colors.Red);
		writeableBitmap.DrawLineDDA((int)(writeableBitmap.Width / 2.0), 0, (int)(writeableBitmap.Width / 2.0), (int)writeableBitmap.Height, Colors.Red);
		if (GridType == 5)
		{
			double centerX = (double)writeableBitmap.PixelWidth / 2.0;
			double centerY = (double)writeableBitmap.PixelHeight / 2.0;
			double radius = (double)writeableBitmap.PixelWidth * 0.6 / 2.0;
			writeableBitmap.DrawEllipse((int)(centerX - radius), (int)(centerY - radius), (int)(centerX + radius), (int)(centerY + radius), Colors.Red);
		}
	}

	private PointCollection ConvertToPointCollection(int[] values)
	{
		int max = values.Max();
		PointCollection points = new PointCollection();
		points.Add(new System.Windows.Point(0.0, max));
		for (int i = 0; i < values.Length; i++)
		{
			points.Add(new System.Windows.Point(i, max - values[i]));
		}
		points.Add(new System.Windows.Point(values.Length - 1, max));
		points.Freeze();
		return points;
	}

	private void ResetCondition()
	{
		Log.Debug("Reset Condition Click");
		ServiceProvider.Settings.isBtnPressed = false;
		ServiceProvider.Settings.GetMeasurePicture = false;
		ServiceProvider.Settings.ThumbailLoaded = true;
		ServiceProvider.Settings.TurnTableShot = false;
		ServiceProvider.Settings.FrontBackMode = 0;
		MainMenuViewModel.IsMusinsaFrontOK = true;
		MainMenuViewModel.IsMusinsaBackOK = true;
		ServiceProvider.DeviceManager.SelectedCameraDevice.StopFocus();
		Thread.Sleep(200);
		StopLiveView();
		Thread.Sleep(200);
		StartLiveView();
	}

	public int getLiveViewRotation()
	{
		return CameraProperty.LiveviewSettings.RotationIndex;
	}

	public void Calibrate()
	{
		try
		{
			if (MessageBox.Show("Do you want to change Measure Scale Value?", "YesOrNo", MessageBoxButton.YesNo) == MessageBoxResult.No)
			{
				SettingArea = false;
				return;
			}
			SettingArea = false;
			int x1 = (int)Bitmap.Width * HorizontalMin / 1000;
			int y1 = (int)Bitmap.Height * VerticalMin / 1000;
			int x2 = (int)Bitmap.Width * (HorizontalMin + HorizontalMax) / 1000;
			int y2 = (int)Bitmap.Height * (VerticalMin + VerticalMax) / 1000;
			int LenX = Math.Abs(x2 - x1);
			int LenY = Math.Abs(y2 - y1);
			double diameter = LenX;
			double ratio = Bitmap.Width / (double)LiveViewData.ImageWidth;
			double metaScale = Convert.ToDouble(CalibrateBase) / diameter * ratio;
			ServiceProvider.Settings.MetaScale = metaScale.ToString();
			ServiceProvider.Settings.GetMeasurePicture = true;
			Log.Debug("수동 캘리브레이션 실행 : " + ServiceProvider.Settings.MetaScale);
		}
		catch (Exception ex)
		{
			string msg = ex?.ToString() + "\r\nTry Again....";
			MessageBox.Show(msg);
		}
	}

	private void ShowOverlay(WriteableBitmap wb)
	{
		if (!OverlayActivated || !ServiceProvider.Settings.ThumbailLoaded || ((SelectedOverlay == null || !File.Exists(SelectedOverlay)) && !OverlayUseLastCaptured))
		{
			return;
		}
		if (OverlayUseLastCaptured && File.Exists(ServiceProvider.Settings.SelectedBitmap.FileItem.LargeThumb) && _lastOverlay != ServiceProvider.Settings.SelectedBitmap.FileItem.LargeThumb)
		{
			_lastOverlay = ServiceProvider.Settings.SelectedBitmap.FileItem.LargeThumb;
		}
		try
		{
			string overlayPath = (OverlayUseLastCaptured ? _lastOverlay : SelectedOverlay);
			if (!File.Exists(overlayPath))
			{
				PhotoUtils.WaitForFile(overlayPath);
				return;
			}
			using FileStream fs = new FileStream(overlayPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			BitmapImage bitmapSource = new BitmapImage();
			bitmapSource.BeginInit();
			bitmapSource.CacheOption = BitmapCacheOption.OnLoad;
			bitmapSource.StreamSource = fs;
			bitmapSource.EndInit();
			bitmapSource.Freeze();
			_overlayImage = new WriteableBitmap(bitmapSource);
			_overlayloadedFilePath = overlayPath;
		}
		catch (Exception)
		{
			_overlayImage = null;
			Log.Debug("오버레이 파일 로딩 중 에러 발생...");
			return;
		}
		try
		{
			double bgW = wb.PixelWidth;
			double bgH = wb.PixelHeight;
			double ovW = _overlayImage.PixelWidth;
			double ovH = _overlayImage.PixelHeight;
			double baseScale = bgH / ovH;
			double multiplier = baseScale * (1.0 + (double)OverlayScale / 100.0);
			if (multiplier < 0.01)
			{
				multiplier = 0.01;
			}
			int finalW = (int)(ovW * multiplier);
			int finalH = (int)(ovH * multiplier);
			double centerX = (bgW - (double)finalW) / 2.0;
			double centerY = (bgH - (double)finalH) / 2.0;
			int destX = (int)(centerX + centerX * ((double)OverlayHorizontal / 100.0));
			int destY = (int)(centerY + centerY * ((double)OverlayVertical / 100.0));
			System.Windows.Media.Color transpColor = Colors.White;
			if (OverlayTransparency < 100)
			{
				transpColor = System.Windows.Media.Color.FromArgb((byte)((double)(255 * OverlayTransparency) / 100.0), byte.MaxValue, byte.MaxValue, byte.MaxValue);
			}
			wb.Blit(new Rect(destX, destY, finalW, finalH), _overlayImage, new Rect(0.0, 0.0, (int)ovW, (int)ovH), transpColor, WriteableBitmapExtensions.BlendMode.Alpha);
		}
		catch
		{
			Log.Debug("오버레이 이미지 그리기 중 에러 발생...");
		}
	}

	private void ShowDetectMotionArea(WriteableBitmap wb)
	{
		System.Windows.Media.Color color = Colors.White;
		color.A = 50;
		color = GridColor;
		int x1 = wb.PixelWidth * HorizontalMin / 1000;
		int y1 = wb.PixelHeight * VerticalMin / 1000;
		int x2 = wb.PixelWidth * (HorizontalMin + HorizontalMax) / 1000;
		int y2 = wb.PixelHeight * (VerticalMin + VerticalMax) / 1000;
		wb.FillRectangle2(0, 0, wb.PixelWidth, wb.PixelHeight, System.Windows.Media.Color.FromArgb(128, 128, 128, 128));
		wb.FillRectangleDeBlend(x1, y1, x2, y2, System.Windows.Media.Color.FromArgb(128, 128, 128, 128));
		wb.DrawRectangle(x1, y1, x2, y2, color);
	}

	private void ShowMeasureBoxSize(WriteableBitmap wb, double scale)
	{
		int x1 = wb.PixelWidth * HorizontalMin / 1000;
		int y1 = wb.PixelHeight * VerticalMin / 1000;
		int x2 = wb.PixelWidth * (HorizontalMin + HorizontalMax) / 1000;
		int y2 = wb.PixelHeight * (VerticalMin + VerticalMax) / 1000;
		int width = x2 - x1;
		int height = y2 - y1;
		if (LiveViewData != null)
		{
			double ratio = Bitmap.Width / (double)LiveViewData.ImageWidth;
			double viewScale = scale / ratio;
			BoxWidth_mm = ((double)width * viewScale).ToString("F1");
			BoxHeight_mm = ((double)height * viewScale).ToString("F1");
		}
	}

	private unsafe void ShowMeasureTilt(WriteableBitmap wb, double realDiameterMm, int blackThreshold = 100)
	{
		int skip = 1;
		int width = wb.PixelWidth;
		int height = wb.PixelHeight;
		int stride = wb.BackBufferStride;
		int startX;
		int endX;
		int startY;
		int endY;
		if (_lastAvgX > 0.0 && _lastBView > 0.0)
		{
			int roiHalfSize = (int)(_lastBView * 1.5);
			startX = Math.Max(0, (int)_lastAvgX - roiHalfSize);
			endX = Math.Min(width, (int)_lastAvgX + roiHalfSize);
			startY = Math.Max(0, (int)_lastAvgY - roiHalfSize);
			endY = Math.Min(height, (int)_lastAvgY + roiHalfSize);
		}
		else
		{
			startX = width / 5;
			endX = width * 4 / 5;
			startY = height / 5;
			endY = height * 4 / 5;
		}
		double sumX = 0.0;
		double sumY = 0.0;
		double sumXX = 0.0;
		double sumYY = 0.0;
		double sumXY = 0.0;
		long count = 0L;
		object lockObj = new object();
		wb.Lock();
		try
		{
			byte* pBase = (byte*)(void*)wb.BackBuffer;
			Parallel.For(startY / skip, endY / skip, () => new double[6], (int i, ParallelLoopState state, double[] local) =>
			{
				int num = i * skip;
				byte* ptr = pBase + num * stride;
				for (int j = startX; j < endX; j += skip)
				{
					byte* ptr2 = ptr + j * 4;
					if (*ptr2 < blackThreshold && ptr2[1] < blackThreshold && ptr2[2] < blackThreshold)
					{
						local[0] += j;
						local[1] += num;
						local[2] += (double)j * (double)j;
						local[3] += (double)num * (double)num;
						local[4] += (double)j * (double)num;
						local[5]++;
					}
				}
				return local;
			}, (double[] local) =>
			{
				lock (lockObj)
				{
					sumX += local[0];
					sumY += local[1];
					sumXX += local[2];
					sumYY += local[3];
					sumXY += local[4];
					count += (long)local[5];
				}
			});
			if (count > 300)
			{
				double avgX = sumX / (double)count;
				double avgY = sumY / (double)count;
				double mxx = sumXX / (double)count - avgX * avgX;
				double myy = sumYY / (double)count - avgY * avgY;
				double mxy = sumXY / (double)count - avgX * avgY;
				double term = Math.Sqrt(Math.Max(0.0, Math.Pow(mxx - myy, 2.0) + 4.0 * Math.Pow(mxy, 2.0)));
				double l1 = (mxx + myy + term) / 2.0;
				double l2 = (mxx + myy - term) / 2.0;
				double a_view = Math.Sqrt(Math.Max(0.1, l1)) * 2.0;
				double b_view = Math.Sqrt(Math.Max(0.1, l2)) * 2.0;
				double viewToRealRatio = (double)orgImageWidth / (double)width;
				double diameterPxInOriginal = a_view * 2.0 * viewToRealRatio;
				double finalScale = ((diameterPxInOriginal > 0.0) ? (realDiameterMm / diameterPxInOriginal) : 0.0);
				double tilt = Math.Acos(Math.Min(1.0, b_view / a_view)) * (180.0 / Math.PI);
				double dir = 0.5 * Math.Atan2(2.0 * mxy, mxx - myy) * (180.0 / Math.PI);
				wb.DrawEllipse((int)(avgX - a_view), (int)(avgY - b_view), (int)(avgX + a_view), (int)(avgY + b_view), Colors.Red);
				wb.DrawRectangle(startX, startY, endX, endY, Colors.Lime);
				if (tilt > 0.3)
				{
					double indicatorRad = (dir + 90.0) * Math.PI / 180.0;
					int arrowLength = (int)(tilt * 10.0);
					int targetX = (int)(avgX + Math.Cos(indicatorRad) * (double)arrowLength);
					int targetY = (int)(avgY + Math.Sin(indicatorRad) * (double)arrowLength);
					wb.DrawLine((int)avgX, (int)avgY, targetX, targetY, Colors.Yellow);
					wb.FillEllipse(targetX - 4, targetY - 4, targetX + 4, targetY + 4, Colors.Yellow);
				}
				_lastAvgX = avgX;
				_lastAvgY = avgY;
				_lastBView = b_view;
				Tilt_Deg = $"TILT : {tilt:F2} deg";
				Direction_Deg = $"DIR  : {dir:F2} deg";
				Scale_mm = $"SCALE: {finalScale:F5} mm/px";
			}
			else
			{
				_lastAvgX = -1.0;
				Scale_mm = "SCALE: Searching Disk...";
			}
		}
		finally
		{
			wb.Unlock();
		}
	}

	private unsafe void ShowMeasureCenter(WriteableBitmap wb, int blackThreshold = 80)
	{
		int width = wb.PixelWidth;
		int height = wb.PixelHeight;
		int stride = wb.BackBufferStride;
		List<(double sumX, double sumY, int count)> stickerPoints = new List<(double, double, int)>();
		object lockObj = new object();
		wb.Lock();
		try
		{
			byte* pBase = (byte*)(void*)wb.BackBuffer;
			Parallel.For(0, height, () => new List<(int, int)>(), (int y, ParallelLoopState state, List<(int x, int y)> localPoints) =>
			{
				byte* ptr = pBase + y * stride;
				for (int i = 0; i < width; i++)
				{
					byte* ptr2 = ptr + i * 4;
					if (*ptr2 < blackThreshold && ptr2[1] < blackThreshold && ptr2[2] < blackThreshold)
					{
						localPoints.Add((i, y));
					}
				}
				return localPoints;
			}, (List<(int x, int y)> local) =>
			{
				if (local.Count > 20)
				{
					double num = 0.0;
					double num2 = 0.0;
					foreach (var current in local)
					{
						num += (double)current.x;
						num2 += (double)current.y;
					}
					lock (lockObj)
					{
						stickerPoints.Add((num, num2, local.Count));
					}
				}
			});
			List<System.Windows.Point> clusters = (from s in stickerPoints.OrderByDescending(((double sumX, double sumY, int count) s) => s.count).Take(4)
				select new System.Windows.Point(s.sumX / (double)s.count, s.sumY / (double)s.count)).ToList();
			if (clusters.Count == 4)
			{
				List<System.Windows.Point> sorted = clusters.OrderBy((System.Windows.Point p) => p.X).ToList();
				System.Windows.Point pLeft = sorted[0];
				System.Windows.Point pRight = sorted[3];
				List<System.Windows.Point> sortedY = clusters.OrderBy((System.Windows.Point p) => p.Y).ToList();
				System.Windows.Point pTop = sortedY[0];
				System.Windows.Point pBottom = sortedY[3];
				double denominator = (pLeft.X - pRight.X) * (pTop.Y - pBottom.Y) - (pLeft.Y - pRight.Y) * (pTop.X - pBottom.X);
				if (Math.Abs(denominator) > 0.0001)
				{
					_centerX = ((pLeft.X * pRight.Y - pLeft.Y * pRight.X) * (pTop.X - pBottom.X) - (pLeft.X - pRight.X) * (pTop.X * pBottom.Y - pTop.Y * pBottom.X)) / denominator;
					_centerY = ((pLeft.X * pRight.Y - pLeft.Y * pRight.X) * (pTop.Y - pBottom.Y) - (pLeft.Y - pRight.Y) * (pTop.X * pBottom.Y - pTop.Y * pBottom.X)) / denominator;
					Center_Disp = $"Turn Table Center : ({_centerX:F2}, {_centerY:F2})";
					int radius = 10;
					wb.DrawEllipse((int)_centerX - radius, (int)_centerY - radius, (int)_centerX + radius, (int)_centerY + radius, Colors.Red);
					wb.FillEllipse((int)_centerX - 3, (int)_centerY - 3, (int)_centerX + 3, (int)_centerY + 3, Colors.Red);
				}
			}
		}
		finally
		{
			wb.Unlock();
		}
	}
}
