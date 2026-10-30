using System;
using System.IO;
using System.Threading.Tasks;
using CameraControl.Core;
using CameraControl.Core.Scripting;
using CameraControl.Core.TclScripting;
using CameraControl.Devices;
using Quartz;

namespace CameraControl.ViewModel;

public class SimpleJob : IJob
{
	public Task Execute(IJobExecutionContext context)
	{
		if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsBusy)
		{
			try
			{
				if (ServiceProvider.Settings.DefaultSession.TimeLapseSettings.Capture)
				{
					if (ServiceProvider.Settings.DefaultSession.TimeLapseSettings.Bracketing)
					{
						BracketingViewModel _bracketingViewModel = new BracketingViewModel();
						Task.Factory.StartNew(_bracketingViewModel.Start);
						StaticHelper.Instance.SystemMessage = _bracketingViewModel.Error;
					}
					else
					{
						ServiceProvider.WindowsManager.ExecuteCommand("Capture");
					}
				}
				if (ServiceProvider.Settings.DefaultSession.TimeLapseSettings.CaptureAll)
				{
					ServiceProvider.WindowsManager.ExecuteCommand("CaptureAll");
				}
				if (ServiceProvider.Settings.DefaultSession.TimeLapseSettings.CaptureScript)
				{
					if (Path.GetExtension(ServiceProvider.Settings.DefaultSession.TimeLapseSettings.ScriptFile.ToLower()) == ".tcl")
					{
						try
						{
							TclScriptManager manager = new TclScriptManager();
							manager.Execute(File.ReadAllText(ServiceProvider.Settings.DefaultSession.TimeLapseSettings.ScriptFile));
						}
						catch (Exception ex)
						{
							Log.Error("Script error", ex);
							StaticHelper.Instance.SystemMessage = "Script error :" + ex.Message;
						}
					}
					else
					{
						ScriptObject script = ServiceProvider.ScriptManager.Load(ServiceProvider.Settings.DefaultSession.TimeLapseSettings.ScriptFile);
						script.CameraDevice = ServiceProvider.DeviceManager.SelectedCameraDevice;
						ServiceProvider.ScriptManager.Execute(script);
					}
				}
			}
			catch (Exception exception)
			{
				Log.Error("Timelapse error ", exception);
				StaticHelper.Instance.SystemMessage = "Capture error";
			}
		}
		Console.WriteLine("Hello, JOb executed");
		return Task.FromResult(0);
	}
}
