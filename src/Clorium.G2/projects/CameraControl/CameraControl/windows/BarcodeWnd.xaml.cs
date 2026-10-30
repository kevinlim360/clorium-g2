using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Interfaces;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class BarcodeWnd : Window, IWindow, IComponentConnector
{
	private Timer _timer = new Timer(1000.0);

	public BarcodeWnd()
	{
		InitializeComponent();
		_timer.AutoReset = true;
		_timer.Elapsed += _timer_Elapsed;
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		if (IsVisible)
		{
			Dispatcher.Invoke(SetFocus);
		}
	}

	private void SetFocus()
	{
		if (((BarcodeViewModel)DataContext).KeepActive && (!TextBox.IsFocused || !IsActive))
		{
			Activate();
			TextBox.Focus();
			TextBox.SelectAll();
		}
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "BarcodeWnd_Show":
			Dispatcher.Invoke(() =>
			{
				if (DataContext == null)
				{
					DataContext = new BarcodeViewModel();
				}
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				_timer.Start();
				Show();
				Activate();
				Focus();
			});
			break;
		case "BarcodeWnd_Hide":
			_timer.Stop();
			Hide();
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				_timer.Stop();
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
			ServiceProvider.WindowsManager.ExecuteCommand("BarcodeWnd_Hide");
		}
	}

	private void TextBox_OnKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return)
		{
			TextBox.SelectAll();
			((BarcodeViewModel)DataContext).EnterPressed();
		}
	}
}
