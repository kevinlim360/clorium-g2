using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;
using System.Text.Json;
using System.Security.Cryptography;
var root=args[0];var output=Path.Combine(root,"build-output/installed");
using var main=AssemblyDefinition.ReadAssembly(Path.Combine(output,"CameraControl.exe"));
using var core=AssemblyDefinition.ReadAssembly(Path.Combine(output,"CameraControl.Core.dll"));
var checks=new List<object>();
void Check(string name,bool passed){checks.Add(new{name,passed});if(!passed)throw new Exception(name);}
Check("Startup label constant is 4.7.1.5",main.MainModule.Types.Single(t=>t.FullName=="CameraControl.StartUpWindow").Methods.Single(m=>m.Name=="Window_Loaded").Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldstr&&Equals(i.Operand,"4.7.1.5")));
Check("About reads product version",main.MainModule.Types.Single(t=>t.FullName=="CameraControl.windows.AboutWnd").Methods.Single(m=>m.Name==".ctor").Body.Instructions.Any(i=>i.Operand is MethodReference mr&&mr.Name=="get_ProductVersion"));
var format=core.MainModule.Types.Single(t=>t.FullName=="CameraControl.Core.Classes.PhotoSession").Methods.Single(m=>m.Name=="FormatFileName");var calls=format.Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>((MethodReference)i.Operand).Name).ToList();
Check("Compiled naming tests defect before front/back",calls.IndexOf("get_IsDefect")>=0&&calls.IndexOf("get_IsDefect")<calls.IndexOf("get_FrontBackMode"));
var settings=core.MainModule.Types.Single(t=>t.FullName=="CameraControl.Core.Classes.Settings");foreach(var name in new[]{"isBtnPressed","ThumbailLoaded"})Check("Compiled Settings notifies "+name,settings.Methods.Single(m=>m.Name=="set_"+name).Body.Instructions.Any(i=>i.Operand is MethodReference mr&&mr.Name=="NotifyPropertyChanged"));
foreach(var a in new[]{main,core}){Check(a.Name.Name+" identity remains 2.1.6.0",a.Name.Version.ToString()=="2.1.6.0");var v=FileVersionInfo.GetVersionInfo(a.MainModule.FileName);Check(a.Name.Name+" file and product versions",v.FileVersion=="4.7.1.5"&&v.ProductVersion=="4.7.1.5");}
Check("Main Core reference compatible",main.MainModule.AssemblyReferences.Single(a=>a.Name=="CameraControl.Core").Version.ToString()=="2.1.6.0");
Check("Core Devices reference compatible",core.MainModule.AssemblyReferences.Single(a=>a.Name=="CameraControl.Devices").Version.ToString()=="2.1.6.0");
var menu=main.MainModule.Types.Single(t=>t.FullName=="CameraControl.ViewModel.MainMenuViewModel");
var create=menu.Methods.Single(m=>m.Name=="CreateNewSessionFolder");
var createCalls=create.Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>((MethodReference)i.Operand).Name).ToList();
Check("Session creation checks capture guard first",createCalls.First()=="CanStartNewCaptureSession");
Check("Session reset follows successful default session switch",createCalls.IndexOf("ResetCapturePurposeForNewSession")>createCalls.IndexOf("set_DefaultSession"));
var reset=menu.Methods.Single(m=>m.Name=="ResetCapturePurposeForNewSession");
var setters=reset.Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>((MethodReference)i.Operand).Name).ToList();
Check("Session reset clears all four purpose fields",new[]{"set_IsDefect","set_FrontBackMode","set_GetMeasurePicture","set_Background"}.All(setters.Contains));
var newSession=menu.Methods.Single(m=>m.Name=="NewSession");var newCalls=newSession.Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>((MethodReference)i.Operand).Name).ToList();
Check("Menu checks guard before previous product API",newCalls.First()=="CanStartNewCaptureSession"&&newCalls.IndexOf("CanStartNewCaptureSession")<newCalls.IndexOf("API_PostMeasureInform"));
var guard=menu.Methods.Single(m=>m.Name=="CanStartNewCaptureSession");var guardCalls=guard.Body.Instructions.Where(i=>i.Operand is MethodReference).Select(i=>((MethodReference)i.Operand).Name).ToList();
Check("Compiled product guard checks busy and thumbnail",guardCalls.Contains("get_isBtnPressed")&&guardCalls.Contains("get_ThumbailLoaded"));
var evidence=JsonDocument.Parse(File.ReadAllText(args[1]));foreach(var e in evidence.RootElement.EnumerateArray()){var text=File.ReadAllText(Path.Combine(root,"projects",e.GetProperty("file").GetString()!));var signature=e.GetProperty("signature").GetString()!;int start=text.IndexOf(signature),begin=text.IndexOf('{',start),depth=0,end=0;for(int i=begin;i<text.Length;i++){if(text[i]=='{')depth++;if(text[i]=='}'&&--depth==0){end=i+1;break;}}var hash=Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text[start..end])));Check("Tested method body unchanged: "+signature,hash==e.GetProperty("sha256").GetString());}
var report=new{version="4.7.1.5",passed=checks.Count,checks,artifacts=new[]{"CameraControl.exe","CameraControl.Core.dll","CameraControl.pdb","CameraControl.Core.pdb"}.Select(n=>new{name=n,sha256=Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(output,n))))})};File.WriteAllText(args[2],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("RELEASE_AUDIT_PASSED="+checks.Count);
