namespace CameraControl.Devices;

// UiText (user message wording) logs a broken Data\Messages.xml through the device log; the test prints it instead.
internal static class Log
{
	public static void Error(string message, System.Exception ex) => System.Console.Error.WriteLine(message + " " + ex.Message);
}
