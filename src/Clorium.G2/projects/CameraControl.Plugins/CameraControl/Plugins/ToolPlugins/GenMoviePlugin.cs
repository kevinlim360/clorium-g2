using System.Windows;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;

namespace CameraControl.Plugins.ToolPlugins;

public class GenMoviePlugin : IToolPlugin
{
	public string Id => "{FEF994F3-6C4C-412F-9374-C92340EA80D2}";

	public string Title { get; set; }

	public bool Execute()
	{
		GenMovieWindow window = new GenMovieWindow();
		((FrameworkElement)window).DataContext = new GenMovieViewModel((Window)(object)window);
		((Window)window).ShowDialog();
		return true;
	}

	public void Init()
	{
	}

	public GenMoviePlugin()
	{
		Title = TranslationStrings.LabelCreateMovie;
	}
}
