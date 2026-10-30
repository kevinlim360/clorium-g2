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
 public override LiveViewData GetLiveViewImage(){return Frame;}
}
class G2InputCheck {
 static object window;static Type type;
 static object Field(string name){return type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(window);}
 static void Set(string name,object value){type.GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(window,value);}
 static object Call(string name,params object[] args){return type.GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(window,args);}
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 [STAThread] static int Main(){try{
  type=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"CameraControl.exe")).GetType("CameraControl.windows.SuperfocusWindow",true);
  var application=new Application(); application.Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.TextBox.xaml")}); var camera=new FrameCamera();window=Activator.CreateInstance(type,BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{camera},null);
  var cuts=(TextBox)Field("cuts");var range=(FocusRangePlan)Field("range");
  Call("MarkA");range.Moved(72);Call("MarkB"); cuts.Measure(new Size(42,24));cuts.Arrange(new Rect(0,0,42,24));cuts.ApplyTemplate();cuts.UpdateLayout();var host=(FrameworkElement)cuts.Template.FindName("PART_ContentHost",cuts);Console.WriteLine("THEMED HOST "+host.ActualWidth+"x"+host.ActualHeight+" Text="+cuts.Text);
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
   ((Task)Call("MarkSelectedAsync",false)).GetAwaiter().GetResult();
   Check((bool?)Field("pendingAnchor")==false,"B click was not queued");
   var snapshot=(FocusBoxContrast)Field("pendingBox");camera.Frame.FocusX=100;
   Check(Math.Abs(snapshot.X-.8)<.001,"queued B must retain its selected box");
   Check(((Button)Field("markB")).Content.ToString()=="B…","queued B status missing");
   Call("RequestStop");Check(Field("pendingAnchor")==null && Field("pendingBox")==null && token.IsCancellationRequested,"stop must discard queued target");
  }
  using(var token=new CancellationTokenSource()){
   Set("activeAnchor",(bool?)false);Set("cancellation",token);Call("Refresh");
   Check(((Button)Field("markA")).IsEnabled && !((Button)Field("markB")).IsEnabled,"A must remain available while B searches");
   ((Task)Call("MarkSelectedAsync",true)).GetAwaiter().GetResult();Check((bool?)Field("pendingAnchor")==true,"B-first queue failed");Call("RequestStop");
  }
  Set("cancellation",null);Set("registeringAnchor",false);Set("activeAnchor",null);Call("InvalidateRange");
  Console.WriteLine("PASS actual WPF: A/B immediate opposite button, queued immutable ROI, B-first and cancellation");
  ((Window)window).Close();return 0;
 }catch(Exception e){Console.WriteLine(e);return 1;}}
}

