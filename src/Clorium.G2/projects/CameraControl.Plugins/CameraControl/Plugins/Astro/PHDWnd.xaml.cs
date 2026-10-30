using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using CameraControl.Devices;

namespace CameraControl.Plugins.Astro;
public partial class PHDWnd : Window
{
    private TcpClient socket;
    public bool Cancel { get; set; }

    public PHDWnd()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, RoutedEventArgs e)
    {
        Connect();
    }

    private void Connect()
    {
        try
        {
            socket = new TcpClient("localhost", 4300);
        }
        catch (Exception exception)
        {
            Log.Error("Unable to connect PHD", exception);
        }
    }

    private void button2_Click(object sender, RoutedEventArgs e)
    {
        SendReceiveTest2(socket, OpersEnum.MSG_PAUSE);
    }

    public static int SendReceiveTest2(TcpClient server, OpersEnum opersEnum)
    {
        //IL_009a: Unknown result type (might be due to invalid IL or missing references)
        byte[] msg = Encoding.UTF8.GetBytes("This is a test");
        byte[] bytes = new byte[256];
        try
        {
            int byteCount = server.Client.Send(new byte[1] { (byte)opersEnum }, SocketFlags.None);
            Console.WriteLine("Sent {0} bytes.", byteCount);
            byteCount = server.Client.Receive(bytes, SocketFlags.None);
            Console.WriteLine(byteCount);
        }
        catch (SocketException ex)
        {
            Console.WriteLine("{0} Error code: {1}.", ex.Message, ex.ErrorCode);
            return ex.ErrorCode;
        }
        catch (Exception ex2)
        {
            MessageBox.Show("Unable to comunicate with PhD" + ex2.Message);
        }

        return 0;
    }

    private void button3_Click(object sender, RoutedEventArgs e)
    {
        SendReceiveTest2(socket, OpersEnum.MSG_RESUME);
    }

    private void but_move_Click(object sender, RoutedEventArgs e)
    {
        SendReceiveTest2(socket, OpersEnum.MSG_MOVE2);
    }

    private void btn_guid_Click(object sender, RoutedEventArgs e)
    {
        SendReceiveTest2(socket, OpersEnum.MSG_GUIDE);
    }

    private void DoDither(TcpClient socket, AggressivenessENUM moveX, float settleAt)
    {
        DateTime now = DateTime.Now;
        DateTime dateTime = now.AddSeconds(45.0);
        int num1 = SetAggressiveness(socket, moveX);
        byte[] buffer = new byte[1];
        float num2 = 2.55f;
        int num3 = 3;
        int num4 = 0;
        while (!Cancel && !(DateTime.Now > dateTime) && (double)num2 > (double)settleAt)
        {
            Thread.Sleep(num1 * 1000);
            socket.GetStream().Flush();
            socket.Client.Send(new byte[1] { 10 });
            while (!Cancel && socket.Available > 0)
            {
                socket.Client.Receive(buffer, 1, SocketFlags.None);
                num2 = (float)int.Parse(buffer[0].ToString()) / 100f;
                Log.Debug("PHD Dithering pixel value = " + num2);
            }

            if ((double)num2 > 0.0 && num4 < num3)
            {
                num2 = 2.55f;
            }

            num4++;
        }

        if (Cancel)
        {
            Log.Debug("Dither canceled");
        }
        else
        {
            Log.Debug($"PHD Dithering ran for {DateTime.Now.Subtract(now).TotalSeconds:0} seconds");
        }
    }

    private int SetAggressiveness(TcpClient socket, AggressivenessENUM moveX)
    {
        byte[] buffer = new byte[1];
        int val2 = 0;
        if (socket != null)
        {
            socket.GetStream().Flush();
            socket.Client.Send(new byte[1] { (byte)(ushort)moveX });
            Thread.Sleep(1000);
            while (!Cancel && socket.Available > 0)
            {
                socket.Client.Receive(buffer, 1, SocketFlags.None);
                val2 = int.Parse(buffer[0].ToString());
            }
        }

        return Math.Max(1, val2);
    }

    private void btn_diter_Click(object sender, RoutedEventArgs e)
    {
        DoDither(socket, AggressivenessENUM.HIGH, 2.55f);
    }
}
