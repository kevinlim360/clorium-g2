using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;

namespace CameraControl.Plugins.AutoExportPlugins;

public class ExecuteFilePlugin : IAutoExportPlugin
{
	public string Name => "Execute File";

	public bool Execute(FileItem item, AutoExportPluginConfig configData)
	{
		configData.IsRedy = false;
		configData.IsError = false;
		ExecuteFilePluginViewModel conf = new ExecuteFilePluginViewModel(configData);
		if (string.IsNullOrEmpty(conf.PathToExe) || !File.Exists(conf.PathToExe))
		{
			configData.IsRedy = true;
			configData.IsError = true;
			configData.Error = "No executable path was set or executable not found";
			return false;
		}
		string outfile = item.FileName;
		if (configData.ConfigDataCollection.Count > 0 || !configData.RunAfterTransfer)
		{
			outfile = PhotoUtils.ReplaceExtension(Path.GetTempFileName(), Path.GetExtension(item.Name));
			outfile = AutoExportPluginHelper.ExecuteTransformPlugins(item, configData, outfile);
		}
		if (File.Exists(outfile))
		{
			PhotoUtils.Run(conf.PathToExe, string.IsNullOrEmpty(conf.Params) ? outfile : conf.Params.Replace("%1", outfile), ProcessWindowStyle.Normal);
		}
		else
		{
			configData.IsError = true;
			configData.Error = "Output file not found !";
		}
		configData.IsRedy = true;
		return true;
	}

	public UserControl GetConfig(AutoExportPluginConfig configData)
	{
		ExecuteFilePluginConfig executeFilePluginConfig = new ExecuteFilePluginConfig();
		((FrameworkElement)executeFilePluginConfig).DataContext = new ExecuteFilePluginViewModel(configData);
		return (UserControl)(object)executeFilePluginConfig;
	}
}
