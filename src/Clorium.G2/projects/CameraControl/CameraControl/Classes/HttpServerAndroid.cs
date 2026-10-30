#define DEBUG
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CameraControl.Core;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.Devices.Others;
using HttpMultipartParser;

namespace CameraControl.Classes;

public class HttpServerAndroid
{
	private HttpListener _listener;

	private bool _isRunning;

	public void StartAndroidServer()
	{
		StartHttpServer();
		ServiceProvider.DeviceManager.AddAppFakeCamera();
		ServiceProvider.Settings.UseAppFakeCamera = true;
		Log.Debug("안드로이드 앱 서버 시작");
	}

	public void StopAndroidServer()
	{
		StopHttpServer();
		foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices.Where((ICameraDevice cameraDevice) => cameraDevice.IsConnected))
		{
			if (connectedDevice.DeviceName == "Z-App camera")
			{
				ServiceProvider.Settings.UseAppFakeCamera = false;
				ServiceProvider.DeviceManager.DisconnectCamera(connectedDevice);
				break;
			}
		}
		Log.Debug("안드로이드 앱 서버 종료");
	}

	public void StartHttpServer()
	{
		if (_listener != null && _listener.IsListening)
		{
			return;
		}
		_listener = new HttpListener();
		_listener.Prefixes.Add("http://*:8080/upload/");
		_listener.Prefixes.Add("http://*:5514/preview/");
		try
		{
			_listener.Start();
			_isRunning = true;
			Task.Run(() => ListenLoop());
		}
		catch (Exception ex)
		{
			Log.Debug("Http 서버 가동 실패: " + ex.Message);
		}
	}

	public void StopHttpServer()
	{
		_isRunning = false;
		if (_listener != null && _listener.IsListening)
		{
			_listener?.Stop();
			_listener?.Close();
		}
	}

	private async Task ListenLoop()
	{
		while (_isRunning && _listener.IsListening)
		{
			try
			{
				HttpListenerContext context = await _listener.GetContextAsync();
				Task.Run(() => ProcessRequestAsync(context));
			}
			catch
			{
			}
		}
	}

	// Recovery inference: dispatch upload and live-view requests by listener port and path; response handling stays inside this boundary.
	private async Task ProcessRequestAsync(HttpListenerContext context)
	{
		try
		{
			int port = context.Request.LocalEndPoint.Port;
			string path = context.Request.Url.AbsolutePath.ToLower();
			string method = context.Request.HttpMethod.ToUpper();
			if (port == 8080 && path.Contains("/upload") && method == "POST")
			{
				MultipartFormDataParser parser = await MultipartFormDataParser.ParseAsync(context.Request.InputStream);
				if (parser.Files.Any())
				{
					FilePart file = parser.Files[0];
					using (MemoryStream ms = new MemoryStream())
					{
						await file.Data.CopyToAsync(ms);
						HandleVirtualCameraEvent(ms.ToArray(), file.FileName);
					}
					context.Response.StatusCode = 200;
				}
			}
			else
			{
				if (port != 5514 || !path.Contains("/preview"))
				{
					return;
				}
				AppFakeCameraDevice androidCam = ServiceProvider.DeviceManager.ConnectedDevices.FirstOrDefault((ICameraDevice d) => d.DeviceName == "Z-App camera") as AppFakeCameraDevice;
				if (method == "POST")
				{
					using MemoryStream ms2 = new MemoryStream();
					await context.Request.InputStream.CopyToAsync(ms2);
					if (androidCam != null)
					{
						androidCam.LiveViewImageData = ms2.ToArray();
					}
					context.Response.StatusCode = 200;
				}
				else
				{
					if (!(method == "GET"))
					{
						return;
					}
					try
					{
						context.Response.AddHeader("Access-Control-Allow-Origin", "*");
						context.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
						context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
						if (context.Request.HttpMethod == "OPTIONS")
						{
							context.Response.StatusCode = 200;
							context.Response.OutputStream.Close();
							return;
						}
						string responseString = (_isRunning ? GetCameraImageBase64() : "SERVER_STOPPED");
						byte[] buf = Encoding.UTF8.GetBytes(responseString);
						context.Response.ContentType = "text/plain";
						context.Response.ContentLength64 = buf.Length;
						context.Response.OutputStream.Write(buf, 0, buf.Length);
					}
					catch
					{
					}
					finally
					{
						context.Response.OutputStream.Close();
					}
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Log.Debug("Http 서버 요청 처리 중 에러: " + ex2.Message);
			context.Response.StatusCode = 500;
		}
		finally
		{
			try
			{
				context.Response.Close();
			}
			catch
			{
			}
		}
	}

	private void HandleVirtualCameraEvent(byte[] fileData, string fileName)
	{
		Task.Run(() =>
		{
			ICameraDevice selectedCameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
			ServiceProvider.DeviceManager.SelectedCameraDevice = ServiceProvider.DeviceManager.ConnectedDevices.FirstOrDefault((ICameraDevice d) => d.DeviceName == "Z-App camera");
			ICameraDevice selectedCameraDevice2 = ServiceProvider.DeviceManager.SelectedCameraDevice;
			if (selectedCameraDevice2 is AppFakeCameraDevice appFakeCameraDevice)
			{
				appFakeCameraDevice.LatestRawData = fileData;
				appFakeCameraDevice.LatestFileName = fileName;
				selectedCameraDevice2.CapturePhoto();
			}
			ServiceProvider.DeviceManager.SelectedCameraDevice = selectedCameraDevice;
		});
	}

	private string GetCameraImageBase64()
	{
		try
		{
			ICameraDevice camera = ServiceProvider.DeviceManager.SelectedCameraDevice;
			if (camera != null)
			{
				LiveViewData liveViewData = camera.GetLiveViewImage();
				if (liveViewData != null && liveViewData.ImageData != null)
				{
					return Convert.ToBase64String(liveViewData.ImageData);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("라이브뷰 캡처 오류: " + ex.Message);
		}
		return "NO_IMAGE";
	}
}
