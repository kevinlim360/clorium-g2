using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CameraControl.Devices;

namespace CameraControl.Core.Classes;

public class TriggerClass
{
	private static bool _eventIsBusy;

	private static bool _altPressed;

	private static bool _ctrlPressed;

	private Process _ngrok_process = null;

	public WebServer WebServer { get; set; }

	public KeyboardHook KeyboardHook { get; set; }

	public TriggerClass()
	{
		WebServer = new WebServer();
		KeyboardHook = new KeyboardHook("DCC");
	}

	public void Start()
	{
		try
		{
			KeyboardHook keyboardHook = KeyboardHook;
			keyboardHook.KeyDownEvent = (KeyboardHook.KeyDownEventDelegate)Delegate.Combine(keyboardHook.KeyDownEvent, new KeyboardHook.KeyDownEventDelegate(KeyDown));
			KeyboardHook keyboardHook2 = KeyboardHook;
			keyboardHook2.KeyUpEvent = (KeyboardHook.KeyUpEventDelegate)Delegate.Combine(keyboardHook2.KeyUpEvent, new KeyboardHook.KeyUpEventDelegate(KeyUp));
		}
		catch (Exception)
		{
			Log.Error("Unable to start webserver");
		}
	}

	private void KeyUp(KeyboardHookEventArgs e)
	{
		_altPressed = e.isAltPressed;
		_ctrlPressed = e.isCtrlPressed;
	}

	private void KeyDown(KeyboardHookEventArgs e)
	{
		_altPressed = e.isAltPressed;
		_ctrlPressed = e.isCtrlPressed;
		if (_eventIsBusy)
		{
			Log.Debug("Key Event busy !");
			return;
		}
		Task.Factory.StartNew(() =>
		{
			KeyDownThread(e);
		});
	}

	private void KeyDownThread(KeyboardHookEventArgs e)
	{
		_eventIsBusy = true;
		try
		{
			Key inputKey = KeyInterop.KeyFromVirtualKey((int)e.Key);
			foreach (WindowCommandItem item in ServiceProvider.Settings.Actions)
			{
				if (item.Global && item.Alt == e.isAltPressed && item.Ctrl == e.isCtrlPressed && item.KeyEnum == inputKey)
				{
					ServiceProvider.WindowsManager.ExecuteCommand(item.Name);
				}
			}
			ICameraDevice lastDevice = null;
			foreach (ICameraDevice device in ServiceProvider.DeviceManager.ConnectedDevices)
			{
				lastDevice?.WaitForCamera(1500);
				device.WaitForCamera(1500);
				if (!device.IsBusy)
				{
					CameraProperty property = device.LoadProperties();
					if (property.KeyTrigger.KeyEnum != Key.None && property.KeyTrigger.Alt == e.isAltPressed && property.KeyTrigger.Ctrl == e.isCtrlPressed && property.KeyTrigger.KeyEnum == inputKey)
					{
						CameraHelper.Capture(device);
						lastDevice = device;
					}
					if (property.KeyTriggerNoAF.KeyEnum != Key.None && property.KeyTriggerNoAF.Alt == e.isAltPressed && property.KeyTriggerNoAF.Ctrl == e.isCtrlPressed && property.KeyTriggerNoAF.KeyEnum == inputKey)
					{
						CameraHelper.CaptureNoAf(device);
						lastDevice = device;
					}
				}
			}
		}
		catch (Exception ex)
		{
			StaticHelper.Instance.SystemMessage = ex.Message;
			Log.Error("Key trigger ", ex);
		}
		finally
		{
			_eventIsBusy = false;
		}
	}

	public void Stop()
	{
		WebServer.Stop();
		if (_ngrok_process != null && !_ngrok_process.HasExited)
		{
			_ngrok_process.Kill();
		}
	}

	public static void KeyDown(KeyEventArgs e)
	{
		foreach (WindowCommandItem item in ServiceProvider.Settings.Actions)
		{
			if (item.KeyEnum == e.Key && item.Alt == _altPressed && item.Ctrl == _ctrlPressed)
			{
				ServiceProvider.WindowsManager.ExecuteCommand(item.Name);
				e.Handled = true;
			}
		}
	}
}
