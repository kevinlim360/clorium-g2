using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CameraControl.Classes
{
	/// <summary>
	/// G2 UI: derives the face tag (PR_F, PR_B, F, B, D) from a capture file name for thumbnails.
	/// Display only; file naming rules are not changed here.
	/// ConverterParameter: null = tag text, "brush" = tag background, "border" = thumbnail border, "visibility" = tag visibility.
	/// </summary>
	public class G2FaceTagConverter : IValueConverter
	{
		private static readonly Regex FaceSuffix = new Regex("_(PR_)?([FBD])$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

		private static readonly Brush PrBrush = Frozen(new SolidColorBrush(Color.FromRgb(0xB3, 0x54, 0x00)));

		private static readonly Brush PlainBrush = Frozen(new SolidColorBrush(Color.FromArgb(0x99, 0x00, 0x00, 0x00)));

		private static readonly Brush LineBrush = Frozen(new SolidColorBrush(Color.FromRgb(0xE1, 0xE4, 0xE9)));

		private static Brush Frozen(Brush brush)
		{
			brush.Freeze();
			return brush;
		}

		public static string TagOf(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return string.Empty;
			}
			string stem;
			try
			{
				stem = Path.GetFileNameWithoutExtension(name);
			}
			catch (ArgumentException)
			{
				return string.Empty;
			}
			Match match = FaceSuffix.Match(stem ?? string.Empty);
			if (!match.Success)
			{
				return string.Empty;
			}
			string face = match.Groups[2].Value.ToUpperInvariant();
			return (match.Groups[1].Success && face != "D" ? "PR_" : string.Empty) + face;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string tag = TagOf(value as string);
			bool pr = tag.StartsWith("PR_", StringComparison.Ordinal);
			switch (parameter as string)
			{
				case "brush":
					return pr ? PrBrush : PlainBrush;
				case "border":
					return pr ? PrBrush : LineBrush;
				case "visibility":
					return tag.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
				default:
					return tag;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}

	/// <summary>G2 UI: shows a zero-based index as a one-based key number (Stream Deck key 1..5 labels).</summary>
	public class G2PlusOneConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is int index ? (index + 1).ToString(CultureInfo.InvariantCulture) : string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}
