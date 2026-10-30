using System.IO;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using ImageMagick;
using ZXing;
using ZXing.Common;

namespace CameraControl.Plugins.FilenameTemplate;

public class BarcodeFromImage : IFilenameTemplate
{
	public bool IsRuntime => true;

	public string Pharse(string template, PhotoSession session, ICameraDevice device, string fileName, string tempfileName)
	{
		if (!File.Exists(tempfileName))
		{
			return "";
		}
		PhotoUtils.WaitForFile(tempfileName);
		if (Path.GetExtension(fileName).ToLower() == ".cr2" || Path.GetExtension(fileName).ToLower() == ".nef")
		{
			string dcraw_exe = Path.Combine(Settings.ApplicationFolder, "dcraw.exe");
			if (File.Exists(dcraw_exe))
			{
				string thumb = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(tempfileName) + ".thumb.jpg");
				PhotoUtils.RunAndWait(dcraw_exe, $" -e -O \"{thumb}\" \"{tempfileName}\"");
				if (File.Exists(thumb))
				{
					string res = GetBarcode(thumb, template);
					File.Delete(thumb);
					return res;
				}
			}
			return template;
		}
		return GetBarcode(tempfileName, template);
	}

	private string GetBarcode(string filename, string template)
	{
		IBarcodeReader reader = new BarcodeReader
		{
			AutoRotate = true,
			Options = new DecodingOptions
			{
				TryHarder = true
			}
		};
		using (MagickImage barcodeBitmap = new MagickImage(filename))
		{
			Result result = reader.Decode(barcodeBitmap.ToBitmap());
			if (result != null)
			{
				return result.Text;
			}
		}
		return template;
	}
}
