using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CameraControl.Devices.Classes;

namespace CameraControl.Core.Classes;

public interface IFocusCaptureCamera
{
    bool IsConnected { get; }
    void MoveFocus(FocusDirection direction, FocusAmount amount);
    // Completes only after the original has been downloaded and its SDK handle released.
    Task<string> CaptureAsync();
}

public sealed class FocusCaptureSequence
{
    public Task<IReadOnlyList<string>> RunRangeAsync(IFocusCaptureCamera camera,
        FocusRangePlan range, int interval, int settleMilliseconds, TimeSpan timeout,
        IProgress<string> progress, CancellationToken token)
        => RunPlannedRangeAsync(camera,range,range.Positions(interval),settleMilliseconds,timeout,progress,token);

    public async Task<IReadOnlyList<string>> RunPlannedRangeAsync(IFocusCaptureCamera camera,
        FocusRangePlan range, IReadOnlyList<int> positions, int settleMilliseconds, TimeSpan timeout,
        IProgress<string> progress, CancellationToken token)
    {
        if (positions == null || positions.Count < 2 || positions.Count > 200 ||
            !range.A.HasValue || !range.B.HasValue)
            throw new ArgumentException("촬영 계획의 A/B 끝점과 컷수를 확인해 주세요.",nameof(positions));
        var targets = new List<int>(positions); // freeze and validate BEFORE motor commands
        if (!((targets[0] == range.A && targets[targets.Count-1] == range.B) || (targets[0] == range.B && targets[targets.Count-1] == range.A)))
            throw new ArgumentException("촬영 계획은 A/B 양 끝을 포함해야 합니다.",nameof(positions));
        int direction = Math.Sign((long)targets[targets.Count-1] - targets[0]);
        for(int i=1;i<targets.Count;i++) if(direction == 0 || Math.Sign((long)targets[i]-targets[i-1]) != direction)
            throw new ArgumentException("촬영 지점은 한 방향으로 진행해야 합니다.",nameof(positions));
        if (settleMilliseconds < 100 || settleMilliseconds > 10000) throw new ArgumentOutOfRangeException(nameof(settleMilliseconds));
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        var files = new List<string>();
        try
        {
            for (int i = 0; i < targets.Count; i++)
            {
                bool moved = false;
                progress?.Report(i == 0 ? "시작 초점 준비" : "초점 이동");
                while (range.Position != targets[i])
                {
                    token.ThrowIfCancellationRequested();
                    if (!camera.IsConnected) throw new InvalidOperationException("카메라 연결이 끊겼습니다.");
                    int step = Math.Sign((long)targets[i] - range.Position);
                    camera.MoveFocus(step > 0 ? FocusDirection.Far : FocusDirection.Near, FocusAmount.Small);
                    range.Moved(step);
                    moved = true;
                    await Task.Delay(100, token).ConfigureAwait(false);
                }
                if (moved) await Task.Delay(settleMilliseconds, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (!camera.IsConnected) throw new InvalidOperationException("카메라 연결이 끊겼습니다.");
                progress?.Report(string.Format("A–B 촬영 및 다운로드 {0}/{1}", i + 1, targets.Count));
                Task<string> download = camera.CaptureAsync();
                using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    if (await Task.WhenAny(download, Task.Delay(timeout, deadline.Token)).ConfigureAwait(false) != download)
                    {
                        _ = download.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                        token.ThrowIfCancellationRequested();
                        throw new TimeoutException("다운로드 대기 시간이 초과됐습니다. 원본은 보존합니다.");
                    }
                    deadline.Cancel();
                    string file = await download.ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    if (!File.Exists(file) || new System.IO.FileInfo(file).Length == 0 || files.Contains(file))
                        throw new IOException("누락되거나 중복된 촬영 원본입니다.");
                    files.Add(file);
                }
            }
            return files.AsReadOnly();
        }
        catch { range.Invalidate(); throw; } // an interrupted drive no longer proves endpoint positions
    }

    public async Task<IReadOnlyList<string>> RunAsync(IFocusCaptureCamera camera, int count,
        FocusDirection direction, FocusAmount amount, int settleMilliseconds,
        TimeSpan downloadTimeout, IProgress<string> progress, CancellationToken token)
    {
        if (count < 2 || count > 200) throw new ArgumentOutOfRangeException(nameof(count));
        if (settleMilliseconds < 100 || settleMilliseconds > 10000) throw new ArgumentOutOfRangeException(nameof(settleMilliseconds));
        if (downloadTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(downloadTimeout));
        if (!Enum.IsDefined(typeof(FocusDirection), direction) || !Enum.IsDefined(typeof(FocusAmount), amount))
            throw new ArgumentException("Invalid focus selection");
        var files = new List<string>();
        for (int i = 0; i < count; i++)
        {
            token.ThrowIfCancellationRequested();
            if (!camera.IsConnected) throw new InvalidOperationException("카메라 연결이 끊겼습니다.");
            if (i > 0)
            {
                camera.MoveFocus(direction, amount);
                await Task.Delay(settleMilliseconds, token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            if (!camera.IsConnected) throw new InvalidOperationException("카메라 연결이 끊겼습니다.");
            progress?.Report(string.Format("촬영 및 다운로드 {0}/{1}", i + 1, count));
            Task<string> download = camera.CaptureAsync();
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                Task timer = Task.Delay(downloadTimeout, deadline.Token);
                if (await Task.WhenAny(download, timer).ConfigureAwait(false) != download)
                {
                    // Observe a later transfer error while the adapter retains the original.
                    _ = download.ContinueWith(t => { var ignored = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                    token.ThrowIfCancellationRequested();
                    throw new TimeoutException("다운로드 대기 시간이 초과됐습니다. 늦게 도착하는 원본은 세션 폴더에 보존합니다.");
                }
                deadline.Cancel();
                string file = await download.ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                if (!File.Exists(file) || new System.IO.FileInfo(file).Length == 0)
                    throw new IOException("다운로드된 원본 파일을 확인할 수 없습니다.");
                if (files.Contains(file)) throw new IOException("중복 다운로드가 감지됐습니다.");
                files.Add(file);
            }
        }
        return files.AsReadOnly();
    }
}
