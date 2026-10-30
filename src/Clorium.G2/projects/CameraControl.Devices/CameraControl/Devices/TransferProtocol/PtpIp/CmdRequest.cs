using System.IO;

namespace CameraControl.Devices.TransferProtocol.PtpIp;

public class CmdRequest : BaseCmd
{
	public uint Code { get; set; }

	public new uint TransactionID { get; set; }

	public uint[] Parameters { get; set; }

	public uint DataType { get; set; }

	public CmdRequest(uint code, uint datatype = 1u)
	{
		TransactionID = PtpIpProtocol.TransactionId++;
		Header = new PtpIpHeader();
		Header.Type = 6u;
		Code = code;
		DataType = datatype;
	}

	public override void Write(Stream s)
	{
		Header.Length = (uint)(18 + ((Parameters != null) ? (Parameters.Length * 4) : 0));
		Header.Write(s);
		WriteInt(DataType, s);
		s.WriteByte((byte)(0xFF & Code));
		s.WriteByte((byte)(0xFF & (Code >> 8)));
		WriteInt(TransactionID, s);
		if (Parameters != null)
		{
			for (int i = 0; i < Parameters.Length; i++)
			{
				WriteInt(Parameters[i], s);
			}
		}
	}

	public override void Read(Stream s)
	{
		Header.Read(s);
		Code = (uint)(readByte(s) | (readByte(s) << 8));
		TransactionID = (uint)(readByte(s) | (readByte(s) << 8) | (readByte(s) << 16) | (readByte(s) << 24));
		byte[] buffer = new byte[Header.Length];
		s.Read(buffer, 0, (int)(Header.Length - 14));
	}
}
