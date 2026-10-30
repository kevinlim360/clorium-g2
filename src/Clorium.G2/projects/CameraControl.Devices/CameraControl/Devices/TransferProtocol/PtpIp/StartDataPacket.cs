using System.IO;

namespace CameraControl.Devices.TransferProtocol.PtpIp;

public class StartDataPacket : BaseCmd
{
	public int Size { get; set; }

	public byte[] Data { get; set; }

	public override void Read(Stream s)
	{
		TransactionID = (uint)(readByte(s) | (readByte(s) << 8) | (readByte(s) << 16) | (readByte(s) << 24));
		Size = readByte(s) | (readByte(s) << 8) | (readByte(s) << 16) | (readByte(s) << 24);
		int num = readByte(s) | (readByte(s) << 8) | (readByte(s) << 16) | (readByte(s) << 24);
	}

	public override void Write(Stream s)
	{
		Header.Type = 9u;
		Header.Length = 20u;
		Header.Write(s);
		WriteInt(TransactionID, s);
		WriteInt((uint)Data.Length, s);
		WriteInt(0u, s);
		WriteInt((uint)(Data.Length + 12), s);
		WriteInt(12u, s);
		WriteInt(TransactionID, s);
		s.Write(Data, 0, Data.Length);
	}
}
