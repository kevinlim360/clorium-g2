using System;
using System.IO;

namespace CameraControl.Devices.TransferProtocol.DDServer;

public class ContainerHeader
{
	public const int HeaderLength = 12;

	public int Code;

	public ContainerType ContainerType = ContainerType.Undefined;

	public int Length;

	public int TransactionID;

	public int PayloadLength
	{
		get
		{
			return Length - 12;
		}
		set
		{
			Length = 12 + value;
		}
	}

	public ContainerHeader()
	{
	}

	public ContainerHeader(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		Length = readByte(stream) | (readByte(stream) << 8) | (readByte(stream) << 16) | (readByte(stream) << 24);
		ContainerType = (ContainerType)readByte(stream);
		readByte(stream);
		Code = readByte(stream) | (readByte(stream) << 8);
		TransactionID = readByte(stream) | (readByte(stream) << 8) | (readByte(stream) << 16) | (readByte(stream) << 24);
	}

	private static int readByte(Stream s)
	{
		int num = s.ReadByte();
		if (num == -1)
		{
			throw new InvalidContainerException("Unexpected end of stream");
		}
		return num;
	}

	public void Write(Stream s)
	{
		s.WriteByte((byte)(0xFF & Length));
		s.WriteByte((byte)(0xFF & (Length >> 8)));
		s.WriteByte((byte)(0xFF & (Length >> 16)));
		s.WriteByte((byte)(0xFF & (Length >> 24)));
		s.WriteByte((byte)((ContainerType)0xFF & ContainerType));
		s.WriteByte(0);
		s.WriteByte((byte)(0xFF & Code));
		s.WriteByte((byte)(0xFF & (Code >> 8)));
		s.WriteByte((byte)(0xFF & TransactionID));
		s.WriteByte((byte)(0xFF & (TransactionID >> 8)));
		s.WriteByte((byte)(0xFF & (TransactionID >> 16)));
		s.WriteByte((byte)(0xFF & (TransactionID >> 24)));
	}
}
