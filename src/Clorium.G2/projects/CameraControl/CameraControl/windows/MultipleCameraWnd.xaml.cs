using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;
using CameraControl.Core.Wpf;
using CameraControl.Devices;

namespace CameraControl.windows;

public partial class MultipleCameraWnd : Window, IWindow, INotifyPropertyChanged, IComponentConnector
{
	private ProgressWindow dlg = new ProgressWindow();

	private System.Timers.Timer _timer = new System.Timers.Timer(1000.0);

	private int _secounter = 0;

	private int _photocounter = 0;

	public CustomConfig SelectedConfig { get; set; }

	public bool UseExternal { get; set; }

	public bool DisbleAutofocus { get; set; }

	public int DelaySec { get; set; } = 1;

	public int WaitSec { get; set; } = 1;

	public int NumOfPhotos { get; set; } = 1;

	public int PhotoCounter
	{
		get
		{
			return _photocounter;
		}
		set
		{
			if (_photocounter != value)
			{
				_photocounter = value;
				NotifyPropertyChanged("PhotoCounter");
			}
		}
	}

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public MultipleCameraWnd()
	{
		InitializeComponent();
		_timer.Elapsed += _timer_Elapsed;
		DataContext = this;
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		_secounter++;
		if (_secounter > WaitSec)
		{
			_secounter = 0;
			_timer.Stop();
			CapturePhotos();
		}
		else
		{
			StaticHelper.Instance.SystemMessage = $"Waiting {_secounter})";
		}
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "MultipleCameraWnd_Show":
			Dispatcher.Invoke(() =>
			{
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				Show();
				Activate();
				Focus();
			});
			{
				foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.IsConnected && cameraDevice.IsChecked))
				{
					if (!(connectedDevice.DeviceName == "Z-App camera") && !(connectedDevice.DeviceName == "Z-FTP camera") && !(connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName))
					{
						ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Show", connectedDevice);
					}
				}
				break;
			}
		case "MultipleCameraWnd_Hide":
			Hide();
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				Hide();
				Close();
			});
			break;
		case "MultipleCamera_Start":
			Dispatcher.Invoke(() =>
			{
				btn_Capture_Click(null, null);
			});
			break;
		case "MultipleCamera_Stop":
			Dispatcher.Invoke(() =>
			{
				btn_Stop_Click(null, null);
			});
			break;
		case "MultipleCamera_Reset":
			Dispatcher.Invoke(() =>
			{
				btn_resetCounters_Click(null, null);
			});
			break;
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("MultipleCameraWnd_Hide");
		}
	}

	private void MetroWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		TriggerClass.KeyDown(e);
	}

	private void CapturePhotos()
	{
		StaticHelper.Instance.SystemMessage = $"Repetitation Count : {++PhotoCounter}";
		Thread thread = new Thread(() =>
		{
			while (CamerasAreBusy())
			{
			}
			try
			{
				if (UseExternal)
				{
					if (SelectedConfig != null)
					{
						ServiceProvider.ExternalDeviceManager.OpenShutter(SelectedConfig);
						Thread.Sleep(300);
						ServiceProvider.ExternalDeviceManager.CloseShutter(SelectedConfig);
					}
				}
				else
				{
					CameraHelper.CaptureAll(DelaySec * 1000);
				}
			}
			catch (Exception message)
			{
				Log.Error(message);
			}
			Thread.Sleep(DelaySec * 1000);
			if (PhotoCounter < NumOfPhotos)
			{
				_timer.Start();
			}
			else
			{
				StopCapture();
			}
		});
		thread.Start();
	}

	private bool CamerasAreBusy()
	{
		return ServiceProvider.DeviceManager.ConnectedDevices.Aggregate(seed: false, (bool current, ICameraDevice connectedDevice) => connectedDevice.IsBusy | current);
	}

	private void StopCapture()
	{
		if (UseExternal && SelectedConfig != null)
		{
			ServiceProvider.ExternalDeviceManager.CloseShutter(SelectedConfig);
			ServiceProvider.ExternalDeviceManager.DeassertFocus(SelectedConfig);
		}
		StaticHelper.Instance.SystemMessage = "All captures done !";
	}

	private void btn_Capture_Click(object sender, RoutedEventArgs e)
	{
		if (UseExternal)
		{
			try
			{
				if (SelectedConfig != null)
				{
					ServiceProvider.ExternalDeviceManager.AssertFocus(SelectedConfig);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Error set focus", ex);
				StaticHelper.Instance.SystemMessage = "Error set focus" + ex.Message;
			}
		}
		_secounter = 0;
		PhotoCounter = 0;
		_timer.Start();
	}

	private void btn_Stop_Click(object sender, RoutedEventArgs e)
	{
		_timer.Stop();
		PhotoCounter = NumOfPhotos;
		StopCapture();
	}

	private void btn_help_Click(object sender, RoutedEventArgs e)
	{
		CameraControl.Core.Classes.HelpProvider.Run(HelpSections.MultipleCamera);
	}

	private void btn_resetCounters_Click(object sender, RoutedEventArgs e)
	{
		foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices)
		{
			CameraProperty property = ServiceProvider.Settings.CameraProperties.Get(connectedDevice);
			property.Counter = 0;
		}
	}

	private void btn_liveview_Click(object sender, RoutedEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("MultipleLiveViewWnd_Show");
	}

	private void chk_noautofocus_Checked(object sender, RoutedEventArgs e)
	{
		System.Windows.Forms.MessageBox.Show("This feature not working reliable ");
	}

	private void btn_format_Click(object sender, RoutedEventArgs e)
	{
		if (System.Windows.Forms.MessageBox.Show(TranslationStrings.LabelAskForDelete, "", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
		{
			dlg.Show();
			Thread thread = new Thread(Format);
			thread.Start();
			Log.Debug("Start format multiple cameras");
		}
	}

	private void Format()
	{
		for (int i = 0; i < ServiceProvider.DeviceManager.ConnectedDevices.Count; i++)
		{
			ICameraDevice device = ServiceProvider.DeviceManager.ConnectedDevices[i];
			if (!(device.DeviceName == "Z-App camera") && !(device.DeviceName == "Z-FTP camera") && !(device.DeviceName == ServiceProvider.Settings.WebCam_DeviceName) && device.IsChecked)
			{
				dlg.Label = device.DisplayName;
				dlg.Progress = i;
				Thread thread = new Thread(() =>
				{
					FormatCard(device);
				});
				thread.Start();
				thread.Join(5000);
			}
		}
		Dispatcher.Invoke(dlg.Hide);
	}

	private void FormatCard(ICameraDevice connectedDevice)
	{
		try
		{
			connectedDevice.IsBusy = true;
			Log.Debug("Start format");
			Log.Debug(connectedDevice.PortName);
			connectedDevice.FormatStorage(null);
			Thread.Sleep(200);
			Log.Debug("Format done");
			connectedDevice.IsBusy = false;
		}
		catch (Exception exception)
		{
			Log.Error("Unable to format device ", exception);
		}
	}

	private void btn_focus_Click(object sender, RoutedEventArgs e)
	{
		if (SelectedConfig != null)
		{
			ServiceProvider.ExternalDeviceManager.Focus(SelectedConfig);
		}
	}

	private void btn_capture_Click(object sender, RoutedEventArgs e)
	{
		if (SelectedConfig != null)
		{
			ServiceProvider.ExternalDeviceManager.Capture(SelectedConfig);
		}
	}

	private void btn_saveOrder_Click(object sender, RoutedEventArgs e)
	{
		for (int i = 0; i < ServiceProvider.DeviceManager.ConnectedDevices.Count; i++)
		{
			ICameraDevice device = ServiceProvider.DeviceManager.ConnectedDevices[i];
			if (!(device.DeviceName == "Z-App camera") && !(device.DeviceName == "Z-FTP camera") && !(device.DeviceName == ServiceProvider.Settings.WebCam_DeviceName))
			{
				ServiceProvider.DeviceManager.ConnectedDevices[i].LoadProperties().SortOrder = i;
			}
		}
		ServiceProvider.Settings.Save();
	}

	private void MenuItem_Click(object sender, RoutedEventArgs e)
	{
	}

	private void MenuItem_Click_1(object sender, RoutedEventArgs e)
	{
	}
}
