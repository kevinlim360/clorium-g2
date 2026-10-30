using System;
using System.IO;
using CameraControl.Devices.Classes;

namespace CameraControl.Devices;

public interface ICameraDevice
{
	bool IsBusy { get; set; }

	bool HaveLiveView { get; set; }

	bool CaptureInSdRam { get; set; }

	PropertyValue<long> FNumber { get; set; }

	PropertyValue<long> IsoNumber { get; set; }

	PropertyValue<long> ShutterSpeed { get; set; }

	PropertyValue<long> WhiteBalance { get; set; }

	PropertyValue<long> Mode { get; set; }

	PropertyValue<long> ExposureCompensation { get; set; }

	PropertyValue<long> CompressionSetting { get; set; }

	PropertyValue<long> ExposureMeteringMode { get; set; }

	PropertyValue<long> FocusMode { get; set; }

	DateTime DateTime { get; set; }

	string PortName { get; set; }

	bool IsConnected { get; set; }

	bool IsChecked { get; set; }

	object AttachedPhotoSession { get; set; }

	string DeviceName { get; set; }

	string Manufacturer { get; set; }

	string SerialNumber { get; set; }

	string DisplayName { get; set; }

	int ExposureStatus { get; set; }

	bool PreventShutDown { get; set; }

	uint TransferProgress { get; set; }

	int Battery { get; set; }

	PropertyValue<long> LiveViewImageZoomRatio { get; set; }

	AsyncObservableCollection<PropertyValue<long>> AdvancedProperties { get; set; }

	AsyncObservableCollection<PropertyValue<long>> Properties { get; set; }

	PropertyValue<long> ColorTemperature { get; set; }

	event PhotoCapturedEventHandler PhotoCaptured;

	event EventHandler CaptureCompleted;

	event CameraDisconnectedEventHandler CameraDisconnected;

	event CameraDeviceManager.CameraConnectedEventHandler CameraInitDone;

	bool GetCapability(CapabilityEnum capabilityEnum);

	bool Init(DeviceDescriptor deviceDescriptor);

	void StartLiveView();

	void StopLiveView();

	string GetLiveViewStream();

	LiveViewData GetLiveViewImage();

	void AutoFocus();

	int Focus(int step);

	void Focus(int x, int y);

	void Focus(FocusDirection direction, FocusAmount amount);

	void CapturePhotoNoAf();

	void CapturePhoto();

	void StartRecordMovie();

	void StopRecordMovie();

	string GetProhibitionCondition(OperationEnum operationEnum);

	bool GetStatus(OperationEnum operationEnum);

	void EndBulbMode();

	void StartBulbMode();

	void LockCamera();

	void UnLockCamera();

	void Close();

	void StartZoom(ZoomDirection direction);

	void StopZoom(ZoomDirection direction);

	void ReleaseResurce(object o);

	void TransferFileThumb(object o, string filename);

	void TransferFile(object o, string filename);

	void TransferFile(object o, Stream stream);

	AsyncObservableCollection<DeviceObject> GetObjects(object storageId, bool loadThumbs);

	void FormatStorage(object storageId);

	bool DeleteObject(DeviceObject deviceObject);

	void SetCameraField(CameraFieldType cameraFieldType, string comment);

	void SetColorTemperature(long val);

	long GetColorTemperature();

	bool IsModeColorTemperature();

	void StopFocus();
}
