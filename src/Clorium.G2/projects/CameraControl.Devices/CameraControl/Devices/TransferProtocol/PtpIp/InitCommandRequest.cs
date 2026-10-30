using System;
using System.IO;
using System.Text;

namespace CameraControl.Devices.TransferProtocol.PtpIp;

public class InitCommandRequest : BaseCmd
{
	public Guid Guid { get; set; }

	public string Name { get; set; }

	public override void Write(Stream s)
	{
		Guid = Guid.NewGuid();
		Name = "DCC V1.00";
		Header = new PtpIpHeader();
		Header.Type = 1u;
		Header.Length = (uint)(24 + (Name.Length + 1) * 2 + 4);
		Header.Write(s);
		s.Write(Guid.ToByteArray(), 0, 16);
		s.Write(Encoding.Unicode.GetBytes(Name), 0, Name.Length * 2);
		s.WriteByte(0);
		s.WriteByte(0);
		s.WriteByte(0);
		s.WriteByte(0);
		s.WriteByte(1);
		s.WriteByte(0);
	}
}
