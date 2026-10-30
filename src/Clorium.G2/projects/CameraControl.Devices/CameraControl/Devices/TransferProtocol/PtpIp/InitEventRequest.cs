using System.IO;

namespace CameraControl.Devices.TransferProtocol.PtpIp;

public class InitEventRequest : BaseCmd
{
	public int SessionId { get; set; }

	public InitEventRequest(int sessionId)
	{
		Header.Type = 3u;
		Header.Length = 12u;
		SessionId = sessionId;
	}

	public override void Write(Stream s)
	{
		Header.Write(s);
		WriteInt((uint)SessionId, s);
	}
}
