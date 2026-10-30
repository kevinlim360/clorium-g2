using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.Plugins.AutoExportPlugins;

public class DropboxPlugin : IAutoExportPlugin
{
	public string Name => "Dropbox";

	public bool Execute(FileItem item, AutoExportPluginConfig configData)
	{
		Thread thread = new Thread(() =>
		{
			Send(item, configData);
		});
		thread.Start();
		return true;
	}

	public UserControl GetConfig(AutoExportPluginConfig configData)
	{
		DropboxConfig dropboxConfig = new DropboxConfig();
		((FrameworkElement)dropboxConfig).DataContext = new DropboxViewModel(configData);
		return (UserControl)(object)dropboxConfig;
	}

	public void Send(FileItem item, AutoExportPluginConfig configData)
	{
		try
		{
			string filename = item.FileName;
			configData.IsRedy = false;
			configData.IsError = false;
			DropboxViewModel conf = new DropboxViewModel(configData);
			string outfile = PhotoUtils.ReplaceExtension(Path.GetTempFileName(), Path.GetExtension(item.Name));
			outfile = AutoExportPluginHelper.ExecuteTransformPlugins(item, configData, outfile);
			conf.Upload(outfile, ServiceProvider.Settings.DefaultSession.Name, item.Name);
			if (outfile != item.FileName)
			{
				PhotoUtils.WaitForFile(outfile);
				File.Delete(outfile);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error send dropbox file", ex);
			configData.IsError = true;
			configData.Error = ex.Message;
		}
		configData.IsRedy = true;
	}
}
