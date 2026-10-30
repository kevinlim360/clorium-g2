using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;

namespace CameraControl.windows;

public partial class EditSession : Window, IComponentConnector
{
	public PhotoSession Session { get; set; }

	public EditSession(PhotoSession session)
	{
		try
		{
			Session = session;
			Session.BeginEdit();
			InitializeComponent();
			DataContext = Session;
		}
		catch (Exception exception)
		{
			Log.Error("EditSession init", exception);
		}
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		SessionName.Focus();
		SessionName.SelectAll();
	}

	private void btn_browse_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			using FolderBrowserDialog dialog = new FolderBrowserDialog();
			string year = DateTime.Now.ToString("yyyy");
			string month = DateTime.Now.ToString("MM");
			string day = DateTime.Now.ToString("dd");
			dialog.Description = "새로운 새션 폴더를 만드세요.";
			dialog.SelectedPath = Path.Combine(ServiceProvider.Settings.DownloadFolder, year.ToString() + "년" + month.ToString() + "월\\", day.ToString());
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				Session.Folder = dialog.SelectedPath;
			}
		}
		catch (Exception exception)
		{
			System.Windows.MessageBox.Show("폴더를 만들 수 없습니다.");
			Log.Error("폴더를 만들 수 없습니다.", exception);
		}
	}

	private void btn_browse_bk_Click(object sender, RoutedEventArgs e)
	{
		FolderBrowserDialog dialog = new FolderBrowserDialog();
		dialog.SelectedPath = Session.BackUpPath;
		if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
		{
			Session.BackUpPath = dialog.SelectedPath;
		}
	}

	private void btn_OK_Click(object sender, RoutedEventArgs e)
	{
		Session.EndEdit();
		DialogResult = true;
		Close();
	}

	private void btn_Cancel_Click(object sender, RoutedEventArgs e)
	{
		Session.CancelEdit();
		Close();
	}

	private void btn_add_tag_Click(object sender, RoutedEventArgs e)
	{
		TagItem item = new TagItem();
		EditTagWnd wnd = new EditTagWnd(item);
		if (wnd.ShowDialog() == true)
		{
			Session.Tags.Add(item);
		}
	}

	private void btn_del_tag_Click(object sender, RoutedEventArgs e)
	{
		if (lst_tags.SelectedItem is TagItem item)
		{
			Session.Tags.Remove(item);
		}
	}

	private void btn_edit_tag_Click(object sender, RoutedEventArgs e)
	{
		if (lst_tags.SelectedItem is TagItem item)
		{
			EditTagWnd wnd = new EditTagWnd(item);
			wnd.ShowDialog();
		}
	}

	private void btn_help_Click(object sender, RoutedEventArgs e)
	{
		CameraControl.Core.Classes.HelpProvider.Run(HelpSections.Session);
	}

	private void btn_template_Click(object sender, RoutedEventArgs e)
	{
		FileNameTemplateEditorWnd wnd = new FileNameTemplateEditorWnd();
		wnd.Owner = this;
		wnd.TemplateString = Session.FileNameTemplate;
		if (wnd.ShowDialog() == true)
		{
			Session.FileNameTemplate = wnd.TemplateString;
		}
	}
}
