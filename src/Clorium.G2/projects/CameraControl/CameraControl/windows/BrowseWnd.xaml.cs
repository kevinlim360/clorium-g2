using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Threading;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.windows;

// 세션 검색: the sessions in the list, plus 다른 폴더 - a Windows folder picker (also opened once each time the window
// opens) for a photo folder somewhere else. A folder that belongs to a listed session selects it; any other folder is
// shown as 폴더에서 찾음 with its photos, and 이 세션 열기 adds it to the session list and makes it the current session.
public partial class BrowseWnd : Window, INotifyPropertyChanged, IWindow, IComponentConnector
{
	private PhotoSession _selectedPhotoSession;

	private PhotoSession _found;

	public PhotoSession SelectedPhotoSession
	{
		get
		{
			return _selectedPhotoSession;
		}
		set
		{
			_selectedPhotoSession = value;
			// the list shows a listed session as selected; the 폴더에서 찾음 entry is outside the list
			object inList = value != null && ServiceProvider.Settings?.PhotoSessions != null && ServiceProvider.Settings.PhotoSessions.Contains(value) ? value : null;
			if (lst_profiles != null && lst_profiles.SelectedItem != inList) lst_profiles.SelectedItem = inList;
			NotifyPropertyChanged("SelectedPhotoSession");
			NotifyPropertyChanged("FoundSelected");
		}
	}

	// the 폴더에서 찾음 entry is the one shown on the right
	public bool FoundSelected => _found != null && _selectedPhotoSession == _found;

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public BrowseWnd()
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

	public void ExecuteCommand(string cmd, object param)
	{
		switch (cmd)
		{
		case "BrowseWnd_Show":
			ServiceProvider.Settings.PropertyChanged += Settings_PropertyChanged;
			Dispatcher.Invoke(() =>
			{
				Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				SelectedPhotoSession = ServiceProvider.Settings.DefaultSession;
				Show();
				Activate();
				Focus();
				// the folder picker opens with the window (other folders may hold the session being looked for)
				Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
				{
					if (IsVisible) PickFolder();
				}));
			});
			break;
		case "BrowseWnd_Hide":
			ServiceProvider.Settings.PropertyChanged -= Settings_PropertyChanged;
			Dispatcher.Invoke(() =>
			{
				Hide();
				ForgetFound();
			});
			break;
		case "All_Close":
			Dispatcher.Invoke(() =>
			{
				Hide();
				Close();
			});
			break;
		}
	}

	private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "DefaultSession")
		{
			Dispatcher.BeginInvoke(new Action(() => SelectedPhotoSession = ServiceProvider.Settings.DefaultSession));
		}
	}

	private void Window_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("BrowseWnd_Hide");
		}
	}

	private void lst_profiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
	{
		if (lst_profiles.SelectedItem is PhotoSession session && session != _selectedPhotoSession) SelectedPhotoSession = session;
	}

	private void lst_profiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		OpenSelected();
	}

	// 이 세션 열기: same as double-clicking the session.
	private void Open_Click(object sender, RoutedEventArgs e)
	{
		OpenSelected();
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("BrowseWnd_Hide");
	}

	private void Folder_Click(object sender, RoutedEventArgs e)
	{
		PickFolder();
	}

	private void FoundPanel_Click(object sender, MouseButtonEventArgs e)
	{
		if (_found == null) return;
		SelectedPhotoSession = _found;
		if (e.ClickCount == 2) OpenSelected();
	}

	private void OpenSelected()
	{
		PhotoSession session = SelectedPhotoSession;
		if (session == null) return;
		if (session == _found)
		{
			// add the folder as a session (name made unique), then switch to it
			string name = session.Name;
			for (int i = 2; ServiceProvider.Settings.PhotoSessions.Any(s => string.Equals(s.Name, session.Name, StringComparison.OrdinalIgnoreCase)); i++)
			{
				session.Name = name + "_" + i;
			}
			ServiceProvider.Settings.Add(session);
			_found = null;
			FoundPanel.Visibility = Visibility.Collapsed;
			Log.Debug("세션 검색 : 폴더를 세션으로 추가 " + session.Name + " -> " + session.Folder);
		}
		else if (lst_profiles.SelectedItem == null)
		{
			return;
		}
		ServiceProvider.Settings.DefaultSession = session;
		ServiceProvider.WindowsManager.ExecuteCommand("BrowseWnd_Hide");
	}

	private void PickFolder()
	{
		string start = StartFolder();
		string picked;
		using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
		{
			dialog.Description = "세션(사진) 폴더를 고르세요.";
			dialog.ShowNewFolderButton = false;
			if (!string.IsNullOrEmpty(start)) dialog.SelectedPath = start;
			if (dialog.ShowDialog(new Win32Owner(new WindowInteropHelper(this).Handle)) != System.Windows.Forms.DialogResult.OK) return;
			picked = dialog.SelectedPath;
		}
		ShowFolder(picked);
	}

	// the folder above the current session (D:\Clorium Images\...), else the download folder
	private static string StartFolder()
	{
		try
		{
			string folder = ServiceProvider.Settings.DefaultSession?.Folder;
			string parent = string.IsNullOrEmpty(folder) ? null : Path.GetDirectoryName(folder.TrimEnd('\\'));
			if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent)) return parent;
			string download = ServiceProvider.Settings.DownloadFolder;
			return !string.IsNullOrEmpty(download) && Directory.Exists(download) ? download : null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public void ShowFolder(string folder)
	{
		if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
		string full = Normalize(folder);
		PhotoSession listed = ServiceProvider.Settings.PhotoSessions.FirstOrDefault(s => !string.IsNullOrEmpty(s.Folder) && string.Equals(Normalize(s.Folder), full, StringComparison.OrdinalIgnoreCase));
		if (listed != null)
		{
			ForgetFound();
			SelectedPhotoSession = listed;
			lst_profiles.ScrollIntoView(listed);
			return;
		}
		ForgetFound();
		var session = new PhotoSession { Name = Path.GetFileName(full), Folder = full };
		_found = session;
		FoundPanel.DataContext = session;
		FoundPanel.Visibility = Visibility.Visible;
		SelectedPhotoSession = session;
		Log.Debug("세션 검색 : 폴더 " + full);
		Task.Run(() => ServiceProvider.Settings.LoadData(session));
	}

	// a picked folder that was not opened: drop it and its thumbnails
	private void ForgetFound()
	{
		if (_found == null) return;
		PhotoSession old = _found;
		_found = null;
		FoundPanel.Visibility = Visibility.Collapsed;
		FoundPanel.DataContext = null;
		if (SelectedPhotoSession == old) SelectedPhotoSession = ServiceProvider.Settings.DefaultSession;
		foreach (FileItem item in old.Files.ToList())
		{
			item.Dispose();
		}
		old.Files.Clear();
	}

	private static string Normalize(string folder)
	{
		try
		{
			return Path.GetFullPath(folder).TrimEnd('\\', '/');
		}
		catch (Exception)
		{
			return folder.TrimEnd('\\', '/');
		}
	}

	private void lst_files_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
	}

	private sealed class Win32Owner : System.Windows.Forms.IWin32Window
	{
		public Win32Owner(IntPtr handle)
		{
			Handle = handle;
		}

		public IntPtr Handle { get; }
	}
}
