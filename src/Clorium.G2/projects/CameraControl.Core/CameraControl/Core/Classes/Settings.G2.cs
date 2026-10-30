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
}
