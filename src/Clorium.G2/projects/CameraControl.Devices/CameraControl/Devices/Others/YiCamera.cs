using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using CameraControl.Devices.Classes;
using CameraControl.Devices.TransferProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortableDeviceLib;

namespace CameraControl.Devices.Others;

public class YiCamera : BaseCameraDevice
{
	private static AutoResetEvent _resetEvent = new AutoResetEvent(initialState: false);

	private static AutoResetEvent _listingEvent = new AutoResetEvent(initialState: false);

	private string _lastData = "";

	private bool _timelapse_running = false;

	public YiCameraProtocol Protocol { get; set; }

	public Dictionary<string, string> CurrentValues { get; set; }

	public override bool Init(DeviceDescriptor deviceDescriptor)
	{
		CurrentValues = new Dictionary<string, string>();
		Capabilities.Add(CapabilityEnum.LiveView);
		Capabilities.Add(CapabilityEnum.LiveViewStream);
		Capabilities.Add(CapabilityEnum.RecordMovie);
		Protocol = deviceDescriptor.StillImageDevice as YiCameraProtocol;
		Protocol.DataReceiverd += Protocol_DataReceiverd;
		DeviceName = Protocol.Model;
		Manufacturer = Protocol.Manufacturer;
		IsConnected = true;
		CompressionSetting = new PropertyValue<long>
		{
			Tag = "photo_quality"
		};
		Mode = new PropertyValue<long>
		{
			Tag = "capture_mode"
		};
		Mode.AddValues("Single", 0L);
		Mode.AddValues("Burst", 1L);
		Mode.AddValues("Delayed", 2L);
		Mode.ReloadValues();
		ExposureMeteringMode = new PropertyValue<long>
		{
			Tag = "meter_mode"
		};
		LiveViewImageZoomRatio = new PropertyValue<long>();
		LiveViewImageZoomRatio.AddValues("All", 0L);
		LiveViewImageZoomRatio.Value = "All";
		SendCommand(3);
		Thread.Sleep(500);
		SerialNumber = GetValue("serial_number");
		SetProperty(Mode.Tag, GetValue(Mode.Tag));
		Thread thread = new Thread(LoadProperties)
		{
			Priority = ThreadPriority.Lowest
		};
		thread.Start();
		return true;
	}

