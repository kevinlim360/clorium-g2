using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using CameraControl.Devices.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CameraControl.Devices.Others;

public class OscCamera : BaseCameraDevice
{
	private string ExecuteEndPoint = "/osc/commands/execute";

	private string InfoEndPoint = "/osc/info";

	private string StatusEndPoint = "/osc/commands/status";

	public string Address { get; set; }

	public string SessionId { get; set; }

	public void Init(string address)
	{
		LiveViewImageZoomRatio = new PropertyValue<long>();
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
			Available = true
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
		Mode = new PropertyValue<long>
		{
			Available = true
		};
		SessionId = null;
		Address = address;
		GetInfo();
		GetSessionId(GetExecute(CreateJson("camera.startSession")));
		InitIso();
		InitExposureCompensation();
		InitMode();
	}

	private void InitIso()
	{
		try
		{
			IsoNumber = new PropertyValue<long>
			{
				Available = true,
				IsEnabled = true
			};
			string property = "iso";
			string property2 = GetProperty(property);
			JObject jObject = Initialize(property2);
			string text = jObject["results"]["options"][property].Value<string>();
			List<int> list = jObject["results"]["options"][property + "Support"].Values<int>().ToList();
			foreach (int item in list)
			{
				IsoNumber.AddValues((item == 0) ? "Auto" : item.ToString(), item);
			}
			if (text == "0")
			{
				if (!list.Contains(0))
				{
					IsoNumber.AddValues("Auto", 0L);
				}
				IsoNumber.Value = "Auto";
				IsoNumber.IsEnabled = false;
			}
			else
			{
				IsoNumber.Value = text;
			}
			IsoNumber.ReloadValues();
			IsoNumber.ValueChanged += (object o, string s, long i) =>
			{
				SetProperty(property, i);
			};
		}
		catch (Exception exception)
		{
			Log.Debug("Unable to get ISO", exception);
			throw;
		}
	}

	private void InitExposureCompensation()
	{
		try
		{
			string property = "exposureCompensation";
			string property2 = GetProperty(property);
			JObject jObject = Initialize(property2);
			string value = jObject["results"]["options"][property].Value<string>();
			IEnumerable<string> enumerable = jObject["results"]["options"][property + "Support"].Values<string>();
			foreach (string item in enumerable)
			{
				ExposureCompensation.AddValues(item, 0L);
			}
			ExposureCompensation.Value = value;
			ExposureCompensation.ReloadValues();
			ExposureCompensation.ValueChanged += (object o, string s, long i) =>
			{
				SetProperty(property, double.Parse(s, CultureInfo.InvariantCulture));
			};
		}
		catch (Exception exception)
		{
			Log.Debug("Unable to get exposureCompensation", exception);
		}
	}

	private void InitMode()
	{
		try
		{
			string property = "exposureProgram";
			string property2 = GetProperty(property);
			JObject jObject = Initialize(property2);
			uint num = jObject["results"]["options"][property].Value<uint>();
			Mode.AddValues("Auto", 2L);
			Mode.AddValues("ISO priority program", 9L);
			Mode.ReloadValues();
			Mode.ValueChanged += (object o, string s, long i) =>
			{
				SetProperty(property, i);
				InitIso();
			};
			Mode.SetValue(num);
		}
		catch (Exception exception)
		{
			Log.Debug("Unable to get exposureProgram", exception);
		}
	}

	private string GetProperty(string name)
	{
		JArray jArray = new JArray();
		jArray.Add(name);
		jArray.Add(name + "Support");
		string postData = CreateJson("camera.getOptions", new JProperty("optionNames", jArray));
		return GetExecute(postData);
	}

	public void SetProperty(string name, object val)
	{
		try
		{
			JObject jObject = new JObject();
			jObject.Add(new JProperty(name, val));
			string postData = CreateJson("camera.setOptions", new JProperty("options", jObject));
			string execute = GetExecute(postData);
			Initialize(execute);
		}
		catch (Exception exception)
		{
			Log.Error("Unable to set property " + name + " " + val.ToString(), exception);
		}
	}

	private void GetInfo()
	{
		string response = Get(InfoEndPoint);
		JObject jObject = Initialize(response);
		Manufacturer = jObject["manufacturer"].Value<string>();
		DeviceName = jObject["model"].Value<string>();
		SerialNumber = jObject["serialNumber"].Value<string>();
	}

