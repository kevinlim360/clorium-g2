using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CameraControl.Devices.Classes;

namespace CameraControl.Core.Classes;

public class BitmapFile : BaseFieldClass
{
	public delegate void BitmapLoadedEventHandler(object sender);

	private PointCollection luminanceHistogramPoints = null;

	private PointCollection redColorHistogramPoints = null;

	private PointCollection greenColorHistogramPoints = null;

	private PointCollection blueColorHistogramPoints = null;

	private FileItem _fileItem;

	private bool _isLoaded;

	private WriteableBitmap _displayImage;

	private bool _rawCodecNeeded;

	private string _infoLabel;

	private string _fileName;

	private string _comment;

	private BitmapSource _preview;

	public FileItem FileItem
	{
		get
		{
			return _fileItem;
		}
		set
		{
			_fileItem = value;
		}
	}

	public bool IsLoaded
	{
		get
		{
			return _isLoaded;
		}
		set
		{
			_isLoaded = value;
			NotifyPropertyChanged("IsLoaded");
		}
	}

	public bool FullResLoaded { get; set; }

	public WriteableBitmap DisplayImage
	{
		get
		{
			return _displayImage;
		}
		set
		{
			_displayImage = value;
			Console.WriteLine("!!!!!!!!!!!!!!!!!!");
			NotifyPropertyChanged("DisplayImage");
		}
	}

	public BitmapSource Preview
	{
		get
		{
			return _preview;
		}
		set
		{
			_preview = value;
			NotifyPropertyChanged("Preview");
		}
	}

	public PointCollection LuminanceHistogramPoints
	{
		get
		{
			return luminanceHistogramPoints;
		}
		set
		{
			luminanceHistogramPoints = value;
			NotifyPropertyChanged("LuminanceHistogramPoints");
		}
	}

	public PointCollection RedColorHistogramPoints
	{
		get
		{
			return redColorHistogramPoints;
		}
		set
		{
			redColorHistogramPoints = value;
			NotifyPropertyChanged("RedColorHistogramPoints");
		}
	}

	public PointCollection GreenColorHistogramPoints
	{
		get
		{
			return greenColorHistogramPoints;
		}
		set
		{
			greenColorHistogramPoints = value;
			NotifyPropertyChanged("GreenColorHistogramPoints");
		}
	}

	public PointCollection BlueColorHistogramPoints
	{
		get
		{
			return blueColorHistogramPoints;
		}
		set
		{
			blueColorHistogramPoints = value;
			NotifyPropertyChanged("BlueColorHistogramPoints");
		}
	}

	public bool RawCodecNeeded
	{
		get
		{
			return _rawCodecNeeded;
		}
		set
		{
			_rawCodecNeeded = value;
			NotifyPropertyChanged("RawCodecNeeded");
		}
	}

	public string InfoLabel
	{
		get
		{
			return _infoLabel;
		}
		set
		{
			_infoLabel = value;
			NotifyPropertyChanged("InfoLabel");
		}
	}

	public string FileName
	{
		get
		{
			return _fileName;
		}
		set
		{
			_fileName = value;
			NotifyPropertyChanged("FileName");
		}
	}

	public string Comment
	{
		get
		{
			return _comment;
		}
		set
		{
			_comment = value;
			NotifyPropertyChanged("Comment");
		}
	}

	public AsyncObservableCollection<DictionaryItem> Metadata { get; set; }

	public virtual event BitmapLoadedEventHandler BitmapLoaded;

	public void OnBitmapLoaded()
	{
		if (BitmapLoaded != null)
		{
			BitmapLoaded(this);
		}
	}

	public void Notify()
	{
		NotifyPropertyChanged("FileItem");
		NotifyPropertyChanged("LuminanceHistogramPoints");
		NotifyPropertyChanged("RedColorHistogramPoints");
		NotifyPropertyChanged("DisplayImage");
	}

	public void SetFileItem(FileItem item)
	{
		FileItem = item;
		IsLoaded = false;
		FullResLoaded = false;
		if (DisplayImage == null)
		{
			DisplayImage = new WriteableBitmap(FileItem.Thumbnail);
		}
	}

	public BitmapFile()
	{
		IsLoaded = false;
		RawCodecNeeded = false;
		Metadata = new AsyncObservableCollection<DictionaryItem>();
	}
}
