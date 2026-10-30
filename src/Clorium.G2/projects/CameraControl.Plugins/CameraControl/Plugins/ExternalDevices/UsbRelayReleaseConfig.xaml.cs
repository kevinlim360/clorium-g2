using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.Plugins.ExternalDevices;
public partial class UsbRelayReleaseConfig : UserControl
{
    private SerialPort sp = new SerialPort();
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

    public string Init
    {
        get
        {
            return CustomConfig.Get("Init");
        }

        set
        {
            CustomConfig.Set("Init", value);
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

    public UsbRelayReleaseConfig()
    {
        //IL_0001: Unknown result type (might be due to invalid IL or missing references)
        //IL_000b: Expected Obj, but got Unknown
        CustomConfig = new CustomConfig();
        InitializeComponent();
    }

    public UsbRelayReleaseConfig(CustomConfig config)
    {
        //IL_0001: Unknown result type (might be due to invalid IL or missing references)
        //IL_000b: Expected Obj, but got Unknown
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
