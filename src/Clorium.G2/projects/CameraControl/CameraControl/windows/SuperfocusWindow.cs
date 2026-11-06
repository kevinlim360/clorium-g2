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
    private readonly CheckBox boxMode = new CheckBox { Content=global::CameraControl.Devices.UiText.T("M0457", "자동 초점 찾기"), IsChecked=false, Foreground=Brushes.White, VerticalAlignment=VerticalAlignment.Center, Margin=new Thickness(4,2,0,6) };
    // Lens profile: lenses without an AF/MF switch (RF24-240) keep the body in AF and find 가까운 곳/먼 곳 by themselves.
    private TextBlock noticeText;
    private bool? lensAuto;
    public string LensName {get;private set;}="";
    public static bool FindsFocusAutomatically(string lens)=>!string.IsNullOrEmpty(lens)&&lens.IndexOf("24-240",StringComparison.OrdinalIgnoreCase)>=0;
    const string MfNotice="렌즈를 MF(초점 수동 조절)로 설정해 주세요.";
    const string AutoNotice="박스에 초점 맞추기 → ‘가까운 곳’ · ‘먼 곳’ 저장 → ‘촬영’";
    private readonly TextBox cuts = new TextBox { Width=48, Height=26, FontSize=13, Margin=new Thickness(4,0,8,0), Padding=new Thickness(3), Background=Brushes.White, Foreground=Brushes.Black, VerticalContentAlignment=VerticalAlignment.Center };
    private readonly TextBox settle = new TextBox { Text="700" }, python = new TextBox(), engine = new TextBox();
    private readonly TextBlock status = new TextBlock { Foreground=Brushes.White, FontSize=12, TextTrimming=TextTrimming.CharacterEllipsis, Margin=new Thickness(2,4,2,0), Visibility=Visibility.Collapsed };
    private readonly Button markA=Button(global::CameraControl.Devices.UiText.T("M0460", "가까운 곳"),global::CameraControl.Devices.UiText.T("M0461", "지금 초점을 가까운 곳(A)으로 저장합니다.")), markB=Button(global::CameraControl.Devices.UiText.T("M0462", "먼 곳"),global::CameraControl.Devices.UiText.T("M0463", "지금 초점을 먼 곳(B)으로 저장합니다."));
    private readonly Border mfNotice;
    private readonly Button focusHere=Button(global::CameraControl.Devices.UiText.T("M0464", "박스에 초점 맞추기"),global::CameraControl.Devices.UiText.T("M0465", "라이브뷰에서 고른 박스에 초점을 자동으로 맞춥니다. 맞춘 뒤 ‘가까운 곳’ 또는 ‘먼 곳’을 누르면 그 위치를 저장합니다."));
    private readonly StackPanel autoRow=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,0,0,6)};
    private readonly Button start=Button(global::CameraControl.Devices.UiText.T("M0466", "촬영"),global::CameraControl.Devices.UiText.T("M0467", "현재 초점에서 가까운 끝점부터 반대쪽 끝점까지 촬영하고 합성합니다.")), cancel=Button(global::CameraControl.Devices.UiText.T("M0418", "중지"),global::CameraControl.Devices.UiText.T("M0468", "초점 탐색·촬영·합성을 중지합니다. 원본은 보존됩니다.")), result=Button(global::CameraControl.Devices.UiText.T("M0469", "결과보기"),global::CameraControl.Devices.UiText.T("M0470", "완료된 합성 이미지를 엽니다."));
    private FocusBoxContrast boxA,boxB;
    private CancellationTokenSource cancellation;
    private bool allowClose;
    private string lastResult;
    public int Quality {get;private set;}
    public bool UseFocusBox {get=>boxMode.IsChecked==true;set=>boxMode.IsChecked=value;}
    public bool IsWorking => cancellation!=null||manualDriving||manualMoves.Count>0;
    public bool CanCapture {get {if(IsWorking)return false;try{if(UseFocusBox){AfCount();return afA&&afB;}CaptureTargets();return true;}catch(InvalidOperationException){return false;}}}
    private IReadOnlyList<int> CaptureTargets(){if(automaticCuts)return range.CapturePositionsForQuality(Quality);if(!int.TryParse(cuts.Text,out int count))throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0034", "컷수는 2부터 ")+MaxCutsText()+global::CameraControl.Devices.UiText.T("M0035", "컷 까지 입력해 주세요."));return range.CapturePositionsForUserCount(count);}
    private void ShowPresetCuts(){string text="";try{text=UseFocusBox?AfPresets[Quality].ToString():range.QualityCount(Quality).ToString();}catch(InvalidOperationException){}if(cuts.Text!=text){updatingCuts=true;try{cuts.Text=text;}finally{updatingCuts=false;}}}
    private void RangeMarked(){Quality=0;automaticCuts=true;Refresh();}
    public string RangeSummary {get;private set;}
    public string Feedback => status.Text;
    public event EventHandler Changed;
    public static SuperfocusWindow For(ICameraDevice camera,Window owner) {
        if(camera==null)throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0047", "카메라를 연결해 주세요."));
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
        w.SetStatus(global::CameraControl.Devices.UiText.T("M0471", "수퍼포커스 중에는 화살표로 초점을 옮겨 주세요."));return true;
    }
    public void RequestStop(){pendingAnchor=null;pendingBox=null;manualMoves.Clear();manualTakeover=false;cancellation?.Cancel();}
    public Task StartCaptureAsync()=>RunAsync();
    public void SelectQuality(int value){if(IsWorking&&!registeringAnchor)return;if(value<0||value>2)throw new ArgumentOutOfRangeException(nameof(value));Quality=value;automaticCuts=true;Refresh();}
    public string QualityDescription(int value){try{return range.QualityDescription(value);}catch(InvalidOperationException){return (value==0?global::CameraControl.Devices.UiText.T("M0026", "고"):value==1?global::CameraControl.Devices.UiText.T("M0027", "중"):global::CameraControl.Devices.UiText.T("M0028", "저"))+" · —";}}
    private SuperfocusWindow(ICameraDevice device) {
        focusCameraFactory=()=>new G2FocusCamera(camera,ServiceProvider.Settings.DefaultSession.Folder);
        stopNativeAutofocus=()=>((CameraControl.Devices.Canon.CanonSDKBase)camera).PrepareFocusStack();
        camera=device;Title="수퍼포커스";Width=440;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStyle=WindowStyle.ToolWindow;ShowInTaskbar=false;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=new SolidColorBrush(Color.FromRgb(36,44,56));
        var panel=new StackPanel {Margin=new Thickness(10)};
        // First thing in the box: the lens must be in MF. Shown again every time the window opens.
        var ok=Button(global::CameraControl.Devices.UiText.T("M0042", "확인"),global::CameraControl.Devices.UiText.T("M0472", "안내를 닫습니다."));ok.MinHeight=26;ok.Padding=new Thickness(10,2,10,2);ok.Margin=new Thickness(8,0,0,0);DockPanel.SetDock(ok,Dock.Right);
        var noticeRow=new DockPanel();noticeRow.Children.Add(ok);
        noticeText=new TextBlock {Text=MfNotice,Foreground=Brushes.Black,FontWeight=FontWeights.SemiBold,FontSize=13,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center};noticeRow.Children.Add(noticeText);
        mfNotice=new Border {Background=new SolidColorBrush(Color.FromRgb(255,200,40)),CornerRadius=new CornerRadius(6),Padding=new Thickness(10,7,7,7),Margin=new Thickness(0,0,0,10),Child=noticeRow};
        ok.Click+=(s,e)=>mfNotice.Visibility=Visibility.Collapsed;panel.Children.Add(mfNotice);
        IsVisibleChanged+=(s,e)=>{if(IsVisible){mfNotice.Visibility=Visibility.Visible;DetectLens();}};
        Section(settings,global::CameraControl.Devices.UiText.T("M0473", "① 초점 범위"),global::CameraControl.Devices.UiText.T("M0474", "화살표로 초점을 옮기고 ‘가까운 곳’, ‘먼 곳’을 눌러 저장해 주세요. 박스를 옮겨도 렌즈는 움직이지 않습니다."));
        Tip(boxMode,global::CameraControl.Devices.UiText.T("M0475", "켜면 라이브뷰에서 고른 박스에 렌즈가 초점을 찾아 가까운 곳/먼 곳으로 저장합니다(AF/MF 스위치 없는 렌즈, 카메라는 AF). 끄면 화살표로 맞춘 위치를 그대로 저장합니다(렌즈 MF)."));
        boxMode.Click+=(s,e)=>{InvalidateRange();Refresh();};settings.Children.Add(boxMode);
        focusHere.Width=200;focusHere.FontSize=12;focusHere.Click+=async(s,e)=>await FocusHereAsync();autoRow.Children.Add(focusHere);
        var focusRow=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
        markA.Width=74;markB.Width=74;markA.FontSize=12;markB.FontSize=12;markA.Padding=markB.Padding=new Thickness(2,4,2,4);
        markA.Click+=async(s,e)=>await MarkSelectedAsync(true);markB.Click+=async(s,e)=>await MarkSelectedAsync(false);
        foreach(int step in new[]{-20,-5,-1})AddNudge(focusRow,step);
        focusRow.Children.Add(markA);
        foreach(int step in new[]{1,5,20})AddNudge(focusRow,step);
        focusRow.Children.Add(markB);
        settings.Children.Add(autoRow);settings.Children.Add(focusRow);
        Section(settings,global::CameraControl.Devices.UiText.T("M0476", "② 품질"),global::CameraControl.Devices.UiText.T("M0477", "고 · 중 · 저를 고르거나 컷수를 직접 입력해 주세요."));
        var qualityRow=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
        qualityRow.Children.Add(new TextBlock {Text=global::CameraControl.Devices.UiText.T("M0478", "품질"),Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,4,0)});
        for(int i=0;i<3;i++){int choice=i;var b=Button("",global::CameraControl.Devices.UiText.T("M0479", "합성 품질을 선택합니다."));b.Width=56;b.Padding=new Thickness(3,4,3,4);b.Click+=(s,e)=>SelectQuality(choice);qualityButtons.Add(b);qualityRow.Children.Add(b);}
        qualityRow.Children.Add(new TextBlock {Text=global::CameraControl.Devices.UiText.T("M0480", "컷수"),Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(12,0,0,0)});
        Tip(cuts,global::CameraControl.Devices.UiText.T("M0481", "촬영할 컷수(2–200)를 직접 입력합니다. 비우거나 품질 버튼을 누르면 자동 계산합니다. A/B 양 끝을 포함해 균등하게 나눕니다."));
        cuts.IsReadOnly=false;cuts.Focusable=true;cuts.Cursor=System.Windows.Input.Cursors.IBeam;
        cuts.GotKeyboardFocus+=(s,e)=>cuts.Dispatcher.BeginInvoke(new Action(()=>{if(cuts.IsKeyboardFocusWithin)cuts.SelectAll();}),System.Windows.Threading.DispatcherPriority.Input);
        cuts.PreviewTextInput+=(s,e)=>e.Handled=e.Text.Any(c=>c<'0'||c>'9')||cuts.Text.Length-cuts.SelectionLength+e.Text.Length>3;
        // Digits typed on the keyboard did not reach the box in G2 (Korean IME / keypad) although injected keys did: digits are
        // taken straight from the key press (top row, keypad, IME-processed) and the IME is off for this number box.
        System.Windows.Input.InputMethod.SetIsInputMethodEnabled(cuts,false);
        cuts.PreviewKeyDown+=(s,e)=>{int digit=DigitOf(e.Key==System.Windows.Input.Key.ImeProcessed?e.ImeProcessedKey:e.Key);if(digit<0||System.Windows.Input.Keyboard.Modifiers!=System.Windows.Input.ModifierKeys.None)return;e.Handled=true;TypeDigit(digit);};
        DataObject.AddPastingHandler(cuts,(s,e)=>{var text=e.DataObject.GetData(typeof(string)) as string;if(text==null||text.Any(c=>c<'0'||c>'9'))e.CancelCommand();});
        cuts.TextChanged+=(s,e)=>{if(!updatingCuts){automaticCuts=false;Refresh();}};qualityRow.Children.Add(cuts);
        settings.Children.Add(qualityRow);panel.Children.Add(settings);
        Section(panel,global::CameraControl.Devices.UiText.T("M0482", "③ 촬영"),global::CameraControl.Devices.UiText.T("M0483", "가까운 끝점부터 반대쪽 끝점까지 촬영하고 한 장으로 합성합니다."));
        var captureRow=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};
        start.Width=150;start.Background=Brushes.SteelBlue;start.Foreground=Brushes.White;start.FontWeight=FontWeights.SemiBold;start.Click+=async(s,e)=>await RunAsync();
        cancel.Width=60;cancel.Click+=(s,e)=>RequestStop();result.Width=86;result.Click+=(s,e)=>{if(File.Exists(lastResult))System.Diagnostics.Process.Start(lastResult);};
        var reset=Button(global::CameraControl.Devices.UiText.T("M0484", "초기화"),global::CameraControl.Devices.UiText.T("M0485", "가까운 곳·먼 곳을 지웁니다. 렌즈 링·줌·카메라·렌즈를 바꾼 뒤에는 다시 저장해 주세요."));reset.Width=76;reset.Click+=(s,e)=>InvalidateRange();
        captureRow.Children.Add(start);captureRow.Children.Add(cancel);captureRow.Children.Add(result);captureRow.Children.Add(reset);panel.Children.Add(captureRow);panel.Children.Add(status);
        advanced.Margin=new Thickness(4,8,4,0);advanced.Visibility=Visibility.Collapsed;
        Field(advanced,global::CameraControl.Devices.UiText.T("M0486", "안정화 (ms)"),settle);Tip(settle,global::CameraControl.Devices.UiText.T("M0487", "초점 이동 뒤 기다릴 시간입니다. 100–10000 ms. 현재 끝점에서 바로 시작할 때는 추가로 기다리지 않습니다."));
        PathField(advanced,"Python",python,"python.exe|python.exe|실행 파일|*.exe");PathField(advanced,global::CameraControl.Devices.UiText.T("M0489", "엔진"),engine,"Python 파일|*.py");panel.Children.Add(advanced);
        Content=new ScrollViewer {Content=panel,MaxHeight=Math.Max(200,SystemParameters.WorkArea.Height-80),VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        string saved=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"stack-python.txt");python.Text=Environment.GetEnvironmentVariable("CLORIUM_STACK_PYTHON")??"";
        if(string.IsNullOrWhiteSpace(python.Text)&&File.Exists(saved))python.Text=File.ReadAllText(saved).Trim();
        for(var dir=new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);dir!=null;dir=dir.Parent){string script=Path.Combine(dir.FullName,"modules","Clorium.Stack","clorium_stack.py");if(File.Exists(script)){engine.Text=script;break;}}
        Closing+=(s,e)=>{if(IsWorking){e.Cancel=true;RequestStop();SetStatus(global::CameraControl.Devices.UiText.T("M0491", "중지하는 중"));}else if(!allowClose){e.Cancel=true;Hide();}};
        camera.CameraDisconnected+=Disconnected;Closed+=(s,e)=>camera.CameraDisconnected-=Disconnected;Refresh();
    }
    private static int DigitOf(System.Windows.Input.Key key){if(key>=System.Windows.Input.Key.D0&&key<=System.Windows.Input.Key.D9)return key-System.Windows.Input.Key.D0;if(key>=System.Windows.Input.Key.NumPad0&&key<=System.Windows.Input.Key.NumPad9)return key-System.Windows.Input.Key.NumPad0;return -1;}
    // Replaces the selection with the digit (at most 3 digits), like typing.
    public void TypeDigit(int digit){
        if(!cuts.IsEnabled||digit<0||digit>9)return;
        int start=Math.Min(cuts.SelectionStart,cuts.Text.Length),length=Math.Min(cuts.SelectionLength,cuts.Text.Length-start);
        string text=cuts.Text.Remove(start,length).Insert(start,digit.ToString());
        if(text.Length>3)return;
        cuts.Text=text;cuts.SelectionStart=start+1;cuts.SelectionLength=0;
    }
    private static Button Button(string label,string hint)=>WithTip(new Button {Content=label,Margin=new Thickness(2),Padding=new Thickness(6,4,6,4),MinHeight=30,FontSize=13,Background=Brushes.LightGray,Foreground=Brushes.Black},hint);
    private static T WithTip<T>(T control,string text) where T:FrameworkElement {Tip(control,text);return control;}
    private static void Tip(FrameworkElement control,string text){control.ToolTip=new ToolTip {Content=new TextBlock {Text=text,TextWrapping=TextWrapping.Wrap,MaxWidth=280},Background=Brushes.White,Foreground=Brushes.Black,Padding=new Thickness(7)};ToolTipService.SetInitialShowDelay(control,300);ToolTipService.SetShowDuration(control,10000);ToolTipService.SetShowOnDisabled(control,true);}
    // Frozen: the live view's worker thread can be the first to touch this class (TrySelectPreviewBox), and an unfrozen
    // brush made there cannot be used by the window (it then opened blank).
    private static readonly Brush Gray=Frozen(new SolidColorBrush(Color.FromRgb(160,172,188)));
    private static Brush Frozen(Brush b){b.Freeze();return b;}
    private static void Section(Panel panel,string title,string hint){panel.Children.Add(new TextBlock {Text=title,Foreground=Brushes.White,FontWeight=FontWeights.SemiBold,FontSize=13,Margin=new Thickness(2,8,2,4)});// hint text is kept only as the title tooltip (small grey lines removed on request)
        Tip((FrameworkElement)panel.Children[panel.Children.Count-1],hint);}
    private void AddNudge(Panel panel,int step){int value=step;var b=Button(step<0?(step==-20?"◀◀◀":step==-5?"◀◀":"◀"):(step==20?"▶▶▶":step==5?"▶▶":"▶"),(step<0?global::CameraControl.Devices.UiText.T("M0492", "가까이 "):global::CameraControl.Devices.UiText.T("M0493", "멀리 "))+Math.Abs(step)+"단계 이동");b.Width=36;b.FontSize=11;b.Padding=new Thickness(1);b.Click+=async(s,e)=>await NudgeAsync(value);focusMoveButtons.Add(b);panel.Children.Add(b);}
    private static void Field(Panel panel,string label,Control control){panel.Children.Add(new TextBlock {Text=label,Foreground=Brushes.White,Margin=new Thickness(0,4,0,2)});control.Padding=new Thickness(4);control.Background=Brushes.White;control.Foreground=Brushes.Black;panel.Children.Add(control);}
    private void PathField(Panel panel,string label,TextBox box,string filter){Field(panel,label,box);Tip(box,label+global::CameraControl.Devices.UiText.T("M0494", " 파일 경로"));var b=Button("…",label+global::CameraControl.Devices.UiText.T("M0495", " 파일 선택"));b.HorizontalAlignment=HorizontalAlignment.Right;b.Click+=(s,e)=>{var dialog=new Microsoft.Win32.OpenFileDialog {Filter=filter};if(dialog.ShowDialog(this)==true)box.Text=dialog.FileName;};panel.Children.Add(b);}
    private void SetStatus(string text){status.Text=(text??"").Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()??"";status.Visibility=string.IsNullOrEmpty(status.Text)?Visibility.Collapsed:Visibility.Visible;Tip(status,text??"");}
    private void Disconnected(object sender,DisconnectCameraEventArgs e){Dispatcher.BeginInvoke(new Action(()=>{RequestStop();range.Invalidate();boxA=boxB=previewBox=null;SetStatus(global::CameraControl.Devices.UiText.T("M0496", "연결이 끊겼습니다"));Refresh();}));}
    public void InvalidateRange(){if(IsWorking){RequestStop();return;}afA=afB=false;afLensAt=null;range.Reset();boxA=boxB=null;automaticCuts=true;Quality=0;SetStatus("");Refresh();} // the chosen box stays on screen
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
        noticeText.Text=(name.Length>0?name+"\n":"")+(UseFocusBox?AutoNotice:MfNotice);
        Refresh();
    }
    // 자동 초점 찾기 (RF24-240): 박스에 초점 맞추기 is camera AF only. 가까운 곳/먼 곳 remember the AF boxes (no lens move); 촬영
    // AFs on evenly spaced points between them, starting at the end the lens is on (see AfPoints).
    private bool afA,afB;
    private bool? afLensAt; // which saved end the lens was last focused on
    private static readonly int[] AfPresets={15,10,5};
    public Task MarkSelectedAsync(bool isA){
        if(!UseFocusBox)return SaveCurrentAsync(isA);
        if(IsWorking)return Task.CompletedTask;
        FocusBoxContrast box=previewBox;
        if(box==null){try{box=FocusBoxContrast.From(camera.GetLiveViewImage());}catch(Exception e){SetStatus(e.GetBaseException().Message);return Task.CompletedTask;}}
        if(isA){afA=true;boxA=box;}else{afB=true;boxB=box;}
        afLensAt=isA;
        Log.Debug("Superfocus : "+(isA?"가까운 곳":"먼 곳")+" box "+box.X.ToString("0.00")+","+box.Y.ToString("0.00"));
        SetStatus((isA?global::CameraControl.Devices.UiText.T("M0460", "가까운 곳"):global::CameraControl.Devices.UiText.T("M0462", "먼 곳"))+global::CameraControl.Devices.UiText.T("M0497", " 저장"));Refresh();
        return Task.CompletedTask;
    }
    private int AfCount(){
        if(automaticCuts)return AfPresets[Quality];
        if(!int.TryParse(cuts.Text,out int count)||count<2||count>200)throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0498", "컷수는 2부터 200까지 입력해 주세요."));
        return count;
    }
    public async Task FocusHereAsync(){
        if(IsWorking)return;
        FocusBoxContrast box;
        try{box=previewBox??FocusBoxContrast.From(camera.GetLiveViewImage());}catch(Exception e){SetStatus(e.GetBaseException().Message);return;}
        using(var token=new CancellationTokenSource()){
            cancellation=token;SetStatus(global::CameraControl.Devices.UiText.T("M0499", "박스에 초점 맞추는 중"));Refresh();Log.Debug("Superfocus : AF on box "+box.X.ToString("0.00")+","+box.Y.ToString("0.00"));
            try{
                await Task.Run(()=>AfOnBoxAsync(box,token.Token));
                afLensAt=null;
                SetStatus(global::CameraControl.Devices.UiText.T("M0500", "초점 맞춤 · ‘가까운 곳’ 또는 ‘먼 곳’을 눌러 저장하세요"));
            }catch(Exception e){
                Log.Debug("Superfocus : AF on box ended: "+e.GetBaseException().Message);
                SetStatus(e is OperationCanceledException?global::CameraControl.Devices.UiText.T("M0501", "중지됨"):e.GetBaseException().Message);
            }finally{cancellation=null;Refresh();}
        }
    }
    private async Task AfOnBoxAsync(FocusBoxContrast box,CancellationToken token){
        var frame=await ReadFrameAsync(token);
        camera.Focus((int)(box.X*frame.ImageWidth),(int)(box.Y*frame.ImageHeight));await Task.Delay(250,token);
        camera.AutoFocus();await Task.Delay(1600,token);stopNativeAutofocus();await Task.Delay(200,token);
    }
    // AF points for one run: evenly spaced on the line from the start box to the other box, both ends included. On RF24-240
    // the SDK's stepped drive barely changes focus (log 2026-11-06: 800 small or 300 medium steps, sharpness ±5 %), while
    // its AF is accurate, so each cut is an AF on the next point instead of counted lens steps.
    public static IReadOnlyList<Point> AfPoints(FocusBoxContrast start,FocusBoxContrast other,int shots){
        if(start==null||other==null)throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0502", "가까운 곳과 먼 곳을 모두 저장해 주세요."));
        if(shots<2)throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0498", "컷수는 2부터 200까지 입력해 주세요."));
        var points=new List<Point>();
        for(int i=0;i<shots;i++){double t=(double)i/(shots-1);points.Add(new Point(start.X+(other.X-start.X)*t,start.Y+(other.Y-start.Y)*t));}
        return points.AsReadOnly();
    }
    private async Task RunAfAsync(){
        if(IsWorking)return;
        string folder=null;
        try{
            if(!afA||!afB)throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0502", "가까운 곳과 먼 곳을 모두 저장해 주세요."));
            int count=AfCount();
            if(!int.TryParse(settle.Text,out int wait)||wait<100||wait>10000)throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0503", "안정화 시간을 100–10000 ms로 입력해 주세요."));
            if(string.IsNullOrWhiteSpace(python.Text)||!File.Exists(python.Text.Trim())){advanced.Visibility=Visibility.Visible;throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0504", "아래 설정에서 Python 파일을 선택해 주세요."));}
            if(string.IsNullOrWhiteSpace(engine.Text)||!File.Exists(engine.Text.Trim())){advanced.Visibility=Visibility.Visible;throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0505", "아래 설정에서 합성 엔진을 선택해 주세요."));}
            string py=Path.GetFullPath(python.Text.Trim()),script=Path.GetFullPath(engine.Text.Trim());
            try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"stack-python.txt"),py);}catch(IOException){}catch(UnauthorizedAccessException){}
            // Start where the lens already is (the end saved last), so nothing has to travel before the first cut.
            bool startA=afLensAt!=false;
            var points=AfPoints(startA?boxA:boxB,startA?boxB:boxA,count);
            int afWait=600+wait; // AF settles, then the stabilising time
            folder=Path.Combine(ServiceProvider.Settings.DefaultSession.Folder,"Superfocus",DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N").Substring(0,8));
            Log.Debug("Superfocus : AF run "+(startA?"가까운 곳 → 먼 곳":"먼 곳 → 가까운 곳")+", "+count+" cuts, AF wait "+afWait+" ms");
            using(var token=new CancellationTokenSource()){
                cancellation=token;Refresh();var progress=new Progress<string>(message=>{SetStatus(CloriumStackProcess.DescribeProgress(message));Refresh();});
                var clock=System.Diagnostics.Stopwatch.StartNew();
                var files=new List<string>();
                using(var adapter=new G2FocusCamera(camera,folder)){
                    for(int i=0;i<points.Count;i++){
                        token.Token.ThrowIfCancellationRequested();
                        SetStatus(global::CameraControl.Devices.UiText.T("M0506", "초점 맞추고 촬영 ")+(i+1)+"/"+points.Count);
                        var p=points[i];
                        await Task.Run(async()=>{
                            var frame=await ReadFrameAsync(token.Token);
                            camera.Focus((int)(p.X*frame.ImageWidth),(int)(p.Y*frame.ImageHeight));await Task.Delay(150,token.Token);
                            camera.AutoFocus();await Task.Delay(afWait,token.Token);
                        });
                        Task<string> download=adapter.CaptureAsync(); // turns EVF AF off and shoots without AF
                        if(await Task.WhenAny(download,Task.Delay(TimeSpan.FromSeconds(90),token.Token))!=download)throw new TimeoutException(global::CameraControl.Devices.UiText.T("M0507", "사진을 받지 못했습니다(")+(i+1)+global::CameraControl.Devices.UiText.T("M0508", "컷)."));
                        files.Add(await download);
                        afLensAt=i==points.Count-1?!startA:(bool?)null;
                    }
                }
                Log.Debug("Superfocus : "+files.Count+" cuts in "+clock.Elapsed.TotalSeconds.ToString("0.0")+" s");
                token.Token.ThrowIfCancellationRequested();SetStatus(global::CameraControl.Devices.UiText.T("M0509", "합성하는 중"));
                lastResult=await CloriumStackProcess.RunAsync(py,script,files,Path.Combine(folder,"Superfocus.png"),progress,token.Token);
                SetStatus(global::CameraControl.Devices.UiText.T("M0510", "완료 · ")+files.Count+global::CameraControl.Devices.UiText.T("M0511", "컷 · 촬영 ")+clock.Elapsed.TotalSeconds.ToString("0")+global::CameraControl.Devices.UiText.T("M0512", "초\n")+lastResult);
            }
        }catch(OperationCanceledException){SetStatus(global::CameraControl.Devices.UiText.T("M0501", "중지됨")+(folder==null?"":global::CameraControl.Devices.UiText.T("M0513", "\n원본: ")+folder));}
        catch(Exception e){Log.Debug("Superfocus : capture ended: "+e.GetBaseException().Message);SetStatus(e.GetBaseException().Message+(folder==null?"":"\n원본: "+folder));}
        finally{cancellation=null;Refresh();}
    }
    private async Task<LiveViewData> ReadFrameAsync(CancellationToken token){for(int i=0;i<20;i++){token.ThrowIfCancellationRequested();var frame=camera.GetLiveViewImage();if(frame?.ImageData!=null&&frame.ImageData.Length>0)return frame;await Task.Delay(50,token);}throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0514", "라이브뷰 화면을 읽지 못했습니다."));}
    // 가까운 곳 / 먼 곳 (렌즈 MF): saves where the arrows left the lens (no search, no AF). The selected preview box is kept with it.
    private Task SaveCurrentAsync(bool isA){
        if(IsWorking)return Task.CompletedTask;
        if((isA&&range.B==range.Position)||(!isA&&range.A==range.Position)){SetStatus("가까운 곳과 먼 곳이 같은 초점입니다. 화살표로 초점을 옮긴 뒤 저장하세요.");return Task.CompletedTask;}
        FocusBoxContrast box=previewBox;
        if(box==null){try{box=FocusBoxContrast.From(camera.GetLiveViewImage());}catch(Exception){box=null;}}
        if(isA){range.MarkA();boxA=box;}else{range.MarkB();boxB=box;}
        RangeMarked();SetStatus(isA?global::CameraControl.Devices.UiText.T("M0515", "가까운 곳을 저장했습니다"):global::CameraControl.Devices.UiText.T("M0516", "먼 곳을 저장했습니다"));
        return Task.CompletedTask;
    }
    private string MaxCutsText(){try{return range.MaximumUserCuts.ToString();}catch(InvalidOperationException){return "200";}}
    private void Refresh(){
        if(!Dispatcher.CheckAccess()){Dispatcher.BeginInvoke(new Action(Refresh));return;}
        if(automaticCuts)ShowPresetCuts();
        bool hasA=UseFocusBox?afA:range.A.HasValue,hasB=UseFocusBox?afB:range.B.HasValue;
        markA.Background=hasA?Brushes.SteelBlue:Brushes.LightGray;markA.Foreground=hasA?Brushes.White:Brushes.Black;
        markB.Background=hasB?Brushes.SteelBlue:Brushes.LightGray;markB.Foreground=hasB?Brushes.White:Brushes.Black;
        string anchors=global::CameraControl.Devices.UiText.T("M0517", "가까운 곳 ")+(hasA?"✓":"—")+global::CameraControl.Devices.UiText.T("M0518", " · 먼 곳 ")+(hasB?"✓":"—");
        if(UseFocusBox){try{RangeSummary=anchors+" · "+(automaticCuts?(Quality==0?global::CameraControl.Devices.UiText.T("M0026", "고"):Quality==1?global::CameraControl.Devices.UiText.T("M0027", "중"):global::CameraControl.Devices.UiText.T("M0028", "저")):global::CameraControl.Devices.UiText.T("M0519", "직접"))+" "+AfCount()+"컷 · 거리는 촬영할 때 잽니다";}catch(InvalidOperationException e){RangeSummary=anchors+" · "+e.Message;}}
        else try{
            var targets=CaptureTargets();string direction=targets[0]==range.A.Value?global::CameraControl.Devices.UiText.T("M0520", "가까운 곳 → 먼 곳"):global::CameraControl.Devices.UiText.T("M0521", "먼 곳 → 가까운 곳");
            long min=long.MaxValue,max=0;for(int i=1;i<targets.Count;i++){long step=Math.Abs((long)targets[i]-targets[i-1]);min=Math.Min(min,step);max=Math.Max(max,step);}
            string gap=min+(min==max?"":"–"+max)+"단위";
            string description=(automaticCuts?(Quality==0?global::CameraControl.Devices.UiText.T("M0026", "고"):Quality==1?global::CameraControl.Devices.UiText.T("M0027", "중"):global::CameraControl.Devices.UiText.T("M0028", "저")):global::CameraControl.Devices.UiText.T("M0519", "직접"))+" · "+targets.Count+"컷 · "+gap;
            RangeSummary=anchors+" · "+description+" · "+direction;
        }catch(InvalidOperationException e){RangeSummary=anchors+" · "+e.Message;}
        Tip(start,global::CameraControl.Devices.UiText.T("M0522", "현재 초점에서 가까운 끝점부터 촬영하고 합성합니다.\n")+RangeSummary);
        Tip(markA,global::CameraControl.Devices.UiText.T("M0523", "지금 초점을 가까운 곳(A)으로 저장합니다. 먼 곳을 먼저 저장해도 됩니다.\n")+anchors);
        Tip(markB,global::CameraControl.Devices.UiText.T("M0524", "지금 초점을 먼 곳(B)으로 저장합니다.\n")+anchors);
        Tip(cuts,global::CameraControl.Devices.UiText.T("M0525", "2부터 ")+(UseFocusBox?"200":MaxCutsText())+global::CameraControl.Devices.UiText.T("M0526", "컷까지 직접 입력할 수 있습니다. 지우고 다시 입력하면 품질 대신 그 컷수로 찍습니다. 품질을 누르면 그 품질의 컷수로 돌아갑니다.\n")+RangeSummary);
        for(int i=0;i<qualityButtons.Count;i++){
            var b=qualityButtons[i];string name=i==0?global::CameraControl.Devices.UiText.T("M0026", "고"):i==1?global::CameraControl.Devices.UiText.T("M0027", "중"):global::CameraControl.Devices.UiText.T("M0028", "저");
            string count="—";try{count=UseFocusBox?AfPresets[i].ToString():range.QualityCount(i).ToString();}catch(InvalidOperationException){}
            b.Content=name+" "+count;
            Tip(b,QualityDescription(i)+(i==0?global::CameraControl.Devices.UiText.T("M0527", " · 예전 컷수(3단계마다 한 장)의 3/5"):i==1?global::CameraControl.Devices.UiText.T("M0528", " · 고 컷수의 약 2/3"):global::CameraControl.Devices.UiText.T("M0529", " · 고 컷수의 약 1/3"))+". 양 끝을 포함하며 최소 2컷. 해상도는 동일합니다.");
            bool selected=i==Quality&&automaticCuts;b.Background=selected?Brushes.SteelBlue:Brushes.LightGray;b.Foreground=selected?Brushes.White:Brushes.Black;b.IsEnabled=!IsWorking||registeringAnchor;
        }
        settings.IsEnabled=!IsWorking||registeringAnchor||manualDriving;foreach(var b in focusMoveButtons)b.IsEnabled=!IsWorking||registeringAnchor||manualDriving;
        markA.IsEnabled=!IsWorking||(registeringAnchor&&activeAnchor!=true);markB.IsEnabled=!IsWorking||(registeringAnchor&&activeAnchor!=false);
        markA.Content=hasA?global::CameraControl.Devices.UiText.T("M0530", "가까운 곳 ✓"):global::CameraControl.Devices.UiText.T("M0460", "가까운 곳");markB.Content=hasB?global::CameraControl.Devices.UiText.T("M0531", "먼 곳 ✓"):global::CameraControl.Devices.UiText.T("M0462", "먼 곳");
        autoRow.Visibility=UseFocusBox?Visibility.Visible:Visibility.Collapsed;focusHere.IsEnabled=!IsWorking;
        if(noticeText!=null&&!IsWorking){string head=LensName.Length>0?LensName+"\n":"";noticeText.Text=head+(UseFocusBox?AutoNotice:MfNotice);}
        cuts.IsEnabled=!IsWorking;boxMode.IsEnabled=!IsWorking;advanced.IsEnabled=!IsWorking;cancel.IsEnabled=IsWorking;start.IsEnabled=CanCapture;result.IsEnabled=File.Exists(lastResult);if(result.IsEnabled)Tip(result,global::CameraControl.Devices.UiText.T("M0532", "합성 결과 열기\n")+lastResult);
        Changed?.Invoke(this,EventArgs.Empty);
    }
    public async Task NudgeAsync(int steps){
        if(steps==0||Math.Abs((long)steps)>20)throw new ArgumentOutOfRangeException(nameof(steps));
        if(IsWorking&&!registeringAnchor&&!manualDriving)return;
        manualMoves.Enqueue(steps);
        if(registeringAnchor){
            manualTakeover=true;pendingAnchor=null;pendingBox=null;cancellation?.Cancel();SetStatus(global::CameraControl.Devices.UiText.T("M0533", "수동 초점으로 전환하는 중"));Refresh();
            if(anchorReleased!=null)await anchorReleased.Task;
        }
        if(manualDriving||manualMoves.Count==0)return;
        manualDriving=true;
        using(var token=new CancellationTokenSource()){
            cancellation=token;SetStatus(global::CameraControl.Devices.UiText.T("M0015", "초점 이동"));Refresh();
            try{
                while(manualMoves.Count>0){
                    int movement=manualMoves.Dequeue();
                    await Task.Run(async()=>{var adapter=focusCameraFactory();using(adapter as IDisposable){
                        for(int i=0;i<Math.Abs(movement);i++){token.Token.ThrowIfCancellationRequested();adapter.MoveFocus(movement>0?FocusDirection.Far:FocusDirection.Near,FocusAmount.Small);range.Moved(Math.Sign(movement));await Task.Delay(100,token.Token);}
                    }});
                }
                SetStatus("");
            }catch(Exception e){range.Invalidate();boxA=boxB=null;SetStatus(e is OperationCanceledException?global::CameraControl.Devices.UiText.T("M0501", "중지됨"):e.GetBaseException().Message);}
            finally{manualMoves.Clear();manualDriving=false;manualTakeover=false;cancellation=null;Refresh();}
        }
    }
    private async Task RunAsync(){
        if(IsWorking)return;
        if(UseFocusBox){await RunAfAsync();return;}
        string folder=null;bool captureStarted=false;
        try{
            var targets=CaptureTargets();
            if(!int.TryParse(settle.Text,out int wait)||wait<100||wait>10000)throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0503", "안정화 시간을 100–10000 ms로 입력해 주세요."));
            if(string.IsNullOrWhiteSpace(python.Text)||!File.Exists(python.Text.Trim())){advanced.Visibility=Visibility.Visible;throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0504", "아래 설정에서 Python 파일을 선택해 주세요."));}
            if(string.IsNullOrWhiteSpace(engine.Text)||!File.Exists(engine.Text.Trim())){advanced.Visibility=Visibility.Visible;throw new InvalidOperationException(global::CameraControl.Devices.UiText.T("M0505", "아래 설정에서 합성 엔진을 선택해 주세요."));}
            string py=Path.GetFullPath(python.Text.Trim()),script=Path.GetFullPath(engine.Text.Trim());
            try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"stack-python.txt"),py);}catch(IOException){}catch(UnauthorizedAccessException){}
            folder=Path.Combine(ServiceProvider.Settings.DefaultSession.Folder,"Superfocus",DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N").Substring(0,8));
            using(var adapter=new G2FocusCamera(camera,folder))using(var token=new CancellationTokenSource()){
                cancellation=token;Refresh();var progress=new Progress<string>(message=>{SetStatus(CloriumStackProcess.DescribeProgress(message));Refresh();});captureStarted=true;
                var files=await Task.Run(()=>new FocusCaptureSequence().RunPlannedRangeAsync(adapter,range,targets,wait,TimeSpan.FromSeconds(90),progress,token.Token));
                token.Token.ThrowIfCancellationRequested();SetStatus(global::CameraControl.Devices.UiText.T("M0509", "합성하는 중"));
                lastResult=await CloriumStackProcess.RunAsync(py,script,files,Path.Combine(folder,"Superfocus.png"),progress,token.Token);
                SetStatus(global::CameraControl.Devices.UiText.T("M0510", "완료 · ")+files.Count+global::CameraControl.Devices.UiText.T("M0534", "컷\n")+lastResult);
            }
        }catch(OperationCanceledException){if(captureStarted){range.Invalidate();boxA=boxB=null;}SetStatus(global::CameraControl.Devices.UiText.T("M0535", "중지됨\n원본: ")+folder);}
        catch(Exception e){if(captureStarted){range.Invalidate();boxA=boxB=null;}SetStatus(e.GetBaseException().Message+(folder==null?"":global::CameraControl.Devices.UiText.T("M0513", "\n원본: ")+folder));}
        finally{cancellation=null;Refresh();}
    }
}
