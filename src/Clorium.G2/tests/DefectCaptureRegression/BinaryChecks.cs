using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;

class BinaryChecks {
 static int count;
 static void Check(string name,bool value){if(!value)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);count++;}
 static PhotoSession CreateSession(string folder){var session=(PhotoSession)FormatterServices.GetUninitializedObject(typeof(PhotoSession));typeof(PhotoSession).GetField("_locker",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(session,new object());session.RawExtensions=new List<string>{".cr2",".nef"};session.Folder=folder;session.FileNameTemplate="Barcode_[Counter 3 digit]";return session;}
 static void Pump(){Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.DataBind,new Action(delegate{}));}
 [STAThread] public static void Main(string[] args){
  AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){var name=new AssemblyName(e.Name).Name+".dll";foreach(var dir in new[]{args[1],Path.Combine(args[0],"dependencies","Managed")}){var file=Path.Combine(dir,name);if(File.Exists(file))return Assembly.LoadFrom(file);}return null;};
  Run(args);
 }
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static void Run(string[] args){
  var root=args[0];
  ServiceProvider.Settings=(Settings)FormatterServices.GetUninitializedObject(typeof(Settings));
  ServiceProvider.Branding=(Branding)FormatterServices.GetUninitializedObject(typeof(Branding));
  var s=ServiceProvider.Settings;s.CameraProperties=new CameraPropertyEnumerator();s.CloriumID="M1_";ServiceProvider.FilenameTemplateManager=new FilenameTemplateManager();
  var camera=new BaseCameraDevice();camera.SerialNumber="TEST-ONLY";
  string temp=Path.Combine(Path.GetTempPath(),"CloriumBinary2161-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  try{
   var folder=Path.Combine(temp,"M1_12345678");Directory.CreateDirectory(folder);
   foreach(int mode in new[]{0,1,2})foreach(bool defect in new[]{false,true}){s.FrontBackMode=mode;s.IsDefect=defect;var session=CreateSession(folder);var name=session.GetNextFileName("IMG.jpg",camera,"");string suffix=defect?"_D.jpg":mode==1?"_F.jpg":mode==2?"_B.jpg":"_001.jpg";Check("actual Core suffix mode="+mode+" defect="+defect,name.EndsWith(suffix));}
   s.FrontBackMode=0;s.IsDefect=true;var duplicates=CreateSession(folder);var first=duplicates.GetNextFileName("IMG.jpg",camera,"");File.WriteAllText(first,"existing data must survive");duplicates.Counter--;var second=duplicates.GetNextFileName("IMG.jpg",camera,"");Check("actual Core duplicate path differs",first!=second);Check("actual Core preserves existing file",File.ReadAllText(first)=="existing data must survive");
   s.isBtnPressed=false;s.ThumbailLoaded=true;int notifications=0;s.PropertyChanged+=delegate(object o,System.ComponentModel.PropertyChangedEventArgs e){if(e.PropertyName=="isBtnPressed"||e.PropertyName=="ThumbailLoaded")notifications++;};s.isBtnPressed=true;s.isBtnPressed=false;s.ThumbailLoaded=false;s.ThumbailLoaded=true;Check("actual Settings emits four state notifications",notifications==4);
   var xml=new XmlDocument();xml.Load(Path.Combine(root,"projects","CameraControl","CameraControl","MainWindow.xaml"));var box=xml.SelectSingleNode("//*[@Name='Check_Defect']");var style=box.SelectSingleNode("*[local-name()='CheckBox.Style']/*[local-name()='Style']");style.Attributes.RemoveNamedItem("BasedOn");
   var checkBox=new CheckBox();checkBox.DataContext=s;checkBox.Style=(Style)XamlReader.Parse(style.OuterXml);checkBox.ApplyTemplate();Pump();Check("WPF defect enabled at idle",checkBox.IsEnabled);s.isBtnPressed=true;Pump();Check("WPF defect disabled while busy",!checkBox.IsEnabled);s.isBtnPressed=false;s.ThumbailLoaded=false;Pump();Check("WPF defect disabled while thumbnail pending",!checkBox.IsEnabled);s.ThumbailLoaded=true;Pump();Check("WPF defect reenabled after completion",checkBox.IsEnabled);
   var asm=Assembly.ReflectionOnlyLoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"CameraControl.exe"));var fv=System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location).FileVersion;Check("Main file version 4.7.1.5",fv=="4.7.1.5");Check("Main assembly compatibility preserved",asm.GetName().Version.ToString()=="2.1.6.0");Check("Core file version 4.7.1.5",System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Settings).Assembly.Location).FileVersion=="4.7.1.5");Check("Core assembly compatibility preserved",typeof(Settings).Assembly.GetName().Version.ToString()=="2.1.6.0");
   Check("Main product version 4.7.1.5",System.Diagnostics.FileVersionInfo.GetVersionInfo(asm.Location).ProductVersion=="4.7.1.5");
   Check("Core product version 4.7.1.5",System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(Settings).Assembly.Location).ProductVersion=="4.7.1.5");
   typeof(Settings).GetField("_defaultSession",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s,CreateSession(folder));
   var runtimeMain=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"CameraControl.exe"));
   var menuType=runtimeMain.GetType("CameraControl.ViewModel.MainMenuViewModel",true);
   var resetPurpose=menuType.GetMethod("ResetCapturePurposeForNewSession",BindingFlags.Static|BindingFlags.NonPublic);
   s.IsDefect=true;s.FrontBackMode=2;s.GetMeasurePicture=true;s.Background=true;
   resetPurpose.Invoke(null,null);
   Check("actual Main clears all previous-product capture flags",!s.IsDefect&&s.FrontBackMode==0&&!s.GetMeasurePicture&&!s.Background);
   var nextFolder=Path.Combine(temp,"M1_NEXTPRODUCT");var nextSession=CreateSession(nextFolder);
   s.FrontBackMode=1;Check("actual Main reset then next product F",nextSession.GetNextFileName("IMG.jpg",camera,"").EndsWith("_F.jpg"));
   s.FrontBackMode=2;Check("actual Main reset then next product B",nextSession.GetNextFileName("IMG.jpg",camera,"").EndsWith("_B.jpg"));
   s.IsDefect=true;Check("actual next product D overrides back",nextSession.GetNextFileName("IMG.jpg",camera,"").EndsWith("_D.jpg"));
   resetPurpose.Invoke(null,null);s.isBtnPressed=false;s.ThumbailLoaded=true;
   var uninitializedMenu=FormatterServices.GetUninitializedObject(menuType);
   Check("actual Main permits new session when idle",(bool)menuType.GetMethod("CanStartNewCaptureSession",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(uninitializedMenu,null));
   // Execute the real release Core, including stale button flags, for each turntable image.
   var format=typeof(PhotoSession).GetMethod("FormatFileName",BindingFlags.Instance|BindingFlags.NonPublic);
   foreach(bool multi in new[]{false,true})foreach(int mode in new[]{0,1,2})foreach(bool defect in new[]{false,true})foreach(bool background in new[]{false,true}){
    s.TurnTableShot=true;s.TurnTableShot_PathName=folder;s.CameraAlias="CAM2";ServiceProvider.Branding.ControlMultiCamera=multi;s.FrontBackMode=mode;s.IsDefect=defect;s.Background=background;
    bool correct=true;var session=CreateSession(folder);
    for(int shot=1;shot<=72;shot++){
     s.TurnTableShot_FileName="PRODUCT-L1-"+shot.ToString("00");
     var expected=folder+"\\"+"M1"+(multi?"CAM2_":"")+s.TurnTableShot_FileName;
     foreach(string ext in new[]{"IMG.jpg","IMG.nef","IMG.cr2"}){
      var actual=(string)format.Invoke(session,new object[]{camera,ext,"",true});correct &= actual==expected;
     }
     if(!background)correct &= session.GetNextFileName("IMG.jpg",camera,"")==expected+".jpg";
    }
    Check("actual Core turntable 72 shots x 3 inputs multi="+multi+" mode="+mode+" defect="+defect+" BG="+background,correct);
   }
   s.TurnTableShot=false;s.Background=false;ServiceProvider.Branding.ControlMultiCamera=false;
   s.IsDefect=false;s.FrontBackMode=1;Check("ordinary F suffix after turntable",CreateSession(folder).GetNextFileName("IMG.jpg",camera,"").EndsWith("_F.jpg"));
   s.FrontBackMode=2;Check("ordinary B suffix after turntable",CreateSession(folder).GetNextFileName("IMG.jpg",camera,"").EndsWith("_B.jpg"));
   s.IsDefect=true;Check("ordinary D suffix after turntable",CreateSession(folder).GetNextFileName("IMG.jpg",camera,"").EndsWith("_D.jpg"));
   Console.WriteLine("BINARY_CHECKS_PASSED="+count);
  }finally{var full=Path.GetFullPath(temp);if(full.StartsWith(Path.GetFullPath(Path.GetTempPath()))&&Path.GetFileName(full).StartsWith("CloriumBinary2161-"))Directory.Delete(full,true);}
 }
}
