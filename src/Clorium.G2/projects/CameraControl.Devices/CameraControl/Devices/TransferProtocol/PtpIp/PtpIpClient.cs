using System;
using System.IO;
using System.Net.Sockets;
using PortableDeviceLib;

namespace CameraControl.Devices.TransferProtocol.PtpIp;

public class PtpIpClient
{
	private Stream _inerStream;

	private TcpClient _client;

	private Stream _eventinerStream;

	private TcpClient _eventclient;

	private object _commandLock = new object();

	public bool Open(string ip, int port)
	{
		try
		{
			PtpIpHeader ptpIpHeader = new PtpIpHeader();
			_client = new TcpClient();
			_client.Connect(ip, port);
			_client.ReceiveTimeout = 300000;
			_client.SendTimeout = 300000;
			_inerStream = _client.GetStream();
			Write(new InitCommandRequest());
			InitCommandAck initCommandAck = (InitCommandAck)Read();
			_eventclient = new TcpClient();
			_eventclient.Connect(ip, port);
			_eventinerStream = _eventclient.GetStream();
			WriteEvent(new InitEventRequest(initCommandAck.SessionId));
			ptpIpHeader.Read(_eventinerStream);
			ptpIpHeader.Length = 8u;
			ptpIpHeader.Type = 13u;
			WriteEvent(ptpIpHeader);
			ptpIpHeader.Read(_eventinerStream);
		}
		catch (Exception message)
		{
			Log.Error(message);
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

	public void Write(IPtpIpCommand container)
	{
		using MemoryStream memoryStream = new MemoryStream();
		container.Write(memoryStream);
		_inerStream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
		_inerStream.Flush();
	}

	public IPtpIpCommand Read(StillImageDevice.TransferCallback callback = null, Stream stream = null)
	{
		PtpIpHeader ptpIpHeader = new PtpIpHeader();
		ptpIpHeader.Read(_inerStream);
		switch ((PtpIpContainerType)ptpIpHeader.Type)
		{
		case PtpIpContainerType.Init_Command_Ack:
		{
			InitCommandAck initCommandAck = new InitCommandAck
			{
				Header = ptpIpHeader
			};
			initCommandAck.Read(_inerStream);
			return initCommandAck;
		}
		case PtpIpContainerType.Cmd_Response:
		{
			CmdResponse cmdResponse = new CmdResponse
			{
				Header = ptpIpHeader
			};
			cmdResponse.Read(_inerStream);
			return cmdResponse;
		}
		case PtpIpContainerType.Start_Data_Packet:
		{
			StartDataPacket startDataPacket = new StartDataPacket
			{
				Header = ptpIpHeader
			};
			startDataPacket.Read(_inerStream);
			return startDataPacket;
		}
		case PtpIpContainerType.Data_Packet:
		{
			DataPacket dataPacket = new DataPacket
			{
				Header = ptpIpHeader
			};
			dataPacket.Read(_inerStream);
			return dataPacket;
		}
		case PtpIpContainerType.End_Data_Packet:
		{
			EndDataPacket endDataPacket = new EndDataPacket
			{
				Header = ptpIpHeader
			};
			if (stream != null)
			{
				endDataPacket.Read(_inerStream, callback, stream);
			}
			else
			{
				endDataPacket.Read(_inerStream, callback);
			}
			return endDataPacket;
		}
		default:
			throw new ArgumentOutOfRangeException();
		case PtpIpContainerType.Init_Command_Request:
		case PtpIpContainerType.Init_Event_Request:
		case PtpIpContainerType.Init_Event_Ack:
		case PtpIpContainerType.Init_Fail:
		case PtpIpContainerType.Cmd_Request:
		case PtpIpContainerType.Event:
		case PtpIpContainerType.Cancel_Transaction:
		case PtpIpContainerType.Ping:
		case PtpIpContainerType.Pong:
			return null;
		}
	}

	public void Write(PtpIpHeader header)
	{
		using MemoryStream memoryStream = new MemoryStream();
		header.Write(memoryStream);
		_inerStream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
		_inerStream.Flush();
	}

	public void WriteEvent(IPtpIpCommand container)
	{
		using MemoryStream memoryStream = new MemoryStream();
		container.Write(memoryStream);
		_eventinerStream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
		if (container.Header.Length != memoryStream.Length)
		{
			throw new Exception("Wrong length");
		}
	}

	public void WriteEvent(PtpIpHeader container)
	{
		using MemoryStream memoryStream = new MemoryStream();
		container.Write(memoryStream);
		_eventinerStream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
	}
}
