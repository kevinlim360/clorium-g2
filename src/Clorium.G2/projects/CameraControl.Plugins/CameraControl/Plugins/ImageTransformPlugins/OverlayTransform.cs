using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using ImageMagick;

namespace CameraControl.Plugins.ImageTransformPlugins;

public class OverlayTransform : IImageTransformPlugin
{
	public string Name => "Overlay";

	public string Execute(FileItem item, string infile, string dest, ValuePairEnumerator configData)
	{
		return ExecuteThread(item, infile, dest, configData);
	}

	public string ExecuteThread(FileItem item, string infile, string dest, ValuePairEnumerator configData)
	{
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		OverlayTransformViewModel conf = new OverlayTransformViewModel(configData);
		using (MagickImage image = new MagickImage(infile))
		{
			Gravity gravity = Gravity.Center;
			if (conf.A11)
			{
				gravity = Gravity.Northwest;
			}
			if (conf.A12)
			{
				gravity = Gravity.North;
			}
			if (conf.A13)
			{
				gravity = Gravity.Northeast;
			}
			if (conf.A21)
			{
				gravity = Gravity.West;
			}
			if (conf.A22)
			{
				gravity = Gravity.Center;
			}
			if (conf.A23)
			{
				gravity = Gravity.East;
			}
			if (conf.A31)
			{
				gravity = Gravity.Southwest;
			}
			if (conf.A32)
			{
				gravity = Gravity.South;
			}
			if (conf.A33)
			{
				gravity = Gravity.Southeast;
			}
			if (File.Exists(conf.OverlayFile))
			{
				using MagickImage watermark = new MagickImage(conf.OverlayFile);
				if (conf.StrechOverlay)
				{
					watermark.Resize(image.Width, image.Height);
				}
				if (conf.Transparency != 100)
				{
					watermark.Evaluate(Channels.Alpha, EvaluateOperator.Add, -(255 * (100 - conf.Transparency) / 100));
				}
				image.Composite(watermark, gravity, CompositeOperator.Over);
			}
			string text = "";
			if (!string.IsNullOrEmpty(conf.Text))
			{
				Regex regPattern = new Regex("\\[(.*?)\\]", RegexOptions.Singleline);
				MatchCollection matchX = regPattern.Matches(conf.Text);
				text = matchX.Cast<Match>().Aggregate(conf.Text, (string current1, Match match) => item.FileNameTemplates.Where((ValuePair template) => string.Compare(template.Name, match.Value, StringComparison.InvariantCultureIgnoreCase) == 0).Aggregate(current1, (string text2, ValuePair template) => text2.Replace(match.Value, template.Value)));
				image.Settings.Font = conf.Font;
				image.Settings.FontPointsize = conf.FontSize;
				Color color = (Color)ColorConverter.ConvertFromString(conf.FontColor);
				image.Settings.FillColor = new MagickColor(color.R, color.G, color.B, color.A);
				image.Settings.StrokeColor = new MagickColor(color.R, color.G, color.B, color.A);
				image.Annotate(text, gravity);
			}
			image.Format = MagickFormat.Jpeg;
			image.Write(dest);
		}
		return dest;
	}

	public UserControl GetConfig(ValuePairEnumerator configData)
	{
		OverlayTransformView overlayTransformView = new OverlayTransformView();
		((FrameworkElement)overlayTransformView).DataContext = new OverlayTransformViewModel(configData);
		return (UserControl)(object)overlayTransformView;
	}
}
