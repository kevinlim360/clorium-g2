using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CameraControl.Devices.Classes;
using CameraControl.Devices.GoPro;
using Newtonsoft.Json.Linq;
using PortableDeviceLib;

namespace CameraControl.Devices.Others;

public class GoProBaseCamera : BaseCameraDevice
{
	private GoProBluetoothDevice _bluetoothDevice;

	protected bool _useOpenGoPro = false;

	protected System.Timers.Timer _timer = new System.Timers.Timer(2000.0);

	protected System.Timers.Timer _keepAlivetimer = new System.Timers.Timer(2500.0);

	private object _locker = new object();

	private List<List<string>> _props = new List<List<string>>();

	public string Address { get; set; }

	public PropertyValue<long> Fps { get; set; }

	public GoProBaseCamera()
	{
		Capabilities.Add(CapabilityEnum.LiveView);
		Capabilities.Add(CapabilityEnum.LiveViewStream);
		Capabilities.Add(CapabilityEnum.RecordMovie);
		LiveViewImageZoomRatio = new PropertyValue<long>();
		LiveViewImageZoomRatio.AddValues("All", 0L);
		LiveViewImageZoomRatio.Value = "All";
		Manufacturer = "GoPro";
	}

	public virtual void BaseInit(string address, JObject json, GoProBluetoothDevice bluetoothDevice)
	{
		_bluetoothDevice = bluetoothDevice;
		Address = "http://" + address;
		DeviceName = json["info"]["model_name"].Value<string>();
		SerialNumber = json["info"]["serial_number"].Value<string>();
		_timer.Elapsed += _timer_Elapsed;
		_timer.AutoReset = true;
		_timer.Start();
		_keepAlivetimer.Elapsed += _keepAlivetimer_Elapsed;
		_keepAlivetimer.AutoReset = true;
		_keepAlivetimer.Start();
		IsConnected = true;
		IsoNumber = new PropertyValue<long>
		{
			Available = false
		};
		FNumber = new PropertyValue<long>
		{
			Available = false
		};
		FocusMode = new PropertyValue<long>
		{
			Available = false
		};
		ExposureMeteringMode = new PropertyValue<long>
		{
			Available = false
		};
	}

	public virtual void Init(string address, JObject json, GoProBluetoothDevice bluetoothDevice)
	{
		BaseInit(address, json, bluetoothDevice);
		foreach (JToken item in (IEnumerable<JToken>)json["display_hints"])
		{
			List<string> list = new List<string>();
			foreach (JToken item2 in (IEnumerable<JToken>)item["settings"])
			{
				list.Add((string?)item2["setting_id"]);
			}
			_props.Add(list);
		}
		Mode = new PropertyValue<long>
		{
			Tag = "144",
			Available = true,
			IsEnabled = true
		};
		Mode.AddValues("Video", 0L);
		Mode.AddValues("Photo", 1L);
		Mode.AddValues("MultiShotMode", 2L);
		Mode.ValueChanged += Mode_ValueChanged;
		Mode.ReloadValues();
		Mode.SetValue(0L);
		CompressionSetting = new PropertyValue<long>
		{
			Tag = "2",
			Available = false,
			IsEnabled = true
		};
		WhiteBalance = new PropertyValue<long>
		{
			Tag = "22",
			Available = true,
			IsEnabled = true
		};
		ShutterSpeed = new PropertyValue<long>
		{
			Tag = "19",
			Available = true,
			IsEnabled = true
		};
		ExposureCompensation = new PropertyValue<long>
		{
			Tag = "26",
			Available = true,
			IsEnabled = true
		};
		LoadJsonValues(WhiteBalance, json);
		LoadJsonValues(ShutterSpeed, json);
		LoadJsonValues(ExposureCompensation, json);
		foreach (List<string> prop in _props)
		{
			foreach (string item3 in prop)
			{
				AdvancedProperties.Add(LoadJsonValues(item3.ToString(), json));
			}
		}
		ReloadSubModes();
	}

	private PropertyValue<long> LoadJsonValues(string tag, JObject json)
	{
		PropertyValue<long> propertyValue = new PropertyValue<long>
		{
			Tag = tag,
			IsEnabled = true,
			Available = true
		};
		propertyValue.ValueChanged += Property_ValueChanged;
		LoadJsonValues(propertyValue, json);
		return propertyValue;
	}

