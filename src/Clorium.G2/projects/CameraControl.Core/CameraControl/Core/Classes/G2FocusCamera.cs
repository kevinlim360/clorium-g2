using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CameraControl.Devices;
using CameraControl.Devices.Canon;
using CameraControl.Devices.Classes;
using Canon.Eos.Framework;

namespace CameraControl.Core.Classes;

// MainWindow routes owned events here before its normal photo-processing pipeline.
// The existing manager remains the sole photo-event entry point.
public sealed class G2FocusCamera : IFocusCaptureCamera, IDisposable
{
    private static readonly object RegistryLock = new object();
    private static readonly Dictionary<ICameraDevice, G2FocusCamera> Owners = new Dictionary<ICameraDevice, G2FocusCamera>();
    private readonly object gate = new object();
    private readonly CanonSDKBase device;
    private readonly string folder;
    private TaskCompletionSource<string> pending;
    private bool disposed;
    private int fileNumber;
    private readonly HashSet<object> handled = new HashSet<object>();

    public G2FocusCamera(ICameraDevice camera, string sessionFolder)
    {
        device = camera as CanonSDKBase ?? throw new InvalidOperationException("현재 수퍼포커스는 Canon SDK 카메라만 지원합니다.");
        Validate();
        folder = Path.GetFullPath(sessionFolder);
        lock (RegistryLock)
        {
            if (Owners.ContainsKey(device)) throw new InvalidOperationException("이전 촬영의 다운로드가 아직 끝나지 않았습니다. 완료를 기다리거나 카메라 연결을 다시 확인해 주세요.");
            Owners.Add(device, this);
        }
        device.CameraDisconnected += Disconnected;
    }

    private void Validate()
    {
        if (!device.IsConnected || device.Camera == null) throw new InvalidOperationException("카메라를 연결해 주세요.");
        if (device.IsBusy) throw new InvalidOperationException("카메라가 다른 작업 중입니다.");
        if (!device.Camera.IsInHostLiveViewMode) throw new InvalidOperationException("먼저 G2 라이브뷰를 열고 시작 초점을 맞춰 주세요.");
        var quality = device.Camera.ImageQuality;
        if (quality.PrimaryImageFormat != EosImageFormat.Jpeg || quality.SecondaryImageFormat != EosImageFormat.Unknown)
            throw new InvalidOperationException("카메라 화질을 JPEG 단독으로 설정해 주세요. RAW 및 RAW+JPEG는 아직 지원하지 않습니다.");
    }

    public bool IsConnected => device.IsConnected;
    public static bool IsOwned(ICameraDevice camera)
    {
        if (camera == null) return false;
        lock (RegistryLock) return Owners.ContainsKey(camera);
    }

    public void MoveFocus(FocusDirection direction, FocusAmount amount)
    {
        lock (gate)
        {
            if (disposed) throw new ObjectDisposedException(nameof(G2FocusCamera));
            Validate();
            device.FocusForStack(direction, amount);
        }
    }

    public Task<string> CaptureAsync()
    {
        TaskCompletionSource<string> next;
        lock (gate)
        {
            if (disposed) throw new ObjectDisposedException(nameof(G2FocusCamera));
            Validate();
            if (pending != null) throw new InvalidOperationException("이전 다운로드가 끝나지 않았습니다.");
            device.PrepareFocusStack(); // The live-view AF button can leave EVF AF engaged.
            handled.Clear(); // SDK pointers may be reused for a later, distinct exposure.
            next = pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        try { device.CapturePhotoNoAf(); }
        catch (Exception error)
        {
            // A command can fail after the body accepted an exposure. Keep routing late files.
            next.TrySetException(error);
        }
        return next.Task;
    }

    public static bool TryHandle(PhotoCapturedEventArgs args)
    {
        if (args?.CameraDevice == null) return false;
        G2FocusCamera owner;
        lock (RegistryLock) if (!Owners.TryGetValue(args.CameraDevice, out owner)) return false;
        owner.Download(args);
        return true;
    }

    private void Download(PhotoCapturedEventArgs args)
    {
        TaskCompletionSource<string> completion;
        Exception failure = null;
        string file;
        lock (gate)
        {
            // Duplicate manager notification must never transfer/release one handle twice.
            if (args.Handle != null && !handled.Add(args.Handle)) return;
            completion = pending;
            string ext = Path.GetExtension(args.FileName);
            if (string.IsNullOrEmpty(ext) || ext.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) ext = ".bin";
            file = Path.Combine(folder, (++fileNumber).ToString("D4") + "_" + Guid.NewGuid().ToString("N") + ext);
        }
        try
        {
            Directory.CreateDirectory(folder);
            device.TransferFocusFile(args.Handle, file);
            if (!File.Exists(file) || new System.IO.FileInfo(file).Length == 0) throw new IOException("원본 전송 결과가 비어 있습니다.");
            if (!string.Equals(Path.GetExtension(file), ".jpg", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Path.GetExtension(file), ".jpeg", StringComparison.OrdinalIgnoreCase))
                throw new IOException("JPEG 이외의 원본을 보존했습니다. 카메라 화질을 확인해 주세요.");
            if (completion == null) throw new IOException("예상하지 않은 촬영 이벤트를 보존했습니다.");
        }
        catch (Exception error)
        {
            failure = error;
            Log.Error("G2 수퍼포커스 다운로드 실패: " + file, error);
        }
        finally
        {
            try { device.ReleaseResurce(args.Handle); }
            catch (Exception error) { failure = failure ?? error; }
            device.TransferProgress = 0;
            device.IsBusy = false;
        }
        lock (gate)
        {
            pending = null;
            if (disposed) RemoveOwner();
        }
        if (failure != null) completion?.TrySetException(failure);
        else completion?.TrySetResult(file);
    }

    private void RemoveOwner()
    {
        device.CameraDisconnected -= Disconnected;
        lock (RegistryLock) Owners.Remove(device);
    }

    private void Disconnected(object sender, DisconnectCameraEventArgs args)
    {
        lock (gate)
        {
            pending?.TrySetException(new IOException("카메라 연결이 끊겼습니다. 내려받은 원본은 보존됩니다."));
            // Keep the transfer route for callbacks already in flight. A reconnected device
            // gets a new device instance from the manager and a separate ownership entry.
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            // A cancelled/timed-out exposure still needs its late SDK download callback.
            if (pending == null) RemoveOwner();
        }
    }
}
