using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using CameraControl.Core;
using CameraControl.Devices;
using CameraControl.ViewModel;

namespace CameraControl.Classes;

internal class ArtNetSender : IDisposable
{
	private readonly IPEndPoint _remoteEndPoint;

	private static UdpClient client;

	private const string eDMX_IP = "2.50.210.35";

	private const string Godox_Address1 = "11";

	private const string Godox_Address2 = "21";

	private const string Godox_Address3 = "31";

	private const string Godox_Address4 = "41";

	public ArtNetSender()
	{
		_remoteEndPoint = new IPEndPoint(IPAddress.Parse("2.50.210.35"), 6454);
		Log.Debug("ArtNetSender 소켓 연결");
	}

	public void Dispose()
	{
		if (client != null)
		{
			byte[] blankData = new byte[512];
			byte[] packet = BuildArtNetPacket(0, blankData);
			client.Send(packet, packet.Length, _remoteEndPoint);
			client.Close();
			client.Dispose();
			client = null;
			Log.Debug("ArtNetSender 소켓 해제");
		}
	}

	public void Send(Preset preset)
	{
		if (client == null)
		{
			client = new UdpClient();
		}
		byte[] dmxData = new byte[512];
		for (int i = 0; i < preset.Lights.Count; i++)
		{
			LightSetting light = preset.Lights[i];
			int address;
			if (light.Name == "조명1")
			{
				address = int.Parse("11");
			}
			else if (light.Name == "조명2")
			{
				address = int.Parse("21");
			}
			else if (light.Name == "조명3")
			{
				address = int.Parse("31");
			}
			else
			{
				if (!(light.Name == "조명4"))
				{
					MessageBox.Show("조명 주소 설정 확인 요망");
					return;
				}
				address = int.Parse("41");
			}
			if (byte.TryParse(light.Brightness, out var brightness) && int.TryParse(light.CCT, out var Kelvin_cct))
			{
				if (address + 1 < dmxData.Length)
				{
					dmxData[address - 1] = brightness;
					dmxData[address] = KelvinToDmx(Kelvin_cct);
				}
				continue;
			}
			MessageBox.Show($"Light {i}의 Brightness 또는 CCT 파싱 실패");
			return;
		}
		SendArtNetPacket(0, dmxData);
	}

	public void SendArtNetPacket(int universe, byte[] dmxData)
	{
		byte[] packet = BuildArtNetPacket(universe, dmxData);
		try
		{
			client.Send(packet, packet.Length, _remoteEndPoint);
		}
		catch (SocketException ex)
		{
			MessageBox.Show("UDP 전송 실패: " + ex.Message + "\nIP 주소 또는 네트워크 연결을 확인하세요.");
		}
	}

	public void SendAllOff()
	{
		if (client == null)
		{
			client = new UdpClient();
		}
		byte[] blankData = new byte[512];
		byte[] packet = BuildArtNetPacket(0, blankData);
		try
		{
			client.Send(packet, packet.Length, _remoteEndPoint);
		}
		catch (SocketException ex)
		{
			MessageBox.Show("UDP 전송 실패: " + ex.Message + "\nIP 주소 또는 네트워크 연결을 확인하세요.");
		}
	}

	private byte[] BuildArtNetPacket(int universe, byte[] dmxData)
	{
		byte[] packet = new byte[18 + dmxData.Length];
		Encoding.ASCII.GetBytes("Art-Net").CopyTo(packet, 0);
		packet[7] = 0;
		packet[8] = 0;
		packet[9] = 80;
		packet[10] = 14;
		packet[11] = 0;
		packet[12] = 0;
		packet[13] = 0;
		packet[14] = (byte)(universe & 0xFF);
		packet[15] = (byte)((universe >> 8) & 0xFF);
		packet[16] = (byte)((dmxData.Length >> 8) & 0xFF);
		packet[17] = (byte)(dmxData.Length & 0xFF);
		dmxData.CopyTo(packet, 18);
		return packet;
	}

	public byte KelvinToDmx(int kelvin)
	{
		int Min_Kelvin = ServiceProvider.Settings.LightTemperatureMin;
		int Max_Kelvin = ServiceProvider.Settings.LightTemperatureMax;
		kelvin = Clamp(kelvin, Min_Kelvin, Max_Kelvin);
		int dmxValue = (kelvin - Min_Kelvin) * 255 / (Max_Kelvin - Min_Kelvin);
		return (byte)dmxValue;
	}

	public int Clamp(int value, int min, int max)
	{
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}
}
