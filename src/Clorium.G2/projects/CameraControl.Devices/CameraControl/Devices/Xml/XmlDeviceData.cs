using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace CameraControl.Devices.Xml;

public class XmlDeviceData
{
	public List<XmlCommandDescriptor> AvaiableCommands { get; set; }

	public List<XmlEventDescriptor> AvaiableEvents { get; set; }

	public List<XmlPropertyDescriptor> AvaiableProperties { get; set; }

	public string Model { get; set; }

	public string Manufacturer { get; set; }

	public XmlDeviceData()
	{
		AvaiableCommands = new List<XmlCommandDescriptor>();
		AvaiableEvents = new List<XmlEventDescriptor>();
		AvaiableProperties = new List<XmlPropertyDescriptor>();
	}

	public static XmlDeviceData Load(string filename)
	{
		XmlDeviceData result = new XmlDeviceData();
		if (File.Exists(filename))
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(XmlDeviceData));
			FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
			result = (XmlDeviceData)xmlSerializer.Deserialize(fileStream);
			fileStream.Close();
		}
		return result;
	}

	public string GetCommandName(uint code)
	{
		foreach (XmlCommandDescriptor avaiableCommand in AvaiableCommands)
		{
			if (avaiableCommand.Code == code)
			{
				return avaiableCommand.Name;
			}
		}
		return "";
	}

	public string GetEventName(uint code)
	{
		foreach (XmlEventDescriptor avaiableEvent in AvaiableEvents)
		{
			if (avaiableEvent.Code == code)
			{
				return avaiableEvent.Name;
			}
		}
		return "";
	}

	public string GetPropName(uint code)
	{
		foreach (XmlPropertyDescriptor avaiableProperty in AvaiableProperties)
		{
			if (avaiableProperty.Code == code)
			{
				return avaiableProperty.Name;
			}
		}
		return "";
	}

	public bool PropertyExist(uint code)
	{
		return AvaiableProperties.Any((XmlPropertyDescriptor xmlPropertyDescriptor) => xmlPropertyDescriptor.Code == code);
	}
}
