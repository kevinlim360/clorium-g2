using System.IO;
using PortableDeviceLib;

namespace CameraControl.Devices.TransferProtocol.PtpIp;

public class EndDataPacket : BaseCmd
{
	public byte[] Data { get; set; }

	public void Read(Stream s, StillImageDevice.TransferCallback callback)
	{
		TransactionID = (uint)(readByte(s) | (readByte(s) << 8) | (readByte(s) << 16) | (readByte(s) << 24));
		Data = new byte[Header.Length - 8 - 4];
		int num = 0;
		while (num != Header.Length - 8 - 4)
		{
			num += s.Read(Data, num, (int)(Header.Length - 8 - 4 - num));
			callback?.Invoke(Header.Length, (uint)num);
		}
	}

	public void Read(Stream s, StillImageDevice.TransferCallback callback, Stream outStream)
	{
		TransactionID = (uint)(readByte(s) | (readByte(s) << 8) | (readByte(s) << 16) | (readByte(s) << 24));
		byte[] array = new byte[102400];
		int num = 0;
		while (num != Header.Length - 8 - 4)
		{
			int num2 = s.Read(array, 0, array.Length);
			outStream.Write(array, 0, num2);
			num += num2;
			callback?.Invoke(Header.Length, (uint)num);
		}
	}
}
