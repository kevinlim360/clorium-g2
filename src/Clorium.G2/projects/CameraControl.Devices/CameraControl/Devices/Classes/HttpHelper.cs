using System;
using System.IO;
using System.Net;

namespace CameraControl.Devices.Classes;

public static class HttpHelper
{
	public static void DownLoadFileByWebRequest(string urlAddress, string filePath, ICameraDevice device, long size = 0L)
	{
		try
		{
			HttpWebRequest httpWebRequest = null;
			HttpWebResponse httpWebResponse = null;
			httpWebRequest = (HttpWebRequest)WebRequest.Create(urlAddress);
			httpWebRequest.Timeout = 30000;
			httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
			Stream responseStream = httpWebResponse.GetResponseStream();
			if (File.Exists(filePath))
			{
				File.Delete(filePath);
			}
			FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
			byte[] array = new byte[1048576];
			int num = 0;
			int num2 = 0;
			while ((num = responseStream.Read(array, 0, array.Length)) > 0)
			{
				fileStream.Write(array, 0, num);
				fileStream.Flush();
				num2 += num;
				if (size > 0)
				{
					device.TransferProgress = (uint)((double)num2 / (double)size * 100.0);
				}
				else
				{
					device.TransferProgress++;
				}
			}
			fileStream.Close();
			responseStream.Close();
			device.TransferProgress = 100u;
		}
		catch (Exception exception)
		{
			Log.Error("Error download file", exception);
		}
		finally
		{
		}
	}
}
