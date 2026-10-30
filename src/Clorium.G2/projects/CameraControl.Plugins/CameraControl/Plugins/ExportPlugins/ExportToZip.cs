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
using CameraControl.Devices.Classes;
using Ionic.Zip;

namespace CameraControl.Plugins.ExportPlugins;

public class ExportToZip : IExportPlugin
{
	private ProgressWindow dlg = new ProgressWindow();

	private string destfile = "";

	private string _title;

	public string Title
	{
		get
		{
			return "Export to zip";
		}
		set
		{
			_title = value;
		}
	}

	public bool Execute()
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected Obj, but got Unknown
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (((UIElement)dlg).IsVisible)
		{
			return true;
		}
		SaveFileDialog saveFileDialog1 = new SaveFileDialog();
		((FileDialog)saveFileDialog1).Filter = "Zip file|*.zip";
		((FileDialog)saveFileDialog1).Title = "Save zip file";
		((CommonDialog)saveFileDialog1).ShowDialog();
		if (((FileDialog)saveFileDialog1).FileName != "")
		{
			destfile = ((FileDialog)saveFileDialog1).FileName;
			((Window)dlg).Show();
			Thread thread = new Thread(ZipFiles);
			thread.Start(ServiceProvider.Settings.DefaultSession.Files);
		}
		return true;
	}

	private void ZipFiles(object o)
	{
		AsyncObservableCollection<FileItem> items = o as AsyncObservableCollection<FileItem>;
		items = new AsyncObservableCollection<FileItem>(items.Where((FileItem file) => file.IsChecked));
		dlg.MaxValue = items.Count;
		int i = 0;
		using (ZipFile zip = new ZipFile(destfile))
		{
			foreach (FileItem fileItem in items)
			{
				dlg.Progress = i;
				dlg.ImageSource = (ImageSource)(object)fileItem.Thumbnail;
				dlg.Label = Path.GetFileName(fileItem.FileName);
				zip.AddFile(fileItem.FileName, "");
				i++;
				zip.Save(destfile);
			}
		}
		dlg.Hide();
	}
}
