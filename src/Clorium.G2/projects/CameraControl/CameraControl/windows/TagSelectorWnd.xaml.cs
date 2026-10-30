using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices.Classes;

namespace CameraControl.windows;

public partial class TagSelectorWnd : Window, IWindow, INotifyPropertyChanged, IComponentConnector
{
	private AsyncObservableCollection<TagItem> _tags1;

	private AsyncObservableCollection<TagItem> _tags2;

	private AsyncObservableCollection<TagItem> _tags3;

	private AsyncObservableCollection<TagItem> _tags4;

	public AsyncObservableCollection<TagItem> Tags1
	{
		get
		{
			return _tags1;
		}
		set
		{
			_tags1 = value;
			NotifyPropertyChanged("Tags1");
		}
	}

	public AsyncObservableCollection<TagItem> Tags2
	{
		get
		{
			return _tags2;
		}
		set
		{
			_tags2 = value;
			NotifyPropertyChanged("Tags2");
		}
	}

	public AsyncObservableCollection<TagItem> Tags3
	{
		get
		{
			return _tags3;
		}
		set
		{
			_tags3 = value;
			NotifyPropertyChanged("Tags3");
		}
	}

	public AsyncObservableCollection<TagItem> Tags4
	{
		get
		{
			return _tags4;
		}
		set
		{
			_tags4 = value;
			NotifyPropertyChanged("Tags4");
		}
	}

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public TagSelectorWnd()
	{
		InitializeComponent();
	}

	private void LoadData()
	{
		Tags1 = new AsyncObservableCollection<TagItem>(ServiceProvider.Settings.DefaultSession.Tags.Where((TagItem x) => x.Tag1Checked));
		Tags2 = new AsyncObservableCollection<TagItem>(ServiceProvider.Settings.DefaultSession.Tags.Where((TagItem x) => x.Tag2Checked));
		Tags3 = new AsyncObservableCollection<TagItem>(ServiceProvider.Settings.DefaultSession.Tags.Where((TagItem x) => x.Tag3Checked));
		Tags4 = new AsyncObservableCollection<TagItem>(ServiceProvider.Settings.DefaultSession.Tags.Where((TagItem x) => x.Tag4Checked));
	}

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "TagSelectorWnd_Show":
			Dispatcher.Invoke(() =>
			{
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				LoadData();
				Show();
				Activate();
				Focus();
				ServiceProvider.DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
			});
			break;
		case "TagSelectorWnd_Hide":
			ServiceProvider.DeviceManager.PhotoCaptured -= DeviceManager_PhotoCaptured;
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

	private void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
	{
		Dispatcher.Invoke(() =>
		{
			Activate();
			Focus();
			txt_barcode.Focus();
			txt_barcode.SelectAll();
		});
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("TagSelectorWnd_Hide");
		}
	}

	public virtual void NotifyPropertyChanged(string info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}

	private void btn_clear_tag1(object sender, RoutedEventArgs e)
	{
		ServiceProvider.Settings.DefaultSession.SelectedTag1 = null;
	}

	private void btn_clear_tag2(object sender, RoutedEventArgs e)
	{
		ServiceProvider.Settings.DefaultSession.SelectedTag2 = null;
	}

	private void btn_clear_tag3(object sender, RoutedEventArgs e)
	{
		ServiceProvider.Settings.DefaultSession.SelectedTag3 = null;
	}

	private void btn_clear_tag4(object sender, RoutedEventArgs e)
	{
		ServiceProvider.Settings.DefaultSession.SelectedTag4 = null;
	}
}
