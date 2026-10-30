using System.IO;
using PortableDeviceLib;

namespace CameraControl.Devices.TransferProtocol.DDServer;

public class DataBlockContainer : Container
{
	public byte[] Payload;

	public DataBlockContainer(ContainerHeader header, Stream payload, StillImageDevice.TransferCallback callback)
	{
		Header = header;
		Payload = new byte[Header.PayloadLength];
		int num = 0;
		while (num != Header.PayloadLength)
		{
			num += payload.Read(Payload, num, Header.PayloadLength - num);
			callback?.Invoke((uint)Header.PayloadLength, (uint)num);
		}
	}

	public DataBlockContainer(int commandCode, byte[] data)
	{
		Header = new ContainerHeader();
		Header.Code = commandCode;
		Header.ContainerType = ContainerType.DataBlock;
		Header.PayloadLength = data.Length;
		Payload = data;
	}

	public override void WritePayload(Stream s)
	{
		s.Write(Payload, 0, Payload.Length);
	}
}
