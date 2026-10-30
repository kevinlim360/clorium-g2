using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Serialization;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace CameraControl.Core.Classes;

public class PhotoSession : BaseFieldClass
{
	private object _locker = new object();

	private string _lastFilename = null;

	private const string _keyBase = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths";

	[JsonIgnore]
	[XmlIgnore]
	public List<string> RawExtensions = new List<string> { ".cr2", ".nef" };

	[JsonIgnore]
	[XmlIgnore]
	public List<string> SupportedExtensions = new List<string> { ".jpg", ".nef", ".tif", ".png", ".bmp", ".cr2", ".mov", ".avi" };

	private string _name;

	private string _folder;

	private AsyncObservableCollection<FileItem> _files;

	private string _fileNameTemplate;

	private string _captureName;

	private int _series;

	private bool _alowFolderChange;

	private bool _allowOverWrite;

	private bool _reloadOnFolderChange;

	private int _counter;

	private bool _useOriginalFilename;

	private string _backUpPath;

	private bool _backUp;

	private string _comment;

	private bool _writeComment;

	private bool _useCameraCounter;

	private bool _downloadOnlyJpg;

	private bool _downloadThumbOnly;

	private bool _deleteFileAfterTransfer;

	private int _leadingZeros;

	private bool _lowerCaseExtension;

	private bool _askSavePath;

	private bool _resetCounterOnSeries;

	private BracketingClass _braketing;

	private AsyncObservableCollection<TagItem> _tags;

	private TagItem _selectedTag1;

	private TagItem _selectedTag2;

	private TagItem _selectedTag3;

	private TagItem _selectedTag4;

	public AsyncObservableCollection<AutoExportPluginConfig> AutoExportPluginConfigs { get; set; }

	public TimeLapseSettings TimeLapseSettings { get; set; }

	public PrintSettings PrintSettings { get; set; }

	[XmlIgnore]
	[JsonIgnore]
	public ExternalData ExternalData { get; set; }

	[XmlIgnore]
	[JsonIgnore]
	public RelayCommand<AutoExportPluginConfig> RemovePluginCommand { get; set; }

	[XmlIgnore]
	[JsonIgnore]
	public RelayCommand<AutoExportPluginConfig> ApplyPluginCommand { get; set; }

	public string Barcode { get; set; }

