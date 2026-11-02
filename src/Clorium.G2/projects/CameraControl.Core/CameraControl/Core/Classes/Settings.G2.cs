namespace CameraControl.Core.Classes;

public partial class Settings
{
    private bool _useSuperfocus = true;

    // G2 UI: Superfocus entry point ON/OFF (main menu > 설정). Persisted; missing in older settings files = ON.
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

    // Main screen 조명 켜기/끄기 panel: the light app and/or the USB LED dimmer.
    [System.Xml.Serialization.XmlIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool ShowLightingPanel => UseCloriumLight || (ServiceProvider.Branding?.ControlLEDDimmer ?? false);
}
