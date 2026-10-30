using System.Runtime.InteropServices;

namespace CameraControl.Devices.Classes;

public class ErrorCodes
{
	public const uint WIA_ERROR_GENERAL_ERROR = 2149646337u;

	public const uint WIA_ERROR_OFFLINE = 2149646341u;

	public const uint WIA_ERROR_BUSY = 2149646342u;

	public const uint WIA_ERROR_DEVICE_COMMUNICATION = 2149646346u;

	public const uint WIA_ERROR_INVALID_COMMAND = 2149646347u;

	public const uint WIA_ERROR_INCORRECT_HARDWARE_SETTING = 2149646348u;

	public const uint WIA_ERROR_DEVICE_LOCKED = 2149646349u;

	public const uint WIA_ERROR_EXCEPTION_IN_DRIVER = 2149646350u;

	public const uint WIA_ERROR_INVALID_DRIVER_RESPONSE = 2149646351u;

	public const uint WIA_S_NO_DEVICE_AVAILABLE = 2149646357u;

	public const uint WIA_ERROR_UNABLE_TO_FOCUS = 2147500037u;

	public const uint MTP_OK = 8193u;

	public const uint MTP_General_Error = 8194u;

	public const uint MTP_Device_Busy = 8217u;

	public const uint MTP_Set_Property_Not_Support = 40965u;

	public const uint MTP_Store_Not_Available = 8211u;

	public const uint MTP_Out_of_Focus = 40962u;

	public const uint MTP_Shutter_Speed_Bulb = 40968u;

	public const uint MTP_Store_Full = 8204u;

	public const uint MTP_Store_Read_Only = 8206u;

	public const uint MTP_Parameter_Not_Supported = 8198u;

	public const uint MTP_Not_LiveView = 40971u;

	public const uint MTP_Invalid_Parameter = 8221u;

	public const uint MTP_Invalid_Status = 40964u;

	public const uint MTP_Operation_Not_Supported = 8197u;

	public const uint MTP_Access_Denied = 8207u;

	public const uint E_WPD_DEVICE_IS_HUNG = 2150236166u;

	public const uint ERROR_BUSY = 2147942570u;

	public static void GetException(int code)
	{
		GetException((uint)code);
	}

	public static void GetCanonException(uint code)
	{
		if (code != 0 && code != 0)
		{
			uint num = code;
			uint num2 = num;
			throw new DeviceException("Canon error code: " + code.ToString("X"), code);
		}
	}

	public static void GetException(uint code)
	{
		if (code != 0 && code != 8193)
		{
			switch (code)
			{
			case 8217u:
				throw new DeviceException("Device MTP error: Device is busy", code);
			case 8194u:
				throw new DeviceException("General error. No focus ?", code);
			case 8211u:
				throw new DeviceException("The card cannot be accessed", code);
			case 40968u:
				throw new DeviceException("Bulb mode isn't supported", code);
			case 40962u:
				throw new DeviceException("Unable to focus.", code);
			case 8204u:
				throw new DeviceException("Storage is full.", code);
			case 8206u:
				throw new DeviceException("Storage is read only.", code);
			case 40971u:
				throw new DeviceException("Not in live view.", code);
			case 8221u:
				throw new DeviceException("Invalid parameter. Coding error !", code);
			case 8198u:
				throw new DeviceException("Parameter Not Supported. Coding error !", code);
			case 40964u:
				throw new DeviceException("Invalid status.", code);
			case 8207u:
				throw new DeviceException("Access denied.", code);
			case 40965u:
				throw new DeviceException("Set Property Not Support.", code);
			case 2150236166u:
				throw new DeviceException("E_WPD_DEVICE_IS_HUNG", code);
			case 2147942570u:
				throw new DeviceException("MTP device busy", code);
			case 8197u:
				throw new DeviceException("Operation Not Supported", code);
			default:
				throw new DeviceException("Device MTP error code: " + code.ToString("X"), code);
			}
		}
	}

	public static void GetException(COMException exception)
	{
		switch ((uint)exception.ErrorCode)
		{
		case 2149646342u:
			throw new DeviceException("Device is busy. Error code :WIA_ERROR_BUSY", exception, (uint)exception.ErrorCode);
		case 2149646346u:
			throw new DeviceException("Device communication error. Error code :WIA_ERROR_DEVICE_COMMUNICATION", exception, (uint)exception.ErrorCode);
		case 2149646349u:
			throw new DeviceException("Device is locked. Error code :WIA_ERROR_DEVICE_LOCKED", exception, (uint)exception.ErrorCode);
		case 2149646350u:
			throw new DeviceException("Exception in driver. Error code :WIA_ERROR_EXCEPTION_IN_DRIVER", exception, (uint)exception.ErrorCode);
		case 2149646337u:
			throw new DeviceException("General error. Error code :WIA_ERROR_GENERAL_ERROR", exception, (uint)exception.ErrorCode);
		case 2149646348u:
			throw new DeviceException("Incorrect hardware error. Error code :WIA_ERROR_INCORRECT_HARDWARE_SETTING", exception, (uint)exception.ErrorCode);
		case 2149646347u:
			throw new DeviceException("Invalid command. Error code :WIA_ERROR_INVALID_COMMAND", exception, (uint)exception.ErrorCode);
		case 2149646351u:
			throw new DeviceException("Invalid driver response. Error code :WIA_ERROR_INVALID_DRIVER_RESPONSE", exception, (uint)exception.ErrorCode);
		case 2149646341u:
			throw new DeviceException("Device is offline. Error code :WIA_ERROR_OFFLINE", exception, 2149646341u, (uint)exception.ErrorCode);
		case 2147500037u:
			throw new DeviceException("Unable to focus. Error code :WIA_ERROR_UNABLE_TO_FOCUS", exception, 2147500037u, (uint)exception.ErrorCode);
		default:
			throw new DeviceException("Unknown error. Error code:" + (uint)exception.ErrorCode, exception, (uint)exception.ErrorCode);
		}
	}
}
