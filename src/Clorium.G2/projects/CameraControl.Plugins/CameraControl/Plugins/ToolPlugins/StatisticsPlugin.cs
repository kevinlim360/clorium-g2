using System.Windows;
using CameraControl.Core;
using CameraControl.Core.Interfaces;

namespace CameraControl.Plugins.ToolPlugins;

public class StatisticsPlugin : IToolPlugin
{
	public string Title { get; set; }

	public string Id => "{3EC27FCC-9BE3-4252-BA54-9A2284B998DE}";

	public bool Execute()
	{
		StatisticsWindow _window = new StatisticsWindow();
		((FrameworkElement)_window).DataContext = new StatisticsViewModel();
		IMainWindowPlugin selectedWindow = ServiceProvider.PluginManager.SelectedWindow;
		((Window)_window).Owner = (Window)((selectedWindow is Window) ? selectedWindow : null);
		((Window)_window).ShowDialog();
		return true;
	}

	public void Init()
	{
	}

	public StatisticsPlugin()
	{
		Title = "Statistics";
	}
}
