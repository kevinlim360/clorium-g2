using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace CameraControl.Controls;

public partial class VUMeterControl : UserControl, IComponentConnector
{
	public class Block
	{
		public int Level { get; set; }
	}

	private ObservableCollection<Block> items = new ObservableCollection<Block>();

	public static readonly DependencyProperty BlockCountProperty = DependencyProperty.Register("BlockCount", typeof(int), typeof(VUMeterControl), new UIPropertyMetadata(15, (DependencyObject o, DependencyPropertyChangedEventArgs e) =>
	{
		((VUMeterControl)o).GenerateBlocks();
	}));

	public static readonly DependencyProperty HighLevelProperty = DependencyProperty.Register("HighLevel", typeof(int), typeof(VUMeterControl), new UIPropertyMetadata(12, (DependencyObject o, DependencyPropertyChangedEventArgs e) =>
	{
		((VUMeterControl)o).GenerateBlocks();
	}));

	public static readonly DependencyProperty MiddleLevelProperty = DependencyProperty.Register("MiddleLevel", typeof(int), typeof(VUMeterControl), new UIPropertyMetadata(7, (DependencyObject o, DependencyPropertyChangedEventArgs e) =>
	{
		((VUMeterControl)o).GenerateBlocks();
	}));

	public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(int), typeof(VUMeterControl), new UIPropertyMetadata(100));

	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(int), typeof(VUMeterControl), new UIPropertyMetadata(0, (DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		VUMeterControl vUMeterControl = (VUMeterControl)sender;
		int num = (int)e.NewValue;
		for (int num2 = vUMeterControl.BlockCount - 1; num2 >= 0; num2--)
		{
			FrameworkElement frameworkElement = (FrameworkElement)vUMeterControl.PART_ItemsPresenter.ItemContainerGenerator.ContainerFromIndex(num2);
			if (frameworkElement != null)
			{
				if (vUMeterControl.BlockCount - num2 > num / (vUMeterControl.MaxValue / vUMeterControl.BlockCount))
				{
					frameworkElement.Visibility = Visibility.Hidden;
				}
				else
				{
					frameworkElement.Visibility = Visibility.Visible;
				}
			}
		}
	}));

	public static readonly DependencyProperty PeakValueProperty = DependencyProperty.Register("PeakValue", typeof(int), typeof(VUMeterControl), new UIPropertyMetadata(0, (DependencyObject sender, DependencyPropertyChangedEventArgs e) =>
	{
		VUMeterControl vUMeterControl = (VUMeterControl)sender;
		int num = (int)e.NewValue;
		int index = num / (vUMeterControl.MaxValue / vUMeterControl.BlockCount);
		FrameworkElement frameworkElement = (FrameworkElement)vUMeterControl.PART_ItemsPresenter.ItemContainerGenerator.ContainerFromIndex(index);
		if (frameworkElement != null)
		{
			frameworkElement.Visibility = Visibility.Visible;
		}
	}));

	public int BlockCount
	{
		get
		{
			return (int)GetValue(BlockCountProperty);
		}
		set
		{
			SetValue(BlockCountProperty, value);
		}
	}

	public int HighLevel
	{
		get
		{
			return (int)GetValue(HighLevelProperty);
		}
		set
		{
			SetValue(HighLevelProperty, value);
		}
	}

	public int MiddleLevel
	{
		get
		{
			return (int)GetValue(MiddleLevelProperty);
		}
		set
		{
			SetValue(MiddleLevelProperty, value);
		}
	}

	public int MaxValue
	{
		get
		{
			return (int)GetValue(MaxValueProperty);
		}
		set
		{
			SetValue(MaxValueProperty, value);
		}
	}

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

	public int PeakValue
	{
		get
		{
			return (int)GetValue(PeakValueProperty);
		}
		set
		{
			SetValue(PeakValueProperty, value);
		}
	}

	public VUMeterControl()
	{
		InitializeComponent();
		GenerateBlocks();
		PART_ItemsPresenter.DataContext = items;
	}

	private void GenerateBlocks()
	{
		items.Clear();
		for (int i = 0; i < BlockCount; i++)
		{
			if (i < BlockCount - HighLevel)
			{
				items.Add(new Block
				{
					Level = 1
				});
			}
			if (i >= BlockCount - HighLevel && i < BlockCount - MiddleLevel)
			{
				items.Add(new Block
				{
					Level = 2
				});
			}
			if (i >= BlockCount - MiddleLevel)
			{
				items.Add(new Block
				{
					Level = 3
				});
			}
		}
	}
}
