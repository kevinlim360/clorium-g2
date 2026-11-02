using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using CameraControl.Devices.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PortableDeviceLib;

namespace CameraControl.Devices.Sony;

public class SonyWifiCamera : BaseCameraDevice
{
	private struct CommonHeader
	{
		public byte StartByte;

		public byte Type;

		public short SequenceNo;

		public int TimeStamp;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct PayloadHeader
	{
		public int StartCode;

		public ushort JpgDataSize;

		public byte PadingSize;

		public int Reserved;

		public byte Flag;
	}

	private LiveViewData _liveViewData = new LiveViewData();

	private bool _shoulStopLiveView;

	private static int request_id;

	private readonly HttpClient mClient = new HttpClient();

	private List<string> AvailableMethods;

	private System.Timers.Timer _timer = new System.Timers.Timer(100.0);

	private string _liveViewUrl = "";

	private long _lastZoomPos = 0L;

	public string EndPoint { get; set; }

	public bool Init(string endpoint)
	{
		Capabilities.Add(CapabilityEnum.LiveView);
		Capabilities.Add(CapabilityEnum.Zoom);
		_timer.Elapsed += _timer_Elapsed;
		_timer.AutoReset = true;
		IsBusy = true;
		EndPoint = endpoint;
		AvailableMethods = GetMethodTypes();
		ExecuteMethod("startRecMode");
		IsConnected = true;
		ExposureMeteringMode = new PropertyValue<long>();
		ExposureMeteringMode.Available = false;
		LiveViewImageZoomRatio = new PropertyValue<long>();
		for (int i = 0; i < 101; i++)
		{
			LiveViewImageZoomRatio.AddValues(i.ToString(), i);
		}
		LiveViewImageZoomRatio.ReloadValues();
		LiveViewImageZoomRatio.IsEnabled = false;
		LiveViewImageZoomRatio.ValueChanged += LiveViewImageZoomRatio_ValueChanged;
		Task.Factory.StartNew(InitProps);
		return true;
	}

	private void LiveViewImageZoomRatio_ValueChanged(object sender, string key, long val)
	{
		long num = Math.Abs(_lastZoomPos - val);
		if (val > _lastZoomPos)
		{
			ExecuteMethod("actZoom", "out", (num < 25) ? "1shot" : "start");
		}
		else
		{
			ExecuteMethod("actZoom", "in", (num < 25) ? "1shot" : "start");
		}
		_lastZoomPos = val;
	}

	private void InitProps()
	{
		Thread.Sleep(3500);
		try
		{
			InitMode();
			InitIso();
			InitShutterSpeed();
			InitFNumber();
			InitFocusMode();
			InitWhitebalce();
			InitCompressionSetting();
		}
		catch (Exception exception)
		{
			Log.Error("Sony prop init error", exception);
		}
		IsBusy = false;
		_timer.Start();
	}

	private void InitIso()
	{
		try
		{
			IsoNumber = new PropertyValue<long>();
			PropertyValue<long> isoNumber = IsoNumber;
			Capability<string> cap = AsCapability<string>(Post(CreateJson("getAvailableIsoSpeedRate")));
			SetCapability(isoNumber, cap);
			isoNumber.ValueChanged += (object sender, string key, long val) =>
			{
				CheckError(Post(CreateJson("setIsoSpeedRate", "1.0", key)));
			};
		}
		catch (Exception)
		{
		}
	}

	private void InitFNumber()
	{
		FNumber = new PropertyValue<long>();
		PropertyValue<long> fNumber = FNumber;
		Capability<string> cap = AsCapability<string>(Post(CreateJson("getAvailableFNumber")));
		SetCapability(fNumber, cap);
		fNumber.ValueChanged += (object sender, string key, long val) =>
		{
			CheckError(Post(CreateJson("setFNumber", "1.0", key)));
		};
	}

	private void InitShutterSpeed()
	{
		ShutterSpeed = new PropertyValue<long>();
		PropertyValue<long> shutterSpeed = ShutterSpeed;
		Capability<string> cap = AsCapability<string>(Post(CreateJson("getAvailableShutterSpeed")));
		SetCapability(shutterSpeed, cap);
		shutterSpeed.ValueChanged += (object sender, string key, long val) =>
		{
			CheckError(Post(CreateJson("setShutterSpeed", "1.0", key)));
		};
	}

	private void InitFocusMode()
	{
		FocusMode = new PropertyValue<long>();
	}

	private void InitWhitebalce()
	{
		WhiteBalance = new PropertyValue<long>();
		try
		{
			PropertyValue<long> whiteBalance = WhiteBalance;
			Capability<string> cap = AsCapability<string>(Post(CreateJson("getAvailableWhiteBalance")));
			SetCapability(whiteBalance, cap);
			whiteBalance.ValueChanged += (object sender, string key, long val) =>
			{
				CheckError(Post(CreateJson("setWhiteBalance", "1.0", key)));
			};
		}
		catch (Exception)
		{
			WhiteBalance.Available = false;
		}
	}

