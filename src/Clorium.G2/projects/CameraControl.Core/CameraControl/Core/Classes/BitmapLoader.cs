using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using ImageMagick;

namespace CameraControl.Core.Classes;

public class BitmapLoader : BaseFieldClass
{
	public delegate void MetaDataUpdatedEventHandler(object sender, FileItem item);

	public const int LargeThumbSize = 1600;

	public const int SmallThumbSize = 400;

	private static BitmapLoader _instance;

	private BitmapImage _defaultThumbnail;

	private BitmapImage _noImageThumbnail;

	public static BitmapLoader Instance
	{
		get
		{
			return _instance ?? (_instance = new BitmapLoader());
		}
		set
		{
			_instance = value;
		}
	}

	public BitmapImage DefaultThumbnail
	{
		get
		{
			if (_defaultThumbnail == null)
			{
				if (!string.IsNullOrEmpty(ServiceProvider.Branding.DefaultThumbImage) && File.Exists(ServiceProvider.Branding.DefaultThumbImage))
				{
					BitmapImage bi = new BitmapImage();
					bi.BeginInit();
					bi.CacheOption = BitmapCacheOption.OnLoad;
					bi.UriSource = new Uri(PhotoUtils.GetFullPath(ServiceProvider.Branding.DefaultThumbImage));
					bi.EndInit();
					bi.Freeze();
					_defaultThumbnail = bi;
				}
				else
				{
					_defaultThumbnail = new BitmapImage(new Uri("pack://application:,,,/Images/logo_big.png"));
				}
			}
			return _defaultThumbnail;
		}
		set
		{
			_defaultThumbnail = value;
		}
	}

	public BitmapImage NoImageThumbnail
	{
		get
		{
			if (_noImageThumbnail == null)
			{
				if (!string.IsNullOrEmpty(ServiceProvider.Branding.DefaultMissingThumbImage) && File.Exists(ServiceProvider.Branding.DefaultMissingThumbImage))
				{
					BitmapImage bi = new BitmapImage();
					bi.BeginInit();
					bi.UriSource = new Uri(ServiceProvider.Branding.DefaultMissingThumbImage);
					bi.EndInit();
					bi.Freeze();
					_noImageThumbnail = bi;
				}
				else
				{
					_noImageThumbnail = new BitmapImage(new Uri("pack://application:,,,/Images/NoImage.png"));
				}
			}
			return _noImageThumbnail;
		}
		set
		{
			_noImageThumbnail = value;
		}
	}

	public event MetaDataUpdatedEventHandler MetaDataUpdated;

