using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Kazyx.DeviceDiscovery;

namespace SonyCameraCommunication;

public class CameraDiscovery
{
	private IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("128.0.0.1"), 60000);

	private IPEndPoint multicastEndpoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);

	private Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

	public static string response;

	private const string upnp_ns = "{urn:schemas-upnp-org:device-1-0}";

	private const string sony_ns = "{urn:schemas-sony-com:av}";

	public bool UDPSocketSetup()
	{
		bool flag = false;
		try
		{
			udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
			udpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(multicastEndpoint.Address, IPAddress.Any));
			udpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
			udpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, optionValue: true);
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
						udpSocket.Bind(new IPEndPoint(unicastAddress.Address, 60000));
						flag = true;
						break;
					}
				}
				udpSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, IPAddress.HostToNetworkOrder(iPv4Properties.Index));
			}
			if (!flag)
			{
				throw new Exception("Camea not connected !");
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public bool MSearch()
	{
		string s = "M-SEARCH * HTTP/1.1\r\nHOST:239.255.255.250:1900\r\nMAN:\"ssdp:discover\"\r\nMX:1\r\nST:urn:schemas-sony-com:service:ScalarWebAPI:1\r\n\r\n";
		byte[] array = new byte[64000];
		int num = 0;
		DateTime now = DateTime.Now;
		while (true)
		{
			try
			{
				udpSocket.SendTo(Encoding.UTF8.GetBytes(s), SocketFlags.None, multicastEndpoint);
				if (udpSocket.Available > 0)
				{
					num = udpSocket.Receive(array, SocketFlags.None);
					if (num > 0)
					{
						response = Encoding.UTF8.GetString(array, 0, num);
						return true;
					}
				}
			}
			catch (Exception)
			{
				return false;
			}
			if ((DateTime.Now - now).TotalSeconds > 4000.0)
			{
				break;
			}
			Thread.Sleep(150);
		}
		return false;
	}

	public string DeviceDescription()
	{
		string[] array = response.Split(new char[1] { '\n' });
		string requestUriString = "";
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.StartsWith("LOCATION: "))
			{
				requestUriString = text.Substring(text.LastIndexOf(" "));
			}
		}
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(requestUriString);
		httpWebRequest.Method = "GET";
		WebResponse webResponse = httpWebRequest.GetResponse();
		Stream responseStream = webResponse.GetResponseStream();
		StreamReader streamReader = new StreamReader(responseStream);
		return streamReader.ReadToEnd();
	}

	public SonyCameraDeviceInfo AnalyzeDescription(string response)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		XDocument xDocument = XDocument.Parse(response);
		XElement xElement = xDocument.Root.Element("{urn:schemas-upnp-org:device-1-0}device");
		if (xElement == null)
		{
			return null;
		}
		string value = xElement.Element("{urn:schemas-upnp-org:device-1-0}friendlyName").Value;
		string value2 = xElement.Element("{urn:schemas-upnp-org:device-1-0}modelName").Value;
		string value3 = xElement.Element("{urn:schemas-upnp-org:device-1-0}UDN").Value;
		XElement xElement2 = xElement.Element("{urn:schemas-sony-com:av}X_ScalarWebAPI_DeviceInfo");
		if (xElement2 == null)
		{
			return null;
		}
		XElement xElement3 = xElement2.Element("{urn:schemas-sony-com:av}X_ScalarWebAPI_ServiceList");
		foreach (XElement item in xElement3.Elements())
		{
			string value4 = item.Element("{urn:schemas-sony-com:av}X_ScalarWebAPI_ServiceType").Value;
			string value5 = item.Element("{urn:schemas-sony-com:av}X_ScalarWebAPI_ActionList_URL").Value;
			if (value4 != null && value5 != null)
			{
				string value6 = ((!value5.EndsWith("/")) ? (value5 + "/" + value4) : (value5 + value4));
				dictionary.Add(value4, value6);
			}
		}
		if (dictionary.Count == 0)
		{
			return null;
		}
		return new SonyCameraDeviceInfo(value3, value2, value, dictionary);
	}
}
