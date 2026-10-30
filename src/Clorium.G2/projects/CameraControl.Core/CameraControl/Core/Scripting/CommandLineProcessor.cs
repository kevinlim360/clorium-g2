using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.Core.Scripting;

public class CommandLineProcessor
{
	public ICameraDevice TargetDevice { get; set; }

	public object Pharse(string[] args)
	{
		string cmd = args[0].ToLower().Trim();
		switch (cmd)
		{
		case "capturenoaf":
		case "capture":
			if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
			{
				string file = string.Join(" ", args.Skip(1)).Trim();
				if (file.Contains(":\\") || file.StartsWith("\\\\"))
				{
					ServiceProvider.Settings.DefaultSession.Folder = Path.GetDirectoryName(file);
					ServiceProvider.Settings.DefaultSession.FileNameTemplate = Path.GetFileNameWithoutExtension(file);
				}
				else
				{
					ServiceProvider.Settings.DefaultSession.FileNameTemplate = file;
				}
			}
			if (cmd == "capturenoaf")
			{
				CameraHelper.CaptureNoAf();
			}
			else
			{
				CameraHelper.CaptureWithError(TargetDevice);
			}
			ServiceProvider.DeviceManager.SelectedCameraDevice.WaitForCamera(30000);
			return null;
		case "set":
			Set(args.Skip(1).ToArray());
			Thread.Sleep(200);
			return null;
		case "do":
			return DoCmd(args.Skip(1).ToArray());
		case "get":
			return Get(args.Skip(1).ToArray());
		case "list":
			return List(args.Skip(1).ToArray());
		default:
			throw new Exception("Unknow parameter " + cmd);
		}
	}

	public void SetCamera(string camera)
	{
		if (string.IsNullOrEmpty(camera))
		{
			return;
		}
		foreach (ICameraDevice cameraDevice in ServiceProvider.DeviceManager.ConnectedDevices)
		{
			if ((PhotoUtils.IsNumeric(camera) && cameraDevice.SerialNumber == camera.Trim()) || cameraDevice.DeviceName.Replace(" ", "_") == camera.Replace(" ", "_"))
			{
				TargetDevice = cameraDevice;
				break;
			}
		}
	}

	private string DoCmd(string[] args)
	{
		ICameraDevice device = GetDevice();
		string arg = args[0].ToLower().Trim();
		string text = arg;
		string text2 = text;
		if (!(text2 == "startrecord"))
		{
			if (text2 == "stoprecord")
			{
				return CameraHelper.StopRecordVideo(device);
			}
			ServiceProvider.WindowsManager.ExecuteCommand(args[0]);
			if (!args[0].Contains("_") && !ServiceProvider.Settings.Actions.Select((WindowCommandItem x) => x.Name).Contains(args[0]))
			{
				throw new Exception($"Invalid command {args[0]}");
			}
			return "";
		}
		return CameraHelper.StartRecordVideo(device);
	}

