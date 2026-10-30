using System.Xml.Serialization;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Xml;

public class XmlCommandDescriptor : BaseFieldClass
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

	[XmlIgnore]
	public string DisplayName => HexCode + " - " + Name;

	public override string ToString()
	{
		return HexCode + Name;
	}
}
