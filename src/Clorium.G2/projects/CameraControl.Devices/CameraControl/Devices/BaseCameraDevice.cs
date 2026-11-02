using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices;

public class BaseCameraDevice : BaseFieldClass, ICameraDevice
{
	protected List<CapabilityEnum> Capabilities = new List<CapabilityEnum>();

	protected object Locker = new object();

	private bool _isBusy;

	private bool _haveLiveView;

	private bool _captureInSdRam;

	private PropertyValue<long> _isoNumber;

	private PropertyValue<long> _shutterSpeed;

	private PropertyValue<long> _mode;

	private PropertyValue<long> _fNumber;

	private PropertyValue<long> _whiteBalance;

	private PropertyValue<long> _exposureCompensation;

	private PropertyValue<long> _compressionSetting;

	private PropertyValue<long> _exposureMeteringMode;

	private PropertyValue<long> _focusMode;

	private DateTime _dateTime;

	private bool _isChecked;

	private object _attachedPhotoSession;

	private string _deviceName;

	private string _manufacturer;

	private string _serialNumber;

	private string _displayName;

	private int _exposureStatus;

	private bool _preventShutDown;

	private bool _isConnected;

	private int _battery;

	private uint _transferProgress;

	private AsyncObservableCollection<PropertyValue<long>> _advancedProperties;

	private AsyncObservableCollection<PropertyValue<long>> _properties;

	private PropertyValue<long> _colorTemperature;

	public string PortName { get; set; }

	public bool IsBusy
	{
		get
		{
			return _isBusy;
		}
		set
		{
			_isBusy = value;
			NotifyPropertyChanged("IsBusy");
		}
	}

	public virtual bool HaveLiveView
	{
		get
		{
			return _haveLiveView;
		}
		set
		{
			_haveLiveView = value;
			NotifyPropertyChanged("HaveLiveView");
		}
	}

	public virtual bool CaptureInSdRam
	{
		get
		{
			return _captureInSdRam;
		}
		set
		{
			_captureInSdRam = value;
			NotifyPropertyChanged("CaptureInSdRam");
		}
	}

	public virtual PropertyValue<long> IsoNumber
	{
		get
		{
			return _isoNumber;
		}
		set
		{
			_isoNumber = value;
			NotifyPropertyChanged("IsoNumber");
		}
	}

	public virtual PropertyValue<long> ShutterSpeed
	{
		get
		{
			return _shutterSpeed;
		}
		set
		{
			_shutterSpeed = value;
			NotifyPropertyChanged("ShutterSpeed");
		}
	}

	public virtual PropertyValue<long> Mode
	{
		get
		{
			return _mode;
		}
		set
		{
			_mode = value;
			NotifyPropertyChanged("Mode");
		}
	}

	public virtual PropertyValue<long> FNumber
	{
		get
		{
			return _fNumber;
		}
		set
		{
			_fNumber = value;
			NotifyPropertyChanged("FNumber");
		}
	}

	public virtual PropertyValue<long> WhiteBalance
	{
		get
		{
			return _whiteBalance;
		}
		set
		{
			_whiteBalance = value;
			NotifyPropertyChanged("WhiteBalance");
		}
	}

	public virtual PropertyValue<long> ExposureCompensation
	{
		get
		{
			return _exposureCompensation;
		}
		set
		{
			_exposureCompensation = value;
			NotifyPropertyChanged("ExposureCompensation");
		}
	}

	public virtual PropertyValue<long> CompressionSetting
	{
		get
		{
			return _compressionSetting;
		}
		set
		{
			_compressionSetting = value;
			NotifyPropertyChanged("CompressionSetting");
		}
	}

	public virtual PropertyValue<long> ExposureMeteringMode
	{
		get
		{
			return _exposureMeteringMode;
		}
		set
		{
			_exposureMeteringMode = value;
			NotifyPropertyChanged("ExposureMeteringMode");
		}
	}

	public virtual PropertyValue<long> FocusMode
	{
		get
		{
			return _focusMode;
		}
		set
		{
			_focusMode = value;
			NotifyPropertyChanged("FocusMode");
		}
	}

	public virtual DateTime DateTime
	{
		get
		{
			return _dateTime;
		}
		set
		{
			_dateTime = value;
			NotifyPropertyChanged("DateTime");
		}
	}

	public virtual bool IsChecked
	{
		get
		{
			return _isChecked;
		}
		set
		{
			_isChecked = value;
			NotifyPropertyChanged("IsChecked");
		}
	}

	public virtual object AttachedPhotoSession
	{
		get
		{
			return _attachedPhotoSession;
		}
		set
		{
			_attachedPhotoSession = value;
			NotifyPropertyChanged("AttachedPhotoSession");
		}
	}

