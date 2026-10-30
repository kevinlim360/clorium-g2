using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using CameraControl.Core;
using CameraControl.Core.Classes;
using GalaSoft.MvvmLight;

namespace CameraControl.Plugins.AutoExportPlugins;

public class BasePluginViewModel : ViewModelBase
{
	protected AutoExportPluginConfig _config = new AutoExportPluginConfig();

	public const string EmptyTransformFilter = "NoTransform";

	public string TransformPlugin
	{
		get
		{
			string pl = _config.ConfigData["TransformPlugin"];
			return string.IsNullOrEmpty(pl) ? "NoTransform" : pl;
		}
		set
		{
			_config.ConfigData["TransformPlugin"] = value;
			RaisePropertyChanged(() => ConfigControl);
		}
	}

	public string Name
	{
		get
		{
			return _config.Name;
		}
		set
		{
			_config.Name = value;
		}
	}

	public UserControl ConfigControl => ServiceProvider.PluginManager.GetImageTransformPlugin(TransformPlugin)?.GetConfig(_config.ConfigData);

	public int ToInt(string name)
	{
		int i = 0;
		int.TryParse(_config.ConfigData[name], out i);
		return i;
	}

	public string TransformTemplate(FileItem item, string text)
	{
		Regex regPattern = new Regex("\\[(.*?)\\]", RegexOptions.Singleline);
		MatchCollection matchX = regPattern.Matches(text);
		return matchX.Cast<Match>().Aggregate(text, (string current1, Match match) => item.FileNameTemplates.Where((ValuePair template) => string.Compare(template.Name, match.Value, StringComparison.InvariantCultureIgnoreCase) == 0).Aggregate(current1, (string text2, ValuePair template) => text2.Replace(match.Value, template.Value)));
	}
}
