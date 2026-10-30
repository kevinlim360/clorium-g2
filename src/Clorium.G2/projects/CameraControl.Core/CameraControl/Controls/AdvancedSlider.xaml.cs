using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace CameraControl.Controls;

public partial class AdvancedSlider : UserControl, IComponentConnector
{
	private static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(AdvancedSlider), new FrameworkPropertyMetadata(100, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	private static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(AdvancedSlider), new FrameworkPropertyMetadata("Label", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	private static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(int), typeof(AdvancedSlider), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	private static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(int), typeof(AdvancedSlider), new FrameworkPropertyMetadata(int.MaxValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public int Value
	{
		get
		{
			return (int)GetValue(ValueProperty);
		}
		set
		{
			SetValue(ValueProperty, value);
		}
	}

	public int Maximum
	{
		get
		{
			return (int)GetValue(MaximumProperty);
		}
		set
		{
			SetValue(MaximumProperty, value);
			if (Value > Maximum)
			{
				Value = Maximum;
			}
		}
	}

	public int Minimum
	{
		get
		{
			return (int)GetValue(MinimumProperty);
		}
		set
		{
			SetValue(MinimumProperty, value);
			if (Value < Minimum)
			{
				Value = Minimum;
			}
		}
	}

	public string Label
	{
		get
		{
			return (string)GetValue(LabelProperty);
		}
		set
		{
			SetValue(LabelProperty, value);
		}
	}

	public AdvancedSlider()
	{
		InitializeComponent();
		DataContext = this;
	}

	private void btn_m_Click(object sender, RoutedEventArgs e)
	{
		Value--;
	}

	private void btn_p_Click(object sender, RoutedEventArgs e)
	{
		Value++;
	}
}