	public virtual string DeviceName
	{
		get
		{
			return _deviceName;
		}
		set
		{
			_deviceName = value;
			NotifyPropertyChanged("DeviceName");
		}
	}

	public virtual string Manufacturer
	{
		get
		{
			return _manufacturer;
		}
		set
		{
			_manufacturer = value;
			NotifyPropertyChanged("Manufacturer");
		}
	}

	public virtual string SerialNumber
	{
		get
		{
			return _serialNumber;
		}
		set
		{
			_serialNumber = value;
			NotifyPropertyChanged("SerialNumber");
		}
	}

	public virtual string DisplayName
	{
		get
		{
			if (string.IsNullOrEmpty(_displayName))
			{
				return DeviceName + " (" + SerialNumber + ")";
			}
			return _displayName;
		}
		set
		{
			_displayName = value;
			NotifyPropertyChanged("DisplayName");
		}
	}

	public virtual int ExposureStatus
	{
		get
		{
			return _exposureStatus;
		}
		set
		{
			_exposureStatus = value;
			NotifyPropertyChanged("ExposureStatus");
		}
	}

	public bool PreventShutDown
	{
		get
		{
			return _preventShutDown;
		}
		set
		{
			_preventShutDown = value;
			NotifyPropertyChanged("PreventShutDown");
		}
	}

	public virtual bool IsConnected
	{
		get
		{
			return _isConnected;
		}
		set
		{
			_isConnected = value;
			NotifyPropertyChanged("IsConnected");
		}
	}

	public virtual int Battery
	{
		get
		{
			return _battery;
		}
		set
		{
			_battery = value;
			NotifyPropertyChanged("Battery");
		}
	}

	public uint TransferProgress
	{
		get
		{
			return _transferProgress;
		}
		set
		{
			_transferProgress = value;
			NotifyPropertyChanged("TransferProgress");
		}
	}

	public AsyncObservableCollection<PropertyValue<long>> AdvancedProperties
	{
		get
		{
			return _advancedProperties;
		}
		set
		{
			_advancedProperties = value;
			NotifyPropertyChanged("AdvancedProperties");
		}
	}

	public AsyncObservableCollection<PropertyValue<long>> Properties
	{
		get
		{
			return _properties;
		}
		set
		{
			_properties = value;
			NotifyPropertyChanged("Properties");
		}
	}

	public virtual PropertyValue<long> LiveViewImageZoomRatio { get; set; }

	public virtual PropertyValue<long> ColorTemperature
	{
		get
		{
			return _colorTemperature;
		}
		set
		{
			_colorTemperature = value;
			NotifyPropertyChanged("ColorTemperature");
		}
	}

	public event PhotoCapturedEventHandler PhotoCaptured;

	public event EventHandler CaptureCompleted;

	public event CameraDisconnectedEventHandler CameraDisconnected;

	public event CameraDeviceManager.CameraConnectedEventHandler CameraInitDone;

	public virtual bool GetCapability(CapabilityEnum capabilityEnum)
	{
		return Capabilities.Contains(capabilityEnum);
	}

	public virtual bool Init(DeviceDescriptor deviceDescriptor)
	{
		return true;
	}

	public virtual void StartLiveView()
	{
	}

	public virtual void StopLiveView()
	{
	}

	public virtual string GetLiveViewStream()
	{
		throw new NotImplementedException();
	}

	public virtual LiveViewData GetLiveViewImage()
	{
		return null;
	}

	public virtual void AutoFocus()
	{
	}

	public virtual int Focus(int step)
	{
		return 0;
	}

	public virtual void Focus(int x, int y)
	{
	}

	public virtual void Focus(FocusDirection direction, FocusAmount amount)
	{
		throw new NotImplementedException();
	}

	public virtual void CapturePhotoNoAf()
	{
	}

	public virtual void CapturePhoto()
	{
	}

	public virtual void StartRecordMovie()
	{
		CameraControl.Devices.LegacyCapturePolicy.RequireMovieRecording();

	}

	public virtual void StopRecordMovie()
	{
	}

	public virtual string GetProhibitionCondition(OperationEnum operationEnum)
	{
		return "";
	}

	public virtual bool GetStatus(OperationEnum operationEnum)
	{
		throw new NotImplementedException();
	}

	public virtual void EndBulbMode()
	{
	}

	public virtual void StartBulbMode()
	{
	}

	public virtual void LockCamera()
	{
	}

	public virtual void UnLockCamera()
	{
	}

	public virtual void Close()
	{
	}

	public virtual void StartZoom(ZoomDirection direction)
	{
		throw new NotImplementedException();
	}

	public virtual void StopZoom(ZoomDirection direction)
	{
		throw new NotImplementedException();
	}

	public virtual void ResetDevice()
	{
	}

	public virtual void ReleaseResurce(object o)
	{
	}

	public virtual void TransferFileThumb(object o, string filename)
	{
		TransferFile(o, filename);
	}

