using System.Windows;
using CameraControl.Core;
using CameraControl.Core.Interfaces;

namespace CameraControl.Plugins.ToolPlugins;

public class EnfusePlugin : IToolPlugin
{
	private EnfusePluginWindow _window;

	public string Title { get; set; }

	public string Id => "{2495C316-F222-4AAC-8F2C-65DF524D75CC}";

	public bool Execute()
	{
		if (_window == null || !((UIElement)_window).IsVisible)
		{
			_window = new EnfusePluginWindow();
			((FrameworkElement)_window).DataContext = new EnfusePluginViewModel((Window)(object)_window);
			EnfusePluginWindow window = _window;
			IMainWindowPlugin selectedWindow = ServiceProvider.PluginManager.SelectedWindow;
			((Window)window).Owner = (Window)((selectedWindow is Window) ? selectedWindow : null);
			((Window)_window).Show();
		}
		else
		{
			((Window)_window).Activate();
		}
		return true;
	}

	public void Init()
	{
		if (_window != null)
		{
			((EnfusePluginViewModel)((FrameworkElement)_window).DataContext).Generate();
		}
	}

	public EnfusePlugin()
	{
		Title = "Enfuse";
	}
}
