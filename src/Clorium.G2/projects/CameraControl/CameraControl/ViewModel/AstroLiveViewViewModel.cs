using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Accord.Imaging.Filters;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using CameraControl.windows;

namespace CameraControl.ViewModel;

public class AstroLiveViewViewModel : LiveViewViewModel
{
	private object _locker = new object();

	private System.Windows.Point _centralPoint;

	private int _zoomFactor;

	private BitmapSource _starWindow;

	private System.Windows.Media.Color[] _lastBitmap;

	private int Threshold = 400;

	private double StarSizeOld = 0.0;

	private double K = 10.0;

	private double _starSize;

	private int _starWindowSize;

	private double _averageCount;

	private int _contrast;

	public int Contrast
	{
		get
		{
			return _contrast;
		}
		set
		{
			_contrast = value;
			RaisePropertyChanged(() => Contrast);
		}
	}

	public int StarWindowSize
	{
		get
		{
			return _starWindowSize;
		}
		set
		{
			_starWindowSize = value;
			RaisePropertyChanged(() => StarWindowSize);
		}
	}

	public double DisplayStarSize => Math.Round(StarSize, 2);

	public double StarSize
	{
		get
		{
			return _starSize;
		}
		set
		{
			_starSize = value;
			RaisePropertyChanged(() => StarSize);
			RaisePropertyChanged(() => DisplayStarSize);
		}
	}

	public System.Windows.Point CentralPoint
	{
		get
		{
			return _centralPoint;
		}
		set
		{
			_centralPoint = value;
			RaisePropertyChanged(() => CentralPoint);
		}
	}

	public int ZoomFactor
	{
		get
		{
			return _zoomFactor;
		}
		set
		{
			_zoomFactor = value;
			RaisePropertyChanged(() => ZoomFactor);
		}
	}

	public BitmapSource StarWindow
	{
		get
		{
			return _starWindow;
		}
		set
		{
			_starWindow = value;
			RaisePropertyChanged(() => StarWindow);
		}
	}

	public bool IsFullImage => CameraDevice.LiveViewImageZoomRatio.Value == "All";

	public double AverageCount
	{
		get
		{
			return _averageCount;
		}
		set
		{
			_averageCount = value;
			RaisePropertyChanged(() => AverageCount);
		}
	}

	public AstroLiveViewViewModel(ICameraDevice device, Window window)
		: base(device, window)
	{
		ZoomFactor = 1;
		StarWindowSize = 30;
		StarSize = 123.11;
	}

	public AstroLiveViewViewModel()
	{
	}

	public void CalculateStarSize(WriteableBitmap bitmap)
	{
		int Max = 0;
		int Min = 800;
		int Count = 0;
		for (int i = 0; i < StarWindowSize - 1; i++)
		{
			for (int j = 0; j < StarWindowSize - 1; j++)
			{
				System.Windows.Media.Color c = bitmap.GetPixel(i, j);
				int greyVal = c.R + c.G + c.B;
				if (greyVal > Threshold)
				{
					Count++;
				}
				if (greyVal > Max)
				{
					Max = greyVal;
				}
				if (greyVal < Min)
				{
					Min = greyVal;
				}
			}
		}
		Threshold = (Max + Min) / 2;
		StarSize = ((double)Count + K * StarSizeOld) / (K + 1.0);
		StarSizeOld = StarSize;
	}

	public override void GetLiveImage()
	{
		lock (_locker)
		{
			try
			{
				LiveViewData = LiveViewManager.GetLiveViewImage(CameraDevice);
			}
			catch (Exception)
			{
				return;
			}
			if (LiveViewData == null || LiveViewData.ImageData == null)
			{
				return;
			}
			MemoryStream stream = new MemoryStream(LiveViewData.ImageData, LiveViewData.ImageDataPosition, LiveViewData.ImageData.Length - LiveViewData.ImageDataPosition);
			using Bitmap bmp = new Bitmap(stream);
			Bitmap res = bmp;
			WriteableBitmap preview = BitmapFactory.ConvertToPbgra32Format(BitmapSourceConvert.ToBitmapSource(res));
			WriteableBitmap zoow = preview.Crop((int)(CentralPoint.X - (double)(StarWindowSize / 2)), (int)(CentralPoint.Y - (double)(StarWindowSize / 2)), StarWindowSize, StarWindowSize);
			CalculateStarSize(zoow);
			zoow.Freeze();
			StarWindow = zoow;
			if (CameraDevice.LiveViewImageZoomRatio.Value == "All")
			{
				preview.Freeze();
				Preview = preview;
			}
			if (Brightness != 0)
			{
				BrightnessCorrection filter = new BrightnessCorrection(Brightness);
				res = filter.Apply(res);
			}
			if (Contrast != 0)
			{
				ContrastCorrection contrastCorrection = new ContrastCorrection(Contrast);
				res = contrastCorrection.Apply(res);
			}
			if (EdgeDetection)
			{
				FiltersSequence filter2 = new FiltersSequence(Grayscale.CommonAlgorithms.BT709, new HomogenityEdgeDetector());
				res = filter2.Apply(res);
			}
			WriteableBitmap _bitmap = BitmapFactory.ConvertToPbgra32Format(BitmapSourceConvert.ToBitmapSource(res));
			AverageImage(_bitmap);
			DrawGrid(_bitmap);
			if (ZoomFactor > 1)
			{
				double d = (double)_bitmap.PixelWidth / (double)ZoomFactor;
				double h = (double)_bitmap.PixelHeight / (double)ZoomFactor;
				_bitmap = _bitmap.Crop((int)(CentralPoint.X - d / 2.0), (int)(CentralPoint.Y - h / 2.0), (int)d, (int)h);
			}
			_bitmap.Freeze();
			Bitmap = _bitmap;
		}
	}

