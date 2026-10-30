using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Canon.Eos.Framework.Eventing;
using Canon.Eos.Framework.Helper;
using Canon.Eos.Framework.Internal;
using Canon.Eos.Framework.Internal.SDK;
using Canon.Eos.Framework.Threading;

namespace Canon.Eos.Framework;

public sealed class EosCamera : EosObject
{
	[Flags]
	private enum SaveLocation
	{
		Camera = 1,
		Host = 2
	}

	private object _lock = new object();

	public Queue<Action> LiveViewqueue = new Queue<Action>();

	private const int WaitTimeoutForNextLiveDownload = 125;

	private const int MaximumCopyrightLengthInBytes = 64;

	private const int MaximumArtistLengthInBytes = 64;

	private const int MaximumOwnerNameLengthInBytes = 32;

	private Edsdk.EdsDeviceInfo _deviceInfo;

	private Edsdk.EdsObjectEventHandler _edsObjectEventHandler;

	private Edsdk.EdsPropertyEventHandler _edsPropertyEventHandler;

	private Edsdk.EdsStateEventHandler _edsStateEventHandler;

	private bool _liveMode;

	private bool _cancelLiveViewRequested;

	private bool _pauseLiveViewRequested;

	public readonly EosImageTransporter _transporter = new EosImageTransporter();

	public new string Artist
	{
		get
		{
			return base.Artist;
		}
		set
		{
			SetPropertyStringData(1048u, value, 64);
		}
	}

	[EosProperty(16u)]
	public long BatteryLevel
	{
		get
		{
			return GetPropertyIntegerData(8u);
		}
		set
		{
			SetPropertyIntegerData(8u, value);
		}
	}

	public EosBatteryQuality BatteryQuality
	{
		get
		{
			return (EosBatteryQuality)GetPropertyIntegerData(16u);
		}
		set
		{
			SetPropertyIntegerData(16u, (long)value);
		}
	}

	public new string Copyright
	{
		get
		{
			return base.Copyright;
		}
		set
		{
			SetPropertyStringData(1049u, value, 64);
		}
	}

	[EosProperty(1284u)]
	public bool DepthOfFieldPreview
	{
		get
		{
			return GetPropertyIntegerData(1284u) != 0;
		}
		set
		{
			SetPropertyIntegerData(1284u, value ? 1 : 0);
		}
	}

	public string DeviceDescription => _deviceInfo.szDeviceDescription;

	[EosProperty(256u)]
	public EosImageQuality ImageQuality
	{
		get
		{
			return EosImageQuality.Create(GetPropertyIntegerData(256u));
		}
		set
		{
			SetPropertyIntegerData(256u, value.ToBitMask());
		}
	}

	public bool IsErrorTolerantMode { get; set; }

	[EosProperty(1281u)]
	public bool IsInLiveViewMode
	{
		get
		{
			return GetPropertyIntegerData(1281u) != 0;
		}
		set
		{
			SetPropertyIntegerData(1281u, value ? 1 : 0);
		}
	}

	public bool IsInHostLiveViewMode => IsInLiveViewMode && LiveViewDevice.HasFlag(EosLiveViewDevice.Host);

	public bool IsSessionOpen { get; private set; }

	[EosProperty(1294u)]
	public EosLiveViewAutoFocus LiveViewAutoFocus
	{
		get
		{
			return (EosLiveViewAutoFocus)GetPropertyIntegerData(1294u);
		}
		set
		{
			SetPropertyIntegerData(1294u, (long)value);
		}
	}

	[EosProperty(1283u)]
	public long LiveViewColorTemperature
	{
		get
		{
			return GetPropertyIntegerData(1283u);
		}
		set
		{
			SetPropertyIntegerData(1283u, value);
		}
	}

	[EosProperty(1280u)]
	public EosLiveViewDevice LiveViewDevice
	{
		get
		{
			return (EosLiveViewDevice)GetPropertyIntegerData(1280u);
		}
		set
		{
			SetPropertyIntegerData(1280u, (long)value);
		}
	}

