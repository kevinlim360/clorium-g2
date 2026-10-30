using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Interfaces;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class StatisticsWnd : Window, IWindow, IComponentConnector
{
	public StatisticsWnd()
	{
		InitializeComponent();
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "StatisticsWnd_Show":
			Dispatcher.Invoke(() =>
			{
				if (DataContext == null)
				{
					DataContext = new StatisticsViewModel();
				}
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				Show();
				Activate();
				Focus();
			});
			break;
		case "StatisticsWnd_Hide":
			Hide();
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
			ServiceProvider.WindowsManager.ExecuteCommand("StatisticsWnd_Hide");
		}
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
	}
}
