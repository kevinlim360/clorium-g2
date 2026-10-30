using System.Collections.Generic;
using System.IO;

namespace CameraControl.Devices.TransferProtocol.PtpIp;

public class CmdResponse : BaseCmd
{
	public int Code { get; set; }

	public new uint TransactionID { get; set; }

	public uint[] Parameters { get; set; }

	public override void Read(Stream s)
	{
		Code = readByte(s) | (readByte(s) << 8);
		TransactionID = (uint)(readByte(s) | (readByte(s) << 8) | (readByte(s) << 16) | (readByte(s) << 24));
		if (Header.Length > 14)
		{
			List<uint> list = new List<uint>();
			for (int i = 0; i < Header.Length - 3; i++)
			{
				list.Add((uint)(readByte(s) | (readByte(s) << 8) | (readByte(s) << 16) | (readByte(s) << 24)));
			}
			Parameters = list.ToArray();
		}
	}
}
