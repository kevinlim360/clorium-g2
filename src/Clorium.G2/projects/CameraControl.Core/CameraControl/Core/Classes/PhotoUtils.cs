using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Reflection;
using System.Threading;
using CameraControl.Devices;

namespace CameraControl.Core.Classes;

public class PhotoUtils
{
	public static bool RunAndWait(string exe, string param)
	{
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(exe);
			startInfo.WindowStyle = ProcessWindowStyle.Minimized;
			startInfo.Arguments = param;
			Process process = Process.Start(startInfo);
			process.WaitForExit();
		}
		catch (Exception)
		{
			return false;
		}
		return true;
	}

	public static bool Run(string exe)
	{
		return Run(exe, "", ProcessWindowStyle.Minimized) != null;
	}

	public static bool Run(string exe, string param)
	{
		return Run(exe, param, ProcessWindowStyle.Minimized) != null;
	}

	public static Process Run(string exe, string param, ProcessWindowStyle processWindowStyle)
	{
		Process process;
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(exe);
			startInfo.WindowStyle = processWindowStyle;
			if (!string.IsNullOrEmpty(param))
			{
				startInfo.Arguments = param;
			}
			process = Process.Start(startInfo);
		}
		catch (Exception message)
		{
			Log.Error(message);
			return null;
		}
		return process;
	}

	[Obsolete("Not used anymore", false)]
	public static void CopyPhotoScale(string sourse, string dest, double scale)
	{
		File.Copy(sourse, dest, overwrite: true);
	}

	public static void PlayCaptureSound()
	{
		try
		{
			string basedir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			if (basedir != null)
			{
				SoundPlayer mplayer = new SoundPlayer(Path.Combine(basedir, "Data", "takephoto.wav"));
				mplayer.Play();
			}
		}
		catch (Exception message)
		{
			Log.Debug(message);
		}
	}

	public static string GetFullPath(string path)
	{
		if (path.Contains(":\\"))
		{
			return path;
		}
		return Path.Combine(Settings.ApplicationFolder, path);
	}

	public static void Donate()
	{
		Run("http://www.digicamcontrol.com/donate/");
	}

	public static bool IsFileLocked(string file)
	{
		FileStream stream = null;
		try
		{
			stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
		}
		catch (IOException)
		{
			return true;
		}
		finally
		{
			stream?.Close();
		}
		return false;
	}

	public static void WaitForFile(string file)
	{
		if (File.Exists(file))
		{
			int retry = 15;
			while (IsFileLocked(file) && retry > 0)
			{
				Thread.Sleep(100);
				retry--;
			}
		}
	}

	public static string ReplaceExtension(string file, string ext)
	{
		return Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ext);
	}

	public static bool IsNumeric(object expression)
	{
		if (expression == null || expression is DateTime)
		{
			return false;
		}
		if (expression is short || expression is int || expression is long || expression is decimal || expression is float || expression is double || expression is bool)
		{
			return true;
		}
		try
		{
			if (expression is string)
			{
				double.Parse(expression as string);
			}
			else
			{
				double.Parse(expression.ToString());
			}
			return true;
		}
		catch
		{
		}
		return false;
	}

	public static bool IsMovie(string file)
	{
		if (string.IsNullOrEmpty(file))
		{
			return false;
		}
		string extension = Path.GetExtension(file);
		return extension != null && (extension.ToLower() == ".mov" || extension.ToLower() == ".avi" || extension.ToLower() == ".mts" || extension.ToLower() == ".mp4");
	}

	public static void CreateFolder(string filename)
	{
		string folder = Path.GetDirectoryName(filename);
		if (folder != null && !Directory.Exists(folder))
		{
			Directory.CreateDirectory(folder);
		}
	}

	public static string GetNextFileName(string file)
	{
		if (!File.Exists(file))
		{
			return file;
		}
		string ext = Path.GetExtension(file);
		string fileName = Path.GetFileNameWithoutExtension(file);
		string folder = Path.GetDirectoryName(file);
		int counter = 0;
		string newfile;
		while (true)
		{
			newfile = Path.Combine(folder, fileName + counter.ToString("0000") + ext);
			if (!File.Exists(newfile))
			{
				break;
			}
			counter++;
		}
		return newfile;
	}

	public static int GetInt(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return 0;
		}
		return Convert.ToInt32(s, CultureInfo.InvariantCulture);
	}

	public static double Getdouble(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return 0.0;
		}
		return Convert.ToDouble(s, CultureInfo.InvariantCulture);
	}
}
