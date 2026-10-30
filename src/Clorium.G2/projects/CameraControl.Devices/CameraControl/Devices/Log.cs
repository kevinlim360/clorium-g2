using System;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices;

public static class Log
{
	public static bool IsVerbose { get; set; }

	public static event LogEventHandler LogDebug;

	public static event LogEventHandler LogError;

	public static event LogEventHandler LogInfo;

	public static void Debug(object message, Exception exception)
	{
		if (LogDebug != null)
		{
			LogDebug(new LogEventArgs
			{
				Exception = exception,
				Message = message
			});
		}
	}

	public static void Debug(object message, bool show = true)
	{
		if (show)
		{
			Debug(message, null);
		}
	}

	public static void Error(object message, Exception exception)
	{
		if (LogError != null)
		{
			LogError(new LogEventArgs
			{
				Exception = exception,
				Message = message
			});
		}
	}

	public static void Error(object message)
	{
		Error(message, null);
	}

	public static void Info(object message, Exception exception)
	{
		if (LogInfo != null)
		{
			LogInfo(new LogEventArgs
			{
				Exception = exception,
				Message = message
			});
		}
	}

	public static void Info(object message)
	{
		LogInfo?.Invoke(new LogEventArgs
		{
			Exception = null,
			Message = message
		});
	}

	public static void InfoWithWriteLine(object message)
	{
		Console.WriteLine(message);
		LogInfo?.Invoke(new LogEventArgs
		{
			Exception = null,
			Message = message
		});
	}

	public static void Verbose(object message)
	{
		if (IsVerbose)
		{
			LogInfo?.Invoke(new LogEventArgs
			{
				Exception = null,
				Message = message
			});
		}
	}

	public static void VerboseWithWriteLine(object message)
	{
		if (IsVerbose)
		{
			Console.WriteLine(message);
			LogInfo?.Invoke(new LogEventArgs
			{
				Exception = null,
				Message = message
			});
		}
	}

	public static void VerboseWithWriteLineAlways(object message)
	{
		Console.WriteLine(message);
		if (IsVerbose)
		{
			LogInfo?.Invoke(new LogEventArgs
			{
				Exception = null,
				Message = message
			});
		}
	}

	public static void VerboseWithWrite(object message)
	{
		if (IsVerbose)
		{
			Console.Write(message);
			LogInfo?.Invoke(new LogEventArgs
			{
				Exception = null,
				Message = message
			});
		}
	}

	public static void VerboseWithWriteAlways(object message)
	{
		Console.Write(message);
		if (IsVerbose)
		{
			LogInfo?.Invoke(new LogEventArgs
			{
				Exception = null,
				Message = message
			});
		}
	}
}