	public string ConfigFile { get; set; }

	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (!AlowFolderChange && Path.GetDirectoryName(Folder) != null && Path.GetFileName(Folder) == _name && Folder != null)
			{
				Folder = Path.Combine(Path.GetDirectoryName(Folder), value);
			}
			_name = value;
			NotifyPropertyChanged("Name");
		}
	}

	public string Folder
	{
		get
		{
			return _folder;
		}
		set
		{
			if (AlowFolderChange && ReloadOnFolderChange && _folder != value)
			{
				_folder = value;
				ServiceProvider.QueueManager.Clear();
				Files.Clear();
				ServiceProvider.Settings.LoadData(this);
			}
			_folder = value;
			NotifyPropertyChanged("Folder");
		}
	}

	public string DisplayFolder
	{
		get
		{
			string onlypath = Path.GetDirectoryName(ServiceProvider.Settings.DefaultSession.Folder) + "\\";
			return onlypath.Replace(ServiceProvider.Settings.DownloadFolder, "..\\");
		}
	}

	public AsyncObservableCollection<FileItem> Files
	{
		get
		{
			return _files;
		}
		set
		{
			if (_files != null)
			{
				_files.CollectionChanged -= Files_CollectionChanged;
			}
			_files = value;
			if (_files != null)
			{
				_files.CollectionChanged += Files_CollectionChanged;
			}
			NotifyPropertyChanged("Files");
			NotifyPropertyChanged("FilesCount");
		}
	}

	public int FilesCount => Files?.Count ?? 0;

	public string FileNameTemplate
	{
		get
		{
			return _fileNameTemplate;
		}
		set
		{
			_fileNameTemplate = value;
			NotifyPropertyChanged("FileNameTemplate");
		}
	}

	public string CaptureName
	{
		get
		{
			return _captureName;
		}
		set
		{
			_captureName = value;
			NotifyPropertyChanged("CaptureName");
		}
	}

	public int Series
	{
		get
		{
			return _series;
		}
		set
		{
			if (_series != value && ResetCounterOnSeries)
			{
				Counter = 0;
			}
			_series = value;
			NotifyPropertyChanged("Series");
		}
	}

	public bool AlowFolderChange
	{
		get
		{
			return _alowFolderChange;
		}
		set
		{
			_alowFolderChange = value;
			NotifyPropertyChanged("AlowFolderChange");
		}
	}

	public bool AllowOverWrite
	{
		get
		{
			return _allowOverWrite;
		}
		set
		{
			_allowOverWrite = value;
			NotifyPropertyChanged("AllowOverWrite");
		}
	}

	public bool ReloadOnFolderChange
	{
		get
		{
			return _reloadOnFolderChange;
		}
		set
		{
			_reloadOnFolderChange = value;
			NotifyPropertyChanged("ReloadOnFolderChange");
		}
	}

	public int Counter
	{
		get
		{
			return _counter;
		}
		set
		{
			_counter = value;
			NotifyPropertyChanged("Counter");
		}
	}

	public bool UseOriginalFilename
	{
		get
		{
			return _useOriginalFilename;
		}
		set
		{
			_useOriginalFilename = value;
			NotifyPropertyChanged("UseOriginalFilename");
		}
	}

	public string BackUpPath
	{
		get
		{
			return _backUpPath;
		}
		set
		{
			_backUpPath = value;
			NotifyPropertyChanged("BackUpPath");
		}
	}

	public bool BackUp
	{
		get
		{
			return _backUp;
		}
		set
		{
			_backUp = value;
			NotifyPropertyChanged("BackUp");
		}
	}

	public string Comment
	{
		get
		{
			return _comment;
		}
		set
		{
			_comment = value;
			NotifyPropertyChanged("Comment");
		}
	}

	public bool WriteComment
	{
		get
		{
			return _writeComment;
		}
		set
		{
			_writeComment = value;
			NotifyPropertyChanged("WriteComment");
		}
	}

	public bool UseCameraCounter
	{
		get
		{
			return _useCameraCounter;
		}
		set
		{
			_useCameraCounter = value;
			NotifyPropertyChanged("UseCameraCounter");
		}
	}

	public bool DownloadOnlyJpg
	{
		get
		{
			return _downloadOnlyJpg;
		}
		set
		{
			_downloadOnlyJpg = value;
			NotifyPropertyChanged("DownloadOnlyJpg");
		}
	}

	public bool DownloadThumbOnly
	{
		get
		{
			return _downloadThumbOnly;
		}
		set
		{
			_downloadThumbOnly = value;
			NotifyPropertyChanged("DownloadThumbOnly");
		}
	}

	public bool DeleteFileAfterTransfer
	{
		get
		{
			return _deleteFileAfterTransfer;
		}
		set
		{
			_deleteFileAfterTransfer = value;
			NotifyPropertyChanged("DeleteFileAfterTransfer");
		}
	}

	public int LeadingZeros
	{
		get
		{
			return _leadingZeros;
		}
		set
		{
			_leadingZeros = value;
			NotifyPropertyChanged("LeadingZeros");
		}
	}

	public bool LowerCaseExtension
	{
		get
		{
			return _lowerCaseExtension;
		}
		set
		{
			_lowerCaseExtension = value;
			NotifyPropertyChanged("LowerCaseExtension");
		}
	}

	public bool AskSavePath
	{
		get
		{
			return _askSavePath;
		}
		set
		{
			_askSavePath = value;
			NotifyPropertyChanged("AskSavePath");
		}
	}

	public bool ResetCounterOnSeries
	{
		get
		{
			return _resetCounterOnSeries;
		}
		set
		{
			_resetCounterOnSeries = value;
			NotifyPropertyChanged("ResetCounterOnSeries");
		}
	}

	public BracketingClass Braketing
	{
		get
		{
			return _braketing;
		}
		set
		{
			_braketing = value;
			NotifyPropertyChanged("Braketing");
		}
	}

	public AsyncObservableCollection<TagItem> Tags
	{
		get
		{
			return _tags;
		}
		set
		{
			_tags = value;
			NotifyPropertyChanged("Tags");
		}
	}

	public TagItem SelectedTag1
	{
		get
		{
			return _selectedTag1;
		}
		set
		{
			_selectedTag1 = value;
			NotifyPropertyChanged("SelectedTag1");
		}
	}

	public TagItem SelectedTag2
	{
		get
		{
			return _selectedTag2;
		}
		set
		{
			_selectedTag2 = value;
			NotifyPropertyChanged("SelectedTag2");
		}
	}

	public TagItem SelectedTag3
	{
		get
		{
			return _selectedTag3;
		}
		set
		{
			_selectedTag3 = value;
			NotifyPropertyChanged("SelectedTag3");
		}
	}

	public TagItem SelectedTag4
	{
		get
		{
			return _selectedTag4;
		}
		set
		{
			_selectedTag4 = value;
			NotifyPropertyChanged("SelectedTag4");
		}
	}

	public PhotoSession()
	{
		FileSystemWatcher systemWatcher = new FileSystemWatcher
		{
			EnableRaisingEvents = false
		};
		RemovePluginCommand = new RelayCommand<AutoExportPluginConfig>(RemovePlugin);
		ApplyPluginCommand = new RelayCommand<AutoExportPluginConfig>(ApplyPlugin);
		Name = "DefaultSession";
		Folder = "D:\\Clorium Images\\Default";
		CaptureName = "Capture";
		FileNameTemplate = "Barcode_[Counter 3 digit]";
		Braketing = new BracketingClass();
		Files = new AsyncObservableCollection<FileItem>();
		UseOriginalFilename = false;
		AlowFolderChange = false;
		Tags = new AsyncObservableCollection<TagItem>();
		UseCameraCounter = false;
		DownloadOnlyJpg = false;
		LeadingZeros = 4;
		WriteComment = false;
		AllowOverWrite = false;
		LowerCaseExtension = true;
		AutoExportPluginConfigs = new AsyncObservableCollection<AutoExportPluginConfig>();
		TimeLapseSettings = new TimeLapseSettings();
		PrintSettings = new PrintSettings();
	}

	private void Files_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		NotifyPropertyChanged("FilesCount");
	}

	public AutoExportPluginConfig AddPlugin(IAutoExportPlugin plugin)
	{
		AutoExportPluginConfig res = new AutoExportPluginConfig(plugin);
		AutoExportPluginConfigs.Add(res);
		return res;
	}

	private void RemovePlugin(AutoExportPluginConfig plugin)
	{
		AutoExportPluginConfigs.Remove(plugin);
	}

	public static void ApplyPlugin(AutoExportPluginConfig plugin)
	{
		plugin.IsError = true;
		plugin.Error = "";
		plugin.IsRedy = true;
		IAutoExportPlugin pl = ServiceProvider.PluginManager.GetAutoExportPlugin(plugin.Type);
		try
		{
			pl.Execute(ServiceProvider.Settings.SelectedBitmap.FileItem, plugin);
			ServiceProvider.WindowsManager.ExecuteCommand("Refresh_Image");
		}
		catch (Exception ex)
		{
			plugin.IsError = true;
			plugin.Error = ex.Message;
			plugin.IsRedy = true;
			Log.Error("Error to apply plugin", ex);
		}
	}

	private void systemWatcher_Created(object sender, FileSystemEventArgs e)
	{
		try
		{
		}
		catch (Exception exception)
		{
			Log.Error("Add file error", exception);
		}
	}

	private void systemWatcher_Deleted(object sender, FileSystemEventArgs e)
	{
		FileItem deletedItem = null;
		lock (this)
		{
			foreach (FileItem fileItem in Files)
			{
				if (fileItem.FileName == e.FullPath)
				{
					deletedItem = fileItem;
				}
			}
		}
		try
		{
			if (deletedItem != null)
			{
				Files.Remove(deletedItem);
			}
		}
		catch (Exception)
		{
		}
	}

	public string GetNextFileName(string file, ICameraDevice device, string tempfile)
	{
		lock (_locker)
		{
			string ext = Path.GetExtension(file);
			if (string.IsNullOrEmpty(ext))
			{
				ext = ".nef";
			}
			int attempts = 0;
			bool templateHasCounter = FileNameTemplate != null && FileNameTemplate.Contains("[Counter");
			if (!string.IsNullOrEmpty(_lastFilename) && RawExtensions.Contains(ext.ToLower()) && !RawExtensions.Contains(Path.GetExtension(_lastFilename).ToLower()))
			{
				string rawfile = Path.Combine(Folder, FormatFileName(device, ext, tempfile, incremetCounter: false) + ((!ext.StartsWith(".")) ? "." : "") + ext);
				if (!File.Exists(rawfile))
				{
					return rawfile;
				}
			}
			string unique;
			while (true)
			{
				string candidate = ((!ServiceProvider.Settings.Background) ? Path.Combine(Folder, FormatFileName(device, file, tempfile) + ((!ext.StartsWith(".")) ? "." : "") + ext) : Path.Combine(Folder, FormatFileName(device, file, tempfile, incremetCounter: false) + ((!ext.StartsWith(".")) ? "." : "") + ".jpeg"));
				if (!File.Exists(candidate) || AllowOverWrite)
				{
					_lastFilename = candidate;
					return candidate;
				}
				attempts++;
				if (attempts >= 1000)
				{
					throw new IOException($"Unable to generate unique filename after {1000} attempts: {candidate}");
				}
				if (!templateHasCounter)
				{
					string dir = Path.GetDirectoryName(Path.Combine(Folder, candidate)) ?? Folder;
					string baseName = Path.GetFileNameWithoutExtension(candidate);
					unique = StaticHelper.GetUniqueFilename(dir + "\\" + baseName + "_", attempts - 1, Path.GetExtension(candidate));
					if (!File.Exists(unique) || AllowOverWrite)
					{
						break;
					}
				}
			}
			_lastFilename = unique;
			return unique;
		}
	}

	private string FormatFileName(ICameraDevice device, string file, string tempfile, bool incremetCounter = true)
	{
		CameraProperty property = ServiceProvider.Settings.CameraProperties.Get(device);
		if (incremetCounter)
		{
			property.Counter += property.CounterInc;
			Counter++;
		}
		string res = FileNameTemplate.Replace("Barcode", "");
		Regex regPattern = new Regex("\\[(.*?)\\]", RegexOptions.Singleline);
		MatchCollection matchX = regPattern.Matches(res);
		foreach (Match match in matchX)
		{
			if (ServiceProvider.FilenameTemplateManager.Templates.ContainsKey(match.Value))
			{
				res = res.Replace(match.Value, ServiceProvider.FilenameTemplateManager.Templates[match.Value].Pharse(match.Value, this, device, file, tempfile));
			}
		}
		while (res.Contains("\\\\"))
		{
			res = res.Replace("\\\\", "\\");
		}
		if (res.StartsWith("\\"))
		{
			res = res.Substring(1);
		}
		string CloriumID = ServiceProvider.Settings.CloriumID.Replace("_", "");
		string lastFolderName = Path.GetFileName(Folder.TrimEnd(Path.DirectorySeparatorChar));
		int index = lastFolderName.IndexOf('_');
		string barcode = ((index > 0) ? lastFolderName.Substring(index + 1) : "");
		string temp = ((!ServiceProvider.Branding.ControlMultiCamera) ? lastFolderName : (CloriumID + ServiceProvider.Settings.CameraAlias + "_" + barcode));
		res = temp + res;
		if (ServiceProvider.Settings.TurnTableShot)
		{
			string pathname = "";
			return (!ServiceProvider.Branding.ControlMultiCamera) ? (ServiceProvider.Settings.TurnTableShot_PathName + "\\" + CloriumID + ServiceProvider.Settings.TurnTableShot_FileName) : (ServiceProvider.Settings.TurnTableShot_PathName + "\\" + CloriumID + ServiceProvider.Settings.CameraAlias + "_" + ServiceProvider.Settings.TurnTableShot_FileName);
		}
		// Defect capture takes precedence over a stale front/back selection.
		if (ServiceProvider.Settings.IsDefect)
		{
			res += "_D";
		}
		else if (ServiceProvider.Settings.FrontBackMode == 1)
		{
			res += ServiceProvider.Settings.IsAestheticImprovementActive ? "_PR_F" : "_F";
		}
		else if (ServiceProvider.Settings.FrontBackMode == 2)
		{
			res += ServiceProvider.Settings.IsAestheticImprovementActive ? "_PR_B" : "_B";
		}
		else if (ServiceProvider.Settings.Background)
		{
			res += "_BG";
		}
		return res;
	}

	public FileItem AddFile(string fileName)
	{
		lock (_locker)
		{
			FileItem oitem = GetFile(fileName);
			if (oitem != null)
			{
				return oitem;
			}
			FileItem item = new FileItem(fileName);
			Files.Add(item);
			return item;
		}
	}

	public FileItem Add(FileItem item)
	{
		lock (_locker)
		{
			Files.Add(item);
			return item;
		}
	}

	public FileItem GetNewFileItem(string fileName)
	{
		lock (_locker)
		{
			FileItem oitem = GetFile(fileName);
			if (oitem != null)
			{
				return oitem;
			}
			return new FileItem(fileName);
		}
	}

	public bool ContainFile(string fileName)
	{
		lock (_locker)
		{
			return !string.IsNullOrEmpty(fileName) && Files.Any((FileItem fileItem) => fileItem.FileName.ToUpper() == fileName.ToUpper());
		}
	}

	public FileItem GetFile(string fileName)
	{
		lock (_locker)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				return null;
			}
			return Files.FirstOrDefault((FileItem fileItem) => fileItem != null && !string.IsNullOrEmpty(fileItem.FileName) && string.Equals(fileItem.FileName, fileName, StringComparison.CurrentCultureIgnoreCase));
		}
	}

	public FileItem GetByName(string name)
	{
		lock (_locker)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			return Files.FirstOrDefault((FileItem fileItem) => !string.IsNullOrEmpty(fileItem.FileName) && string.Equals(fileItem.Name, name, StringComparison.CurrentCultureIgnoreCase));
		}
	}

	public FileItem GetFile(string fileName, string serial)
	{
		lock (_locker)
		{
			return Files.FirstOrDefault((FileItem fileItem) => fileItem.OriginalName == fileName && fileItem.CameraSerial == serial);
		}
	}

	public override string ToString()
	{
		return Name;
	}

	public AsyncObservableCollection<FileItem> GetSelectedFiles()
	{
		lock (_locker)
		{
			AsyncObservableCollection<FileItem> list = new AsyncObservableCollection<FileItem>();
			foreach (FileItem fileItem in Files)
			{
				if (fileItem.IsChecked && fileItem.Visible)
				{
					list.Add(fileItem);
				}
			}
			return list;
		}
	}

	public AsyncObservableCollection<FileItem> GetSeries(int i)
	{
		lock (_locker)
		{
			AsyncObservableCollection<FileItem> list = new AsyncObservableCollection<FileItem>();
			foreach (FileItem fileItem in Files)
			{
				if (fileItem.Series == i && fileItem.Visible)
				{
					list.Add(fileItem);
				}
			}
			return list;
		}
	}

	public void SelectAll()
	{
		lock (_locker)
		{
			foreach (FileItem fileItem in Files)
			{
				fileItem.IsChecked = true;
			}
		}
	}

	public void SelectNone()
	{
		lock (_locker)
		{
			foreach (FileItem fileItem in Files)
			{
				fileItem.IsChecked = false;
			}
		}
	}

	public void SelectLiked()
	{
		lock (_locker)
		{
			foreach (FileItem fileItem in Files)
			{
				fileItem.IsChecked = fileItem.IsLiked;
			}
		}
	}

	public void OpenInLightroom()
	{
		try
		{
			ProcessStartInfo lr = new ProcessStartInfo();
			string path = GetProperLightroomPath(GetPathForExe("Phootoshop.exe"));
			if (string.IsNullOrEmpty(path))
			{
				path = GetProperLightroomPath(GetInstallLocation("Lightroom"));
			}
			bool noneSelected = false;
			lr.FileName = path;
			for (int i = 0; i < Files.Count; i++)
			{
				if (Files[i].IsChecked)
				{
					lr.Arguments = Path.Combine(Folder, Files[i].Name) + " " + lr.Arguments;
				}
			}
			if (string.IsNullOrEmpty(lr.Arguments))
			{
				lr.Arguments = Folder;
			}
			Process.Start(lr);
		}
		catch (Exception ex)
		{
			Log.Error("Unable to start Lightroom", ex);
			MessageBox.Show("Unable to start Lightroom " + ex.Message);
		}
	}

	public void OpenInPhotoshop()
	{
		try
		{
			ProcessStartInfo ps = new ProcessStartInfo();
			string path = GetPathForExe("Photoshop.exe");
			bool noneSelected = false;
			ps.FileName = path;
			for (int i = 0; i < Files.Count; i++)
			{
				if (Files[i].IsChecked)
				{
					ps.Arguments = Path.Combine(Folder, Files[i].Name) + " " + ps.Arguments;
				}
			}
			if (string.IsNullOrEmpty(ps.Arguments))
			{
				MessageBox.Show("Please Select an image first");
			}
			else
			{
				Process.Start(ps);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Unable to start Photoshop", ex);
			MessageBox.Show("Unable to start Photoshop " + ex.Message);
		}
	}

	public bool IsAvailable(string applicationName)
	{
		string registry_key = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
		bool toReturn = false;
		using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
		{
			string[] subKeyNames = key.GetSubKeyNames();
			foreach (string subkey_name in subKeyNames)
			{
				using RegistryKey subkey = key.OpenSubKey(subkey_name);
				string searchname = (string)subkey.GetValue("DisplayName");
				if (searchname != null && searchname.ToLower().Contains(applicationName.ToLower()))
				{
					toReturn = true;
				}
			}
		}
		return toReturn;
	}

	private string GetInstallLocation(string applicationName)
	{
		string registry_key = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
		string toReturn = string.Empty;
		using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
		{
			string[] subKeyNames = key.GetSubKeyNames();
			foreach (string subkey_name in subKeyNames)
			{
				using RegistryKey subkey = key.OpenSubKey(subkey_name);
				string searchname = (string)subkey.GetValue("DisplayName");
				if (searchname != null && searchname.ToLower().Contains(applicationName.ToLower()))
				{
					toReturn = (string)subkey.GetValue("InstallLocation");
				}
			}
		}
		return toReturn;
	}

	private string GetPathForExe(string fileName)
	{
		RegistryKey localMachine = Registry.LocalMachine;
		object result = null;
		using (RegistryKey fileKey = localMachine.OpenSubKey(string.Format("{0}\\{1}", "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths", fileName)))
		{
			result = ((fileKey == null) ? string.Empty : fileKey.GetValue(string.Empty));
		}
		return (string)result;
	}

	private string GetProperLightroomPath(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return path;
		}
		string[] thesplit = path.Split(Path.DirectorySeparatorChar);
		return thesplit[0] + Path.DirectorySeparatorChar + thesplit[1] + Path.DirectorySeparatorChar + thesplit[2] + Path.DirectorySeparatorChar + "Adobe Lightroom" + Path.DirectorySeparatorChar + "lightroom.exe";
	}

	public void SelectUnLiked()
	{
		lock (_locker)
		{
			foreach (FileItem fileItem in Files)
			{
				fileItem.IsChecked = fileItem.IsUnLiked;
			}
		}
	}

	public void SelectInver()
	{
		lock (_locker)
		{
			foreach (FileItem fileItem in Files)
			{
				fileItem.IsChecked = !fileItem.IsChecked;
			}
		}
	}

	public void SelectSameSeries(int s)
	{
		lock (_locker)
		{
			foreach (FileItem fileItem in Files)
			{
				fileItem.IsChecked = fileItem.Series == s;
			}
		}
	}

	public string CopyBackUp(string source, string dest)
	{
		try
		{
			if (string.IsNullOrEmpty(BackUpPath))
			{
				return null;
			}
			if (!Directory.Exists(BackUpPath))
			{
				Directory.CreateDirectory(BackUpPath);
			}
			string backupFile = Path.Combine(BackUpPath, Path.GetFileName(dest));
			if (File.Exists(backupFile))
			{
				backupFile = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
			}
			File.Copy(source, backupFile);
			return backupFile;
		}
		catch (Exception exception)
		{
			Log.Error("Unable to make backup ", exception);
			return "";
		}
	}
}
