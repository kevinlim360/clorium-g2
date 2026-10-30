using System.Windows;
using CameraControl.Devices.Classes;

namespace CameraControl.Plugins.ToolPlugins;

public class ArduinoButton : BaseFieldClass
{
	private bool _visible;

	private string _title;

	private string _command;

	public bool Visible
	{
		get
		{
			return _visible;
		}
		set
		{
			_visible = value;
			NotifyPropertyChanged("Visible");
			NotifyPropertyChanged("Visibility");
		}
	}

	public Visibility Visibility
	{
		get
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			return (Visibility)((!Visible) ? 2 : 0);
		}
	}

	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			_title = value;
			NotifyPropertyChanged("Title");
		}
	}

	public string Command
	{
		get
		{
			return _command;
		}
		set
		{
			_command = value;
			NotifyPropertyChanged("Command");
		}
	}
}
