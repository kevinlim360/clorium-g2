using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using ImageMagick;

namespace CameraControl.Plugins.ExportPlugins;

public class ExportToFolder : IExportPlugin
{
	private ProgressWindow dlg = new ProgressWindow();

	private string destfolder = "";

	private string _title;

	public string Title
	{
		get
		{
			return "Export To Folder";
		}
		set
		{
			_title = value;
		}
	}

	public bool Execute()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected Obj, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Invalid comparison between Unknown and I4
		if (((UIElement)dlg).IsVisible)
		{
			return true;
		}
		MessageBox.Show("All existing files will be overwritten !");
		FolderBrowserDialog dialog = new FolderBrowserDialog();
		if ((int)((System.Windows.Forms.CommonDialog)dialog).ShowDialog() == 1)
		{
			destfolder = dialog.SelectedPath;
			((Window)dlg).Show();
			Thread thread = new Thread(CopyFiles);
			thread.Start(ServiceProvider.Settings.DefaultSession.Files);
		}
		return true;
	}

	private void CopyFiles(object o)
	{
		AsyncObservableCollection<FileItem> items = o as AsyncObservableCollection<FileItem>;
		items = new AsyncObservableCollection<FileItem>(items.Where((FileItem file) => file.IsChecked));
		dlg.MaxValue = items.Count;
		int i = 0;
		foreach (FileItem fileItem in items)
		{
			dlg.Progress = i;
			dlg.ImageSource = (ImageSource)(object)fileItem.Thumbnail;
			dlg.Label = Path.GetFileName(fileItem.FileName);
			if (File.Exists(fileItem.FileName))
			{
				try
				{
					string dest = Path.Combine(destfolder, Path.GetFileName(fileItem.FileName));
					if (fileItem.RotationAngle == 0 || fileItem.IsRaw || fileItem.IsMovie)
					{
						File.Copy(fileItem.FileName, dest, overwrite: true);
					}
					else
					{
						using MagickImage image = new MagickImage(fileItem.FileName);
						image.BackgroundColor = new MagickColor(Color.Black.R, Color.Black.G, Color.Black.B);
						image.Rotate(fileItem.RotationAngle);
						image.Format = MagickFormat.Jpeg;
						image.Write(dest);
					}
				}
				catch (Exception exception)
				{
					Log.Error("ErrorCopy file", exception);
				}
			}
			i++;
		}
		dlg.Hide();
	}
}
