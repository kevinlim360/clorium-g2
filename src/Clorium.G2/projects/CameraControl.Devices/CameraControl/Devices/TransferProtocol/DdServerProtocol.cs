using System.IO;
using System.Text;
using CameraControl.Devices.TransferProtocol.DDServer;
using PortableDeviceLib;
using ddserverTest;

namespace CameraControl.Devices.TransferProtocol;

public class DdServerProtocol : ITransferProtocol
{
	private DdClient _client;

	private uint _sessionId;

	private readonly object _syncRoot = new object();

	public string Model { get; private set; }

	public string Manufacturer { get; private set; }

	public string SerialNumber { get; private set; }

	public bool IsConnected { get; set; }

	public string DeviceId { get; private set; }

	public MTPDataResponse ExecuteReadBigData(uint code, Stream stream, StillImageDevice.TransferCallback callback, params uint[] parameters)
	{
		lock (_syncRoot)
		{
			ReconnectIfNeeded();
			MTPDataResponse mTPDataResponse = new MTPDataResponse();
			_client.Write(new CommandBlockContainer((int)code, parameters));
			int num = _client.ReadInt();
			Container container = _client.ReadContainer(callback);
			if (container.Header.Length >= num - 4)
			{
				return new MTPDataResponse
				{
					ErrorCode = (uint)container.Header.Code
				};
			}
			DataBlockContainer dataBlockContainer = (DataBlockContainer)container;
			container = _client.ReadContainer();
			return new MTPDataResponse
			{
				Data = dataBlockContainer.Payload,
				ErrorCode = (uint)dataBlockContainer.Header.Code
			};
		}
	}

	public MTPDataResponse ExecuteReadData(uint code, params uint[] parameters)
	{
		lock (_syncRoot)
		{
			ReconnectIfNeeded();
			MTPDataResponse mTPDataResponse = new MTPDataResponse();
			_client.Write(new CommandBlockContainer((int)code, parameters));
			int num = _client.ReadInt();
			Container container = _client.ReadContainer();
			if (container.Header.Length >= num - 4)
			{
				return new MTPDataResponse
				{
					ErrorCode = (uint)container.Header.Code
				};
			}
			DataBlockContainer dataBlockContainer = (DataBlockContainer)container;
			container = _client.ReadContainer();
			return new MTPDataResponse
			{
				Data = dataBlockContainer.Payload,
				ErrorCode = (uint)dataBlockContainer.Header.Code
			};
		}
	}

	public uint ExecuteWithNoData(uint code, params uint[] parameters)
	{
		lock (_syncRoot)
		{
			ReconnectIfNeeded();
			_client.Write(new CommandBlockContainer((int)code, parameters));
			int num = _client.ReadInt();
			Container container = _client.ReadContainer();
			if (container.Header.Length >= num - 4)
			{
				return (uint)container.Header.Code;
			}
			container = _client.ReadContainer();
			return (uint)container.Header.Code;
		}
	}

	public uint ExecuteWriteData(uint code, byte[] data, params uint[] parameters)
	{
		lock (_syncRoot)
		{
			ReconnectIfNeeded();
			_client.Write(new CommandBlockContainer((int)code, parameters), new DataBlockContainer((int)code, data));
			int num = _client.ReadInt();
			Container container = _client.ReadContainer();
			if (container.Header.Length >= num - 4)
			{
				return (uint)container.Header.Code;
			}
			container = _client.ReadContainer();
			return (uint)container.Header.Code;
		}
	}

	public void Disconnect()
	{
	}

	public DdServerProtocol(DdClient client)
	{
		Init(client);
	}

	public void Init(DdClient client)
	{
		_client = client;
		LoadDeviceInfo();
		OpenSession();
	}

	public void OpenSession()
	{
		_sessionId++;
		ExecuteWithNoData(4098u, _sessionId);
	}

	private void LoadDeviceInfo()
	{
		MTPDataResponse mTPDataResponse = ExecuteReadData(4097u);
		int num = 8;
		int num2 = mTPDataResponse.Data[num];
		num += num2 * 2;
		num += 3;
		int num3 = mTPDataResponse.Data[num];
		num += 2;
		for (int i = 0; i < num3; i++)
		{
			num += 2;
		}
		num += 2;
		int num4 = mTPDataResponse.Data[num];
		num += 2;
		for (int j = 0; j < num4; j++)
		{
			num += 2;
		}
		num += 2;
		int num5 = mTPDataResponse.Data[num];
		num += 2;
		for (int k = 0; k < num5; k++)
		{
			num += 2;
		}
		num += 2;
		int num6 = mTPDataResponse.Data[num];
		num += 2;
		for (int l = 0; l < num6; l++)
		{
			num += 2;
		}
		num += 2;
		int num7 = mTPDataResponse.Data[num];
		num += 2;
		for (int m = 0; m < num7; m++)
		{
			num += 2;
		}
		num += 2;
		int num8 = mTPDataResponse.Data[num] * 2;
		num++;
		Manufacturer = Encoding.Unicode.GetString(mTPDataResponse.Data, num, num8 - 2);
		num += num8;
		int num9 = mTPDataResponse.Data[num] * 2;
		num++;
		Model = Encoding.Unicode.GetString(mTPDataResponse.Data, num, num9 - 2);
		num += num9;
		int num10 = mTPDataResponse.Data[num] * 2;
		num++;
		num += num10;
		int num11 = mTPDataResponse.Data[num] * 2;
		num++;
		SerialNumber = Encoding.Unicode.GetString(mTPDataResponse.Data, num, num11 - 2);
	}

	private void ReconnectIfNeeded()
	{
		if (!_client.IsConnected())
		{
			_client.Reconnect();
			Init(_client);
		}
	}
}
