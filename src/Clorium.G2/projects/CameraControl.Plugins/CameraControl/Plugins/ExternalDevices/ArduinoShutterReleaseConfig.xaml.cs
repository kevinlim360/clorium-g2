using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.Plugins.ExternalDevices;
public partial class ArduinoShutterReleaseConfig : UserControl
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

    public string CaptureOn
    {
        get
        {
            return CustomConfig.Get("CaptureOn");
        }

        set
        {
            CustomConfig.Set("CaptureOn", value);
        }
    }

    public string CaptureOff
    {
        get
        {
            return CustomConfig.Get("CaptureOff");
        }

        set
        {
            CustomConfig.Set("CaptureOff", value);
        }
    }

    public bool IsHexa
    {
        get
        {
            return CustomConfig.Get("IsHexa") == "True";
        }

        set
        {
            CustomConfig.Set("IsHexa", value.ToString());
        }
    }

    public ArduinoShutterReleaseConfig()
    {
        CustomConfig = new CustomConfig();
        InitializeComponent();
    }

    public ArduinoShutterReleaseConfig(CustomConfig config)
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
