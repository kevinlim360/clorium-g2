using CameraControl.Core.Classes;

namespace CameraControl.Plugins.AutoExportPlugins;

public class TransformPluginViewModel : BasePluginViewModel
{
	public bool CreateNew
	{
		get
		{
			return _config.ConfigData["CreateNew"] == "True";
		}
		set
		{
			_config.ConfigData["CreateNew"] = value.ToString();
			RaisePropertyChanged(() => OverWrite);
		}
	}

	public bool OverWrite => !CreateNew;

	public TransformPluginViewModel(AutoExportPluginConfig config)
	{
		_config = config;
	}

	public TransformPluginViewModel()
	{
	}
}
