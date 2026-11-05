namespace CameraControl.Core.Classes;

public partial class Settings
{
    private bool _useSuperfocus;

    // G2 UI: 수퍼포커스 entry point ON/OFF (클로리움 설정 > 수퍼포커스; ON shows the left-rail button). Persisted;
    // default off like the other G2 features (2026-11-03). Settings files saved before keep their stored value.
    public bool UseSuperfocus
    {
        get => _useSuperfocus;
        set
        {
            if (_useSuperfocus == value) return;
            _useSuperfocus = value;
            NotifyPropertyChanged("UseSuperfocus");
        }
    }

    private int _shutterSound = 1;

    // G2: 촬영음 종류 — number of Data\ShutterSounds\NN_*.wav|mp3 played after a shot when UsePlaySound is on
    // (see ShutterSounds). Persisted; default 1. A number with no file falls back to the first sound.
    public int ShutterSound
    {
        get => _shutterSound;
        set
        {
            if (_shutterSound == value) return;
            _shutterSound = value;
            NotifyPropertyChanged("ShutterSound");
        }
    }

    private bool _useMeasureCapture;

    // 촬영 설정 > 치수 촬영 (M): shows the 치수 button (half width, left of 앞면) and the 치수자동저장 box above 앞면.
    // Off (default): the screen keeps 앞면/뒷면 and 앞면 saves the measurement copy as before.
    public bool UseMeasureCapture
    {
        get => _useMeasureCapture;
        set
        {
            if (_useMeasureCapture == value) return;
            _useMeasureCapture = value;
            NotifyPropertyChanged("UseMeasureCapture");
            NotifyPropertyChanged("FrontSavesMeasure");
        }
    }

    private bool _measureAutoSave = true;

    // 치수자동저장 (checked by default): 앞면 also saves 치수측정\*_M.jpeg. Unchecked: only the 치수 button makes it.
    public bool MeasureAutoSave
    {
        get => _measureAutoSave;
        set
        {
            if (_measureAutoSave == value) return;
            _measureAutoSave = value;
            NotifyPropertyChanged("MeasureAutoSave");
            NotifyPropertyChanged("FrontSavesMeasure");
        }
    }

