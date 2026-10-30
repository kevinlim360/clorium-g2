using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices.Classes;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;

namespace CameraControl.ViewModel;

internal class AIRemoveBackgroundViewModel : ViewModelBase
{
	public class PhotoRoomInfo
	{
		public string API_Key;

		public string pathfilename;
	}

	public class Json_Credits
	{
		public Credits credits { get; set; }
	}

	public class Credits
	{
		public int available { get; set; }

		public int subscription { get; set; }
	}

	private bool bStopConvert = false;

	private ObservableCollection<FileItem> _files;

	private BitmapSource _previewBitmap;

	private FileItem _selectedFileItem;

	private string _validCreditCount;

	private string _licenseKey;

	private string _folder;

	private string _progressRate;

	private bool _startProgress;

	private bool _isIndeterminate;

	private int _convertedCount;

	private int _maxConvertCount;

	public static PhotoRoomInfo photoRoomInfo = new PhotoRoomInfo();

	public static Json_Credits json_Credit_data;

	public RelayCommand SaveLicenseKeyCommand { get; set; }

	public RelayCommand ConvertOneCommand { get; set; }

	public RelayCommand StartConvertCommand { get; set; }

	public RelayCommand StopConvertCommand { get; set; }

	public RelayCommand ReloadCommand { get; set; }

