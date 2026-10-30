using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Scripting;
using CameraControl.Devices;
using CameraControl.windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.ViewModel;

public class AutoExportPluginEditViewModel : ViewModelBase
{
	private PreviewWnd _wnd;

	private AutoExportPluginConfig _config;

	private TransformPluginItem _selectedTransformPluginItem;

	private bool _fullSize;

	public GalaSoft.MvvmLight.Command.RelayCommand<string> AddTransforPluginCommand { get; set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<TransformPluginItem> RemoveTransforPluginCommand { get; set; }

	public RelayCommand PreviewCommand { get; set; }

	public RelayCommand ApplyCommand { get; set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<PluginCondition> RemoveConditionCommand { get; set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<PluginCondition> GetValueCommand { get; set; }

	public RelayCommand AddConditionCommand { get; set; }

	public RelayCommand CheckConditionCommand { get; set; }

	public AutoExportPluginConfig Config
	{
		get
		{
			return _config;
		}
		set
		{
			_config = value;
			RaisePropertyChanged(() => Config);
		}
	}

	public string Name
	{
		get
		{
			return Config.Name;
		}
		set
		{
			Config.Name = value;
			RaisePropertyChanged(() => Name);
		}
	}

	public bool IsEnabled
	{
		get
		{
			return Config.IsEnabled;
		}
		set
		{
			Config.IsEnabled = value;
			RaisePropertyChanged(() => IsEnabled);
		}
	}

	public bool RunAfterTransfer
	{
		get
		{
			return Config.RunAfterTransfer;
		}
		set
		{
			Config.RunAfterTransfer = value;
			RaisePropertyChanged(() => RunAfterTransfer);
		}
	}

	public bool FullSize
	{
		get
		{
			return _fullSize;
		}
		set
		{
			_fullSize = value;
			RaisePropertyChanged(() => FullSize);
		}
	}

	public System.Windows.Controls.UserControl ConfigControl => ServiceProvider.PluginManager.GetAutoExportPlugin(Config.Type)?.GetConfig(_config);

	public ObservableCollection<TransformPluginItem> TransformPluginItems
	{
		get
		{
			ObservableCollection<TransformPluginItem> list = new ObservableCollection<TransformPluginItem>();
			foreach (ValuePairEnumerator enumerator2 in _config.ConfigDataCollection)
			{
				list.Add(new TransformPluginItem(enumerator2));
			}
			return list;
		}
	}

	public TransformPluginItem SelectedTransformPluginItem
	{
		get
		{
			return _selectedTransformPluginItem;
		}
		set
		{
			_selectedTransformPluginItem = value;
			RaisePropertyChanged(() => SelectedTransformPluginItem);
			RaisePropertyChanged(() => TransformControl);
		}
	}

	public System.Windows.Controls.UserControl TransformControl
	{
		get
		{
			if (SelectedTransformPluginItem == null)
			{
				return null;
			}
			return ServiceProvider.PluginManager.GetImageTransformPlugin(SelectedTransformPluginItem.Name)?.GetConfig(SelectedTransformPluginItem.Config);
		}
	}

	public List<string> ImageTransformPlugins
	{
		get
		{
			if (ServiceProvider.PluginManager == null)
			{
				return null;
			}
			return ServiceProvider.PluginManager.ImageTransformPlugins.Select((IImageTransformPlugin x) => x.Name).ToList();
		}
	}

	public AutoExportPluginEditViewModel()
	{
		Config = new AutoExportPluginConfig
		{
			Name = "Test"
		};
	}

	public AutoExportPluginEditViewModel(AutoExportPluginConfig config)
	{
		Config = config;
		AddTransforPluginCommand = new GalaSoft.MvvmLight.Command.RelayCommand<string>(AddTransforPlugin);
		RemoveTransforPluginCommand = new GalaSoft.MvvmLight.Command.RelayCommand<TransformPluginItem>(RemoveTransforPlugin);
		PreviewCommand = new RelayCommand(Preview);
		ApplyCommand = new RelayCommand(Apply);
		RemoveConditionCommand = new GalaSoft.MvvmLight.Command.RelayCommand<PluginCondition>(RemoveCondition);
		AddConditionCommand = new RelayCommand(AddCondition);
		CheckConditionCommand = new RelayCommand(CheckCondition);
		GetValueCommand = new GalaSoft.MvvmLight.Command.RelayCommand<PluginCondition>(GetValue);
	}

	private void GetValue(PluginCondition obj)
	{
		try
		{
			CommandLineProcessor processor = new CommandLineProcessor();
			object resp = processor.Pharse(new string[2] { "get", obj.Variable });
			obj.Value = resp.ToString();
		}
		catch (Exception exception)
		{
			Log.Error("GetValue", exception);
		}
	}

	private void CheckCondition()
	{
		try
		{
			System.Windows.Forms.MessageBox.Show(Config.Evaluate(ServiceProvider.DeviceManager.SelectedCameraDevice) ? "Conditions is evaluated : True" : "Conditions is evaluated : False");
		}
		catch (Exception ex)
		{
			System.Windows.Forms.MessageBox.Show("Evaluation error " + ex.Message);
		}
	}

	private void AddCondition()
	{
		Config.Conditions.Add(new PluginCondition());
	}

	private void RemoveCondition(PluginCondition obj)
	{
		Config.Conditions.Remove(obj);
	}

	private void Apply()
	{
		PhotoSession.ApplyPlugin(Config);
		if (Config.IsError)
		{
			System.Windows.Forms.MessageBox.Show(Config.Error);
		}
	}

	public void Preview()
	{
		try
		{
			string outfile = Path.GetTempFileName();
			outfile = AutoExportPluginHelper.ExecuteTransformPlugins(ServiceProvider.Settings.SelectedBitmap.FileItem, Config, outfile, !FullSize);
			if (_wnd == null || !_wnd.IsVisible)
			{
				_wnd = new PreviewWnd();
				_wnd.Owner = (Window)ServiceProvider.PluginManager.SelectedWindow;
			}
			_wnd.Show();
			_wnd.Image.BeginInit();
			_wnd.Image.Source = new BitmapImage(new Uri(outfile));
			_wnd.Image.EndInit();
			_wnd.ImageO.BeginInit();
			_wnd.ImageO.Source = new BitmapImage(new Uri(ServiceProvider.Settings.SelectedBitmap.FileItem.LargeThumb));
			_wnd.ImageO.EndInit();
		}
		catch (Exception ex)
		{
			System.Windows.Forms.MessageBox.Show("Error to preview filter \n" + ex.Message);
		}
	}

	public void AddTransforPlugin(string plugin)
	{
		ValuePairEnumerator c = new ValuePairEnumerator();
		c["TransformPlugin"] = plugin;
		Config.ConfigDataCollection.Add(c);
		RaisePropertyChanged(() => TransformPluginItems);
		foreach (TransformPluginItem item in TransformPluginItems)
		{
			if (item.Config == c)
			{
				SelectedTransformPluginItem = item;
			}
		}
	}

	public void RemoveTransforPlugin(TransformPluginItem item)
	{
		Config.ConfigDataCollection.Remove(item.Config);
		RaisePropertyChanged(() => TransformPluginItems);
	}
}
