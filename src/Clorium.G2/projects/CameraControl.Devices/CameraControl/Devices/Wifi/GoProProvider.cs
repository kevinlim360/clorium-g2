using System.Net;
using CameraControl.Devices.Classes;
using CameraControl.Devices.GoPro;
using CameraControl.Devices.Others;
using Newtonsoft.Json.Linq;

namespace CameraControl.Devices.Wifi;

public class GoProProvider : IWifiDeviceProvider
{
	public string Name { get; set; }

	public string DefaultIp { get; set; }

	public DeviceDescriptor Connect(string address)
	{
		return Connect(address, null);
	}

	public DeviceDescriptor Connect(string address, GoProBluetoothDevice bluetoothDevice)
	{
		DeviceDescriptor deviceDescriptor = new DeviceDescriptor
		{
			WpdId = "GoProCamera"
		};
		using (WebClient webClient = new WebClient())
		{
			JObject jObject = JObject.Parse(webClient.DownloadString("http://" + address + "/gp/gpControl"));
			Log.Debug(string.Format("Model/Firmware: {0} / {1}", jObject["info"]["model_name"], jObject["info"]["firmware_version"]));
			string text = jObject["info"]["firmware_version"].Value<string>().Substring(0, 3);
			switch (jObject["info"]["firmware_version"].Value<string>().Substring(0, 3))
			{
			case "H22":
			case "H21":
			case "HD9":
			{
				GoPro9Camera goPro9Camera = (GoPro9Camera)(deviceDescriptor.CameraDevice = new GoPro9Camera());
				goPro9Camera.Init(address, jObject, bluetoothDevice);
				break;
			}
			default:
			{
				GoProBaseCamera goProBaseCamera = (GoProBaseCamera)(deviceDescriptor.CameraDevice = new GoProBaseCamera());
				goProBaseCamera.Init(address, jObject, bluetoothDevice);
				break;
			}
			}
		}
		return deviceDescriptor;
	}

	public GoProProvider()
	{
		Name = "GoPro Camera";
		DefaultIp = "10.5.5.9";
	}
}
