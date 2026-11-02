using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;

namespace CameraControl.windows;

public partial class SettingsWnd : Window, IComponentConnector
{
	public AsyncObservableCollection<RotateFlipType> RotateFlipTypesValues { get; set; }

	public AsyncObservableCollection<string> AvailableKeys { get; set; }

	public SettingsWnd()
	{
		AvailableKeys = new AsyncObservableCollection<string>();
		InitializeComponent();
		string[] names = Enum.GetNames(typeof(Key));
		foreach (string key in names)
		{
			AvailableKeys.Add(key);
		}
		RotateFlipTypesValues = new AsyncObservableCollection<RotateFlipType>(Enum.GetValues(typeof(RotateFlipType)).Cast<RotateFlipType>().Distinct());
		DataContext = ServiceProvider.Settings;
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		ServiceProvider.Settings.BeginEdit();
	}

	private void Btn_OK_Click(object sender, RoutedEventArgs e)
	{
		ServiceProvider.Settings.EndEdit();
		ServiceProvider.Settings.Save();
		ServiceProvider.WindowsManager.ApplyTheme();
		Close();
	}

	private void Btn_Cancle_Click(object sender, RoutedEventArgs e)
	{
		ServiceProvider.Settings.CancelEdit();
		Close();
	}

	private void Btn_reset_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("설정 초기화 Click");
		ServiceProvider.Settings.ResetSettings();
	}

	private void Btn_ExternalViewer_Click(object sender, RoutedEventArgs e)
	{
		Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
		dialog.FileName = ServiceProvider.Settings.ExternalViewerPath;
		if (dialog.ShowDialog() == true)
		{
			ServiceProvider.Settings.ExternalViewerPath = dialog.FileName;
		}
	}

	private void Btn_CloriumLightPath_Click(object sender, RoutedEventArgs e)
	{
		Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
		dialog.Filter = "CloriumLightControl.exe|CloriumLightControl.exe|프로그램 (*.exe)|*.exe";
		dialog.FileName = ServiceProvider.Settings.CloriumLightPath;
		if (dialog.ShowDialog() == true)
		{
			ServiceProvider.Settings.CloriumLightPath = dialog.FileName;
		}
	}

	private void Btn_AutoSizingPath_Click(object sender, RoutedEventArgs e)
	{
		Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
		dialog.Filter = "Clorium AutoSizing.exe|Clorium AutoSizing.exe;Clorium Measure.exe|프로그램 (*.exe)|*.exe";
		dialog.FileName = ServiceProvider.Settings.AutoSizingPath;
		if (dialog.ShowDialog() == true)
		{
			ServiceProvider.Settings.AutoSizingPath = dialog.FileName;
		}
	}

	private void Btn_add_device_Click(object sender, RoutedEventArgs e)
	{
		if (ServiceProvider.ExternalDeviceManager.ExternalDevices.Count == 0)
		{
			System.Windows.MessageBox.Show("No external devices are defined");
			return;
		}
		ServiceProvider.Settings.DeviceConfigs.Items.Add(new CustomConfig
		{
			Name = "New device config",
			DriverName = ServiceProvider.ExternalDeviceManager.ExternalDeviceNames[0]
		});
		ServiceProvider.ExternalDeviceManager.RefreshDeviceLists();
	}

	private void Btn_del_device_Click(object sender, RoutedEventArgs e)
	{
		if (lst_device.SelectedItem != null && System.Windows.MessageBox.Show("Do you want to delete the selected device configuration ?", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
		{
			CustomConfig config = lst_device.SelectedItem as CustomConfig;
			ServiceProvider.Settings.DeviceConfigs.Items.Remove(config);
			ServiceProvider.ExternalDeviceManager.RefreshDeviceLists();
		}
	}

	private void Btn_ShowOverlayFolder_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("Show Overlay Folder Click");
		if (!Directory.Exists(ServiceProvider.Settings.OverlayFolder))
		{
			Directory.CreateDirectory(ServiceProvider.Settings.OverlayFolder);
		}
		PhotoUtils.Run("explorer", ServiceProvider.Settings.OverlayFolder, ProcessWindowStyle.Normal);
	}

	private void Btn_browse_file_Click(object sender, RoutedEventArgs e)
	{
		Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
		dialog.FileName = ServiceProvider.Settings.ExternalViewer;
		if (dialog.ShowDialog() == true)
		{
			ServiceProvider.Settings.ExternalViewer = dialog.FileName;
		}
	}

	private void Btn_help_Click(object sender, RoutedEventArgs e)
	{
		CameraControl.Core.Classes.HelpProvider.Run(HelpSections.Settings);
	}

	private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
	{
		PhotoUtils.Run("http://www.clorium.com");
	}

	private void Btn_browse_folder_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Log.Debug("이미지 저장 폴더 Click");
			using FolderBrowserDialog dialog = new FolderBrowserDialog();
			dialog.Description = "캡쳐 이미지를 저장할 폴더를 만드세요.";
			dialog.RootFolder = Environment.SpecialFolder.MyComputer;
			dialog.SelectedPath = "D:\\";
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				if (dialog.SelectedPath.Contains("새 폴더"))
				{
					System.Windows.MessageBox.Show("폴더 이름을 바꾸고 엔터를 눌러 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				}
				ServiceProvider.Settings.DownloadFolder = dialog.SelectedPath + "\\";
			}
		}
		catch (Exception exception)
		{
			Log.Error("폴더를 만들 수 없습니다.", exception);
			System.Windows.MessageBox.Show("폴더를 만들 수 없습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void Btn_TermReset_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("기간 초기화 Click");
		ServiceProvider.Settings.ProcessCounter = int.Parse(ServiceProvider.Settings.SessionFileKeepDays);
		ServiceProvider.Settings.RemoveSessionFile(ServiceProvider.Settings.SessionFileKeepDays);
	}

	private void Btn_FactoryReset_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (System.Windows.MessageBox.Show("모든 작업 데이터가 삭제되고 프로그램은 다시 시작됩니다.", "경고", MessageBoxButton.YesNo) != MessageBoxResult.No)
			{
				Log.Debug("공장 초기화 Click");
				Directory.Delete(Path.Combine(Settings.DataFolder, "Cache"), recursive: true);
				Directory.Delete(Path.Combine(Settings.DataFolder, "Sessions"), recursive: true);
				File.Delete(Settings.DataFolder + "\\settings.json");
				Process.Start(new ProcessStartInfo
				{
					FileName = "cmd.exe",
					Arguments = "/C timeout /t 3 & del \"" + Settings.DataFolder + "\\database.db\" & start \"\" \"C:\\Program Files (x86)\\Clorium\\CameraControl.exe\"",
					WindowStyle = ProcessWindowStyle.Hidden
				});
				Environment.Exit(0);
			}
		}
		catch (Exception ex)
		{
			System.Windows.MessageBox.Show("모든 작업 데이터 삭제에 실패했습니다.\r\n" + ex);
		}
	}

	private void Btn_BackupSettings_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("설정 백업 Click");
		string dstFolder = "D:\\Clorium Data\\Backup 설정\\";
		string date = DateTime.Now.ToString("yyyy-MM-dd HH시mm분");
		if (!Directory.Exists(dstFolder))
		{
			Directory.CreateDirectory(dstFolder);
		}
		string src = Path.Combine("D:\\Clorium Data\\Clorium_LightPresets.json");
		string dst = dstFolder + date + " Clorium_LightPresets.json";
		// DMX light presets were removed in G2; older installs may still have the file.
		if (File.Exists(src)) File.Copy(src, dst, overwrite: true);
		src = Path.Combine("D:\\Clorium Data\\Clorium_MDPData.txt");
		dst = dstFolder + date + " Clorium_MDPData.txt";
		File.Copy(src, dst, overwrite: true);
		src = Path.Combine("D:\\Clorium Data\\Clorium_Measure_Settings.txt");
		dst = dstFolder + date + " Clorium_Measure_Settings.txt";
		File.Copy(src, dst, overwrite: true);
		src = Path.Combine("D:\\Clorium Data\\Clorium_MeasureItems.txt");
		dst = dstFolder + date + " Clorium_MeasureItems.txt";
		File.Copy(src, dst, overwrite: true);
		src = Path.Combine(Settings.DataFolder, "settings.json");
		dst = dstFolder + date + " settings.json";
		File.Copy(src, dst, overwrite: true);
		src = Path.Combine(Settings.DataFolder + "\\Presets");
		dst = dstFolder + "Presets";
		FileSystem.CopyDirectory(src, dst, overwrite: true);
		src = Path.Combine(Settings.DataFolder + "\\LiveViewOverlay");
		dst = dstFolder + "LiveViewOverlay";
		FileSystem.CopyDirectory(src, dst, overwrite: true);
	}

	private void Btn_elapsed_folder_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Log.Debug("작업 통계파일(csv) 저장 폴더 Click");
			using FolderBrowserDialog dialog = new FolderBrowserDialog();
			dialog.Description = "작업 통계 데이터 파일(csv)을 저장할 폴더를 만드세요.";
			dialog.RootFolder = Environment.SpecialFolder.MyComputer;
			dialog.SelectedPath = "D:\\";
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				if (dialog.SelectedPath.Contains("새 폴더"))
				{
					System.Windows.MessageBox.Show("폴더 이름을 바꾸고 엔터를 눌러 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Asterisk);
				}
				ServiceProvider.Settings.ElapsedDownloadFolder = dialog.SelectedPath + "\\";
			}
		}
		catch (Exception exception)
		{
			Log.Error("폴더를 만들 수 없습니다.", exception);
			System.Windows.MessageBox.Show("폴더를 만들 수 없습니다.", "에러", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void Btn_SyncCameraDateTime_Click(object sender, RoutedEventArgs e)
	{
		Log.Debug("모든 카메라 시간 동기화 Click");
		foreach (ICameraDevice cameraDevice in ServiceProvider.DeviceManager.ConnectedDevices)
		{
			try
			{
				cameraDevice.DateTime = DateTime.Now;
			}
			catch (Exception exception)
			{
				Log.Error("Setting : 카메라 시간을 동기화 할 수 없습니다.", exception);
			}
		}
		System.Windows.MessageBox.Show("⚠\ufe0f 모든 카메라 날짜/시간 동기화했습니다.\n\n⚠\ufe0f 모든 카메라 전원을 껏다 다시 켜 주세요.", "Info", MessageBoxButton.OK, MessageBoxImage.Asterisk);
	}

	private void Expander_Click(object sender, RoutedEventArgs e)
	{
		if (!(sender is Expander { Parent: System.Windows.Controls.Panel parentPanel } currentExpander))
		{
			return;
		}
		foreach (object child in parentPanel.Children)
		{
			if (child is Expander otherExpander && otherExpander != currentExpander)
			{
				otherExpander.IsExpanded = false;
			}
		}
	}

	private void AndroidCameraDevice_Clicked(object sender, RoutedEventArgs e)
	{
		if (Owner is MainWindow mainWindow)
		{
			System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
			if (checkBox.IsChecked == true)
			{
				mainWindow.WatcherFtpServer.StopFTPServer();
				mainWindow.SignalRWebServer.StartSignalRWebServer();
				mainWindow.AndroidAppServer.StartAndroidServer();
			}
			else
			{
				mainWindow.AndroidAppServer.StopAndroidServer();
				mainWindow.SignalRWebServer.StopSignalRWebServer();
			}
		}
	}

	private void FTPserver_Clicked(object sender, RoutedEventArgs e)
	{
		if (Owner is MainWindow mainWindow)
		{
			System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
			if (checkBox.IsChecked == true)
			{
				mainWindow.AndroidAppServer.StopAndroidServer();
				mainWindow.SignalRWebServer.StopSignalRWebServer();
				mainWindow.WatcherFtpServer.StartFTPServer();
			}
			else
			{
				mainWindow.WatcherFtpServer.StopFTPServer();
			}
		}
	}

	private void Btn_Calibration_Click(object sender, RoutedEventArgs e)
	{
		if (Owner is MainWindow mainWindow)
		{
			mainWindow.but_Calibration_Click(sender, e);
		}
	}
}
