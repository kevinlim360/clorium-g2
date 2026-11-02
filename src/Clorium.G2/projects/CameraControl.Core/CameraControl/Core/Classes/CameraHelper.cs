using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.Core.Classes;

public static class CameraHelper
{
	public static void Capture()
	{
		try
		{
			CaptureWithError();
		}
		catch (Exception ex)
		{
			ServiceProvider.Settings.isBtnPressed = false;
			ServiceProvider.Settings.ThumbailLoaded = true;
			StaticHelper.Instance.SystemMessage = ex.Message;
			Application.Current.Dispatcher.Invoke(() =>
			{
				MessageBox.Show("포커스가 맞지 않습니다.\r\n박스내 포커스를 맞추고 다시 시도해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				Log.Debug("포커스가 맞지 않습니다.");
			});
		}
	}

	public static void CaptureWithError(ICameraDevice device = null)
	{
		CaptureInternal(device ?? ServiceProvider.DeviceManager.SelectedCameraDevice);
	}

	public static void Capture(object o)
	{
		if (o != null && o is ICameraDevice camera)
		{
			if (G2FocusCamera.IsOwned(camera)) throw new InvalidOperationException("수퍼포커스 촬영 또는 다운로드가 진행 중입니다.");
			ServiceProvider.DeviceManager.LastCapturedImage[camera] = "-";
			CameraProperty property = ServiceProvider.Settings.CameraProperties.Get(camera);
			for (int i = 0; i < property.Delay; i++)
			{
				StaticHelper.Instance.SystemMessage = "Countig down " + (property.Delay - i);
				Thread.Sleep(1000);
			}
			if (property.UseExternalShutter && property.SelectedConfig != null)
			{
				ServiceProvider.ExternalDeviceManager.AssertFocus(property.SelectedConfig);
				Thread.Sleep(ServiceProvider.Settings.ExternalDeviceWaitForFocus);
				ServiceProvider.ExternalDeviceManager.OpenShutter(property.SelectedConfig);
				Thread.Sleep(ServiceProvider.Settings.ExternalDeviceWaitForCapture);
				ServiceProvider.ExternalDeviceManager.CloseShutter(property.SelectedConfig);
			}
			else
			{
				camera.CapturePhoto();
				ServiceProvider.Analytics.CameraCapture(camera);
			}
		}
	}

	public static void CaptureNoAf()
	{
		try
		{
			CaptureNoAf(ServiceProvider.DeviceManager.SelectedCameraDevice);
		}
		catch (Exception ex)
		{
			StaticHelper.Instance.SystemMessage = ex.Message;
		}
	}

	public static void CaptureNoAf(object o)
	{
		if (o != null && o is ICameraDevice camera)
		{
			if (G2FocusCamera.IsOwned(camera)) throw new InvalidOperationException("수퍼포커스 촬영 또는 다운로드가 진행 중입니다.");
			CameraProperty property = ServiceProvider.Settings.CameraProperties.Get(camera);
			if (property.UseExternalShutter && property.SelectedConfig != null)
			{
				ServiceProvider.ExternalDeviceManager.OpenShutter(property.SelectedConfig);
				Thread.Sleep(200);
				ServiceProvider.ExternalDeviceManager.CloseShutter(property.SelectedConfig);
			}
			else
			{
				camera.CapturePhotoNoAf();
			}
		}
	}

	public static void WaitForCamera(this ICameraDevice device, int timeout)
	{
		DateTime startTime = DateTime.Now;
		while (device.IsBusy && !((DateTime.Now - startTime).TotalMilliseconds > (double)timeout))
		{
			Thread.Sleep(50);
		}
	}

	public static string StartRecordVideo(ICameraDevice device)
	{
		if (!CameraControl.Devices.LegacyCapturePolicy.MovieRecordingEnabled) return CameraControl.Devices.LegacyCapturePolicy.DisabledMessage;
		ServiceProvider.DeviceManager.LastCapturedImage[device] = "-";
		try
		{
			string resp = device.GetProhibitionCondition(OperationEnum.RecordMovie);
			if (!string.IsNullOrEmpty(resp))
			{
				return resp;
			}
			device.StartRecordMovie();
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
		return "";
	}

	public static string StopRecordVideo(ICameraDevice device)
	{
		ServiceProvider.DeviceManager.LastCapturedImage[device] = "-";
		try
		{
			device.StopRecordMovie();
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
		return "";
	}

	public static CameraProperty LoadProperties(this ICameraDevice cameraDevice)
	{
		CameraProperty property = ServiceProvider.Settings.CameraProperties.Get(cameraDevice);
		cameraDevice.DisplayName = property.DeviceName;
		cameraDevice.AttachedPhotoSession = ServiceProvider.Settings.GetSession(property.PhotoSessionName);
		return property;
	}

	public static PropertyValue<long> GetProperty(this ICameraDevice cameraDevice, uint cod)
	{
		foreach (PropertyValue<long> property in cameraDevice.AdvancedProperties)
		{
			if (property.Code == cod)
			{
				return property;
			}
		}
		return cameraDevice.Properties.FirstOrDefault((PropertyValue<long> propertyValue) => propertyValue.Code == cod);
	}

	public static string GetPropertyValue(this ICameraDevice cameraDevice, uint cod)
	{
		return cameraDevice.GetProperty(cod)?.Value;
	}

	public static int GetExposureDelay(this ICameraDevice cameraDevice)
	{
		PropertyValue<long> prop = cameraDevice.GetProperty(53354u);
		if (prop != null && !string.IsNullOrEmpty(prop.Value) && prop.Value != "OFF")
		{
			if (prop.Value == "ON")
			{
				return 2;
			}
			int.TryParse(prop.Value.Substring(0, 1), out var expDelay);
			return expDelay;
		}
		return 0;
	}

	public static List<PropertyValue<long>> GetAllEllements(this ICameraDevice cameraDevice)
	{
		List<PropertyValue<long>> res = new List<PropertyValue<long>>
		{
			cameraDevice.Mode, cameraDevice.CompressionSetting, cameraDevice.ExposureCompensation, cameraDevice.ExposureMeteringMode, cameraDevice.FNumber, cameraDevice.FocusMode, cameraDevice.IsoNumber, cameraDevice.LiveViewImageZoomRatio, cameraDevice.ShutterSpeed, cameraDevice.WhiteBalance,
			cameraDevice.ColorTemperature
		};
		res.AddRange(cameraDevice.Properties);
		res.AddRange(cameraDevice.AdvancedProperties);
		return res;
	}

	public static void StartLive(object o)
	{
		if (o != null && o is ICameraDevice camera)
		{
			camera.StartLiveView();
		}
	}

	public static void StartLiveAll()
	{
		foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.IsConnected && cameraDevice.IsChecked))
		{
			if (!(connectedDevice.DeviceName == "Z-App camera") && !(connectedDevice.DeviceName == "Z-FTP camera") && !(connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName))
			{
				ICameraDevice device = connectedDevice;
				ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_StartView", device);
				Thread.Sleep(100);
			}
		}
	}

	public static void StopLiveAll()
	{
		foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.IsConnected && cameraDevice.IsChecked))
		{
			if (!(connectedDevice.DeviceName == "Z-App camera") && !(connectedDevice.DeviceName == "Z-FTP camera") && !(connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName))
			{
				ICameraDevice device = connectedDevice;
				ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_StopView", device);
				Thread.Sleep(100);
			}
		}
	}

	public static void CaptureNoAfAll(int msecdelay)
	{
		foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.IsConnected && cameraDevice.IsChecked))
		{
			if (connectedDevice.DeviceName == "Z-App camera" || connectedDevice.DeviceName == "Z-FTP camera" || connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName)
			{
				continue;
			}
			ICameraDevice device = connectedDevice;
			Thread thread = new Thread(() =>
			{
				try
				{
					CaptureNoAf(device);
				}
				catch (Exception ex)
				{
					Log.Error(ex);
					StaticHelper.Instance.SystemMessage = ex.Message;
				}
			});
			thread.Start();
			Thread.Sleep(msecdelay);
		}
	}

	public static bool WaitForCameraAll(int timeout)
	{
		foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.IsConnected && cameraDevice.IsChecked))
		{
			if (connectedDevice.DeviceName == "Z-App camera" || connectedDevice.DeviceName == "Z-FTP camera" || connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName)
			{
				continue;
			}
			ICameraDevice device = connectedDevice;
			DateTime startTime = DateTime.Now;
			while (device.IsBusy)
			{
				if ((DateTime.Now - startTime).TotalMilliseconds > (double)timeout)
				{
					return true;
				}
				Thread.Sleep(50);
			}
		}
		return false;
	}

	public static async Task<bool> CaptureAsync()
	{
		try
		{
			return await Task.Run(() => CaptureAsyncWithError());
		}
		catch (Exception ex)
		{
			Exception e = ex;
			// A failed exposure has no PhotoCaptured finally callback to clear its purpose.
			ServiceProvider.Settings.FrontBackMode = 0;
			ServiceProvider.Settings.GetMeasurePicture = false;
			ServiceProvider.Settings.Background = false;
			ServiceProvider.Settings.isBtnPressed = false;
			ServiceProvider.Settings.ThumbailLoaded = true;
			StaticHelper.Instance.SystemMessage = e.Message;
			Application.Current.Dispatcher.Invoke(() =>
			{
				MessageBox.Show("포커스가 맞지 않습니다.\r\n박스내 포커스를 맞추고 다시 시도해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				Log.Debug("포커스가 맞지 않습니다.");
			});
			return false;
		}
	}

	public static bool CaptureAsyncWithError(ICameraDevice device = null)
	{
		return CaptureInternal(device ?? ServiceProvider.DeviceManager.SelectedCameraDevice);
	}

	public static void CaptureAll(int msecdelay)
	{
		foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.IsConnected && cameraDevice.IsChecked))
		{
			if (connectedDevice.DeviceName == "Z-App camera" || connectedDevice.DeviceName == "Z-FTP camera" || connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName)
			{
				continue;
			}
			ICameraDevice device = connectedDevice;
			Thread thread = new Thread(() =>
			{
				try
				{
					Capture(device);
				}
				catch (Exception ex)
				{
					Log.Error(ex);
					StaticHelper.Instance.SystemMessage = ex.Message;
				}
			});
			thread.Start();
			Thread.Sleep(msecdelay);
		}
	}

	private static bool CaptureInternal(object o)
	{
		if (!(o is ICameraDevice camera))
		{
			return false;
		}
		if (G2FocusCamera.IsOwned(camera)) throw new InvalidOperationException("수퍼포커스 촬영 또는 다운로드가 진행 중입니다.");
		ServiceProvider.DeviceManager.LastCapturedImage[camera] = "-";
		CameraProperty property = ServiceProvider.Settings.CameraProperties.Get(camera);
		for (int i = 0; i < property.Delay; i++)
		{
			StaticHelper.Instance.SystemMessage = "Counting down " + (property.Delay - i);
			Thread.Sleep(1000);
		}
		if (property.UseExternalShutter && property.SelectedConfig != null)
		{
			ServiceProvider.ExternalDeviceManager.AssertFocus(property.SelectedConfig);
			Thread.Sleep(ServiceProvider.Settings.ExternalDeviceWaitForFocus);
			ServiceProvider.ExternalDeviceManager.OpenShutter(property.SelectedConfig);
			Thread.Sleep(ServiceProvider.Settings.ExternalDeviceWaitForCapture);
			ServiceProvider.ExternalDeviceManager.CloseShutter(property.SelectedConfig);
			return true;
		}
		camera.CapturePhoto();
		ServiceProvider.Analytics.CameraCapture(camera);
		return true;
	}
}
