using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices.Others;

internal class FTPFakeCameraDevice : BaseCameraDevice
{
	private string FfpPathFolder = "";

	public FTPFakeCameraDevice(string ftpFolder)
	{
		DeviceName = "Z-FTP camera";
		SerialNumber = "No2";
		FfpPathFolder = ftpFolder;
		HaveLiveView = false;
		IsBusy = false;
		IsConnected = true;
		Battery = 100;
	}

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		return true;
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		string[] array = new string[3] { "*.jpg", "*.jpeg", "*.mp4" };
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string searchPattern in array2)
		{
			list.AddRange(Directory.GetFiles(FfpPathFolder, searchPattern));
		}
		if (list.Count > 0)
		{
			Random random = new Random();
			string path = list[random.Next(list.Count)];
			string text = Path.GetExtension(path).ToLower();
			byte[] handle = File.ReadAllBytes(path);
			PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
			{
				WiaImageItem = null,
				EventArgs = null,
				CameraDevice = this,
				FileName = ((text == ".mp4") ? "EventVideo.mp4" : "img0000.jpg"),
				Handle = handle
			};
			Task.Factory.StartNew(() =>
			{
				OnPhotoCapture(this, args);
			});
			return;
		}
		throw new Exception("폴더에 처리할 파일(jpg, mp4)이 없습니다.");
	}

	public override void CapturePhotoNoAf()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		CapturePhoto();
	}

	public override void TransferFile(object o, Stream stream)
	{
		byte[] array = File.ReadAllBytes((string)o);
		stream.Write(array, 0, array.Length);
	}

	public override void TransferFile(object o, string filename)
	{
		try
		{
			if (o is byte[] bytes)
			{
				File.WriteAllBytes(filename, bytes);
				return;
			}
			if (o is string text)
			{
				if (File.Exists(text))
				{
					File.Copy(text, filename, overwrite: true);
				}
				return;
			}
			throw new InvalidCastException("지원하지 않는 데이터 형식입니다: " + o.GetType().Name);
		}
		catch (Exception exception)
		{
			Log.Error("파일 전송(TransferFile) 중 오류 발생: ", exception);
		}
	}

	public override bool DeleteObject(DeviceObject deviceObject)
	{
		return true;
	}
}
