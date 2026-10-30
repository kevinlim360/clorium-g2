using System.Windows;
using CameraControl.Core;
using CameraControl.Core.Interfaces;

namespace CameraControl.Plugins.ToolPlugins;

public class ArduinoPlugin : IToolPlugin
{
	private ArduinoWindow _window;

	private ArduinoViewModel _viewModel;

	public string Title { get; set; }

	public string Id => "{5B6842B3-E486-4A3E-A0C3-26988B6F0123}";

	public bool Execute()
	{
		if (_window == null || !((UIElement)_window).IsVisible)
		{
			_window = new ArduinoWindow();
			((FrameworkElement)_window).DataContext = _viewModel;
			ArduinoWindow window = _window;
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
		_viewModel = new ArduinoViewModel();
		if (_viewModel.Active)
		{
			_viewModel.OpenPort();
			if (_viewModel.ButtonsStartup)
			{
				_viewModel.ShowButtons();
			}
		}
	}

	public ArduinoPlugin()
	{
		Title = "Arduino (Serial)";
	}
}
