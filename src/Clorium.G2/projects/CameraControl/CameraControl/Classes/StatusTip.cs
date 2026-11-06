using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CameraControl.Classes;

// Tooltip content for the rail status icons: label rows, usage bars with the percentage after them, and a small
// battery glyph. Colours come from the G2 theme of the element that shows the tooltip (light/dark).
public sealed class StatusTip
{
	private readonly FrameworkElement owner;

	public StackPanel Panel { get; } = new StackPanel { MinWidth = 230 };

	public StatusTip(FrameworkElement owner, string title)
	{
		this.owner = owner;
		Panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
	}

	public Brush Theme(string key, Color fallback)
	{
		return owner.TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);
	}

	private Brush Sub => Theme("G2Sub", Color.FromRgb(0x8A, 0x91, 0x9C));

	private Brush Track => Theme("G2Line", Color.FromRgb(0x3A, 0x3F, 0x47));

	private Brush Ok => Theme("G2Ok", Color.FromRgb(0x2E, 0xB8, 0x72));

	private Brush Alarm => Theme("G2Err", Color.FromRgb(0xC6, 0x28, 0x28));

	private Brush Caution => Theme("G2Pr", Color.FromRgb(0xB3, 0x54, 0x00));

	private Grid Row(string label)
	{
		var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(74) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		g.Children.Add(new TextBlock { Text = label, Foreground = Sub, VerticalAlignment = VerticalAlignment.Center });
		Panel.Children.Add(g);
		return g;
	}

	private static void Put(Grid g, UIElement e, int column)
	{
		Grid.SetColumn(e, column);
		g.Children.Add(e);
	}

	public TextBlock Text(string label, string value)
	{
		Grid g = Row(label);
		var t = new TextBlock { Text = value, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumnSpan(t, 3);
		Put(g, t, 1);
		return t;
	}

	// Usage bar, then "62%", then the amounts in grey (e.g. 21.6 / 31.7 GB). Red from 90 %, the icon's alarm level.
	public void Usage(string label, double percent, string detail)
	{
		percent = Math.Max(0, Math.Min(100, percent));
		Grid g = Row(label);
		Brush fill = percent >= 90 ? Alarm : percent >= 75 ? Caution : Ok;
		var bar = new Grid { Width = 96, Height = 8, VerticalAlignment = VerticalAlignment.Center };
		bar.Children.Add(new Border { Background = Track, CornerRadius = new CornerRadius(4) });
		bar.Children.Add(new Border { Background = fill, CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Left, Width = Math.Max(percent > 0 ? 4 : 0, 96 * percent / 100) });
		Put(g, bar, 1);
		Put(g, new TextBlock { Text = percent.ToString("0") + "%", FontWeight = FontWeights.SemiBold, Foreground = percent >= 90 ? Alarm : (Brush)TextElement(), Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, MinWidth = 34 }, 2);
		Put(g, new TextBlock { Text = detail, Foreground = Sub, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }, 3);
	}

	// Battery glyph (body, cap, level fill) followed by the percentage. Red at 20 % or less, amber up to 50 %.
	public void Battery(string label, int percent)
	{
		percent = Math.Max(0, Math.Min(100, percent));
		Grid g = Row(label);
		Brush fill = percent <= 20 ? Alarm : percent <= 50 ? Caution : Ok;
		var glyph = new Canvas { Width = 26, Height = 12, VerticalAlignment = VerticalAlignment.Center };
		glyph.Children.Add(new Rectangle { Width = 23, Height = 12, RadiusX = 2.5, RadiusY = 2.5, Stroke = Sub, StrokeThickness = 1.3 });
		var cap = new Rectangle { Width = 2, Height = 5, Fill = Sub };
		Canvas.SetLeft(cap, 23.5);
		Canvas.SetTop(cap, 3.5);
		glyph.Children.Add(cap);
		var level = new Rectangle { Width = Math.Max(percent > 0 ? 1.5 : 0, 19 * percent / 100.0), Height = 8, RadiusX = 1.2, RadiusY = 1.2, Fill = fill };
		Canvas.SetLeft(level, 2);
		Canvas.SetTop(level, 2);
		glyph.Children.Add(level);
		Put(g, glyph, 1);
		Put(g, new TextBlock { Text = percent + "%", FontWeight = FontWeights.SemiBold, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center }, 2);
	}

	public void Note(string text)
	{
		Panel.Children.Add(new TextBlock { Text = text, Foreground = Sub, FontSize = 11, Margin = new Thickness(0, 6, 0, 0) });
	}

	private object TextElement()
	{
		return owner.TryFindResource("G2Text") as Brush ?? SystemColors.ControlTextBrush;
	}

	// Canon shooting mode code (Mode.NumericValue) as shown on the camera dial.
	public static string ModeName(long code, string fallback)
	{
		switch (code)
		{
		case 0: return "P (프로그램)";
		case 1: return "Tv (셔터 우선)";
		case 2: return "Av (조리개 우선)";
		case 3: return "M (매뉴얼)";
		case 4: return "Bulb (벌브)";
		case 9: return "Auto (자동)";
		default: return string.IsNullOrEmpty(fallback) ? "확인할 수 없음" : fallback;
		}
	}
}
