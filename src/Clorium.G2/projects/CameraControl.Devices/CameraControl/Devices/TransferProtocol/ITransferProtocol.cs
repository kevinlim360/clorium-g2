using System.IO;
using PortableDeviceLib;

namespace CameraControl.Devices.TransferProtocol;

public interface ITransferProtocol
{
	string Model { get; }

	string Manufacturer { get; }

	string SerialNumber { get; }

	bool IsConnected { get; set; }

	string DeviceId { get; }

	MTPDataResponse ExecuteReadBigData(uint code, Stream stream, StillImageDevice.TransferCallback callback, params uint[] parameters);

	MTPDataResponse ExecuteReadData(uint code, params uint[] parameters);

	uint ExecuteWithNoData(uint code, params uint[] parameters);

	uint ExecuteWriteData(uint code, byte[] data, params uint[] parameters);

	void Disconnect();
}
