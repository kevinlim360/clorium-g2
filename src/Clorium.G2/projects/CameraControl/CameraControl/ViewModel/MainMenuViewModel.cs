using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;

namespace CameraControl.ViewModel;

public class MainMenuViewModel : ViewModelBase
{
	public class JsonData_FolderInform
	{
		public string barcodeId { get; set; }

		public string cameraUnit { get; set; }

		public string folder { get; set; }
	}

	public class JsonData_MeasureInform
	{
		public string barcodeId { get; set; }

		public string cameraUnit { get; set; }

		public string folder { get; set; }

		public string imageFolder { get; set; }

		public string[] sizeFiles { get; set; }

		public string[] images { get; set; }

		public Measureitem measureItemGroup { get; set; }
	}

	public class Measureitem
	{
		public int typeId { get; set; }

		public Item[] items { get; set; }
	}

	public class Item
	{
		public string name { get; set; }

		public float value { get; set; }
	}

	public class SendDataInfo
	{
		// External vendor credentials are configured per deployment; empty values leave that integration unauthenticated.
		public string Vendor_aip_key = Environment.GetEnvironmentVariable("CLORIUM_VENDOR_API_KEY") ?? string.Empty;

		public string VendorID = Environment.GetEnvironmentVariable("CLORIUM_VENDOR_ID") ?? string.Empty;

		public string URL = "";

		public string JsonData = "";
	}

	public enum CommandType
	{
		CMD_Folder,
		CMD_Measure,
		RES_NoData
	}

	private static MainMenuViewModel _instance = new MainMenuViewModel();

	private ProgressWindow _dlg;

	private bool _lightroomIsInstalled;

	private bool _photoshopIsInstalled;

	public static JsonData_FolderInform Data_FolderInform = new JsonData_FolderInform();

	private static JsonData_MeasureInform Data_MeasureInform = new JsonData_MeasureInform();

	public SendDataInfo APISendData = new SendDataInfo();

	private const int MAX_MEASUREITEM = 10;

	private const int MAX_ITEM = 20;

	public static MainMenuViewModel Instance => _instance;

