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
}
