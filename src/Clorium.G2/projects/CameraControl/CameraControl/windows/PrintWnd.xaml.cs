using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.ViewModel;
using Microsoft.Win32;

namespace CameraControl.windows;

public partial class PrintWnd : Window, IWindow, IComponentConnector
{
	public PrintWnd()
	{
		InitializeComponent();
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "PrintWnd_Show":
			Dispatcher.Invoke(() =>
			{
				PrintViewModel printViewModel = new PrintViewModel();
				Owner = (Window)ServiceProvider.PluginManager.SelectedWindow;
				printViewModel.LoadPrinterSettings();
				DataContext = printViewModel;
				Show();
				Activate();
				Focus();
			});
			break;
		case "PrintWnd_Hide":
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
			ServiceProvider.WindowsManager.ExecuteCommand("PrintWnd_Hide");
		}
	}

	private void PrintButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			((PrintViewModel)DataContext).Dlg.PrintVisual(ItemsControl, "");
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message);
			Log.Error("Error print", ex);
		}
	}

	private void JpgButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			SaveFileDialog dialog = new SaveFileDialog
			{
				Filter = "Jpg files (*.jpg)|*.jpg|All files|*.*"
			};
			if (dialog.ShowDialog() == true)
			{
				BitmapLoader.Save2Jpg(BitmapLoader.SaveImageSource(ItemsControl, (int)ItemsControl.Width, (int)ItemsControl.Height), dialog.FileName);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, "Save error");
			Log.Error("Save error", ex);
		}
	}
}
