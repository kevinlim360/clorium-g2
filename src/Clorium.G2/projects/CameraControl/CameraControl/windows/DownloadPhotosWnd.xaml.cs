using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Markup;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Translation;
using CameraControl.Core.Wpf;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.windows;

public partial class DownloadPhotosWnd : Window, INotifyPropertyChanged, IWindow, IComponentConnector
{
	private bool delete;

	private bool format;

	private bool saveseries;

	private ProgressWindow dlg = new ProgressWindow();

	private Dictionary<ICameraDevice, int> _timeDif = new Dictionary<ICameraDevice, int>();

	private Dictionary<ICameraDevice, AsyncObservableCollection<FileItem>> _itembycamera = new Dictionary<ICameraDevice, AsyncObservableCollection<FileItem>>();

	private List<DateTime> _timeTable = new List<DateTime>();

	private ICameraDevice _cameraDevice;

	private AsyncObservableCollection<FileItem> _items;

	private CollectionView _myView;

	public ObservableCollection<DownloadableItems> Groups { get; set; }

	public CollectionView MyView
	{
		get
		{
			return _myView;
		}
		set
		{
			_myView = value;
			NotifyPropertyChanged("MyView");
		}
	}

	public ICameraDevice CameraDevice
	{
		get
		{
			return _cameraDevice;
		}
		set
		{
			_cameraDevice = value;
			NotifyPropertyChanged("CameraDevice");
		}
	}

	public AsyncObservableCollection<FileItem> Items
	{
		get
		{
			return _items;
		}
		set
		{
			_items = value;
			NotifyPropertyChanged("Items");
		}
	}

	public RelayCommand<string> SelectAllCommand { get; set; }

	public RelayCommand<string> SelectNoneCommand { get; set; }

	public RelayCommand<string> SelectInvertCommand { get; set; }

	public virtual event PropertyChangedEventHandler PropertyChanged;

	public DownloadPhotosWnd()
	{
		Groups = new ObservableCollection<DownloadableItems>();
		Items = new AsyncObservableCollection<FileItem>();
		SelectAllCommand = new RelayCommand<string>(SelectAll);
		SelectNoneCommand = new RelayCommand<string>(SelectNone);
		SelectInvertCommand = new RelayCommand<string>(SelectInvert);
		InitializeComponent();
	}

	private void SelectAll(string serial)
	{
		foreach (FileItem fileItem in Items)
		{
			if (fileItem.Device.SerialNumber == serial)
			{
				fileItem.IsChecked = true;
			}
		}
	}

	private void SelectNone(string serial)
	{
		foreach (FileItem fileItem in Items)
		{
			if (fileItem.Device.SerialNumber == serial)
			{
				fileItem.IsChecked = false;
			}
		}
	}

	private void SelectInvert(string serial)
	{
		foreach (FileItem fileItem in Items)
		{
			if (fileItem.Device.SerialNumber == serial)
			{
				fileItem.IsChecked = !fileItem.IsChecked;
			}
		}
	}

