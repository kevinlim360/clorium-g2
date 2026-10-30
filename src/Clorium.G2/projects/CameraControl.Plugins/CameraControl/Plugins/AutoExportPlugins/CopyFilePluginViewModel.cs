using System.Windows.Forms;
using CameraControl.Core.Classes;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.Plugins.AutoExportPlugins;

public class CopyFilePluginViewModel : BasePluginViewModel
{
	public RelayCommand BrowseCommand { get; set; }

	public string Path
	{
		get
		{
			return _config.ConfigData["Path"];
		}
		set
		{
			_config.ConfigData["Path"] = value;
			RaisePropertyChanged(() => Path);
		}
	}

	public string FileName
	{
		get
		{
			return _config.ConfigData["FileName"];
		}
		set
		{
			_config.ConfigData["FileName"] = value;
			RaisePropertyChanged(() => Path);
		}
	}

	public CopyFilePluginViewModel(AutoExportPluginConfig config)
	{
		_config = config;
		BrowseCommand = new RelayCommand(Browse);
	}

	private void Browse()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected Obj, but got Unknown
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		FolderBrowserDialog dialog = new FolderBrowserDialog();
		dialog.SelectedPath = Path;
		if ((int)((System.Windows.Forms.CommonDialog)dialog).ShowDialog() == 1)
		{
			Path = dialog.SelectedPath;
		}
	}

	public CopyFilePluginViewModel()
	{
	}
}
