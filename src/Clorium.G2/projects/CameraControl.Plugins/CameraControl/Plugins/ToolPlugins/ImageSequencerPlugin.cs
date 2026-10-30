using System.Windows;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;

namespace CameraControl.Plugins.ToolPlugins;

public class ImageSequencerPlugin : IToolPlugin
{
	public string Id => "{6E020FD0-BEF0-4B22-8247-8271D07B4900}";

	public string Title { get; set; }

	public bool Execute()
	{
		GenThumbWindow windowthumb = new GenThumbWindow();
		((FrameworkElement)windowthumb).DataContext = new GenThumbViewModel((Window)(object)windowthumb);
		((Window)windowthumb).ShowDialog();
		ImageSequencerWindow window = new ImageSequencerWindow();
		((FrameworkElement)window).DataContext = new ImageSequencerViewModel((Window)(object)window);
		((Window)window).Show();
		return true;
	}

	public void Init()
	{
	}

	public ImageSequencerPlugin()
	{
		Title = TranslationStrings.LabelImageSequencer;
	}
}
