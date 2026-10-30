using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class CameraPropertyWnd : Window, IWindow, INotifyPropertyChanged, IComponentConnector
{
	private ICameraDevice _cameraDevice;

	private AsyncObservableCollection<string> _cameraPresets;

	public AsyncObservableCollection<string> AvailableKeys { get; set; }

	public CameraProperty CameraProperty
	{
		get
		{
			return ((CameraPropertyViewModel)DataContext).CameraProperty;
		}
		set
		{
			((CameraPropertyViewModel)DataContext).CameraProperty = value;
			NotifyPropertyChanged("CameraProperty");
		}
	}

	public AsyncObservableCollection<string> CameraPresets
	{
		get
		{
			return _cameraPresets;
		}
		set
		{
			_cameraPresets = value;
			NotifyPropertyChanged("CameraPresets");
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public CameraPropertyWnd()
	{
		AvailableKeys = new AsyncObservableCollection<string>();
		InitializeComponent();
		string[] names = Enum.GetNames(typeof(Key));
		foreach (string key in names)
		{
			AvailableKeys.Add(key);
		}
		CameraPresets = new AsyncObservableCollection<string>();
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "CameraPropertyWnd_Show":
			((CameraPropertyViewModel)DataContext).Init();
			_cameraDevice = param as ICameraDevice;
			if (_cameraDevice != null)
			{
				CameraProperty = _cameraDevice.LoadProperties();
				CameraProperty.BeginEdit();
				Dispatcher.Invoke(() =>
				{
					Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
					Show();
					Activate();
					Focus();
				});
			}
			break;
		case "CameraPropertyWnd_Hide":
			CameraProperty = null;
			Hide();
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				Hide();
				Close();
			});
			break;
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("CameraPropertyWnd_Hide");
		}
	}

	public virtual void NotifyPropertyChanged(string info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}

	private void btn_save_Click(object sender, RoutedEventArgs e)
	{
		CameraProperty.EndEdit();
		ServiceProvider.Settings.Save();
		_cameraDevice.LoadProperties();
		ServiceProvider.WindowsManager.ExecuteCommand("CameraPropertyWnd_Hide");
		ServiceProvider.WindowsManager.ExecuteCommand("SortCameras");
	}

	private void btn_cancel_Click(object sender, RoutedEventArgs e)
	{
		CameraProperty.CancelEdit();
		ServiceProvider.WindowsManager.ExecuteCommand("CameraPropertyWnd_Hide");
	}

	private void btn_identify_Click(object sender, RoutedEventArgs e)
	{
		Thread thread = new Thread(() =>
		{
			for (int i = 0; i < 5; i++)
			{
				_cameraDevice.LockCamera();
				Thread.Sleep(800);
				_cameraDevice.UnLockCamera();
				Thread.Sleep(800);
			}
		});
		thread.Start();
	}

	private void btn_dateTime_Click(object sender, RoutedEventArgs e)
	{
		if (ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
		{
			ServiceProvider.DeviceManager.SelectedCameraDevice.DateTime = DateTime.Now;
		}
	}
}