	[HandleProcessCorruptedStateExceptions]
	public void GenerateCache(FileItem fileItem)
	{
		bool deleteFile = false;
		if (fileItem == null || !File.Exists(fileItem.FileName) || (File.Exists(fileItem.LargeThumb) && File.Exists(fileItem.SmallThumb) && File.Exists(fileItem.InfoFile)) || fileItem.Loading)
		{
			return;
		}
		fileItem.Loading = true;
		PhotoUtils.WaitForFile(fileItem.FileName);
		string filename = fileItem.FileName;
		if (fileItem.IsMovie)
		{
			try
			{
				string ffmpeg_exe = Path.Combine(Settings.ApplicationFolder, "ffmpeg.exe");
				if (File.Exists(ffmpeg_exe))
				{
					string thumb = Path.Combine(Path.GetDirectoryName(fileItem.FileName), Path.GetFileNameWithoutExtension(fileItem.FileName) + ".thumb.jpg");
					PhotoUtils.RunAndWait(ffmpeg_exe, $"-i \"{fileItem.FileName}\" -ss 00:00:01.000 -f image2 -vframes 1 \"{thumb}\"");
					if (File.Exists(thumb))
					{
						deleteFile = true;
						filename = thumb;
					}
				}
			}
			catch (Exception exception)
			{
				Log.Error("Error get video thumb", exception);
			}
		}
		if (fileItem.IsRaw)
		{
			try
			{
				string dcraw_exe = Path.Combine(Settings.ApplicationFolder, "dcraw.exe");
				if (File.Exists(dcraw_exe))
				{
					string thumb2 = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fileItem.FileName) + ".thumb.jpg");
					PhotoUtils.RunAndWait(dcraw_exe, $" -e -O \"{thumb2}\" \"{fileItem.FileName}\"");
					if (File.Exists(thumb2))
					{
						deleteFile = true;
						filename = thumb2;
					}
				}
			}
			catch (Exception exception2)
			{
				Log.Error("Error get dcraw thumb", exception2);
			}
		}
		GetMetadata(fileItem);
		try
		{
			ServiceProvider.Settings.StartElapse();
			using (MagickImage image = new MagickImage(filename))
			{
				fileItem.FileInfo.SetSize((int)image.Width, (int)image.Height);
				double largeDw = 1600.0 / (double)image.Width;
				using (IMagickImage<byte> largeThumb = image.Clone())
				{
					largeThumb.FilterType = FilterType.Box;
					largeThumb.Thumbnail((uint)((double)image.Width * largeDw), (uint)((double)image.Height * largeDw));
					if (!ServiceProvider.Settings.DisableHardwareAccelerationNew)
					{
						largeThumb.UnsharpMask(1.0, 1.0, 0.5, 0.1);
					}
					PhotoUtils.CreateFolder(fileItem.LargeThumb);
					largeThumb.Write(fileItem.LargeThumb);
				}
				double smallDw = 400.0 / (double)image.Width;
				using IMagickImage<byte> smallThumb = image.Clone();
				smallThumb.FilterType = FilterType.Box;
				smallThumb.Thumbnail((uint)((double)image.Width * smallDw), (uint)((double)image.Height * smallDw));
				if (!ServiceProvider.Settings.DisableHardwareAccelerationNew)
				{
					smallThumb.UnsharpMask(1.0, 1.0, 0.5, 0.1);
				}
				PhotoUtils.CreateFolder(fileItem.SmallThumb);
				smallThumb.Write(fileItem.SmallThumb);
			}
			fileItem.Thumbnail = LoadImage(fileItem.SmallThumb);
			fileItem.SaveInfo();
			SetImageInfo(fileItem);
			OnMetaDataUpdated(fileItem);
			if (deleteFile)
			{
				File.Delete(filename);
			}
			ServiceProvider.Settings.EndElapse();
			Log.Debug("Thumbnail Time : " + ServiceProvider.Settings.diffTime + "msec\n");
		}
		catch (Exception exception3)
		{
			Log.Error("Thumbnail Error : " + fileItem.FileName, exception3);
		}
		finally
		{
			fileItem.Loading = false;
			fileItem.IsLoaded = true;
			ServiceProvider.Settings.ThumbailLoaded = true;
		}
	}

	public void GenerateOverlayFile(string filename)
	{
		try
		{
			using MagickImage image = new MagickImage(filename);
			double dw = 1600.0 / (double)image.Width;
			image.FilterType = FilterType.Box;
			image.Thumbnail((uint)((double)image.Width * dw), (uint)((double)image.Height * dw));
			if (!ServiceProvider.Settings.DisableHardwareAccelerationNew)
			{
				image.UnsharpMask(1.0, 1.0, 0.5, 0.1);
			}
			string newname = Path.GetFileNameWithoutExtension(filename);
			newname += ".png";
			PhotoUtils.CreateFolder(ServiceProvider.Settings.OverlayFolder);
			image.Write(ServiceProvider.Settings.OverlayFolder + "\\" + newname);
		}
		catch (Exception exception)
		{
			Log.Error("오버레이용 png 이미지 생성 에러 : " + filename, exception);
		}
	}

	public static BitmapFrame GetBitmapFrame(BitmapFrame photo, int width, int height, BitmapScalingMode mode)
	{
		TransformedBitmap target = new TransformedBitmap(photo, new ScaleTransform((double)width / photo.Width * 96.0 / photo.DpiX, (double)height / photo.Height * 96.0 / photo.DpiY, 0.0, 0.0));
		BitmapFrame thumbnail = BitmapFrame.Create(target);
		return Resize(thumbnail, width, height, mode);
	}

	private static BitmapFrame Rotate(BitmapFrame photo, double angle, BitmapScalingMode mode)
	{
		TransformedBitmap target = new TransformedBitmap(photo, new RotateTransform(angle, photo.PixelWidth / 2, photo.PixelHeight / 2));
		BitmapFrame thumbnail = BitmapFrame.Create(target);
		return Resize(thumbnail, photo.PixelWidth, photo.PixelHeight, mode);
	}

	private static BitmapFrame Resize(BitmapFrame photo, int width, int height, BitmapScalingMode scalingMode)
	{
		DrawingGroup group = new DrawingGroup();
		RenderOptions.SetBitmapScalingMode(group, scalingMode);
		group.Children.Add(new ImageDrawing(photo, new Rect(0.0, 0.0, width, height)));
		DrawingVisual targetVisual = new DrawingVisual();
		DrawingContext targetContext = targetVisual.RenderOpen();
		targetContext.DrawDrawing(group);
		RenderTargetBitmap target = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Default);
		targetContext.Close();
		target.Render(targetVisual);
		return BitmapFrame.Create(target);
	}

	public static BitmapSource SaveImageSource(FrameworkElement obj, int width, int height)
	{
		obj.LayoutTransform = null;
		obj.Width = width;
		obj.Height = height;
		obj.UpdateLayout();
		obj.UpdateLayout();
		Thickness margin = obj.Margin;
		obj.Margin = new Thickness(0.0, 0.0, margin.Right - margin.Left, margin.Bottom - margin.Top);
		Size size = new Size(width, height);
		obj.Measure(size);
		obj.Arrange(new Rect(size));
		RenderTargetBitmap bmp = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
		bmp.Render(obj);
		return bmp;
	}

	public static void Save2Jpg(BitmapSource source, string filename)
	{
		string dir = Path.GetDirectoryName(filename);
		if (dir != null && !Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
		using FileStream stream = new FileStream(filename, FileMode.Create);
		JpegBitmapEncoder encoder = new JpegBitmapEncoder();
		encoder.Frames.Add(BitmapFrame.Create(source));
		encoder.QualityLevel = 90;
		encoder.Save(stream);
		stream.Close();
	}

	public void LoadHistogram(FileItem item)
	{
		try
		{
			FileInfo fileInfo = item.FileInfo;
			if (fileInfo != null)
			{
				if (fileInfo.IsLoading)
				{
					return;
				}
				fileInfo.IsLoading = true;
			}
			if (fileInfo == null || fileInfo.ExifTags == null || fileInfo.ExifTags.Items.Count == 0)
			{
				GetMetadata(item);
				fileInfo = item.FileInfo;
				fileInfo.IsLoading = true;
			}
			if (!File.Exists(item.SmallThumb))
			{
				fileInfo.IsLoading = false;
				return;
			}
			using (MagickImage image = new MagickImage(item.SmallThumb))
			{
				uint[] Blue = new uint[256];
				uint[] Green = new uint[256];
				uint[] Red = new uint[256];
				uint[] Luminance = new uint[256];
				IReadOnlyDictionary<IMagickColor<byte>, uint> h = image.Histogram();
				foreach (KeyValuePair<IMagickColor<byte>, uint> i in h)
				{
					byte R = i.Key.R;
					byte G = i.Key.G;
					byte B = i.Key.B;
					Blue[B] += i.Value;
					Green[G] += i.Value;
					Red[R] += i.Value;
					int lum = R + R + R + B + G + G + G + G >> 3;
					Luminance[lum] += i.Value;
				}
				fileInfo.HistogramBlue = Blue;
				fileInfo.HistogramGreen = Green;
				fileInfo.HistogramRed = Red;
				fileInfo.HistogramLuminance = Luminance;
				fileInfo.IsLoading = false;
				item.FileInfo = fileInfo;
			}
			item.SaveInfo();
			if (ServiceProvider.Settings.SelectedBitmap.FileItem == item)
			{
				SetData(ServiceProvider.Settings.SelectedBitmap, ServiceProvider.Settings.SelectedBitmap.FileItem);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Unable to load histogram", exception);
		}
	}

	public static void Highlight(BitmapFile file, bool under, bool over)
	{
		if ((under || over) && file != null && file.DisplayImage != null)
		{
			WriteableBitmap bitmap = Highlight(file.DisplayImage.Clone(), under, over);
			bitmap.Freeze();
			file.DisplayImage = bitmap;
		}
	}

	public unsafe static WriteableBitmap Highlight(WriteableBitmap bitmap, bool under, bool over)
	{
		if (!under && !over)
		{
			return bitmap;
		}
		int color1 = ConvertColor(Colors.Blue);
		int color2 = ConvertColor(Colors.Red);
		int treshold = 2;
		using (BitmapContext bitmapContext = bitmap.GetBitmapContext())
		{
			for (int i = 0; i < bitmapContext.Width * bitmapContext.Height; i++)
			{
				int num1 = bitmapContext.Pixels[i];
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
				if (under && R < treshold && G < treshold && B < treshold)
				{
					bitmapContext.Pixels[i] = color1;
				}
				if (over && R > 255 - treshold && G > 255 - treshold && B > 255 - treshold)
				{
					bitmapContext.Pixels[i] = color2;
				}
			}
		}
		return bitmap;
	}

	public void SetData(BitmapFile file, FileItem fileItem)
	{
		if (fileItem == null || fileItem.FileInfo == null)
		{
			return;
		}
		fileItem.LoadInfo();
		file.FileName = Path.GetFileNameWithoutExtension(fileItem.FileName);
		file.Comment = "";
		if (fileItem.FileInfo.ExifTags.ContainName("Iptc.Application2.Caption"))
		{
			file.Comment = fileItem.FileInfo.ExifTags["Iptc.Application2.Caption"];
		}
		file.Metadata.Clear();
		List<ValuePair> tagsCopy = fileItem.FileInfo.ExifTags.Items.ToList();
		foreach (ValuePair item in tagsCopy)
		{
			file.Metadata.Add(new DictionaryItem
			{
				Name = item.Name,
				Value = item.Value
			});
		}
		file.BlueColorHistogramPoints = ConvertToPointCollection(fileItem.FileInfo.HistogramBlue);
		file.RedColorHistogramPoints = ConvertToPointCollection(fileItem.FileInfo.HistogramRed);
		file.GreenColorHistogramPoints = ConvertToPointCollection(fileItem.FileInfo.HistogramGreen);
		file.LuminanceHistogramPoints = ConvertToPointCollection(fileItem.FileInfo.HistogramLuminance);
		file.InfoLabel = "";
		if (fileItem.FileInfo.ExifTags.ContainName("Exif.Photo.ExposureTime"))
		{
			file.InfoLabel = file.InfoLabel + "E " + fileItem.FileInfo.ExifTags["Exif.Photo.ExposureTime"];
		}
		if (fileItem.FileInfo.ExifTags.ContainName("Exif.Photo.FNumber"))
		{
			file.InfoLabel = file.InfoLabel + " | " + fileItem.FileInfo.ExifTags["Exif.Photo.FNumber"];
		}
		if (fileItem.FileInfo.ExifTags.ContainName("Exif.Photo.ISOSpeedRatings"))
		{
			file.InfoLabel = file.InfoLabel + " | ISO " + fileItem.FileInfo.ExifTags["Exif.Photo.ISOSpeedRatings"];
		}
		if (fileItem.FileInfo.ExifTags.ContainName("Exif.Photo.FocalLength"))
		{
			file.InfoLabel = file.InfoLabel + " | " + fileItem.FileInfo.ExifTags["Exif.Photo.FocalLength"];
		}
		SetImageInfo(fileItem);
	}

	public void SetImageInfo(FileItem fileItem)
	{
		if (fileItem != null && fileItem.FileInfo != null)
		{
			if (fileItem.FileInfo.ExifTags.ContainName("Exif.Photo.ExposureTime"))
			{
				fileItem.E = fileItem.FileInfo.ExifTags["Exif.Photo.ExposureTime"];
			}
			if (fileItem.FileInfo.ExifTags.ContainName("Exif.Photo.FNumber"))
			{
				fileItem.F = fileItem.FileInfo.ExifTags["Exif.Photo.FNumber"].Replace("F", "");
			}
			if (fileItem.FileInfo.ExifTags.ContainName("Exif.Photo.ISOSpeedRatings"))
			{
				fileItem.Iso = fileItem.FileInfo.ExifTags["Exif.Photo.ISOSpeedRatings"];
			}
			if (fileItem.FileInfo.ExifTags.ContainName("Exif.Photo.ExposureBiasValue"))
			{
				fileItem.ExposureBias = fileItem.FileInfo.ExifTags["Exif.Photo.ExposureBiasValue"];
			}
			if (fileItem.FileInfo.ExifTags.ContainName("Exif.Photo.FocalLength"))
			{
				fileItem.FocalLength = fileItem.FileInfo.ExifTags["Exif.Photo.FocalLength"];
			}
		}
	}

	public BitmapSource LoadImage(string filename)
	{
		BitmapImage bi = new BitmapImage();
		bi.BeginInit();
		bi.CacheOption = BitmapCacheOption.OnLoad;
		bi.UriSource = new Uri(filename);
		bi.EndInit();
		bi.Freeze();
		return bi;
	}

	public BitmapSource LoadImage(string filename, int width, int rotateAngle)
	{
		BitmapImage bi = new BitmapImage();
		bi.BeginInit();
		if (width > 0)
		{
			bi.DecodePixelWidth = width;
		}
		bi.CacheOption = BitmapCacheOption.OnLoad;
		bi.UriSource = new Uri(filename);
		bi.EndInit();
		bi.Freeze();
		WriteableBitmap bitmap = BitmapFactory.ConvertToPbgra32Format(bi);
		if (rotateAngle != 0)
		{
			bitmap = bitmap.Rotate(rotateAngle);
		}
		bitmap.Freeze();
		return bitmap;
	}

	public WriteableBitmap LoadImage(FileItem fileItem, bool fullres)
	{
		return LoadImage(fileItem, fullres, ServiceProvider.Settings.ShowFocusPoints);
	}

	[HandleProcessCorruptedStateExceptions]
	public WriteableBitmap LoadImage(FileItem fileItem, bool fullres, bool showfocuspoints)
	{
		if (fileItem == null)
		{
			return null;
		}
		if (!File.Exists(fileItem.LargeThumb) && !fullres)
		{
			return null;
		}
		if (File.Exists(fileItem.InfoFile))
		{
			fileItem.LoadInfo();
		}
		if (fileItem.FileInfo == null)
		{
			fileItem.FileInfo = new FileInfo();
		}
		try
		{
			BitmapDecoder bmpDec = null;
			if (fullres && fileItem.IsRaw)
			{
				try
				{
					string dcraw_exe = Path.Combine(Settings.ApplicationFolder, "dcraw.exe");
					if (File.Exists(dcraw_exe))
					{
						string thumb = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fileItem.FileName) + ".thumb.jpg");
						PhotoUtils.RunAndWait(dcraw_exe, $" -e -O \"{thumb}\" \"{fileItem.FileName}\"");
						if (File.Exists(thumb))
						{
							bmpDec = BitmapDecoder.Create(new Uri(thumb), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
							File.Delete(thumb);
						}
					}
				}
				catch (Exception exception)
				{
					Log.Error("Error get dcraw thumb", exception);
				}
			}
			PhotoUtils.WaitForFile(fullres ? fileItem.FileName : fileItem.LargeThumb);
			if (bmpDec == null)
			{
				bmpDec = BitmapDecoder.Create(new Uri(fullres ? fileItem.FileName : fileItem.LargeThumb), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
			}
			if (!showfocuspoints && ServiceProvider.Settings.RotateIndex == 0 && !ServiceProvider.Settings.FlipPreview)
			{
				fileItem.Loading = false;
				WriteableBitmap b = new WriteableBitmap(bmpDec.Frames[0]);
				b.Freeze();
				return b;
			}
			WriteableBitmap bitmap = BitmapFactory.ConvertToPbgra32Format(bmpDec.Frames[0]);
			if (showfocuspoints && !fileItem.Transformed)
			{
				DrawFocusPoints(fileItem, bitmap);
			}
			if (ServiceProvider.Settings.FlipPreview)
			{
				bitmap = bitmap.Flip(WriteableBitmapExtensions.FlipMode.Vertical);
			}
			bitmap.Freeze();
			return bitmap;
		}
		catch (Exception ex)
		{
			Log.Error("Error loading image", ex);
			if ((ex.GetType() == typeof(OutOfMemoryException)) & fullres)
			{
				return LoadImage(fileItem, fullres: false);
			}
		}
		return null;
	}

	public WriteableBitmap LoadSmallImage(FileItem fileItem, int width = 0)
	{
		if (!File.Exists(fileItem.SmallThumb))
		{
			return null;
		}
		PhotoUtils.WaitForFile(fileItem.SmallThumb);
		GetMetadata(fileItem);
		SetImageInfo(fileItem);
		try
		{
			BitmapImage bi = new BitmapImage();
			bi.BeginInit();
			if (width > 0)
			{
				bi.DecodePixelWidth = width;
			}
			bi.CacheOption = BitmapCacheOption.OnLoad;
			bi.UriSource = new Uri(fileItem.SmallThumb);
			bi.EndInit();
			bi.Freeze();
			return BitmapFactory.ConvertToPbgra32Format(bi);
		}
		catch (Exception exception)
		{
			Log.Error("Error loading image", exception);
		}
		return null;
	}

	public void GetMetadata(FileItem fileItem)
	{
		try
		{
			PhotoUtils.WaitForFile(fileItem.FileName);
			Exiv2Helper exiv2Helper = new Exiv2Helper();
			exiv2Helper.Load(fileItem);
			if (fileItem.FileInfo.ExifTags.ContainName("Exif.Image.Orientation"))
			{
				if (fileItem.FileInfo.ExifTags["Exif.Image.Orientation"] == "bottom, right")
				{
					fileItem.AutoRotation = 2;
				}
				if (fileItem.FileInfo.ExifTags["Exif.Image.Orientation"] == "right, top")
				{
					fileItem.AutoRotation = 1;
				}
				if (fileItem.FileInfo.ExifTags["Exif.Image.Orientation"] == "left, bottom")
				{
					fileItem.AutoRotation = 3;
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("Error loading metadata ", exception);
		}
	}

	public void DrawFocusPoints(FileItem fileItem, WriteableBitmap bitmap)
	{
		if (fileItem.FileInfo == null || fileItem.FileInfo.Width == 0 || fileItem.FileInfo.Height == 0)
		{
			return;
		}
		bitmap.Lock();
		double dw = (double)bitmap.PixelWidth / (double)fileItem.FileInfo.Width;
		double dh = (double)bitmap.PixelHeight / (double)fileItem.FileInfo.Height;
		foreach (Rect focuspoint in fileItem.FileInfo.FocusPoints)
		{
			DrawRect(bitmap, (int)(focuspoint.X * dw), (int)(focuspoint.Y * dh), (int)((focuspoint.X + focuspoint.Width) * dw), (int)((focuspoint.Y + focuspoint.Height) * dh), Colors.Aqua, bitmap.PixelWidth / 1000 + 2);
		}
		bitmap.Unlock();
	}

	private void DrawRect(WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color, int line)
	{
		DrawFocusRect(bmp, x1, y1, x2, y2, color, line);
	}

	private void DrawFocusRect(WriteableBitmap bmp, int x1, int y1, int x2, int y2, Color color, int tick)
	{
		int width = (x2 - x1) / 4;
		int height = (y2 - y1) / 4;
		DrawLineEx(bmp, x1 - tick / 2, y1, width, 0, color, tick);
		DrawLineEx(bmp, x1, y1, 0, height, color, tick);
		DrawLineEx(bmp, x1, y2, width, 0, color, tick);
		DrawLineEx(bmp, x1 + tick / 2, y2, 0, -height, color, tick);
		DrawLineEx(bmp, x2, y1, -width, 0, color, tick);
		DrawLineEx(bmp, x2 - tick / 2, y1, 0, height, color, tick);
		DrawLineEx(bmp, x2 + tick / 2, y2, -width, 0, color, tick);
		DrawLineEx(bmp, x2, y2, 0, -height, color, tick);
	}

	public void DrawLineEx(WriteableBitmap bmp, int x, int y, int width, int height, Color color, int tick)
	{
		bmp.DrawLineAa(x, y, x + width, y + height, color, tick);
	}

	public static PointCollection ConvertToPointCollection(uint[] values)
	{
		PointCollection points = new PointCollection();
		if (values == null)
		{
			points.Freeze();
			return points;
		}
		values = SmoothHistogram(values);
		uint max = values.Max();
		points.Add(new Point(0.0, max));
		for (int i = 0; i < values.Length; i++)
		{
			points.Add(new Point(i, max - values[i]));
		}
		points.Add(new Point(values.Length - 1, max));
		points.Freeze();
		return points;
	}

	private static uint[] SmoothHistogram(uint[] originalValues)
	{
		uint[] smoothedValues = new uint[originalValues.Length];
		double[] mask = new double[3] { 0.25, 0.5, 0.25 };
		for (int bin = 1; bin < originalValues.Length - 1; bin++)
		{
			double smoothedValue = 0.0;
			for (int i = 0; i < mask.Length; i++)
			{
				smoothedValue += (double)originalValues[bin - 1 + i] * mask[i];
			}
			smoothedValues[bin] = (uint)smoothedValue;
		}
		return smoothedValues;
	}

	private static int ConvertColor(Color color)
	{
		int num = color.A + 1;
		return (color.A << 24) | ((byte)(color.R * num >> 8) << 16) | ((byte)(color.G * num >> 8) << 8) | (byte)(color.B * num >> 8);
	}

	protected virtual void OnMetaDataUpdated(FileItem item)
	{
		MetaDataUpdated?.Invoke(this, item);
	}

	private static bool HasJpegHeader(Stream stream)
	{
		try
		{
			using BinaryReader br = new BinaryReader(stream);
			ushort soi = br.ReadUInt16();
			ushort jfif = br.ReadUInt16();
			return soi == 55551 && (jfif == 57599 || jfif == 57855 || jfif == 61183);
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static bool IsCompleteJPG(Stream stream)
	{
		try
		{
			if (HasJpegHeader(stream))
			{
				int maxBytestoTest = 20480;
				long numBytes = stream.Length;
				long offset = 2L;
				byte[] last_bytes = new byte[2];
				using BinaryReader reader = new BinaryReader(stream);
				do
				{
					reader.BaseStream.Seek(-1 * offset, SeekOrigin.End);
					reader.Read(last_bytes, 0, 2);
					if (last_bytes[0] == byte.MaxValue && last_bytes[1] == 217)
					{
						return true;
					}
					offset++;
				}
				while (offset < maxBytestoTest);
			}
		}
		catch (Exception)
		{
			return false;
		}
		return false;
	}
}
