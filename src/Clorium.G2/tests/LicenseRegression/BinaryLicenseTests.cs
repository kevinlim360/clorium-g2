using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;

class BinaryLicenseTests {
 static int count;
 static void Check(string name,bool value){if(!value)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);count++;}
 [STAThread] static int Main(string[] args){
  AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){string name=new AssemblyName(e.Name).Name+".dll";foreach(string dir in new[]{Path.Combine(args[0],"build-output/installed"),"C:\\Program Files (x86)\\Clorium"}){string file=Path.Combine(dir,name);if(File.Exists(file))return Assembly.LoadFrom(file);}return null;};
  string temp=Path.Combine(Path.GetTempPath(),"CloriumLicenseFixed-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  try{
   var assembly=Assembly.LoadFrom(Path.Combine(args[0],"build-output/installed/CameraControl.exe"));
   var appType=assembly.GetType("CameraControl.App",true);var app=FormatterServices.GetUninitializedObject(appType);
   var matcher=assembly.GetType("CameraControl.LicenseFileMatcher",true);
   Func<string,string,string> encrypt=(id,key)=>(string)appType.GetMethod("Encrypt").Invoke(app,new object[]{id,key});
   Func<string,string> clean=value=>(string)appType.GetMethod("RemoveSpecialCharacters").Invoke(app,new object[]{value});
   Func<string,IEnumerable<string>,IEnumerable<string>,string> find=(dir,candidateIds,keys)=>(string)matcher.GetMethod("FindMatchingFile",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{dir,candidateIds,keys,encrypt,clean});
   Func<string,string,string> license=(id,key)=>Path.Combine(temp,clean(encrypt(id,key))+".lock");
   string embedded=(string)matcher.GetMethod("ReadCompatibilityKey",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);
   Check("release compatibility configuration equals hash-verified original",embedded==File.ReadAllText(args[1])&&!string.IsNullOrEmpty(embedded));
   var ids=new[]{"TEST-BIOS","TEST-MAC-1","TEST-MAC-2","TEST-BOARD","TEST-DISK-1","TEST-DISK-2"};
   foreach(var id in ids){string path=license(id,"TEST-KEY");File.WriteAllText(path,"");Check("any matching local hardware candidate "+Array.IndexOf(ids,id),find(temp,ids,new[]{"TEST-KEY"})==path);File.Delete(path);}
   File.WriteAllText(license("OTHER-PC","TEST-KEY"),"");File.WriteAllText(Path.Combine(temp,"unrelated.lock"),"");
   Check("other PC and arbitrary lock files rejected",find(temp,ids,new[]{"TEST-KEY"})==null);
   string valid=license(ids[5],"TEST-KEY");File.WriteAllText(valid,"");Check("invalid files plus one valid license accepted",find(temp,ids,new[]{"TEST-KEY"})==valid);
   File.SetAttributes(valid,FileAttributes.Hidden|FileAttributes.ReadOnly);Check("hidden read-only license recognized",find(temp,ids,new[]{"TEST-KEY"})==valid);File.SetAttributes(valid,FileAttributes.Normal);File.Delete(valid);
   Check("wrong key rejected",find(temp,ids,new[]{"WRONG-KEY"})==null);
   Check("empty configuration rejected",find(temp,ids,new[]{"",(string)null})==null);
   Check("empty hardware list rejected",find(temp,new string[0],new[]{"TEST-KEY"})==null);
   Check("placeholder hardware rejected",find(temp,new[]{null,""," ","N/A","default STRING","To be filled by OEM"},new[]{"TEST-KEY"})==null);
   foreach(var value in new[]{"N/A","Default string","To be filled by OEM"}){var path=license(value,"TEST-KEY");File.WriteAllText(path,"");Check("placeholder issued-looking file not accepted "+value,find(temp,new[]{value},new[]{"TEST-KEY"})==null);File.Delete(path);}
   string padded=" TEST-BOARD ";var raw=license(padded,"TEST-KEY");File.WriteAllText(raw,"");Check("historical WMI whitespace spelling preserved",find(temp,new[]{padded},new[]{"TEST-KEY"})==raw);File.Delete(raw);
   string trimmed=license(padded.Trim(),"TEST-KEY");File.WriteAllText(trimmed,"");Check("trimmed hardware identifier supported",find(temp,new[]{padded},new[]{"TEST-KEY"})==trimmed);File.Delete(trimmed);
   string bios=license(ids[0],embedded);File.WriteAllText(bios,"");
   Check("direct execution without launcher environment uses original compatibility key",find(temp,ids,new[]{"",embedded})==bios);
   Check("stale override does not hide valid legacy license",find(temp,ids,new[]{"WRONG",embedded})==bios);File.Delete(bios);
   string custom=license(ids[0],"SITE-KEY");File.WriteAllText(custom,"");Check("explicit site key remains supported",find(temp,ids,new[]{"SITE-KEY",embedded})==custom);File.Delete(custom);
   Check("missing drive directory rejected",find(Path.Combine(temp,"missing"),ids,new[]{"TEST-KEY"})==null);
   string many=license(ids[3],"TEST-KEY");File.WriteAllText(many,"");var repeated=Enumerable.Repeat(ids,100).SelectMany(x=>x);Check("duplicate identities preserve match",find(temp,repeated,new[]{"TEST-KEY","TEST-KEY"})==many);
   var hardware=(List<string>)matcher.GetMethod("ReadHardwareIdentifiers",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,null);Check("host WMI enumeration completes without exposing IDs",hardware!=null);
   var method=appType.GetMethod("CheckLicense",BindingFlags.Instance|BindingFlags.NonPublic);Check("startup license method retained",method!=null);
   Check("license attribute errors handled separately",method.GetMethodBody().ExceptionHandlingClauses.Count>=4);
   Console.WriteLine("LICENSE_BINARY_CHECKS_PASSED="+count);return 0;
  }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
  finally{if(Path.GetFileName(temp).StartsWith("CloriumLicenseFixed-"))Directory.Delete(temp,true);}
 }
}
