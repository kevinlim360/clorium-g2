using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using CameraControl.Devices.Nikon;

namespace CameraControl.windows;

public class LiveViewManager : IWindow
{
	public delegate void PreviewCapturedEventHandler(ICameraDevice cameraDevice, string file);

	private static object _locker = new object();

	private Dictionary<object, LiveViewWnd> _register;

	private static Dictionary<ICameraDevice, bool> _recordtoRam;

	private static Dictionary<ICameraDevice, bool> _hostMode;

	private static Dictionary<ICameraDevice, CameraPreset> _presets;

	public static Dictionary<ICameraDevice, bool> PreviewRequest;

	public static Dictionary<ICameraDevice, string> Preview;

	public static bool Active;

	public List<ICameraDevice> ActiveDeviceList
	{
		get
		{
			lock (_locker)
			{
				return _register.Keys.OfType<ICameraDevice>().ToList();
			}
		}
	}

	public bool IsVisible { get; private set; }

	public static event PreviewCapturedEventHandler PreviewCaptured;

	public static event PreviewCapturedEventHandler PreviewLoaded;

	public LiveViewManager()
	{
		_register = new Dictionary<object, LiveViewWnd>();
		_recordtoRam = new Dictionary<ICameraDevice, bool>();
		_hostMode = new Dictionary<ICameraDevice, bool>();
		_presets = new Dictionary<ICameraDevice, CameraPreset>();
		PreviewRequest = new Dictionary<ICameraDevice, bool>();
		Preview = new Dictionary<ICameraDevice, string>();
		try
		{
		}
		catch (Exception exception)
		{
			Log.Error("Unable to start XSplit ", exception);
		}
	}

	public void ExecuteCommand(string cmd, object param)
	{
		if (param == null)
		{
			param = ServiceProvider.DeviceManager.SelectedCameraDevice;
		}
		lock (_locker)
		{
			switch (cmd)
			{
			case "LiveViewWnd_Show":
				if (!_register.ContainsKey(param))
				{
					Application.Current.Dispatcher.Invoke(() =>
					{
						LiveViewWnd liveViewWnd = new LiveViewWnd();
						_register.Add(param, liveViewWnd);
						liveViewWnd.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
					});
				}
				if (param is NikonBase nikonBase2 && ServiceProvider.Settings.EasyLiveViewControl)
				{
					CameraPreset preset = new CameraPreset();
					preset.Get(nikonBase2);
					if (!_presets.ContainsKey(nikonBase2))
					{
						_presets.Add(nikonBase2, preset);
					}
					else
					{
						_presets[nikonBase2] = preset;
					}
					if (nikonBase2.ShutterSpeed.Value == "Bulb")
					{
						nikonBase2.ShutterSpeed.Value = nikonBase2.ShutterSpeed.Values[nikonBase2.ShutterSpeed.Values.Count / 2];
					}
					nikonBase2.FocusMode.Value = nikonBase2.FocusMode.Values[0];
					nikonBase2.FNumber.Value = nikonBase2.FNumber.Values[0];
				}
				_register[param].ExecuteCommand(cmd, param);
				Active = true;
				return;
			case "LiveViewWnd_Hide":
			{
				if (_register.ContainsKey(param))
				{
					_register[param].ExecuteCommand(cmd, param);
					_register.Remove(param);
				}
				NikonBase nikonBase = param as NikonBase;
				if (ServiceProvider.Settings.EasyLiveViewControl && nikonBase != null && _presets.ContainsKey(nikonBase))
				{
					nikonBase.ShutterSpeed.Value = _presets[nikonBase].GetValue("ShutterSpeed");
					nikonBase.FNumber.Value = _presets[nikonBase].GetValue("FNumber");
					nikonBase.FocusMode.Value = _presets[nikonBase].GetValue("FocusMode");
				}
				Active = false;
				return;
			}
			case "All_Minimize":
			case "All_Close":
			{
				foreach (KeyValuePair<object, LiveViewWnd> item in _register)
				{
					item.Value.ExecuteCommand(cmd, param);
					Active = false;
				}
				return;
			}
			}
			if (!cmd.StartsWith("LiveView"))
			{
				return;
			}
			if (param is ICameraDevice selectedDevice)
			{
				if (_register.ContainsKey(selectedDevice))
				{
					_register[selectedDevice].ExecuteCommand(cmd, param);
				}
				return;
			}
			foreach (KeyValuePair<object, LiveViewWnd> item2 in _register)
			{
				item2.Value.ExecuteCommand(cmd, param);
			}
		}
	}

	public static void StartLiveView(ICameraDevice device)
	{
		if (device is NikonBase)
		{
		}
		device.StartLiveView();
	}

	public static void StopLiveView(ICameraDevice device)
	{
		if (device != null && device.IsConnected)
		{
			device.StopLiveView();
			if (!(device is NikonBase))
			{
			}
		}
	}

	public static LiveViewData GetLiveViewImage(ICameraDevice device)
	{
		return device.GetLiveViewImage();
	}

	public static void OnPreviewCaptured(ICameraDevice cameradevice, string file)
	{
		PreviewCaptured?.Invoke(cameradevice, file);
	}

	public static void OnPreviewLoaded(ICameraDevice cameradevice, string file)
	{
		PreviewLoaded?.Invoke(cameradevice, file);
	}
}
