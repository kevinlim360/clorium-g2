import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
const out=path.dirname(fileURLToPath(import.meta.url));
const source=path.resolve(process.argv[2]??path.join(out,'../../projects'));
fs.mkdirSync(out,{recursive:true});
const files={main:'CameraControl/CameraControl/MainWindow.xaml.cs',session:'CameraControl.Core/CameraControl/Core/Classes/PhotoSession.cs',helper:'CameraControl.Core/CameraControl/Core/Classes/CameraHelper.cs'};
files.menu='CameraControl/CameraControl/ViewModel/MainMenuViewModel.cs';
const texts=Object.fromEntries(Object.entries(files).map(([k,p])=>[k,fs.readFileSync(path.join(source,p),'utf8')]));
const evidence=[];
function extract(key,signature){const str=texts[key],start=str.indexOf(signature);if(start<0)throw Error(signature);const begin=str.indexOf('{',start);let depth=0,end;for(let i=begin;i<str.length;i++){if(str[i]==='{')depth++;if(str[i]==='}'&&--depth===0){end=i+1;break;}}const code=str.slice(start,end);evidence.push({file:files[key],signature,line:str.slice(0,start).split('\n').length,sha256:crypto.createHash('sha256').update(code).digest('hex')});return code;}
const front=extract('main','private async void CaptureOfFrontView(').replace('async void','async Task');
const back=extract('main','private async void CaptureOfBackView(').replace('async void','async Task');
const capture=extract('main','private async Task<bool> CaptureAsync(');
const wait=extract('main','private async Task<bool> WaitForCaptureComplete(');
const failedReset=extract('main','private void ResetFailedCaptureState(');
const bg=extract('main','private async Task<bool> CaptureBackground(');
const photo=extract('main','private void PhotoCaptured(');
const reset=photo.slice(photo.lastIndexOf('finally')+'finally'.length).trim().slice(0,-1).trim();
const format=extract('session','private string FormatFileName(');
const nextName=extract('session','public string GetNextFileName(');
const helper=extract('helper','public static async Task<bool> CaptureAsync(');
const withError=extract('helper','public static bool CaptureAsyncWithError(');
const internal=extract('helper','private static bool CaptureInternal(');
const createSession=extract('menu','public string CreateNewSessionFolder(');
const sessionGuard=texts.menu.includes('private bool CanStartNewCaptureSession(')?extract('menu','private bool CanStartNewCaptureSession('):'private bool CanStartNewCaptureSession(){return true;}';
const resetPurpose=texts.menu.includes('private static void ResetCapturePurposeForNewSession(')?extract('menu','private static void ResetCapturePurposeForNewSession('):'private static void ResetCapturePurposeForNewSession(){}';
const code=String.raw`using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PhotoSession = Session;
namespace System.Windows {
 public class Window {}
 public enum MessageBoxButton { OK } public enum MessageBoxImage { Exclamation }
 public static class MessageBox { public static void Show(string x) { } public static void Show(string x,string y,MessageBoxButton b,MessageBoxImage i) { } }
 public class Application { public static Application Current=new(); public Dispatch Dispatcher=new(); }
 public class Dispatch { public void Invoke(Action a)=>a(); }
}
public interface ICameraDevice { bool IsConnected {get;set;} bool IsBusy {get;set;} void CapturePhoto(); }
public class FakeCamera:ICameraDevice { public bool IsConnected {get;set;}=true;public bool IsBusy {get;set;} public void CapturePhoto(){if(Program.Outcome=="focus-error")throw new IOException("Simulated camera CapturePhoto failure");Program.OnPhoto();} }
public class CameraProperty { public int Counter,CounterInc=1,Delay; public bool UseExternalShutter;public object SelectedConfig; }
public class Props { public CameraProperty P=new();public CameraProperty Get(ICameraDevice d)=>P; }
public class Settings { public bool IsAestheticImprovementActive; public bool isBtnPressed,GetMeasurePicture,IsDefect,Background,TurnTableShot,ModeLighting;public bool ThumbailLoaded=true;public int FrontBackMode,LightPowerSaveTime=99999,ExternalDeviceWaitForFocus,ExternalDeviceWaitForCapture;public string CloriumID="M1_",CameraAlias="",TurnTableShot_PathName="",TurnTableShot_FileName="";public Props CameraProperties=new(); public bool UseDebugLog;public string DownloadFolder=Program.ProductRoot;public static string SessionFolder=>Path.Combine(Program.TempBase,"config");public Session DefaultSession;public int TodaySessionCounter,TotalSessionCounter,AddedSessions,LoadedSessions;public Session LoadSession(string f){LoadedSessions++;return new Session{Counter=7};}public void Add(Session s){AddedSessions++;} }
public class PluginManager {public object SelectedWindow;}
public class EditSession {public Session Session;public object Owner;public EditSession(Session s){Session=s;}}
public class Branding {public bool ControlMultiCamera,ControlLighting;}
public class Devices { public ICameraDevice SelectedCameraDevice=new FakeCamera();public Dictionary<ICameraDevice,string> LastCapturedImage=new(); }
public class Windows {public void ExecuteCommand(string c,bool b){} }
public class External {public void AssertFocus(object c){}public void OpenShutter(object c){}public void CloseShutter(object c){} }
public class Analytics {public void CameraCapture(ICameraDevice d){} }
public interface Template {string Pharse(string m,Session s,ICameraDevice d,string f,string t);}
public class CounterTemplate:Template {public string Pharse(string m,Session s,ICameraDevice d,string f,string t)=>s.Counter.ToString("000");}
public class Templates {public Dictionary<string,Template> TemplatesMap=new(){{"[Counter 3 digit]",new CounterTemplate()}};public Dictionary<string,Template> Templates=>TemplatesMap;}
public static class ServiceProvider {public static PluginManager PluginManager=new();public static Settings Settings=new();public static Branding Branding=new();public static Devices DeviceManager=new();public static Windows WindowsManager=new();public static Templates FilenameTemplateManager=new();public static External ExternalDeviceManager=new();public static Analytics Analytics=new();}
public class StaticHelper {public static StaticHelper Instance=new();public string SystemMessage="";public static string GetUniqueFilename(string prefix,int count,string ext){while(File.Exists(prefix+count+ext))count++;return prefix+count+ext;}}
public static class Log {public static void Debug(string s,bool verbose=false){}public static void Error(string s,Exception e){} }
public class LightControlWnd {public static LightControlWnd Instance=new();public void AllLightOn(){} }
public class MainMenuViewModel {public static bool IsMusinsaFrontOK,IsMusinsaBackOK;
${sessionGuard}
${resetPurpose}
${createSession}
}
public class Session {public string Name="";public List<object> Files=new();public string Folder=Program.TempFolder;public string FileNameTemplate="Barcode_[Counter 3 digit]";public int Counter;readonly object _locker=new();string _lastFilename;static List<string> RawExtensions=new(){".nef",".cr2",".cr3"};public bool AllowOverWrite;
 public string FormatOnly()=>FormatFileName(ServiceProvider.DeviceManager.SelectedCameraDevice,"IMG.JPG","temp")+".jpg";
 ${format}
 ${nextName}
}
public class Toggle {public bool? IsChecked=false;}
public class RemoveBackgroundWnd {public Task PrecessingOneImage(string f)=>Task.CompletedTask;public Task PrecessingTwoImages(string f)=>Task.CompletedTask;}
public class MainCapture {
 bool is270CaptureMode,isCalibrationMode;DateTime lastPowerSaveTime=DateTime.Now;
 public Toggle ModeA=new(){IsChecked=true},Bg_Auto=new();void Backgroud_LightOff(){}void Backgroud_LightOn(){}
 void setExifOrientation(bool x){} public Task Front(bool rotate=false,bool bg=false)=>CaptureOfFrontView(rotate,bg);public Task Back(bool rotate=false,bool bg=false)=>CaptureOfBackView(rotate,bg);public Task<bool> Direct(bool wait=true)=>CaptureAsync(wait);
 public void PhotoFinally(){var eventArgs=new {CameraDevice=ServiceProvider.DeviceManager.SelectedCameraDevice};${reset}}
 ${front}
 ${back}
 ${capture}
 ${wait}
 ${failedReset}
 ${bg}
}
public static class CameraHelper {
 ${helper}
 ${withError}
 ${internal}
}
public class Result {public string test {get;set;}public bool passed {get;set;}public object details {get;set;}}

`;
// Avoid colliding type and member names in stub only.
const prefix=code.slice(0,code.indexOf('public static class Program {'));
const tests=fs.readFileSync(path.join(out,'Scenarios.cs.txt'),'utf8');
fs.writeFileSync(path.join(out,'Program.cs'),(prefix+tests).replaceAll('class Templates {','class TemplateRegistry {').replaceAll('static Templates FilenameTemplateManager=new()','static TemplateRegistry FilenameTemplateManager=new()').replace('public void CapturePhoto(){if(Program.Outcome=="focus-error")throw new IOException("Simulated camera CapturePhoto failure");Program.OnPhoto();}','public void CapturePhoto(){Program.DeviceCapture();}'));
fs.writeFileSync(path.join(out,'DefectHarness.csproj'),'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><EnableNETAnalyzers>false</EnableNETAnalyzers><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" /></ItemGroup></Project>');
fs.writeFileSync(path.join(out,'NuGet.Config'),'<configuration><packageSources><clear /></packageSources></configuration>');
fs.writeFileSync(path.join(out,'evidence.json'),JSON.stringify(evidence,null,2));
