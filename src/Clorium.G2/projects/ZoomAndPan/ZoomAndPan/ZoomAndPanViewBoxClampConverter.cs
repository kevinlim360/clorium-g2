using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ZoomAndPan;

public class ZoomAndPanViewBoxClampConverter : MarkupExtension, IMultiValueConverter
{
	public override object ProvideValue(IServiceProvider serviceProvider)
	{
		return this;
	}

	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		try
		{
			ZoomAndPanControl zoomAndPanControl = (ZoomAndPanControl)values[3];
			if (values[0] == null || zoomAndPanControl == null)
			{
				return DependencyProperty.UnsetValue;
			}
			double val = (double)values[0];
			double num = (double)values[1];
			double num2 = (double)values[2];
			return Math.Max((parameter?.ToString().ToLower() == "width") ? Math.Min(zoomAndPanControl.ExtentWidth / num2 - num, val) : Math.Min(zoomAndPanControl.ExtentHeight / num2 - num, val), 0.0);
		}
		catch (Exception)
		{
		}
		return null;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
