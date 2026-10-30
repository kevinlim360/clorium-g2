using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Timers;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace PanonoTest;

public class PanonoCamera : BaseCameraDevice
{
	private WebSocket _ws;

	private static int request_id;

	private System.Timers.Timer _timer = new System.Timers.Timer(500.0);

	public bool Init(string endpoint)
	{
		_ws = new WebSocket(endpoint);
		_ws.WaitTime = new TimeSpan(0, 0, 4);
		_ws.OnMessage += Ws_OnMessage;
		_ws.OnClose += _ws_OnClose;
		_ws.OnError += _ws_OnError;
		_ws.Connect();
		LiveViewImageZoomRatio = new PropertyValue<long>();
		Auth();
		IsConnected = true;
		IsoNumber = new PropertyValue<long>
		{
			Available = true
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
		InitIso();
		InitMode();
		_timer.Elapsed += _timer_Elapsed;
		_timer.AutoReset = true;
		return true;
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		try
		{
		}
		catch (WebException ex)
		{
			if (ex.HResult != -2146233067)
			{
				Log.Error("Ping error ", ex);
			}
		}
	}

	private void _ws_OnError(object sender, ErrorEventArgs e)
	{
		StaticHelper.Instance.SystemMessage = e.Message;
	}

	private void _ws_OnClose(object sender, CloseEventArgs e)
	{
		Close();
		OnCameraDisconnected(this, new DisconnectCameraEventArgs());
	}

	public override void Close()
	{
		_timer.Stop();
		try
		{
			_ws.OnMessage -= Ws_OnMessage;
			_ws.OnClose -= _ws_OnClose;
			_ws.OnError -= _ws_OnError;
			_ws.Close();
		}
		catch (Exception exception)
		{
			Log.Error("Camera close error", exception);
		}
	}

	private void InitIso()
	{
		IsoNumber.Tag = "ISO";
		IsoNumber.AddValues("50", 50L);
		IsoNumber.AddValues("100", 100L);
		IsoNumber.AddValues("200", 200L);
		IsoNumber.AddValues("400", 400L);
		IsoNumber.AddValues("800", 800L);
		IsoNumber.AddValues("1600", 1600L);
		IsoNumber.ReloadValues();
		IsoNumber.ValueChanged += IsoNumber_ValueChanged;
		ExecuteMethod("get_options", IsoNumber.Tag);
	}

	private void InitMode()
	{
		Mode = new PropertyValue<long>
		{
			Tag = "ImageType"
		};
		Mode.AddValues("Default", 0L);
		Mode.AddValues("HDR", 1L);
		Mode.ReloadValues();
		Mode.ValueChanged += Mode_ValueChanged;
		ExecuteMethod("get_options", Mode.Tag);
	}

	private void SetProperty(string key, string value)
	{
		_ws.Send("{ \"id\":3,\"method\":\"set_options\",\"params\":{ \"" + key + "\":\"" + value + "\"},\"jsonrpc\":\"2.0\"}");
	}

	private void Mode_ValueChanged(object sender, string key, long val)
	{
		SetProperty(Mode.Tag, key);
	}

	private void IsoNumber_ValueChanged(object sender, string key, long val)
	{
		SetProperty(IsoNumber.Tag, key);
	}

	public void Auth()
	{
		ExecuteMethod("auth", "{\"device\":\"test\",\"force\":\"test\"}");
	}

	public override void CapturePhoto()
	{
        global::CameraControl.Devices.Classes.CaptureAuthorization.AssertAllowed();
		IsBusy = true;
		ExecuteMethod("capture");
	}

	public override bool DeleteObject(DeviceObject deviceObject)
	{
		return true;
	}

	private void Ws_OnMessage(object sender, MessageEventArgs e)
	{
		try
		{
			JObject jObject = JObject.Parse(e.Data);
			if (jObject["result"] != null)
			{
				Dictionary<string, object> dictionary = jObject["result"].ToObject<Dictionary<string, object>>();
				if (dictionary.ContainsKey("serial_number"))
				{
					SerialNumber = (string)dictionary["serial_number"];
				}
				if (dictionary.ContainsKey(IsoNumber.Tag))
				{
					IsoNumber.SetValue((string)dictionary[IsoNumber.Tag], notifyValuChange: false);
				}
				if (dictionary.ContainsKey(Mode.Tag))
				{
					Mode.SetValue((string)dictionary[Mode.Tag], notifyValuChange: false);
				}
			}
			if ((string?)jObject["method"] == "status_update")
			{
				if (jObject["params"]["battery_value"] != null)
				{
					Battery = jObject["params"]["battery_value"].Value<int>();
				}
				if (jObject["params"]["capture_available"] != null)
				{
					IsBusy = jObject["params"]["capture_available"].Value<bool>();
				}
			}
			if ((string?)jObject["method"] == "upf_infos_update" && jObject["params"] != null)
			{
				Dictionary<string, object> dictionary2 = jObject["params"]["upf_infos"][0].ToObject<Dictionary<string, object>>();
				if (dictionary2.ContainsKey("upf_status") && (string)dictionary2["upf_status"] == "ready")
				{
					string text = (string)dictionary2["upf_url"];
					PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
					{
						WiaImageItem = null,
						EventArgs = null,
						CameraDevice = this,
						FileName = text.Replace('/', '\\'),
						Handle = new List<object>
						{
							text,
							(long)dictionary2["upf_size"]
						}
					};
					OnPhotoCapture(this, args);
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public override void TransferFile(object o, string filename)
	{
		_timer.Stop();
		Thread.Sleep(100);
		TransferProgress = 0u;
		List<object> list = o as List<object>;
		HttpHelper.DownLoadFileByWebRequest((string)list[0], filename, this, (long)list[1]);
		_timer.Start();
	}

	public override void TransferFileThumb(object o, string filename)
	{
		_timer.Stop();
		Thread.Sleep(100);
		TransferProgress = 0u;
		List<object> list = o as List<object>;
		HttpHelper.DownLoadFileByWebRequest(((string)list[0]).Replace("panoramas", "previews"), filename, this, 0L);
		_timer.Start();
	}

	private void ExecuteMethod(string method, params object[] prms)
	{
		_ws.Send(CreateJson(method, prms));
	}

	private int GetId()
	{
		int result = Interlocked.Increment(ref request_id);
		if (request_id > 1000000000)
		{
			request_id = 0;
		}
		return result;
	}

	private string CreateJson(string name, params object[] prms)
	{
		return CreateJson(name, "2.0", prms);
	}

	private string CreateJson(string name, string version, params object[] prms)
	{
		JArray jArray = new JArray();
		JObject jObject;
		if (prms != null && prms.Length != 0)
		{
			foreach (object content in prms)
			{
				jArray.Add(content);
			}
			jObject = new JObject(new JProperty("id", GetId()), new JProperty("method", name), new JProperty("params", jArray), new JProperty("jsonrpc", version));
		}
		else
		{
			jObject = new JObject(new JProperty("id", GetId()), new JProperty("method", name), new JProperty("jsonrpc", version));
		}
		return jObject.ToString(Formatting.None);
	}
}