	[EosProperty(1282u)]
	public EosWhiteBalance LiveViewWhiteBalance
	{
		get
		{
			return (EosWhiteBalance)GetPropertyIntegerData(1282u);
		}
		set
		{
			SetPropertyIntegerData(1282u, (long)value);
		}
	}

	public bool IsLegacy => _deviceInfo.DeviceSubType == 0;

	public bool IsLocked { get; private set; }

	public new string OwnerName
	{
		get
		{
			return base.OwnerName;
		}
		set
		{
			SetPropertyStringData(4u, value, 32);
		}
	}

	public string PortName => _deviceInfo.szPortName;

	public event EventHandler LiveViewStarted;

	public event EventHandler LiveViewStopped;

	public event EventHandler<EosLiveImageEventArgs> LiveViewUpdate;

	public event EventHandler LiveViewPaused;

	public event EventHandler<EosImageEventArgs> PictureTaken;

	public event EventHandler Shutdown;

	public event EventHandler WillShutdown;

	public event EventHandler<EosVolumeInfoEventArgs> VolumeInfoChanged;

	public event EventHandler<EosPropertyEventArgs> PropertyChanged;

	public event EventHandler<EosExceptionEventArgs> Error;

	internal EosCamera(IntPtr camera)
		: base(camera)
	{
		Util.Assert(Edsdk.EdsGetDeviceInfo(Handle, out _deviceInfo), "Failed to get device info.");
	}

	private void ChangePicturesSaveLocation(SaveLocation saveLocation)
	{
		CheckDisposed();
		EnsureOpenSession();
		Util.Assert(Edsdk.EdsSetPropertyData(Handle, 11u, 0, Marshal.SizeOf(typeof(int)), (int)saveLocation), "Failed to set SaveTo location.");
		if (!IsLegacy)
		{
			LockAndExceute(() =>
			{
				Edsdk.EdsCapacity inCapacity = new Edsdk.EdsCapacity
				{
					NumberOfFreeClusters = int.MaxValue,
					BytesPerSector = 4096,
					Reset = 1
				};
				Util.Assert(Edsdk.EdsSetCapacity(Handle, inCapacity), "Failed to set capacity.");
			});
		}
	}

	protected internal override void DisposeUnmanaged()
	{
		if (IsSessionOpen)
		{
			Edsdk.EdsCloseSession(Handle);
		}
		base.DisposeUnmanaged();
	}

	public void EnsureOpenSession()
	{
		CheckDisposed();
		if (!IsSessionOpen)
		{
			Util.Assert(Edsdk.EdsOpenSession(Handle), "Failed to open session.");
			IsSessionOpen = true;
		}
	}

	protected override TResult ExecuteGetter<TResult>(Func<TResult> function)
	{
		try
		{
			if (IsLegacy && !IsLocked)
			{
				return LockAndExceute(function);
			}
			return base.ExecuteGetter(function);
		}
		catch (Exception exception)
		{
			OnError(exception);
			return default;
		}
	}

	protected override void ExecuteSetter(Action action)
	{
		lock (_locker)
		{
			try
			{
				if (IsLegacy && !IsLocked)
				{
					LockAndExceute(action);
				}
				else
				{
					base.ExecuteSetter(action);
				}
			}
			catch (Exception exception)
			{
				OnError(exception);
			}
		}
	}

	public void Lock()
	{
		lock (_locker)
		{
			CheckDisposed();
			if (!IsLocked)
			{
				Util.Assert(Edsdk.EdsSendStatusCommand(Handle, 0u, 0), "Failed to lock camera.");
				IsLocked = true;
			}
		}
	}

	public void DeleteItem(IntPtr itemInfo)
	{
		LockAndExceute(() =>
		{
			Edsdk.EdsDeleteDirectoryItem(itemInfo);
		});
	}

