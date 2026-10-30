using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Windows.Forms;
using CloriumUpdate;

class WindowsUpdaterTests {
 [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)] static extern IntPtr CreateFile(string path,uint access,uint share,IntPtr security,uint creation,uint flags,IntPtr template);
 [System.Runtime.InteropServices.DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr CreateFileMapping(IntPtr file,IntPtr security,uint protection,uint sizeHigh,uint sizeLow,string name);
 [System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 static int count;static string temp,original,r1,r2;
 static Dictionary<string,byte[]> files;
 static void Check(string name,bool result){if(!result)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);count++;}
 static Dictionary<string,string> Snapshot(string root){return UpdateEngine.Names.ToDictionary(n=>n,n=>File.Exists(Path.Combine(root,n))?UpdateEngine.FileHash(Path.Combine(root,n)):null);}
 static bool Equal(string root,Dictionary<string,string> expected){return Snapshot(root).All(p=>p.Value==expected[p.Key]);}
 static void Reject(string name,Action action){bool rejected=false;try{action();}catch(Exception){rejected=true;}Check(name,rejected);}
 static string Fixture(string source,bool symbols){string root=Path.Combine(temp,Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(root);foreach(string name in UpdateEngine.Names){string from=Path.Combine(source,name);if(name.StartsWith("Clorium AutoSizing")&&!File.Exists(from))from=Path.Combine(original,name);if((symbols||!name.EndsWith(".pdb"))&&File.Exists(from))File.Copy(from,Path.Combine(root,name));}File.WriteAllText(Path.Combine(root,"settings-canary.json"),"KEEP-SETTINGS");File.WriteAllText(Path.Combine(root,"customer-license.lock"),"KEEP-LICENSE");return root;}
 static bool Applied(string root){return UpdateEngine.Names.All(n=>UpdateEngine.FileHash(Path.Combine(root,n))==UpdateEngine.Hash(files[n]));}
 static UpdateEngine NewEngine(Assembly assembly){return new UpdateEngine(assembly.GetManifestResourceStream("UpdatePayload"));}
 [STAThread] static int Main(string[] args){
  temp=Path.Combine(Path.GetTempPath(),"CWU-"+Guid.NewGuid().ToString("N").Substring(0,8));Directory.CreateDirectory(temp);
  try{
   original=args[1];r1=args[2];r2=args[3];var assembly=Assembly.LoadFrom(args[0]);var engine=NewEngine(assembly);
   files=new Dictionary<string,byte[]>();using(var zip=new ZipArchive(assembly.GetManifestResourceStream("UpdatePayload"),ZipArchiveMode.Read))foreach(string name in UpdateEngine.Names)using(var stream=zip.GetEntry("files/"+name).Open())using(var memory=new MemoryStream()){stream.CopyTo(memory);files[name]=memory.ToArray();}
   foreach(var item in new[]{new{label="legacy original",source=original,symbols=false},new{label="Oct19 4.7.1.4",source=r1,symbols=true},new{label="Oct22 4.7.1.4",source=r2,symbols=true},new{label="Oct23 license 4.7.1.4",source=args.Length>4?args[4]:r2,symbols=true},new{label="aesthetic 4.7.1.4",source=args[5],symbols=true}}){
    var root=Fixture(item.source,item.symbols);var before=Snapshot(root);string backup=engine.Install(root);
    Check(item.label+" update exact payload",Applied(root));
    Check(item.label+" preserves unrelated settings and license",File.ReadAllText(Path.Combine(root,"settings-canary.json"))=="KEEP-SETTINGS"&&File.ReadAllText(Path.Combine(root,"customer-license.lock"))=="KEEP-LICENSE");
    Check(item.label+" backup exact originals",before.All(p=>p.Value==null?!File.Exists(Path.Combine(backup,p.Key)):UpdateEngine.FileHash(Path.Combine(backup,p.Key))==p.Value));
    Check(item.label+" idempotent reapply",engine.Install(root)==null&&Directory.GetDirectories(Path.Combine(root,"UpdateBackups")).Length==1);
    engine.Rollback(root,backup);Check(item.label+" rollback restores original hashes and missing PDBs",Equal(root,before));
   }
   var missingAuto=Fixture(r2,true);File.Delete(Path.Combine(missingAuto,"Clorium AutoSizing.exe"));Reject("missing installed AutoSizing rejected",()=>engine.Install(missingAuto));
   var lockedAuto=Fixture(r2,true);var autoBefore=Snapshot(lockedAuto);using(var handle=File.Open(Path.Combine(lockedAuto,"Clorium AutoSizing.exe"),FileMode.Open,FileAccess.Read,FileShare.Read)){Reject("locked AutoSizing rejected before replacing main",()=>engine.Install(lockedAuto));Check("locked AutoSizing leaves all six files unchanged",Equal(lockedAuto,autoBefore));}
   var mapped=Fixture(r2,true);var mappedBefore=Snapshot(mapped);IntPtr fileHandle=CreateFile(Path.Combine(mapped,"CameraControl.exe"),0x80000000,7,IntPtr.Zero,3,0,IntPtr.Zero);if(fileHandle.ToInt64()==-1)throw new Exception("Mapping fixture open failed");IntPtr section=CreateFileMapping(fileHandle,IntPtr.Zero,2,0,0,null);CloseHandle(fileHandle);if(section==IntPtr.Zero)throw new Exception("Mapping fixture failed");string mappedBackup=null;
   try{mappedBackup=engine.Install(mapped);Check("memory-mapped image updated by atomic replacement",Applied(mapped));}finally{CloseHandle(section);}
   engine.Rollback(mapped,mappedBackup);Check("memory-mapped update rollback exact originals",Equal(mapped,mappedBefore));Check("atomic update leaves no staging files",Directory.GetFiles(mapped,"*.clorium-*.tmp").Length==0);
   var locked=Fixture(r2,true);var lockedBefore=Snapshot(locked);using(var stream=File.Open(Path.Combine(locked,"CameraControl.pdb"),FileMode.Open,FileAccess.Read,FileShare.Read)){Reject("locked target rejected",()=>engine.Install(locked));Check("locked target leaves every file unchanged",Equal(locked,lockedBefore));}
   var unknown=Fixture(r2,true);using(var stream=new FileStream(Path.Combine(unknown,"CameraControl.exe"),FileMode.Append)){stream.WriteByte(0);}var unknownBefore=Snapshot(unknown);Reject("unknown same-version build rejected",()=>engine.Install(unknown));Check("unknown build preserved",Equal(unknown,unknownBefore));
   var mixed=Fixture(r2,true);File.Copy(Path.Combine(original,"CameraControl.Core.dll"),Path.Combine(mixed,"CameraControl.Core.dll"),true);Reject("mixed product file versions rejected",()=>engine.Install(mixed));
   var missing=Fixture(r2,true);File.Delete(Path.Combine(missing,"CameraControl.exe"));Reject("missing product rejected",()=>engine.Install(missing));
   Reject("drive root rejected",()=>engine.Install(Path.GetPathRoot(temp)));
   var corrupt=Fixture(r2,true);string backupCorrupt=engine.Install(corrupt);using(var stream=new FileStream(Path.Combine(backupCorrupt,"CameraControl.exe"),FileMode.Append)){stream.WriteByte(0);}Reject("corrupt backup rejected",()=>engine.Rollback(corrupt,backupCorrupt));Check("corrupt backup rejection preserves current release",Applied(corrupt));
   var later=Fixture(r2,true);string laterBackup=engine.Install(later);using(var stream=new FileStream(Path.Combine(later,"CameraControl.exe"),FileMode.Append)){stream.WriteByte(0);}Reject("rollback refuses changed subsequent release",()=>engine.Rollback(later,laterBackup));
   var wrong=Fixture(r2,true);Reject("foreign backup folder rejected",()=>engine.Rollback(wrong,backupCorrupt));
   var invalidList=Fixture(r2,true);string invalidBackup=engine.Install(invalidList);var xml=XDocument.Load(Path.Combine(invalidBackup,"backup.xml"));xml.Root.Elements("File").First().SetAttributeValue("name","../outside.exe");xml.Save(Path.Combine(invalidBackup,"backup.xml"));Reject("backup path traversal rejected",()=>engine.Rollback(invalidList,invalidBackup));Check("path traversal rejection preserves current release",Applied(invalidList));
   using(var memory=new MemoryStream()) {
    using(var output=new ZipArchive(memory,ZipArchiveMode.Create,true))using(var input=new ZipArchive(assembly.GetManifestResourceStream("UpdatePayload"),ZipArchiveMode.Read))foreach(var entry in input.Entries){var created=output.CreateEntry(entry.FullName);using(var dest=created.Open())using(var source=entry.Open()){source.CopyTo(dest);if(entry.FullName=="files/CameraControl.exe")dest.WriteByte(0);}}
    memory.Position=0;Reject("corrupted embedded payload rejected before installation",()=>new UpdateEngine(memory));
   }
   using(var form=new UpdateForm(NewEngine(assembly))){Check("Windows form has update and rollback buttons",form.Controls.OfType<Button>().Any(b=>b.Text=="업데이트")&&form.Controls.OfType<Button>().Any(b=>b.Text=="이전 파일 복원"));Check("Windows form defaults to existing Clorium install path",form.Controls.OfType<TextBox>().Any(b=>b.Text.EndsWith("\\Clorium")));}
   Check("delivered EXE requests Windows administrator elevation",Encoding.UTF8.GetString(File.ReadAllBytes(args[0])).Contains("requireAdministrator"));
   Check("delivered EXE version 4.7.1.5",System.Diagnostics.FileVersionInfo.GetVersionInfo(args[0]).FileVersion=="4.7.1.5");
   Console.WriteLine("WINDOWS_UPDATER_CHECKS_PASSED="+count);return 0;
  }catch(Exception error){Console.Error.WriteLine(error);return 1;}
  finally{if(Path.GetFileName(temp).StartsWith("CWU-"))Directory.Delete(temp,true);}
 }
}
