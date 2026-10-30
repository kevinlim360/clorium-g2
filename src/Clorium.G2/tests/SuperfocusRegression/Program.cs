using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CameraControl.Core.Classes;
using CameraControl.Devices.Classes;

class Camera : IFocusCaptureCamera
{
    public bool IsConnected { get; set; } = true;
    public int Captures, Moves;
    public bool FailMove, FailDownload;
    public TaskCompletionSource<string> Pending;
    public void MoveFocus(FocusDirection direction, FocusAmount amount) { Moves++; if (FailMove) throw new IOException("lens"); }
    public Task<string> CaptureAsync()
    {
        Captures++;
        Pending = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (FailDownload) Pending.SetException(new IOException("download"));
        return Pending.Task;
    }
}

class Program
{
    static Task<IReadOnlyList<string>> Run(Camera camera, CancellationToken token = default, int timeout = 2000)
        => new FocusCaptureSequence().RunAsync(camera, 2, FocusDirection.Far, FocusAmount.Small, 100,
            TimeSpan.FromMilliseconds(timeout), null, token);
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static async Task Expect<T>(Task task) where T : Exception
    { try { await task; } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    static async Task Until(Func<bool> ready) { for (int i = 0; i < 100 && !ready(); i++) await Task.Delay(10); Check(ready(), "wait failed"); }
    static async Task Main()
    {
        int boxPosition = 0;
        var search = new FocusBoxSearch();
        int peak = await search.FindFarPeakAsync(() => Task.FromResult(2 + 100 * Math.Exp(-Math.Pow((boxPosition-20)/8.0,2))),
            step => { boxPosition += step; return Task.CompletedTask; },100,CancellationToken.None);
        Check(Math.Abs(peak-20)<=1 && boxPosition==peak,"box search did not return to measured peak");
        boxPosition=0;
        await Expect<InvalidOperationException>(search.FindFarPeakAsync(() => Task.FromResult(20.0),step=>{ boxPosition+=step;return Task.CompletedTask;},40,CancellationToken.None));
        Check(boxPosition==40,"flat target must fail bounded search");
        boxPosition=0;
        peak=await search.FindFarPeakAsync(()=>Task.FromResult(2+Math.Min(boxPosition,20)*5.0),step=>{boxPosition+=step;return Task.CompletedTask;},100,CancellationToken.None);
        Check(peak==20 && boxPosition==20,"plateau did not return to first full sharpness");
        using(var boxStop=new CancellationTokenSource()) {
            boxPosition=0;
            await Expect<OperationCanceledException>(search.FindFarPeakAsync(()=>Task.FromResult(2+boxPosition*2.0),step=>{boxPosition+=step;if(boxPosition==5)boxStop.Cancel();return Task.CompletedTask;},100,boxStop.Token));
            Check(boxPosition==5,"cancelled box search continued moving");
        }
        Console.WriteLine("PASS focus-box peak return, flat rejection, plateau, cancellation");
        foreach (int target in new[]{-20,20}) {
            boxPosition=0;
            peak=await search.FindAroundPeakAsync(()=>Task.FromResult(2+100*Math.Exp(-Math.Pow((boxPosition-target)/8.0,2))),step=>{boxPosition+=step;return Task.CompletedTask;},100,CancellationToken.None);
            Check(Math.Abs(peak-target)<=1 && boxPosition==peak,"A-box bidirectional peak search failed");
        }
        Console.WriteLine("PASS A-box focus search from either side of the target");
        var range = new FocusRangePlan(); range.MarkA(); range.Moved(5); range.MarkB();
        Check(string.Join(",",range.Positions(2)) == "0,2,4,5", "A/B inclusive remainder");
        Check(range.Positions(1).Count == 6, "automatic shot count");
        range.Reset(); range.Moved(-10); range.MarkA(); range.Moved(5); range.MarkB();
        Check(string.Join(",",range.Positions(4)) == "-10,-6,-5", "negative positions");
        range.Reset(); range.MarkA(); range.MarkB();
        try { range.Positions(1); throw new Exception("zero range accepted"); } catch (InvalidOperationException) {}
        range.Moved(200); range.MarkB();
        try { range.Positions(1); throw new Exception("silent count truncation"); } catch (InvalidOperationException) {}
        Check(range.Positions(2).Count == 101, "larger interval count");
        Console.WriteLine("PASS A/B count, endpoints, remainder, negative origin, invalid range, 200-frame limit");
        range.Reset(); range.MarkA(); range.Moved(72); range.MarkB();
        Check(range.QualityCount(0)==25 && range.QualityCount(1)==17 && range.QualityCount(2)==8,"quality fractions must use high cut count");
        foreach(int quality in new[]{0,1,2}) {
            var planned=range.PositionsForQuality(quality);
            Check(planned.Count==range.QualityCount(quality) && planned[0]==0 && planned[planned.Count-1]==72,"quality endpoints/count");
            int smallest=int.MaxValue,largest=0;
            for(int i=1;i<planned.Count;i++) { int gap=planned[i]-planned[i-1]; Check(gap>0,"duplicate quality cut"); smallest=Math.Min(smallest,gap);largest=Math.Max(largest,gap); }
            Check(largest-smallest<=1,"uneven quality spacing");
        }
        range.Reset(); range.MarkA(); range.Moved(1); range.MarkB();
        Check(range.PositionsForQuality(2).Count==2,"low quality must keep both endpoints");
        range.Reset();range.Moved(-10);range.MarkA();range.Moved(10);range.MarkB();
        Check(string.Join(",",range.PositionsForQuality(1))=="-10,-5,0","negative origin quality distribution");
        range.Reset();range.MarkA();range.Moved(1200);range.MarkB();
        try{range.PositionsForQuality(0);throw new Exception("high silently capped");}catch(InvalidOperationException){}
        Check(range.PositionsForQuality(2).Count==134,"low plan should remain available when high exceeds limit");
        Console.WriteLine("PASS quality ratios, rounding, inclusive endpoints, even gaps, negative origin, minimum and limit");
        range.Reset();range.MarkA();range.Moved(10);range.MarkB();
        Check(string.Join(",",range.PositionsForQuality(0))=="0,3,6,9,10","three-unit spacing and remainder endpoint");
        Check(range.QualityCount(0)==5 && range.QualityCount(1)==3 && range.QualityCount(2)==2,"reduced quality ratios");
        Check(string.Join(",",range.CapturePositionsForCount(4))=="10,6,3,0","custom count includes both ends in reverse");
        range.Moved(-10);
        Check(string.Join(",",range.CapturePositionsForCount(4))=="0,3,6,10","custom count starts at nearest end");
        foreach(int invalidCount in new[]{-1,0,1,12,201}) {
            try{range.CapturePositionsForCount(invalidCount);throw new Exception("invalid custom count accepted");}catch(InvalidOperationException){}
        }
        range.Reset();range.MarkB();range.Moved(-10);range.MarkA();
        Check(string.Join(",",range.PositionsForQuality(0))=="-10,-7,-4,-1,0","B-first three-unit plan");
        Console.WriteLine("PASS three-unit presets, remainder, custom count, nearest direction and invalid counts");
        range.Reset();range.MarkA();range.Moved(72);range.MarkB();
        Check(range.MaximumUserCuts==25,"maximum user cuts must use high quality");
        for(int entered=2;entered<=25;entered++) {
            var userPlan=range.CapturePositionsForUserCount(entered);
            Check(userPlan.Count==entered && userPlan[0]==72 && userPlan[userPlan.Count-1]==0,"every permitted typed count must remain exact");
        }
        foreach(int invalidCount in new[]{-1,0,1,26,200,201}) {
            try{range.CapturePositionsForUserCount(invalidCount);throw new Exception("above-high count accepted");}catch(InvalidOperationException){}
        }
        range.Reset();range.MarkB();range.Moved(-1200);range.MarkA();
        Check(range.MaximumUserCuts==200 && range.CapturePositionsForUserCount(200).Count==200,"large high count must keep existing200 cap");
        Console.WriteLine("PASS editable count from2 through high maximum, inclusive exact counts and200 cap");
        string folder = Path.Combine(AppContext.BaseDirectory, "fixtures", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        string first = Path.Combine(folder, "1.jpg"), second = Path.Combine(folder, "2.jpg");
        File.WriteAllText(first, "first-original"); File.WriteAllText(second, "second-original");
        var c = new Camera(); var task = Run(c);
        await Task.Delay(150); Check(c.Captures == 1 && c.Moves == 0, "moved before download");
        c.Pending.SetResult(first); await Until(() => c.Captures == 2); Check(c.Moves == 1, "move count");
        c.Pending.SetResult(second); var files = await task;
        Check(files.Count == 2 && files[0] == first && files[1] == second, "ordered originals");
        Console.WriteLine("PASS download gate + ordered sequence");
        c = new Camera(); using (var cancel = new CancellationTokenSource()) {
            task = Run(c, cancel.Token); cancel.Cancel(); await Expect<OperationCanceledException>(task);
            c.Pending.SetResult(first); await Task.Delay(150); Check(c.Captures == 1 && c.Moves == 0, "capture after cancellation");
        } Console.WriteLine("PASS cancel during download + late original");
        c = new Camera(); task = Run(c, timeout: 40); await Expect<TimeoutException>(task);
        c.Pending.SetResult(first); await Task.Delay(150); Check(c.Captures == 1 && c.Moves == 0, "capture after timeout");
        Console.WriteLine("PASS timeout + no continuation");
        c = new Camera { FailDownload = true }; await Expect<IOException>(Run(c)); Check(c.Captures == 1, "download failure continued");
        Console.WriteLine("PASS download failure");
        c = new Camera { FailMove = true }; task = Run(c); c.Pending.SetResult(first); await Expect<IOException>(task); Check(c.Captures == 1, "focus failure continued");
        Console.WriteLine("PASS focus failure");
        c = new Camera(); task = Run(c); c.IsConnected = false; c.Pending.SetResult(first); await Expect<InvalidOperationException>(task); Check(c.Captures == 1 && c.Moves == 0, "disconnect continued");
        Console.WriteLine("PASS disconnect");
        c = new Camera(); task = Run(c); c.Pending.SetResult(Path.Combine(folder, "absent.jpg")); await Expect<IOException>(task);
        Console.WriteLine("PASS missing download");
        c = new Camera(); task = Run(c); c.Pending.SetResult(first); await Until(() => c.Captures == 2); c.Pending.SetResult(first); await Expect<IOException>(task);
        Check(File.ReadAllText(first) == "first-original" && File.ReadAllText(second) == "second-original", "original modified");
        Console.WriteLine("PASS duplicate file + original preservation");
        range.Reset(); range.MarkA(); range.Moved(5); range.MarkB();
        var rc = new RangeCamera(folder);
        var ranged = await new FocusCaptureSequence().RunRangeAsync(rc,range,2,100,TimeSpan.FromSeconds(2),null,CancellationToken.None);
        Check(string.Join(",",rc.CapturedPositions) == "-5,-3,-1,0", "return A then exact B traversal");
        Check(range.Position == 5 && ranged.Count == 4, "finish at B/count");
        range.Moved(1); rc.FailMove = true;
        await Expect<IOException>(new FocusCaptureSequence().RunRangeAsync(rc,range,2,100,TimeSpan.FromSeconds(2),null,CancellationToken.None));
        Check(!range.A.HasValue && !range.B.HasValue, "failed drive kept stale endpoints");
        Console.WriteLine("PASS return-to-A, inclusive B capture, failure invalidates endpoints");
        range.Reset();range.MarkA();range.Moved(5);range.MarkB();
        var qualityCamera=new RangeCamera(folder);
        var qualityPlan=range.PositionsForCount(4);
        var qualityFiles=await new FocusCaptureSequence().RunPlannedRangeAsync(qualityCamera,range,qualityPlan,100,TimeSpan.FromSeconds(2),null,CancellationToken.None);
        Check(qualityFiles.Count==4 && string.Join(",",qualityCamera.CapturedPositions)=="-5,-4,-2,0" && range.Position==5,"quality capture used wrong targets");
        c=new Camera();
        await Expect<ArgumentException>(new FocusCaptureSequence().RunPlannedRangeAsync(c,range,new[]{0,2,2,5},100,TimeSpan.FromSeconds(2),null,CancellationToken.None));
        Check(c.Moves==0 && c.Captures==0,"invalid plan moved camera");
        Console.WriteLine("PASS quality plan capture order and preflight rejection");
        range.Reset();range.MarkA();range.Moved(5);range.MarkB();
        var reversePlan=range.CapturePositionsForCount(4);
        Check(string.Join(",",reversePlan)=="5,3,1,0","last B must start B-to-A without rewind");
        var reverseCamera=new RangeCamera(folder);
        var reversedFiles=await new FocusCaptureSequence().RunPlannedRangeAsync(reverseCamera,range,reversePlan,100,TimeSpan.FromSeconds(2),null,CancellationToken.None);
        Check(reversedFiles.Count==4 && string.Join(",",reverseCamera.CapturedPositions)=="0,-2,-4,-5" && range.Position==0,"reverse capture order/endpoint");
        range.Reset();range.MarkB();range.Moved(-5);range.MarkA();
        var forwardPlan=range.CapturePositionsForCount(4);
        Check(string.Join(",",forwardPlan)=="-5,-4,-2,0","B-first then A must start at A");
        var forwardCamera=new RangeCamera(folder);
        await new FocusCaptureSequence().RunPlannedRangeAsync(forwardCamera,range,forwardPlan,100,TimeSpan.FromSeconds(2),null,CancellationToken.None);
        Check(string.Join(",",forwardCamera.CapturedPositions)=="0,1,3,5" && range.Position==0,"B-first capture order");
        range.Reset();range.MarkA();range.Moved(-5);range.MarkB();
        Check(range.QualityCount(0)==3 && string.Join(",",range.CapturePositionsForCount(4))=="-5,-3,-1,0","opposite signed endpoints");
        range.Moved(4);
        Check(range.CapturePositionsForCount(4)[0]==0,"nearest endpoint after manual nudge");
        Console.WriteLine("PASS A-first/B-first, reverse capture, opposite signed endpoints and nearest-start optimization");
        range.Reset();range.MarkA();range.Moved(5);range.MarkB();
        c=new Camera();
        using(var immediateStop=new CancellationTokenSource()) {
            var immediate=new FocusCaptureSequence().RunPlannedRangeAsync(c,range,range.CapturePositionsForQuality(2),10000,TimeSpan.FromSeconds(2),null,immediateStop.Token);
            Check(c.Captures==1 && c.Moves==0,"current endpoint must capture immediately, without initial rewind/settle");
            immediateStop.Cancel();await Expect<OperationCanceledException>(immediate);c.Pending.SetResult(first);
        }
        range.Reset();range.MarkA();range.Moved(5);range.MarkB();c=new Camera();
        await Expect<ArgumentException>(new FocusCaptureSequence().RunPlannedRangeAsync(c,range,new[]{5,1,3,0},100,TimeSpan.FromSeconds(2),null,CancellationToken.None));
        Check(c.Moves==0 && c.Captures==0,"zigzag plan must fail before camera activity");
        Console.WriteLine("PASS immediate endpoint capture and zigzag plan preflight rejection");
        range.Reset(); range.MarkA(); range.Moved(1); range.MarkB();
        c = new Camera(); task = new FocusCaptureSequence().RunRangeAsync(c,range,1,100,TimeSpan.FromSeconds(2),null,CancellationToken.None);
        await Until(() => c.Captures == 1); await Task.Delay(150);
        Check(c.Moves == 1 && range.Position == 0, "range advanced before download");
        c.Pending.SetResult(first); await Until(() => c.Captures == 2); c.Pending.SetResult(second); await task;
        Check(range.Position == 1, "range did not end at B");
        using (var stop = new CancellationTokenSource()) {
            c = new Camera(); task = new FocusCaptureSequence().RunRangeAsync(c,range,1,100,TimeSpan.FromSeconds(2),null,stop.Token);
            await Until(() => c.Captures == 1); stop.Cancel(); await Expect<OperationCanceledException>(task);
            c.Pending.SetResult(first); await Task.Delay(150);
            Check(c.Captures == 1 && !range.A.HasValue, "range cancel kept shooting/stale A");
        }
        Console.WriteLine("PASS A/B download gate and cancellation with late original");
    }
}

class RangeCamera : IFocusCaptureCamera
{
    public bool IsConnected => true;
    public int Position;
    public bool FailMove;
    public List<int> CapturedPositions = new List<int>();
    private readonly string folder;
    public RangeCamera(string path) { folder = path; }
    public void MoveFocus(FocusDirection direction, FocusAmount amount) {
        CheckSmall(amount);
        if (FailMove) throw new IOException("drive failure");
        Position += direction == FocusDirection.Far ? 1 : -1;
    }
    static void CheckSmall(FocusAmount amount) { if (amount != FocusAmount.Small) throw new Exception("mixed step sizes"); }
    public Task<string> CaptureAsync() {
        CapturedPositions.Add(Position);
        string file = Path.Combine(folder,Guid.NewGuid().ToString("N") + ".jpg"); File.WriteAllText(file,"original"); return Task.FromResult(file);
    }
}