	private void LockAndExceute(Action action)
	{
		Lock();
		try
		{
			action();
		}
		finally
		{
			Unlock();
		}
	}

	private TResult LockAndExceute<TResult>(Func<TResult> function)
	{
		Lock();
		try
		{
			return function();
		}
		finally
		{
			Unlock();
		}
	}

	private void OnError(Exception exception)
	{
		if (!IsErrorTolerantMode)
		{
			throw exception;
		}
		OnError(new EosExceptionEventArgs
		{
			Exception = exception
		});
	}

	private void OnError(EosExceptionEventArgs args)
	{
		if (Error != null)
		{
			Error(this, args);
		}
	}

	public void SetProperty(uint propertyId, long val)
	{
		lock (_locker)
		{
			SendCommand(258u);
			bool flag = false;
			int num = 0;
			while (num <= 10)
			{
				try
				{
					SetPropertyIntegerData(propertyId, val);
				}
				catch (EosPropertyException)
				{
					Thread.Sleep(50);
					flag = true;
					num++;
				}
				if (!flag)
				{
					break;
				}
			}
		}
	}

	public long GetProperty(uint propertyId)
	{
		lock (_locker)
		{
			return GetPropertyIntegerData(propertyId);
		}
	}

	public DateTime GetDate()
	{
		lock (_locker)
		{
			Edsdk.EdsTime propertyStruct = GetPropertyStruct<Edsdk.EdsTime>(6u, Edsdk.EdsDataType.Time);
			return new DateTime(propertyStruct.Year, propertyStruct.Month, propertyStruct.Day, propertyStruct.Hour, propertyStruct.Minute, propertyStruct.Second);
		}
	}

	public void SetDate(DateTime time)
	{
		lock (_locker)
		{
			Edsdk.EdsTime data = new Edsdk.EdsTime
			{
				Day = time.Day,
				Hour = time.Hour,
				Year = time.Year,
				Minute = time.Minute,
				Month = time.Month,
				Second = time.Second
			};
			SetPropertyStruct(6u, data);
		}
	}

	public void SavePicturesToCamera()
	{
		CheckDisposed();
		ChangePicturesSaveLocation(SaveLocation.Camera);
	}

	public void SavePicturesToHost(string pathFolder)
	{
		SavePicturesToHost(pathFolder, SaveLocation.Host);
	}

	public void SavePicturesToHostAndCamera(string pathFolder)
	{
		SavePicturesToHost(pathFolder, SaveLocation.Camera);
	}

	private void SavePicturesToHost(string pathFolder, SaveLocation saveLocation)
	{
		if (string.IsNullOrWhiteSpace(pathFolder))
		{
			throw new ArgumentException("Cannot be null or white space.", "pathFolder");
		}
		CheckDisposed();
		ChangePicturesSaveLocation(saveLocation | SaveLocation.Host);
	}

	public uint SendCommand(uint command, int parameter = 0)
	{
		lock (_locker)
		{
			EnsureOpenSession();
			return Edsdk.EdsSendCommand(Handle, command, parameter);
		}
	}

	public void SetEventHandlers()
	{
		_edsStateEventHandler = HandleStateEvent;
		Util.Assert(Edsdk.EdsSetCameraStateEventHandler(Handle, 768u, _edsStateEventHandler, IntPtr.Zero), "Failed to set state handler.");
		_edsObjectEventHandler = HandleObjectEvent;
		Util.Assert(Edsdk.EdsSetObjectEventHandler(Handle, 512u, _edsObjectEventHandler, IntPtr.Zero), "Failed to set object handler.");
		_edsPropertyEventHandler = HandlePropertyEvent;
		Util.Assert(Edsdk.EdsSetPropertyEventHandler(Handle, 256u, _edsPropertyEventHandler, IntPtr.Zero), "Failed to set object handler.");
	}

