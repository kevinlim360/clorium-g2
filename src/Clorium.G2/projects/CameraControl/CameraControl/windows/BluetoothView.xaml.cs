using System.Windows;
using System.Windows.Markup;
using CameraControl.ViewModel;

namespace CameraControl.windows;

public partial class BluetoothView : Window, IComponentConnector
{
	public BluetoothView()
	{
		InitializeComponent();
		((BlueToothViewModel)DataContext).Window = this;
	}
}
