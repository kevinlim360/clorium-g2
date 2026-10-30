using System.Xml.Serialization;

namespace CameraControl.Devices.Xml;

public class XmlEventDescriptor
{
	private string _hexCode;

	[XmlAttribute]
	public uint Code { get; set; }

	[XmlAttribute]
	public string Name { get; set; }

	[XmlAttribute]
	public string Description { get; set; }

	[XmlAttribute]
	public string HexCode
	{
		get
		{
			_hexCode = Code.ToString("X");
			return _hexCode;
		}
		set
		{
			_hexCode = value;
		}
	}

	public override string ToString()
	{
		return HexCode + Name;
	}
}