	private object List(string[] args)
	{
		ICameraDevice device = GetDevice();
		string arg = args[0].ToLower().Trim();
		switch (arg)
		{
		case "shutterspeed":
			return device.ShutterSpeed.Values;
		case "iso":
			return device.IsoNumber.Values;
		case "exposurecompensation":
			return device.ExposureCompensation.Values;
		case "aperture":
			return device.FNumber.Values;
		case "focusmode":
			return device.FocusMode.Values;
		case "whitebalance":
			return device.WhiteBalance.Values;
		case "mode":
			return device.Mode.Values;
		case "compressionsetting":
			return device.CompressionSetting.Values;
		case "sessions":
			return ServiceProvider.Settings.PhotoSessions.Select((PhotoSession x) => x.Name).ToArray();
		case "cmds":
		{
			List<string> reslist = ServiceProvider.WindowsManager.WindowCommands.Select((WindowCommandItem x) => x.Name).ToList();
			reslist.Add("StartRecord");
			reslist.Add("StopRecord");
			return reslist;
		}
		case "cameras":
			return (from x in ServiceProvider.DeviceManager.ConnectedDevices
				where x.IsConnected
				select x.SerialNumber).ToArray();
		case "session":
		{
			IList<PropertyInfo> props5 = new List<PropertyInfo>(typeof(PhotoSession).GetProperties());
			return (from prop in props5
				where prop.PropertyType == typeof(string) || prop.PropertyType == typeof(int) || prop.PropertyType == typeof(bool)
				select "session." + prop.Name.ToLower() + "=" + prop.GetValue(ServiceProvider.Settings.DefaultSession, null)).ToList();
		}
		case "property":
		{
			IList<PropertyInfo> props4 = new List<PropertyInfo>(typeof(CameraProperty).GetProperties());
			return (from prop in props4
				where prop.PropertyType == typeof(string) || prop.PropertyType == typeof(int) || prop.PropertyType == typeof(bool)
				select "property." + prop.Name.ToLower() + "=" + prop.GetValue(device.LoadProperties(), null)).ToList();
		}
		case "liveview":
		{
			IList<PropertyInfo> props3 = new List<PropertyInfo>(typeof(LiveviewSettings).GetProperties());
			return (from prop in props3
				where prop.PropertyType == typeof(string) || prop.PropertyType == typeof(int) || prop.PropertyType == typeof(bool)
				select "liveview." + prop.Name.ToLower() + "=" + prop.GetValue(device.LoadProperties().LiveviewSettings, null)).ToList();
		}
		case "camera":
		{
			IList<PropertyInfo> props2 = new List<PropertyInfo>(typeof(ICameraDevice).GetProperties());
			List<string> res = new List<string>();
			foreach (PropertyInfo info2 in props2)
			{
				if (info2.PropertyType.Name.StartsWith("PropertyValue"))
				{
					dynamic valp2 = info2.GetValue(device, null);
					object val = valp2.Value;
					if (val != null)
					{
						res.Add("camera." + info2.Name.ToLower() + "=" + val);
					}
				}
			}
			foreach (PropertyValue<long> property2 in device.AdvancedProperties)
			{
				if (!string.IsNullOrEmpty(property2.Name) && property2.Value != null)
				{
					res.Add("camera." + property2.Name.ToLower().Replace(" ", "_") + "=" + property2.Value);
				}
			}
			res.Add("camera.exposurestatus=" + device.ExposureStatus);
			return res;
		}
		default:
			if (arg.StartsWith("camera."))
			{
				IList<PropertyInfo> props = new List<PropertyInfo>(typeof(ICameraDevice).GetProperties());
				foreach (PropertyInfo info in props)
				{
					if (info.PropertyType.Name.StartsWith("PropertyValue") && arg.Split('.')[1].ToLower().Replace(" ", "_") == info.Name.ToLower())
					{
						dynamic valp = info.GetValue(device, null);
						if (valp != null)
						{
							return valp.Values;
						}
						return new string[1] { "" };
					}
				}
				foreach (PropertyValue<long> property in device.AdvancedProperties)
				{
					if (!string.IsNullOrEmpty(property.Name) && property.Value != null && arg.Split('.')[1].ToLower() == property.Name.ToLower().Replace(" ", "_"))
					{
						return property.Values;
					}
				}
			}
			throw new Exception("Unknow parameter");
		}
	}

