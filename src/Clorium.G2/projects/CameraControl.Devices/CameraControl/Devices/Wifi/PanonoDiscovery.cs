using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Rssdp;
using Rssdp.Infrastructure;

namespace CameraControl.Devices.Wifi;

public class PanonoDiscovery
{
	public string EndPoint { get; set; }

	public bool UDPSocketSetup()
	{
		try
		{
			SsdpDeviceLocator ssdpDeviceLocator = null;
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			NetworkInterface[] array = allNetworkInterfaces;
			foreach (NetworkInterface networkInterface in array)
			{
				IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
				if (!networkInterface.GetIPProperties().MulticastAddresses.Any() || !networkInterface.SupportsMulticast || OperationalStatus.Up != networkInterface.OperationalStatus || networkInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
				{
					continue;
				}
				IPv4InterfaceProperties iPv4Properties = networkInterface.GetIPProperties().GetIPv4Properties();
				if (iPv4Properties == null)
				{
					continue;
				}
				foreach (UnicastIPAddressInformation unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
				{
					if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
					{
						ssdpDeviceLocator = new SsdpDeviceLocator(new SsdpCommunicationsServer(new SocketFactory(unicastAddress.Address.ToString())));
						break;
					}
				}
			}
			if (ssdpDeviceLocator == null)
			{
				throw new Exception("Camea not connected !");
			}
			ssdpDeviceLocator.NotificationFilter = "ssdp:all";
			ssdpDeviceLocator.StartListeningForNotifications();
			List<DiscoveredSsdpDevice> list = ssdpDeviceLocator.SearchAsync(new TimeSpan(0, 0, 15)).Result.ToList();
			if (list.Any())
			{
				foreach (DiscoveredSsdpDevice item in list)
				{
					if (item.NotificationType == "panono:ball-camera")
					{
						EndPoint = item.DescriptionLocation.ToString();
						return true;
					}
				}
			}
			return false;
		}
		catch (Exception exception)
		{
			Log.Debug("Camera discovery fail", exception);
			return false;
		}
	}
}