	public EosLiveViewAutoFocus StartLiveView()
	{
		if (!IsInLiveViewMode)
		{
			IsInLiveViewMode = true;
		}
		_cancelLiveViewRequested = false;
		_pauseLiveViewRequested = false;
		EosLiveViewDevice liveViewDevice = LiveViewDevice;
		liveViewDevice |= EosLiveViewDevice.Host;
		LiveViewDevice = liveViewDevice;
		return LiveViewAutoFocus;
	}

	public EosLiveViewAutoFocus StartLiveViewCamera()
	{
		if (!IsInLiveViewMode)
		{
			IsInLiveViewMode = true;
		}
		_cancelLiveViewRequested = false;
		_pauseLiveViewRequested = false;
		EosLiveViewDevice liveViewDevice = LiveViewDevice;
		liveViewDevice = liveViewDevice | EosLiveViewDevice.Camera | EosLiveViewDevice.Host;
		LiveViewDevice = liveViewDevice;
		LiveViewAutoFocus = EosLiveViewAutoFocus.LiveMode;
		return LiveViewAutoFocus;
	}

	public EosLiveViewAutoFocus StartLiveView(EosLiveViewAutoFocus autoFocus)
	{
		StartLiveView();
		LiveViewAutoFocus = autoFocus;
		return LiveViewAutoFocus;
	}

	public void StopLiveView()
	{
		LiveViewDevice = EosLiveViewDevice.None;
	}

	public void StartRecord()
	{
		LiveViewqueue.Enqueue(() =>
		{
			StopLiveView();
			SavePicturesToCamera();
			SendCommand(263u);
			StartLiveView();
			SendCommand(258u);
			SetPropertyIntegerData(1296u, 4L);
		});
	}

	public void StopRecord()
	{
		SendCommand(258u);
		SetPropertyIntegerData(1296u, 0L);
		SendCommand(264u);
	}

	public bool IsOldCanon()
	{
		if (string.IsNullOrEmpty(ProductName))
		{
			return false;
		}
		string[] source = new string[11]
		{
			" 1000D", " 40D", " 450D", " 50D", " 400D", " 500D", "Rebel XSi", "Rebel XTi", "Rebel XS", "Rebel T1i",
			"1Ds Mark III"
		};
		return source.Any((string model) => DeviceDescription.ToLower().Contains(model.ToLower()));
	}

	public void TakePicture()
	{
		lock (_locker)
		{
			if (IsOldCanon())
			{
				Util.Assert(SendCommand(0u), "Failed to capture picture with CameraCommand_TakePicture.");
				return;
			}
			if (IsLegacy && !IsLocked)
			{
				LockAndExceute(TakePicture);
				return;
			}
			Util.Assert(SendCommand(4u, 3), "Failed to press fully.");
			Util.Assert(SendCommand(4u), "Failed to release.");
		}
	}

	public void TakePictureNoAf()
	{
		if (IsOldCanon())
		{
			Util.Assert(SendCommand(0u), "Failed to capture picture with CameraCommand_TakePicture.");
			return;
		}
		if (IsLegacy && !IsLocked)
		{
			LockAndExceute(TakePictureNoAf);
			return;
		}
		Util.Assert(SendCommand(4u, 65539), "Failed to take picture no AF.");
		Util.Assert(SendCommand(4u), "Failed to release.");
	}

	public void ResetShutterButton()
	{
		if (IsLegacy && !IsLocked)
		{
			LockAndExceute(ResetShutterButton);
		}
		else
		{
			Util.Assert(SendCommand(4u), "Failed to take picture no AF.");
		}
	}

	public void AutoFocus()
	{
		lock (_locker)
		{
			if (IsLegacy && !IsLocked)
			{
				LockAndExceute(AutoFocus);
				return;
			}
			Util.Assert(SendCommand(4u), "Failed to take picture no AF.");
			Util.Assert(SendCommand(4u, 1), "Failed to take picture no AF.");
		}
	}

