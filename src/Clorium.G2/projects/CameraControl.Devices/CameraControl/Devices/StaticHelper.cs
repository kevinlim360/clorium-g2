using System;
using System.IO;
using System.Timers;
using CameraControl.Devices.Classes;
using PortableDeviceLib;

namespace CameraControl.Devices;

public class StaticHelper : BaseFieldClass
{
	private static StaticHelper _instance;

	private Timer _timer = new Timer(2000.0);

	private string _systemMessage;

	private int _loadingProgress;

	private bool _isActive;

	public AsyncObservableCollection<string> Messages { get; set; }

	public static StaticHelper Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new StaticHelper();
			}
			return _instance;
		}
		set
		{
			_instance = value;
		}
	}

	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			_isActive = value;
			NotifyPropertyChanged("IsActive");
		}
	}

	public string SystemMessage
	{
		get
		{
			return _systemMessage;
		}
		set
		{
			_systemMessage = value;
			IsActive = !string.IsNullOrWhiteSpace(_systemMessage);
			NotifyPropertyChanged("SystemMessage");
			if (!string.IsNullOrWhiteSpace(_systemMessage))
			{
				Messages.Add(DateTime.Now.ToShortTimeString() + " - " + _systemMessage);
			}
			_timer.Stop();
			_timer.Start();
		}
	}

	public int LoadingProgress
	{
		get
		{
			return _loadingProgress;
		}
		set
		{
			_loadingProgress = value;
			NotifyPropertyChanged("LoadingProgress");
		}
	}

	public StaticHelper()
	{
		SystemMessage = "";
		_timer.Elapsed += _timer_Elapsed;
		_timer.Start();
		Messages = new AsyncObservableCollection<string>();
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		SystemMessage = "";
	}

	public static bool GetBit(int b, int bitNumber)
	{
		return (b & (1 << bitNumber)) != 0;
	}

	public static bool GetBit(byte b, int bitNumber)
	{
		return (b & (1 << bitNumber)) != 0;
	}

	public static bool GetBit(long b, int bitNumber)
	{
		return (b & (1 << bitNumber)) != 0;
	}

	public static string GetSerial(string pnpstring)
	{
		if (pnpstring == null)
		{
			return "";
		}
		string result = "";
		if (pnpstring.Contains("#"))
		{
			string[] array = pnpstring.Split(new char[1] { '#' });
			if (array.Length > 2)
			{
				result = array[2];
			}
		}
		return result;
	}

	public static string GetUniqueFilename(string prefix, int counter, string sufix)
	{
		string text = prefix + counter + sufix;
		if (File.Exists(text))
		{
			return GetUniqueFilename(prefix, counter + 1, sufix);
		}
		return text;
	}

	public static int GetDataLength(uint dataType)
	{
		int result = 0;
		switch (dataType)
		{
		case 1u:
			result = 1;
			break;
		case 2u:
			result = 1;
			break;
		case 3u:
			result = 2;
			break;
		case 4u:
			result = 2;
			break;
		case 5u:
			result = 4;
			break;
		case 6u:
			result = 4;
			break;
		case 7u:
			result = 8;
			break;
		case 8u:
			result = 8;
			break;
		case 9u:
			result = 16;
			break;
		case 10u:
			result = 16;
			break;
		case 16385u:
			result = 1;
			break;
		case 16386u:
			result = 1;
			break;
		case 16387u:
			result = 2;
			break;
		case 16388u:
			result = 2;
			break;
		case 16389u:
			result = 4;
			break;
		case 16390u:
			result = 4;
			break;
		case 16391u:
			result = 8;
			break;
		case 16392u:
			result = 8;
			break;
		case 16393u:
			result = 16;
			break;
		case 16394u:
			result = 16;
			break;
		case 65535u:
			result = -1;
			break;
		}
		return result;
	}

	public static long GetValue(MTPDataResponse result, int index, int dataLength)
	{
		long num = 0L;
		return dataLength switch
		{
			1 => result.Data[index], 
			2 => BitConverter.ToUInt16(result.Data, index), 
			4 => BitConverter.ToUInt32(result.Data, index), 
			_ => (long)BitConverter.ToUInt64(result.Data, index), 
		};
	}
}
