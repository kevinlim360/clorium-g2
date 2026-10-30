using CameraControl.Core.Classes;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.windows;

public class DownloadableItems
{
	public AsyncObservableCollection<FileItem> Items { get; set; }

	public ICameraDevice Device { get; set; }

	public DownloadableItems()
	{
		Items = new AsyncObservableCollection<FileItem>();
	}
}
