using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using PortableDeviceLib;

namespace CameraControl.Devices.TransferProtocol;

public class YiCameraProtocol : ITransferProtocol
{
	public delegate void DataReceiverdHandler(object sender, string data);

	private object _locker = new object();

	private Socket m_sock;

	private byte[] m_byBuff = new byte[256];

	private string data = "";

	public string Token { get; set; }

	public string Ip { get; set; }

	public string Model { get; private set; }

	public string Manufacturer { get; private set; }

	public string SerialNumber { get; private set; }

	public bool IsConnected { get; set; }

	public string DeviceId { get; private set; }

	public event DataReceiverdHandler DataReceiverd;

	public YiCameraProtocol()
	{
		Manufacturer = "Xiaomi";
		Model = "Yi action camera";
	}

	public void Connect(string address, int port)
	{
		Ip = address;
		if (m_sock != null && m_sock.Connected)
		{
			m_sock.Shutdown(SocketShutdown.Both);
			Thread.Sleep(10);
			m_sock.Close();
		}
		m_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		IPAddress address2 = IPAddress.Parse(address);
		IPEndPoint remoteEP = new IPEndPoint(address2, port);
		m_sock.Blocking = false;
		AsyncCallback callback = OnConnect;
		m_sock.BeginConnect(remoteEP, callback, m_sock);
		DateTime now = DateTime.Now;
		while (!m_sock.Connected)
		{
			Thread.Sleep(50);
			if ((DateTime.Now - now).TotalMilliseconds > 3000.0)
			{
				throw new Exception("Connection timeout.");
			}
		}
		SendCommand(257);
		now = DateTime.Now;
		while (string.IsNullOrEmpty(Token))
		{
			Thread.Sleep(50);
			if ((DateTime.Now - now).TotalMilliseconds > 3000.0)
			{
				throw new Exception("Unable to get token.");
			}
		}
	}

	public void SendCommand(int command)
	{
		m_sock.Send(Encoding.UTF8.GetBytes(string.Format("{{\"msg_id\":{0},\"token\":{1}}}", command, Token ?? "0")));
	}

	public void SendCommand(int command, string param)
	{
		string s = string.Format("{{\"msg_id\":{0},\"param\":\"{1}\",\"token\":{2}}}", command, param, Token ?? "0");
		m_sock.Send(Encoding.UTF8.GetBytes(s));
	}

	public void SendValue(string param, string value)
	{
		string s = string.Format("{{\"type\":\"{0}\",\"msg_id\":2,\"param\":\"{1}\",\"token\":{2}}}", param, value, Token ?? "0");
		m_sock.Send(Encoding.UTF8.GetBytes(s));
	}

	public void OnConnect(IAsyncResult ar)
	{
		Socket socket = (Socket)ar.AsyncState;
		try
		{
			if (socket.Connected)
			{
				SetupRecieveCallback(socket);
			}
		}
		catch (Exception exception)
		{
			Log.Error("OnRecievedData", exception);
		}
	}

	public void OnRecievedData(IAsyncResult ar)
	{
		Socket socket = (Socket)ar.AsyncState;
		try
		{
			int num = socket.EndReceive(ar);
			if (num > 0)
			{
				data += Encoding.ASCII.GetString(m_byBuff, 0, num);
				if ((from x in data.ToCharArray()
					where x == '{'
					select x).Count() == (from x in data.ToCharArray()
					where x == '}'
					select x).Count())
				{
					Console.WriteLine(data + "**************");
					if (data.Contains("}{"))
					{
						data = data.Replace("}{", "}|{");
						string[] array = data.Split(new string[1] { "|" }, StringSplitOptions.RemoveEmptyEntries);
						string[] array2 = array;
						foreach (string text in array2)
						{
							ProcessData(text);
						}
					}
					else
					{
						ProcessData(data);
					}
					data = "";
				}
				SetupRecieveCallback(socket);
			}
			else
			{
				Console.WriteLine("Client {0}, disconnected", socket.RemoteEndPoint);
				socket.Shutdown(SocketShutdown.Both);
				socket.Close();
			}
		}
		catch (Exception exception)
		{
			Log.Error("OnRecievedData", exception);
		}
	}

	private void ProcessData(string data)
	{
		try
		{
			dynamic val = JsonConvert.DeserializeObject(data);
			string text = (string)val.msg_id;
			string text2 = text;
			if (text2 == "257")
			{
				Token = val.param;
			}
			else
			{
				OnDataReceiverd(data);
			}
		}
		catch (Exception exception)
		{
			Log.Error("SetupRecieveCallback", exception);
		}
	}

	public void SetupRecieveCallback(Socket sock)
	{
		try
		{
			AsyncCallback callback = OnRecievedData;
			sock.BeginReceive(m_byBuff, 0, m_byBuff.Length, SocketFlags.None, callback, sock);
		}
		catch (Exception exception)
		{
			Log.Error("SetupRecieveCallback", exception);
		}
	}

	public MTPDataResponse ExecuteReadBigData(uint code, Stream stream, StillImageDevice.TransferCallback callback, params uint[] parameters)
	{
		throw new NotImplementedException();
	}

	public MTPDataResponse ExecuteReadData(uint code, params uint[] parameters)
	{
		throw new NotImplementedException();
	}

	public uint ExecuteWithNoData(uint code, params uint[] parameters)
	{
		throw new NotImplementedException();
	}

	public uint ExecuteWriteData(uint code, byte[] data, params uint[] parameters)
	{
		throw new NotImplementedException();
	}

	public void Disconnect()
	{
	}

	protected virtual void OnDataReceiverd(string data)
	{
		DataReceiverd?.Invoke(this, data);
	}
}
