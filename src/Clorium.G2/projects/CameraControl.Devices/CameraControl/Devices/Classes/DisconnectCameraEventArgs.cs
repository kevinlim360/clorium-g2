using System;
using CameraControl.Devices.TransferProtocol;
using Canon.Eos.Framework;

namespace CameraControl.Devices.Classes;

public class DisconnectCameraEventArgs : EventArgs
{
	public string WiaId { get; set; }

	public ITransferProtocol StillImageDevice { get; set; }

	public EosCamera EosCamera { get; set; }
}
