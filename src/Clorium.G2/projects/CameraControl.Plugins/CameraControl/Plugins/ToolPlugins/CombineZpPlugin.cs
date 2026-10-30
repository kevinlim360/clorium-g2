using System.IO;
using System.Windows;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices.Classes;
using Newtonsoft.Json;

namespace CameraControl.Plugins.ToolPlugins;

public class CombineZpPlugin : IToolPlugin, IExecutePlugin
{
	private CombineZpWindow _window;

	private CombineZpViewModel _viewModel;

	public string Title { get; set; }

	public string Id => "{F3155291-D688-49B8-B22D-E74A2D5E020E}";

	public bool Execute()
	{
		if (_window == null || !((UIElement)_window).IsVisible)
		{
			_window = new CombineZpWindow();
			_viewModel = new CombineZpViewModel((Window)(object)_window);
			((FrameworkElement)_window).DataContext = _viewModel;
			CombineZpWindow window = _window;
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
	}

	public CombineZpPlugin()
	{
		Title = "Combine Zp";
	}

	public string Execute(string command)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected Obj, but got Unknown
		dynamic data = JsonConvert.DeserializeObject(command);
		CombineZpViewModel model = _viewModel ?? new CombineZpViewModel((Window)ServiceProvider.PluginManager.SelectedWindow);
		model.Init();
		model.Files = new AsyncObservableCollection<FileItem>();
		foreach (object item in data.Files)
		{
			string file = (string)(dynamic)item;
			model.Files.Add(new FileItem(file));
		}
		model.CopyFiles((bool)data.ThumbOnly);
		model.Combine();
		if (File.Exists(model._resulfile))
		{
			File.Copy(model._resulfile, (string)data.ResultFile);
		}
		return "";
	}
}
