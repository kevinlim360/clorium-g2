using WIA;

namespace CameraControl.Devices.Classes;

public class PhotoCapturedEventArgs
{
	private Item _wiaImageItem;

	public Item WiaImageItem
	{
		get
		{
			return _wiaImageItem;
		}
		set
		{
			_wiaImageItem = value;
		}
	}

	public ImageFile ImageFile { get; set; }

	public object EventArgs { get; set; }

	public ICameraDevice CameraDevice { get; set; }

	public string FileName { get; set; }

	public object Handle { get; set; }

	public bool Transfer(string fileName)
	{
		return true;
	}
}