	public virtual void ReadDeviceProperties(uint prop)
	{
	}

	public virtual void TransferFile(object o, string filename)
	{
	}

	public virtual void TransferFile(object o, Stream stream)
	{
	}

	public void OnCaptureCompleted(object sender, EventArgs args)
	{
		if (CaptureCompleted != null)
		{
			CaptureCompleted(sender, args);
		}
	}

	public void OnPhotoCapture(object sender, PhotoCapturedEventArgs args)
	{
		if (PhotoCaptured != null)
		{
			PhotoCaptured(sender, args);
		}
	}

	public void OnCameraDisconnected(object sender, DisconnectCameraEventArgs eventHandler)
	{
		if (CameraDisconnected != null)
		{
			CameraDisconnected(sender, eventHandler);
		}
	}

	public void OnCameraInitDone()
	{
		try
		{
			CameraInitDone?.Invoke(this);
		}
		catch (Exception exception)
		{
			Log.Error("OnCameraInitDone", exception);
		}
	}

	public virtual AsyncObservableCollection<DeviceObject> GetObjects(object storageId, bool loadThumbs)
	{
		throw new NotImplementedException();
	}

	public virtual void FormatStorage(object storageId)
	{
		throw new NotImplementedException();
	}

	public virtual bool DeleteObject(DeviceObject deviceObject)
	{
		throw new NotImplementedException();
	}

	public virtual void SetCameraField(CameraFieldType cameraFieldType, string comment)
	{
	}

	public virtual void WaitForReady()
	{
	}

	public BaseCameraDevice()
	{
		IsChecked = true;
		AdvancedProperties = new AsyncObservableCollection<PropertyValue<long>>();
		Properties = new AsyncObservableCollection<PropertyValue<long>>();
		Capabilities = new List<CapabilityEnum>();
	}

	public override string ToString()
	{
		return DisplayName;
	}

	public virtual string ToStringCameraData()
	{
		StringBuilder stringBuilder = new StringBuilder("\n\nPost shot camera data:\n");
		try
		{
			if (PortName != null)
			{
				stringBuilder.AppendFormat("Port: {0}\n", PortName);
			}
			stringBuilder.AppendFormat("    {0}{1}", string.IsNullOrWhiteSpace(Manufacturer) ? "" : ("(" + Manufacturer + ") "), DeviceName);
			stringBuilder.AppendFormat("\n\tSerial number........ {0}", SerialNumber);
			stringBuilder.AppendFormat("\n\tBusy..................{0}", IsBusy ? "Yes" : "No");
			stringBuilder.AppendFormat("\n\tHave LiveView.........{0}", HaveLiveView ? "Yes" : "No");
			stringBuilder.AppendFormat("\n\tCapture SDRam.........{0}", CaptureInSdRam ? "Yes" : "No");
			stringBuilder.AppendFormat("\n\tIs Connected..........{0}", IsConnected ? "Yes" : "No");
			stringBuilder.AppendFormat("\n\tMode..................{0}, {1} focus", Mode?.Value, FocusMode?.Value);
			stringBuilder.AppendFormat("\n\texposure..............{0} f{1} +/-{2}, ISO{3}", new object[4] { ShutterSpeed.Value, FNumber.Value, ExposureCompensation.Value, IsoNumber.Value });
			stringBuilder.AppendFormat("\n\tmetering mode.........{0}", ExposureMeteringMode.Value);
			stringBuilder.AppendFormat("\n\twhite balance.........{0}", WhiteBalance.Value);
			stringBuilder.AppendFormat("\n\tTransfer progress.....{0}%", TransferProgress);
			stringBuilder.AppendFormat("\n\tExposure status.......{0}", ExposureStatus);
			stringBuilder.AppendFormat("\n\tBattery...............{0}%", Battery);
			stringBuilder.AppendFormat("\n\tCompression...........{0}%", CompressionSetting);
			stringBuilder.AppendFormat("\n\tColor Temperature.....{0}%", ColorTemperature);
			if (Capabilities.Count > 0)
			{
				stringBuilder.Append($"\n\tCapabilities ({Capabilities.Count} present of {Enum.GetValues(typeof(CapabilityEnum)).Length}):");
				foreach (CapabilityEnum value in Enum.GetValues(typeof(CapabilityEnum)))
				{
					stringBuilder.AppendFormat("\n\t  {0,-20}{1}", value.ToString(""), Capabilities.Contains(value) ? "Yes" : "No");
				}
			}
		}
		catch (Exception)
		{
		}
		return stringBuilder.ToString();
	}

	public virtual void SetColorTemperature(long val)
	{
	}

	public virtual long GetColorTemperature()
	{
		return 0L;
	}

	public virtual bool IsModeColorTemperature()
	{
		return false;
	}

	public virtual void StopFocus()
	{
	}
}