	public GalaSoft.MvvmLight.Command.RelayCommand<string> SendCommand { get; set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<string> ThumbSizeCommand { get; set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<string> SetLayoutCommand { get; set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<IExportPlugin> ExecuteExportPluginCommand { get; private set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<IToolPlugin> ExecuteToolPluginCommand { get; private set; }

	public RelayCommand SettingsCommand { get; set; }

	public RelayCommand ToggleFocusCommand { get; set; }

	public RelayCommand NewSessionCommand { get; private set; }

	public RelayCommand EditSessionCommand { get; private set; }

	public RelayCommand DelSessionCommand { get; private set; }

	public RelayCommand DelSessionFilesCommand { get; private set; }

	public RelayCommand RefreshSessionCommand { get; private set; }

	public RelayCommand ShowSessionCommand { get; private set; }

	public RelayCommand SelectLiked { get; private set; }

	public RelayCommand SelectUnLiked { get; private set; }

	public RelayCommand SelectNoneCommand { get; private set; }

	public RelayCommand SelectInvertCommand { get; private set; }

	public RelayCommand SelectSeries { get; private set; }

	public RelayCommand RefreshCommand { get; private set; }

	public RelayCommand CameraPropertyCommand { get; private set; }

	public RelayCommand UseAsMasterCommand { get; private set; }

	public RelayCommand FlipPreviewCommand { get; set; }

	public RelayCommand ManualPageCommand { get; set; }

	public RelayCommand HomePageCommand { get; set; }

	public RelayCommand CheckUpdateCommand { get; set; }

	public RelayCommand ForumCommand { get; set; }

	public RelayCommand SendLogFileCommand { get; set; }

	public RelayCommand ShowChangeLogCommand { get; set; }

	public RelayCommand AboutCommand { get; set; }

	public RelayCommand ExportSessionCommand { get; set; }

	public RelayCommand ImportSessionCommand { get; set; }

	public RelayCommand CopyNameClipboardCommand { get; private set; }

	public RelayCommand OpenInLightroomCommand { get; private set; }

	public RelayCommand OpenInPhotoshopCommand { get; private set; }

	public AsyncObservableCollection<IExportPlugin> ExportPlugins => ServiceProvider.PluginManager.ExportPlugins;

	public AsyncObservableCollection<IToolPlugin> ToolsPlugins => ServiceProvider.PluginManager.ToolPlugins;

	public bool ShowFocusPoints
	{
		get
		{
			return ServiceProvider.Settings.ShowFocusPoints;
		}
		set
		{
			ServiceProvider.Settings.ShowFocusPoints = value;
			RaisePropertyChanged(() => ShowFocusPoints);
		}
	}

	public bool FlipPreview
	{
		get
		{
			return ServiceProvider.Settings.FlipPreview;
		}
		set
		{
			ServiceProvider.Settings.FlipPreview = value;
			RaisePropertyChanged(() => FlipPreview);
		}
	}

	public bool ShowThumbInfo
	{
		get
		{
			return ServiceProvider.Settings.ShowThumbInfo;
		}
		set
		{
			ServiceProvider.Settings.ShowThumbInfo = value;
			RaisePropertyChanged(() => ShowThumbInfo);
		}
	}

	public bool EnhancedThumbs
	{
		get
		{
			if (IsInDesignMode)
			{
				return true;
			}
			return ServiceProvider.Settings.EnhancedThumbs;
		}
		set
		{
			ServiceProvider.Settings.EnhancedThumbs = value;
			RaisePropertyChanged(() => EnhancedThumbs);
		}
	}

	public bool HomePageMenuVisible => !string.IsNullOrEmpty(Branding.HomePageUrl);

	public Branding Branding => ServiceProvider.Branding;

	public bool CameraConnected => ServiceProvider.DeviceManager.SelectedCameraDevice != null && ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected;

	public Settings Settings => ServiceProvider.Settings;

	public bool LightroomIsInstalled
	{
		get
		{
			return _lightroomIsInstalled;
		}
		private set
		{
			_lightroomIsInstalled = value;
		}
	}

	public bool PhotoshopIsInstalled
	{
		get
		{
			return _photoshopIsInstalled;
		}
		private set
		{
			_photoshopIsInstalled = value;
		}
	}

	public string BarCodeValue { get; set; }

	public static EditSession Edit_Session { get; set; }

	public static bool IsMusinsaFrontOK { get; set; } = true;

	public static bool IsMusinsaBackOK { get; set; } = true;

	public event Action<string> BarcodeChanged;

	public void SetBarcode(string code)
	{
		BarCodeValue = code;
		BarcodeChanged?.Invoke(code);
	}

	public MainMenuViewModel()
	{
		if (IsInDesignMode)
		{
			return;
		}
		SendCommand = new GalaSoft.MvvmLight.Command.RelayCommand<string>(Send);
		SettingsCommand = new RelayCommand(EditSettings);
		ToggleFocusCommand = new RelayCommand(() =>
		{
			ShowFocusPoints = !ShowFocusPoints;
		});
		FlipPreviewCommand = new RelayCommand(() =>
		{
			FlipPreview = !FlipPreview;
		});
		ThumbSizeCommand = new GalaSoft.MvvmLight.Command.RelayCommand<string>(ThumbSize);
		SetLayoutCommand = new GalaSoft.MvvmLight.Command.RelayCommand<string>(SetLayout);
		NewSessionCommand = new RelayCommand(NewSession);
		EditSessionCommand = new RelayCommand(EditSession);
		DelSessionCommand = new RelayCommand(DelSession);
		DelSessionFilesCommand = new RelayCommand(DelSessionFiles);
		RefreshSessionCommand = new RelayCommand(RefreshSession);
		ShowSessionCommand = new RelayCommand(ShowSession);
		ImportSessionCommand = new RelayCommand(ImportSession);
		ExportSessionCommand = new RelayCommand(ExportSession);
		CameraPropertyCommand = new RelayCommand(() =>
		{
			ServiceProvider.WindowsManager.ExecuteCommand("CameraPropertyWnd_Show", ServiceProvider.DeviceManager.SelectedCameraDevice);
		});
		UseAsMasterCommand = new RelayCommand(UseAsMaster);
		RefreshCommand = new RelayCommand(Refresh);
		ExecuteExportPluginCommand = new GalaSoft.MvvmLight.Command.RelayCommand<IExportPlugin>(ExecuteExportPlugin);
		ExecuteToolPluginCommand = new GalaSoft.MvvmLight.Command.RelayCommand<IToolPlugin>(ExecuteToolPlugin);
		ManualPageCommand = new RelayCommand(() =>
		{
			HelpProvider.Run(HelpSections.MainMenu);
		});
		HomePageCommand = new RelayCommand(() =>
		{
			PhotoUtils.Run(string.IsNullOrEmpty(Branding.HomePageUrl) ? "http://www.clorium.com/" : Branding.HomePageUrl, "");
		});
		ForumCommand = new RelayCommand(() =>
		{
			PhotoUtils.Run("http://www.clorium.com", "");
		});
		SendLogFileCommand = new RelayCommand(() =>
		{
			new ErrorReportWnd("Log file").ShowDialog();
		});
		CheckUpdateCommand = new RelayCommand(() =>
		{
			NewVersionWnd.CheckForUpdate(notify: true);
		});
		ShowChangeLogCommand = new RelayCommand(NewVersionWnd.ShowChangeLog);
		AboutCommand = new RelayCommand(() =>
		{
			new AboutWnd().ShowDialog();
		});
		OpenInLightroomCommand = new RelayCommand(() =>
		{
			ServiceProvider.Settings.DefaultSession.OpenInLightroom();
		}, () => ServiceProvider.Settings.DefaultSession.IsAvailable("Lightroom"));
		OpenInPhotoshopCommand = new RelayCommand(OpenInPhotoshop, () => ServiceProvider.Settings.DefaultSession.IsAvailable("Photoshop"));
		LightroomIsInstalled = ServiceProvider.Settings.DefaultSession.IsAvailable("Lightroom");
		PhotoshopIsInstalled = ServiceProvider.Settings.DefaultSession.IsAvailable("Photoshop");
		SelectNoneCommand = new RelayCommand(() =>
		{
			ServiceProvider.Settings.DefaultSession.SelectNone();
		});
		SelectLiked = new RelayCommand(() =>
		{
			ServiceProvider.Settings.DefaultSession.SelectLiked();
		});
		SelectUnLiked = new RelayCommand(() =>
		{
			ServiceProvider.Settings.DefaultSession.SelectUnLiked();
		});
		SelectInvertCommand = new RelayCommand(() =>
		{
			ServiceProvider.Settings.DefaultSession.SelectInver();
		});
		SelectSeries = new RelayCommand(() =>
		{
			try
			{
				ServiceProvider.Settings.DefaultSession.SelectSameSeries(ServiceProvider.Settings.SelectedBitmap.FileItem.Series);
			}
			catch (Exception exception)
			{
				Log.Error("SelectSeries", exception);
			}
		});
		CopyNameClipboardCommand = new RelayCommand(() =>
		{
			try
			{
				Clipboard.SetText(ServiceProvider.Settings.SelectedBitmap.FileItem.FileName);
			}
			catch (Exception exception)
			{
				Log.Error("Copy to Clipboard fail ", exception);
				StaticHelper.Instance.SystemMessage = "Copy to Clipboard fail";
			}
		});
		if (ServiceProvider.DeviceManager != null)
		{
			ServiceProvider.DeviceManager.CameraConnected += DeviceManager_CameraConnected;
			ServiceProvider.DeviceManager.CameraDisconnected += DeviceManager_CameraConnected;
			ServiceProvider.DeviceManager.CameraSelected += DeviceManager_CameraSelected;
		}
		InitArray();
	}

	private void DeviceManager_CameraSelected(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice)
	{
		RaisePropertyChanged(() => CameraConnected);
	}

	private void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
	{
		RaisePropertyChanged(() => CameraConnected);
	}

	private void Send(string command)
	{
		ServiceProvider.WindowsManager.ExecuteCommand(command);
	}

	private void EditSettings()
	{
		if (!ServiceProvider.Settings.CheckPassword())
		{
			return;
		}
		Log.Debug("Menu : Settings Click");
		SettingsWnd wnd = new SettingsWnd();
		wnd.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
		wnd.ShowDialog();
		try
		{
			RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			if (rk != null)
			{
				if (ServiceProvider.Settings.StartupWithWindows)
				{
					rk.SetValue(Settings.AppName, Assembly.GetExecutingAssembly().Location);
				}
				else
				{
					rk.DeleteValue(Settings.AppName, throwOnMissingValue: false);
				}
			}
		}
		catch (Exception ex)
		{
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", "Usable to set startup\n" + ex.Message);
			Log.Error("Usable to set startup", ex);
		}
	}

	private void ThumbSize(string size)
	{
		ServiceProvider.Settings.ThumbHeigh = Convert.ToInt32(size);
	}

	private void SetLayout(string type)
	{
		ServiceProvider.WindowsManager.ExecuteCommand("SetLayout", type);
	}

	private void NewSession()
	{
		if (!CanStartNewCaptureSession()) return;
		if ((ServiceProvider.Settings.ModeBarcodeSession || ServiceProvider.Settings.ModeBarcodeCapture) && ServiceProvider.Settings.UseBarcodeScanner)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0252", "바코드 모드에서는 새 세션을 만들 수 없습니다."), global::CameraControl.Devices.UiText.T("M0002", "알림"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		else if (ServiceProvider.Settings.UseAlarm && !ServiceProvider.Settings.IsTimerRunning)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0253", "작업시작 버튼을 눌러 주세요."), global::CameraControl.Devices.UiText.T("M0002", "알림"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		else
		{
			if (!API_PostMeasureInform())
			{
				return;
			}
			Log.Debug("Menu : New Session Click");
			try
			{
				PhotoSession tempsession = new PhotoSession();
				EditSession editSession = new EditSession(tempsession);
				editSession.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
				RoutedEventHandler loadedHandler = null;
				loadedHandler = (object s, RoutedEventArgs e) =>
				{
					Edit_Session = editSession;
					editSession.Loaded -= loadedHandler;
				};
				editSession.Loaded += loadedHandler;
				if (editSession.ShowDialog() != true)
				{
					return;
				}
				string barcodeID = editSession.Session.Name;
				string NewFolder = CreateNewSessionFolder(barcodeID);
				if (string.IsNullOrEmpty(NewFolder)) return;
				if (ServiceProvider.Settings.UseAlarm)
				{
					int elapse_time = (int)(DateTime.Now - ServiceProvider.Settings.PrevTime).TotalSeconds;
					string barcodeScanTime = DateTime.Now.ToString("HH:mm:ss");
					ServiceProvider.WindowsManager.ExecuteCommand("ResetTimer");
					SaveCSVforElapsedTimeBetweenSessions(ServiceProvider.Settings.TodaySessionCounter, barcodeID, elapse_time, barcodeScanTime);
				}
				if (API_PostFolderInform(NewFolder, barcodeID))
				{
					IsMusinsaFrontOK = false;
					IsMusinsaBackOK = false;
					if (ServiceProvider.Settings.UseAlarm)
					{
						Instance.SetBarcode(barcodeID);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("세션 생성 에러 : ", ex);
				ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", "Error create session " + ex.Message);
			}
		}
	}

	private bool CanStartNewCaptureSession()
	{
		if (ServiceProvider.Settings.isBtnPressed || !ServiceProvider.Settings.ThumbailLoaded)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0254", "촬영 이미지의 저장과 썸네일 처리가 끝난 뒤 다음 제품으로 이동해 주세요."));
			return false;
		}
		return true;
	}

	private static void ResetCapturePurposeForNewSession()
	{
		// A retry keeps its capture purpose. A successfully created product session does not.
		ServiceProvider.Settings.IsDefect = false;
		ServiceProvider.Settings.FrontBackMode = 0;
		ServiceProvider.Settings.GetMeasurePicture = false;
		ServiceProvider.Settings.Background = false;
	}

	public string CreateNewSessionFolder(string barcord)
	{
		if (!CanStartNewCaptureSession()) return null;
		string defaultsessionfile = Path.Combine(Settings.SessionFolder, "Default.json");
		PhotoSession session = new PhotoSession();
		if (File.Exists(defaultsessionfile))
		{
			session = ServiceProvider.Settings.LoadSession(defaultsessionfile);
			session.Files.Clear();
		}
		EditSession editSession = new EditSession(session);
		editSession.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
		string year = DateTime.Now.ToString("yyyy");
		string month = DateTime.Now.ToString("MM");
		string day = DateTime.Now.ToString("dd");
		editSession.Session.Name = ServiceProvider.Settings.CloriumID + barcord;
		editSession.Session.Folder = Path.Combine(ServiceProvider.Settings.DownloadFolder, year.ToString() + "년" + month.ToString() + "월\\", day.ToString() + "일\\", editSession.Session.Name);
		string orginalFolder = editSession.Session.Folder;
		for (int i = 1; i < 100; i++)
		{
			if (!Directory.Exists(editSession.Session.Folder))
			{
				Directory.CreateDirectory(editSession.Session.Folder);
				break;
			}
			editSession.Session.Folder = orginalFolder + "___" + i;
			Log.Debug("세션 중복 : " + i, ServiceProvider.Settings.UseDebugLog);
		}
		ServiceProvider.Settings.Add(editSession.Session);
		ServiceProvider.Settings.DefaultSession = editSession.Session;
		ResetCapturePurposeForNewSession();
		ServiceProvider.Settings.TodaySessionCounter++;
		ServiceProvider.Settings.TotalSessionCounter++;
		return editSession.Session.Folder;
	}

	private void EditSession()
	{
		Log.Debug("Menu : Edit Session Click");
		try
		{
			EditSession editSession = new EditSession(ServiceProvider.Settings.DefaultSession);
			editSession.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
			editSession.ShowDialog();
			ServiceProvider.Settings.Save(ServiceProvider.Settings.DefaultSession);
		}
		catch (Exception ex)
		{
			Log.Error("세션 편집 에러 : ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", ex.Message);
		}
	}

	// G2: 파일·세션 > 세션 목록에서 제거 — the session leaves the list; its photo folder and files stay on disk.
	private void DelSession()
	{
		Log.Debug("Menu : Delete Session Click");
		PhotoSession session = ServiceProvider.Settings.DefaultSession;
		if (session == null || !CanRemoveSession())
		{
			return;
		}
		if (G2Dialog.Show("'" + session.Name + "' 세션을 목록에서 제거할까요?\n\n사진 파일과 폴더는 그대로 남습니다.\n" + session.Folder, "세션 목록에서 제거", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
		{
			RemoveSessionFromList(session);
		}
	}

	private static bool CanRemoveSession()
	{
		if (ServiceProvider.Settings.PhotoSessions.Count > 1)
		{
			return true;
		}
		ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", TranslationStrings.MsgLastSessionCantBeDeleted);
		G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0258", "세션이 하나뿐이면 삭제할 수 없습니다."));
		return false;
	}

	// Removes a session from the session list only (menu, Delete key in the top-bar session popup). Files and folder stay.
	public static bool RemoveSessionFromList(PhotoSession session)
	{
		Settings settings = ServiceProvider.Settings;
		if (session == null || !settings.PhotoSessions.Contains(session) || !CanRemoveSession())
		{
			return false;
		}
		try
		{
			// Out of the list first: switching away saves the old session's .json in the background only while it is listed.
			settings.PhotoSessions.Remove(session);
			if (settings.DefaultSession == session)
			{
				settings.DefaultSession = settings.PhotoSessions[0];
			}
			DeleteSessionConfig(session);
			settings.Save();
			Log.Debug("세션 목록에서 제거 : " + session.Name + " (" + session.Folder + ")");
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("세션 지우기 에러 : ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", TranslationStrings.LabelUnabletoDeleteSession + ex.Message);
			return false;
		}
	}

	// The session's .json in the data folder; a save still writing it gets a moment to finish.
	private static void DeleteSessionConfig(PhotoSession session)
	{
		if (string.IsNullOrEmpty(session.ConfigFile))
		{
			return;
		}
		for (int attempt = 1; File.Exists(session.ConfigFile); attempt++)
		{
			try
			{
				File.Delete(session.ConfigFile);
			}
			catch (IOException ex) when (attempt < 20)
			{
				Log.Debug("세션 파일 사용 중, 다시 시도 : " + ex.Message);
				Thread.Sleep(100);
			}
		}
	}

	// G2: 파일·세션 > 세션 및 파일 삭제 — after a confirmation, the current session's folder (with every file in it) goes to
	// the Recycle Bin and the session leaves the list. Refuses folders that hold the download root or another session.
	private void DelSessionFiles()
	{
		Log.Debug("Menu : Delete Session And Files Click");
		Settings settings = ServiceProvider.Settings;
		PhotoSession session = settings.DefaultSession;
		const string caption = "세션 및 파일 삭제";
		if (session == null || !CanRemoveSession())
		{
			return;
		}
		if (ServiceProvider.DeviceManager.SelectedCameraDevice?.IsBusy == true)
		{
			G2Dialog.Show("촬영이나 사진 전송이 끝난 뒤 다시 시도해 주세요.", caption);
			return;
		}
		string folder = string.IsNullOrWhiteSpace(session.Folder) ? null : Path.GetFullPath(session.Folder).TrimEnd('\\');
		string refusal = SessionFolderDeleteRefusal(session, folder);
		if (refusal != null)
		{
			G2Dialog.Show(refusal, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		bool exists = Directory.Exists(folder);
		string detail;
		if (exists)
		{
			long bytes = 0;
			int count = 0;
			foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
			{
				count++;
				bytes += new System.IO.FileInfo(file).Length;
			}
			detail = folder + "\n파일 " + count + "개 · " + (bytes / 1048576.0).ToString("0.#") + " MB\n\n폴더와 그 안의 모든 파일이 휴지통으로 이동합니다.";
		}
		else
		{
			detail = folder + "\n(폴더가 이미 없습니다. 세션만 목록에서 제거합니다.)";
		}
		if (G2Dialog.Show("'" + session.Name + "' 세션과 사진 파일을 정말 삭제할까요?\n\n" + detail, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
		{
			return;
		}
		// out of the list (and switched away, so its thumbnails let go of the files) before the folder goes
		if (!RemoveSessionFromList(session))
		{
			return;
		}
		try
		{
			if (exists)
			{
				Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(folder, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
			}
			Log.Debug("세션 및 파일 삭제 : " + session.Name + " (" + folder + ")");
		}
		catch (Exception ex)
		{
			Log.Error("세션 및 파일 삭제 에러 : ", ex);
			// the folder (or part of it) is still there: put the session back in the list
			settings.Add(session);
			settings.Save();
			G2Dialog.Show("폴더를 삭제하지 못했습니다. 세션은 목록에 다시 넣어 둡니다.\n" + ex.Message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
		}
	}

	private static string SessionFolderDeleteRefusal(PhotoSession session, string folder)
	{
		if (folder == null)
		{
			return "이 세션에는 폴더가 지정되어 있지 않습니다.";
		}
		if (Path.GetPathRoot(folder).TrimEnd('\\').Equals(folder, StringComparison.OrdinalIgnoreCase))
		{
			return "드라이브 전체는 삭제할 수 없습니다.\n" + folder;
		}
		string download = ServiceProvider.Settings.DownloadFolder;
		if (!string.IsNullOrWhiteSpace(download) && IsSameOrInside(Path.GetFullPath(download).TrimEnd('\\'), folder))
		{
			return "사진 저장 기본 폴더(또는 그 상위 폴더)는 삭제할 수 없습니다.\n" + folder;
		}
		foreach (PhotoSession other in ServiceProvider.Settings.PhotoSessions)
		{
			if (other == session || string.IsNullOrWhiteSpace(other.Folder))
			{
				continue;
			}
			string otherFolder = Path.GetFullPath(other.Folder).TrimEnd('\\');
			if (IsSameOrInside(otherFolder, folder) || IsSameOrInside(folder, otherFolder))
			{
				return "다른 세션 '" + other.Name + "'과 폴더가 겹쳐 삭제할 수 없습니다.\n" + folder + "\n" + otherFolder;
			}
		}
		return null;
	}

	// true when path is folder itself or somewhere below it
	private static bool IsSameOrInside(string path, string folder)
	{
		return path.Equals(folder, StringComparison.OrdinalIgnoreCase) || path.StartsWith(folder + "\\", StringComparison.OrdinalIgnoreCase);
	}

	private void ExportSession()
	{
		Log.Debug("Menu : Export Session Click");
		SaveFileDialog dialog = new SaveFileDialog();
		dialog.FileName = ServiceProvider.Settings.DefaultSession.Name + ".dccsession";
		dialog.Filter = "Session files (*.dccsession)|*.dccsession|All files (*.*)|*.*";
		if (dialog.ShowDialog() == true)
		{
			try
			{
				string tempfile = Path.GetTempFileName();
				ServiceProvider.Settings.SaveSession(ServiceProvider.Settings.DefaultSession, tempfile);
				PhotoSession session = ServiceProvider.Settings.LoadSession(tempfile);
				session.Files.Clear();
				ServiceProvider.Settings.SaveSession(session, dialog.FileName);
			}
			catch (Exception ex)
			{
				Log.Error("세션 내보기기 에러 :", ex);
				ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", "Unable to export session " + ex.Message);
			}
		}
	}

	private void ImportSession()
	{
		Log.Debug("Menu : Import Session Click");
		OpenFileDialog dialog = new OpenFileDialog();
		dialog.Filter = "Session files (*.dccsession)|*.dccsession|All files (*.*)|*.*";
		if (dialog.ShowDialog() != true)
		{
			return;
		}
		try
		{
			PhotoSession session = ServiceProvider.Settings.LoadSession(dialog.FileName);
			if (ServiceProvider.Settings.PhotoSessions.Any((PhotoSession photoSession) => photoSession.Name.ToLower() == session.Name.ToLower()))
			{
				throw new Exception("Session with same name already exist !");
			}
			ServiceProvider.Settings.Add(session);
			ServiceProvider.Settings.DefaultSession = session;
		}
		catch (Exception ex)
		{
			Log.Error("세션 가져오기 에러 : ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", "Unable to import session " + ex.Message);
		}
	}

	private void RefreshSession()
	{
		try
		{
			ServiceProvider.Settings.LoadData(ServiceProvider.Settings.DefaultSession);
		}
		catch (Exception ex)
		{
			Log.Error("세션 갱신하기 에러 : ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", ex.Message);
		}
	}

	private void ShowSession()
	{
		try
		{
			PhotoUtils.Run(ServiceProvider.Settings.DefaultSession.Folder);
		}
		catch (Exception ex)
		{
			Log.Error("세션 보기 에러 : ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", ex.Message);
		}
	}

	private void UseAsMaster()
	{
		Log.Debug("Menu : Copy Porperty to Multi Cameras Click");
		if (_dlg == null)
		{
			_dlg = new ProgressWindow();
		}
		_dlg.Show();
		Thread thread = new Thread(SetAsMaster);
		thread.Start();
	}

	public void SetAsMaster()
	{
		try
		{
			int i = 0;
			_dlg.MaxValue = ServiceProvider.DeviceManager.ConnectedDevices.Count;
			CameraPreset preset = new CameraPreset();
			preset.Get(ServiceProvider.DeviceManager.SelectedCameraDevice);
			foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices)
			{
				if (connectedDevice.DeviceName == "Z-App camera")
				{
					continue;
				}
				if (connectedDevice.DeviceName == "Z-FTP camera")
				{
					return;
				}
				if (connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName || connectedDevice == null || !connectedDevice.IsConnected)
				{
					continue;
				}
				try
				{
					if (connectedDevice != ServiceProvider.DeviceManager.SelectedCameraDevice)
					{
						_dlg.Label = connectedDevice.DisplayName;
						_dlg.Progress = i;
						i++;
						preset.Set(connectedDevice);
					}
				}
				catch (Exception exception)
				{
					Log.Error("케메라 속성 변경 에러 : ", exception);
				}
				Thread.Sleep(250);
			}
		}
		catch (Exception exception2)
		{
			Log.Error("속성 마스터 지정 에러 : ", exception2);
		}
		_dlg.Hide();
	}

	private void Refresh()
	{
		Log.Debug("Menu : Reconnect to Camera Click");
		try
		{
			ServiceProvider.DeviceManager.ConnectToCamera(ServiceProvider.Settings.UseDebugLog);
		}
		catch (Exception ex)
		{
			Log.Error("카메라 연결 에러 : ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", "Unable to connect \n" + ex.Message);
		}
	}

	private void ExecuteExportPlugin(IExportPlugin obj)
	{
		try
		{
			obj.Execute();
		}
		catch (Exception ex)
		{
			Log.Error("Error ExecuteExportPlugin ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", ex.Message);
		}
	}

	private void ExecuteToolPlugin(IToolPlugin obj)
	{
		try
		{
			obj.Execute();
		}
		catch (Exception ex)
		{
			Log.Error("Error ExecuteToolPlugin ", ex);
			ServiceProvider.WindowsManager.ExecuteCommand("MainWnd_Message", ex.Message);
		}
	}

	private void OpenInPhotoshop()
	{
		ServiceProvider.Settings.DefaultSession.OpenInPhotoshop();
	}

	private void SaveCSVforElapsedTimeBetweenSessions(int count, string barcode, int elapsedtime, string scanTime)
	{
		string directory = Path.Combine(ServiceProvider.Settings.ElapsedDownloadFolder, DateTime.Now.ToString("yyyy년MM월dd일"));
		string filename = Path.Combine(directory, ServiceProvider.Settings.CloriumID + DateTime.Now.ToString("yyMMdd") + ".csv");
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}
		try
		{
			WaitUntilFileIsFree(filename);
			if (File.Exists(filename) && new System.IO.FileInfo(filename).Length > 0)
			{
				string[] lines = File.ReadAllLines(filename, Encoding.UTF8);
				if (lines.Length >= 7)
				{
					string avgLine = lines[lines.Length - 4];
					string totalLine = lines[lines.Length - 3];
					if (avgLine.Contains("평균시간(초)") || totalLine.Contains("누적시간(초)"))
					{
						string[] pureLines = lines.Take(lines.Length - 5).ToArray();
						File.WriteAllLines(filename, pureLines, Encoding.UTF8);
					}
				}
			}
			using StreamWriter sw = new StreamWriter(filename, append: true, Encoding.UTF8);
			if (new System.IO.FileInfo(filename).Length == 0)
			{
				sw.WriteLine("횟수,바코드,경과시간,촬영시각");
			}
			string csvLine = $"{count},{barcode},{elapsedtime},{scanTime}";
			sw.WriteLine(csvLine);
		}
		catch (IOException)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0260", "CSV 파일을 쓰는 중입니다."), global::CameraControl.Devices.UiText.T("M0214", "경고"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
	}

	private void WaitUntilFileIsFree(string filePath)
	{
		while (true)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					break;
				}
				using (File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
				{
					break;
				}
			}
			catch (IOException)
			{
				global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0261", "CSV 파일을 먼저 닫은 뒤 이 창을 닫아 주세요."), global::CameraControl.Devices.UiText.T("M0214", "경고"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
		}
	}

	private bool API_PostFolderInform(string folder, string barcode)
	{
		if (!ServiceProvider.Settings.UseAPICommunication)
		{
			return true;
		}
		Data_FolderInform.cameraUnit = ServiceProvider.Settings.CloriumID;
		Data_FolderInform.folder = folder;
		Data_FolderInform.barcodeId = barcode;
		APISendData.JsonData = CreateJsonData_Folder(Data_FolderInform.cameraUnit, Data_FolderInform.folder, Data_FolderInform.barcodeId);
		APISendData.URL = ServiceProvider.Settings.APIHost + "/api2/recommerce/external/" + APISendData.VendorID + "/barcodes/" + Data_FolderInform.barcodeId + "/image-folder";
		if (!webAPI_Post(APISendData.Vendor_aip_key, APISendData.URL, APISendData.JsonData))
		{
			Log.Debug("API 송신 실패");
			Data_FolderInform.cameraUnit = "";
			Data_FolderInform.folder = "";
			Data_FolderInform.barcodeId = "";
			return false;
		}
		if (ServiceProvider.Settings.ShowAPICommunication)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0262", "API로 전송합니다.\n") + APISendData.JsonData, global::CameraControl.Devices.UiText.T("M0002", "알림"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		return true;
	}

	public bool API_PostMeasureInform()
	{
		if (!ServiceProvider.Settings.UseAPICommunication)
		{
			return true;
		}
		if (Data_FolderInform.cameraUnit == null || Data_FolderInform.cameraUnit == "")
		{
			return true;
		}
		if (ServiceProvider.Settings.UseAlarm && !IsMusinsaFrontOK && !IsMusinsaBackOK)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0263", "앞면과 뒷면을 모두 촬영해 주세요."), global::CameraControl.Devices.UiText.T("M0002", "알림"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return false;
		}
		if (ServiceProvider.Settings.UseAlarm && !IsMusinsaFrontOK)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0264", "앞면을 촬영해 주세요."), global::CameraControl.Devices.UiText.T("M0002", "알림"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return false;
		}
		if (ServiceProvider.Settings.UseAlarm && !IsMusinsaBackOK)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0265", "뒷면을 촬영해 주세요."), global::CameraControl.Devices.UiText.T("M0002", "알림"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return false;
		}
		makeMeasureInformData();
		APISendData.JsonData = MakeJsonData_Measure(Data_MeasureInform.cameraUnit, Data_MeasureInform.folder, Data_MeasureInform.imageFolder, Data_MeasureInform.barcodeId, Data_MeasureInform.sizeFiles, Data_MeasureInform.measureItemGroup, Data_MeasureInform.images);
		APISendData.URL = ServiceProvider.Settings.APIHost + "/api2/recommerce/external/" + APISendData.VendorID + "/barcodes/" + Data_MeasureInform.barcodeId + "/measurements";
		if (!webAPI_Post(APISendData.Vendor_aip_key, APISendData.URL, APISendData.JsonData))
		{
			Log.Debug("API 송신 실패");
			return false;
		}
		if (ServiceProvider.Settings.ShowAPICommunication)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0262", "API로 전송합니다.\n") + APISendData.JsonData, global::CameraControl.Devices.UiText.T("M0002", "알림"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
		}
		return true;
	}

	private void makeMeasureInformData()
	{
		Data_MeasureInform.cameraUnit = Data_FolderInform.cameraUnit;
		Data_MeasureInform.barcodeId = Data_FolderInform.barcodeId;
		Data_MeasureInform.imageFolder = Data_FolderInform.folder;
		Data_MeasureInform.folder = Data_FolderInform.folder + "\\치수측정";
		Data_MeasureInform.sizeFiles[0] = "";
		Data_MeasureInform.sizeFiles[1] = "";
		Data_MeasureInform.sizeFiles[2] = "";
		GetMeasureFilename(Data_MeasureInform.folder, Data_MeasureInform.sizeFiles);
		GetCapturedFilename(Data_MeasureInform.imageFolder, Data_MeasureInform.images);
	}

	private void GetMeasureFilename(string imageFolder, string[] sizeFiles)
	{
		if (Directory.Exists(imageFolder))
		{
			string[] files = Directory.GetFiles(imageFolder);
			Array.Sort(files);
			int fileCount = 0;
			string[] array = files;
			foreach (string file in array)
			{
				if (Path.GetExtension(file).ToLower() == ".jpeg")
				{
					sizeFiles[fileCount++] = Path.GetFileName(file);
				}
			}
		}
		else
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0267", "이미지 폴더가 없습니다."));
		}
	}

	private void GetCapturedFilename(string imageFolder, string[] imagenames)
	{
		for (int i = 0; i < imagenames.Length; i++)
		{
			imagenames[i] = null;
		}
		if (Directory.Exists(imageFolder))
		{
			string[] files = Directory.GetFiles(imageFolder);
			Array.Sort(files);
			int fileCount = 0;
			string[] array = files;
			foreach (string file in array)
			{
				if (Path.GetExtension(file).ToLower() == ".jpg")
				{
					imagenames[fileCount++] = Path.GetFileName(file);
				}
			}
		}
		else
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0267", "이미지 폴더가 없습니다."));
		}
	}

	private void InitArray()
	{
		Data_FolderInform = new JsonData_FolderInform
		{
			barcodeId = "",
			cameraUnit = "",
			folder = ""
		};
		JsonData_MeasureInform jsonData_MeasureInform = new JsonData_MeasureInform();
		jsonData_MeasureInform.barcodeId = "";
		jsonData_MeasureInform.cameraUnit = "";
		jsonData_MeasureInform.folder = "";
		jsonData_MeasureInform.imageFolder = "";
		jsonData_MeasureInform.sizeFiles = new string[20];
		jsonData_MeasureInform.images = new string[20];
		jsonData_MeasureInform.measureItemGroup = new Measureitem();
		Data_MeasureInform = jsonData_MeasureInform;
		for (int i = 0; i < 10; i++)
		{
			Data_MeasureInform.measureItemGroup = new Measureitem
			{
				typeId = 0,
				items = new Item[20]
			};
			for (int j = 0; j < 20; j++)
			{
				Data_MeasureInform.measureItemGroup.items[j] = new Item
				{
					name = "",
					value = 0f
				};
			}
		}
	}

	private string CreateJsonData_Folder(string cameraUnit, string folder, string barcodeid)
	{
		JsonData_FolderInform jsonData = new JsonData_FolderInform
		{
			barcodeId = barcodeid,
			cameraUnit = cameraUnit,
			folder = folder
		};
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			WriteIndented = true
		};
		return JsonSerializer.Serialize(jsonData, options);
	}

	public string MakeJsonData_Measure(string cameraUnit, string folder, string imagefolder, string barcodeid, string[] sizefiles, Measureitem measureItemGroup, string[] images)
	{
		JsonData_MeasureInform jsonData = new JsonData_MeasureInform
		{
			barcodeId = barcodeid,
			cameraUnit = cameraUnit,
			folder = folder,
			imageFolder = imagefolder,
			sizeFiles = sizefiles,
			images = images,
			measureItemGroup = measureItemGroup
		};
		jsonData.sizeFiles = jsonData.sizeFiles.Where((string image) => !string.IsNullOrEmpty(image)).ToArray();
		jsonData.images = jsonData.images.Where((string image) => !string.IsNullOrEmpty(image)).ToArray();
		measureItemGroup.items = measureItemGroup.items.Where((Item item) => !string.IsNullOrEmpty(item.name) && item.value != 0f).ToArray();
		JsonSerializerOptions options = new JsonSerializerOptions
		{
			WriteIndented = true
		};
		return JsonSerializer.Serialize(jsonData, options);
	}

	private bool webAPI_Get(string venderapikey, string url, CommandType command = CommandType.RES_NoData)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
		httpWebRequest.Method = "GET";
		httpWebRequest.ContentType = "application/json";
		httpWebRequest.Timeout = 30000;
		httpWebRequest.Headers.Add("x-api-key", venderapikey);
		return API_Respose(httpWebRequest, command);
	}

	private bool webAPI_Post(string venderapikey, string url, string data, CommandType command = CommandType.RES_NoData)
	{
		try
		{
			byte[] bytes = Encoding.ASCII.GetBytes(data);
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/json";
			httpWebRequest.Timeout = 30000;
			httpWebRequest.ContentLength = bytes.Length;
			httpWebRequest.Headers.Add("x-api-key", venderapikey);
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
				stream.Close();
			}
			return API_Respose(httpWebRequest, command);
		}
		catch (WebException ex)
		{
			if (ex.Response is HttpWebResponse errorResponse)
			{
				int statusCode = (int)errorResponse.StatusCode;
				global::CameraControl.Core.Classes.G2Dialog.Show(string.Format(global::CameraControl.Devices.UiText.T("M0268", "API 서버 오류: 코드 {0} ({1})"), statusCode, errorResponse.StatusDescription));
			}
			else
			{
				global::CameraControl.Core.Classes.G2Dialog.Show(string.Format(global::CameraControl.Devices.UiText.T("M0269", "네트워크 연결에 실패했습니다: {0}"), ex.Status));
			}
			return false;
		}
		catch (Exception ex2)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0270", "오류가 발생했습니다: ") + ex2.Message);
			return false;
		}
	}

