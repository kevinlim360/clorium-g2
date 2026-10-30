using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Xml;
using CameraControl.Core;
using CameraControl.Core.Translation;
using CameraControl.Devices;

namespace CameraControl.windows;

public partial class NewVersionWnd : Window, IComponentConnector
{
	private string _downloadUrl = "";

	private string _logUrl = "";

	public NewVersionWnd(string changelog, string download)
	{
		_downloadUrl = download;
		_logUrl = changelog;
		InitializeComponent();
		if (string.IsNullOrEmpty(download))
		{
			button2.Visibility = Visibility.Hidden;
		}
	}

	private void button2_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Process.Start(_downloadUrl);
			ServiceProvider.WindowsManager.ExecuteCommand("All_Close");
		}
		catch (Exception)
		{
			System.Windows.Forms.MessageBox.Show("Error initialize download !");
		}
	}

	public static void ShowChangeLog()
	{
		NewVersionWnd wnd = new NewVersionWnd("http://digicamcontrol.com/updates/changelog.html", "");
		wnd.ShowDialog();
	}

	public static bool CheckForUpdate(bool notify)
	{
		try
		{
			string tempfile = Path.GetTempFileName();
			using (WebClient client = new WebClient())
			{
				client.DownloadFile("http://digicamcontrol.com/updates/versioninfo.xml", tempfile);
			}
			XmlDocument document = new XmlDocument();
			document.Load(tempfile);
			string ver = document.SelectSingleNode("application/version").InnerText;
			string url = "http://digicamcontrol.com/download";
			string logurl = "";
			XmlNode selectSingleNode = document.SelectSingleNode("application/url");
			if (selectSingleNode != null)
			{
				url = selectSingleNode.InnerText;
			}
			XmlNode logNode = document.SelectSingleNode("application/logurl");
			if (logNode != null)
			{
				logurl = logNode.InnerText;
			}
			Version v_ver = new Version(ver);
			if (v_ver > new Version(System.Diagnostics.FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion))
			{
				NewVersionWnd wnd = new NewVersionWnd(logurl, url);
				wnd.ShowDialog();
			}
			else if (notify)
			{
				System.Windows.Forms.MessageBox.Show(TranslationStrings.MsgApplicationUpToDate);
			}
			File.Delete(tempfile);
		}
		catch (Exception exception)
		{
			Log.Error("Error download update information", exception);
		}
		return false;
	}

	private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
	{
		webBrowser.Source = new Uri(_logUrl);
	}

	private void button1_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
