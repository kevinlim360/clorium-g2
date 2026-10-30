using System.IO;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using ImageMagick;

namespace CameraControl.Plugins.ImageTransformPlugins;

public class PixelBinning : IImageTransformPlugin
{
	public string Name => "Pixel Binning";

	public string Execute(FileItem item, string infile, string dest, ValuePairEnumerator configData)
	{
		PixelBinningViewModel conf = new PixelBinningViewModel(configData);
		dest = Path.Combine(Path.GetDirectoryName(dest), Path.GetFileNameWithoutExtension(dest) + ".jpg");
		using (MagickImage image = new MagickImage(infile))
		{
			int newx = (int)image.Width / (conf.SelectedMode + 2);
			int newy = (int)image.Height / (conf.SelectedMode + 2);
			int cropx = newx * (conf.SelectedMode + 2);
			int cropy = newy * (conf.SelectedMode + 2);
			if (cropx != image.Width || cropy != image.Height)
			{
				image.Crop((uint)cropx, (uint)cropy, Gravity.Center);
			}
			image.FilterType = FilterType.Box;
			image.Resize((uint)newx, (uint)newy);
			image.Format = MagickFormat.Jpeg;
			image.Write(dest);
		}
		return dest;
	}

	public UserControl GetConfig(ValuePairEnumerator configData)
	{
		PixelBinningView control = new PixelBinningView();
		((FrameworkElement)control).DataContext = new PixelBinningViewModel(configData);
		return (UserControl)(object)control;
	}
}
