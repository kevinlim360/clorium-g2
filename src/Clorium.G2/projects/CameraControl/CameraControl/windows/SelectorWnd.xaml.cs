using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Interfaces;

namespace CameraControl.windows;

public partial class SelectorWnd : Window, IComponentConnector
{
	public SelectorWnd()
	{
		InitializeComponent();
		foreach (IMainWindowPlugin mainWindowPlugin in ServiceProvider.PluginManager.MainWindowPlugins)
		{
			lst_windows.Items.Add(mainWindowPlugin.DisplayName);
		}
		btn_select.Focus();
	}

	private void btn_select_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