	private void LoadProperties()
	{
		try
		{
			IsoNumber = new PropertyValue<long>
			{
				Available = false
			};
			FNumber = new PropertyValue<long>
			{
				Available = false
			};
			ExposureCompensation = new PropertyValue<long>
			{
				Available = false
			};
			FocusMode = new PropertyValue<long>
			{
				Available = false
			};
			ShutterSpeed = new PropertyValue<long>
			{
				Available = false
			};
			WhiteBalance = new PropertyValue<long>
			{
				Available = false
			};
			Properties.Add(AddNames("photo_size", "Photo size"));
			Properties.Add(AddNames("precise_selftime", "Capture delay"));
			Properties.Add(AddNames("burst_capture_number", "Burst capture number"));
			Properties.Add(AddNames("auto_low_light", "Auto low light"));
			AdvancedProperties.Add(AddNames("video_resolution", "Video resolution"));
			AdvancedProperties.Add(AddNames("led_mode", "Led mode"));
			AdvancedProperties.Add(AddNames("auto_power_off", "Auto power off"));
			AdvancedProperties.Add(AddNames("loop_record", "Loop record"));
			AdvancedProperties.Add(AddNames("warp_enable", "Lens correction"));
			AdvancedProperties.Add(AddNames("buzzer_ring", "Find device"));
			CompressionSetting.ValueChanged += (object sender, string key, long val) =>
			{
				Protocol.SendValue(CompressionSetting.Tag, key);
			};
			SendCommand(9, CompressionSetting.Tag);
			Mode.ValueChanged += Mode_ValueChanged;
			SendCommand(9, Mode.Tag);
			ExposureMeteringMode.ValueChanged += (object sender, string key, long val) =>
			{
				Protocol.SendValue(ExposureMeteringMode.Tag, key);
			};
			SendCommand(9, ExposureMeteringMode.Tag);
			foreach (PropertyValue<long> property in Properties)
			{
				SendCommand(9, property.Tag);
			}
			foreach (PropertyValue<long> advancedProperty in AdvancedProperties)
			{
				SendCommand(9, advancedProperty.Tag);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Unable to load data", exception);
		}
	}

	private void Mode_ValueChanged(object sender, string key, long val)
	{
		if ((ulong)val <= 3uL)
		{
			switch ((int)val)
			{
			case 0:
				Protocol.SendValue("capture_mode", "precise quality");
				break;
			case 1:
				Protocol.SendValue("capture_mode", "burst quality");
				break;
			case 2:
				Protocol.SendValue("capture_mode", "precise self quality");
				break;
			case 3:
				Protocol.SendValue("capture_mode", "precise quality cont.");
				break;
			}
		}
	}

	public void SendCommand(int command)
	{
		_resetEvent.Reset();
		Protocol.SendCommand(command);
		_resetEvent.WaitOne(500);
	}

	public void SendCommand(int command, string param)
	{
		_resetEvent.Reset();
		Protocol.SendCommand(command, param);
		_resetEvent.WaitOne(1000);
	}

	private PropertyValue<long> AddNames(string keyname, string name)
	{
		PropertyValue<long> res = new PropertyValue<long>
		{
			Tag = keyname,
			Name = name
		};
		res.ValueChanged += (object sender, string key, long val) =>
		{
			Protocol.SendValue(res.Tag, key);
		};
		return res;
	}

	private string GetValue(string key)
	{
		return CurrentValues.ContainsKey(key) ? CurrentValues[key] : "";
	}

	private void Protocol_DataReceiverd(object sender, string data)
	{
		try
		{
			if (data.Contains("msg_id"))
			{
				dynamic val = JsonConvert.DeserializeObject(data);
				switch ((string)val.msg_id)
				{
				case "3":
					CurrentValues.Clear();
					foreach (object item in val.param)
					{
						JObject jObject = (JObject)(dynamic)item;
						Dictionary<string, string> source = jObject.ToObject<Dictionary<string, string>>();
						CurrentValues.Add(source.First().Key, source.First().Value);
					}
					break;
				case "7":
				{
					string text = (string)val.type;
					string text2 = text;
					if (text2 == "burst_complete" || text2 == "photo_taken")
					{
						string fileName = Path.GetFileName(((string)val.param).Replace('/', '\\'));
						PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
						{
							WiaImageItem = null,
							EventArgs = new PortableDeviceEventArgs(),
							CameraDevice = this,
							FileName = fileName,
							Handle = fileName
						};
						OnPhotoCapture(this, args);
					}
					else
					{
						SetProperty((string)val.type, (string)val.param);
					}
					break;
				}
				case "9":
					SetValues(data);
					break;
				}
				_lastData = data;
				if (data.Contains("listing"))
				{
					_listingEvent.Set();
				}
			}
		}
		catch (Exception)
		{
		}
		_resetEvent.Set();
	}

	public override AsyncObservableCollection<DeviceObject> GetObjects(object storageId, bool loadThumbs)
	{
		AsyncObservableCollection<DeviceObject> asyncObservableCollection = new AsyncObservableCollection<DeviceObject>();
		SendCommand(1283, "\\/var\\/www\\/DCIM\\/100MEDIA/");
		_listingEvent.Reset();
		SendCommand(1282, " -D -S");
		_listingEvent.WaitOne(2500);
		dynamic val = JsonConvert.DeserializeObject(_lastData);
		WebClient webClient = new WebClient();
		foreach (object item in val.listing)
		{
			JObject jObject = (JObject)(dynamic)item;
			Dictionary<string, string> source = jObject.ToObject<Dictionary<string, string>>();
			string value = source.First().Value;
			DeviceObject deviceObject = new DeviceObject();
			deviceObject.FileName = source.First().Key;
			if (deviceObject.FileName.ToLower().Contains("thm"))
			{
				continue;
			}
			deviceObject.Handle = deviceObject.FileName;
			try
			{
				if (loadThumbs)
				{
					if (deviceObject.FileName.Contains(".jpg"))
					{
						deviceObject.ThumbData = webClient.DownloadData($"http://{Protocol.Ip}/DCIM/100MEDIA/{deviceObject.FileName}?type=thumb");
					}
					if (deviceObject.FileName.Contains(".mp4"))
					{
						deviceObject.ThumbData = webClient.DownloadData(string.Format("http://{0}/DCIM/100MEDIA/{1}?type=thumb", Protocol.Ip, deviceObject.FileName.Replace("mp4", "THM")));
					}
				}
				if (value.Contains("|"))
				{
					deviceObject.FileDate = DateTime.ParseExact(value.Split(new char[1] { '|' })[1], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
				}
			}
			catch (Exception)
			{
			}
			asyncObservableCollection.Add(deviceObject);
		}
		return asyncObservableCollection;
	}

	public override void FormatStorage(object storageId)
	{
		AsyncObservableCollection<DeviceObject> objects = GetObjects(storageId, loadThumbs: false);
		foreach (DeviceObject item in objects)
		{
			DeleteObject(item);
		}
	}

	public override void CapturePhotoNoAf()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		CapturePhoto();
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		IsBusy = true;
		if (Mode.NumericValue == 3)
		{
			_timelapse_running = !_timelapse_running;
		}
		else
		{
			_timelapse_running = false;
		}
		Mode.IsEnabled = !_timelapse_running;
		Protocol.SendCommand(_timelapse_running ? 770 : 769);
	}

	public override void StartRecordMovie()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		CameraControl.Devices.LegacyCapturePolicy.RequireMovieRecording();

		Protocol.SendCommand(513);
	}

