using System;
using System.IO;
using System.Windows.Forms;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using Microsoft.Win32;

namespace CameraControl.Plugins.ExportPlugins;

public class ExportCsv : IExportPlugin
{
	public string Title
	{
		get
		{
			return "Export file list to csv";
		}
		set
		{
		}
	}

	public bool Execute()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected Obj, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		SaveFileDialog dialog = new SaveFileDialog();
		((FileDialog)dialog).FileName = "export.csv";
		((FileDialog)dialog).Filter = "*.csv|*.csv|All files|*.*";
		if (((CommonDialog)dialog).ShowDialog() == true)
		{
			try
			{
				using TextWriter writer = File.CreateText(((FileDialog)dialog).FileName);
				writer.WriteLine("File,Time,TimeDif");
				for (int i = 0; i < ServiceProvider.Settings.DefaultSession.Files.Count; i++)
				{
					FileItem file = ServiceProvider.Settings.DefaultSession.Files[i];
					writer.WriteLine("{0},{1},{2}", file.FileName, file.FileDate.ToString("O"), (i > 0) ? Math.Round((file.FileDate - ServiceProvider.Settings.DefaultSession.Files[i - 1].FileDate).TotalMilliseconds, 0) : 0.0);
				}
				PhotoUtils.Run(((FileDialog)dialog).FileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error to export " + ex.Message);
				Log.Error("Error to export ", ex);
			}
		}
		return true;
	}
}
