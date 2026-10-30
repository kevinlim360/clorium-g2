using System.IO;
using System.Text;
using CameraControl.Devices.TransferProtocol.PtpIp;
using PortableDeviceLib;

namespace CameraControl.Devices.TransferProtocol;

public class PtpIpProtocol : ITransferProtocol
{
	private object _locker = new object();

	private PtpIpClient _client;

	public static uint TransactionId { get; set; }

	public string Model { get; private set; }

	public string Manufacturer { get; private set; }

	public string SerialNumber { get; private set; }

	public bool IsConnected { get; set; }

	public string DeviceId { get; private set; }

	public PtpIpProtocol(PtpIpClient client)
	{
		TransactionId = 1u;
		_client = client;
		LoadDeviceInfo();
		IsConnected = true;
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
		if (Model.Contains("\0"))
		{
			Model = Model.Substring(0, Model.IndexOf('\0'));
		}
		num += num9;
		int num10 = mTPDataResponse.Data[num] * 2;
		num++;
		num += num10;
		int num11 = mTPDataResponse.Data[num] * 2;
		num++;
		SerialNumber = Encoding.Unicode.GetString(mTPDataResponse.Data, num, num11 - 2);
	}

	public MTPDataResponse ExecuteReadBigData(uint code, Stream stream, StillImageDevice.TransferCallback callback, params uint[] parameters)
	{
		lock (_locker)
		{
			CmdRequest container = new CmdRequest(code)
			{
				Parameters = parameters
			};
			_client.Write(container);
			IPtpIpCommand ptpIpCommand = _client.Read();
			if (ptpIpCommand is CmdResponse cmdResponse)
			{
				return new MTPDataResponse
				{
					ErrorCode = (uint)cmdResponse.Code
				};
			}
			StartDataPacket startDataPacket = ptpIpCommand as StartDataPacket;
			IPtpIpCommand ptpIpCommand2 = _client.Read(callback, stream);
			EndDataPacket endDataPacket = (EndDataPacket)ptpIpCommand2;
			IPtpIpCommand ptpIpCommand3 = _client.Read();
			return new MTPDataResponse
			{
				Data = endDataPacket.Data
			};
		}
	}

	public MTPDataResponse ExecuteReadData(uint code, params uint[] parameters)
	{
		lock (_locker)
		{
			CmdRequest container = new CmdRequest(code)
			{
				Parameters = parameters
			};
			_client.Write(container);
			IPtpIpCommand ptpIpCommand = _client.Read();
			if (ptpIpCommand is CmdResponse cmdResponse)
			{
				return new MTPDataResponse
				{
					ErrorCode = (uint)cmdResponse.Code
				};
			}
			StartDataPacket startDataPacket = ptpIpCommand as StartDataPacket;
			IPtpIpCommand ptpIpCommand2 = _client.Read();
			EndDataPacket endDataPacket = (EndDataPacket)ptpIpCommand2;
			IPtpIpCommand ptpIpCommand3 = _client.Read();
			return new MTPDataResponse
			{
				Data = endDataPacket.Data
			};
		}
	}

	public uint ExecuteWithNoData(uint code, params uint[] parameters)
	{
		lock (_locker)
		{
			_client.Write(new CmdRequest(code)
			{
				Parameters = parameters
			});
			IPtpIpCommand ptpIpCommand = _client.Read();
			if (ptpIpCommand is CmdResponse cmdResponse)
			{
				return (uint)cmdResponse.Code;
			}
			return 0u;
		}
	}

	public uint ExecuteWriteData(uint code, byte[] data, params uint[] parameters)
	{
		lock (_locker)
		{
			CmdRequest cmdRequest = new CmdRequest(code, 2u)
			{
				Parameters = parameters
			};
			_client.Write(cmdRequest);
			_client.Write(new StartDataPacket
			{
				Data = data,
				TransactionID = cmdRequest.TransactionID
			});
			IPtpIpCommand ptpIpCommand = _client.Read();
			if (ptpIpCommand is CmdResponse cmdResponse)
			{
				return (uint)cmdResponse.Code;
			}
		}
		return 0u;
	}

	public void Disconnect()
	{
	}
}
