using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using PortableDeviceLib;
using ddserverTest;

namespace CameraControl.Devices.TransferProtocol.DDServer;

public class DdClient
{
	private readonly object _commandLock = new object();

	private TcpClient _client;

	private Stream _inerStream;

	private DdServerDevice _lastdevice;

	private string _ip;

	private int _port;

	public bool Open(string ip, int port)
	{
		try
		{
			_ip = ip;
			_port = port;
			_client = new TcpClient(ip, port);
			_inerStream = _client.GetStream();
			string text = Encoding.ASCII.GetString(ReadPacket());
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public int ReadInt()
	{
		return _inerStream.ReadByte() | (_inerStream.ReadByte() << 8) | (_inerStream.ReadByte() << 16) | (_inerStream.ReadByte() << 24);
	}

	private byte[] ReadPacket()
	{
		int num = ReadInt();
		byte[] array = new byte[num - 4];
		_inerStream.Read(array, 0, num - 4);
		return array;
	}

	public void Write(Container container)
	{
		using MemoryStream memoryStream = new MemoryStream();
		int num = container.Header.Length + 4;
		memoryStream.WriteByte((byte)(0xFF & num));
		memoryStream.WriteByte((byte)(0xFF & (num >> 8)));
		memoryStream.WriteByte((byte)(0xFF & (num >> 16)));
		memoryStream.WriteByte((byte)(0xFF & (num >> 24)));
		container.Write(memoryStream);
		_inerStream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
	}

	public void Write(Container container1, Container container2)
	{
		using MemoryStream memoryStream = new MemoryStream();
		int num = container1.Header.Length + container2.Header.Length + 4;
		memoryStream.WriteByte((byte)(0xFF & num));
		memoryStream.WriteByte((byte)(0xFF & (num >> 8)));
		memoryStream.WriteByte((byte)(0xFF & (num >> 16)));
		memoryStream.WriteByte((byte)(0xFF & (num >> 24)));
		container1.Write(memoryStream);
		container2.Write(memoryStream);
		_inerStream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
		_inerStream.Flush();
	}

	public virtual Container ReadContainer(StillImageDevice.TransferCallback callback = null)
	{
		return getContainer(synchronized: false, callback);
	}

	protected Container getContainer(bool synchronized, StillImageDevice.TransferCallback callback)
	{
		ContainerHeader containerHeader = new ContainerHeader(_inerStream);
		switch (containerHeader.ContainerType)
		{
		case ContainerType.DataBlock:
			return new DataBlockContainer(containerHeader, _inerStream, callback);
		case ContainerType.ResponseBlock:
			if (synchronized)
			{
				Monitor.Exit(_commandLock);
			}
			return new ResponseBlockContainer(containerHeader, _inerStream);
		case ContainerType.CommandBlock:
		case ContainerType.EventBlock:
			throw new Exception("Invalid container type. " + containerHeader.ContainerType);
		default:
			throw new Exception("Unkown container type");
		}
	}

	public List<DdServerDevice> GetDevices()
	{
		List<DdServerDevice> list = new List<DdServerDevice>();
		Write(new CommandBlockContainer(2));
		byte[] array = new byte[6];
		_inerStream.Read(array, 0, 4);
		int num = array[3];
		Container container = ReadContainer();
		if (container is DataBlockContainer dataBlockContainer)
		{
			int num2 = 0;
			int num3 = dataBlockContainer.Payload[num2] | (dataBlockContainer.Payload[num2 + 1] << 8);
			for (int i = 0; i < num3; i++)
			{
				list.Add(new DdServerDevice(dataBlockContainer.Payload, num2 + 2));
			}
			Container container2 = ReadContainer();
			_inerStream.Read(array, 0, 4);
		}
		return list;
	}

	public bool Connect(DdServerDevice device)
	{
		_lastdevice = device;
		Write(new CommandBlockContainer(1, (uint)device.VendorId, (uint)device.ProductId));
		int num = ReadInt();
		Container container = ReadContainer();
		Container container2 = ReadContainer();
		return true;
	}

	public void Reconnect()
	{
		Open(_ip, _port);
		Connect(_lastdevice);
	}

	public bool IsConnected()
	{
		return _client.Connected;
	}
}