	public RelayCommand BuyCreditCommand { get; set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<FileItem> RemoveItemCommand { get; set; }

	public ObservableCollection<FileItem> Files
	{
		get
		{
			return _files;
		}
		set
		{
			_files = value;
			RaisePropertyChanged(() => Files);
		}
	}

	public BitmapSource PreviewBitmap
	{
		get
		{
			return _previewBitmap;
		}
		set
		{
			_previewBitmap = value;
			RaisePropertyChanged(() => PreviewBitmap);
		}
	}

	public FileItem SelectedFileItem
	{
		get
		{
			return _selectedFileItem;
		}
		set
		{
			_selectedFileItem = value;
			if (_selectedFileItem != null && File.Exists(_selectedFileItem.LargeThumb))
			{
				PreviewBitmap = BitmapLoader.Instance.LoadImage(_selectedFileItem.LargeThumb);
			}
			RaisePropertyChanged(() => SelectedFileItem);
		}
	}

	public PhotoSession Session => ServiceProvider.Settings.DefaultSession;

	public string ValidCreditCount
	{
		get
		{
			return _validCreditCount;
		}
		set
		{
			_validCreditCount = value;
			RaisePropertyChanged(() => ValidCreditCount);
		}
	}

	public string LicenseKey
	{
		get
		{
			return _licenseKey;
		}
		set
		{
			_licenseKey = value;
			RaisePropertyChanged(() => LicenseKey);
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
			_folder = value;
			RaisePropertyChanged(() => Folder);
		}
	}

	public string ProgressRate
	{
		get
		{
			return _progressRate;
		}
		set
		{
			_progressRate = value;
			RaisePropertyChanged(() => ProgressRate);
		}
	}

	public bool StartProgress
	{
		get
		{
			return _startProgress;
		}
		set
		{
			_startProgress = value;
			RaisePropertyChanged(() => StartProgress);
		}
	}

	public bool IsIndeterminate
	{
		get
		{
			return _isIndeterminate;
		}
		set
		{
			_isIndeterminate = value;
			RaisePropertyChanged(() => IsIndeterminate);
		}
	}

	public int ConvertedCount
	{
		get
		{
			return _convertedCount;
		}
		set
		{
			_convertedCount = value;
			RaisePropertyChanged(() => ConvertedCount);
		}
	}

	public int MaxConvertCount
	{
		get
		{
			return _maxConvertCount;
		}
		set
		{
			_maxConvertCount = value;
			RaisePropertyChanged(() => MaxConvertCount);
		}
	}

	public AIRemoveBackgroundViewModel()
	{
	}

	public AIRemoveBackgroundViewModel(Window window)
	{
		ServiceProvider.FileTransfered += ServiceProvider_FileTransfered;
		ServiceProvider.WindowsManager.Event += WindowsManager_Event;
		InitCommands();
		LoadData();
		if (Files.Count > 0)
		{
			SelectedFileItem = Files[0];
		}
	}

	public void InitCommands()
	{
		BuyCreditCommand = new RelayCommand(BuyCredit);
		SaveLicenseKeyCommand = new RelayCommand(SaveLicenseKey);
		ConvertOneCommand = new RelayCommand(ConvertOne);
		RemoveItemCommand = new GalaSoft.MvvmLight.Command.RelayCommand<FileItem>(RemoveItem);
		ReloadCommand = new RelayCommand(Reload);
		StartConvertCommand = new RelayCommand(StartConvertAll);
		StopConvertCommand = new RelayCommand(StopConvert);
	}

	public void Init()
	{
		ReadMDP("Clorium_AIBgLicense.txt");
		GetValidCredit();
		Folder = Session.Folder;
	}

	public void window_Closing()
	{
		ServiceProvider.FileTransfered -= ServiceProvider_FileTransfered;
		ServiceProvider.WindowsManager.Event -= WindowsManager_Event;
	}

	private void ServiceProvider_FileTransfered(object sender, FileItem fileItem)
	{
		if (Files.Count > 0 && Files[0].Series != fileItem.Series)
		{
			Files.Clear();
		}
		Files.Add(fileItem);
	}

	private void WindowsManager_Event(string cmd, object o)
	{
	}

	private void LoadData()
	{
		Files = new AsyncObservableCollection<FileItem>();
		if (Session.Files.Count != 0 && ServiceProvider.Settings.SelectedBitmap.FileItem != null)
		{
			AsyncObservableCollection<FileItem> files = Session.GetSelectedFiles();
			if (files.Count > 0)
			{
				Files = files;
			}
			else
			{
				Files = Session.GetSeries(ServiceProvider.Settings.SelectedBitmap.FileItem.Series);
			}
			MaxConvertCount = Files.Count;
		}
	}

	private void Reload()
	{
		LoadData();
	}

	private void RemoveItem(FileItem obj)
	{
		if (Files.Contains(obj))
		{
			Files.Remove(obj);
		}
		MaxConvertCount = Files.Count;
	}

	private void BuyCredit()
	{
		string url = "https://app.photoroom.com/api-dashboard";
		Process.Start(url);
	}

	private void SaveLicenseKey()
	{
		WriteMDP("D:\\Clorium Data\\Clorium_AIBgLicense.txt");
		GetValidCredit();
	}

	private void ConvertOne()
	{
		OpenFileDialog dialog = new OpenFileDialog();
		dialog.Filter = "Jpg file|*.Jpg|All files|*.*";
		dialog.InitialDirectory = Session.Folder;
		if (dialog.ShowDialog() == true)
		{
			StartConvertOne(dialog.FileName);
		}
	}

	private void WriteMDP(string file)
	{
		IniFile ini = new IniFile();
		ini["AI License Key"]["License Key"] = LicenseKey;
		photoRoomInfo.API_Key = LicenseKey;
		ini.Save(file);
	}

	public void ReadMDP(string file)
	{
		if (File.Exists(file))
		{
			try
			{
				IniFile ini = new IniFile();
				ini.Load(file);
				LicenseKey = ini["AI License Key"]["License Key"].ToString();
				photoRoomInfo.API_Key = LicenseKey;
				return;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				return;
			}
		}
		MessageBox.Show("PhotoRoom License Key 정보 파일을 열수가 없습니다.");
	}

	public async void GetValidCredit()
	{
		await AccountAsync();
		ValidCreditCount = "사용 가능 : " + json_Credit_data.credits.available + " [개]";
	}

	private void StartConvertOne(string filename)
	{
		Thread exe = new Thread(() =>
		{
			ConvertOne_JpgtoPng(filename);
		});
		exe.Start();
	}

	private void StartConvertAll()
	{
		Thread exe = new Thread(() =>
		{
			ConvertAll_JpgtoPng();
		});
		exe.Start();
	}

	private void StopConvert()
	{
		bStopConvert = true;
	}

	private async void ConvertOne_JpgtoPng(string filename)
	{
		StartProgress = true;
		IsIndeterminate = true;
		photoRoomInfo.pathfilename = filename;
		await SendNReadAsync();
		StartProgress = false;
	}

	private async void ConvertAll_JpgtoPng()
	{
		StartProgress = true;
		IsIndeterminate = false;
		bStopConvert = false;
		ConvertedCount = 0;
		ProgressRate = "^^ 변환중입니다. ^^[" + ConvertedCount + "/" + MaxConvertCount + "]";
		for (int i = 0; i < Files.Count; i++)
		{
			SelectedFileItem = Files[i];
			photoRoomInfo.pathfilename = SelectedFileItem.FileName;
			await SendNReadAsync();
			ConvertedCount++;
			ProgressRate = "^^ 변환중입니다. ^^[" + ConvertedCount + "/" + MaxConvertCount + "]";
			if (bStopConvert)
			{
				break;
			}
		}
		StartProgress = false;
	}

	// Recovery inference: post the selected image using the configured license key and save a returned PNG beside the input file.
	public static async Task SendNReadAsync()
	{
		string apiKey = photoRoomInfo.API_Key;
		string apiUrl = "https://sdk.photoroom.com/v1/segment";
		string imagePath = photoRoomInfo.pathfilename;
		using HttpClient httpClient = new HttpClient();
		using MultipartFormDataContent content = new MultipartFormDataContent();
		httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
		ByteArrayContent imageContent = new ByteArrayContent(File.ReadAllBytes(imagePath));
		imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
		content.Add(imageContent, "image_file", "image.jpg");
		HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);
		if (response.IsSuccessStatusCode)
		{
			Console.WriteLine("Background removal successful.");
			byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
			string outputPath = Path.GetDirectoryName(imagePath) + "\\" + Path.GetFileNameWithoutExtension(imagePath) + ".png";
			File.WriteAllBytes(outputPath, responseBytes);
			Console.WriteLine("Resulting image saved to: " + outputPath);
		}
		else
		{
			Console.WriteLine("Error: " + response.ReasonPhrase);
			Console.WriteLine("Error message: " + await response.Content.ReadAsStringAsync());
		}
	}

	public static async Task AccountAsync()
	{
		HttpClient client = new HttpClient();
		HttpRequestMessage request = new HttpRequestMessage
		{
			Method = HttpMethod.Get,
			RequestUri = new Uri("https://beta-sdk.photoroom.com/v1/account"),
			Headers = 
			{
				{ "Accept", "application/json" },
				{ "x-api-key", photoRoomInfo.API_Key }
			}
		};
		using HttpResponseMessage response = await client.SendAsync(request);
		try
		{
			response.EnsureSuccessStatusCode();
			json_Credit_data = JsonSerializer.Deserialize<Json_Credits>(await response.Content.ReadAsStringAsync());
		}
		catch (Exception ex)
		{
			Exception ex2 = ex;
			MessageBox.Show(ex2.Message);
		}
	}
}
