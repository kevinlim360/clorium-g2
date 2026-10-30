using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace CameraControl.windows;

public partial class FullScreenWnd : MetroWindow, IWindow, IComponentConnector
{
	private System.Timers.Timer _timer = new System.Timers.Timer();

	public FullScreenWnd()
	{
		InitializeComponent();
		KeyDown += FullScreenWnd_KeyDown;
		_timer.Elapsed += _timer_Elapsed;
		KeyDown += image1_KeyDown;
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("FullScreenWnd_Hide");
	}

	private void FullScreenWnd_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
	}

	private void image1_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Escape)
		{
			HideWindow();
		}
	}

	private void image1_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount >= 2 && e.LeftButton == MouseButtonState.Pressed)
		{
			HideWindow();
		}
	}

	private void image1_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
	{
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "FullScreenWnd_Show":
			Dispatcher.BeginInvoke((Action)(() =>
			{
				Show();
				Activate();
				Focus();
				if (ServiceProvider.Settings.FullScreenInSecondaryMonitor)
				{
					List<Screen> source = Screen.AllScreens.ToList();
					using IEnumerator<Rectangle> enumerator = (from item in source
						where !item.Primary
						select item.WorkingArea).GetEnumerator();
					if (enumerator.MoveNext())
					{
						Rectangle current = enumerator.Current;
						Left = current.Left;
						Top = current.Top;
						Width = current.Width;
						Height = current.Height;
						Topmost = true;
					}
				}
				WindowState = WindowState.Maximized;
				WindowStyle = WindowStyle.None;
			}));
			break;
		case "FullScreenWnd_ShowTimed":
		{
			DateTime startTime = DateTime.Now;
			Thread.Sleep(150);
			while (ServiceProvider.Settings.SelectedBitmap != null && ServiceProvider.Settings.SelectedBitmap.FileItem != null && ServiceProvider.Settings.SelectedBitmap.FileItem.Loading)
			{
				Thread.Sleep(10);
				if ((DateTime.Now - startTime).TotalSeconds > 3.0)
				{
					break;
				}
			}
			Dispatcher.BeginInvoke((Action)(() =>
			{
				try
				{
					Show();
					Activate();
					Topmost = true;
					Topmost = false;
					Focus();
					if (ServiceProvider.Settings.FullScreenInSecondaryMonitor)
					{
						List<Screen> source = Screen.AllScreens.ToList();
						using IEnumerator<Rectangle> enumerator = (from item in source
							where !item.Primary
							select item.WorkingArea).GetEnumerator();
						if (enumerator.MoveNext())
						{
							Rectangle current = enumerator.Current;
							Left = current.Left;
							Top = current.Top;
							Width = current.Width;
							Height = current.Height;
							Topmost = true;
						}
					}
					WindowState = WindowState.Maximized;
					WindowStyle = WindowStyle.None;
					_timer.Stop();
					_timer.Interval = ServiceProvider.Settings.PreviewSeconds * 1000;
					_timer.Start();
				}
				catch (Exception exception)
				{
					Log.Error("Full screen ", exception);
				}
			}));
			break;
		}
		case "FullScreenWnd_Hide":
			Dispatcher.Invoke(() =>
			{
				_timer.Stop();
				Hide();
			});
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
			HideWindow();
		}
	}

	private async void HideWindow()
	{
		if (!string.IsNullOrEmpty(ServiceProvider.Settings.FullScreenPassword))
		{
			KeyDown -= image1_KeyDown;
			LoginDialogData result = await this.ShowLoginAsync("Closing fullscreen ...", "Enter your password", new LoginDialogSettings
			{
				ColorScheme = MetroDialogOptions.ColorScheme,
				ShouldHideUsername = true,
				AffirmativeButtonText = "Close"
			});
			if (result != null && result.Password == ServiceProvider.Settings.FullScreenPassword)
			{
				ServiceProvider.WindowsManager.ExecuteCommand("FullScreenWnd_Hide");
			}
			KeyDown += image1_KeyDown;
		}
		else
		{
			ServiceProvider.WindowsManager.ExecuteCommand("FullScreenWnd_Hide");
		}
	}

	private void ButtonPrev_Click(object sender, RoutedEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("Prev_Image");
	}

	private void ButtonNext_Click(object sender, RoutedEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("Next_Image");
	}

	private void MetroWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
	{
		TriggerClass.KeyDown(e);
	}
}