    // Whether an 앞면 shot writes the measurement copy.
    [System.Xml.Serialization.XmlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool FrontSavesMeasure => !UseMeasureCapture || MeasureAutoSave;

    // Runtime only: the next download is a 치수 shot (only 치수측정\*_M.jpeg is kept, named after the item's 앞면).
    [System.Xml.Serialization.XmlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsMeasureOnly { get; set; }

    private bool _useLargeText;

    // G2 UI: 큰 글자 보기 — main screen text x1.35 (product name excluded). Persisted; default off.
    public bool UseLargeText
    {
        get => _useLargeText;
        set
        {
            if (_useLargeText == value) return;
            _useLargeText = value;
            NotifyPropertyChanged("UseLargeText");
        }
    }

    private bool _useCloriumLight;

    // G2: 조명 조절 — Clorium Light Control (Godox Bluetooth app). Replaces the removed DMX controller;
    // ModeLighting (촬영 후 조명 끄기) and LightPowerSaveTime keep their meaning. Persisted; default off.
    public bool UseCloriumLight
    {
        get => _useCloriumLight;
        set
        {
            if (_useCloriumLight == value) return;
            _useCloriumLight = value;
            NotifyPropertyChanged("UseCloriumLight");
            NotifyPropertyChanged("ShowLightingPanel");
        }
    }

    private string _cloriumLightPath = "";

    // Empty = <G2 folder>\CloriumLightControl\CloriumLightControl.exe
    public string CloriumLightPath
    {
        get => _cloriumLightPath;
        set
        {
            if (_cloriumLightPath == value) return;
            _cloriumLightPath = value ?? "";
            NotifyPropertyChanged("CloriumLightPath");
        }
    }

    private bool _useAutoSizing;

    // G2: 오토사이징 — Clorium AutoSizing Desk (garment measurement app). Replaces the removed legacy
    // "C:\Program Files (x86)\Clorium\Clorium AutoSizing.exe" launcher; ON shows the left-rail 오토사이징 button.
    // MetaScale/DiskSize (치수측정\*_M.jpeg Subject scale) are unchanged. Persisted; default off.
    public bool UseAutoSizing
    {
        get => _useAutoSizing;
        set
        {
            if (_useAutoSizing == value) return;
            _useAutoSizing = value;
            NotifyPropertyChanged("UseAutoSizing");
        }
    }

    private string _autoSizingPath = "";

    // Empty = <G2 folder>\CloriumAutoSizing\Clorium AutoSizing.exe
    public string AutoSizingPath
    {
        get => _autoSizingPath;
        set
        {
            if (_autoSizingPath == value) return;
            _autoSizingPath = value ?? "";
            NotifyPropertyChanged("AutoSizingPath");
        }
    }

    private bool _useBackgroundRemoval;

    // G2: 배경제거 — Clorium Matting Studio (local BiRefNet web app, 127.0.0.1:8765). Replaces the removed
    // PhotoScissors launcher, PhotoRoom window and 배경 제거 촬영 mode; ON shows the left-rail 배경제거 button.
    // Legacy Background/ShowRemoveBg/ModeBg* stay only for settings-file compatibility. Persisted; default off.
    public bool UseBackgroundRemoval
    {
        get => _useBackgroundRemoval;
        set
        {
            if (_useBackgroundRemoval == value) return;
            _useBackgroundRemoval = value;
            NotifyPropertyChanged("UseBackgroundRemoval");
        }
    }

    private string _backgroundRemovalPath = "";

    // Matting Studio Start.ps1. Empty = <G2 folder>\CloriumBackgroundRemoval\outputs\matting-studio\Start.ps1
    public string BackgroundRemovalPath
    {
        get => _backgroundRemovalPath;
        set
        {
            if (_backgroundRemovalPath == value) return;
            _backgroundRemovalPath = value ?? "";
            NotifyPropertyChanged("BackgroundRemovalPath");
        }
    }

    private bool _useImageAdjust;

    // G2: 이미지보정 — built-in colour correction window (G2ColorAdjustWnd, Magick.NET: templates, _cc batch, side-by-side).
    // Replaces the removed ColorCompensationWnd (thumbnail menu, D:\Clorium Data\Clorium_ColorCompensation.json);
    // ON shows the left-rail 이미지보정 button. Templates stay in %APPDATA%\Clorium\G2\color-templates.xml. Persisted; default off.
    public bool UseImageAdjust
    {
        get => _useImageAdjust;
        set
        {
            if (_useImageAdjust == value) return;
            _useImageAdjust = value;
            NotifyPropertyChanged("UseImageAdjust");
        }
    }

    private string _backupFolder = "";

    // 촬영 > 항상 원본 백업: target folder. Empty = the original rule (D:\<top folder> Backup\<same sub folders>);
    // otherwise <BackupFolder>\<source path without the drive>.
    public string BackupFolder
    {
        get => _backupFolder;
        set
        {
            if (_backupFolder == value) return;
            _backupFolder = value ?? "";
            NotifyPropertyChanged("BackupFolder");
        }
    }

    private bool _useDarkScreen;

    // 화면과 언어 > 테마: 밝은 화면 (default) / 어두운 화면. Replaces the MaterialDesign colour list (CurrentThemeNameNew is
    // kept only for settings-file compatibility). G2 windows read it at start-up (G2Palette), so a change needs a restart.
    public bool UseDarkScreen
    {
        get => _useDarkScreen;
        set
        {
            if (_useDarkScreen == value) return;
            _useDarkScreen = value;
            NotifyPropertyChanged("UseDarkScreen");
            NotifyPropertyChanged("ScreenTheme");
        }
    }

    // Settings window selector: 0 = 밝은 화면, 1 = 어두운 화면.
    [System.Xml.Serialization.XmlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int ScreenTheme
    {
        get => UseDarkScreen ? 1 : 0;
        set => UseDarkScreen = value == 1;
    }

    private int _calibrationTarget;

    // 오토사이징 캘리브레이션 판: 0 = 원판 (DiskSize diameter, MetaScale only), 1 = 정사각형 판 (SquareBoardSize; X/Y scale and
    // perspective from its four detected corners, saved in SquareCalibration and written to 치수측정\*_M.jpeg only).
    public int CalibrationTarget
    {
        get => _calibrationTarget;
        set
        {
            if (_calibrationTarget == value) return;
            _calibrationTarget = value;
            NotifyPropertyChanged("CalibrationTarget");
            NotifyPropertyChanged("SquareCalibrationSummary");
        }
    }

    private double _squareBoardSize = 500.0;

    public double SquareBoardSize
    {
        get => _squareBoardSize;
        set
        {
            if (_squareBoardSize == value) return;
            _squareBoardSize = value;
            NotifyPropertyChanged("SquareBoardSize");
        }
    }

    private string _squareCalibration = "";

    // Last accepted square-board calibration (SquareBoardCalibration.Format); empty = not calibrated yet.
    public string SquareCalibration
    {
        get => _squareCalibration;
        set
        {
            if (_squareCalibration == value) return;
            _squareCalibration = value ?? "";
            NotifyPropertyChanged("SquareCalibration");
            NotifyPropertyChanged("SquareCalibrationSummary");
        }
    }

    [System.Xml.Serialization.XmlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string SquareCalibrationSummary => SquareBoardCalibration.Describe(SquareCalibration);

    // Main screen 조명 켜기/끄기 panel: the light app and/or the USB LED dimmer.
    [System.Xml.Serialization.XmlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool ShowLightingPanel => UseCloriumLight || (ServiceProvider.Branding?.ControlLEDDimmer ?? false);
}
