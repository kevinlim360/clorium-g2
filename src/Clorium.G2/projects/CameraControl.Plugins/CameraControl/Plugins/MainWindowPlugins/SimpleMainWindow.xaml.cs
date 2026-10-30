using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.Plugins.MainWindowPlugins;
public partial class SimpleMainWindow : Window, IMainWindowPlugin, INotifyPropertyChanged
{
    private string _saveFolder;
    public string DisplayName { get; set; }

    public string SaveFolder
    {
        get
        {
            return _saveFolder;
        }

        set
        {
            _saveFolder = value;
            NotifyPropertyChanged("SaveFolder");
        }
    }

    public virtual event PropertyChangedEventHandler PropertyChanged;
    public SimpleMainWindow()
    {
        DisplayName = "Simple Capture";
        InitializeComponent();
    }

    private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
    {
        //IL_0006: Unknown result type (might be due to invalid IL or missing references)
        MessageBox.Show("This is a demostration of Clorium plugin usage.\nFor normal usage please restart the application and select Default window!");
        SaveFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        ServiceProvider.DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
    }

    private void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
    {
        PhotoCaptured(eventArgs);
    }

    private void MetroWindow_Closed(object sender, EventArgs e)
    {
        ServiceProvider.WindowsManager.ExecuteCommand("All_Close");
    }

    public virtual void NotifyPropertyChanged(string info)
    {
        if (PropertyChanged != null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(info));
        }
    }

    private void btn_capture_Click(object sender, RoutedEventArgs e)
    {
        //IL_0027: Unknown result type (might be due to invalid IL or missing references)
        try
        {
            ServiceProvider.DeviceManager.SelectedCameraDevice.CapturePhoto();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error occurred :" + ex.Message);
        }
    }

    private void btn_Click(object sender, RoutedEventArgs e)
    {
        //IL_0001: Unknown result type (might be due to invalid IL or missing references)
        //IL_0007: Expected Obj, but got Unknown
        //IL_0015: Unknown result type (might be due to invalid IL or missing references)
        //IL_001b: Invalid comparison between Unknown and I4
        FolderBrowserDialog dialog = new FolderBrowserDialog();
        dialog.SelectedPath = SaveFolder;
        if ((int)((System.Windows.Forms.CommonDialog)dialog).ShowDialog() == 1)
        {
            SaveFolder = dialog.SelectedPath;
        }
    }

    private void PhotoCaptured(object o)
    {
        //IL_00c1: Unknown result type (might be due to invalid IL or missing references)
        if (!(o is PhotoCapturedEventArgs eventArgs))
        {
            return;
        }

        try
        {
            string fileName = Path.Combine(SaveFolder, eventArgs.FileName);
            if (File.Exists(fileName))
            {
                fileName = StaticHelper.GetUniqueFilename(Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_", 0, Path.GetExtension(fileName));
            }

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            eventArgs.CameraDevice.TransferFile(eventArgs.Handle, fileName);
            eventArgs.CameraDevice.IsBusy = false;
        }
        catch (Exception ex)
        {
            eventArgs.CameraDevice.IsBusy = false;
            MessageBox.Show("Error download photo from camera :\n" + ex.Message);
        }
    }

    private void Btn_showfolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "explorer";
            processStartInfo.UseShellExecute = true;
            processStartInfo.WindowStyle = ProcessWindowStyle.Normal;
            processStartInfo.Arguments = $"/e,/select,\"{SaveFolder}\"";
            Process.Start(processStartInfo);
        }
        catch (Exception exception)
        {
            Log.Error("Error to show file in explorer", exception);
        }
    }

    private void btn_top_Click(object sender, RoutedEventArgs e)
    {
        ((Window)this).Topmost = !((Window)this).Topmost;
    }

    void IMainWindowPlugin.Show()
    {
        ((Window)this).Show();
    }
}
