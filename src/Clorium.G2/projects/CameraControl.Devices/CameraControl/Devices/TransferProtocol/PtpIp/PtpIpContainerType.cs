namespace CameraControl.Devices.TransferProtocol.PtpIp;

public enum PtpIpContainerType
{
	Init_Command_Request = 1,
	Init_Command_Ack,
	Init_Event_Request,
	Init_Event_Ack,
	Init_Fail,
	Cmd_Request,
	Cmd_Response,
	Event,
	Start_Data_Packet,
	Data_Packet,
	Cancel_Transaction,
	End_Data_Packet,
	Ping,
	Pong
}
