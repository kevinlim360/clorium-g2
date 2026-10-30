using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Annotations;
using CameraControl.Core;
using CameraControl.Devices;

namespace CameraControl.windows;

public partial class GetIpWnd : Window, INotifyPropertyChanged, IComponentConnector
{
	private IWifiDeviceProvider _wifiDeviceProvider;

	private string _ip;

	public string Ip
	{
		get
		{
			return _ip;
		}
		set
		{
			_ip = value;
			OnPropertyChanged("Ip");
		}
	}

	public int Type { get; set; }

	public List<IWifiDeviceProvider> Providers => ServiceProvider.DeviceManager.WifiDeviceProviders;

	public IWifiDeviceProvider WifiDeviceProvider
	{
		get
		{
			return _wifiDeviceProvider;
		}
		set
		{
			_wifiDeviceProvider = value;
			OnPropertyChanged("WifiDeviceProvider");
			Ip = _wifiDeviceProvider.DefaultIp;
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public GetIpWnd()
	{
		InitializeComponent();
	}

	private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
	{
	}

	private void btn_conect_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
	}

	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