	private void InitCompressionSetting()
	{
		CompressionSetting = new PropertyValue<long>();
		try
		{
			PropertyValue<long> compressionSetting = CompressionSetting;
			Capability<string> cap = AsCapability<string>(Post(CreateJson("getAvailableStillQuality")));
			SetCapability(compressionSetting, cap);
			compressionSetting.ValueChanged += (object sender, string key, long val) =>
			{
				CheckError(Post(CreateJson("setStillQuality", "1.0", key)));
			};
		}
		catch (Exception)
		{
			CompressionSetting.Available = false;
		}
	}

	private void InitMode()
	{
		try
		{
			Mode = new PropertyValue<long>();
			PropertyValue<long> mode = Mode;
			Capability<string> cap = AsCapability<string>(Post(CreateJson("getAvailableExposureMode")));
			SetCapability(mode, cap);
			mode.ValueChanged += (object sender, string key, long val) =>
			{
				CheckError(Post(CreateJson("setExposureMode", "1.0", key)));
			};
		}
		catch (Exception)
		{
		}
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		_timer.Stop();
		Task.Factory.StartNew(GetEvent);
	}

	private void GetEvent()
	{
		try
		{
			string response = Post(CreateJson("getEvent", "1.0", false));
			JObject jObject = Initialize(response);
			if (!(jObject["result"] is JArray jArray))
			{
				return;
			}
			JToken jToken = jArray[2];
			if (jToken.HasValues)
			{
				LiveViewImageZoomRatio.SetValue(jToken.Value<int>("zoomPositionCurrentBox").ToString(), notifyValuChange: false);
			}
			jToken = jArray[5];
			if (jToken.HasValues)
			{
				foreach (JToken item in jToken.Children())
				{
					foreach (string item2 in item["takePictureUrl"].Values<string>())
					{
						string text = item2;
						if (text.Contains("?"))
						{
							text = text.Split(new char[1] { '?' })[0];
						}
						PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
						{
							WiaImageItem = null,
							EventArgs = new PortableDeviceEventArgs(),
							CameraDevice = this,
							FileName = text.Replace('/', '\\'),
							Handle = text
						};
						OnPhotoCapture(this, args);
					}
				}
			}
			jToken = jArray[18];
			if (jToken.HasValues)
			{
				SetCapability(Mode, new Capability<string>
				{
					Current = jToken.Value<string>("currentExposureMode"),
					Candidates = jToken["exposureModeCandidates"].Values<string>().ToList()
				});
			}
			jToken = jArray[27];
			if (jToken.HasValues)
			{
				SetCapability(FNumber, new Capability<string>
				{
					Current = jToken.Value<string>("currentFNumber"),
					Candidates = jToken["fNumberCandidates"].Values<string>().ToList()
				});
			}
			jToken = jArray[28];
			if (jToken.HasValues)
			{
				SetCapability(FocusMode, new Capability<string>
				{
					Current = jToken.Value<string>("currentFocusMode"),
					Candidates = jToken["focusModeCandidates"].Values<string>().ToList()
				});
			}
			jToken = jArray[29];
			if (jToken.HasValues)
			{
				SetCapability(IsoNumber, new Capability<string>
				{
					Current = jToken.Value<string>("currentIsoSpeedRate"),
					Candidates = jToken["isoSpeedRateCandidates"].Values<string>().ToList()
				});
			}
			jToken = jArray[32];
			if (jToken.HasValues)
			{
				SetCapability(ShutterSpeed, new Capability<string>
				{
					Current = jToken.Value<string>("currentShutterSpeed"),
					Candidates = jToken["shutterSpeedCandidates"].Values<string>().ToList()
				});
			}
			jToken = jArray[33];
			if (jToken.HasValues)
			{
				WhiteBalance.SetValue(jToken.Value<string>("currentWhiteBalanceMode"), notifyValuChange: false);
			}
			if (jArray.Count > 36)
			{
				jToken = jArray[37];
				if (jToken.HasValues)
				{
					SetCapability(CompressionSetting, new Capability<string>
					{
						Current = jToken.Value<string>("stillQuality"),
						Candidates = jToken["candidate"].Values<string>().ToList()
					});
				}
			}
		}
		catch (Exception exception)
		{
			Log.Debug("Sony get error ", exception);
		}
		_timer.Start();
	}

	public override void CapturePhotoNoAf()
	{
		CapturePhoto();
	}

