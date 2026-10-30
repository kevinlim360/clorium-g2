using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;

namespace CameraControl.Panels;

public partial class ImagePropertiesControl : UserControl, IComponentConnector
{
	public ImagePropertiesControl()
	{
		InitializeComponent();
	}

	private void btn_set_Click(object sender, RoutedEventArgs e)
	{
		btn_set.IsEnabled = false;
		try
		{
			string filename = ServiceProvider.Settings.SelectedBitmap.FileItem.FileName;
			Exiv2Helper.SaveComment(filename, ServiceProvider.Settings.SelectedBitmap.Comment);
			if (ServiceProvider.Settings.SelectedBitmap.FileItem.FileInfo.ExifTags.ContainName("Iptc.Application2.Caption"))
			{
				ServiceProvider.Settings.SelectedBitmap.FileItem.FileInfo.ExifTags["Iptc.Application2.Caption"] = ServiceProvider.Settings.SelectedBitmap.Comment;
			}
			else
			{
				ServiceProvider.Settings.SelectedBitmap.FileItem.FileInfo.ExifTags.Add(new ValuePair
				{
					Name = "Iptc.Application2.Caption",
					Value = ServiceProvider.Settings.SelectedBitmap.Comment
				});
			}
			ServiceProvider.Settings.SelectedBitmap.FileItem.SaveInfo();
			if (chk_tags.IsChecked == true)
			{
				Exiv2Helper.DelKeyword(filename);
				if (!string.IsNullOrEmpty(ServiceProvider.Settings.DefaultSession.SelectedTag1.Value))
				{
					Exiv2Helper.AddKeyword(filename, ServiceProvider.Settings.DefaultSession.SelectedTag1.Value);
				}
				if (!string.IsNullOrEmpty(ServiceProvider.Settings.DefaultSession.SelectedTag2.Value))
				{
					Exiv2Helper.AddKeyword(filename, ServiceProvider.Settings.DefaultSession.SelectedTag2.Value);
				}
				if (!string.IsNullOrEmpty(ServiceProvider.Settings.DefaultSession.SelectedTag3.Value))
				{
					Exiv2Helper.AddKeyword(filename, ServiceProvider.Settings.DefaultSession.SelectedTag3.Value);
				}
				if (!string.IsNullOrEmpty(ServiceProvider.Settings.DefaultSession.SelectedTag4.Value))
				{
					Exiv2Helper.AddKeyword(filename, ServiceProvider.Settings.DefaultSession.SelectedTag4.Value);
				}
			}
			if (Path.GetFileNameWithoutExtension(filename) != ServiceProvider.Settings.SelectedBitmap.FileName)
			{
				try
				{
					string newfilename = Path.Combine(Path.GetDirectoryName(filename), ServiceProvider.Settings.SelectedBitmap.FileName + Path.GetExtension(filename));
					PhotoUtils.WaitForFile(filename);
					File.Move(filename, newfilename);
					ServiceProvider.Settings.SelectedBitmap.FileItem.SetFile(newfilename);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error rename file" + ex.Message);
					Log.Error("Error rename file", ex);
				}
			}
			btn_set.IsEnabled = true;
		}
		catch (Exception ex2)
		{
			Log.Error("Error set property ", ex2);
			MessageBox.Show("Error set property !" + ex2.Message);
		}
	}
}
