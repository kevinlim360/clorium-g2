using System.Xml.Serialization;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Xml;

public class XmlPropertyValue : BaseFieldClass
{
	private string _name;

	private string _hexCode;

	[XmlAttribute]
	public long Value { get; set; }

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
	public string HexValue
	{
		get
		{
			_hexCode = Value.ToString("X");
			return _hexCode;
		}
		set
		{
			_hexCode = value;
		}
	}

	[XmlIgnore]
	public string DisplayName => Value + " - " + HexValue + " - " + Name;

	public override string ToString()
	{
		return HexValue + Name;
	}
}
