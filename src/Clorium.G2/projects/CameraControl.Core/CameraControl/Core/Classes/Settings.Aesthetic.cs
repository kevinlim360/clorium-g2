using System.Xml.Serialization;
using Newtonsoft.Json;

namespace CameraControl.Core.Classes;

public partial class Settings
{
    private bool _useAestheticImprovement;
    private bool _isAestheticImprovement = true;

    // This feature switch is persisted; existing configurations keep their legacy behavior.
    public bool UseAestheticImprovement
    {
        get => _useAestheticImprovement;
        set
        {
            if (_useAestheticImprovement == value) return;
            _useAestheticImprovement = value;
            NotifyPropertyChanged("UseAestheticImprovement");
            NotifyPropertyChanged("IsAestheticImprovementActive");
        }
    }

    // The operator's mode lasts across products, but starts ON in each new application instance.
    [XmlIgnore, JsonIgnore]
    public bool IsAestheticImprovement
    {
        get => _isAestheticImprovement;
        set
        {
            if (_isAestheticImprovement == value) return;
            if (!CanChangeAestheticImprovement)
            {
                NotifyPropertyChanged("IsAestheticImprovement");
                return;
            }
            _isAestheticImprovement = value;
            NotifyPropertyChanged("IsAestheticImprovement");
            NotifyPropertyChanged("IsAestheticImprovementActive");
        }
    }

    [XmlIgnore, JsonIgnore]
    public bool IsAestheticImprovementActive => UseAestheticImprovement && IsAestheticImprovement;

    [XmlIgnore, JsonIgnore]
    public bool CanChangeAestheticImprovement => !isBtnPressed && ThumbailLoaded && !TurnTableShot;
}