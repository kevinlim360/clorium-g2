using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using Kaliko.ImageLibrary;
using Kaliko.ImageLibrary.Filters;
using Kaliko.ImageLibrary.Scaling;

namespace CameraControl.Plugins.ImageTransformPlugins;

public class Chromakey : IImageTransformPlugin
{
	public string Name => "Chromakey";

	public string Execute(FileItem item, string infile, string dest, ValuePairEnumerator configData)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			ChromakeyViewModel conf = new ChromakeyViewModel(configData);
			dest = Path.Combine(Path.GetDirectoryName(dest), Path.GetFileNameWithoutExtension(dest) + ".jpg");
			KalikoImage image = new KalikoImage(infile);
			Color x = (Color)ColorConverter.ConvertFromString(conf.BackgroundColor);
			ChromaKeyFilter filter = new ChromaKeyFilter();
			filter.KeyColor = Color.FromArgb(x.R, x.G, x.B);
			filter.ToleranceHue = conf.Hue;
			filter.ToleranceSaturnation = (float)conf.Saturnation / 100f;
			filter.ToleranceBrightness = (float)conf.Brigthness / 100f;
			image.ApplyFilter(filter);
			KalikoImage res = image.Clone();
			if (conf.UnsharpMask)
			{
				res.ApplyFilter(new UnsharpMaskFilter(1.4f, 1.32f, 5));
			}
			KalikoImage backdrop = new KalikoImage(conf.BackgroundFile);
			backdrop = backdrop.Scale(new FitScaling(image.Width, image.Height));
			backdrop.BlitImage(res);
			backdrop.SaveJpg(dest, 90L);
			return dest;
		}
		catch (Exception exception)
		{
			Log.Debug("Chromakey error", exception);
		}
		return null;
	}

	public UserControl GetConfig(ValuePairEnumerator configData)
	{
		ChromakeyView control = new ChromakeyView();
		((FrameworkElement)control).DataContext = new ChromakeyViewModel(configData);
		return (UserControl)(object)control;
	}
}
