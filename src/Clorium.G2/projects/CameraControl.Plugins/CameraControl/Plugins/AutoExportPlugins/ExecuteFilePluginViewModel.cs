using CameraControl.Core.Classes;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;

namespace CameraControl.Plugins.AutoExportPlugins;

public class ExecuteFilePluginViewModel : BasePluginViewModel
{
	public RelayCommand BrowseCommand { get; set; }

	public string PathToExe
	{
		get
		{
			return _config.ConfigData["PathToExe"];
		}
		set
		{
			_config.ConfigData["PathToExe"] = value;
			RaisePropertyChanged(() => PathToExe);
		}
	}

	public string Params
	{
		get
		{
			return _config.ConfigData["Params"];
		}
		set
		{
			_config.ConfigData["Params"] = value;
			RaisePropertyChanged(() => Params);
		}
	}

	public ExecuteFilePluginViewModel(AutoExportPluginConfig config)
	{
		_config = config;
		BrowseCommand = new RelayCommand(Browse);
	}

	public ExecuteFilePluginViewModel()
	{
	}

	private void Browse()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected Obj, but got Unknown
		OpenFileDialog dialog = new OpenFileDialog();
		((FileDialog)dialog).FileName = PathToExe;
		if (((CommonDialog)dialog).ShowDialog() == true)
		{
			PathToExe = ((FileDialog)dialog).FileName;
		}
	}
}
