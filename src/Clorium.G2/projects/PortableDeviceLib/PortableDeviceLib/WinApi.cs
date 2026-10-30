using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PortableDeviceLib;

public class WinApi
{
	[Flags]
	public enum DesiredAccess : uint
	{
		GENERIC_READ = 0x80000000u,
		GENERIC_WRITE = 0x40000000u
	}

	[Flags]
	public enum ShareMode : uint
	{
		FILE_SHARE_NONE = 0u,
		FILE_SHARE_READ = 1u,
		FILE_SHARE_WRITE = 2u,
		FILE_SHARE_DELETE = 4u
	}

	public enum CreationDisposition : uint
	{
		CREATE_NEW = 1u,
		CREATE_ALWAYS,
		OPEN_EXISTING,
		OPEN_ALWAYS,
		TRUNCATE_EXSTING
	}

	[Flags]
	public enum FlagsAndAttributes : uint
	{
		FILE_ATTRIBUTES_ARCHIVE = 0x20u,
		FILE_ATTRIBUTE_HIDDEN = 2u,
		FILE_ATTRIBUTE_NORMAL = 0x80u,
		FILE_ATTRIBUTE_OFFLINE = 0x1000u,
		FILE_ATTRIBUTE_READONLY = 1u,
		FILE_ATTRIBUTE_SYSTEM = 4u,
		FILE_ATTRIBUTE_TEMPORARY = 0x100u,
		FILE_FLAG_WRITE_THROUGH = 0x80000000u,
		FILE_FLAG_OVERLAPPED = 0x40000000u,
		FILE_FLAG_NO_BUFFERING = 0x20000000u,
		FILE_FLAG_RANDOM_ACCESS = 0x10000000u,
		FILE_FLAG_SEQUENTIAL_SCAN = 0x8000000u,
		FILE_FLAG_DELETE_ON = 0x4000000u,
		FILE_FLAG_POSIX_SEMANTICS = 0x1000000u,
		FILE_FLAG_OPEN_REPARSE_POINT = 0x200000u,
		FILE_FLAG_OPEN_NO_CALL = 0x100000u
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool WriteFile(IntPtr handle, IntPtr buffer, int numBytesToWrite, IntPtr numBytesWritten, [In] ref NativeOverlapped lpOverlapped);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool CloseHandle(IntPtr hObject);
}
