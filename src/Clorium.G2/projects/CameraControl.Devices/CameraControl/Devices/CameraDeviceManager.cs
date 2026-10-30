using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Accord.Video.DirectShow;
using CameraControl.Devices.Canon;
using CameraControl.Devices.Classes;
using CameraControl.Devices.Custom;
using CameraControl.Devices.Nikon;
using CameraControl.Devices.Others;
using CameraControl.Devices.TransferProtocol;
using CameraControl.Devices.TransferProtocol.DDServer;
using CameraControl.Devices.TransferProtocol.PtpIp;
using CameraControl.Devices.Wifi;
using Canon.Eos.Framework;
using PortableDeviceLib;
using WIA;

namespace CameraControl.Devices;

public class CameraDeviceManager : BaseFieldClass
{
	public delegate void CameraConnectedEventHandler(ICameraDevice cameraDevice);

	public delegate void CameraSelectedEventHandler(ICameraDevice oldcameraDevice, ICameraDevice newcameraDevice);

	private const string AppName = "CameraControl";

	private const int AppMajorVersionNumber = 1;

	private const int AppMinorVersionNumber = 0;

	private bool _connectionInProgress = false;

	private object _locker = new object();

	private bool debugMode = false;

	private DeviceDescriptorEnumerator _deviceEnumerator;

	private EosFramework _framework;

	private List<DeviceDescription> _deviceDescriptions = new List<DeviceDescription>();

	public ConcurrentDictionary<ICameraDevice, byte[]> LiveViewImage;

	public ConcurrentDictionary<ICameraDevice, string> LastCapturedImage;

	private ICameraDevice _selectedCameraDevice;

	private AsyncObservableCollection<ICameraDevice> _connectedDevices;

	public List<IWifiDeviceProvider> WifiDeviceProviders { get; set; }

	public static Dictionary<string, Type> DeviceClass { get; set; }

	public Dictionary<string, Type> CustomDeviceClass { get; set; }

	private DeviceManager WiaDeviceManager { get; set; }

	public bool LoadWiaDevices { get; set; }

	public bool DetectWebcams { get; set; }

	public bool StartInNewThread { get; set; }

	public bool DisableNativeDrivers { get; set; }

	public bool UseExperimentalDrivers { get; set; }

	public ICameraDevice SelectedCameraDevice
	{
		get
		{
			return _selectedCameraDevice;
		}
		set
		{
			ICameraDevice selectedCameraDevice = _selectedCameraDevice;
			ICameraDevice cameraDevice = value;
			if (cameraDevice == null)
			{
				if (ConnectedDevices.Count <= 0)
				{
					ICameraDevice cameraDevice2 = new NotConnectedCameraDevice();
					cameraDevice = cameraDevice2;
				}
				else
				{
					cameraDevice = ConnectedDevices[0];
				}
			}
			_selectedCameraDevice = cameraDevice;
			CameraSelected?.Invoke(selectedCameraDevice, _selectedCameraDevice);
			NotifyPropertyChanged("SelectedCameraDevice");
		}
	}

	public AsyncObservableCollection<ICameraDevice> ConnectedDevices
	{
		get
		{
			return _connectedDevices;
		}
		set
		{
			_connectedDevices = value;
			NotifyPropertyChanged("ConnectedDevices");
		}
	}

	public event PhotoCapturedEventHandler PhotoCaptured;

	public event CameraConnectedEventHandler CameraConnected;

	public event CameraConnectedEventHandler CameraDisconnected;

	public event CameraSelectedEventHandler CameraSelected;

