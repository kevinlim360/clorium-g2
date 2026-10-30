using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using CameraControl.Devices.Classes;
using Newtonsoft.Json;

namespace CameraControl.Core.Classes;

public class FileInfo : BaseFieldClass
{
	private int _width;

	private int _height;

	private uint[] _histogramRed;

	private uint[] _histogramBlue;

	private uint[] _histogramGreen;

	private uint[] _histogramLuminance;

	public string InfoLabel { get; set; }

	public int Orientation { get; set; }

	[XmlIgnore]
	[JsonIgnore]
	public PointCollection LuminanceHistogramPoints => BitmapLoader.ConvertToPointCollection(HistogramLuminance);

	[XmlIgnore]
	[JsonIgnore]
	public bool IsFocused => FocusPoints != null && FocusPoints.Count > 0;

	public bool IsOverExposed => HistogramLuminance != null && HistogramLuminance.Length == 256 && HistogramLuminance[254] > 5 && HistogramLuminance[255] > 5;

	public bool IsUnderExposed => HistogramLuminance != null && HistogramLuminance.Length == 256 && HistogramLuminance[0] > 5 && HistogramLuminance[1] > 5;

	public int Width
	{
		get
		{
			if (_width == 0)
			{
				_width = 4000;
			}
			return _width;
		}
		set
		{
			_width = value;
			NotifyPropertyChanged("Width");
		}
	}

	public int Height
	{
		get
		{
			if (_height == 0)
			{
				_height = 3000;
			}
			return _height;
		}
		set
		{
			_height = value;
			NotifyPropertyChanged("Height");
		}
	}

	public ValuePairEnumerator ExifTags { get; set; }

	public List<Rect> FocusPoints { get; set; }

	public uint[] HistogramRed
	{
		get
		{
			return _histogramRed;
		}
		set
		{
			_histogramRed = value;
			NotifyPropertyChanged("HistogramRed");
		}
	}

	public uint[] HistogramBlue
	{
		get
		{
			return _histogramBlue;
		}
		set
		{
			_histogramBlue = value;
			NotifyPropertyChanged("HistogramBlue");
		}
	}

	public uint[] HistogramGreen
	{
		get
		{
			return _histogramGreen;
		}
		set
		{
			_histogramGreen = value;
			NotifyPropertyChanged("HistogramGreen");
		}
	}

	public uint[] HistogramLuminance
	{
		get
		{
			return _histogramLuminance;
		}
		set
		{
			_histogramLuminance = value;
			NotifyPropertyChanged("HistogramLuminance");
			NotifyPropertyChanged("LuminanceHistogramPoints");
		}
	}

	[XmlIgnore]
	[JsonIgnore]
	public bool IsLoading { get; set; }

	public FileInfo()
	{
		ExifTags = new ValuePairEnumerator();
		FocusPoints = new List<Rect>();
	}

	public void ValidateValues()
	{
		foreach (ValuePair exifTag in ExifTags.Items)
		{
			exifTag.Value = exifTag.Value.Replace('\0', ' ');
			exifTag.Value = CleanInvalidXmlChars(exifTag.Value);
		}
	}

	public static string CleanInvalidXmlChars(string text)
	{
		string re = "[^\\x07\\x09\\x0A\\x0D\\x20-\\uD7FF\\uE000-\\uFFFD\\u10000-\\u10FFFF]";
		return Regex.Replace(text, re, "");
	}

	public void SetSize(int w, int h)
	{
		_width = w;
		_height = h;
	}
}
