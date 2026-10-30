using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Devices.Others;
using MaterialDesignThemes.Wpf;

namespace CameraControl.windows;

public partial class FileNameTemplateEditorWnd : Window, INotifyPropertyChanged, IComponentConnector, IStyleConnector
{
	private string _templateString;

	public string TemplateString
	{
		get
		{
			return _templateString;
		}
		set
		{
			_templateString = value;
			NotifyPropertyChanged("TemplateString");
			NotifyPropertyChanged("ExampleTemplateString");
		}
	}

	public string ExampleTemplateString
	{
		get
		{
			try
			{
				return ServiceProvider.FilenameTemplateManager.GetExample(TemplateString, ServiceProvider.Settings.DefaultSession, ServiceProvider.DeviceManager.SelectedCameraDevice ?? new AppFakeCameraDevice(), "filename.jpg");
			}
			catch (Exception ex)
			{
				return ex.Message;
			}
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public FileNameTemplateEditorWnd()
	{
		InitializeComponent();
	}

	public virtual void NotifyPropertyChanged(string info)
	{
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}

	private void Button_Click(object sender, RoutedEventArgs e)
	{
		if (sender is Chip s)
		{
			string text = "_" + (string)s.Content;
			if (txt_templateName.SelectionLength > 0)
			{
				txt_templateName.SelectedText = text;
				txt_templateName.SelectionLength = 0;
				txt_templateName.CaretIndex += text.Length;
			}
			else
			{
				int index = txt_templateName.CaretIndex;
				txt_templateName.Text = txt_templateName.Text.Insert(index, text);
				txt_templateName.CaretIndex = index + text.Length;
				txt_templateName.SelectionLength = 0;
			}
			txt_templateName.Focus();
			NotifyPropertyChanged("ExampleTemplateString");
		}
	}

	private void btn_ok_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
		Close();
	}

	private void btn_cancel_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = false;
		Close();
	}

	private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
	{
		txt_templateName.CaretIndex += TemplateString.Length;
	}
}