	private void PopulateDeviceClass()
	{
		Dictionary<string, Type> dictionary = new Dictionary<string, Type>();
		dictionary.Add("D200", typeof(NikonD40));
		dictionary.Add("D3", typeof(NikonD90));
		dictionary.Add("D3S", typeof(NikonD90));
		dictionary.Add("D3X", typeof(NikonD3X));
		dictionary.Add("D300", typeof(NikonD300));
		dictionary.Add("D300s", typeof(NikonD300));
		dictionary.Add("D300S", typeof(NikonD300));
		dictionary.Add("D3200", typeof(NikonD3200));
		dictionary.Add("D3300", typeof(NikonD600Base));
		dictionary.Add("D3400", typeof(NikonD600Base));
		dictionary.Add("D3500", typeof(NikonD600Base));
		dictionary.Add("D4", typeof(NikonD4));
		dictionary.Add("D4s", typeof(NikonD4));
		dictionary.Add("D4S", typeof(NikonD4));
		dictionary.Add("D40", typeof(NikonD40));
		dictionary.Add("D40X", typeof(NikonD40));
		dictionary.Add("D5", typeof(NikonD5));
		dictionary.Add("D50", typeof(NikonD40));
		dictionary.Add("D500", typeof(NikonD500));
		dictionary.Add("D5600", typeof(NikonD5200));
		dictionary.Add("D5500", typeof(NikonD5200));
		dictionary.Add("D5300", typeof(NikonD5200));
		dictionary.Add("D5200", typeof(NikonD5200));
		dictionary.Add("D5100", typeof(NikonD5100));
		dictionary.Add("D5000", typeof(NikonD90));
		dictionary.Add("D60", typeof(NikonD60));
		dictionary.Add("D610", typeof(NikonD600Base));
		dictionary.Add("D600", typeof(NikonD600));
		dictionary.Add("D70", typeof(NikonD40));
		dictionary.Add("D70s", typeof(NikonD40));
		dictionary.Add("D700", typeof(NikonD700));
		dictionary.Add("D750", typeof(NikonD750));
		dictionary.Add("D780", typeof(NikonD750));
		dictionary.Add("D7000", typeof(NikonD7000));
		dictionary.Add("D7100", typeof(NikonD7100));
		dictionary.Add("D7200", typeof(NikonD7100));
		dictionary.Add("D7500", typeof(NikonD7500));
		dictionary.Add("D80", typeof(NikonD80));
		dictionary.Add("D800", typeof(NikonD800));
		dictionary.Add("D800E", typeof(NikonD800));
		dictionary.Add("D800e", typeof(NikonD800));
		dictionary.Add("D810", typeof(NikonD810));
		dictionary.Add("D810A", typeof(NikonD810));
		dictionary.Add("D810a", typeof(NikonD810));
		dictionary.Add("D850", typeof(NikonD850));
		dictionary.Add("D90", typeof(NikonD90));
		dictionary.Add("V1", typeof(NikonD5100));
		dictionary.Add("V2", typeof(NikonD5100));
		dictionary.Add("V3", typeof(NikonD600Base));
		dictionary.Add("J3", typeof(NikonD600Base));
		dictionary.Add("J4", typeof(NikonD600Base));
		dictionary.Add("J5", typeof(NikonD600Base));
		dictionary.Add("Df", typeof(NikonD500Base));
		dictionary.Add("L830", typeof(NikonL830));
		dictionary.Add("L840", typeof(NikonL830));
		dictionary.Add("Z 30", typeof(NikonZ6));
		dictionary.Add("Z 50", typeof(NikonZ6));
		dictionary.Add("Z 5", typeof(NikonZ6));
		dictionary.Add("Z 6", typeof(NikonZ6));
		dictionary.Add("Z 6_2", typeof(NikonZ6));
		dictionary.Add("Z 7", typeof(NikonZ7));
		dictionary.Add("Z 7_2", typeof(NikonZ7));
		dictionary.Add("Z 9", typeof(NikonZ7));
		dictionary.Add("Z fc", typeof(NikonZ7));
		dictionary.Add("MTP Sim", typeof(BaseMTPCamera));
		DeviceClass = dictionary;
		WifiDeviceProviders.Add(new DDServerProvider());
		WifiDeviceProviders.Add(new PtpIpProvider());
		WifiDeviceProviders.Add(new YiCameraProvider());
		WifiDeviceProviders.Add(new SonyProvider());
		WifiDeviceProviders.Add(new PanonoProvider());
		WifiDeviceProviders.Add(new OscProvider());
		WifiDeviceProviders.Add(new PtzOpticsProvider());
		WifiDeviceProviders.Add(new GoProProvider());
		foreach (KeyValuePair<string, Type> item in CustomDeviceClass)
		{
			DeviceClass.Add(item.Key, item.Value);
		}
	}

	public CameraDeviceManager(string datafolder = null)
	{
		UseExperimentalDrivers = true;
		LoadWiaDevices = true;
		DetectWebcams = true;
		StartInNewThread = false;
		CustomDeviceClass = new Dictionary<string, Type>();
		SelectedCameraDevice = new NotConnectedCameraDevice();
		ConnectedDevices = new AsyncObservableCollection<ICameraDevice>();
		_deviceEnumerator = new DeviceDescriptorEnumerator();
		LiveViewImage = new ConcurrentDictionary<ICameraDevice, byte[]>();
		LastCapturedImage = new ConcurrentDictionary<ICameraDevice, string>();
		WifiDeviceProviders = new List<IWifiDeviceProvider>();
		if (datafolder == null || !Directory.Exists(datafolder))
		{
			return;
		}
		try
		{
			string[] files = Directory.GetFiles(datafolder, "*.xml");
			string[] array = files;
			foreach (string file in array)
			{
				DeviceDescription deviceDescription = DeviceDescription.Load(file);
				if (deviceDescription != null)
				{
					_deviceDescriptions.Add(deviceDescription);
				}
			}
		}
		catch (Exception)
		{
			Log.Error("Error loading custom data");
		}
	}

