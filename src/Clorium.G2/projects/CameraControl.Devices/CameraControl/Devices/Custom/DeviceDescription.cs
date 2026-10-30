using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace CameraControl.Devices.Custom;

public class DeviceDescription
{
	[XmlAttribute]
	public string Model { get; set; }

	[XmlAttribute]
	public string BaseDevice { get; set; }

	[XmlAttribute]
	public string Manufacturer { get; set; }

	public string Description { get; set; }

	public List<DeviceProperty> Properties { get; set; }

	public DeviceDescription()
	{
		Properties = new List<DeviceProperty>();
	}

	public void Save(string file)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(DeviceDescription));
		Stream stream = new FileStream(file, FileMode.Create);
		xmlSerializer.Serialize(stream, this);
		stream.Close();
	}

	public static DeviceDescription Load(string file)
	{
		try
		{
			DeviceDescription deviceDescription = new DeviceDescription();
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(DeviceDescription));
			FileStream fileStream = new FileStream(file, FileMode.Open);
			deviceDescription = (DeviceDescription)xmlSerializer.Deserialize(fileStream);
			fileStream.Close();
			return deviceDescription;
		}
		catch (Exception exception)
		{
			Log.Error("Error load device description " + file, exception);
			return null;
		}
	}
}
