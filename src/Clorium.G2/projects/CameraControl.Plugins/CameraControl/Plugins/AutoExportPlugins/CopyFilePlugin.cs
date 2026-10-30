using System.IO;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;

namespace CameraControl.Plugins.AutoExportPlugins;

public class CopyFilePlugin : IAutoExportPlugin
{
	public string Name => "Copy File";

	public bool Execute(FileItem item, AutoExportPluginConfig configData)
	{
		configData.IsRedy = false;
		configData.IsError = false;
		string filename = item.FileName;
		CopyFilePluginViewModel conf = new CopyFilePluginViewModel(configData);
		if (string.IsNullOrEmpty(conf.Path))
		{
			configData.IsRedy = true;
			configData.IsError = true;
			configData.Error = "No export path was set";
			return false;
		}
		if (!Directory.Exists(conf.Path))
		{
			Directory.CreateDirectory(conf.Path);
		}
		string name = item.Name;
		if (!string.IsNullOrEmpty(conf.FileName))
		{
			name = conf.FileName;
			if (name.Contains("%1"))
			{
				name = name.Replace("%1", Path.GetFileNameWithoutExtension(item.Name));
			}
			if (!name.Contains("."))
			{
				name += Path.GetExtension(item.Name);
			}
		}
		string outfile = Path.Combine(conf.Path, name);
		AutoExportPluginHelper.ExecuteTransformPlugins(item, configData, outfile);
		configData.IsRedy = true;
		return true;
	}

	public UserControl GetConfig(AutoExportPluginConfig configData)
	{
		CopyFilePluginConfig copyFilePluginConfig = new CopyFilePluginConfig();
		((FrameworkElement)copyFilePluginConfig).DataContext = new CopyFilePluginViewModel(configData);
		return (UserControl)(object)copyFilePluginConfig;
	}
}