	private void LoadJsonValues(PropertyValue<long> prop, JObject json)
	{
		prop.Clear();
		try
		{
			foreach (JToken item in (IEnumerable<JToken>)json["modes"])
			{
				foreach (JToken item2 in (IEnumerable<JToken>)item["settings"])
				{
					if (!(((int)item2["id"]).ToString() == prop.Tag))
					{
						continue;
					}
					prop.Name = (string?)item2["display_name"] + " - " + prop.Tag;
					foreach (JToken item3 in (IEnumerable<JToken>)item2["options"])
					{
						prop.AddValues((string?)item3["display_name"], (long)item3["value"]);
					}
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("Error loading property values ", exception);
		}
		prop.ReloadValues();
	}

	private void Property_ValueChanged(object sender, string key, long val)
	{
		GetJson($"/gp/gpControl/setting/{((PropertyValue<long>)sender).Tag}/{val}");
	}

	private void Mode_ValueChanged(object sender, string key, long val)
	{
		try
		{
			GetJson("gp/gpControl/command/sub_mode?mode=" + val + "&sub_mode=0");
		}
		catch (Exception)
		{
		}
	}

	private void _keepAlivetimer_Elapsed(object sender, ElapsedEventArgs e)
	{
		try
		{
			_keepAlivetimer.Stop();
			string s = "_GPHD_:1:0:2:0.000000\n";
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
			EndPoint remoteEP = new IPEndPoint(IPAddress.Parse(Address.Replace("http://", "")), 8554);
			socket.SendTo(Encoding.ASCII.GetBytes(s), remoteEP);
			if (_useOpenGoPro)
			{
				GetJson("gopro/camera/keep_alive");
			}
		}
		catch (Exception)
		{
		}
		_keepAlivetimer.Start();
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		Task.Factory.StartNew(GetEvent);
	}

	public override string GetLiveViewStream()
	{
		return string.Format("udp://10.5.5.9:8554", Array.Empty<object>());
	}

	public override void StartLiveView()
	{
		GetJson("gp/gpControl/execute?p1=gpStream&c1=stop");
		JObject json = GetJson("gp/gpControl/execute?p1=gpStream&c1=restart");
	}

	public override void StopLiveView()
	{
		GetJson("gp/gpControl/execute?p1=gpStream&c1=stop");
	}

	public override void CapturePhoto()
	{
		StopLiveView();
		_timer.Stop();
		IsBusy = true;
		if (Mode.Value != "Photo")
		{
			Mode.SetValue("Photo");
			Thread.Sleep(1000);
		}
		if (_bluetoothDevice != null)
		{
			_bluetoothDevice.ExecuteCommand(GoProBluetoothDevice.BLE_CMD_CAPTURE_ON);
		}
		else
		{
			JObject json = GetJson(_useOpenGoPro ? "/gopro/camera/shutter/start" : "gp/gpControl/command/shutter?p=1");
		}
		Thread.Sleep(500);
		while (IsBusy)
		{
			Thread.Sleep(50);
			GetEvent();
			Console.WriteLine("====");
		}
		GoProMediaHandle lastMedia = GetLastMedia();
		PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
		{
			WiaImageItem = null,
			EventArgs = new PortableDeviceEventArgs(),
			CameraDevice = this,
			FileName = lastMedia.Location,
			Handle = lastMedia
		};
		OnPhotoCapture(this, args);
		_timer.Start();
	}

	public override void StartRecordMovie()
	{
		CameraControl.Devices.LegacyCapturePolicy.RequireMovieRecording();

		StopLiveView();
		IsBusy = true;
		if (Mode.Value != "Video")
		{
			Mode.SetValue("Video");
			Thread.Sleep(1000);
		}
		if (_bluetoothDevice != null)
		{
			_bluetoothDevice.ExecuteCommand(GoProBluetoothDevice.BLE_CMD_CAPTURE_ON);
		}
		else
		{
			JObject json = GetJson(_useOpenGoPro ? "/gopro/camera/shutter/start" : "gp/gpControl/command/shutter?p=1");
		}
	}

	public override void StopRecordMovie()
	{
		if (_bluetoothDevice != null)
		{
			_bluetoothDevice.ExecuteCommand(GoProBluetoothDevice.BLE_CMD_CAPTURE_OFF);
		}
		else
		{
			JObject json = GetJson(_useOpenGoPro ? "/gopro/camera/shutter/stop" : "gp/gpControl/command/shutter?p=0");
		}
		Thread.Sleep(500);
		while (IsBusy)
		{
			Thread.Sleep(50);
			GetEvent();
			Console.WriteLine("====");
		}
		GoProMediaHandle lastMedia = GetLastMedia();
		PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
		{
			WiaImageItem = null,
			EventArgs = new PortableDeviceEventArgs(),
			CameraDevice = this,
			FileName = lastMedia.Location,
			Handle = lastMedia
		};
		OnPhotoCapture(this, args);
	}

	public override void TransferFile(object o, string filename)
	{
		_keepAlivetimer.Stop();
		_timer.Stop();
		TransferProgress = 0u;
		if (_useOpenGoPro)
		{
			GetJson("/gopro/media/turbo_transfer?p=1");
		}
		HttpHelper.DownLoadFileByWebRequest($"{Address}/videos/DCIM/{((GoProMediaHandle)o).Location}", filename, this, ((GoProMediaHandle)o).Size);
		if (_useOpenGoPro)
		{
			GetJson("/gopro/media/turbo_transfer?p=0");
		}
		_keepAlivetimer.Start();
		_timer.Start();
	}

	public override void TransferFileThumb(object o, string filename)
	{
		TransferProgress = 0u;
		HttpHelper.DownLoadFileByWebRequest(string.Format(_useOpenGoPro ? "{0}/gopro/media/screennail?path={1}" : "{0}/gp/gpMediaMetadata?p={1}", Address, ((GoProMediaHandle)o).Location), filename, this, ((GoProMediaHandle)o).Size);
	}

	public override bool DeleteObject(DeviceObject deviceObject)
	{
		GetJson(string.Format(_useOpenGoPro ? "/gopro/media/delete/file?path={0}" : "gp/gpControl/command/storage/delete?p={0}", ((GoProMediaHandle)deviceObject.Handle).Location));
		return true;
	}

	public override AsyncObservableCollection<DeviceObject> GetObjects(object storageId, bool loadThumbs)
	{
		_keepAlivetimer.Stop();
		_timer.Stop();
		AsyncObservableCollection<DeviceObject> asyncObservableCollection = new AsyncObservableCollection<DeviceObject>();
		JObject json = GetJson(_useOpenGoPro ? "gopro/media/list" : "gp/gpMediaList");
		foreach (JToken item in (IEnumerable<JToken>)json["media"])
		{
			string text = (string?)item["d"];
			foreach (JToken item2 in (IEnumerable<JToken>)item["fs"])
			{
				string text2 = (string?)item2["n"];
				asyncObservableCollection.Add(new DeviceObject
				{
					FileName = text2,
					Handle = new GoProMediaHandle
					{
						Location = string.Format("{1}/{2}", Address, text, text2),
						Size = (long)item2["s"]
					},
					ThumbData = ((!loadThumbs) ? null : GetData(string.Format(_useOpenGoPro ? "/gopro/media/thumbnail?path={0}/{1}" : "/gp/gpMediaMetadata?p={0}/{1}", text, text2)))
				});
			}
		}
		_keepAlivetimer.Start();
		_timer.Start();
		return asyncObservableCollection;
	}

	private GoProMediaHandle GetLastMedia()
	{
		try
		{
			string arg = "";
			string arg2 = "";
			long size = 0L;
			JObject json = GetJson(_useOpenGoPro ? "gopro/media/list" : "gp/gpMediaList");
			foreach (JToken item in (IEnumerable<JToken>)json["media"])
			{
				arg = (string?)item["d"];
				foreach (JToken item2 in (IEnumerable<JToken>)item["fs"])
				{
					arg2 = (string?)item2["n"];
					size = (long)item2["s"];
				}
			}
			return new GoProMediaHandle
			{
				Location = string.Format("{1}/{2}", Address, arg, arg2),
				Size = size
			};
		}
		catch (Exception exception)
		{
			Log.Error("Error to get last media", exception);
		}
		return new GoProMediaHandle();
	}

	public void GetEvent()
	{
		if (!Monitor.TryEnter(Locker, 5000))
		{
			return;
		}
		try
		{
			JObject json = GetJson(_useOpenGoPro ? "gopro/camera/state" : "gp/gpControl/status");
			if (json.ContainsKey("status"))
			{
				foreach (JProperty item in (IEnumerable<JToken>)json["status"])
				{
					if (item.Name == "70")
					{
						Battery = (int)item.Value;
					}
					if (item.Name == "8")
					{
						IsBusy = (int)item.Value != 0;
					}
					if (item.Name == "43" && !_useOpenGoPro && Mode.NumericValue != (long)item.Value)
					{
						Mode.SetValue((int)item.Value, notifyValuChange: false);
						ReloadSubModes();
					}
					if (item.Name == "96" && _useOpenGoPro && Mode.NumericValue != (long)item.Value)
					{
						Mode.SetValue((int)item.Value, notifyValuChange: false);
						ReloadSubModes();
					}
				}
			}
			if (!json.ContainsKey("settings"))
			{
				return;
			}
			foreach (JProperty item2 in (IEnumerable<JToken>)json["settings"])
			{
				if (Mode != null && Mode.Tag == item2.Name && !_useOpenGoPro && Mode.NumericValue != (long)item2.Value)
				{
					Mode.SetValue((long)item2.Value, notifyValuChange: false);
					ReloadSubModes();
				}
				if (CompressionSetting != null && CompressionSetting.Tag == item2.Name)
				{
					CompressionSetting.SetValue((long)item2.Value, notifyValuChange: false);
				}
				if (ShutterSpeed != null && ShutterSpeed.Tag == item2.Name)
				{
					ShutterSpeed.SetValue((long)item2.Value, notifyValuChange: false);
				}
				if (WhiteBalance != null && WhiteBalance.Tag == item2.Name)
				{
					WhiteBalance.SetValue((long)item2.Value, notifyValuChange: false);
				}
				if (ExposureCompensation != null && WhiteBalance.Tag == item2.Name)
				{
					ExposureCompensation.SetValue((long)item2.Value, notifyValuChange: false);
				}
				foreach (PropertyValue<long> advancedProperty in AdvancedProperties)
				{
					if (!(advancedProperty.Tag == item2.Name))
					{
						continue;
					}
					if (!advancedProperty.NumericValues.Contains((long)item2.Value))
					{
						advancedProperty.Available = false;
						continue;
					}
					advancedProperty.SetValue((long)item2.Value, notifyValuChange: false);
					if (_useOpenGoPro)
					{
						advancedProperty.Available = true;
					}
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("GoPro get event error ", exception);
		}
		finally
		{
			Monitor.Exit(Locker);
		}
	}

	public virtual void ReloadSubModes()
	{
		if (Mode.NumericValue >= _props.Count)
		{
			return;
		}
		foreach (PropertyValue<long> advancedProperty in AdvancedProperties)
		{
			if (_props[(int)Mode.NumericValue].Contains(advancedProperty.Tag) || _props[3].Contains(advancedProperty.Tag))
			{
				advancedProperty.Available = true;
			}
			else
			{
				advancedProperty.Available = false;
			}
		}
	}

	public JObject GetJson(string endpoint)
	{
		if (Monitor.TryEnter(Locker, 5000))
		{
			try
			{
				Console.WriteLine(endpoint);
				string json = "";
				if (!endpoint.StartsWith("/"))
				{
					endpoint = "/" + endpoint;
				}
				using (WebClient webClient = new WebClient())
				{
					json = webClient.DownloadString(Address + endpoint);
				}
				return JObject.Parse(json);
			}
			catch (Exception)
			{
				return new JObject();
			}
			finally
			{
				Monitor.Exit(Locker);
			}
		}
		return new JObject();
	}

	public byte[] GetData(string endpoint)
	{
		if (Monitor.TryEnter(Locker, 5000))
		{
			try
			{
				if (!endpoint.StartsWith("/"))
				{
					endpoint = "/" + endpoint;
				}
				using WebClient webClient = new WebClient();
				return webClient.DownloadData(Address + endpoint);
			}
			catch (Exception)
			{
				return null;
			}
			finally
			{
				Monitor.Exit(Locker);
			}
		}
		return null;
	}
}
