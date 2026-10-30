using System.Windows;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;

namespace CameraControl.Plugins.ToolPlugins;

internal class GenThumbPlugin : IToolPlugin
{
	public string Id => "{C66CBC77-71A3-491A-B844-56AF460DBEF5}";

	public string Title { get; set; }

	public bool Execute()
	{
		GenThumbWindow window = new GenThumbWindow();
		((FrameworkElement)window).DataContext = new GenThumbViewModel((Window)(object)window);
		((Window)window).ShowDialog();
		return true;
	}

	public void Init()
	{
	}

	public GenThumbPlugin()
	{
		Title = TranslationStrings.LabelGenerateThumbs;
	}
}
