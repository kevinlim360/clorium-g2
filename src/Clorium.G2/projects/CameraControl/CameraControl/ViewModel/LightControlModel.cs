using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CameraControl.Classes;
using CameraControl.Core;
using GalaSoft.MvvmLight.Command;

namespace CameraControl.ViewModel;

internal class LightControlModel : INotifyPropertyChanged, IDisposable
{
	private Preset _newPreset = new Preset();

	private Preset _selectedPreset = new Preset();

	private ICollectionView _sortedPresets;

	private ListSortDirection _currentSortDirection = ListSortDirection.Descending;

	private string _defaultlightpresetName;

	private readonly MainWindow _mainWindow;

	private int _floorValue = 0;

	public LightSetting Light1 { get; set; } = new LightSetting("조명1");

	public LightSetting Light2 { get; set; } = new LightSetting("조명2");

	public LightSetting Light3 { get; set; } = new LightSetting("조명3");

	public LightSetting Light4 { get; set; } = new LightSetting("조명4");

	public ObservableCollection<Preset> Presets { get; set; } = new ObservableCollection<Preset>();

	public Preset NewPreset
	{
		get
		{
			return _newPreset;
		}
		set
		{
			_newPreset = value;
			OnPropertyChanged("NewPreset");
		}
	}

	public Preset SelectedPreset
	{
		get
		{
			return _selectedPreset;
		}
		set
		{
			_selectedPreset = value;
			OnPropertyChanged("SelectedPreset");
		}
	}

	public ICollectionView SortedPresets
	{
		get
		{
			return _sortedPresets;
		}
		set
		{
			_sortedPresets = value;
			OnPropertyChanged("SortedPresets");
		}
	}

	public ListSortDirection CurrentSortDirection
	{
		get
		{
			return _currentSortDirection;
		}
		set
		{
			_currentSortDirection = value;
			OnPropertyChanged("CurrentSortDirection");
		}
	}

	public string DefaultLightPresetName
	{
		get
		{
			return _defaultlightpresetName;
		}
		set
		{
			_defaultlightpresetName = value;
			OnPropertyChanged("DefaultLightPresetName");
		}
	}

	public RelayCommand SavePresetCommand { get; set; }

	public RelayCommand ToggleSortCommand { get; set; }

	public ArtNetSender ArtNet { get; private set; }

	public int FloorValue
	{
		get
		{
			return _floorValue;
		}
		set
		{
			if (value < 0)
			{
				value = 0;
			}
			if (value > 100)
			{
				value = 100;
			}
			_floorValue = value;
			ServiceProvider.Settings.LED_Dimming = value.ToString();
			OnPropertyChanged("FloorValue");
			OnPropertyChanged("FloorStatusBrush");
			_mainWindow?.Send_Dimming(_floorValue.ToString());
		}
	}

	public SolidColorBrush FloorStatusBrush
	{
		get
		{
			byte alpha = (byte)((double)_floorValue * 2.35 + 20.0);
			return new SolidColorBrush(Color.FromArgb(alpha, 76, 175, 80));
		}
	}

