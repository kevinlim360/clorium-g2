using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;

namespace CameraControl.Plugins.AutoExportPlugins;

public class TransformPlugin : IAutoExportPlugin
{
	public string Name => "Transform";

	public bool Execute(FileItem item, AutoExportPluginConfig configData)
	{
		configData.IsRedy = false;
		configData.IsError = false;
		string filename = item.FileName;
		TransformPluginViewModel conf = new TransformPluginViewModel(configData);
		string outfile = Path.GetTempFileName();
		outfile = PhotoUtils.ReplaceExtension(outfile, Path.GetExtension(filename));
		outfile = AutoExportPluginHelper.ExecuteTransformPlugins(item, configData, outfile);
		if (conf.CreateNew)
		{
			string newFile = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "_transformed.jpg");
			newFile = PhotoUtils.GetNextFileName(newFile);
			File.Copy(outfile, newFile, overwrite: true);
			if (ServiceProvider.Settings.DefaultSession.GetFile(newFile) == null)
			{
				((DispatcherObject)Application.Current).Dispatcher.Invoke((Action)(() =>
				{
					FileItem item2 = new FileItem(newFile)
					{
						Transformed = true
					};
					int num = ServiceProvider.Settings.DefaultSession.Files.IndexOf(item);
					if (ServiceProvider.Settings.DefaultSession.Files.Count - 1 == num)
					{
						ServiceProvider.Settings.DefaultSession.Files.Add(item2);
					}
					else
					{
						ServiceProvider.Settings.DefaultSession.Files.Insert(num + 1, item2);
					}
				}));
			}
		}
		else
		{
			PhotoUtils.WaitForFile(filename);
			File.Copy(outfile, filename, overwrite: true);
			item.IsLoaded = false;
			item.RemoveThumbs();
			item.Transformed = true;
		}
		if (outfile != item.FileName)
		{
			PhotoUtils.WaitForFile(outfile);
			File.Delete(outfile);
		}
		configData.IsRedy = true;
		return true;
	}

	public UserControl GetConfig(AutoExportPluginConfig configData)
	{
		TransformPluginConfig transformPluginConfig = new TransformPluginConfig();
		((FrameworkElement)transformPluginConfig).DataContext = new TransformPluginViewModel(configData);
		return (UserControl)(object)transformPluginConfig;
	}
}
