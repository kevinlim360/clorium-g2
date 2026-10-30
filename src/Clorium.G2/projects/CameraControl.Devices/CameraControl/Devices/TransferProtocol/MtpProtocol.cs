using System.IO;
using PortableDeviceLib;

namespace CameraControl.Devices.TransferProtocol;

public class MtpProtocol : StillImageDevice, ITransferProtocol
{
	public MtpProtocol(string deviceId)
		: base(deviceId)
	{
	}

	MTPDataResponse ITransferProtocol.ExecuteReadBigData(uint code, Stream stream, TransferCallback callback, uint[] parameters)
	{
		return ExecuteReadBigData(code, stream, callback, parameters);
	}

	MTPDataResponse ITransferProtocol.ExecuteReadData(uint code, uint[] parameters)
	{
		return ExecuteReadData(code, parameters);
	}

	uint ITransferProtocol.ExecuteWithNoData(uint code, uint[] parameters)
	{
		return ExecuteWithNoData(code, parameters);
	}

	uint ITransferProtocol.ExecuteWriteData(uint code, byte[] data, uint[] parameters)
	{
		return ExecuteWriteData(code, data, parameters);
	}

	void ITransferProtocol.Disconnect()
	{
		Disconnect();
	}
}
