using System.Collections.Generic;
using System.Linq;
using Canon.Eos.Framework;

namespace CameraControl.Devices.Classes;

public class DeviceDescriptorEnumerator
{
	public List<DeviceDescriptor> Devices { get; set; }

	public DeviceDescriptorEnumerator()
	{
		Devices = new List<DeviceDescriptor>();
	}

	public DeviceDescriptor GetBySerialNumber(string serial)
	{
		return Devices.FirstOrDefault((DeviceDescriptor deviceDescriptor) => deviceDescriptor.SerialNumber == serial);
	}

	public DeviceDescriptor GetByWiaId(string id)
	{
		return Devices.FirstOrDefault((DeviceDescriptor deviceDescriptor) => deviceDescriptor.WiaId == id);
	}

	public DeviceDescriptor GetByWpdId(string id)
	{
		return Devices.FirstOrDefault((DeviceDescriptor deviceDescriptor) => deviceDescriptor.WpdId == id);
	}

	public DeviceDescriptor GetByEosCamera(EosCamera id)
	{
		return Devices.FirstOrDefault((DeviceDescriptor deviceDescriptor) => deviceDescriptor.EosCamera == id);
	}

	public void RemoveDisconnected()
	{
		List<DeviceDescriptor> list = Devices.Where((DeviceDescriptor deviceDescriptor) => !deviceDescriptor.CameraDevice.IsConnected).ToList();
		foreach (DeviceDescriptor item in list)
		{
			Devices.Remove(item);
		}
	}

	public void Add(DeviceDescriptor descriptor)
	{
		Devices.Add(descriptor);
	}

	public void Remove(DeviceDescriptor descriptor)
	{
		Devices.Remove(descriptor);
	}
}
