using System.IO;

namespace CameraControl.Devices.Others;

public class FileTransferDevice : AppFakeCameraDevice
{
	public FileTransferDevice()
	{
		DeviceName = "FileTransfer";
	}

	public override void TransferFile(object o, string filename)
	{
		File.WriteAllBytes(filename, (byte[])o);
	}
}
