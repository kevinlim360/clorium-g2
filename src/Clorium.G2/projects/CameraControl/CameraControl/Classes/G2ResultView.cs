using System;
using System.IO;
using System.Windows;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;

namespace CameraControl.Classes;

// G2: a finished 수퍼포커스 / 배경제거 result goes into the open session's photo strip and is shown on the main stage,
// the way the color adjust window adds its saved copy. Auto export (FileTransfered) is not run for it.
public static class G2ResultView
{
	public static void Show(string file)
	{
		if (string.IsNullOrEmpty(file))
		{
			return;
		}
		Action show = () =>
		{
			try
			{
				PhotoSession session = ServiceProvider.Settings?.DefaultSession;
				if (session == null || !File.Exists(file))
				{
					return;
				}
				FileItem item = session.GetFile(file);
				if (item == null)
				{
					item = session.GetNewFileItem(file);
					item.RemoveThumbs();
					session.Add(item);
				}
				ServiceProvider.WindowsManager.ExecuteCommand("Select_Image", item);
				Log.Debug("G2 결과 표시 : " + file);
			}
			catch (Exception ex)
			{
				Log.Error("G2 result view", ex);
			}
		};
		var dispatcher = Application.Current?.Dispatcher;
		if (dispatcher == null || dispatcher.CheckAccess()) show();
		else dispatcher.BeginInvoke(show);
	}
}
