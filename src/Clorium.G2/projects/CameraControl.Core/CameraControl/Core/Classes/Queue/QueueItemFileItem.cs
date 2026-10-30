using System;
using System.IO;
using System.Windows.Media.Imaging;
using CameraControl.Core.Interfaces;

namespace CameraControl.Core.Classes.Queue;

public class QueueItemFileItem : IQueueItem
{
	public FileItem FileItem { get; set; }

	public QueueType Generate { get; set; }

	public bool Execute(QueueManager manager)
	{
		try
		{
			if (FileItem.ItemType != FileItemType.File)
			{
				return true;
			}
			switch (Generate)
			{
			case QueueType.Thumb:
				if (File.Exists(FileItem.SmallThumb))
				{
					WriteableBitmap thumb = BitmapLoader.Instance.LoadSmallImage(FileItem);
					thumb.Freeze();
					FileItem.Thumbnail = thumb;
					thumb = thumb.Clone();
					if (ServiceProvider.Settings.UseHighlight)
					{
						BitmapLoader.Highlight(thumb, under: true, over: true);
					}
					BitmapLoader.Instance.DrawFocusPoints(FileItem, thumb);
					thumb.Freeze();
					FileItem.ThumbnailMarks = thumb;
				}
				else
				{
					FileItem.GetExtendedThumb();
					if (ServiceProvider.Settings.SelectedBitmap.FileItem != FileItem)
					{
						ServiceProvider.QueueManager.Add(new QueueItemFileItem
						{
							FileItem = FileItem,
							Generate = QueueType.Cache
						});
					}
				}
				break;
			case QueueType.Cache:
				BitmapLoader.Instance.GenerateCache(FileItem);
				if (!FileItem.HaveHistogramReady())
				{
					ServiceProvider.QueueManager.AddWithLowPriority(new QueueItemFileItem
					{
						FileItem = FileItem,
						Generate = QueueType.Histogram
					});
				}
				break;
			case QueueType.Histogram:
				BitmapLoader.Instance.LoadHistogram(FileItem);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
		catch (Exception)
		{
		}
		return true;
	}
}
