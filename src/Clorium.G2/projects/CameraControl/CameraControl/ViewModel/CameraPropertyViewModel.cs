using System.Collections.ObjectModel;
using CameraControl.Core;
using CameraControl.Core.Classes;
using GalaSoft.MvvmLight;

namespace CameraControl.ViewModel;

public class CameraPropertyViewModel : ViewModelBase
{
	private CameraProperty _cameraProperty;

	private ObservableCollection<string> _photoSessionNames;

	private ObservableCollection<string> _cameraPresets;

	public CameraProperty CameraProperty
	{
		get
		{
			return _cameraProperty;
		}
		set
		{
			_cameraProperty = value;
			RaisePropertyChanged(() => CameraProperty);
		}
	}

	public ObservableCollection<string> PhotoSessionNames
	{
		get
		{
			return _photoSessionNames;
		}
		set
		{
			_photoSessionNames = value;
			RaisePropertyChanged(() => PhotoSessionNames);
		}
	}

	public ObservableCollection<string> CameraPresets
	{
		get
		{
			return _cameraPresets;
		}
		set
		{
			_cameraPresets = value;
			RaisePropertyChanged(() => CameraPresets);
		}
	}

	public CameraPropertyViewModel()
	{
		if (!IsInDesignMode)
		{
			Init();
		}
	}

	public void Init()
	{
		PhotoSessionNames = new ObservableCollection<string> { "(None)" };
		foreach (PhotoSession photoSession in ServiceProvider.Settings.PhotoSessions)
		{
			PhotoSessionNames.Add(photoSession.Name);
		}
		CameraPresets = new ObservableCollection<string> { "(None)" };
		foreach (CameraPreset cameraPresets in ServiceProvider.Settings.CameraPresets)
		{
			CameraPresets.Add(cameraPresets.Name);
		}
	}
}