	public bool ConnectToCamera(bool debug)
	{
		Log.Debug("Cmd : 카메라 연결", debugMode);
		debugMode = debug;
		if (UseExperimentalDrivers)
		{
			InitCanon();
		}
		return ConnectToCameraExe(retry: true);
	}

	public bool ConnectToCameraExe(bool retry)
	{
		if (DeviceClass == null || DeviceClass.Count == 0)
		{
			PopulateDeviceClass();
		}
		if (UseExperimentalDrivers && _framework != null && _framework.GetCameraCollection().Count > 0)
		{
			return true;
		}
		if (!LoadWiaDevices)
		{
			return true;
		}
		return AddWiaCameras();
	}

	private void ConnectDevices()
	{
		if (_connectionInProgress)
		{
			return;
		}
		_connectionInProgress = true;
		if (PortableDeviceCollection.Instance == null)
		{
			PortableDeviceCollection.CreateInstance("CameraControl", 1f, 0f);
			PortableDeviceCollection.Instance.AutoConnectToPortableDevice = false;
		}
		_deviceEnumerator.RemoveDisconnected();
		try
		{
			IEnumerable<PortableDevice> devices = PortableDeviceCollection.Instance.Devices;
			foreach (PortableDevice item in devices)
			{
				if ((!item.DeviceId.StartsWith("\\\\?\\usb") && !item.DeviceId.StartsWith("\\\\?\\comp")) || !SupportedCanonCamera(item.DeviceId))
				{
					continue;
				}
				item.ConnectToDevice("CameraControl", 1f, 0f);
				if (_deviceEnumerator.GetByWpdId(item.DeviceId) == null && GetNativeDriver(item.Model) != null)
				{
					DeviceDescriptor deviceDescriptor = new DeviceDescriptor
					{
						WpdId = item.DeviceId
					};
					ICameraDevice cameraDevice = (ICameraDevice)Activator.CreateInstance(GetNativeDriver(item.Model));
					MtpProtocol mtpProtocol = new MtpProtocol(deviceDescriptor.WpdId);
					mtpProtocol.ConnectToDevice("CameraControl", 1f, 0f);
					deviceDescriptor.StillImageDevice = mtpProtocol;
					cameraDevice.SerialNumber = StaticHelper.GetSerial(item.DeviceId);
					cameraDevice.Init(deviceDescriptor);
					if (string.IsNullOrWhiteSpace(cameraDevice.SerialNumber))
					{
						cameraDevice.SerialNumber = StaticHelper.GetSerial(item.DeviceId);
					}
					ConnectedDevices.Add(cameraDevice);
					NewCameraConnected(cameraDevice);
					deviceDescriptor.CameraDevice = cameraDevice;
					_deviceEnumerator.Add(deviceDescriptor);
				}
				if (_deviceEnumerator.GetByWpdId(item.DeviceId) == null && GetNativeDriver(item.Model) == null)
				{
					DeviceDescription deviceDescription = getDeviceDescription(item.Model);
					if (deviceDescription != null)
					{
						CustomDevice customDevice = new CustomDevice();
						DeviceDescriptor deviceDescriptor2 = new DeviceDescriptor
						{
							WpdId = item.DeviceId
						};
						MtpProtocol mtpProtocol2 = new MtpProtocol(deviceDescriptor2.WpdId);
						mtpProtocol2.ConnectToDevice("CameraControl", 1f, 0f);
						deviceDescriptor2.StillImageDevice = mtpProtocol2;
						customDevice.SerialNumber = StaticHelper.GetSerial(item.DeviceId);
						customDevice.Init(deviceDescriptor2, deviceDescription);
						ConnectedDevices.Add(customDevice);
						NewCameraConnected(customDevice);
						deviceDescriptor2.CameraDevice = customDevice;
						_deviceEnumerator.Add(deviceDescriptor2);
						break;
					}
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("Unable to connect to cameras ", exception);
		}
		_connectionInProgress = false;
	}

	private bool SupportedCanonCamera(string id)
	{
		if (!id.Contains("vid_04a9"))
		{
			return true;
		}
		return false;
	}

	private void cameraDevice_CameraDisconnected(object sender, DisconnectCameraEventArgs e)
	{
		Log.Debug("Cmd : 이벤트 발생 -> 카메라 연결 해제", debugMode);
		if (e.StillImageDevice != null)
		{
			DisconnectCamera(e.StillImageDevice);
		}
		if (e.EosCamera != null)
		{
			DisconnectCamera(e.EosCamera);
		}
		OnCameraDisconnected((ICameraDevice)sender);
	}

	private void cameraDevice_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
	{
		if (PhotoCaptured != null)
		{
			PhotoCaptured(sender, eventArgs);
		}
	}

	public void OnPhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
	{
		if (PhotoCaptured != null)
		{
			PhotoCaptured(sender, eventArgs);
		}
	}

	private void InitCanon()
	{
		try
		{
			if (_framework == null)
			{
				_framework = new EosFramework();
				_framework.CameraAdded += _framework_CameraAdded;
			}
			AddCanonCameras();
		}
		catch (Exception ex)
		{
			Log.Error("Unable init canon driver - Give specific guidance if the error is a missing DLL", ex);
			if (ex.InnerException != null && ex.InnerException.Message != null && ex.InnerException.Message.Contains("EDSDK.dll"))
			{
				Console.WriteLine("\n**CRITICAL ERROR**\n\nCanon EOS camera library, EDSDK.dll is missing\nInstall it after downloading from Canon's site\n");
			}
		}
	}

	private void _framework_CameraAdded(object sender, EventArgs e)
	{
		Log.Debug("Cmd : 이벤트 발생 -> 카메라 연결 추가", debugMode);
		SelectedCameraDevice.StopLiveView();
		Thread.Sleep(200);
		AddCanonCameras();
	}

	private void AddCanonCameras()
	{
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		lock (_locker)
		{
			try
			{
				List<EosCamera> list = GetEosCameras().ToList();
				int num = list.Count;
				if (num == 0)
				{
					return;
				}
				Log.Debug("감지된 캐논 카메라 : " + num + "대");
				foreach (EosCamera eosCamera in list)
				{
					if (ConnectedDevices.OfType<CanonSDKBase>().All((CanonSDKBase camera) => camera.PortName != eosCamera.PortName))
					{
						CanonSDKBase canonSDKBase = new CanonSDKBase();
						Log.Debug("||||||||||||||||||||||    카메라 연결    |||||||||||||||||||||||||||");
						DeviceDescriptor deviceDescriptor = new DeviceDescriptor
						{
							EosCamera = eosCamera
						};
						deviceDescriptor.CameraDevice = canonSDKBase;
						canonSDKBase.Init(eosCamera, debugMode);
						if (num > 1)
						{
							Thread.Sleep(500);
						}
						ConnectedDevices.Add(canonSDKBase);
						_deviceEnumerator.Add(deviceDescriptor);
						NewCameraConnected(canonSDKBase);
					}
					num--;
				}
			}
			catch (Exception)
			{
				Log.Error("카메라 초기화 실패했습니다. 카메라 전원 On/Off 시도해 주세요.");
				MessageBox.Show("카메라 초기화 실패했습니다. 카메라 전원 On/Off 시도해 주세요.", "에러", (MessageBoxButton)0, (MessageBoxImage)16);
			}
		}
	}

	private void NewCameraConnected(ICameraDevice cameraDevice)
	{
		StaticHelper.Instance.SystemMessage = "New Camera is connected ! Driver :" + cameraDevice.DeviceName;
		string text;
		string text2;
		string empty;
		if (cameraDevice.PortName != null && cameraDevice.PortName.Substring(0, "\\\\?\\usb".Length).Equals("\\\\?\\usb"))
		{
			text = "";
			text2 = "";
			char[] separator = new char[3] { '#', '&', '_' };
			string[] array = cameraDevice.PortName.Split(separator);
			for (int i = 1; i < array.Length - 1; i++)
			{
				if (array[i].Equals("pid"))
				{
					text2 = array[i + 1];
				}
				else if (array[i].Equals("vid"))
				{
					text = array[i + 1];
				}
			}
			if (!text.Equals("") && !text2.Equals(""))
			{
				Log.Debug("제조사 : " + cameraDevice.Manufacturer);
				Log.Debug("이   름 : " + cameraDevice.DeviceName);
				Log.Debug("시리얼 : " + cameraDevice.SerialNumber);
				empty = string.Empty;
				PropertyValue<long> mode = cameraDevice.Mode;
				long numericValue = mode.NumericValue;
				long num = numericValue;
				if ((ulong)num > 9uL)
				{
					goto IL_01ce;
				}
				switch ((int)num)
				{
				case 0:
					break;
				case 1:
					goto IL_01a1;
				case 2:
					goto IL_01aa;
				case 3:
					goto IL_01b3;
				case 4:
					goto IL_01bc;
				case 9:
					goto IL_01c5;
				default:
					goto IL_01ce;
				}
				empty = "P Mode";
				goto IL_01d7;
			}
		}
		goto IL_0204;
		IL_01aa:
		empty = "Av Mode";
		goto IL_01d7;
		IL_01b3:
		empty = "M Mode";
		goto IL_01d7;
		IL_01a1:
		empty = "Tv Mode";
		goto IL_01d7;
		IL_01bc:
		empty = "Bulb Mode";
		goto IL_01d7;
		IL_0204:
		if (CameraConnected != null)
		{
			CameraConnected(cameraDevice);
		}
		SelectedCameraDevice = cameraDevice;
		cameraDevice.PhotoCaptured += cameraDevice_PhotoCaptured;
		cameraDevice.CameraDisconnected += cameraDevice_CameraDisconnected;
		return;
		IL_01d7:
		Log.Debug("모   드 : " + empty);
		Log.Debug("U-S-B  : VID=" + text + ", PID=" + text2);
		goto IL_0204;
		IL_01c5:
		empty = "Auto";
		goto IL_01d7;
		IL_01ce:
		empty = "Fail";
		goto IL_01d7;
	}

	public IEnumerable<EosCamera> GetEosCameras()
	{
		using EosCameraCollection source = _framework.GetCameraCollection();
		return source.ToArray();
	}

	private void DisconnectCamera(EosCamera device)
	{
		lock (_locker)
		{
			DeviceDescriptor descriptor = _deviceEnumerator.GetByEosCamera(device);
			if (descriptor != null)
			{
				descriptor.CameraDevice.PhotoCaptured -= cameraDevice_PhotoCaptured;
				descriptor.CameraDevice.CameraDisconnected -= cameraDevice_CameraDisconnected;
				StaticHelper.Instance.SystemMessage = "Canon Camera disconnected :" + descriptor.CameraDevice.DeviceName;
				Log.Debug("Name : " + descriptor.CameraDevice.DeviceName + " - 연결 해제");
				((DispatcherObject)Application.Current).Dispatcher.Invoke((Action)(() =>
				{
					ConnectedDevices.Remove(descriptor.CameraDevice);
					descriptor.CameraDevice.Close();
					_deviceEnumerator.Remove(descriptor);
					_deviceEnumerator.RemoveDisconnected();
				}));
			}
		}
		RemoveDisconnected();
		Log.Debug("=========== 캐논 카메라 연결 해제 ==============");
	}

	private void RemoveDisconnected()
	{
		List<ICameraDevice> list = ConnectedDevices.Where((ICameraDevice device) => !device.IsConnected).ToList();
		foreach (ICameraDevice item in list)
		{
			ConnectedDevices.Remove(item);
		}
	}

	public void AddAppFakeCamera()
	{
		AppFakeCameraDevice appFakeCameraDevice = new AppFakeCameraDevice();
		ConnectedDevices.Add(appFakeCameraDevice);
		NewCameraConnected(appFakeCameraDevice);
	}

	public void AddFTPFakeCamera(string ftpFolder)
	{
		FTPFakeCameraDevice fTPFakeCameraDevice = new FTPFakeCameraDevice(ftpFolder);
		ConnectedDevices.Add(fTPFakeCameraDevice);
		NewCameraConnected(fTPFakeCameraDevice);
	}

	public void AddWebcameras(string deviceName)
	{
		try
		{
			Log.Debug("||||||||||||||||||||||   웹캠 연결    |||||||||||||||||||||||||||");
			List<string> list = new List<string>();
			FilterInfoCollection filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
			foreach (FilterInfo item in filterInfoCollection)
			{
				if (item.Name != deviceName)
				{
					continue;
				}
				list.Add(item.MonikerString);
				bool flag = false;
				foreach (ICameraDevice connectedDevice in ConnectedDevices)
				{
					if (connectedDevice is WebCameraDevice webCameraDevice && webCameraDevice.PortName == item.MonikerString)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					WebCameraDevice webCameraDevice2 = new WebCameraDevice();
					webCameraDevice2.Init(item.MonikerString);
					webCameraDevice2.DeviceName = item.Name;
					webCameraDevice2.SerialNumber = item.MonikerString;
					webCameraDevice2.DisplayName = item.Name;
					ConnectedDevices.Add(webCameraDevice2);
					NewCameraConnected(webCameraDevice2);
					Log.Debug("제조사 : " + webCameraDevice2.Manufacturer);
					Log.Debug("이   름 : " + webCameraDevice2.DeviceName);
					Log.Debug("시리얼 : " + webCameraDevice2.SerialNumber);
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error("Unable to connect to a webcamera", exception);
		}
	}

	[HandleProcessCorruptedStateExceptions]
	public void DisconnectCamera(ICameraDevice cameraDevice)
	{
		cameraDevice.PhotoCaptured -= cameraDevice_PhotoCaptured;
		cameraDevice.CameraDisconnected -= cameraDevice_CameraDisconnected;
		((DispatcherObject)Application.Current).Dispatcher.Invoke((Action)(() =>
		{
			ConnectedDevices.Remove(cameraDevice);
			cameraDevice.Close();
			OnCameraDisconnected(cameraDevice);
		}));
		StaticHelper.Instance.SystemMessage = "모바일 또는 웹 카메라 연결 해제 :" + cameraDevice.DeviceName;
		Log.Debug("=========== 모발일 또는 웹 카메라 연결 해제 ==============");
		Log.Debug("Name :" + cameraDevice.DeviceName);
	}

	public void OnCameraDisconnected(ICameraDevice cameraDevice)
	{
		if (CameraDisconnected != null)
		{
			CameraDisconnected(cameraDevice);
		}
	}

	private bool AddWiaCameras()
	{
		bool result = false;
		bool flag = ConnectedDevices.Count == 0;
		int num = 0;
		foreach (IDeviceInfo deviceInfo in ((DeviceManager)Activator.CreateInstance(Marshal.GetTypeFromCLSID(new Guid("E1C5D730-7E97-4D8A-9E42-BBAE87C2059F")))).DeviceInfos)
		{
			Properties properties = deviceInfo.Properties;
			object Index = "Name";
			string text = (dynamic)properties.get_Item(ref Index).Value;
			if (getDeviceDescription(text) != null)
			{
				continue;
			}
			Type nativeDriver = GetNativeDriver(text);
			result = nativeDriver != null;
			if ((deviceInfo.Type != WiaDeviceType.CameraDeviceType && deviceInfo.Type != WiaDeviceType.VideoDeviceType) || !((nativeDriver == null || DisableNativeDrivers) | flag) || num >= 3)
			{
				continue;
			}
			do
			{
				Log.Debug("Wia Camera Found: " + text);
				try
				{
					GetWiaIDevice(deviceInfo);
					num = 4;
					result = true;
				}
				catch (Exception exception)
				{
					Log.Error("Unable to connect to the camera", exception);
					num++;
					if (num < 3)
					{
						Log.Debug("Retrying");
						StaticHelper.Instance.SystemMessage = "Unable to connect to the camera. Retrying";
					}
					else
					{
						StaticHelper.Instance.SystemMessage = "Unable to connect to the camera. Please reconnect your camera !";
					}
					Thread.Sleep(1000);
				}
			}
			while (num < 3);
		}
		return result;
	}

	private void DeviceManager_OnEvent(string eventId, string deviceId, string itemId)
	{
		if (eventId == "{A28BBADE-64B6-11D2-A231-00C04FA31809}")
		{
			if (StartInNewThread)
			{
				Thread thread = new Thread(() =>
				{
					ConnectToCameraExe(retry: true);
				});
				thread.SetApartmentState(ApartmentState.MTA);
				thread.Start();
			}
			else
			{
				ConnectToCameraExe(retry: true);
			}
		}
		else if (eventId == "{143E4E83-6497-11D2-A231-00C04FA31809}")
		{
			DisconnectCamera(deviceId);
		}
	}

	private void DisconnectCamera(string wiaId)
	{
		DeviceDescriptor byWiaId = _deviceEnumerator.GetByWiaId(wiaId);
		if (byWiaId != null)
		{
			byWiaId.CameraDevice.PhotoCaptured -= cameraDevice_PhotoCaptured;
			byWiaId.CameraDevice.CameraDisconnected -= cameraDevice_CameraDisconnected;
			ConnectedDevices.Remove(byWiaId.CameraDevice);
			StaticHelper.Instance.SystemMessage = "Camera disconnected :" + byWiaId.CameraDevice.DeviceName;
			Log.Debug("Name :" + byWiaId.CameraDevice.DeviceName);
			Log.Debug("===========Camera disconnected==============");
			_deviceEnumerator.Remove(byWiaId);
			byWiaId.CameraDevice.Close();
			if (byWiaId.CameraDevice is WiaCameraDevice cameraDevice)
			{
				OnCameraDisconnected(cameraDevice);
			}
			if (PortableDeviceCollection.Instance != null)
			{
				PortableDeviceCollection.Instance.RefreshDevices();
			}
		}
	}

	private void DisconnectCamera(ITransferProtocol device)
	{
		DeviceDescriptor byWpdId = _deviceEnumerator.GetByWpdId(device.DeviceId);
		if (byWpdId != null)
		{
			byWpdId.CameraDevice.PhotoCaptured -= cameraDevice_PhotoCaptured;
			byWpdId.CameraDevice.CameraDisconnected -= cameraDevice_CameraDisconnected;
			StaticHelper.Instance.SystemMessage = "Camera disconnected :" + byWpdId.CameraDevice.DeviceName;
			Log.Debug("Name :" + byWpdId.CameraDevice.DeviceName);
			Log.Debug("===========Camera disconnected==============");
			PortableDeviceCollection.Instance.RemoveDevice(device.DeviceId);
			device.IsConnected = false;
			ConnectedDevices.Remove(byWpdId.CameraDevice);
			byWpdId.CameraDevice.Close();
			_deviceEnumerator.Remove(byWpdId);
			_deviceEnumerator.RemoveDisconnected();
		}
		RemoveDisconnected();
	}

	private void AddGoProCamera(string serialNr)
	{
		bool flag = false;
		foreach (ICameraDevice connectedDevice in ConnectedDevices)
		{
			if (connectedDevice is GoProBaseCamera goProBaseCamera && goProBaseCamera.SerialNumber == serialNr)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			GoProProvider goProProvider = new GoProProvider();
			string address = $"172.2{serialNr.Substring(serialNr.Length - 3, 1)}.1{serialNr.Substring(serialNr.Length - 2, 2)}.51";
			Thread.Sleep(3000);
			AddDevice(goProProvider.Connect(address));
		}
	}

	public void AddDevice(DeviceDescriptor descriptor)
	{
		_deviceEnumerator.RemoveDisconnected();
		ConnectedDevices.Add(descriptor.CameraDevice);
		NewCameraConnected(descriptor.CameraDevice);
		_deviceEnumerator.Add(descriptor);
	}

	private void CreateWqlWatcher(string verb, EventArrivedEventHandler eventHandler)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected Obj, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected Obj, but got Unknown
		ManagementEventWatcher val = new ManagementEventWatcher();
		WqlEventQuery query = new WqlEventQuery("SELECT * FROM " + verb + " WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
		val.EventArrived += eventHandler;
		val.Query = (EventQuery)(object)query;
		val.Start();
	}

	private void watcher_EventArrived(object sender, EventArrivedEventArgs e)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected Obj, but got Unknown
		try
		{
			ManagementBaseObject val = (ManagementBaseObject)e.NewEvent["TargetInstance"];
			if ((string)val.GetPropertyValue("Name") == "GoPro RNDIS Device")
			{
				string text = (string)val.GetPropertyValue("DeviceID");
				string serialNr = text.Substring(text.LastIndexOf('\\')).Replace("\\", "");
				AddGoProCamera(serialNr);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Connection error to GoProCamera", exception);
		}
	}

	private void watcher_EventRemoved(object sender, EventArrivedEventArgs e)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected Obj, but got Unknown
		try
		{
			ManagementBaseObject val = (ManagementBaseObject)e.NewEvent["TargetInstance"];
			if (!((string)val.GetPropertyValue("Name") == "GoPro RNDIS Device"))
			{
				return;
			}
			string text = (string)val.GetPropertyValue("DeviceID");
			string text2 = text.Substring(text.LastIndexOf('\\')).Replace("\\", "");
			ICameraDevice cameraDevice = null;
			foreach (ICameraDevice connectedDevice in ConnectedDevices)
			{
				if (connectedDevice is GoProBaseCamera goProBaseCamera && goProBaseCamera.SerialNumber == text2)
				{
					cameraDevice = connectedDevice;
				}
			}
			if (cameraDevice != null)
			{
				DisconnectCamera(cameraDevice);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Connection error to GoProCamera", exception);
		}
	}

	private DeviceDescription getDeviceDescription(string model)
	{
		return _deviceDescriptions.FirstOrDefault((DeviceDescription description) => description.Model == model);
	}

	private ICameraDevice GetWiaIDevice(IDeviceInfo devInfo)
	{
		if (_deviceEnumerator.GetByWiaId(devInfo.DeviceID) != null)
		{
			return _deviceEnumerator.GetByWiaId(devInfo.DeviceID).CameraDevice;
		}
		_deviceEnumerator.RemoveDisconnected();
		DeviceDescriptor deviceDescriptor = new DeviceDescriptor
		{
			WiaDeviceInfo = devInfo,
			WiaId = devInfo.DeviceID
		};
		ICameraDevice cameraDevice = new WiaCameraDevice();
		bool flag = cameraDevice.Init(deviceDescriptor);
		deviceDescriptor.CameraDevice = cameraDevice;
		_deviceEnumerator.Add(deviceDescriptor);
		ConnectedDevices.Add(cameraDevice);
		if (flag)
		{
			NewCameraConnected(cameraDevice);
		}
		return SelectedCameraDevice;
	}

	public void ConnectToServer(string s, int type)
	{
		if (string.IsNullOrEmpty(s))
		{
			return;
		}
		if (type == 0)
		{
			int result = 15740;
			string ip = s;
			if (s.Contains(":"))
			{
				ip = s.Split(new char[1] { ':' })[0];
				int.TryParse(s.Split(new char[1] { ':' })[1], out result);
			}
			ConnectDevicesPtpIp(ip, result);
		}
		if (type == 1)
		{
			int result2 = 4757;
			string ip2 = s;
			if (s.Contains(":"))
			{
				ip2 = s.Split(new char[1] { ':' })[0];
				int.TryParse(s.Split(new char[1] { ':' })[1], out result2);
			}
			ConnectDevicesDDServer(ip2, result2);
		}
	}

	public void ConnectDevicesDDServer(string ip, int port)
	{
		if (_connectionInProgress)
		{
			return;
		}
		try
		{
			_connectionInProgress = true;
			_deviceEnumerator.RemoveDisconnected();
			DdClient ddClient = new DdClient();
			if (!ddClient.Open(ip, port))
			{
				throw new Exception("No server was found!");
			}
			List<DdServerDevice> devices = ddClient.GetDevices();
			if (devices.Count == 0)
			{
				throw new Exception("No connected device was found!");
			}
			ddClient.Connect(devices[0]);
			DdServerProtocol ddServerProtocol = new DdServerProtocol(ddClient);
			if (GetNativeDriver(ddServerProtocol.Model) != null)
			{
				DeviceDescriptor deviceDescriptor = new DeviceDescriptor
				{
					WpdId = "ddserver"
				};
				ICameraDevice cameraDevice = (ICameraDevice)Activator.CreateInstance(GetNativeDriver(ddServerProtocol.Model));
				deviceDescriptor.StillImageDevice = ddServerProtocol;
				cameraDevice.Init(deviceDescriptor);
				ConnectedDevices.Add(cameraDevice);
				NewCameraConnected(cameraDevice);
				deviceDescriptor.CameraDevice = cameraDevice;
				_deviceEnumerator.Add(deviceDescriptor);
				return;
			}
			throw new Exception("Not Supported device " + ddServerProtocol.Model);
		}
		finally
		{
			_connectionInProgress = false;
		}
	}

	public void ConnectDevicesPtpIp(string ip, int port)
	{
		if (_connectionInProgress)
		{
			return;
		}
		try
		{
			_connectionInProgress = true;
			_deviceEnumerator.RemoveDisconnected();
			PtpIpClient ptpIpClient = new PtpIpClient();
			if (!ptpIpClient.Open(ip, 15740))
			{
				throw new Exception("No server was found!");
			}
			PtpIpProtocol ptpIpProtocol = new PtpIpProtocol(ptpIpClient);
			ptpIpProtocol.ExecuteWithNoData(4098u, 1u);
			if (GetNativeDriver(ptpIpProtocol.Model) != null)
			{
				DeviceDescriptor deviceDescriptor = new DeviceDescriptor
				{
					WpdId = "ptpip"
				};
				ICameraDevice cameraDevice = (ICameraDevice)Activator.CreateInstance(GetNativeDriver(ptpIpProtocol.Model));
				deviceDescriptor.StillImageDevice = ptpIpProtocol;
				cameraDevice.Init(deviceDescriptor);
				ConnectedDevices.Add(cameraDevice);
				NewCameraConnected(cameraDevice);
				deviceDescriptor.CameraDevice = cameraDevice;
				_deviceEnumerator.Add(deviceDescriptor);
				return;
			}
			throw new Exception("Not Supported device " + ptpIpProtocol.Model);
		}
		finally
		{
			_connectionInProgress = false;
		}
	}

	public static Type GetNativeDriver(string model)
	{
		if (string.IsNullOrEmpty(model))
		{
			return null;
		}
		if (DeviceClass.ContainsKey(model))
		{
			return DeviceClass[model];
		}
		return null;
	}

	public void CloseAll()
	{
		foreach (ICameraDevice item in ConnectedDevices.Where((ICameraDevice connectedDevice) => connectedDevice.IsConnected))
		{
			item.Close();
		}
	}

	public void SelectNextCamera()
	{
		if (ConnectedDevices.Count == 0)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < ConnectedDevices.Count; i++)
		{
			ICameraDevice cameraDevice = ConnectedDevices[i];
			if (cameraDevice == SelectedCameraDevice)
			{
				num = i;
				break;
			}
		}
		num++;
		if (num < ConnectedDevices.Count)
		{
			SelectedCameraDevice = ConnectedDevices[num];
		}
	}

	public void SelectPrevCamera()
	{
		if (ConnectedDevices.Count == 0)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < ConnectedDevices.Count; i++)
		{
			ICameraDevice cameraDevice = ConnectedDevices[i];
			if (cameraDevice == SelectedCameraDevice)
			{
				num = i;
				break;
			}
		}
		num--;
		if (num >= 0)
		{
			SelectedCameraDevice = ConnectedDevices[num];
		}
	}
}
