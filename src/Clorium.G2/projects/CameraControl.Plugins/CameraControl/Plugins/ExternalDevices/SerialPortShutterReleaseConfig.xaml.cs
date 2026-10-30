using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.Plugins.ExternalDevices;
public partial class SerialPortShutterReleaseConfig : UserControl
{
    public CustomConfig CustomConfig { get; set; }

    public string Port
    {
        get
        {
            return CustomConfig.Get("Port");
        }

        set
        {
            CustomConfig.Set("Port", value);
        }
    }

    public SerialPortShutterReleaseConfig()
    {
        InitializeComponent();
        CustomConfig = new CustomConfig();
    }

    public SerialPortShutterReleaseConfig(CustomConfig config)
    {
        CustomConfig = config;
        InitializeComponent();
        string[] ports = SerialPort.GetPortNames();
        string[] array = ports;
        foreach (string port in array)
        {
            ((ItemsControl)cmb_ports).Items.Add((object)port);
        }
    }
}
