using System.Collections.Generic;
using System.Xml.Serialization;

namespace CameraControl.Devices.Custom;

public class DeviceProperty
{
	[XmlAttribute]
	public string Name { get; set; }

	[XmlAttribute]
	public uint Code { get; set; }

	[XmlAttribute]
	public bool ReadOnly { get; set; }

	[XmlAttribute]
	public string MapTo { get; set; }

	public List<DevicePropertyValue> Values { get; set; }

	public DeviceProperty()
	{
		Values = new List<DevicePropertyValue>();
	}
}