	public void BulbStart()
	{
		Util.Assert(SendCommand(4u, 65539), "Failed to start bulb mode");
	}

	public void BulbEnd()
	{
		try
		{
			Util.Assert(SendCommand(4u), "Failed to start bulb mode");
		}
		finally
		{
			if (IsLocked)
			{
				Unlock();
			}
		}
	}

	public void FocusInLiveView(uint focus)
	{
		if (IsLegacy && !IsLocked)
		{
			LockAndExceute(() =>
			{
				FocusInLiveView(focus);
			});
		}
		else
		{
			Util.Assert(SendCommand(259u, (int)focus), "Failed to focus");
		}
	}

	public void FocusModeLiveView(uint focus)
	{
		if (IsLegacy && !IsLocked)
		{
			LockAndExceute(() =>
			{
				FocusModeLiveView(focus);
			});
		}
		else
		{
			Util.Assert(SendCommand(258u, (int)focus), "Failed to focus");
		}
	}

	public void TakePictureInLiveview()
	{
		_pauseLiveViewRequested = true;
	}

	public void ResumeLiveview()
	{
		_pauseLiveViewRequested = false;
	}

	public void PauseLiveview()
	{
		_pauseLiveViewRequested = true;
		Thread.Sleep(150);
	}

	public override string ToString()
	{
		return DeviceDescription ?? string.Empty;
	}

	public void Unlock()
	{
		lock (_locker)
		{
			if (IsLocked)
			{
				Edsdk.EdsSendStatusCommand(Handle, 1u, 0);
				IsLocked = false;
			}
		}
	}

	private void OnLiveViewStarted(EventArgs eventArgs)
	{
		if (LiveViewStarted != null)
		{
			LiveViewStarted(this, eventArgs);
		}
	}

	private void OnLiveViewStopped(EventArgs eventArgs)
	{
		if (LiveViewStopped != null)
		{
			LiveViewStopped(this, eventArgs);
		}
	}

	private void OnLiveViewPaused(EventArgs eventArgs)
	{
		if (LiveViewPaused != null)
		{
			LiveViewPaused(this, eventArgs);
		}
	}

	private void OnLiveViewUpdate(EosLiveImageEventArgs eventArgs)
	{
		if (LiveViewUpdate != null)
		{
			LiveViewUpdate(this, eventArgs);
		}
	}

	public bool DownloadEvf()
	{
		lock (_locker)
		{
			if (_pauseLiveViewRequested)
			{
				return true;
			}
			if ((LiveViewDevice & EosLiveViewDevice.Host) == 0 || _cancelLiveViewRequested)
			{
				return false;
			}
			DownloadEvfInternal();
			if (LiveViewqueue.Count > 0)
			{
				try
				{
					LiveViewqueue.Dequeue()();
				}
				catch (Exception)
				{
				}
			}
			return true;
		}
	}

	public bool DownloadEvfInternal()
	{
		IntPtr outStream = IntPtr.Zero;
		try
		{
			Edsdk.EdsCreateMemoryStream(0uL, out outStream);
			using EosLiveImage eosLiveImage = EosLiveImage.CreateFromStream(outStream);
			Edsdk.EdsDownloadEvfImage(Handle, eosLiveImage.Handle);
			EosConverter eosConverter = new EosConverter();
			OnLiveViewUpdate(new EosLiveImageEventArgs(eosConverter.ConvertImageStreamToBytes(outStream))
			{
				Zoom = eosLiveImage.Zoom,
				ZommBounds = eosLiveImage.ZoomBounds,
				ImagePosition = eosLiveImage.ImagePosition,
				ImageSize = eosLiveImage.Size
			});
		}
		catch (EosException)
		{
		}
		finally
		{
			if (outStream != IntPtr.Zero)
			{
				Edsdk.EdsRelease(outStream);
			}
		}
		return true;
	}

