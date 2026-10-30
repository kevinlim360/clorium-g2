using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using CameraControl.Devices;

namespace CameraControl.Core.Wpf;

public partial class ProgressWindow : Window, INotifyPropertyChanged, IComponentConnector
{
	private ICameraDevice camera;

	public string Label
	{
		get
		{
			return (string)lbl_label.Content;
		}
		set
		{
			Dispatcher.Invoke(() =>
			{
				lbl_label.Content = value;
			});
		}
	}

	public string Label2
	{
		get
		{
			return (string)lbl_label2.Content;
		}
		set
		{
			Dispatcher.Invoke(() =>
			{
				lbl_label2.Content = value;
			});
		}
	}

	public double Progress
	{
		get
		{
			return progressBar.Value;
		}
		set
		{
			Dispatcher.Invoke(() =>
			{
				progressBar.Value = ((progressBar.Maximum < value) ? value : progressBar.Maximum);
			});
		}
	}

	public double MaxValue
	{
		get
		{
			return progressBar.Maximum;
		}
		set
		{
			Dispatcher.Invoke(() =>
			{
				progressBar.Maximum = value;
			});
		}
	}

	public ICameraDevice Camera
	{
		get
		{
			return camera;
		}
		set
		{
			camera = value;
			NotifyPropertyChanged("Camera");
		}
	}

	public ImageSource ImageSource
	{
		get
		{
			return image.Source;
		}
		set
		{
			Dispatcher.Invoke(() =>
			{
				image.BeginInit();
				image.Source = value;
				image.EndInit();
			});
		}
	}

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public ProgressWindow()
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

	public new void Hide()
	{
		try
		{
			Dispatcher.Invoke(() =>
			{
				base.Hide();
			});
		}
		catch (Exception message)
		{
			Log.Error(message);
		}
	}
}
