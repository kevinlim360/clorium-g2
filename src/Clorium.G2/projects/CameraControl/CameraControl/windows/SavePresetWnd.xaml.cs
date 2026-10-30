using System.Windows;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.windows;

public partial class SavePresetWnd : Window, IComponentConnector
{
	public CameraPreset CameraPreset { get; set; }

	public SavePresetWnd(CameraPreset cameraPreset)
	{
		InitializeComponent();
		CameraPreset = cameraPreset;
		CameraPreset.BeginEdit();
	}

	private void btn_save_Click(object sender, RoutedEventArgs e)
	{
		CameraPreset.EndEdit();
		DialogResult = true;
		Close();
	}

	private void btn_cancel_Click(object sender, RoutedEventArgs e)
	{
		CameraPreset.CancelEdit();
		Close();
	}
}
