using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using CameraControl.Devices;

namespace CameraControl.Controls;

public partial class ExposureStatusControl : UserControl, IComponentConnector
{
	private SolidColorBrush brushActive;

	private SolidColorBrush brushInactive;

	private Shape[] shapes;

	public static readonly DependencyProperty ExposureStatusProperty = DependencyProperty.Register("ExposureStatus", typeof(int), typeof(ExposureStatusControl), new PropertyMetadata(0, OnExposureStatusChanged));

	public int ExposureStatus
	{
		get
		{
			return (int)GetValue(ExposureStatusProperty);
		}
		set
		{
			try
			{
				Shape[] array = shapes;
				foreach (Shape shape in array)
				{
					shape.Fill = brushInactive;
				}
				SetValue(ExposureStatusProperty, value);
				double absValue = Math.Abs(value);
				double delta = 2.0;
				int count = (int)Math.Round(absValue / delta);
				count = Math.Min(count, 7);
				if ((double)value > 0.0)
				{
					for (int i2 = 6; i2 > 6 - count; i2--)
					{
						shapes[i2].Fill = brushActive;
					}
				}
				else
				{
					for (int j = 7; j < 7 + count; j++)
					{
						shapes[j].Fill = brushActive;
					}
				}
			}
			catch (Exception exception)
			{
				Log.Debug("EXS error", exception);
			}
		}
	}

	public ExposureStatusControl()
	{
		InitializeComponent();
		brushActive = Pos1.Fill as SolidColorBrush;
		brushInactive = new SolidColorBrush(Color.FromArgb(32, brushActive.Color.R, brushActive.Color.G, brushActive.Color.B));
		shapes = new List<Shape>
		{
			PosMax, Pos6, Pos5, Pos4, Pos3, Pos2, Pos1, Neg1, Neg2, Neg3,
			Neg4, Neg5, Neg6, NegMax
		}.ToArray();
		ExposureStatus = 0;
	}

	private static void OnExposureStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		ExposureStatusControl control = d as ExposureStatusControl;
		control.ExposureStatus = (int)e.NewValue;
	}
}
