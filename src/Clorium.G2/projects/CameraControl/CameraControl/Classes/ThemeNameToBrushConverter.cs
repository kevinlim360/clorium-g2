using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CameraControl.Classes;

public class ThemeNameToBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string themeString && !string.IsNullOrEmpty(themeString))
		{
			string[] parts = themeString.Split('\\');
			string theme = parts[0];
			string accentColor = ((parts.Length > 1) ? parts[1] : null);
			if (!string.IsNullOrEmpty(accentColor))
			{
				if (accentColor.Equals("grey", StringComparison.OrdinalIgnoreCase))
				{
					accentColor = "gray";
				}
				Brush originalBrush = (Brush)new BrushConverter().ConvertFromString(accentColor);
				Brush mutableBrush = originalBrush.Clone();
				mutableBrush.Opacity = 1.0;
				return mutableBrush;
			}
		}
		return Brushes.Transparent;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}
