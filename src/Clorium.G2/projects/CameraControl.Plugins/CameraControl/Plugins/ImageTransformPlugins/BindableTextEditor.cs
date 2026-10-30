using System;
using System.ComponentModel;
using System.Windows;
using ICSharpCode.AvalonEdit;

namespace CameraControl.Plugins.ImageTransformPlugins;

// AvalonEdit 텍스트를 WPF 바인딩으로 노출하는 어댑터입니다. 원본에는 명시적 정적 생성자가 없어 BeforeFieldInit을 유지합니다.
public class BindableTextEditor : TextEditor, INotifyPropertyChanged
{
	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(BindableTextEditor), new PropertyMetadata((PropertyChangedCallback)((DependencyObject obj, DependencyPropertyChangedEventArgs args) =>
	{
		BindableTextEditor bindableTextEditor = (BindableTextEditor)(object)obj;
		bindableTextEditor.Text = (string)args.NewValue;
	})));

	public new string Text
	{
		get
		{
			return base.Text;
		}
		set
		{
			base.Text = value;
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected override void OnTextChanged(EventArgs e)
	{
		RaisePropertyChanged("Text");
		base.OnTextChanged(e);
	}

	public void RaisePropertyChanged(string property)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(property));
		}
	}

	
}
