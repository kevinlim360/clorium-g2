using System;
using System.IO;
using System.Net;
using System.Net.FtpClient;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.Plugins.AutoExportPlugins;

public class FtpPlugin : IAutoExportPlugin
{
	public string Name => "Ftp";

	public bool Execute(FileItem item, AutoExportPluginConfig configData)
	{
		Thread thread = new Thread(() =>
		{
			Send(item, configData);
		});
		thread.Start();
		return true;
	}

	private void Send(FileItem item, AutoExportPluginConfig configData)
	{
		try
		{
			configData.IsRedy = false;
			configData.IsError = false;
			FtpPluginViewModel conf = new FtpPluginViewModel(configData);
			string outfile = PhotoUtils.ReplaceExtension(Path.GetTempFileName(), Path.GetExtension(item.Name));
			outfile = AutoExportPluginHelper.ExecuteTransformPlugins(item, configData, outfile);
			using (FtpClient conn = new FtpClient())
			{
				conn.Host = conf.Server;
				if (conf.Port > 0)
				{
					conn.Port = conf.Port;
				}
				conn.Credentials = new NetworkCredential(conf.User, conf.Pass);
				if (!string.IsNullOrWhiteSpace(conf.ServerPath))
				{
					conn.SetWorkingDirectory(conf.ServerPath);
				}
				using Stream ostream = conn.OpenWrite(item.Name);
				try
				{
					byte[] data = File.ReadAllBytes(outfile);
					ostream.Write(data, 0, data.Length);
				}
				finally
				{
					ostream.Close();
				}
			}
			if (outfile != item.FileName)
			{
				PhotoUtils.WaitForFile(outfile);
				File.Delete(outfile);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error senf ftp file", ex);
			configData.IsError = true;
			configData.Error = ex.Message;
		}
		configData.IsRedy = true;
	}

	public UserControl GetConfig(AutoExportPluginConfig configData)
	{
		FtpPluginConfig ftpPluginConfig = new FtpPluginConfig();
		((FrameworkElement)ftpPluginConfig).DataContext = new FtpPluginViewModel(configData);
		return (UserControl)(object)ftpPluginConfig;
	}
}