	public override void StopRecordMovie()
	{
		Protocol.SendCommand(514);
	}

	public override bool DeleteObject(DeviceObject deviceObject)
	{
		SendCommand(1283, "\\/var\\/www\\/DCIM\\/100MEDIA/");
		SendCommand(1281, (string)deviceObject.Handle);
		return true;
	}

	public override void TransferFile(object o, string filename)
	{
		TransferProgress = 0u;
		HttpHelper.DownLoadFileByWebRequest($"http://{Protocol.Ip}/DCIM/100MEDIA/{o}", filename, this, 0L);
	}

	public override void TransferFileThumb(object o, string filename)
	{
		TransferProgress = 0u;
		HttpHelper.DownLoadFileByWebRequest($"http://{Protocol.Ip}/DCIM/100MEDIA/{o}?type=screen", filename, this, 0L);
	}

	private void SetProperty(string prop, string val)
	{
		switch (prop)
		{
		case "battery":
		{
			if (int.TryParse(val, out var result))
			{
				Battery = result;
			}
			return;
		}
		case "photo_quality":
			CompressionSetting.Value = val;
			return;
		case "capture_mode":
			switch (val)
			{
			case "precise quality":
				Mode.SetValue(0L);
				break;
			case "burst quality":
				Mode.SetValue(1L);
				break;
			case "precise self quality":
				Mode.SetValue(2L);
				break;
			case "precise quality cont.":
				Mode.SetValue(3L);
				break;
			}
			return;
		case "meter_mode":
			ExposureMeteringMode.Value = val;
			return;
		}
		foreach (PropertyValue<long> property in Properties)
		{
			if (property.Tag == prop)
			{
				property.Value = val;
			}
		}
		foreach (PropertyValue<long> advancedProperty in AdvancedProperties)
		{
			if (advancedProperty.Tag == prop)
			{
				advancedProperty.Value = val;
			}
		}
	}

	private void SetValues(string data)
	{
		List<string> list = new List<string>();
		dynamic val = JsonConvert.DeserializeObject(data);
		foreach (object item2 in val.options)
		{
			string item = (string)(dynamic)item2;
			list.Add(item);
		}
		string text = (string)val.param;
		string text2 = text;
		string text3 = text2;
		if (!(text3 == "photo_quality"))
		{
			if (!(text3 == "meter_mode"))
			{
				foreach (PropertyValue<long> property in Properties)
				{
					if (property.Tag == text)
					{
						property.Clear();
						for (int i = 0; i < list.Count; i++)
						{
							property.AddValues(list[i], i);
						}
						property.IsEnabled = val.permission == "settable";
						property.ReloadValues();
						property.Value = GetValue(text);
					}
				}
				{
					foreach (PropertyValue<long> advancedProperty in AdvancedProperties)
					{
						if (advancedProperty.Tag == text)
						{
							advancedProperty.Clear();
							for (int j = 0; j < list.Count; j++)
							{
								advancedProperty.AddValues(list[j], j);
							}
							advancedProperty.IsEnabled = val.permission == "settable";
							advancedProperty.ReloadValues();
							advancedProperty.Value = GetValue(text);
						}
					}
					return;
				}
			}
			ExposureMeteringMode.Clear();
			for (int k = 0; k < list.Count; k++)
			{
				ExposureMeteringMode.AddValues(list[k], k);
			}
			ExposureMeteringMode.ReloadValues();
			ExposureMeteringMode.IsEnabled = val.permission == "settable";
			ExposureMeteringMode.Value = GetValue(text);
		}
		else
		{
			CompressionSetting.Clear();
			for (int l = 0; l < list.Count; l++)
			{
				CompressionSetting.AddValues(list[l], l);
			}
			CompressionSetting.IsEnabled = val.permission == "settable";
			CompressionSetting.ReloadValues();
			CompressionSetting.Value = GetValue(text);
		}
	}

	public override string GetLiveViewStream()
	{
		return $"rtsp://{Protocol.Ip}/live";
	}

	public override void StartLiveView()
	{
		Protocol.SendCommand(259, "none_force");
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder(base.ToString() + "\n\tType..................YiA");
		return stringBuilder.ToString();
	}
}