	private bool API_Respose(HttpWebRequest request, CommandType command)
	{
		try
		{
			using (HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse())
			{
				if (command != CommandType.RES_NoData)
				{
					using StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream());
					string result = streamReader.ReadToEnd();
					if (command == CommandType.CMD_Folder)
					{
						Data_FolderInform = JsonSerializer.Deserialize<JsonData_FolderInform>(result);
					}
				}
			}
			return true;
		}
		catch (WebException ex)
		{
			string message = global::CameraControl.Devices.UiText.T("M0271", "네트워크 오류: ") + ex.Message;
			if (ex.Response != null && ex.Response is HttpWebResponse errorResponse)
			{
				using Stream stream = errorResponse.GetResponseStream();
				if (stream != null)
				{
					using StreamReader reader = new StreamReader(stream);
					string errorBody = reader.ReadToEnd();
					message = message + $"\nHTTP {(int)errorResponse.StatusCode} - {errorResponse.StatusCode}" + global::CameraControl.Devices.UiText.T("M0272", "\n응답 메시지:\n") + errorBody;
				}
			}
			global::CameraControl.Core.Classes.G2Dialog.Show(message, global::CameraControl.Devices.UiText.T("M0002", "알림"), MessageBoxButton.OK, MessageBoxImage.Asterisk);
			return false;
		}
	}
}
