using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;

namespace CameraControl.windows;

public partial class BrowseWnd : Window, INotifyPropertyChanged, IWindow, IComponentConnector
{
	private PhotoSession _selectedPhotoSession;

	public PhotoSession SelectedPhotoSession
	{
		get
		{
			return _selectedPhotoSession;
		}
		set
		{
			_selectedPhotoSession = value;
			NotifyPropertyChanged("SelectedPhotoSession");
		}
	}

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public BrowseWnd()
	{
		InitializeComponent();
	}

	public virtual void NotifyPropertyChanged(string info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "BrowseWnd_Show":
			ServiceProvider.Settings.PropertyChanged += Settings_PropertyChanged;
			Dispatcher.Invoke(() =>
			{
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				SelectedPhotoSession = ServiceProvider.Settings.DefaultSession;
				Show();
				Activate();
				Focus();
			});
			break;
		case "BrowseWnd_Hide":
			ServiceProvider.Settings.PropertyChanged -= Settings_PropertyChanged;
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

	private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "DefaultSession")
		{
			SelectedPhotoSession = ServiceProvider.Settings.DefaultSession;
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("BrowseWnd_Hide");
		}
	}

	private void lst_profiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		if (lst_profiles.SelectedItem != null)
		{
			ServiceProvider.Settings.DefaultSession = SelectedPhotoSession;
			ServiceProvider.WindowsManager.ExecuteCommand("BrowseWnd_Hide");
		}
	}

	private void lst_files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
	}
}
