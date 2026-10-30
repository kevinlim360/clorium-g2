using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CameraControl.Classes;

public class MultiOrBooleanToVisibilityConverter : IMultiValueConverter
{
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (targetType != typeof(Visibility))
		{
			return DependencyProperty.UnsetValue;
		}
		bool booleanValue = default;
		foreach (object value in values)
		{
			int num;
			if (value is bool)
			{
				booleanValue = (bool)value;
				num = 1;
			}
			else
			{
				num = 0;
			}
			if (((uint)num & (booleanValue ? 1u : 0u)) != 0)
			{
				return Visibility.Visible;
			}
		}
		return Visibility.Collapsed;
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
