#define DEBUG
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace CameraControl.Core.Wpf;

public partial class Colorpicker : UserControl, IComponentConnector
{
	public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register("SelectedColor", typeof(Color), typeof(Colorpicker), new FrameworkPropertyMetadata(OnSelectedColorChanged));

	public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register("SelectedBrush", typeof(Brush), typeof(Colorpicker), new FrameworkPropertyMetadata(OnSelectedBrushChanged));

	public static readonly RoutedEvent ColorChangedEvent = EventManager.RegisterRoutedEvent("ColorChanged", RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<Color>), typeof(Colorpicker));

	private static Brush _CheckerBrush = CreateCheckerBrush();

	public Color SelectedColor
	{
		get
		{
			return (Color)GetValue(SelectedColorProperty);
		}
		set
		{
			SetValue(SelectedColorProperty, value);
		}
	}

	public Brush SelectedBrush
	{
		get
		{
			return (Brush)GetValue(SelectedBrushProperty);
		}
		set
		{
			SetValue(SelectedBrushProperty, value);
		}
	}

	public static Brush CheckerBrush => _CheckerBrush;

	public event RoutedPropertyChangedEventHandler<Color> ColorChanged
	{
		add
		{
			AddHandler(ColorChangedEvent, value);
		}
		remove
		{
			RemoveHandler(ColorChangedEvent, value);
		}
	}

	private static void OnSelectedColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
	{
		Colorpicker cp = obj as Colorpicker;
		Debug.Assert(cp != null);
		Color newColor = (Color)args.NewValue;
		Color oldColor = (Color)args.OldValue;
		if (!(newColor == oldColor))
		{
			if ((!(cp.ColorList1.SelectedValue is ColorViewModel selectedColorViewModel) || selectedColorViewModel.Color != newColor) && !cp.ListContains(newColor))
			{
				cp.AddColor(newColor, newColor.ToString());
			}
			cp.SelectedBrush = new SolidColorBrush(newColor);
			cp.OnColorChanged(oldColor, newColor);
		}
	}

	private bool ListContains(Color newColor)
	{
		foreach (object o in (IEnumerable)ColorList1.Items)
		{
			if (!(o is ColorViewModel vcm) || !(vcm.Color == newColor))
			{
				continue;
			}
			return true;
		}
		return false;
	}

	private static void OnSelectedBrushChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
	{
		Colorpicker cp = (Colorpicker)obj;
		SolidColorBrush newBrush = (SolidColorBrush)args.NewValue;
		if (cp.SelectedColor != newBrush.Color)
		{
			cp.SelectedColor = newBrush.Color;
		}
	}

	protected virtual void OnColorChanged(Color oldValue, Color newValue)
	{
		RoutedPropertyChangedEventArgs<Color> args = new RoutedPropertyChangedEventArgs<Color>(oldValue, newValue);
		args.RoutedEvent = ColorChangedEvent;
		RaiseEvent(args);
	}

	public Colorpicker()
	{
		InitializeComponent();
		InitializeColors();
	}

	public void InitializeColors()
	{
		ColorList1.Items.Clear();
		AddColor(Colors.Black, "Black");
		AddColor(Colors.Gray, "Gray");
		AddColor(Colors.LightGray, "LightGray");
		AddColor(Colors.White, "White");
		AddColor(Colors.Transparent, "Transparent");
		AddColor(Colors.Red, "Red");
		AddColor(Colors.Green, "Green");
		AddColor(Colors.Blue, "Blue");
		AddColor(Colors.Cyan, "Cyan");
		AddColor(Colors.Magenta, "Magenta");
		AddColor(Colors.Yellow, "Yellow");
		AddColor(Colors.Purple, "Purple");
		AddColor(Colors.Orange, "Orange");
		AddColor(Colors.Brown, "Brown");
		AddColor(Color.FromArgb(128, 0, 0, 0), "Black 50%");
		AddColor(Color.FromArgb(128, byte.MaxValue, byte.MaxValue, byte.MaxValue), "White 50%");
		AddColor(Color.FromArgb(128, byte.MaxValue, 0, 0), "Red 50%");
		AddColor(Color.FromArgb(128, 0, byte.MaxValue, 0), "Green 50%");
		AddColor(Color.FromArgb(128, 0, 0, byte.MaxValue), "Blue 50%");
		Type colorsType = typeof(Colors);
		PropertyInfo[] pis = colorsType.GetProperties();
		PropertyInfo[] array = pis;
		foreach (PropertyInfo pi in array)
		{
			AddColor((Color)pi.GetValue(null, null), pi.Name);
		}
		ColorList1.SelectedValuePath = "Color";
	}

	private void AddColor(Color color, string name)
	{
		if (!name.StartsWith("#", StringComparison.Ordinal))
		{
			name = NiceName(name);
		}
		ColorViewModel cvm = new ColorViewModel
		{
			Color = color,
			Name = name
		};
		ColorList1.Items.Add(cvm);
	}

	private static string NiceName(string name)
	{
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < name.Length; i++)
		{
			if (i > 0 && char.IsUpper(name[i]))
			{
				sb.Append(" ");
			}
			sb.Append(name[i]);
		}
		return sb.ToString();
	}

	public static Brush CreateCheckerBrush()
	{
		DrawingBrush checkerBrush = new DrawingBrush();
		GeometryDrawing backgroundSquare = new GeometryDrawing(Brushes.White, null, new RectangleGeometry(new Rect(0.0, 0.0, 8.0, 8.0)));
		GeometryGroup aGeometryGroup = new GeometryGroup();
		aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(0.0, 0.0, 4.0, 4.0)));
		aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(4.0, 4.0, 4.0, 4.0)));
		GeometryDrawing checkers = new GeometryDrawing(Brushes.Black, null, aGeometryGroup);
		DrawingGroup checkersDrawingGroup = new DrawingGroup();
		checkersDrawingGroup.Children.Add(backgroundSquare);
		checkersDrawingGroup.Children.Add(checkers);
		checkerBrush.Drawing = checkersDrawingGroup;
		checkerBrush.Viewport = new Rect(0.0, 0.0, 0.5, 0.5);
		checkerBrush.TileMode = TileMode.Tile;
		return checkerBrush;
	}
}
