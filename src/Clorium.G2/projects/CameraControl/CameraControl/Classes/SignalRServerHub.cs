#define DEBUG
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNet.SignalR;

namespace CameraControl.Classes;

public class SignalRServerHub : Hub
{
	public void ExeCommand(string cmd, object data)
	{
		Application.Current.Dispatcher.Invoke(() =>
		{
			MainWindow mainWindow = Application.Current.MainWindow as MainWindow;
			if (mainWindow == null)
			{
				foreach (Window window in Application.Current.Windows)
				{
					if (window is MainWindow)
					{
						mainWindow = window as MainWindow;
						break;
					}
				}
			}
			if (mainWindow != null)
			{
				try
				{
					mainWindow.ExeWinCommand(cmd, data);
					Clients.Caller.onCommandReceived(cmd + " - > 실행 완료");
					return;
				}
				catch (Exception ex)
				{
					Debug.WriteLine("ExeWinCommand 오류: " + ex.Message);
					return;
				}
			}
			Debug.WriteLine("오류: MainWindow를 찾을 수 없습니다.");
		});
	}

	public override Task OnConnected()
	{
		Debug.WriteLine("클라이언트 접속: " + Context.ConnectionId);
		return base.OnConnected();
	}

	public override Task OnDisconnected(bool stopCalled)
	{
		Debug.WriteLine("클라이언트 접속 종료: " + Context.ConnectionId);
		return base.OnDisconnected(stopCalled);
	}

	public void Join()
	{
		Groups.Add(Context.ConnectionId, "Cameras");
		Debug.WriteLine("안드로이드 기기가 Join 했습니다.");
	}

	public static void SendTakePhoto()
	{
		IHubContext context = GlobalHost.ConnectionManager.GetHubContext<SignalRServerHub>();
		context.Clients.Group("Cameras").TakePhoto("CaptureStart");
		Debug.WriteLine("앱으로 보낸 명령 : TakePhoto CaptureStart");
	}

	public static void SendToggleVideo()
	{
		IHubContext context = GlobalHost.ConnectionManager.GetHubContext<SignalRServerHub>();
		context.Clients.Group("Cameras").ToggleVideo("ToggleStart");
		Debug.WriteLine("앱으로 보낸 명령 : ToggleVideo ToggleStart");
	}
}
