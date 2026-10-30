using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.Plugins.ExternalDevices;
public partial class HidUsbRelayConfig : UserControl
{
    public CustomConfig CustomConfig { get; set; }

    public HidUsbRelayConfig(CustomConfig config)
    {
        CustomConfig = config;
        InitializeComponent();
    }

    public HidUsbRelayConfig()
    {
        CustomConfig = new CustomConfig();
        InitializeComponent();
    }
}
