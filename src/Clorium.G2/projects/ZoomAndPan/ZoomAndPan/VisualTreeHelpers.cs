using System.Windows;
using System.Windows.Media;

namespace ZoomAndPan;

public static class VisualTreeHelpers
{
	public static T FindParentControl<T>(this DependencyObject control) where T : DependencyObject
	{
		DependencyObject parent = VisualTreeHelper.GetParent(control);
		while (parent != null && !(parent is T))
		{
			parent = VisualTreeHelper.GetParent(parent);
		}
		return parent as T;
	}

	public static T FindChildControl<T>(this DependencyObject control) where T : DependencyObject
	{
		int childrenCount = VisualTreeHelper.GetChildrenCount(control);
		for (int i = 0; i < childrenCount; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(control, i);
			if (!(child is T result))
			{
				T val = child.FindChildControl<T>();
				if (val != null)
				{
					return val;
				}
				continue;
			}
			return result;
		}
		return null;
	}
}
