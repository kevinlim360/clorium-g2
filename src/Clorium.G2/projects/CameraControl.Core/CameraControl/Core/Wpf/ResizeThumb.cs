using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CameraControl.Core.Wpf;

public class ResizeThumb : Thumb
{
	public ResizeThumb()
	{
		DragDelta += ResizeThumb_DragDelta;
	}

	private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
	{
		if (DataContext is Control designerItem)
		{
			switch (VerticalAlignment)
			{
			case VerticalAlignment.Bottom:
			{
				double deltaVertical = Math.Min(0.0 - e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight);
				designerItem.Height -= deltaVertical;
				break;
			}
			case VerticalAlignment.Top:
			{
				double deltaVertical = Math.Min(e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight);
				Canvas.SetTop(designerItem, Canvas.GetTop(designerItem) + deltaVertical);
				designerItem.Height -= deltaVertical;
				break;
			}
			}
			switch (HorizontalAlignment)
			{
			case HorizontalAlignment.Left:
			{
				double deltaHorizontal = Math.Min(e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth);
				Canvas.SetLeft(designerItem, Canvas.GetLeft(designerItem) + deltaHorizontal);
				designerItem.Width -= deltaHorizontal;
				break;
			}
			case HorizontalAlignment.Right:
			{
				double deltaHorizontal = Math.Min(0.0 - e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth);
				designerItem.Width -= deltaHorizontal;
				break;
			}
			}
			e.Handled = true;
		}
		else
		{
			e.Handled = false;
		}
	}
}
