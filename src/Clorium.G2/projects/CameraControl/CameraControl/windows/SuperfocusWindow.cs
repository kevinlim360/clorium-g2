using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
namespace CameraControl.windows;

public sealed class SuperfocusWindow : Window
{
    private static readonly ConcurrentDictionary<ICameraDevice,SuperfocusWindow> Windows = new ConcurrentDictionary<ICameraDevice,SuperfocusWindow>();
    private readonly ICameraDevice camera;
    private readonly FocusRangePlan range = new FocusRangePlan();
    private readonly StackPanel settings = new StackPanel(), advanced = new StackPanel();
    private readonly List<Button> qualityButtons = new List<Button>();
    private readonly List<Button> focusMoveButtons = new List<Button>();
    private bool automaticCuts=true, updatingCuts, registeringAnchor;
    private readonly Queue<int> manualMoves = new Queue<int>();
    private bool manualTakeover,manualDriving;
    private TaskCompletionSource<bool> anchorReleased;
    private Func<IFocusCaptureCamera> focusCameraFactory;
    private Action stopNativeAutofocus;
    private bool? activeAnchor, pendingAnchor;
    private FocusBoxContrast pendingBox,previewBox;
    private readonly CheckBox boxMode = new CheckBox { Content="자동 초점 찾기", IsChecked=false, Foreground=Brushes.White, VerticalAlignment=VerticalAlignment.Center, Margin=new Thickness(4,2,0,6) };
    // Lens profile: lenses without an AF/MF switch (RF24-240) keep the body in AF and find 가까운 곳/먼 곳 by themselves.
    private TextBlock noticeText;
    private bool? lensAuto;
    public string LensName {get;private set;}="";
    public static bool FindsFocusAutomatically(string lens)=>!string.IsNullOrEmpty(lens)&&lens.IndexOf("24-240",StringComparison.OrdinalIgnoreCase)>=0;
    const string MfNotice="렌즈를 MF(초점 수동 조절)로 설정해 주세요.";
    const string AutoNotice="이 렌즈는 AF/MF 스위치가 없습니다. 카메라의 포커스 모드를 AF로 두세요. 가까운 물체에 박스를 놓고 ‘박스에 초점 맞추기’ → ‘가까운 곳’, 먼 물체도 같은 방법으로 → ‘먼 곳’.";
    private readonly TextBox cuts = new TextBox { Width=42, Height=24, Margin=new Thickness(4,0,8,0), Padding=new Thickness(3), Background=Brushes.White, Foreground=Brushes.Black, VerticalContentAlignment=VerticalAlignment.Center };
    private readonly TextBox settle = new TextBox { Text="700" }, python = new TextBox(), engine = new TextBox();
    private readonly TextBlock status = new TextBlock { Foreground=Brushes.White, FontSize=12, TextTrimming=TextTrimming.CharacterEllipsis, Margin=new Thickness(2,4,2,0), Visibility=Visibility.Collapsed };
    private readonly Button markA=Button("가까운 곳","지금 초점을 가까운 곳(A)으로 저장합니다."), markB=Button("먼 곳","지금 초점을 먼 곳(B)으로 저장합니다.");
    private readonly Border mfNotice;
    private readonly Button focusHere=Button("박스에 초점 맞추기","라이브뷰에서 고른 박스에 초점을 자동으로 맞춥니다. 맞춘 뒤 ‘가까운 곳’ 또는 ‘먼 곳’을 누르면 그 위치를 저장합니다.");
    private readonly StackPanel autoRow=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,6)};
    private readonly Button start=Button("촬영","현재 초점에서 가까운 끝점부터 반대쪽 끝점까지 촬영하고 합성합니다."), cancel=Button("중지","초점 탐색·촬영·합성을 중지합니다. 원본은 보존됩니다."), result=Button("↗","완료된 합성 이미지를 엽니다.");
    private FocusBoxContrast boxA,boxB;
    private CancellationTokenSource cancellation;
    private bool allowClose;
    private string lastResult;
    public int Quality {get;private set;}
    public bool UseFocusBox {get=>boxMode.IsChecked==true;set=>boxMode.IsChecked=value;}
    public bool IsWorking => cancellation!=null||manualDriving||manualMoves.Count>0;
    public bool CanCapture {get {if(IsWorking)return false;try{CaptureTargets();return true;}catch(InvalidOperationException){return false;}}}
    private IReadOnlyList<int> CaptureTargets(){if(automaticCuts)return range.CapturePositionsForQuality(Quality);if(!int.TryParse(cuts.Text,out int count))throw new InvalidOperationException("컷수는 2부터 "+MaxCutsText()+"까지 입력해 주세요.");return range.CapturePositionsForUserCount(count);}
    private void ShowPresetCuts(){string text="";try{text=range.QualityCount(Quality).ToString();}catch(InvalidOperationException){}if(cuts.Text!=text){updatingCuts=true;try{cuts.Text=text;}finally{updatingCuts=false;}}}
    private void RangeMarked(){Quality=0;automaticCuts=true;Refresh();}
    public string RangeSummary {get;private set;}
    public string Feedback => status.Text;
    public event EventHandler Changed;
    public static SuperfocusWindow For(ICameraDevice camera,Window owner) {
        if(camera==null)throw new InvalidOperationException("카메라를 연결해 주세요.");
        if(!Windows.TryGetValue(camera,out var w)) {w=new SuperfocusWindow(camera){Owner=owner};Windows.TryAdd(camera,w);w.Closed+=(s,e)=>Windows.TryRemove(camera,out var removed);}
        return w;
    }
    public static bool AnyWorking => Windows.Values.Any(w=>w.IsWorking);
    public static void StopAll(){foreach(var w in Windows.Values.ToArray())w.RequestStop();}
    public static void CloseAll(){foreach(var w in Windows.Values.ToArray()){w.allowClose=true;w.Close();}}
    public static void InvalidateFor(ICameraDevice device){if(device!=null&&Windows.TryGetValue(device,out var w))w.InvalidateRange();}
    public static bool IsWorkingFor(ICameraDevice device)=>device!=null&&Windows.TryGetValue(device,out var w)&&w.IsWorking;
    public static bool TrySelectPreviewBox(ICameraDevice device,LiveViewData frame,int x,int y){
        if(device==null||!Windows.TryGetValue(device,out var w))return false;
        // While the window is open a click only moves the box on screen; the lens moves only with the arrow buttons.
        return w.Dispatcher.Invoke(()=>{
            if(!w.IsVisible){w.previewBox=null;return false;}
            try{w.previewBox=FocusBoxContrast.At(frame,x,y);}catch(InvalidOperationException){}
            return true;
        });
    }
    public static FocusBoxContrast PreviewBoxFor(ICameraDevice device)=>device!=null&&Windows.TryGetValue(device,out var w)?w.previewBox:null;
    // Live view (+)/(−) while the window is open: (+) magnifies the chosen box, (−) returns to the whole picture.
    public static FocusBoxContrast ZoomBoxFor(ICameraDevice device)=>device!=null&&Windows.TryGetValue(device,out var w)&&w.Dispatcher.Invoke(()=>w.IsVisible)?w.previewBox:null;
    public static bool IsOpenFor(ICameraDevice device)=>device!=null&&Windows.TryGetValue(device,out var w)&&w.Dispatcher.Invoke(()=>w.IsVisible);
    public static bool TryFocusSelectedBox(ICameraDevice device) {
        if(device==null||!Windows.TryGetValue(device,out var w))return false;
        if(w.IsWorking)return true;
        if(!w.IsVisible)return false;
        // 자동 초점 찾기 with a saved end: AF refines focus on the selected box by stepping (the range stays counted).
        if(w.UseFocusBox){_=w.FocusHereAsync();return true;}
        // Otherwise AF from the live view would move the lens uncounted and clear the saved range.
        w.SetStatus("수퍼포커스 중에는 화살표로 초점을 옮겨 주세요.");return true;
    }
    public void RequestStop(){pendingAnchor=null;pendingBox=null;manualMoves.Clear();manualTakeover=false;cancellation?.Cancel();}
    public Task StartCaptureAsync()=>RunAsync();
    public void SelectQuality(int value){if(IsWorking&&!registeringAnchor)return;if(value<0||value>2)throw new ArgumentOutOfRangeException(nameof(value));Quality=value;automaticCuts=true;Refresh();}
    public string QualityDescription(int value){try{return range.QualityDescription(value);}catch(InvalidOperationException){return (value==0?"고":value==1?"중":"저")+" · —";}}
    private SuperfocusWindow(ICameraDevice device) {
        focusCameraFactory=()=>new G2FocusCamera(camera,ServiceProvider.Settings.DefaultSession.Folder);
        stopNativeAutofocus=()=>((CameraControl.Devices.Canon.CanonSDKBase)camera).PrepareFocusStack();
        camera=device;Title="수퍼포커스";Width=440;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStyle=WindowStyle.ToolWindow;ShowInTaskbar=false;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=new SolidColorBrush(Color.FromRgb(36,44,56));
        var panel=new StackPanel {Margin=new Thickness(10)};
        // First thing in the box: the lens must be in MF. Shown again every time the window opens.
        var ok=Button("확인","안내를 닫습니다.");ok.MinHeight=26;ok.Padding=new Thickness(10,2,10,2);ok.Margin=new Thickness(8,0,0,0);DockPanel.SetDock(ok,Dock.Right);
        var noticeRow=new DockPanel();noticeRow.Children.Add(ok);
        noticeText=new TextBlock {Text=MfNotice,Foreground=Brushes.Black,FontWeight=FontWeights.SemiBold,FontSize=13,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center};noticeRow.Children.Add(noticeText);
        mfNotice=new Border {Background=new SolidColorBrush(Color.FromRgb(255,200,40)),CornerRadius=new CornerRadius(6),Padding=new Thickness(10,7,7,7),Margin=new Thickness(0,0,0,10),Child=noticeRow};
        ok.Click+=(s,e)=>mfNotice.Visibility=Visibility.Collapsed;panel.Children.Add(mfNotice);
        IsVisibleChanged+=(s,e)=>{if(IsVisible){mfNotice.Visibility=Visibility.Visible;DetectLens();}};
        Section(settings,"① 초점 범위","화살표로 초점을 옮기고 ‘가까운 곳’, ‘먼 곳’을 눌러 저장하세요. 박스를 옮겨도 렌즈는 움직이지 않습니다.");
        Tip(boxMode,"켜면 라이브뷰에서 고른 박스에 렌즈가 초점을 찾아 가까운 곳/먼 곳으로 저장합니다(AF/MF 스위치 없는 렌즈, 카메라는 AF). 끄면 화살표로 맞춘 위치를 그대로 저장합니다(렌즈 MF).");
        boxMode.Click+=(s,e)=>Refresh();settings.Children.Add(boxMode);
        focusHere.Width=200;focusHere.FontSize=12;focusHere.Click+=async(s,e)=>await FocusHereAsync();autoRow.Children.Add(focusHere);
        var focusRow=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
        markA.Width=74;markB.Width=74;markA.FontSize=12;markB.FontSize=12;markA.Padding=markB.Padding=new Thickness(2,4,2,4);
        markA.Click+=async(s,e)=>await MarkSelectedAsync(true);markB.Click+=async(s,e)=>await MarkSelectedAsync(false);
        foreach(int step in new[]{-20,-5,-1})AddNudge(focusRow,step);
        focusRow.Children.Add(markA);
        foreach(int step in new[]{1,5,20})AddNudge(focusRow,step);
        focusRow.Children.Add(markB);
        settings.Children.Add(autoRow);settings.Children.Add(focusRow);
        var directions=new DockPanel {Margin=new Thickness(4,0,4,6)};
        var far=new TextBlock {Text="멀리 ▶",Foreground=Gray,FontSize=11};DockPanel.SetDock(far,Dock.Right);directions.Children.Add(far);
        directions.Children.Add(new TextBlock {Text="◀ 가까이",Foreground=Gray,FontSize=11});settings.Children.Add(directions);
        Section(settings,"② 품질","고 · 중 · 저를 고르거나 컷수를 직접 입력하세요.");
        var qualityRow=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
        qualityRow.Children.Add(new TextBlock {Text="품질",Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,4,0)});
        for(int i=0;i<3;i++){int choice=i;var b=Button("","합성 품질을 선택합니다.");b.Width=40;b.Padding=new Thickness(3,4,3,4);b.Click+=(s,e)=>SelectQuality(choice);qualityButtons.Add(b);qualityRow.Children.Add(b);}
        qualityRow.Children.Add(new TextBlock {Text="컷수",Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(12,0,0,0)});
        Tip(cuts,"직접 촬영할 컷수(2–200)를 입력합니다. 비우거나 품질 버튼을 누르면 자동 계산합니다. A/B 양 끝을 포함해 균등 배치합니다.");
        cuts.IsReadOnly=false;cuts.Focusable=true;cuts.Cursor=System.Windows.Input.Cursors.IBeam;cuts.MaxLength=3;
        cuts.GotKeyboardFocus+=(s,e)=>cuts.Dispatcher.BeginInvoke(new Action(()=>{if(cuts.IsKeyboardFocusWithin)cuts.SelectAll();}),System.Windows.Threading.DispatcherPriority.Input);
        cuts.PreviewTextInput+=(s,e)=>e.Handled=e.Text.Any(c=>c<'0'||c>'9');
        DataObject.AddPastingHandler(cuts,(s,e)=>{var text=e.DataObject.GetData(typeof(string)) as string;if(text==null||text.Any(c=>c<'0'||c>'9'))e.CancelCommand();});
        cuts.TextChanged+=(s,e)=>{if(!updatingCuts){automaticCuts=false;Refresh();}};qualityRow.Children.Add(cuts);
        settings.Children.Add(qualityRow);panel.Children.Add(settings);
        Section(panel,"③ 촬영","가까운 끝점부터 반대쪽 끝점까지 찍고 한 장으로 합성합니다.");
        var captureRow=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
        start.Width=170;start.Background=Brushes.SteelBlue;start.Foreground=Brushes.White;start.FontWeight=FontWeights.SemiBold;start.Click+=async(s,e)=>await RunAsync();
        cancel.Width=56;cancel.Click+=(s,e)=>RequestStop();result.Width=34;result.Click+=(s,e)=>{if(File.Exists(lastResult))System.Diagnostics.Process.Start(lastResult);};
        var reset=Button("↺","가까운 곳·먼 곳을 지웁니다. 렌즈 링·줌·카메라·렌즈를 바꾼 뒤에는 다시 저장하세요.");reset.Width=34;reset.Click+=(s,e)=>InvalidateRange();
        var gear=Button("⚙","안정화 시간과 합성 엔진 경로 설정을 펼칩니다.");gear.Width=34;gear.Click+=(s,e)=>advanced.Visibility=advanced.Visibility==Visibility.Visible?Visibility.Collapsed:Visibility.Visible;
        captureRow.Children.Add(start);captureRow.Children.Add(cancel);captureRow.Children.Add(result);captureRow.Children.Add(reset);captureRow.Children.Add(gear);panel.Children.Add(captureRow);panel.Children.Add(status);
        advanced.Margin=new Thickness(4,8,4,0);advanced.Visibility=Visibility.Collapsed;
        Field(advanced,"안정화 (ms)",settle);Tip(settle,"초점 이동 뒤 기다릴 시간입니다. 100–10000 ms. 현재 끝점에서 바로 시작할 때에는 추가로 기다리지 않습니다.");
        PathField(advanced,"Python",python,"python.exe|python.exe|실행 파일|*.exe");PathField(advanced,"엔진",engine,"Python 파일|*.py");panel.Children.Add(advanced);
        Content=new ScrollViewer {Content=panel,MaxHeight=Math.Max(200,SystemParameters.WorkArea.Height-80),VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        string saved=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"stack-python.txt");python.Text=Environment.GetEnvironmentVariable("CLORIUM_STACK_PYTHON")??"";
        if(string.IsNullOrWhiteSpace(python.Text)&&File.Exists(saved))python.Text=File.ReadAllText(saved).Trim();
        for(var dir=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);dir!=null;dir=dir.Parent){string script=Path.Combine(dir.FullName,"modules","Clorium.Stack","clorium_stack.py");if(File.Exists(script)){engine.Text=script;break;}}
        Closing+=(s,e)=>{if(IsWorking){e.Cancel=true;RequestStop();SetStatus("중지 중");}else if(!allowClose){e.Cancel=true;Hide();}};
        camera.CameraDisconnected+=Disconnected;Closed+=(s,e)=>camera.CameraDisconnected-=Disconnected;Refresh();
    }
    private static Button Button(string label,string hint)=>WithTip(new Button {Content=label,Margin=new Thickness(2),Padding=new Thickness(6,4,6,4),MinHeight=30,FontSize=13,Background=Brushes.LightGray,Foreground=Brushes.Black},hint);
    private static T WithTip<T>(T control,string text) where T:FrameworkElement {Tip(control,text);return control;}
    private static void Tip(FrameworkElement control,string text){control.ToolTip=new ToolTip {Content=new TextBlock {Text=text,TextWrapping=TextWrapping.Wrap,MaxWidth=280},Background=Brushes.White,Foreground=Brushes.Black,Padding=new Thickness(7)};ToolTipService.SetInitialShowDelay(control,300);ToolTipService.SetShowDuration(control,10000);ToolTipService.SetShowOnDisabled(control,true);}
    // Frozen: the live view's worker thread can be the first to touch this class (TrySelectPreviewBox), and an unfrozen
    // brush made there cannot be used by the window (it then opened blank).
    private static readonly Brush Gray=Frozen(new SolidColorBrush(Color.FromRgb(160,172,188)));
    private static Brush Frozen(Brush b){b.Freeze();return b;}
    private static void Section(Panel panel,string title,string hint){panel.Children.Add(new TextBlock {Text=title,Foreground=Brushes.White,FontWeight=FontWeights.SemiBold,FontSize=13,Margin=new Thickness(2,4,2,0)});panel.Children.Add(new TextBlock {Text=hint,Foreground=Gray,FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(2,1,2,4)});}
    private void AddNudge(Panel panel,int step){int value=step;var b=Button(step<0?(step==-20?"◀◀◀":step==-5?"◀◀":"◀"):(step==20?"▶▶▶":step==5?"▶▶":"▶"),(step<0?"가까이 ":"멀리 ")+Math.Abs(step)+"단계 이동");b.Width=36;b.FontSize=11;b.Padding=new Thickness(1);b.Click+=async(s,e)=>await NudgeAsync(value);focusMoveButtons.Add(b);panel.Children.Add(b);}
    private static void Field(Panel panel,string label,Control control){panel.Children.Add(new TextBlock {Text=label,Foreground=Brushes.White,Margin=new Thickness(0,4,0,2)});control.Padding=new Thickness(4);control.Background=Brushes.White;control.Foreground=Brushes.Black;panel.Children.Add(control);}
    private void PathField(Panel panel,string label,TextBox box,string filter){Field(panel,label,box);Tip(box,label+" 파일 경로");var b=Button("…",label+" 파일 선택");b.HorizontalAlignment=HorizontalAlignment.Right;b.Click+=(s,e)=>{var dialog=new Microsoft.Win32.OpenFileDialog {Filter=filter};if(dialog.ShowDialog(this)==true)box.Text=dialog.FileName;};panel.Children.Add(b);}
    private void SetStatus(string text){status.Text=(text??"").Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()??"";status.Visibility=string.IsNullOrEmpty(status.Text)?Visibility.Collapsed:Visibility.Visible;Tip(status,text??"");}
    private void Disconnected(object sender,DisconnectCameraEventArgs e){Dispatcher.BeginInvoke(new Action(()=>{RequestStop();range.Invalidate();boxA=boxB=previewBox=null;SetStatus("연결 끊김");Refresh();}));}
    public void InvalidateRange(){if(IsWorking){RequestStop();return;}needsMeasure=false;range.Reset();boxA=boxB=null;automaticCuts=true;Quality=0;SetStatus("");Refresh();} // the chosen box stays on screen
    public void MarkA(){if(!IsWorking){range.MarkA();RangeMarked();}}
    public void MarkB(){if(!IsWorking){range.MarkB();RangeMarked();}}
    // Lens profile from the body (on each open): RF24-240 turns 자동 초점 찾기 on and shows the AF notice. A user's own choice is
    // kept until another lens is detected.
    private async void DetectLens(){
        if(!(camera is CameraControl.Devices.Canon.CanonSDKBase canon))return;
        string name="";
        try{name=await Task.Run(()=>canon.LensName)??"";}catch(Exception){}
        LensName=name;bool auto=FindsFocusAutomatically(name);
        Log.Debug("Superfocus : lens '"+name+"' -> "+(auto?"자동 초점 찾기":"MF 화살표"));
        if(lensAuto!=auto){lensAuto=auto;if(!IsWorking)UseFocusBox=auto;}
        noticeText.Text=(name.Length>0?name+" · ":"")+(UseFocusBox?AutoNotice:MfNotice);
        Refresh();
    }
    // 자동 초점 찾기 (RF24-240): 박스에 초점 맞추기 is camera AF only (the lens is accurate, a stepped search is not). AF moves the
    // lens an unknown number of steps, so after it the distance to the other saved end is measured when this end is saved.
    public Task MarkSelectedAsync(bool isA)=>UseFocusBox&&needsMeasure&&(isA?boxB:boxA)!=null?MeasureToOtherEndAsync(isA):SaveCurrentAsync(isA);
    private bool needsMeasure;
    public async Task FocusHereAsync(){
        if(IsWorking)return;
        FocusBoxContrast box;
        try{box=previewBox??FocusBoxContrast.From(camera.GetLiveViewImage());}catch(Exception e){SetStatus(e.GetBaseException().Message);return;}
        bool anyEnd=range.A.HasValue||range.B.HasValue;
        using(var token=new CancellationTokenSource()){
            cancellation=token;SetStatus("박스에 초점 맞추는 중");Refresh();Log.Debug("Superfocus : AF on box "+box.X.ToString("0.00")+","+box.Y.ToString("0.00"));
            try{
                await Task.Run(async()=>{
                    var frame=await ReadFrameAsync(token.Token);
                    camera.Focus((int)(box.X*frame.ImageWidth),(int)(box.Y*frame.ImageHeight));await Task.Delay(250,token.Token);
                    camera.AutoFocus();await Task.Delay(1600,token.Token);stopNativeAutofocus();await Task.Delay(200,token.Token);
                });
                if(anyEnd)needsMeasure=true;else{range.Reset();needsMeasure=false;}
                SetStatus("초점 맞춤 · ‘가까운 곳’ 또는 ‘먼 곳’을 눌러 저장하세요");
            }catch(Exception e){
                if(anyEnd)needsMeasure=true;
                Log.Debug("Superfocus : AF on box ended: "+e.GetBaseException().Message);
                SetStatus(e is OperationCanceledException?"중지됨":e.GetBaseException().Message);
            }finally{cancellation=null;Refresh();}
        }
    }
    // Saves this end where AF left the lens, then steps towards the other end (가까운 곳 is nearer, 먼 곳 farther) until that end's
    // box is sharpest again, counting every step. The lens finishes on the other end; both ends are then known.
    private async Task MeasureToOtherEndAsync(bool isA){
        if(IsWorking)return;
        FocusBoxContrast here=previewBox,other=isA?boxB:boxA;
        if(here==null){try{here=FocusBoxContrast.From(camera.GetLiveViewImage());}catch(Exception){here=null;}}
        string name=isA?"가까운 곳":"먼 곳",otherName=isA?"먼 곳":"가까운 곳";
        int direction=isA?1:-1; // towards the other end
        int steps=0;bool saved=false;
        using(var token=new CancellationTokenSource()){
            cancellation=token;SetStatus(name+" 저장 · "+otherName+"까지 거리 재는 중");Refresh();
            try{
                await Task.Run(async()=>{var adapter=focusCameraFactory();using(adapter as IDisposable){
                    range.Reset();if(isA)range.MarkA();else range.MarkB();saved=true;
                    Func<Task<double>> measure=async()=>other.Measure(await ReadFrameAsync(token.Token));
                    double initial=await measure(),best=initial;int bestAt=0,falling=0;bool found=false;
                    Log.Debug("Superfocus : measure "+name+" -> "+otherName+" start score "+initial.ToString("0.0"));
                    for(int i=1;i<=MeasureLimit;i++){
                        token.Token.ThrowIfCancellationRequested();
                        adapter.MoveFocus(direction>0?FocusDirection.Far:FocusDirection.Near,FocusAmount.Small);range.Moved(direction);steps=i;
                        await Task.Delay(120,token.Token);
                        double score=await measure();
                        if(score>best){best=score;bestAt=i;falling=0;}else falling=score<best*.85?falling+1:0;
                        if(i%20==0){int n=i;Dispatcher.BeginInvoke(new Action(()=>SetStatus(name+" 저장 · "+otherName+"까지 거리 재는 중 ("+n+"단계)")));Log.Debug("Superfocus : step "+i+" score "+score.ToString("0.0")+" best "+best.ToString("0.0")+" at "+bestAt);}
                        // The other end's box must clearly sharpen and then soften again.
                        if(best>initial*1.3&&falling>=3){found=true;break;}
                    }
                    if(!found)throw new InvalidOperationException(otherName+" 박스가 다시 선명해지지 않았습니다("+steps+"단계). "+otherName+"을 다시 저장해 주세요.");
                    for(int i=steps;i>bestAt;i--){token.Token.ThrowIfCancellationRequested();adapter.MoveFocus(direction>0?FocusDirection.Near:FocusDirection.Far,FocusAmount.Small);range.Moved(-direction);await Task.Delay(120,token.Token);}
                    if(isA)range.MarkB();else range.MarkA();
                    Log.Debug("Superfocus : "+otherName+" found "+bestAt+" steps from "+name);
                }});
                if(isA)boxA=here;else boxB=here;
                needsMeasure=false;RangeMarked();SetStatus(name+" 저장 · 두 곳 사이 "+Math.Abs((long)range.B.Value-range.A.Value)+"단계");
            }catch(Exception e){
                Log.Debug("Superfocus : measure ended: "+e.GetBaseException().Message);
                // Steps were counted from this end, so it stays valid; the other end has to be saved again.
                needsMeasure=false;
                if(saved){if(isA){boxA=here;range.ClearB();boxB=null;}else{boxB=here;range.ClearA();boxA=null;}}
                RangeMarked();
                SetStatus(e is OperationCanceledException?"중지됨 · "+name+"만 저장했습니다":e.GetBaseException().Message);
            }finally{cancellation=null;Refresh();}
        }
    }
    private const int MeasureLimit=800;
    private async Task<LiveViewData> ReadFrameAsync(CancellationToken token){for(int i=0;i<20;i++){token.ThrowIfCancellationRequested();var frame=camera.GetLiveViewImage();if(frame?.ImageData!=null&&frame.ImageData.Length>0)return frame;await Task.Delay(50,token);}throw new InvalidOperationException("라이브뷰 프레임을 읽지 못했습니다.");}
    // 가까운 곳 / 먼 곳 (렌즈 MF): saves where the arrows left the lens (no search, no AF). The selected preview box is kept with it.
    private Task SaveCurrentAsync(bool isA){
        if(IsWorking)return Task.CompletedTask;
        if((isA&&range.B==range.Position)||(!isA&&range.A==range.Position)){SetStatus("가까운 곳과 먼 곳이 같은 초점입니다. 화살표로 초점을 옮긴 뒤 저장하세요.");return Task.CompletedTask;}
        FocusBoxContrast box=previewBox;
        if(box==null){try{box=FocusBoxContrast.From(camera.GetLiveViewImage());}catch(Exception){box=null;}}
        if(isA){range.MarkA();boxA=box;}else{range.MarkB();boxB=box;}
        RangeMarked();SetStatus(isA?"가까운 곳 저장":"먼 곳 저장");
        return Task.CompletedTask;
    }
    private string MaxCutsText(){try{return range.MaximumUserCuts.ToString();}catch(InvalidOperationException){return "200";}}
    private void Refresh(){
        if(!Dispatcher.CheckAccess()){Dispatcher.BeginInvoke(new Action(Refresh));return;}
        if(automaticCuts)ShowPresetCuts();
        markA.Background=range.A.HasValue?Brushes.SteelBlue:Brushes.LightGray;markA.Foreground=range.A.HasValue?Brushes.White:Brushes.Black;
        markB.Background=range.B.HasValue?Brushes.SteelBlue:Brushes.LightGray;markB.Foreground=range.B.HasValue?Brushes.White:Brushes.Black;
        string anchors="가까운 곳 "+(range.A.HasValue?"✓":"—")+" · 먼 곳 "+(range.B.HasValue?"✓":"—");
        try{
            var targets=CaptureTargets();string direction=targets[0]==range.A.Value?"가까운 곳 → 먼 곳":"먼 곳 → 가까운 곳";
            long min=long.MaxValue,max=0;for(int i=1;i<targets.Count;i++){long step=Math.Abs((long)targets[i]-targets[i-1]);min=Math.Min(min,step);max=Math.Max(max,step);}
            string gap=min+(min==max?"":"–"+max)+"단위";
            string description=(automaticCuts?(Quality==0?"고":Quality==1?"중":"저"):"직접")+" · "+targets.Count+"컷 · "+gap;
            RangeSummary=anchors+" · "+description+" · "+direction;
        }catch(InvalidOperationException e){RangeSummary=anchors+" · "+e.Message;}
        Tip(start,"현재 초점에서 가까운 끝점부터 촬영·합성합니다.\n"+RangeSummary);
        Tip(markA,"지금 초점을 가까운 곳(A)으로 저장합니다. 먼 곳을 먼저 저장해도 됩니다.\n"+anchors);
        Tip(markB,"지금 초점을 먼 곳(B)으로 저장합니다.\n"+anchors);
        Tip(cuts,"2부터 "+MaxCutsText()+"컷까지 직접 입력할 수 있습니다(3단계마다 한 장이 최대). 품질을 선택하면 해당 컷수가 표시됩니다.\n"+RangeSummary);
        for(int i=0;i<qualityButtons.Count;i++){
            var b=qualityButtons[i];string name=i==0?"고":i==1?"중":"저";
            b.Content=name;
            Tip(b,QualityDescription(i)+(i==0?" · 3단계마다 한 장 찍던 컷수의 3/5":i==1?" · 고 컷수의 약 2/3":" · 고 컷수의 약 1/3")+". 양 끝을 포함하며 최소 2컷. 해상도는 동일합니다.");
            bool selected=i==Quality&&automaticCuts;b.Background=selected?Brushes.SteelBlue:Brushes.LightGray;b.Foreground=selected?Brushes.White:Brushes.Black;b.IsEnabled=!IsWorking||registeringAnchor;
        }
        settings.IsEnabled=!IsWorking||registeringAnchor||manualDriving;foreach(var b in focusMoveButtons)b.IsEnabled=!IsWorking||registeringAnchor||manualDriving;
        markA.IsEnabled=!IsWorking||(registeringAnchor&&activeAnchor!=true);markB.IsEnabled=!IsWorking||(registeringAnchor&&activeAnchor!=false);
        markA.Content=pendingAnchor==true?"가까운 곳…":range.A.HasValue?"가까운 곳 ✓":"가까운 곳";markB.Content=pendingAnchor==false?"먼 곳…":range.B.HasValue?"먼 곳 ✓":"먼 곳";
        autoRow.Visibility=UseFocusBox?Visibility.Visible:Visibility.Collapsed;focusHere.IsEnabled=!IsWorking;
        if(noticeText!=null&&!IsWorking){string head=LensName.Length>0?LensName+" · ":"";noticeText.Text=head+(UseFocusBox?AutoNotice:MfNotice);}
        cuts.IsEnabled=!IsWorking||registeringAnchor;boxMode.IsEnabled=!IsWorking;advanced.IsEnabled=!IsWorking;cancel.IsEnabled=IsWorking;start.IsEnabled=CanCapture;result.IsEnabled=File.Exists(lastResult);if(result.IsEnabled)Tip(result,"합성 결과 열기\n"+lastResult);
        Changed?.Invoke(this,EventArgs.Empty);
    }
    public async Task NudgeAsync(int steps){
        if(steps==0||Math.Abs((long)steps)>20)throw new ArgumentOutOfRangeException(nameof(steps));
        if(IsWorking&&!registeringAnchor&&!manualDriving)return;
        manualMoves.Enqueue(steps);
        if(registeringAnchor){
            manualTakeover=true;pendingAnchor=null;pendingBox=null;cancellation?.Cancel();SetStatus("수동 초점으로 전환 중");Refresh();
            if(anchorReleased!=null)await anchorReleased.Task;
        }
        if(manualDriving||manualMoves.Count==0)return;
        manualDriving=true;
        using(var token=new CancellationTokenSource()){
            cancellation=token;SetStatus("초점 이동");Refresh();
            try{
                while(manualMoves.Count>0){
                    int movement=manualMoves.Dequeue();
                    await Task.Run(async()=>{var adapter=focusCameraFactory();using(adapter as IDisposable){
                        for(int i=0;i<Math.Abs(movement);i++){token.Token.ThrowIfCancellationRequested();adapter.MoveFocus(movement>0?FocusDirection.Far:FocusDirection.Near,FocusAmount.Small);range.Moved(Math.Sign(movement));await Task.Delay(100,token.Token);}
                    }});
                }
                SetStatus("");
            }catch(Exception e){range.Invalidate();boxA=boxB=null;SetStatus(e is OperationCanceledException?"중지됨":e.GetBaseException().Message);}
            finally{manualMoves.Clear();manualDriving=false;manualTakeover=false;cancellation=null;Refresh();}
        }
    }
    private async Task RunAsync(){
        if(IsWorking)return;
        string folder=null;bool captureStarted=false;
        try{
            var targets=CaptureTargets();
            if(!int.TryParse(settle.Text,out int wait)||wait<100||wait>10000)throw new InvalidOperationException("안정화 시간을 100–10000 ms로 입력해 주세요.");
            if(string.IsNullOrWhiteSpace(python.Text)||!File.Exists(python.Text.Trim()))throw new InvalidOperationException("⚙에서 Python 파일을 선택해 주세요.");
            if(string.IsNullOrWhiteSpace(engine.Text)||!File.Exists(engine.Text.Trim()))throw new InvalidOperationException("⚙에서 합성 엔진을 선택해 주세요.");
            string py=Path.GetFullPath(python.Text.Trim()),script=Path.GetFullPath(engine.Text.Trim());
            try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"stack-python.txt"),py);}catch(IOException){}catch(UnauthorizedAccessException){}
            folder=Path.Combine(ServiceProvider.Settings.DefaultSession.Folder,"Superfocus",DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N").Substring(0,8));
            using(var adapter=new G2FocusCamera(camera,folder))using(var token=new CancellationTokenSource()){
                cancellation=token;Refresh();var progress=new Progress<string>(message=>{SetStatus(CloriumStackProcess.DescribeProgress(message));Refresh();});captureStarted=true;
                var files=await Task.Run(()=>new FocusCaptureSequence().RunPlannedRangeAsync(adapter,range,targets,wait,TimeSpan.FromSeconds(90),progress,token.Token));
                token.Token.ThrowIfCancellationRequested();SetStatus("합성 중");
                lastResult=await CloriumStackProcess.RunAsync(py,script,files,Path.Combine(folder,"Superfocus.png"),progress,token.Token);
                SetStatus("완료 · "+files.Count+"컷\n"+lastResult);
            }
        }catch(OperationCanceledException){if(captureStarted){range.Invalidate();boxA=boxB=null;}SetStatus("중지됨\n원본: "+folder);}
        catch(Exception e){if(captureStarted){range.Invalidate();boxA=boxB=null;}SetStatus(e.GetBaseException().Message+(folder==null?"":"\n원본: "+folder));}
        finally{cancellation=null;Refresh();}
    }
}
