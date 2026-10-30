using System;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;

namespace CameraControl.Classes;

[MarkupExtensionReturnType(typeof(string))]
public class CommandShortcutExtensioncs : DynamicResourceExtension
{
	[ConstructorArgument("key")]
	public string Key { get; set; }

	public string DefaultValue { get; set; }

	public CommandShortcutExtensioncs(string key)
	{
		Key = key;
	}

	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		if (ServiceProvider.Settings == null)
		{
			return "";
		}
		foreach (WindowCommandItem act in ServiceProvider.Settings.Actions)
		{
			if (act.Name == Key)
			{
				return (act.Ctrl ? "CTRL + " : "") + (act.Alt ? "ALT + " : "") + ((act.Key == "None") ? "" : act.Key);
			}
		}
		return "";
	}
}
