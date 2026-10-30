using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using CameraControl.Core.Classes.Queue;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using Newtonsoft.Json;

namespace CameraControl.Core.Classes;

public class FileItem : BaseFieldClass, IDisposable
{
	private string _f;

	private string _iso;

	private string _e;

	private string _focalLength;

	private string _exposureBias;

	private bool _visible;

	private string _fileName;

	private string _backupFileName;

	private int _rotation;

	private int _autoRotation;

	private bool _transformed;

	private bool _loading;

	private bool _alternate;

	private bool _isChecked;

	private bool _isLiked;

	private bool _isUnLiked;

	private DeviceObject _deviceObject;

	private ICameraDevice _device;

	private FileItemType _itemType;

	private int _id;

	private BitmapSource _thumbnailMarks;

	private BitmapSource _thumbnail;

	private FileInfo _fileInfo;

	private bool _disposed = false;

	public AsyncObservableCollection<ValuePair> FileNameTemplates { get; set; }

	public byte[] ThumbData { get; set; }

	[XmlAttribute]
	public string CameraSerial { get; set; }

	[XmlAttribute]
	public string OriginalName { get; set; }

	[XmlAttribute]
	public int Series { get; set; }

	[XmlAttribute]
	public DateTime FileDate { get; set; }

	[XmlAttribute]
	public string Name { get; set; }

	[JsonIgnore]
	[XmlIgnore]
	public bool IsLoaded { get; set; }

	public bool IsLoading { get; set; }

	[XmlAttribute]
	public string F
	{
		get
		{
			return _f;
		}
		set
		{
			_f = value;
			NotifyPropertyChanged("F");
		}
	}

	[XmlAttribute]
	public string Iso
	{
		get
		{
			return _iso;
		}
		set
		{
			_iso = value;
			NotifyPropertyChanged("Iso");
		}
	}

	[XmlAttribute]
	public string E
	{
		get
		{
			return _e;
		}
		set
		{
			_e = value;
			NotifyPropertyChanged("E");
		}
	}

	[XmlAttribute]
	public string FocalLength
	{
		get
		{
			return _focalLength;
		}
		set
		{
			_focalLength = value;
			NotifyPropertyChanged("FocalLength");
		}
	}

	[XmlAttribute]
	public string ExposureBias
	{
		get
		{
			return _exposureBias;
		}
		set
		{
			_exposureBias = value;
			NotifyPropertyChanged("ExposureBias");
		}
	}