	public override void CapturePhoto()
	{
		IsBusy = true;
		bool flag = true;
		List<string> list;
		while (true)
		{
			try
			{
				list = AsPrimitiveList<string>(flag ? Post(CreateJson("actTakePicture")) : Post(CreateJson("awaitTakePicture")));
			}
			catch (DeviceException ex)
			{
				if (ex.ErrorCode == 40403)
				{
					flag = false;
					continue;
				}
				IsBusy = false;
				Log.Error("Sony capture error ", ex);
				throw;
			}
			catch (Exception exception)
			{
				IsBusy = false;
				Log.Error("Sony capture error ", exception);
				throw;
			}
			break;
		}
		foreach (string item in list)
		{
			string text = item;
			if (text.Contains("?"))
			{
				text = text.Split(new char[1] { '?' })[0];
			}
			Log.Debug("Url to process " + text);
			PhotoCapturedEventArgs args = new PhotoCapturedEventArgs
			{
				WiaImageItem = null,
				EventArgs = new PortableDeviceEventArgs(),
				CameraDevice = this,
				FileName = text.Replace('/', '\\'),
				Handle = text
			};
			OnPhotoCapture(this, args);
		}
	}

	public override void StartLiveView()
	{
		_shoulStopLiveView = false;
		_liveViewUrl = AsPrimitive<string>(Post(CreateJson("startLiveview")));
		BeginGetStream(new Uri(_liveViewUrl));
	}

	public override void StopLiveView()
	{
		_shoulStopLiveView = true;
		Post(CreateJson("stopLiveview"));
	}

	public override LiveViewData GetLiveViewImage()
	{
		return _liveViewData;
	}

	public override void TransferFile(object o, string filename)
	{
		TransferProgress = 0u;
		HttpHelper.DownLoadFileByWebRequest((string)o + "?size=Origin", filename, this, 0L);
	}

	public override void TransferFileThumb(object o, string filename)
	{
		TransferProgress = 0u;
		HttpHelper.DownLoadFileByWebRequest((string)o + "?size=Scn", filename, this, 0L);
	}

	private static void SetCapability<T>(PropertyValue<T> prop, Capability<string> cap)
	{
		if (prop == null || (prop?.Value == cap.Current && prop.Values.Count == cap.Candidates.Count))
		{
			return;
		}
		foreach (string candidate in cap.Candidates)
		{
			prop.AddValues(candidate, default);
		}
		if (cap.Candidates.Count == 0 && !string.IsNullOrEmpty(cap.Current))
		{
			prop.AddValues(cap.Current, default);
			prop.IsEnabled = false;
		}
		else
		{
			prop.IsEnabled = true;
		}
		prop.ReloadValues();
		prop.SetValue(cap.Current, notifyValuChange: false);
	}

	private void ExecuteMethod(string method, params object[] prms)
	{
		string text = Post(CreateJson(method, prms));
	}

	private List<string> GetMethodTypes()
	{
		List<string> list = new List<string>();
		string response = Post(CreateJson("getMethodTypes", "1.0", ""));
		JObject jObject = Initialize(response);
		foreach (JToken item in (IEnumerable<JToken>)jObject["results"])
		{
			list.Add(item.Value<string>(0));
		}
		return list;
	}

	private static string CreateJson(string name, params object[] prms)
	{
		return CreateJson(name, "1.0", prms);
	}

	private static string CreateJson(string name, string version, params object[] prms)
	{
		JArray jArray = new JArray();
		if (prms != null)
		{
			foreach (object content in prms)
			{
				jArray.Add(content);
			}
		}
		JObject jObject = new JObject(new JProperty("method", name), new JProperty("version", version), new JProperty("id", GetID()), new JProperty("params", jArray));
		return jObject.ToString(Formatting.None);
	}

	private static int GetID()
	{
		int result = Interlocked.Increment(ref request_id);
		if (request_id > 1000000000)
		{
			request_id = 0;
		}
		return result;
	}

	internal string Post(string body)
	{
		if (EndPoint == null || body == null)
		{
			throw new ArgumentNullException();
		}
		StringContent stringContent = new StringContent(body);
		stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
		try
		{
			HttpResponseMessage result = mClient.PostAsync(EndPoint, stringContent).Result;
			return Encoding.UTF8.GetString(result.Content.ReadAsByteArrayAsync().Result);
		}
		catch (Exception ex)
		{
			throw ex;
		}
	}

	internal static void CheckError(string jString)
	{
		JObject jObject = Initialize(jString);
	}

	internal static JObject Initialize(string response)
	{
		JObject jObject = JObject.Parse(response);
		if (jObject == null)
		{
		}
		if (jObject["error"] != null)
		{
			throw new DeviceException(jObject["error"].Value<string>(1), jObject["error"].Value<int>(0));
		}
		return jObject;
	}

	internal static List<T> AsPrimitiveList<T>(string jString)
	{
		JObject jObject = Initialize(jString);
		return jObject["result"][0].Values<T>().ToList();
	}

