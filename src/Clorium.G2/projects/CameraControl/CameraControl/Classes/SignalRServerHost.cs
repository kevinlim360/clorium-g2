using System;
using System.Windows;
using CameraControl.Devices;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Hosting;

namespace CameraControl.Classes;

[HubName("SignalRServerHub")]
public class SignalRServerHost : IDisposable
{
	private IDisposable _signalRServer;

	private bool _disposed = false;

	public void Dispose()
	{
		if (!_disposed)
		{
			StopSignalRWebServer();
			_disposed = true;
		}
	}

	public void StartSignalRWebServer()
	{
		if (_signalRServer != null)
		{
			StopSignalRWebServer();
		}
		try
		{
			string url = "http://*:8080";
			_signalRServer = WebApp.Start<Startup>(url);
			Log.Debug("모바일 웹 서버 시작");
		}
		catch (Exception ex)
		{
			Log.Debug("모바일 웹 서버 시작 실패: " + ex.Message);
			MessageBox.Show("모바일 웹 서버 시작할 수 없습니다.\n 모바일 웹서버 시작을 원하시면 관리자 권한으로 다시 실행해 주세요.");
			_signalRServer = null;
		}
	}

	public void StopSignalRWebServer()
	{
		try
		{
			if (_signalRServer != null)
			{
				_signalRServer.Dispose();
				_signalRServer = null;
			}
			Log.Debug("모바일 웹 서버 종료");
		}
		catch (Exception ex)
		{
			Console.WriteLine("서버 정지 중 오류: " + ex.Message);
		}
	}
}