	private void GetSessionId(string response)
	{
		JObject jObject = Initialize(response);
		SessionId = jObject["results"]["sessionId"].Value<string>();
	}

	public override void CapturePhoto()
	{
		JObject jObject = Initialize(GetExecute(CreateJson("camera.takePicture")));
		string id = jObject["id"].Value<string>();
		string text = null;
		do
		{
			JObject jObject2 = Initialize(GetStatus(id));
			if (jObject2["state"].Value<string>() == "done")
			{
				text = jObject2["results"]["fileUri"].Value<string>();
			}
			else
			{
				Thread.Sleep(150);
			}
		}
		while (string.IsNullOrEmpty(text));
		Log.Debug("Url to process " + text);
		PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
		{
			WiaImageItem = null,
			EventArgs = null,
			CameraDevice = this,
			FileName = text.Replace('/', '\\'),
			Handle = text
		};
		OnPhotoCapture(this, args);
	}

	public override void TransferFile(object o, string filename)
	{
		string text = (string)o;
		if (!text.StartsWith("http"))
		{
			text = Address + "/" + text;
		}
		TransferProgress = 0u;
		HttpHelper.DownLoadFileByWebRequest(text, filename, this, 0L);
	}

	public override bool DeleteObject(DeviceObject deviceObject)
	{
		try
		{
			string text = (string)deviceObject.Handle;
			JArray content = new JArray { text };
			string postData = CreateJson("camera.delete", new JProperty("fileUrls", content));
			GetExecute(postData);
		}
		catch (Exception exception)
		{
			Log.Error("Unable to set property ", exception);
		}
		return true;
	}

	internal static JObject Initialize(string response)
	{
		JObject jObject = JObject.Parse(response);
		if (jObject == null)
		{
		}
		if (jObject["error"] != null)
		{
			throw new DeviceException(jObject["error"]["code"].Value<string>(), jObject["error"]["message"].Value<string>());
		}
		return jObject;
	}

	private string CreateJson(string name, params object[] prms)
	{
		JObject jObject = new JObject();
		if (!string.IsNullOrEmpty(SessionId))
		{
			jObject.Add(new JProperty("sessionId", SessionId));
		}
		if (prms != null)
		{
			foreach (object content in prms)
			{
				jObject.Add(content);
			}
		}
		JObject jObject2 = new JObject(new JProperty("name", name), new JProperty("parameters", jObject));
		return jObject2.ToString(Formatting.Indented);
	}

	private string GetUrl(string command)
	{
		return Address + command;
	}

	public string GetStatus(string id)
	{
		return Get(StatusEndPoint, new JObject(new JProperty("id", id)).ToString(Formatting.None));
	}

	public string GetExecute(string postData = null)
	{
		return Get(ExecuteEndPoint, postData);
	}

	public string Get(string command, string postData = null)
	{
		try
		{
			if (string.IsNullOrEmpty(postData))
			{
				using (WebClient webClient = new WebClient())
				{
					return webClient.DownloadString(GetUrl(command));
				}
			}
			WebRequest webRequest = WebRequest.Create(GetUrl(command));
			webRequest.Method = "POST";
			byte[] bytes = Encoding.UTF8.GetBytes(postData);
			webRequest.ContentType = "application/json;charset=utf-8";
			webRequest.ContentLength = bytes.Length;
			Stream stream = null;
			stream = webRequest.GetRequestStream();
			stream.Write(bytes, 0, bytes.Length);
			stream.Close();
			WebResponse response = webRequest.GetResponse();
			Console.WriteLine(((HttpWebResponse)response).StatusDescription);
			stream = response.GetResponseStream();
			StreamReader streamReader = new StreamReader(stream);
			string result = streamReader.ReadToEnd();
			streamReader.Close();
			stream.Close();
			response.Close();
			return result;
		}
		catch (WebException ex)
		{
			using WebResponse webResponse = ex.Response;
			HttpWebResponse httpWebResponse = (HttpWebResponse)webResponse;
			if (httpWebResponse != null)
			{
				using (Stream stream2 = httpWebResponse.GetResponseStream())
				{
					using StreamReader streamReader2 = new StreamReader(stream2);
					return streamReader2.ReadToEnd();
				}
			}
		}
		return "";
	}
}