	[XmlIgnore]
	[JsonIgnore]
	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			_visible = value;
			NotifyPropertyChanged("Visible");
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
			if (File.Exists(_fileName))
			{
				FileDate = File.GetCreationTime(_fileName);
			}
			NotifyPropertyChanged("FileName");
			NotifyPropertyChanged("ToolTip");
		}
	}

	public string BackupFileName
	{
		get
		{
			return _backupFileName;
		}
		set
		{
			_backupFileName = value;
			NotifyPropertyChanged("BackupFileName");
		}
	}

	[XmlAttribute]
	public int Rotation
	{
		get
		{
			return _rotation;
		}
		set
		{
			_rotation = value;
			NotifyPropertyChanged("RotationAngle");
		}
	}

	[XmlAttribute]
	public int AutoRotation
	{
		get
		{
			return _autoRotation;
		}
		set
		{
			_autoRotation = value;
			NotifyPropertyChanged("AutoRotation");
			NotifyPropertyChanged("RotationAngle");
		}
	}

	[XmlAttribute]
	public bool Transformed
	{
		get
		{
			return _transformed;
		}
		set
		{
			_transformed = value;
			NotifyPropertyChanged("Transformed");
		}
	}

	[XmlIgnore]
	[JsonIgnore]
	public bool Loading
	{
		get
		{
			return _loading;
		}
		set
		{
			_loading = value;
			NotifyPropertyChanged("Loading");
		}
	}

	[XmlIgnore]
	[JsonIgnore]
	public bool Alternate
	{
		get
		{
			return _alternate;
		}
		set
		{
			_alternate = value;
			NotifyPropertyChanged("Alternate");
		}
	}

	[XmlAttribute]
	public bool IsChecked
	{
		get
		{
			if (!Visible)
			{
				return false;
			}
			return _isChecked;
		}
		set
		{
			_isChecked = value;
			NotifyPropertyChanged("IsChecked");
		}
	}

	[XmlAttribute]
	public bool IsLiked
	{
		get
		{
			return _isLiked;
		}
		set
		{
			_isLiked = value;
			_isUnLiked = (!_isUnLiked || !_isLiked) && _isUnLiked;
			NotifyPropertyChanged("IsLiked");
			NotifyPropertyChanged("IsUnLiked");
		}
	}

	[XmlAttribute]
	public bool IsUnLiked
	{
		get
		{
			return _isUnLiked;
		}
		set
		{
			_isUnLiked = value;
			_isLiked = (!_isUnLiked || !_isLiked) && _isLiked;
			NotifyPropertyChanged("IsLiked");
			NotifyPropertyChanged("IsUnLiked");
		}
	}

	[JsonIgnore]
	[XmlIgnore]
	public DeviceObject DeviceObject
	{
		get
		{
			return _deviceObject;
		}
		set
		{
			_deviceObject = value;
			NotifyPropertyChanged("DeviceObject");
		}
	}

	[JsonIgnore]
	[XmlIgnore]
	public ICameraDevice Device
	{
		get
		{
			return _device;
		}
		set
		{
			_device = value;
			NotifyPropertyChanged("Device");
		}
	}

	[JsonIgnore]
	[XmlIgnore]
	public FileItemType ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			_itemType = value;
			NotifyPropertyChanged("ItemType");
		}
	}

	[JsonIgnore]
	[XmlIgnore]
	public string ToolTip => $"File name: {FileName}\nFile date :{FileDate}";

	public bool IsJpg => !IsRaw && !IsMovie;

	public bool IsRaw
	{
		get
		{
			string extension = Path.GetExtension(FileName);
			return extension != null && !string.IsNullOrEmpty(FileName) && (extension.ToLower() == ".nef" || extension.ToLower() == ".cr2");
		}
	}

	public bool IsMovie => PhotoUtils.IsMovie(FileName);

	public int RotationAngle => (Rotation + (ServiceProvider.Settings.Autorotate ? AutoRotation : 0)) * 90;

	public bool HaveGeneratedThumbnail => File.Exists(SmallThumb) && File.Exists(LargeThumb);

	public bool HaveBackupFile => File.Exists(BackupFileName);

	public string ShortName => (FileName == null) ? "" : Path.GetFileNameWithoutExtension(FileName);

	public string Format => IsRaw ? "RAW" : "JPG";

	public int Id
	{
		get
		{
			if (_id == 0 && !string.IsNullOrEmpty(FileName) && File.Exists(FileName))
			{
				_id = Math.Abs(FileName.GetHashCode() + File.GetCreationTimeUtc(FileName).GetHashCode());
			}
			return _id;
		}
		set
		{
			_id = value;
		}
	}

	public string SmallThumb => Path.Combine(Settings.DataFolder, "Cache\\Small", Id.ToString().Substring(0, 1), Id + ".jpg");

	public string LargeThumb => Path.Combine(Settings.DataFolder, "Cache\\Large", Id.ToString().Substring(0, 1), Id + ".jpg");

	public string InfoFile => Path.Combine(Settings.DataFolder, "Cache\\InfoFile", Id.ToString().Substring(0, 1), Id + ".json");

	[JsonIgnore]
	[XmlIgnore]
	public BitmapSource ThumbnailMarks
	{
		get
		{
			return _thumbnailMarks;
		}
		set
		{
			_thumbnailMarks = value;
			NotifyPropertyChanged("ThumbnailMarks");
		}
	}

	[JsonIgnore]
	[XmlIgnore]
	public BitmapSource Thumbnail
	{
		get
		{
			if (_thumbnail == null)
			{
				if (ItemType == FileItemType.CameraObject)
				{
					if (ThumbData != null && ThumbData.Length > 4)
					{
						try
						{
							using MemoryStream stream = new MemoryStream(ThumbData, 0, ThumbData.Length);
							BitmapImage image = new BitmapImage();
							image.BeginInit();
							image.DecodePixelWidth = 90;
							image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
							image.CacheOption = BitmapCacheOption.OnLoad;
							image.UriSource = null;
							image.StreamSource = stream;
							image.EndInit();
							image.Freeze();
							Thumbnail = image;
						}
						catch (Exception exception)
						{
							Log.Debug("Error loading device thumb ", exception);
						}
					}
				}
				else
				{
					_thumbnail = ((ItemType == FileItemType.Missing) ? BitmapLoader.Instance.NoImageThumbnail : BitmapLoader.Instance.DefaultThumbnail);
					if (!ServiceProvider.Settings.DontLoadThumbnails)
					{
						ServiceProvider.QueueManager.AddWithPriority(new QueueItemFileItem
						{
							FileItem = this,
							Generate = QueueType.Thumb
						});
					}
				}
			}
			return _thumbnail;
		}
		set
		{
			_thumbnail = value;
			NotifyPropertyChanged("Thumbnail");
		}
	}

	[JsonIgnore]
	[XmlIgnore]
	public FileInfo FileInfo
	{
		get
		{
			if (_fileInfo == null)
			{
				LoadInfo();
			}
			return _fileInfo;
		}
		set
		{
			_fileInfo = value;
			NotifyPropertyChanged("FileInfo");
		}
	}

	public FileItem()
	{
		IsLoaded = false;
		Visible = true;
	}

	public FileItem(string file)
	{
		IsLoaded = false;
		FileName = file;
		Name = Path.GetFileName(file);
		ItemType = FileItemType.File;
		Loading = false;
		FileNameTemplates = new AsyncObservableCollection<ValuePair>();
		Visible = true;
	}

	public FileItem(DeviceObject deviceObject, ICameraDevice device)
	{
		Device = device;
		DeviceObject = deviceObject;
		ItemType = FileItemType.CameraObject;
		FileName = deviceObject.FileName;
		Name = FileName;
		FileDate = deviceObject.FileDate;
		IsChecked = true;
		IsLiked = false;
		IsUnLiked = false;
		ThumbData = deviceObject.ThumbData;
		Visible = true;
	}

	public FileItem(ICameraDevice device, DateTime time)
	{
		Device = device;
		ItemType = FileItemType.CameraObject;
		IsChecked = true;
		ItemType = FileItemType.Missing;
		FileName = "Missing";
		FileDate = time;
	}

	public void RemoveThumbs()
	{
		try
		{
			if (File.Exists(SmallThumb))
			{
				PhotoUtils.WaitForFile(SmallThumb);
				File.Delete(SmallThumb);
			}
			if (File.Exists(LargeThumb))
			{
				PhotoUtils.WaitForFile(LargeThumb);
				File.Delete(LargeThumb);
			}
			if (File.Exists(InfoFile))
			{
				PhotoUtils.WaitForFile(InfoFile);
				File.Delete(InfoFile);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Unable to remove thumb data", exception);
		}
	}

	public void GetExtendedThumb()
	{
		if (ItemType != FileItemType.File)
		{
			return;
		}
		try
		{
			if (IsRaw)
			{
				try
				{
					BitmapDecoder bmpDec = BitmapDecoder.Create(new Uri(FileName), BitmapCreateOptions.None, BitmapCacheOption.Default);
					if (bmpDec.Thumbnail != null)
					{
						WriteableBitmap bitmap = new WriteableBitmap(bmpDec.Thumbnail);
						bitmap.Freeze();
						Thumbnail = bitmap;
					}
					return;
				}
				catch (Exception)
				{
					return;
				}
			}
			Thread.Sleep(50);
			string extension = Path.GetExtension(FileName);
			if (extension.ToLower() == ".mp4" || extension.ToLower() == ".mov")
			{
				return;
			}
			try
			{
				Image.GetThumbnailImageAbort myCallback = ThumbnailCallback;
				using Stream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				Image tempImage = Image.FromStream(fs, useEmbeddedColorManagement: true, validateImageData: false);
				Thumbnail = BitmapSourceConvert.ToBitmapSource((Bitmap)tempImage.GetThumbnailImage(180, 120, myCallback, IntPtr.Zero));
				tempImage.Dispose();
			}
			catch
			{
				Log.Debug("[Yjh 썸네일 열기 에러 발생] 300ms Delay 후 재시도 : " + FileName);
				Thread.Sleep(300);
				Image.GetThumbnailImageAbort myCallback2 = ThumbnailCallback;
				using Stream fs2 = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				Image tempImage2 = Image.FromStream(fs2, useEmbeddedColorManagement: true, validateImageData: false);
				Thumbnail = BitmapSourceConvert.ToBitmapSource((Bitmap)tempImage2.GetThumbnailImage(180, 120, myCallback2, IntPtr.Zero));
				tempImage2.Dispose();
			}
		}
		catch (Exception exception)
		{
			Log.Debug("Unable load thumbnail: " + FileName, exception);
		}
	}

	private bool ThumbnailCallback()
	{
		return false;
	}

	public void SaveInfo()
	{
		if (FileInfo == null)
		{
			return;
		}
		try
		{
			PhotoUtils.CreateFolder(InfoFile);
			string json = JsonConvert.SerializeObject(FileInfo);
			File.WriteAllText(InfoFile, json);
		}
		catch (Exception exception)
		{
			Log.Error("Unable to save session info file", exception);
		}
	}

	public void LoadInfo()
	{
		try
		{
			if (File.Exists(InfoFile))
			{
				FileInfo = JsonConvert.DeserializeObject<FileInfo>(File.ReadAllText(InfoFile));
			}
		}
		catch (Exception message)
		{
			Log.Error(message);
		}
	}

	public void SetFile(string file)
	{
		IsLoaded = false;
		FileName = file;
		Name = Path.GetFileName(file);
		ItemType = FileItemType.File;
		Visible = true;
	}

	public void AddTemplates(ICameraDevice device, PhotoSession session)
	{
		string[] skipItems = new string[15]
		{
			"[Session Name]", "[Counter 3 digit]", "[Counter 4 digit]", "[Counter 5 digit]", "[Counter 6 digit]", "[Counter 7 digit]", "[Counter 8 digit]", "[Counter 9 digit]", "[Camera Counter 3 digit]", "[Camera Counter 4 digit]",
			"[Camera Counter 5 digit]", "[Camera Counter 6 digit]", "[Camera Counter 7 digit]", "[Camera Counter 8 digit]", "[Camera Counter 9 digit]"
		};
		foreach (KeyValuePair<string, IFilenameTemplate> template in ServiceProvider.FilenameTemplateManager.Templates)
		{
			if (!skipItems.Contains(template.Key) && (!template.Value.IsRuntime || session.FileNameTemplate.Contains(template.Key)))
			{
				string val = template.Value.Pharse(template.Key, session, device, FileName, "");
				if (!string.IsNullOrWhiteSpace(val))
				{
					FileNameTemplates.Add(new ValuePair
					{
						Name = template.Key,
						Value = val
					});
				}
			}
		}
	}

	public bool HaveHistogramReady()
	{
		if (!ServiceProvider.Settings.UseHistogram)
		{
			return true;
		}
		if (FileInfo == null)
		{
			return false;
		}
		if (FileInfo.HistogramBlue == null || FileInfo.HistogramBlue.Length == 0)
		{
			return false;
		}
		if (FileInfo.ExifTags == null || FileInfo.ExifTags.Items.Count == 0)
		{
			return false;
		}
		return true;
	}

	public void Dispose()
	{
		CleanUp();
	}

	private void CleanUp()
	{
		if (!_disposed)
		{
			Thumbnail = null;
			ThumbnailMarks = null;
			ThumbData = null;
			if (_fileInfo is IDisposable finfo)
			{
				finfo.Dispose();
			}
			_fileInfo = null;
			FileNameTemplates?.Clear();
			FileNameTemplates = null;
			if (Device is IDisposable dev)
			{
				dev.Dispose();
			}
			Device = null;
			DeviceObject = null;
			_disposed = true;
		}
	}
}
