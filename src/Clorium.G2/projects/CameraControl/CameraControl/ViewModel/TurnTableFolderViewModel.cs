using GalaSoft.MvvmLight;

namespace CameraControl.ViewModel;

internal class TurnTableFolderViewModel : ViewModelBase
{
	private string _prefixImage;

	private string _prefixFolder;

	private string _fullName;

	private int _delaytimeindex;

	public string PrefixImage
	{
		get
		{
			return _prefixImage;
		}
		set
		{
			_prefixImage = value;
			RaisePropertyChanged(() => PrefixImage);
		}
	}

	public string PrefixFolder
	{
		get
		{
			return _prefixFolder;
		}
		set
		{
			_prefixFolder = value;
			RaisePropertyChanged(() => PrefixFolder);
		}
	}

	public string FullName
	{
		get
		{
			return _fullName;
		}
		set
		{
			_fullName = value;
			RaisePropertyChanged(() => FullName);
		}
	}

	public int DelayTimeIndex
	{
		get
		{
			return _delaytimeindex;
		}
		set
		{
			_delaytimeindex = value;
			RaisePropertyChanged(() => DelayTimeIndex);
		}
	}
}