	private void StartDownloadEvfInBackGround()
	{
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.Work(() =>
		{
			while (DownloadEvf())
			{
				if (_pauseLiveViewRequested)
				{
					OnLiveViewPaused(EventArgs.Empty);
					while (_pauseLiveViewRequested)
					{
						Thread.Sleep(125);
					}
				}
				Thread.Sleep(125);
			}
			LiveViewDevice = EosLiveViewDevice.None;
		});
	}

	private void OnPropertyEventPropertyEvfOutputDeviceChanged(uint param, IntPtr context)
	{
		if (!_liveMode && (LiveViewDevice & EosLiveViewDevice.Host) != 0)
		{
			_liveMode = true;
			OnLiveViewStarted(EventArgs.Empty);
		}
		else if (_liveMode && (LiveViewDevice & EosLiveViewDevice.Host) == 0)
		{
			_liveMode = false;
			OnLiveViewStopped(EventArgs.Empty);
		}
	}

	private void OnPropertyEventPropertyChanged(uint propertyId, uint param, IntPtr context)
	{
		EosFramework.LogInstance.Debug("OnPropertyEventPropertyChanged: " + propertyId);
		uint num = propertyId;
		uint num2 = num;
		if (num2 == 1280)
		{
			OnPropertyEventPropertyEvfOutputDeviceChanged(param, context);
		}
		if (PropertyChanged != null)
		{
			PropertyChanged(this, new EosPropertyEventArgs
			{
				PropertyId = propertyId
			});
		}
	}

	private void OnPropertyEventPropertyDescChanged(uint propertyId, uint param, IntPtr context)
	{
		EosFramework.LogInstance.Debug("OnPropertyEventPropertyDescChanged: " + propertyId);
	}

	private uint HandlePropertyEvent(uint propertyEvent, uint propertyId, uint param, IntPtr context)
	{
		EosFramework.LogInstance.Debug("HandlePropertyEvent fired: " + propertyEvent + ", id: " + propertyId);
		switch (propertyEvent)
		{
		case 257u:
			OnPropertyEventPropertyChanged(propertyId, param, context);
			break;
		case 258u:
			OnPropertyEventPropertyDescChanged(propertyId, param, context);
			break;
		}
		return 0u;
	}

	private void OnStateEventShutdown(EventArgs eventArgs)
	{
		if (Shutdown != null)
		{
			Shutdown.BeginInvoke(this, eventArgs, null, null);
		}
	}

	private void OnStateEventWillShutdown(EventArgs eventArgs)
	{
		if (WillShutdown != null)
		{
			WillShutdown.BeginInvoke(this, eventArgs, null, null);
		}
	}

	private uint HandleStateEvent(uint stateEvent, uint param, IntPtr context)
	{
		EosFramework.LogInstance.Debug("HandleStateEvent fired: " + stateEvent);
		switch (stateEvent)
		{
		case 769u:
			OnStateEventShutdown(EventArgs.Empty);
			break;
		case 771u:
			OnStateEventWillShutdown(EventArgs.Empty);
			break;
		}
		return 0u;
	}

	private void OnPictureTaken(EosMemoryImageEventArgs eventArgs)
	{
		Edsdk.EdsDirectoryItemInfo directoryItemInfo = EosImageTransporter.GetDirectoryItemInfo(eventArgs.Pointer);
		eventArgs.FileName = directoryItemInfo.szFileName;
		if (PictureTaken != null)
		{
			Task.Factory.StartNew(() =>
			{
				PictureTaken(this, eventArgs);
			});
		}
	}

	private void OnVolumeInfoChanged(EosVolumeInfoEventArgs eventArgs)
	{
		if (VolumeInfoChanged != null)
		{
			VolumeInfoChanged(this, eventArgs);
		}
	}