	private void btn_help_Click(object sender, RoutedEventArgs e)
	{
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
		case "DownloadPhotosWnd_Show":
			Dispatcher.BeginInvoke((Action)(() =>
			{
				if (!dlg.IsVisible)
				{
					Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
					CameraDevice = param as ICameraDevice;
					Title = TranslationStrings.DownloadWindowTitle + "-" + ServiceProvider.Settings.CameraProperties.Get(CameraDevice).DeviceName;
					if (param != null)
					{
						Show();
						Activate();
						Focus();
						dlg.Show();
						Items.Clear();
						FreeResources();
						dlg.Owner = this;
						Thread thread = new Thread(PopulateImageList);
						thread.Start();
					}
				}
			}));
			break;
		case "DownloadPhotosWnd_Hide":
			Dispatcher.Invoke(() =>
			{
				Hide();
				FreeResources();
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

	private void FreeResources()
	{
		foreach (FileItem fileItem in Items)
		{
			if (fileItem.ItemType != FileItemType.Missing)
			{
				fileItem.Device.ReleaseResurce(fileItem.DeviceObject.Handle);
			}
			fileItem.Dispose();
		}
		MyView = null;
		Items.Clear();
		Items = null;
		Items = new AsyncObservableCollection<FileItem>();
	}

	private void MetroWindow_Closing(object sender, CancelEventArgs e)
	{
		if (IsVisible)
		{
			e.Cancel = true;
			ServiceProvider.WindowsManager.ExecuteCommand("DownloadPhotosWnd_Hide");
		}
	}

	private void PopulateImageList()
	{
		_timeDif.Clear();
		_itembycamera.Clear();
		Items.Clear();
		_timeTable.Clear();
		if (ServiceProvider.DeviceManager.ConnectedDevices.Count == 0)
		{
			return;
		}
		int counter = 0;
		dlg.MaxValue = ServiceProvider.DeviceManager.ConnectedDevices.Count;
		dlg.Label = "......";
		foreach (ICameraDevice cameraDevice in ServiceProvider.DeviceManager.ConnectedDevices)
		{
			counter++;
			dlg.Progress = counter;
			CameraProperty property = cameraDevice.LoadProperties();
			cameraDevice.DisplayName = property.DeviceName;
			dlg.Label = cameraDevice.DisplayName;
			dlg.Label2 = "";
			try
			{
				AsyncObservableCollection<DeviceObject> images = cameraDevice.GetObjects(null, ServiceProvider.Settings.LoadThumbsDownload);
				if (images.Count > 0)
				{
					int index = 0;
					foreach (DeviceObject deviceObject in images)
					{
						index++;
						if (!_itembycamera.ContainsKey(cameraDevice))
						{
							_itembycamera.Add(cameraDevice, new AsyncObservableCollection<FileItem>());
						}
						FileItem fileitem = new FileItem(deviceObject, cameraDevice);
						fileitem.Series = index;
						dlg.Label2 = fileitem.FileName;
						PhotoSession session = ((PhotoSession)cameraDevice.AttachedPhotoSession) ?? ServiceProvider.Settings.DefaultSession;
						fileitem.IsChecked = session.GetFile(deviceObject.FileName, cameraDevice.SerialNumber) == null;
						_itembycamera[cameraDevice].Add(fileitem);
					}
				}
			}
			catch (Exception exception)
			{
				StaticHelper.Instance.SystemMessage = TranslationStrings.LabelErrorLoadingFileList;
				Log.Error("Error loading file list", exception);
			}
			Thread.Sleep(500);
		}
		Dispatcher.Invoke(() =>
		{
			foreach (FileItem current in _itembycamera.Values.SelectMany((AsyncObservableCollection<FileItem> lists) => lists))
			{
				Items.Add(current);
			}
			if (Items.Select((FileItem x) => x.Device).Distinct().Count() > 1)
			{
				lst_items.Visibility = Visibility.Visible;
				lst_items_simple.Visibility = Visibility.Collapsed;
			}
			else
			{
				lst_items.Visibility = Visibility.Collapsed;
				lst_items_simple.Visibility = Visibility.Visible;
				lst_items_simple.ItemsSource = Items;
			}
		});
		dlg.Hide();
	}

	private void btn_download_Click(object sender, RoutedEventArgs e)
	{
		if ((chk_delete.IsChecked != true && chk_format.IsChecked != true) || System.Windows.Forms.MessageBox.Show(TranslationStrings.LabelAskForDelete, "", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
		{
			dlg.Show();
			delete = chk_delete.IsChecked == true;
			format = chk_format.IsChecked == true;
			saveseries = chk_series.IsChecked == true;
			Thread thread = new Thread(TransferFiles);
			thread.Start();
		}
	}

	private void TransferFiles()
	{
		try
		{
			DateTime starttime = DateTime.Now;
			long totalbytes = 0L;
			bool somethingwrong = false;
			AsyncObservableCollection<FileItem> itemstoExport = new AsyncObservableCollection<FileItem>(Items.Where((FileItem x) => x.IsChecked));
			dlg.MaxValue = itemstoExport.Count;
			dlg.Progress = 0.0;
			int i = 0;
			foreach (FileItem fileItem in itemstoExport)
			{
				if (fileItem.ItemType == FileItemType.Missing || !fileItem.IsChecked)
				{
					continue;
				}
				dlg.Label = fileItem.FileName;
				dlg.ImageSource = fileItem.Thumbnail;
				dlg.Camera = fileItem.Device;
				PhotoSession session = ((PhotoSession)fileItem.Device.AttachedPhotoSession) ?? ServiceProvider.Settings.DefaultSession;
				if (saveseries)
				{
					session.Series = fileItem.Series;
				}
				string fileName = "";
				if (!session.UseOriginalFilename)
				{
					fileName = session.GetNextFileName(Path.GetExtension(fileItem.FileName), fileItem.Device, "");
				}
				else
				{
					fileName = Path.Combine(session.Folder, fileItem.FileName);
					if (File.Exists(fileName))
					{
						fileName = StaticHelper.GetUniqueFilename(Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName) + "_", 0, Path.GetExtension(fileName));
					}
				}
				string dir = Path.GetDirectoryName(fileName);
				if (dir != null && !Directory.Exists(dir))
				{
					Directory.CreateDirectory(dir);
				}
				try
				{
					fileItem.Device.TransferFile(fileItem.DeviceObject.Handle, fileName);
					fileItem.Device.ReleaseResurce(fileItem.DeviceObject.Handle);
					fileItem.Device.TransferProgress = 0u;
					fileItem.Device.IsBusy = false;
				}
				catch (Exception exception)
				{
					somethingwrong = true;
					Log.Error("Transfer error", exception);
				}
				if (File.Exists(fileName))
				{
					if (delete)
					{
						fileItem.Device.DeleteObject(fileItem.DeviceObject);
					}
				}
				else
				{
					somethingwrong = true;
				}
				if (!File.Exists(fileName))
				{
					System.Windows.Forms.MessageBox.Show("Unable download file. Aborting!");
					break;
				}
				totalbytes += new System.IO.FileInfo(fileName).Length;
				FileItem item1 = fileItem;
				int serie = fileItem.Series;
				Dispatcher.Invoke(() =>
				{
					FileItem fileItem2 = session.AddFile(fileName);
					if (saveseries)
					{
						fileItem2.Series = serie;
					}
					fileItem2.CameraSerial = item1.Device.SerialNumber;
					fileItem2.OriginalName = item1.FileName;
				});
				i++;
				dlg.Progress = i;
			}
			Log.Debug("File transfer done");
			if (format)
			{
				dlg.MaxValue = ServiceProvider.DeviceManager.ConnectedDevices.Count;
				dlg.Progress = 0.0;
				int ii = 0;
				if (!somethingwrong)
				{
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
						if (!(connectedDevice.DeviceName == ServiceProvider.Settings.WebCam_DeviceName))
						{
							try
							{
								dlg.Label = connectedDevice.DisplayName;
								ii++;
								dlg.Progress = ii;
								Log.Debug("Start format");
								Log.Debug(connectedDevice.PortName);
								connectedDevice.FormatStorage(null);
								Thread.Sleep(200);
								Log.Debug("Format done");
							}
							catch (Exception exception2)
							{
								Log.Error("Unable to format device ", exception2);
							}
						}
					}
				}
				else
				{
					Log.Debug("File transfer failed, format aborted!");
					StaticHelper.Instance.SystemMessage = "File transfer failed, format aborted!";
				}
			}
			dlg.Hide();
			double transfersec = (DateTime.Now - starttime).TotalSeconds;
			Log.Debug(string.Format("[BENCHMARK]Total byte transferred ;{0} Total seconds :{1} Speed : {2} Mbyte/sec ", totalbytes, transfersec, ((double)totalbytes / transfersec / 1024.0 / 1024.0).ToString("0000.00")));
		}
		catch (Exception exception3)
		{
			Log.Error("Crash on download window ", exception3);
		}
		ServiceProvider.Settings.Save();
		ServiceProvider.WindowsManager.ExecuteCommand("DownloadPhotosWnd_Hide");
	}

	private void btn_all_Click(object sender, RoutedEventArgs e)
	{
		foreach (FileItem fileItem in Items)
		{
			fileItem.IsChecked = true;
		}
	}

	private void btn_none_Click(object sender, RoutedEventArgs e)
	{
		foreach (FileItem fileItem in Items)
		{
			fileItem.IsChecked = false;
		}
	}

	private void btn_invert_Click(object sender, RoutedEventArgs e)
	{
		foreach (FileItem fileItem in Items)
		{
			fileItem.IsChecked = !fileItem.IsChecked;
		}
	}

	private void btn_select_Click(object sender, RoutedEventArgs e)
	{
		SetIndex((int)txt_indx.Value.Value);
	}

	private void SetIndex(int selectedidx)
	{
		foreach (ICameraDevice connectedDevice in ServiceProvider.DeviceManager.ConnectedDevices)
		{
			int index = 1;
			foreach (FileItem fileItem in Items)
			{
				if (fileItem.Device == connectedDevice)
				{
					if (index == selectedidx)
					{
						fileItem.IsChecked = !fileItem.IsChecked;
					}
					index++;
				}
			}
		}
	}

	private void button1_Click(object sender, RoutedEventArgs e)
	{
		dlg.Show();
		Thread thread = new Thread(PopulateImageList);
		thread.Start();
	}
}
