using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CameraControl.Core;
using CameraControl.Devices;
using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem.DotNet;
using Microsoft.Extensions.DependencyInjection;

namespace CameraControl.Classes;

public class FTPServerWatcher
{
	private object _locker = new object();

	private IFtpServerHost _ftpServerHost;

	private FileSystemWatcher _watcher;

	private CancellationTokenSource _cts;

	private readonly string FfpPathFolder = "D:\\Clorium FTPFolder";

	private bool _isRunning = false;

	public async void StartFTPServer()
	{
		if (!_isRunning)
		{
			if (!Directory.Exists(FfpPathFolder))
			{
				Directory.CreateDirectory(FfpPathFolder);
			}
			string[] files = Directory.GetFiles(FfpPathFolder);
			foreach (string file in files)
			{
				File.Delete(file);
			}
			ServiceCollection services = new ServiceCollection();
			services.Configure((FtpServerOptions opt) =>
			{
				opt.ServerAddress = "0.0.0.0";
				opt.Port = 5514;
			});
			services.Configure((DotNetFileSystemOptions opt) =>
			{
				opt.RootPath = FfpPathFolder;
			});
			services.AddFtpServer((IFtpServerBuilder builder) =>
			{
				builder.UseDotNetFileSystem().EnableAnonymousAuthentication();
			});
			Microsoft.Extensions.DependencyInjection.ServiceProvider serviceProvider = services.BuildServiceProvider();
			_ftpServerHost = serviceProvider.GetRequiredService<IFtpServerHost>();
			_cts = new CancellationTokenSource();
			await _ftpServerHost.StartAsync(_cts.Token);
			StartFileWatcher();
			_isRunning = true;
			CameraControl.Core.ServiceProvider.DeviceManager.AddFTPFakeCamera(FfpPathFolder);
			Log.Debug("FTP 서버 시작되었습니다. FTP 폴더 감시합니다.");
		}
	}

	public async void StopFTPServer()
	{
		if (!_isRunning)
		{
			return;
		}
		foreach (ICameraDevice connectedDevice in CameraControl.Core.ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.IsConnected))
		{
			if (connectedDevice.DeviceName == "Z-FTP camera")
			{
				CameraControl.Core.ServiceProvider.DeviceManager.DisconnectCamera(connectedDevice);
				break;
			}
		}
		try
		{
			StopFileWatcher();
			if (_ftpServerHost != null)
			{
				_cts?.Cancel();
				await _ftpServerHost.StopAsync(CancellationToken.None);
				_ftpServerHost = null;
			}
			_isRunning = false;
			Log.Debug("FTP 서버 중지합니다. FTP 폴더를 종료합니다.");
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Debug("서버 중지 중 에러 발생: " + ex2.Message);
		}
		finally
		{
			_cts?.Dispose();
			_cts = null;
		}
	}

	private void StartFileWatcher()
	{
		try
		{
			if (_watcher == null)
			{
				_watcher = new FileSystemWatcher
				{
					Path = FfpPathFolder,
					Filter = "*.*",
					NotifyFilter = (NotifyFilters.FileName | NotifyFilters.LastWrite),
					EnableRaisingEvents = true
				};
				_watcher.Created += OnFileDownloaded;
				_watcher.Renamed += OnFileDownloaded;
				_watcher.EnableRaisingEvents = true;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("모바일 카메라 감시자 설정 중 오류: " + ex.Message, "폴더 오류", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private void StopFileWatcher()
	{
		if (_watcher != null)
		{
			try
			{
				_watcher.EnableRaisingEvents = false;
				_watcher.Created -= OnFileDownloaded;
				_watcher.Renamed -= OnFileDownloaded;
				_watcher.Dispose();
				_watcher = null;
			}
			catch (Exception ex)
			{
				Log.Debug("감시자 중단 중 오류: " + ex.Message);
			}
		}
	}

	// Recovery inference: a completed FTP trigger file selects the matching camera for one capture, then restores the prior selection.
	private async void OnFileDownloaded(object source, FileSystemEventArgs e)
	{
		try
		{
			if (!(await WaitForFileAsync(e.FullPath)))
			{
				return;
			}
			await Task.Run(() =>
			{
				lock (_locker)
				{
					ICameraDevice selectedCameraDevice = CameraControl.Core.ServiceProvider.DeviceManager.SelectedCameraDevice;
					foreach (ICameraDevice current in CameraControl.Core.ServiceProvider.DeviceManager.ConnectedDevices)
					{
						if (current.DeviceName == "Z-FTP camera")
						{
							CameraControl.Core.ServiceProvider.DeviceManager.SelectedCameraDevice = current;
							ICameraDevice selectedCameraDevice2 = CameraControl.Core.ServiceProvider.DeviceManager.SelectedCameraDevice;
							selectedCameraDevice2.CapturePhoto();
							File.Delete(e.FullPath);
							break;
						}
					}
					CameraControl.Core.ServiceProvider.DeviceManager.SelectedCameraDevice = selectedCameraDevice;
				}
			});
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			MessageBox.Show("모바일 카메라 공용 폴더 이벤트 오류: " + ex2.Message, "이벤트 오류", MessageBoxButton.OK, MessageBoxImage.Hand);
		}
	}

	private async Task<bool> WaitForFileAsync(string filePath)
	{
		int attempts = 0;
		int maxAttempts = 50;
		while (attempts < maxAttempts)
		{
			try
			{
				using (new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					return true;
				}
			}
			catch (IOException)
			{
				attempts++;
				await Task.Delay(100);
			}
		}
		Log.Error(filePath + " 파일 준비 대기 시간 초과.");
		return false;
	}
}