	private void OnObjectEventVolumeInfoChanged(IntPtr sender)
	{
		Util.Assert(Edsdk.EdsGetVolumeInfo(sender, out var outVolumeInfo), "Failed to get volume info.");
		OnVolumeInfoChanged(new EosVolumeInfoEventArgs(new EosVolumeInfo
		{
			Access = outVolumeInfo.Access,
			FreeSpaceInBytes = outVolumeInfo.FreeSpaceInBytes,
			MaxCapacityInBytes = outVolumeInfo.MaxCapacity,
			StorageType = outVolumeInfo.StorageType,
			VolumeLabel = outVolumeInfo.szVolumeLabel
		}));
	}

	private void OnObjectEventVolumeUpdateItems(IntPtr sender, IntPtr context)
	{
	}

	private void OnObjectEventFolderUpdateItems(IntPtr sender, IntPtr context)
	{
	}

	private void OnObjectEventDirItemCreated(IntPtr sender, IntPtr context)
	{
		OnPictureTaken(new EosMemoryImageEventArgs(null)
		{
			Pointer = sender
		});
	}

	private void OnObjectEventDirItemRemoved(IntPtr sender, IntPtr context)
	{
	}

	private void OnObjectEventDirItemInfoChanged(IntPtr sender, IntPtr context)
	{
	}

	private void OnObjectEventDirItemContentChanged(IntPtr sender, IntPtr context)
	{
	}

	private void OnObjectEventDirItemRequestTransfer(IntPtr sender)
	{
		OnPictureTaken(new EosMemoryImageEventArgs(null)
		{
			Pointer = sender
		});
	}

	private void OnObjectEventDirItemRequestTransferDt(IntPtr sender, IntPtr context)
	{
	}

	private void OnObjectEventDirItemCancelTransferDt(IntPtr sender, IntPtr context)
	{
	}

	private void OnObjectEventVolumeAdded(IntPtr sender, IntPtr context)
	{
	}

	private void OnObjectEventVolumeRemoved(IntPtr sender, IntPtr context)
	{
	}

	private uint HandleObjectEvent(uint objectEvent, IntPtr sender, IntPtr context)
	{
		try
		{
			EosFramework.LogInstance.Debug("HandleObjectEvent fired: " + objectEvent);
			Console.WriteLine("Canon event {0}", objectEvent);
			switch (objectEvent)
			{
			case 513u:
				Edsdk.EdsRelease(sender);
				break;
			case 514u:
				OnObjectEventVolumeUpdateItems(sender, context);
				Edsdk.EdsRelease(sender);
				break;
			case 515u:
				OnObjectEventFolderUpdateItems(sender, context);
				Edsdk.EdsRelease(sender);
				break;
			case 516u:
				OnObjectEventDirItemCreated(sender, context);
				break;
			case 517u:
				OnObjectEventDirItemRemoved(sender, context);
				Edsdk.EdsRelease(sender);
				break;
			case 518u:
				OnObjectEventDirItemInfoChanged(sender, context);
				Edsdk.EdsRelease(sender);
				break;
			case 519u:
				OnObjectEventDirItemContentChanged(sender, context);
				Edsdk.EdsRelease(sender);
				break;
			case 520u:
				OnObjectEventDirItemRequestTransfer(sender);
				break;
			case 521u:
				OnObjectEventDirItemRequestTransferDt(sender, context);
				Edsdk.EdsRelease(sender);
				break;
			case 522u:
				OnObjectEventDirItemCancelTransferDt(sender, context);
				Edsdk.EdsRelease(sender);
				break;
			case 524u:
				OnObjectEventVolumeAdded(sender, context);
				Edsdk.EdsRelease(sender);
				break;
			case 525u:
				OnObjectEventVolumeRemoved(sender, context);
				Edsdk.EdsRelease(sender);
				break;
			default:
				Edsdk.EdsRelease(sender);
				break;
			}
		}
		catch (Exception ex)
		{
			EosFramework.LogInstance.Error("Handing HandleObjectEvent: {0}", ex);
		}
		finally
		{
		}
		return 0u;
	}
}
