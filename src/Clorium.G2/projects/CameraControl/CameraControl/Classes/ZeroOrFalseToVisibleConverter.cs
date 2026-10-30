using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CameraControl.Classes;

public class ZeroOrFalseToVisibleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is int length && length == 0)
		{
			return Visibility.Visible;
		}
		if (value is bool b && !b)
		{
			return Visibility.Visible;
		}
		return Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
