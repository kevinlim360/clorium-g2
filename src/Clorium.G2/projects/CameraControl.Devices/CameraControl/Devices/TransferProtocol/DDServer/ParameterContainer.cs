using System.IO;

namespace CameraControl.Devices.TransferProtocol.DDServer;

public class ParameterContainer : Container
{
	public uint[] Parameters;

	protected int payloadLength => (Parameters != null) ? (Parameters.Length * 4) : 0;

	public ParameterContainer()
	{
	}

	public ParameterContainer(uint[] parameters)
	{
		Header = new ContainerHeader();
		Parameters = parameters;
		Header.PayloadLength = payloadLength;
	}

	public override void WritePayload(Stream s)
	{
		if (payloadLength > 0)
		{
			for (int i = 0; i < Parameters.Length; i++)
			{
				s.WriteByte((byte)(0xFF & Parameters[i]));
				s.WriteByte((byte)(0xFF & (Parameters[i] >> 8)));
				s.WriteByte((byte)(0xFF & (Parameters[i] >> 16)));
				s.WriteByte((byte)(0xFF & (Parameters[i] >> 24)));
			}
		}
	}
}
