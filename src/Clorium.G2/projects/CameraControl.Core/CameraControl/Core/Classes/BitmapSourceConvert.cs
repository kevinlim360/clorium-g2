using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace CameraControl.Core.Classes;

public static class BitmapSourceConvert
{
	[DllImport("gdi32")]
	private static extern int DeleteObject(IntPtr o);

	public static BitmapSource ToBitmapSource(Bitmap source)
	{
		IntPtr ptr = source.GetHbitmap();
		BitmapSource bs = Imaging.CreateBitmapSourceFromHBitmap(ptr, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		DeleteObject(ptr);
		bs.Freeze();
		return bs;
	}

	public static WriteableBitmap CreateWriteableBitmapFromBitmap(Bitmap bitmap)
	{
		if (bitmap == null)
		{
			throw new ArgumentNullException("bitmap");
		}
		using MemoryStream memoryStream = new MemoryStream();
		try
		{
			bitmap.Save(memoryStream, ImageFormat.Png);
			memoryStream.Seek(0L, SeekOrigin.Begin);
			BitmapDecoder bitmapDecoder = BitmapDecoder.Create(memoryStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
			WriteableBitmap writable = new WriteableBitmap(BitmapFactory.ConvertToPbgra32Format(bitmapDecoder.Frames.Single()));
			writable.Freeze();
			return writable;
		}
		catch (Exception)
		{
			return null;
		}
	}
}
