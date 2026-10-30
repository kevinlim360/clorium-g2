using System;
using System.IO;
using CameraControl.Devices.TransferProtocol.DDServer;

namespace ddserverTest;

public class ResponseBlockContainer : ParameterContainer
{
	public ResponseBlockContainer(int responseCode, params uint[] parameters)
		: base(parameters)
	{
		Header.Code = responseCode;
		Header.ContainerType = ContainerType.ResponseBlock;
	}

	public ResponseBlockContainer(ContainerHeader header, Stream payload)
	{
		if (payload == null)
		{
			throw new ArgumentNullException("payload");
		}
		Header = header;
		readParameters(payload);
	}

	protected void readParameters(Stream stream)
	{
		int num = Header.PayloadLength;
		if (num < 0)
		{
			throw new InvalidContainerException("Invalid container length");
		}
		if (num % 4 != 0)
		{
			throw new InvalidContainerException("Response payload length must by multiplier of 4");
		}
		if (num > 20)
		{
			throw new InvalidContainerException("Response datasets may have at most five parameters");
		}
		int num2 = 0;
		Parameters = new uint[num / 4];
		while (num2 < Parameters.Length)
		{
			Parameters[num2++] = (uint)(stream.ReadByte() | (stream.ReadByte() << 8) | (stream.ReadByte() << 16) | (stream.ReadByte() << 24));
		}
	}
}