	public override void SetFocusPos(int x, int y)
	{
		lock (_locker)
		{
			base.SetFocusPos(x, y);
		}
	}

	private static int ColorstoInt(byte A, byte R, byte G, byte B)
	{
		return (A << 24) | (R << 16) | (G << 8) | B;
	}

	private System.Windows.Media.Color IntToColor(int num1)
	{
		byte a = (byte)(num1 >> 24);
		int num2 = a;
		if (num2 == 0)
		{
			num2 = 1;
		}
		int num3 = 65280 / num2;
		byte R = (byte)(((num1 >> 16) & 0xFF) * num3 >> 8);
		byte G = (byte)(((num1 >> 8) & 0xFF) * num3 >> 8);
		byte B = (byte)((num1 & 0xFF) * num3 >> 8);
		return new System.Windows.Media.Color
		{
			A = a,
			B = B,
			G = G,
			R = R
		};
	}

	private unsafe void AverageImage(WriteableBitmap bitmap)
	{
		if (AverageCount == 0.0)
		{
			return;
		}
		if (_lastBitmap == null || bitmap.PixelWidth * bitmap.PixelHeight != _lastBitmap.Length)
		{
			_lastBitmap = new System.Windows.Media.Color[bitmap.PixelWidth * bitmap.PixelHeight];
			using BitmapContext bitmapContext = bitmap.GetBitmapContext();
			for (int i = 0; i < bitmapContext.Width * bitmapContext.Height; i++)
			{
				System.Windows.Media.Color c = IntToColor(bitmapContext.Pixels[i]);
				_lastBitmap[i] = c;
			}
			return;
		}
		try
		{
			using BitmapContext bitmapContext2 = bitmap.GetBitmapContext();
			for (int j = 0; j < bitmapContext2.Width * bitmapContext2.Height; j++)
			{
				System.Windows.Media.Color c2 = IntToColor(bitmapContext2.Pixels[j]);
				System.Windows.Media.Color oldc = _lastBitmap[j];
				c2.R = (byte)(((double)(int)c2.R + AverageCount * (double)(int)oldc.R) / (AverageCount + 1.0));
				c2.G = (byte)(((double)(int)c2.G + AverageCount * (double)(int)oldc.G) / (AverageCount + 1.0));
				c2.B = (byte)(((double)(int)c2.B + AverageCount * (double)(int)oldc.B) / (AverageCount + 1.0));
				bitmapContext2.Pixels[j] = ColorstoInt(c2.A, c2.R, c2.G, c2.B);
				_lastBitmap[j] = c2;
			}
		}
		catch (Exception)
		{
			Log.Error("Error average image");
		}
	}

	private void DrawGrid(WriteableBitmap bitmap)
	{
		if (CentralPoint.X == 0.0 && CentralPoint.Y == 0.0)
		{
			CentralPoint = new System.Windows.Point(bitmap.PixelWidth / 2, bitmap.PixelHeight / 2);
		}
		WriteableBitmap tempbitmap = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight, bitmap.DpiX, bitmap.DpiY, PixelFormats.Pbgra32, bitmap.Palette);
		tempbitmap.DrawLine(0, (int)CentralPoint.Y - StarWindowSize / 2, bitmap.PixelWidth, (int)CentralPoint.Y - StarWindowSize / 2, Colors.White);
		tempbitmap.DrawLine(0, (int)CentralPoint.Y + StarWindowSize / 2, bitmap.PixelWidth, (int)CentralPoint.Y + StarWindowSize / 2, Colors.White);
		tempbitmap.DrawLine((int)CentralPoint.X - StarWindowSize / 2, 0, (int)CentralPoint.X - StarWindowSize / 2, bitmap.PixelHeight, Colors.White);
		tempbitmap.DrawLine((int)CentralPoint.X + StarWindowSize / 2, 0, (int)CentralPoint.X + StarWindowSize / 2, bitmap.PixelHeight, Colors.White);
		bitmap.Blit(new Rect(0.0, 0.0, bitmap.PixelWidth, bitmap.PixelHeight), tempbitmap, new Rect(0.0, 0.0, bitmap.PixelWidth, bitmap.PixelHeight));
	}
}
