using System.IO;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using ImageMagick;

namespace CameraControl.Plugins.ImageTransformPlugins;

public class Effect : IImageTransformPlugin
{
	public string Name => "Effect";

	public string Execute(FileItem item, string infile, string dest, ValuePairEnumerator configData)
	{
		EffectViewModel conf = new EffectViewModel(configData);
		dest = Path.Combine(Path.GetDirectoryName(dest), Path.GetFileNameWithoutExtension(dest) + ".jpg");
		using (MagickImage image = new MagickImage(infile))
		{
			switch (conf.SelectedMode)
			{
			case 0:
				image.SepiaTone(new Percentage(conf.Param1));
				break;
			case 1:
				image.OilPaint(conf.Param1, 0.1);
				break;
			case 2:
				image.Sketch();
				break;
			case 3:
				image.Charcoal();
				break;
			case 4:
				image.Solarize();
				break;
			case 5:
				image.Swirl(conf.Param1);
				break;
			case 6:
				image.Wave(PixelInterpolateMethod.Bilinear, conf.Param1, conf.Param2);
				break;
			case 7:
				image.BlueShift();
				break;
			case 8:
				image.RotationalBlur(conf.Param1);
				break;
			case 9:
				image.Raise(conf.Param1);
				break;
			case 10:
				image.Negate();
				break;
			case 11:
				image.ColorSpace = ColorSpace.Gray;
				break;
			}
			image.Format = MagickFormat.Jpeg;
			image.Write(dest);
		}
		return dest;
	}

	public UserControl GetConfig(ValuePairEnumerator configData)
	{
		EffectView control = new EffectView();
		((FrameworkElement)control).DataContext = new EffectViewModel(configData);
		return (UserControl)(object)control;
	}
}
