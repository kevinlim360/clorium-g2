using System.IO;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using ImageMagick;

namespace CameraControl.Plugins.ImageTransformPlugins;

internal class ResizeTransform : IImageTransformPlugin
{
	public string Name => "Resize";

	public string Execute(FileItem fileItem, string infile, string dest, ValuePairEnumerator configData)
	{
		ResizeTransformViewModel conf = new ResizeTransformViewModel(configData);
		dest = Path.Combine(Path.GetDirectoryName(dest), Path.GetFileNameWithoutExtension(dest) + ".jpg");
		using (MagickImage image = new MagickImage(infile))
		{
			MagickGeometry geometry = new MagickGeometry((uint)conf.Width, (uint)conf.Height);
			geometry.IgnoreAspectRatio = !conf.KeepAspect;
			image.Resize(geometry);
			image.Format = MagickFormat.Jpeg;
			image.Write(dest);
		}
		return dest;
	}

	public UserControl GetConfig(ValuePairEnumerator configData)
	{
		ResizeTransformView control = new ResizeTransformView();
		((FrameworkElement)control).DataContext = new ResizeTransformViewModel(configData);
		return (UserControl)(object)control;
	}
}
