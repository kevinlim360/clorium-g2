using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using ImageMagick;

namespace CameraControl.Plugins.ImageTransformPlugins;

public class RotateTransform : IImageTransformPlugin
{
	public string Name => "Rotate";

	public string Execute(FileItem item, string infile, string dest, ValuePairEnumerator configData)
	{
		RotateTransformViewModel conf = new RotateTransformViewModel(configData);
		using (MagickImage image = new MagickImage(infile))
		{
			image.BackgroundColor = new MagickColor(Color.Black.R, Color.Black.G, Color.Black.B);
			if (conf.ManualRotate)
			{
				image.Rotate(item.RotationAngle);
			}
			else if (conf.AutoRotate)
			{
				IExifProfile profile = image.GetExifProfile();
				image.AutoOrient();
				profile.SetValue<ushort>(ExifTag.Orientation, 0);
			}
			if (conf.Angle > 0)
			{
				image.Rotate(conf.Angle);
			}
			if (conf.FlipHorizontal)
			{
				image.Flop();
			}
			if (conf.FlipVertical)
			{
				image.Flip();
			}
			image.Format = MagickFormat.Jpeg;
			image.Write(dest);
		}
		return dest;
	}

	public UserControl GetConfig(ValuePairEnumerator configData)
	{
		RotateTransformView rotateTransformView = new RotateTransformView();
		((FrameworkElement)rotateTransformView).DataContext = new RotateTransformViewModel(configData);
		return (UserControl)(object)rotateTransformView;
	}
}