	internal static T AsPrimitive<T>(string jString)
	{
		JObject jObject = Initialize(jString);
		return jObject["result"].Value<T>(0);
	}

	internal static Capability<T> AsCapability<T>(string jString)
	{
		JObject jObject = Initialize(jString);
		return new Capability<T>
		{
			Current = jObject["result"].Value<T>(0),
			Candidates = jObject["result"][1].Values<T>().ToList()
		};
	}

	public void BeginGetStream(Uri uri)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
		httpWebRequest.BeginGetResponse(OnGetResponse, httpWebRequest);
	}

	private void OnGetResponse(IAsyncResult asyncResult)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)asyncResult.AsyncState;
		try
		{
			HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.EndGetResponse(asyncResult);
			Stream responseStream = httpWebResponse.GetResponseStream();
			do
			{
				CommonHeader commonHeader = default;
				byte[] array = ReadBytes(responseStream, 8);
				commonHeader.StartByte = array[0];
				commonHeader.Type = array[1];
				commonHeader.SequenceNo = BitConverter.ToInt16(array, 2);
				commonHeader.TimeStamp = BitConverter.ToInt32(array, 4);
				PayloadHeader payloadHeader = default;
				array = ReadBytes(responseStream, 128);
				payloadHeader.StartCode = BitConverter.ToInt32(array, 0);
				payloadHeader.JpgDataSize = BitConverter.ToUInt16(new byte[4]
				{
					array[6],
					array[5],
					array[4],
					0
				}, 0);
				payloadHeader.PadingSize = array[7];
				if (commonHeader.Type == 2)
				{
					if (payloadHeader.JpgDataSize > 0)
					{
						byte[] array2 = ReadBytes(responseStream, payloadHeader.JpgDataSize);
						_liveViewData.FocusX = BitConverter.ToInt16(new byte[2]
						{
							array2[1],
							array2[0]
						}, 0);
						_liveViewData.FocusY = BitConverter.ToInt16(new byte[2]
						{
							array2[3],
							array2[2]
						}, 0);
						_liveViewData.FocusFrameXSize = BitConverter.ToInt16(new byte[2]
						{
							array2[5],
							array2[4]
						}, 0) - _liveViewData.FocusX;
						_liveViewData.FocusFrameYSize = BitConverter.ToInt16(new byte[2]
						{
							array2[7],
							array2[6]
						}, 0) - _liveViewData.FocusY;
						_liveViewData.HaveFocusData = true;
						_liveViewData.Focused = array2[9] != 0;
						_liveViewData.ImageWidth = 10000;
						_liveViewData.ImageHeight = 10000;
						_liveViewData.IsLiveViewRunning = true;
						Console.WriteLine(payloadHeader.JpgDataSize);
					}
				}
				else
				{
					_liveViewData.ImageData = ReadBytes(responseStream, payloadHeader.JpgDataSize);
				}
				if (payloadHeader.PadingSize > 0)
				{
					ReadBytes(responseStream, payloadHeader.PadingSize);
				}
			}
			while (!_shoulStopLiveView);
			httpWebResponse.Close();
		}
		catch (Exception exception)
		{
			Log.Error("Unable to download stream ", exception);
		}
	}

	private void ByteArrayToStructure(Stream br, ref object obj)
	{
		int num = Marshal.SizeOf(obj);
		byte[] source = ReadBytes(br, num);
		IntPtr intPtr = Marshal.AllocHGlobal(num);
		Marshal.Copy(source, 0, intPtr, num);
		obj = Marshal.PtrToStructure(intPtr, obj.GetType());
		Marshal.FreeHGlobal(intPtr);
	}

	private byte[] ReadBytes(Stream s, int length)
	{
		byte[] array = new byte[length];
		for (int i = 0; i != length; i += s.Read(array, i, length - i))
		{
		}
		return array;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder(base.ToString() + "\n\tType..................Sony WiFi");
		return stringBuilder.ToString();
	}

	public override void StartZoom(ZoomDirection direction)
	{
		switch (direction)
		{
		case ZoomDirection.In:
			ExecuteMethod("actZoom", "in", "start");
			break;
		case ZoomDirection.Out:
			ExecuteMethod("actZoom", "out", "start");
			break;
		default:
			throw new ArgumentOutOfRangeException("direction", direction, null);
		}
	}

	public override void StopZoom(ZoomDirection direction)
	{
		switch (direction)
		{
		case ZoomDirection.In:
			ExecuteMethod("actZoom", "in", "stop");
			break;
		case ZoomDirection.Out:
			ExecuteMethod("actZoom", "out", "stop");
			break;
		default:
			throw new ArgumentOutOfRangeException("direction", direction, null);
		}
	}
}
