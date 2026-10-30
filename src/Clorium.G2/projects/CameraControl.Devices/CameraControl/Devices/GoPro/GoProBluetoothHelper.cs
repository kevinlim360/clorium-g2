extern alias WinInterop;
using System;
using Windows.Devices.Bluetooth;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using CameraControl.Devices.Classes;
using Windows.Devices.Enumeration;

namespace CameraControl.Devices.GoPro;

public class GoProBluetoothHelper : INotifyPropertyChanged
{
	private DeviceWatcher mDeviceWatcher = null;

	private string message;

	private readonly Dictionary<string, DeviceInformation> mAllDevices = new Dictionary<string, DeviceInformation>();

	public string Message
	{
		get
		{
			return message;
		}
		set
		{
			message = value;
			Log.Debug("Bluetooth helper message :" + message);
			OnPropertyChanged("Message");
		}
	}

	public AsyncObservableCollection<GDeviceInformation> Devices { get; set; }

	public event PropertyChangedEventHandler PropertyChanged;

	public GoProBluetoothHelper()
	{
		Devices = new AsyncObservableCollection<GDeviceInformation>();
	}

	public void Initialize()
	{
		// Register watcher callbacks through the WinRT token manager before scanning.
            string BLESelector = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"";
            DeviceInformationKind deviceInformationKind = DeviceInformationKind.AssociationEndpoint;
            string[] requiredProperties = { "System.Devices.Aep.Bluetooth.Le.IsConnectable", "System.Devices.Aep.IsConnected" };

            mDeviceWatcher = DeviceInformation.CreateWatcher(BLESelector, requiredProperties, deviceInformationKind);
            WinInterop::System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.Foundation.TypedEventHandler<DeviceWatcher, DeviceInformation>>(mDeviceWatcher.add_Added, mDeviceWatcher.remove_Added, MDeviceWatcher_Added); ;
            WinInterop::System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.Foundation.TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>>(mDeviceWatcher.add_Updated, mDeviceWatcher.remove_Updated, MDeviceWatcher_Updated); ;
            WinInterop::System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.Foundation.TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>>(mDeviceWatcher.add_Removed, mDeviceWatcher.remove_Removed, MDeviceWatcher_Removed); ;
            WinInterop::System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.Foundation.TypedEventHandler<DeviceWatcher, object>>(mDeviceWatcher.add_EnumerationCompleted, mDeviceWatcher.remove_EnumerationCompleted, MDeviceWatcher_EnumerationCompleted); ;
            WinInterop::System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.Foundation.TypedEventHandler<DeviceWatcher, object>>(mDeviceWatcher.add_Stopped, mDeviceWatcher.remove_Stopped, MDeviceWatcher_Stopped); ;

            Message = "Scanning for devices...";
            mDeviceWatcher.Start();
        }

	public void Rescan()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if (mDeviceWatcher != null)
		{
			mDeviceWatcher.Stop();
			Thread.Sleep(1000);
			if ((int)mDeviceWatcher.Status == 4)
			{
				mDeviceWatcher.Start();
			}
		}
	}

	public void Pair(GDeviceInformation lDevice)
	{
		// Keep the custom pairing object stable for both event accessors and pairing.
            if (lDevice != null)
            {
                Message = "Pairing started";

                var mBLED = BluetoothLEDevice.FromIdAsync(lDevice.DeviceInfo.Id).AsTask().Result;
                var customPairing = mBLED.DeviceInformation.Pairing.Custom;
                WinInterop::System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<Windows.Foundation.TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs>>(customPairing.add_PairingRequested, customPairing.remove_PairingRequested, Custom_PairingRequested);
                if (mBLED.DeviceInformation.Pairing.CanPair)
                {
                    DevicePairingProtectionLevel dppl = mBLED.DeviceInformation.Pairing.ProtectionLevel;
                    DevicePairingResult dpr = mBLED.DeviceInformation.Pairing.Custom.PairAsync(DevicePairingKinds.ConfirmOnly, dppl).AsTask().Result;

                    Message = "Pairing result = " + dpr.Status.ToString();
                }
                else
                {
                    Message = "Pairing failed";
                }
            }
            else
            {
                Message = "Select a device";
            }
        }

	private void Custom_PairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
	{
		Message = "Pairing request...";
		args.Accept();
	}

	private void MDeviceWatcher_Stopped(DeviceWatcher sender, object args)
	{
		Message = "Scan Stoped";
	}

	private void MDeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
	{
		Message = "Scan Complete";
	}

	private void MDeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
	{
		for (int i = 0; i < Devices.Count; i++)
		{
			if (Devices[i].DeviceInfo.Id == args.Id)
			{
				Devices.RemoveAt(i);
				break;
			}
		}
	}

	private void MDeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		if (args.Properties.ContainsKey("System.Devices.Aep.Bluetooth.Le.IsConnectable"))
		{
			flag = (bool)args.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"];
		}
		if (args.Properties.ContainsKey("System.Devices.Aep.IsConnected"))
		{
			flag2 = (bool)args.Properties["System.Devices.Aep.IsConnected"];
		}
		for (int i = 0; i < Devices.Count; i++)
		{
			if (Devices[i].DeviceInfo.Id == args.Id)
			{
				flag3 = true;
				Devices[i].DeviceInfo.Update(args);
				Devices[i].IsPresent = flag;
				Devices[i].IsConnected = flag2;
				break;
			}
		}
		if (!flag3 && (flag | flag2) && mAllDevices.ContainsKey(args.Id))
		{
			mAllDevices[args.Id].Update(args);
			Devices.Add(new GDeviceInformation(mAllDevices[args.Id], flag, flag2));
		}
	}

	private void MDeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
	{
		bool flag = false;
		bool flag2 = false;
		if (args.Properties.ContainsKey("System.Devices.Aep.Bluetooth.Le.IsConnectable"))
		{
			flag = (bool)args.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"];
		}
		if (args.Properties.ContainsKey("System.Devices.Aep.IsConnected"))
		{
			flag2 = (bool)args.Properties["System.Devices.Aep.IsConnected"];
		}
		if (!(args.Name != "") || !args.Name.Contains("GoPro") || !(args.Name != "GoPro Cam"))
		{
			return;
		}
		bool flag3 = false;
		if (!mAllDevices.ContainsKey(args.Id))
		{
			mAllDevices.Add(args.Id, args);
		}
		for (int i = 0; i < Devices.Count; i++)
		{
			if (Devices[i].DeviceInfo.Id == args.Id)
			{
				flag3 = true;
				Devices[i].DeviceInfo = args;
				Devices[i].IsPresent = flag;
				Devices[i].IsConnected = flag2;
				break;
			}
		}
		if (!flag3 && (flag | flag2))
		{
			Devices.Add(new GDeviceInformation(args, flag, flag2));
		}
	}

	private void OnPropertyChanged(string name)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
	}
}
