using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Threading;
using CameraControl.Core.Classes;
using CameraControl.Devices;

namespace CameraControl.Plugins.ExternalDevices;
public partial class DCCUSBShutterReleaseConfig : UserControl
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

    public bool IrRemote
    {
        get
        {
            return CustomConfig.Get("IrRemote") == "True";
        }

        set
        {
            CustomConfig.Set("IrRemote", value.ToString());
        }
    }

    public DCCUSBShutterReleaseConfig()
    {
        //IL_0001: Unknown result type (might be due to invalid IL or missing references)
        //IL_000b: Expected Obj, but got Unknown
        InitializeComponent();
        CustomConfig = new CustomConfig();
    }

    public DCCUSBShutterReleaseConfig(CustomConfig config)
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

    private void btn_vers_Click(object sender, RoutedEventArgs e)
    {
        //IL_00ab: Unknown result type (might be due to invalid IL or missing references)
        //IL_0084: Unknown result type (might be due to invalid IL or missing references)
        //IL_008e: Expected Obj, but got Unknown
        //IL_001f: Unknown result type (might be due to invalid IL or missing references)
        //IL_0029: Expected Obj, but got Unknown
        try
        {
            if (sp.IsOpen)
            {
                sp.DataReceived -= sp_DataReceived;
                sp.Close();
            }

            sp.PortName = Port;
            sp.BaudRate = 9600;
            sp.WriteTimeout = 3500;
            sp.Open();
            sp.DataReceived += sp_DataReceived;
            sp.WriteLine("v");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            Log.Error("Serial error", ex);
            ((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
            {
                ((ContentControl)lbl_mess).Content = "Error";
            }));
        }
    }

    private void sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        //IL_000f: Unknown result type (might be due to invalid IL or missing references)
        //IL_0015: Expected Obj, but got Unknown
        SerialPort spL = (SerialPort)sender;
        string str = spL.ReadLine();
        ((DispatcherObject)this).Dispatcher.Invoke((Action)(() =>
        {
            ((ContentControl)lbl_mess).Content = str;
        }));
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        //IL_001f: Unknown result type (might be due to invalid IL or missing references)
        //IL_0029: Expected Obj, but got Unknown
        try
        {
            if (sp.IsOpen)
            {
                sp.DataReceived -= sp_DataReceived;
                sp.Close();
            }
        }
        catch (Exception)
        {
        }
    }
}
