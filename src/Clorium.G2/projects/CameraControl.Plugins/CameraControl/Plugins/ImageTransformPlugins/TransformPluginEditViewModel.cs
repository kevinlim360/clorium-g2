using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.Plugins.ImageTransformPlugins;

public class TransformPluginEditViewModel : ViewModelBase
{
	private AutoExportPluginConfig _config;

	private TransformPluginItem _selectedTransformPluginItem;

	public GalaSoft.MvvmLight.Command.RelayCommand<string> AddTransforPluginCommand { get; set; }

	public GalaSoft.MvvmLight.Command.RelayCommand<TransformPluginItem> RemoveTransforPluginCommand { get; set; }

	public RelayCommand PreviewCommand { get; set; }

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

	public UserControl TransformControl
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

	public List<string> ImageTransformPlugins => ServiceProvider.PluginManager.ImageTransformPlugins.Select((IImageTransformPlugin x) => x.Name).ToList();

	public TransformPluginEditViewModel()
	{
		Config = new AutoExportPluginConfig
		{
			Name = "Test"
		};
	}

	public TransformPluginEditViewModel(AutoExportPluginConfig config)
	{
		Config = config;
		AddTransforPluginCommand = new GalaSoft.MvvmLight.Command.RelayCommand<string>(AddTransforPlugin);
		RemoveTransforPluginCommand = new GalaSoft.MvvmLight.Command.RelayCommand<TransformPluginItem>(RemoveTransforPlugin);
		PreviewCommand = new RelayCommand(Preview);
	}

	public void Preview()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			string outfile = Path.GetTempFileName();
			outfile = AutoExportPluginHelper.ExecuteTransformPlugins(ServiceProvider.Settings.SelectedBitmap.FileItem, Config, outfile, preview: true);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Error to preview filter " + ex.Message);
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