	private object Get(string[] args)
	{
		ICameraDevice device = GetDevice();
		string arg = args[0].ToLower().Trim();
		switch (arg)
		{
		case "transfer":
		{
			CameraProperty property2 = ServiceProvider.DeviceManager.SelectedCameraDevice.LoadProperties();
			if (ServiceProvider.DeviceManager.SelectedCameraDevice.GetCapability(CapabilityEnum.CaptureInRam))
			{
				if (ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam)
				{
					return "Save to PC only";
				}
				if (!ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam && property2.NoDownload)
				{
					return "Save to camera only";
				}
				return "Save to PC and camera";
			}
			return property2.NoDownload ? "Save to camera only" : "Save to PC and camera";
		}
		case "shutterspeed":
			return device.ShutterSpeed.Value;
		case "iso":
			return device.IsoNumber.Value;
		case "exposurecompensation":
			return device.ExposureCompensation.Value;
		case "aperture":
			return device.FNumber.Value;
		case "focusmode":
			return device.FocusMode.Value;
		case "whitebalance":
			return device.WhiteBalance.Value;
		case "mode":
			return device.Mode.Value;
		case "compressionsetting":
			return device.CompressionSetting.Value;
		case "lastcaptured":
			if (!ServiceProvider.DeviceManager.LastCapturedImage.ContainsKey(device) || string.IsNullOrEmpty(ServiceProvider.DeviceManager.LastCapturedImage[device]))
			{
				return "?";
			}
			if (ServiceProvider.DeviceManager.LastCapturedImage[device] == "-")
			{
				return "-";
			}
			return Path.GetFileName(ServiceProvider.DeviceManager.LastCapturedImage[device]);
		case "session":
			return ServiceProvider.Settings.DefaultSession.Name;
		case "camera.exposurestatus":
			return device.ExposureStatus;
		case "camera.recordcondition":
			return device.GetProhibitionCondition(OperationEnum.RecordMovie);
		case "camera":
			return device.SerialNumber;
		default:
			if (arg.StartsWith("session."))
			{
				IList<PropertyInfo> props = new List<PropertyInfo>(typeof(PhotoSession).GetProperties());
				foreach (PropertyInfo prop in props)
				{
					if ((prop.PropertyType == typeof(string) || prop.PropertyType == typeof(int) || prop.PropertyType == typeof(bool)) && arg.Split('.')[1].ToLower() == prop.Name.ToLower())
					{
						return prop.GetValue(ServiceProvider.Settings.DefaultSession, null);
					}
				}
			}
			if (arg.StartsWith("property."))
			{
				IList<PropertyInfo> props2 = new List<PropertyInfo>(typeof(CameraProperty).GetProperties());
				foreach (PropertyInfo prop2 in props2)
				{
					if ((prop2.PropertyType == typeof(string) || prop2.PropertyType == typeof(int) || prop2.PropertyType == typeof(bool)) && arg.Split('.')[1].ToLower() == prop2.Name.ToLower())
					{
						return prop2.GetValue(device.LoadProperties(), null);
					}
				}
			}
			if (arg.StartsWith("liveview."))
			{
				IList<PropertyInfo> props3 = new List<PropertyInfo>(typeof(LiveviewSettings).GetProperties());
				foreach (PropertyInfo prop3 in props3)
				{
					if ((prop3.PropertyType == typeof(string) || prop3.PropertyType == typeof(int) || prop3.PropertyType == typeof(bool)) && arg.Split('.')[1].ToLower() == prop3.Name.ToLower())
					{
						return prop3.GetValue(device.LoadProperties().LiveviewSettings, null);
					}
				}
			}
			if (arg.StartsWith("camera."))
			{
				IList<PropertyInfo> props4 = new List<PropertyInfo>(typeof(ICameraDevice).GetProperties());
				foreach (PropertyInfo info in props4)
				{
					if (info.PropertyType.Name.StartsWith("PropertyValue") && arg.Split('.')[1].ToLower().Replace("_", " ") == info.Name.ToLower())
					{
						dynamic valp = info.GetValue(device, null);
						object val = valp.Value;
						if (val != null)
						{
							return val;
						}
					}
				}
				foreach (PropertyValue<long> property in device.AdvancedProperties)
				{
					if (!string.IsNullOrEmpty(property.Name) && property.Value != null && arg.Split('.')[1].ToLower().Replace("_", " ") == property.Name.ToLower())
					{
						return property.Value;
					}
				}
			}
			throw new Exception("Unknow parameter");
		}
	}

