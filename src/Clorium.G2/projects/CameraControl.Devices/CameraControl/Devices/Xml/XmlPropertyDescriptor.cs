using System.Collections.Generic;
using System.Xml.Serialization;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Xml;

public class XmlPropertyDescriptor : BaseFieldClass
{
	private string _name;

	private string _hexCode;

	[XmlAttribute]
	public uint Code { get; set; }

	[XmlAttribute]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			_name = value;
			NotifyPropertyChanged("Name");
			NotifyPropertyChanged("DisplayName");
		}
	}

	[XmlIgnore]
	public string DisplayName => HexCode + " - " + Name;

	[XmlAttribute]
	public string Description { get; set; }

	[XmlAttribute]
	public uint DataType { get; set; }

	[XmlAttribute]
	public uint DataForm { get; set; }

	[XmlAttribute]
	public long MinVal { get; set; }

	[XmlAttribute]
	public long MaxVal { get; set; }

	[XmlAttribute]
	public long Inc { get; set; }

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

	public List<XmlPropertyValue> Values { get; set; }

	public XmlPropertyDescriptor()
	{
		Values = new List<XmlPropertyValue>();
	}

	public override string ToString()
	{
		return HexCode + Name;
	}
}
