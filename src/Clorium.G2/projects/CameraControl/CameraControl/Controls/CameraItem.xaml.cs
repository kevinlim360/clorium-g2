using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;

namespace CameraControl.Controls;

public partial class CameraItem : UserControl, INotifyPropertyChanged, IComponentConnector
{
	public static readonly DependencyProperty CameraDeviceProperty = DependencyProperty.Register("CameraDevice", typeof(ICameraDevice), typeof(CameraItem), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, CameraDeviceCallBack));

	private CameraProperty _cameraProperty;

	public CameraProperty CameraProperty
	{
		get
		{
			return _cameraProperty;
		}
		set
		{
			_cameraProperty = value;
			NotifyPropertyChanged("CameraProperty");
		}
	}

	public ICameraDevice CameraDevice
	{
		get
		{
			return (ICameraDevice)GetValue(CameraDeviceProperty);
		}
		set
		{
			SetValue(CameraDeviceProperty, value);
		}
	}

	public virtual event PropertyChangedEventHandler PropertyChanged;

	private static void CameraDeviceCallBack(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
	{
		CameraItem item = dependencyObject as CameraItem;
		ICameraDevice device = dependencyPropertyChangedEventArgs.NewValue as ICameraDevice;
		if (item != null && device != null)
		{
			item.CameraProperty = ServiceProvider.Settings.CameraProperties.Get(device);
		}
	}

	public CameraItem()
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
}
