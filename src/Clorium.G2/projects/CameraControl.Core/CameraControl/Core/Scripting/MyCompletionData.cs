using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace CameraControl.Core.Scripting;

public class MyCompletionData : ICompletionData
{
	public string DisplayText { get; set; }

	public ImageSource Image => null;

	public string Text { get; private set; }

	public object Content => DisplayText;

	public object Description { get; private set; }

	public double Priority => 0.0;

	public MyCompletionData(string text, string description, string displaytext = null)
	{
		Text = text;
		Description = description;
		DisplayText = displaytext;
		if (string.IsNullOrEmpty(DisplayText))
		{
			DisplayText = Text;
		}
		if (string.IsNullOrEmpty(Text))
		{
			Text = displaytext;
		}
	}

	public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
	{
		textArea.Document.Replace(completionSegment, Text);
	}
}
