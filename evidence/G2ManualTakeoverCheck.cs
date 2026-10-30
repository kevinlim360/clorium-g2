using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Devices.Canon;
using CameraControl.Devices.Classes;
using CameraControl.Core.Classes;
class FrameCamera : CanonSDKBase {
 public LiveViewData Frame=new LiveViewData {ImageWidth=1000,ImageHeight=1000,FocusFrameXSize=100,FocusFrameYSize=100,FocusX=800,FocusY=300};
 public volatile int AfCalls;public override void AutoFocus(){Interlocked.Increment(ref AfCalls);}public int FocusCalls;public override void Focus(int x,int y){FocusCalls++;}public override LiveViewData GetLiveViewImage(){return Frame;}
}
class G2InputCheck {
 static object window;static Type type;
 static object Field(string name){return type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(window);}
 static void Set(string name,object value){type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(window,value);}
 static object Call(string name,params object[] args){return type.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(window,args);}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void PumpUntil(Func<bool> ready){var end=DateTime.UtcNow.AddSeconds(8);while(!ready()&&DateTime.UtcNow<end){var frame=new System.Windows.Threading.DispatcherFrame();System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,new Action(()=>frame.Continue=false));System.Windows.Threading.Dispatcher.PushFrame(frame);Thread.Sleep(5);}Check(ready(),"async test timed out");}
 [STAThread] static int Main(){try{
  type=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"CameraControl.exe")).GetType("CameraControl.windows.SuperfocusWindow",true);
  var camera=new FrameCamera();window=type.GetMethod("For").Invoke(null,new object[]{camera,null});
  var cuts=(TextBox)Field("cuts");var range=(FocusRangePlan)Field("range");
  Call("MarkA");range.Moved(72);Call("MarkB");
  Check(cuts.Text=="25" && (int)type.GetProperty("Quality").GetValue(window,null)==0,"anchors must populate high count25");
  Call("SelectQuality",1);Check(cuts.Text=="17","medium count17 not displayed");
  Call("SelectQuality",2);Check(cuts.Text=="8","low count8 not displayed");
  for(int count=2;count<=25;count++){cuts.Text=count.ToString();Call("Refresh");Check(cuts.Text==count.ToString() && (bool)type.GetProperty("CanCapture").GetValue(window,null),"typed count overwritten/rejected");}
  cuts.Text="26";Check(!(bool)type.GetProperty("CanCapture").GetValue(window,null),"above-high value accepted");
  cuts.Text="1";Check(!(bool)type.GetProperty("CanCapture").GetValue(window,null),"below2 accepted");
  cuts.Text="";Check(!(bool)type.GetProperty("CanCapture").GetValue(window,null),"empty manual edit silently accepted");
  Call("SelectQuality",0);Check(cuts.Text=="25" && cuts.IsEnabled && !cuts.IsReadOnly && cuts.Focusable,"preset/editable state wrong");
  Console.WriteLine("PASS actual WPF: high25/medium17/low8, every typed2..25, invalid/empty input, refresh/caret settings");
  using(var token=new CancellationTokenSource()){
   Set("registeringAnchor",true);Set("activeAnchor",(bool?)true);Set("cancellation",token);Call("Refresh");
   Check(((Button)Field("markB")).IsEnabled && !((Button)Field("markA")).IsEnabled,"B must remain available while A searches");
   var vmType=type.Assembly.GetType("CameraControl.ViewModel.LiveViewViewModel",true);var vm=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(vmType);vmType.GetField("_cameraDevice",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(vm,camera);vmType.GetProperty("LiveViewData").SetValue(vm,camera.Frame,null);vmType.GetMethod("SetFocusPos",new[]{typeof(int),typeof(int)}).Invoke(vm,new object[]{200,600});Check(camera.FocusCalls==0,"moving preview must not redirect hardware AF");Check(Math.Abs(((FocusBoxContrast)type.GetMethod("PreviewBoxFor").Invoke(null,new object[]{camera})).X-.2)<.001,"preview overlay did not move");Check(cuts.IsEnabled,"typed cuts blocked during anchor search");cuts.Text="3";Check(cuts.Text=="3","search erased typed count");((Task)Call("MarkSelectedAsync",false)).GetAwaiter().GetResult();
   Check((bool?)Field("pendingAnchor")==false,"B click was not queued");
   var snapshot=(FocusBoxContrast)Field("pendingBox");camera.Frame.FocusX=100;
   Check(Math.Abs(snapshot.X-.2)<.001,"queued B must retain its selected box");
   Check(((Button)Field("markB")).Content.ToString()=="B…","queued B status missing");
   Call("RequestStop");Check(Field("pendingAnchor")==null && Field("pendingBox")==null && token.IsCancellationRequested,"stop must discard queued target");
  }
  using(var token=new CancellationTokenSource()){
   Set("activeAnchor",(bool?)false);Set("cancellation",token);Call("Refresh");
   Check(((Button)Field("markA")).IsEnabled && !((Button)Field("markB")).IsEnabled,"A must remain available while B searches");
   ((Task)Call("MarkSelectedAsync",true)).GetAwaiter().GetResult();Check((bool?)Field("pendingAnchor")==true,"B-first queue failed");Call("RequestStop");
  }
  Set("cancellation",null);Set("registeringAnchor",false);Set("activeAnchor",null);Call("InvalidateRange");
  Console.WriteLine("PASS actual WPF: immediate preview movement, captured B ROI, editable count while searching, B-first and cancellation");
  SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext());
  Set("focusCameraFactory",new Func<IFocusCaptureCamera>(()=>new ManualFakeAdapter()));
  Set("stopNativeAutofocus",new Action(()=>Interlocked.Increment(ref ManualFakeAdapter.Stops)));
  var registration=(Task)Call("MarkSelectedAsync",true);
  PumpUntil(()=>camera.AfCalls==1);
  foreach(Button arrow in (System.Collections.Generic.List<Button>)Field("focusMoveButtons"))Check(arrow.IsEnabled,"arrow locked during A search");
  var movement=(Task)Call("NudgeAsync",5);
  PumpUntil(()=>ManualFakeAdapter.Moves>0);
  foreach(Button arrow in (System.Collections.Generic.List<Button>)Field("focusMoveButtons"))Check(arrow.IsEnabled,"arrow locked while moving manually");
  ((Task)Call("NudgeAsync",-1)).GetAwaiter().GetResult();
  PumpUntil(()=>movement.IsCompleted && registration.IsCompleted);movement.GetAwaiter().GetResult();registration.GetAwaiter().GetResult();
  Check(range.A==0 && range.Position==4 && !range.B.HasValue,"manual takeover lost current A or accepted extra AF moves");
  Check(ManualFakeAdapter.Moves==6 && ManualFakeAdapter.Owners==0 && ManualFakeAdapter.MaxOwners==1 && ManualFakeAdapter.Stops==1,"manual takeover command order/ownership");
  using(var captureLock=new CancellationTokenSource()){
    Set("cancellation",captureLock);Call("Refresh");int factoryCount=ManualFakeAdapter.Factories;
    foreach(Button arrow in (System.Collections.Generic.List<Button>)Field("focusMoveButtons"))Check(!arrow.IsEnabled,"capture must keep focus lock");
    ((Task)Call("NudgeAsync",1)).GetAwaiter().GetResult();Check(ManualFakeAdapter.Factories==factoryCount,"capture lock allowed manual lens drive");Set("cancellation",null);
  }
  Call("Refresh");var stopped=(Task)Call("NudgeAsync",20);PumpUntil(()=>ManualFakeAdapter.Moves>=7);Call("RequestStop");PumpUntil(()=>stopped.IsCompleted);stopped.GetAwaiter().GetResult();Check(!range.A.HasValue && ManualFakeAdapter.Owners==0,"stop did not release manual drive/invalidate incomplete movement");
  Console.WriteLine("PASS real WPF async: A search interrupted, native AF stopped, A preserved, arrows enabled, ordered manual queue, capture lock and stop");
  ((Window)window).Close();return 0;
 }catch(Exception e){Console.WriteLine(e);return 1;}}
}

class ManualFakeAdapter : IFocusCaptureCamera,IDisposable {
 public static int Factories,Owners,MaxOwners,Stops,Moves;
 public bool IsConnected {get{return true;}}
 public ManualFakeAdapter(){Interlocked.Increment(ref Factories);int owners=Interlocked.Increment(ref Owners);MaxOwners=Math.Max(MaxOwners,owners);if(owners!=1)throw new Exception("concurrent motor owner");}
 public void MoveFocus(FocusDirection direction,FocusAmount amount){if(Stops==0)throw new Exception("manual move before native AF stop");if(amount!=FocusAmount.Small)throw new Exception("mixed step size");Interlocked.Increment(ref Moves);}
 public Task<string> CaptureAsync(){throw new Exception("unexpected exposure");}
 public void Dispose(){Interlocked.Decrement(ref Owners);}
}
