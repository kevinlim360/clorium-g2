using System.Collections.Generic;

namespace Kazyx.DeviceDiscovery;

public class SonyCameraDeviceInfo
{
	private Dictionary<string, string> _Endpoints;

	public Dictionary<string, string> Endpoints
	{
		get
		{
			return new Dictionary<string, string>(_Endpoints);
		}
		private set
		{
			_Endpoints = value;
		}
	}

	public string FriendlyName { get; private set; }

	public string ModelName { get; private set; }

	public string UDN { get; private set; }

	internal SonyCameraDeviceInfo(string udn, string mname, string fname, Dictionary<string, string> ep)
	{
		UDN = udn;
		ModelName = mname;
		FriendlyName = fname;
		Endpoints = ep;
	}
}
