using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;

namespace CameraControl.windows;

public partial class PresetEditWnd : Window, INotifyPropertyChanged, IComponentConnector
{
	private CameraPreset _selectedCameraPreset;

	public CameraPreset SelectedCameraPreset
	{
		get
		{
			return _selectedCameraPreset;
		}
		set
		{
			_selectedCameraPreset = value;
			NotifyPropertyChanged("SelectedCameraPreset");
		}
	}

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public PresetEditWnd()
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

	private void btn_del_preset_Click(object sender, RoutedEventArgs e)
	{
		if (lst_preset.SelectedItem != null)
		{
			ServiceProvider.Settings.CameraPresets.Remove((CameraPreset)lst_preset.SelectedItem);
		}
	}

	private void btn_del_prop_Click(object sender, RoutedEventArgs e)
	{
		if (lst_properties.SelectedItem != null)
		{
			SelectedCameraPreset.Values.Remove((ValuePair)lst_properties.SelectedItem);
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		ServiceProvider.Settings.Save();
	}
}