	private void Set(string[] args)
	{
		ICameraDevice device = GetDevice();
		args = args.ToArray().Aggregate("", (string current, string s) => current + s + " ").Split('|');
		for (int k = 0; k < args.Length; k++)
		{
			bool notFound = true;
			string arg = args[k].Split(' ')[0];
			string param = args[k].Skip(arg.Length).ToArray().Aggregate("", (string current, char s) => current + s)
				.Trim();
			arg = arg.Trim().ToLower();
			switch (arg)
			{
			case "transfer":
			{
				CameraProperty property = ServiceProvider.DeviceManager.SelectedCameraDevice.LoadProperties();
				switch (param.Replace("_", " "))
				{
				case "Save to PC only":
					if (ServiceProvider.DeviceManager.SelectedCameraDevice.GetCapability(CapabilityEnum.CaptureInRam))
					{
						ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = true;
					}
					break;
				case "Save to camera only":
					property.NoDownload = true;
					ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = false;
					break;
				case "Save to PC and camera":
					property.NoDownload = false;
					ServiceProvider.DeviceManager.SelectedCameraDevice.CaptureInSdRam = false;
					break;
				}
				continue;
			}
			case "shutterspeed":
			{
				string val2 = param;
				if (val2.Equals("bulb"))
				{
					val2 = "Bulb";
				}
				if (!device.ShutterSpeed.Values.Contains(val2) && !val2.Contains("/") && !val2.EndsWith("s") && !val2.Equals("bulb"))
				{
					val2 += "s";
				}
				if (!device.ShutterSpeed.Values.Contains(val2))
				{
					throw new Exception($"Wrong value {val2} for property {arg}");
				}
				device.ShutterSpeed.SetValue(val2);
				continue;
			}
			case "iso":
				if (!device.IsoNumber.Values.Contains(param))
				{
					throw new Exception($"Wrong value {param} for property {arg}");
				}
				device.IsoNumber.SetValue(param);
				continue;
			case "exposurecompensation":
				if (!device.ExposureCompensation.Values.Contains(param))
				{
					throw new Exception($"Wrong value {param} for property {arg}");
				}
				device.ExposureCompensation.SetValue(param);
				continue;
			case "aperture":
			{
				string val = param;
				if (!val.Contains("."))
				{
					val += ".0";
				}
				if (!device.FNumber.Values.Contains(val))
				{
					throw new Exception($"Wrong value {val} for property aperture");
				}
				device.FNumber.SetValue(param);
				continue;
			}
			case "focusmode":
				if (!device.FocusMode.Values.Contains(param))
				{
					throw new Exception($"Wrong value {param} for property {arg}");
				}
				device.FocusMode.SetValue(param);
				continue;
			case "whitebalance":
				if (device?.WhiteBalance != null)
				{
					if (!device.WhiteBalance.Values.Contains(param))
					{
						throw new Exception($"Wrong value {param} for property {arg}");
					}
					device.WhiteBalance.SetValue(param);
				}
				continue;
			case "mode":
				if (device != null)
				{
					PropertyValue<long> mode = device.Mode;
					bool? flag;
					if (mode == null)
					{
						flag = null;
					}
					else
					{
						AsyncObservableCollection<string> values = mode.Values;
						flag = ((values != null) ? new bool?(!values.Contains(param)) : ((bool?)null));
					}
					bool? flag2 = flag;
					if (flag2 == true)
					{
						throw new Exception($"Wrong value {param} for property {arg}");
					}
				}
				device.Mode.SetValue(param);
				continue;
			case "compressionsetting":
				if (!device.CompressionSetting.Values.Contains(param))
				{
					throw new Exception($"Wrong value {param} for property {arg}");
				}
				device.CompressionSetting.SetValue(param);
				continue;
			case "camera":
				foreach (ICameraDevice cameraDevice in ServiceProvider.DeviceManager.ConnectedDevices)
				{
					if ((PhotoUtils.IsNumeric(param) && cameraDevice.SerialNumber == param) || cameraDevice.DeviceName.Replace(" ", "_") == param.Replace(" ", "_"))
					{
						ServiceProvider.DeviceManager.SelectedCameraDevice = cameraDevice;
						break;
					}
				}
				continue;
			case "session":
				device.CompressionSetting.SetValue(param);
				foreach (PhotoSession session in ServiceProvider.Settings.PhotoSessions)
				{
					if (session.Name.ToLower() == param.ToLower())
					{
						ServiceProvider.Settings.DefaultSession = session;
						notFound = false;
						break;
					}
				}
				if (notFound)
				{
					throw new Exception("Unknow session name");
				}
				continue;
			}
			if (arg.StartsWith("session."))
			{
				string val3 = param;
				IList<PropertyInfo> props = new List<PropertyInfo>(typeof(PhotoSession).GetProperties());
				foreach (PropertyInfo prop in props)
				{
					if ((!(prop.PropertyType == typeof(string)) && !(prop.PropertyType == typeof(int)) && !(prop.PropertyType == typeof(bool))) || !(arg.Split('.')[1] == prop.Name.ToLower()))
					{
						continue;
					}
					if (prop.PropertyType == typeof(string))
					{
						prop.SetValue(ServiceProvider.Settings.DefaultSession, val3, null);
						notFound = false;
						break;
					}
					if (prop.PropertyType == typeof(bool))
					{
						val3 = val3.ToLower().Trim();
						if (val3 != "true" && val3 != "false" && val3 != "0" && val3 != "1")
						{
							throw new Exception($"Wrong value {val3} for property {arg}");
						}
						prop.SetValue(ServiceProvider.Settings.DefaultSession, val3 == "true" || val3 == "1", null);
						notFound = false;
						break;
					}
					if (prop.PropertyType == typeof(int))
					{
						int i = 0;
						if (int.TryParse(val3, out i))
						{
							prop.SetValue(ServiceProvider.Settings.DefaultSession, i, null);
							notFound = false;
							break;
						}
						throw new Exception($"Wrong value {val3} for property {arg}");
					}
					break;
				}
			}
			else if (arg.StartsWith("property."))
			{
				string val4 = param;
				IList<PropertyInfo> props2 = new List<PropertyInfo>(typeof(CameraProperty).GetProperties());
				foreach (PropertyInfo prop2 in props2)
				{
					if ((!(prop2.PropertyType == typeof(string)) && !(prop2.PropertyType == typeof(int)) && !(prop2.PropertyType == typeof(bool))) || !(arg.Split('.')[1] == prop2.Name.ToLower()))
					{
						continue;
					}
					if (prop2.PropertyType == typeof(string))
					{
						notFound = false;
						prop2.SetValue(device.LoadProperties(), val4, null);
						break;
					}
					if (prop2.PropertyType == typeof(bool))
					{
						val4 = val4.ToLower().Trim();
						if (val4 != "true" && val4 != "false" && val4 != "0" && val4 != "1")
						{
							throw new Exception($"Wrong value {val4} for property {arg}");
						}
						notFound = false;
						prop2.SetValue(ServiceProvider.Settings.DefaultSession, val4 == "true" || val4 == "1", null);
						break;
					}
					if (prop2.PropertyType == typeof(int))
					{
						int i2 = 0;
						if (int.TryParse(val4, out i2))
						{
							notFound = false;
							prop2.SetValue(device.LoadProperties(), i2, null);
							break;
						}
						throw new Exception($"Wrong value {val4} for property {arg}");
					}
					break;
				}
			}
			else if (arg.StartsWith("liveview."))
			{
				string val5 = param;
				IList<PropertyInfo> props3 = new List<PropertyInfo>(typeof(LiveviewSettings).GetProperties());
				foreach (PropertyInfo prop3 in props3)
				{
					if ((!(prop3.PropertyType == typeof(string)) && !(prop3.PropertyType == typeof(int)) && !(prop3.PropertyType == typeof(bool))) || !(arg.Split('.')[1] == prop3.Name.ToLower()))
					{
						continue;
					}
					if (prop3.PropertyType == typeof(string))
					{
						notFound = false;
						prop3.SetValue(device.LoadProperties().LiveviewSettings, val5, null);
						break;
					}
					if (prop3.PropertyType == typeof(bool))
					{
						val5 = val5.ToLower().Trim();
						if (val5 != "true" && val5 != "false" && val5 != "0" && val5 != "1")
						{
							throw new Exception($"Wrong value {val5} for property {arg}");
						}
						notFound = false;
						prop3.SetValue(ServiceProvider.Settings.DefaultSession, val5 == "true" || val5 == "1", null);
						break;
					}
					if (prop3.PropertyType == typeof(int))
					{
						int i3 = 0;
						if (int.TryParse(val5, out i3))
						{
							notFound = false;
							prop3.SetValue(device.LoadProperties().LiveviewSettings, i3, null);
							break;
						}
						throw new Exception($"Wrong value {val5} for property {arg}");
					}
					break;
				}
			}
			else if (arg.StartsWith("camera."))
			{
				IList<PropertyInfo> props4 = new List<PropertyInfo>(typeof(ICameraDevice).GetProperties());
				foreach (PropertyInfo info in props4)
				{
					if (info.PropertyType.Name.StartsWith("PropertyValue") && arg.Split('.')[1].Replace("_", " ") == info.Name.ToLower())
					{
						dynamic valp = info.GetValue(device, null);
						if ((!valp.Values.Contains(param.Replace("_", " "))))
						{
							throw new Exception($"Wrong value {param} for property {arg}");
						}
						valp.Value = param.Replace("_", " ");
						notFound = false;
						break;
					}
				}
				if (notFound)
				{
					foreach (PropertyValue<long> property2 in device.AdvancedProperties)
					{
						if (!string.IsNullOrEmpty(property2.Name) && property2.Value != null && arg.Split('.')[1].Replace("_", " ") == property2.Name.ToLower())
						{
							if (!property2.Values.Contains(param.Replace("_", " ")))
							{
								throw new Exception($"Wrong value {param} for property {arg}");
							}
							property2.Value = param.Replace("_", " ");
							notFound = false;
							break;
						}
					}
				}
			}
			if (notFound)
			{
				throw new Exception("Unknow parameter");
			}
		}
	}

	private ICameraDevice GetDevice()
	{
		if (ServiceProvider.DeviceManager == null)
		{
			return null;
		}
		if (TargetDevice != null)
		{
			return TargetDevice;
		}
		return ServiceProvider.DeviceManager.SelectedCameraDevice;
	}
}
