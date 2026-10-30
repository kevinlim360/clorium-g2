using System.Collections.Generic;

namespace CameraControl.ViewModel;

public class HistoryManager
{
	private Stack<FilterSettings> undo = new Stack<FilterSettings>();

	private Stack<FilterSettings> redo = new Stack<FilterSettings>();

	public void Push(FilterSettings current)
	{
		undo.Push(current.Clone());
		redo.Clear();
	}

	public FilterSettings Undo()
	{
		if (undo.Count == 0)
		{
			return null;
		}
		if (undo.Count > 1)
		{
			redo.Push(undo.Pop());
			return undo.Peek();
		}
		if (undo.Count == 1)
		{
			redo.Push(undo.Pop());
			FilterSettings InitFilters = new FilterSettings
			{
				Brightness = 0,
				Contrast = 100,
				Gamma = 100,
				Sharpness = 100,
				Saturation = 100,
				Hue = 0
			};
			Push(InitFilters);
		}
		return undo.Peek();
	}

	public FilterSettings Redo()
	{
		if (redo.Count > 0)
		{
			undo.Push(redo.Pop());
			return undo.Peek();
		}
		return undo.Peek();
	}

	public void Clear()
	{
		undo.Clear();
		redo.Clear();
	}
}
