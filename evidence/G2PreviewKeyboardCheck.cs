using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using CameraControl.Devices.Canon;
using CameraControl.Devices.Classes;
using CameraControl.Core.Classes;
class PreviewFrameCamera : CanonSDKBase {public override LiveViewData GetLiveViewImage(){return new LiveViewData {ImageWidth=1000,ImageHeight=1000,FocusFrameXSize=100,FocusFrameYSize=100,FocusX=500,FocusY=500};}}
class G2PreviewKeyboardCheck {
 [STAThread] static int Main(string[] args){try{
  if(args.Length==0)args=new[]{@"C:\Users\user\Documents\ChatGPT\클로리움G2\src\Clorium.G2\projects\CameraControl\CameraControl\App.xaml",@"C:\Users\user\Documents\ChatGPT\클로리움G2\evidence\superfocus-keyboard-events.log"};var app=new Application();
  foreach(Match m in Regex.Matches(File.ReadAllText(args[0]),"ResourceDictionary Source=\"([^\"]+)\""))app.Resources.MergedDictionaries.Add(new ResourceDictionary {Source=new Uri(m.Groups[1].Value)});
  var type=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"CameraControl.exe")).GetType("CameraControl.windows.SuperfocusWindow",true);
  var window=(Window)type.GetMethod("For").Invoke(null,new object[]{new PreviewFrameCamera(),null});
  var range=(FocusRangePlan)type.GetField("range",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
  type.GetMethod("MarkA").Invoke(window,null);range.Moved(72);type.GetMethod("MarkB").Invoke(window,null);
  type.GetField("allowClose",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(window,true);
  var cuts=(TextBox)type.GetField("cuts",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(window);
  string log=Path.GetFullPath(args[1]);
  cuts.TextChanged+=(s,e)=>File.AppendAllText(log,"TEXT="+cuts.Text+" FOCUS="+cuts.IsKeyboardFocusWithin+Environment.NewLine);
  cuts.GotKeyboardFocus+=(s,e)=>File.AppendAllText(log,"KEYBOARD FOCUS"+Environment.NewLine);
  cuts.PreviewKeyDown+=(s,e)=>File.AppendAllText(log,"KEY="+e.Key+Environment.NewLine);
  window.Title="수퍼포커스 입력 검사";window.ShowInTaskbar=true;window.WindowStartupLocation=WindowStartupLocation.CenterScreen;
  window.Closed+=(s,e)=>File.AppendAllText(log,"CLOSED"+Environment.NewLine);
  app.Run(window);return 0;
 }catch(Exception e){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"keyboard-check-error.txt"),e.ToString());return 1;}}
}
