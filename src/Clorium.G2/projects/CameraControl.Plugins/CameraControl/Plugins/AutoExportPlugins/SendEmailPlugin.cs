using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.Plugins.AutoExportPlugins;

public class SendEmailPlugin : IAutoExportPlugin
{
	public string Name => "Email";

	public bool Execute(FileItem item, AutoExportPluginConfig configData)
	{
		Thread thread = new Thread(() =>
		{
			Send(item, configData);
		});
		thread.Start();
		return true;
	}

	public void Send(FileItem item, AutoExportPluginConfig configData)
	{
		try
		{
			configData.IsRedy = false;
			configData.IsError = false;
			SendEmailPluginViewModel conf = new SendEmailPluginViewModel(configData);
			string outfile = PhotoUtils.ReplaceExtension(Path.GetTempFileName(), Path.GetExtension(item.Name));
			outfile = AutoExportPluginHelper.ExecuteTransformPlugins(item, configData, outfile);
			HelpProvider.SendEmail(string.IsNullOrEmpty(conf.Message) ? "." : conf.TransformTemplate(item, conf.Message), string.IsNullOrEmpty(conf.Subject) ? "Your photo" : conf.TransformTemplate(item, conf.Subject), conf.From, conf.To, outfile);
			if (outfile != item.FileName)
			{
				PhotoUtils.WaitForFile(outfile);
				File.Delete(outfile);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error send email file", ex);
			configData.IsError = true;
			configData.Error = ex.Message;
		}
		configData.IsRedy = true;
	}

	public UserControl GetConfig(AutoExportPluginConfig configData)
	{
		SendEmailPluginConfig sendEmailPluginConfig = new SendEmailPluginConfig();
		((FrameworkElement)sendEmailPluginConfig).DataContext = new SendEmailPluginViewModel(configData);
		return (UserControl)(object)sendEmailPluginConfig;
	}
}
