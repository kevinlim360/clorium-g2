using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Interfaces;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class TimeLapseWnd : Window, IWindow, IComponentConnector
{
	public TimeLapseWnd()
	{
		InitializeComponent();
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "TimeLapseWnd_Show":
			Dispatcher.Invoke(() =>
			{
				if (DataContext == null)
				{
					DataContext = new TimelapseViewModel();
				}
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				Show();
				Activate();
				Focus();
			});
			break;
		case "TimeLapseWnd_Hide":
			Dispatcher.Invoke(Hide);
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				Hide();
				Close();
			});
			break;
		}
	}

	private void MetroWindow_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("TimeLapseWnd_Hide");
		}
	}
}
