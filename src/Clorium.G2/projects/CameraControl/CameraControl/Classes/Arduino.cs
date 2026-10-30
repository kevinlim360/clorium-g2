using System;
using System.IO.Ports;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using CameraControl.Core;
using CameraControl.Devices;

namespace CameraControl.Classes;

internal class Arduino
{
	private readonly SerialPort Port;

	private readonly Timer timerHeartBeat;

	private readonly string Manufacturer;

	private string _currentPosPluse;

	private Brush _connectStatus;

	private const uint interval_HeartBeat = 2000u;

	private int prevMillis_TickCount = 0;

	private string rxBuffer = "";

	public bool bIsConnected { get; private set; } = false;

	public string CurrentPos_pluse
	{
		get
		{
			return _currentPosPluse;
		}
		private set
		{
			if (_currentPosPluse != value)
			{
				_currentPosPluse = value;
				CurrentPosPluseChanged?.Invoke(_currentPosPluse);
			}
		}
	}

	public Brush ConnectStatus
	{
		get
		{
			return _connectStatus;
		}
		private set
		{
			if (_connectStatus != value)
			{
				_connectStatus = value;
				ConnectStatusChanged?.Invoke(_connectStatus);
			}
		}
	}

	public event Action<string> CurrentPosPluseChanged;

	public event Action<Brush> ConnectStatusChanged;

	public Arduino(SerialPort port, int baudRate, string manufacturer)
	{
		Port = port;
		Port.BaudRate = baudRate;
		Port.DataBits = 8;
		Port.Parity = Parity.None;
		Port.StopBits = StopBits.One;
		Port.ReadTimeout = 100;
		Manufacturer = manufacturer;
		timerHeartBeat = new Timer();
		ConnectStatus = Brushes.Red;
	}

	public bool Init()
	{
		bool ret = ConnectSerial();
		if (!ret)
		{
			return false;
		}
		prevMillis_TickCount = 0;
		timerHeartBeat.Elapsed += Timer_HeartBeat;
		timerHeartBeat.AutoReset = true;
		timerHeartBeat.Interval = 500.0;
		timerHeartBeat.Start();
		Port.DataReceived += SerialPort_DataReceived;
		Port.Encoding = Encoding.UTF8;
		bIsConnected = true;
		return ret;
	}

	public void Uninit()
	{
		ConnectStatus = Brushes.Red;
		timerHeartBeat.Stop();
		timerHeartBeat.Elapsed -= Timer_HeartBeat;
		Port.DataReceived -= SerialPort_DataReceived;
		bIsConnected = false;
		DisconnectSerial();
	}

	public bool ConnectSerial()
	{
		string comPort = ServiceProvider.Settings.GetPortByKeywords(Manufacturer);
		if (comPort == null)
		{
			return false;
		}
		try
		{
			if (Port.IsOpen)
			{
				Port.Close();
			}
			Port.PortName = comPort;
			Port.Open();
			return Port.IsOpen;
		}
		catch
		{
			return false;
		}
	}

	public bool DisconnectSerial()
	{
		if (Port.IsOpen)
		{
			Port.Close();
		}
		return Port.IsOpen;
	}

	private void Timer_HeartBeat(object source, ElapsedEventArgs e)
	{
		bool isNowConnected = (bIsConnected = (long)Math.Abs(Environment.TickCount - prevMillis_TickCount) <= 2000L);
		ConnectStatus = (isNowConnected ? Brushes.Blue : Brushes.Red);
	}

	public void CommandSend(string msg)
	{
		if (bIsConnected && !(msg == ""))
		{
			Port.Write(msg);
			Log.Debug("TX : " + msg, ServiceProvider.Settings.UseDebugLog);
		}
	}

	private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
	{
		SerialPort sp = (SerialPort)sender;
		try
		{
			rxBuffer += sp.ReadExisting();
			int newlineIndex;
			while ((newlineIndex = rxBuffer.IndexOf('\n')) >= 0)
			{
				string line = rxBuffer.Substring(0, newlineIndex).Trim();
				rxBuffer = rxBuffer.Substring(newlineIndex + 1);
				prevMillis_TickCount = Environment.TickCount;
				if (line.StartsWith("OK:getpos: "))
				{
					CurrentPos_pluse = line.Substring(11).Trim();
				}
				if (!line.StartsWith("live") && !line.StartsWith("OK:getpos: "))
				{
					Log.Debug("Turntable RX : " + line, ServiceProvider.Settings.UseDebugLog);
				}
			}
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			Exception ex3 = ex2;
			Application.Current.Dispatcher.Invoke(() =>
			{
				MessageBox.Show("⚠\ufe0f 시리얼 수신 중 예외 발생: " + ex3.Message, "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			});
		}
	}
}