	public SolidColorBrush ConnectStatus
	{
		get
		{
			if (_mainWindow == null)
			{
				return new SolidColorBrush(Colors.Red);
			}
			return _mainWindow.ConnectStatus;
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public LightControlModel(MainWindow owner)
	{
		SavePresetCommand = new RelayCommand(SavePreset);
		ToggleSortCommand = new RelayCommand(ToggleSort);
		_mainWindow = owner;
		ArtNet = new ArtNetSender();
		Init();
		FloorValue = int.Parse(ServiceProvider.Settings.LED_Dimming);
	}

	public void Dispose()
	{
		if (ArtNet != null)
		{
			ArtNet.Dispose();
		}
	}

	private void Init()
	{
		LoadAllPresets();
		SortPresetList();
		DefaultLightPresetName = ServiceProvider.Settings.LightPresetName;
		if (DefaultLightPresetName == null)
		{
			DefaultLightPresetName = "Default";
		}
		SetLightCondition(DefaultLightPresetName);
	}

	private void LoadAllPresets()
	{
		List<Preset> loaded = PresetManager.LoadPresets();
		Presets.Clear();
		foreach (Preset p in loaded)
		{
			Presets.Add(p);
		}
	}

	private void SortPresetList()
	{
		SortedPresets = CollectionViewSource.GetDefaultView(Presets);
		SortedPresets.SortDescriptions.Clear();
		SortedPresets.SortDescriptions.Add(new SortDescription("PresetName", ListSortDirection.Ascending));
	}

	private void SetLightCondition(string defaultLightPresetName)
	{
		Preset targetPreset = Presets.FirstOrDefault((Preset p) => p.PresetName == defaultLightPresetName);
		if (targetPreset != null)
		{
			targetPreset.PresetName = defaultLightPresetName;
			Light1.Name = targetPreset.Lights[0].Name;
			Light1.Brightness = targetPreset.Lights[0].Brightness;
			Light1.CCT = targetPreset.Lights[0].CCT;
			Light2.Name = targetPreset.Lights[1].Name;
			Light2.Brightness = targetPreset.Lights[1].Brightness;
			Light2.CCT = targetPreset.Lights[1].CCT;
			Light3.Name = targetPreset.Lights[2].Name;
			Light3.Brightness = targetPreset.Lights[2].Brightness;
			Light3.CCT = targetPreset.Lights[2].CCT;
			Light4.Name = targetPreset.Lights[3].Name;
			Light4.Brightness = targetPreset.Lights[3].Brightness;
			Light4.CCT = targetPreset.Lights[3].CCT;
		}
	}

	private void SavePreset()
	{
		if (NewPreset.PresetName == null || NewPreset.PresetName.Trim() == "")
		{
			MessageBox.Show("프리셋 이름을 입력하세요.");
			return;
		}
		if (NewPreset.PresetName == "Default")
		{
			MessageBox.Show("Default 프리셋은 변경할 수 없습니다.");
			return;
		}
		Preset newPreset = new Preset
		{
			PresetName = NewPreset.PresetName,
			CreatedAt = DateTime.Now,
			Lights = new List<LightSetting>
			{
				new LightSetting
				{
					Name = Light1.Name,
					Brightness = Light1.Brightness,
					CCT = Light1.CCT
				},
				new LightSetting
				{
					Name = Light2.Name,
					Brightness = Light2.Brightness,
					CCT = Light2.CCT
				},
				new LightSetting
				{
					Name = Light3.Name,
					Brightness = Light3.Brightness,
					CCT = Light3.CCT
				},
				new LightSetting
				{
					Name = Light4.Name,
					Brightness = Light4.Brightness,
					CCT = Light4.CCT
				}
			}
		};
		Preset existingPreset = Presets.FirstOrDefault((Preset p) => p.PresetName == newPreset.PresetName);
		if (existingPreset != null)
		{
			MessageBoxResult result = MessageBox.Show("이미 같은 이름의 프리셋 \"" + newPreset.PresetName + "\"이 존재합니다.\n덮어쓰시겠습니까?", "확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result == MessageBoxResult.No)
			{
				return;
			}
			Presets.Remove(existingPreset);
		}
		if (SortedPresets != null)
		{
			SortedPresets.SortDescriptions.Clear();
			SortedPresets.SortDescriptions.Add(new SortDescription("CreatedAt", ListSortDirection.Descending));
		}
		Presets.Insert(0, newPreset);
		SelectedPreset = newPreset;
		DefaultLightPresetName = newPreset.PresetName;
		ServiceProvider.Settings.LightPresetName = DefaultLightPresetName;
		PresetManager.SavePresets(Presets);
	}

	private void ToggleSort()
	{
		CurrentSortDirection = ((CurrentSortDirection != ListSortDirection.Descending) ? ListSortDirection.Descending : ListSortDirection.Ascending);
		if (SortedPresets != null)
		{
			SortedPresets.SortDescriptions.Clear();
			SortedPresets.SortDescriptions.Add(new SortDescription("PresetName", CurrentSortDirection));
		}
	}

	public void LightOn(Preset targetPreset)
	{
		ArtNet.Send(targetPreset);
	}

	public void LightOff()
	{
		ArtNet.SendAllOff();
	}
}
