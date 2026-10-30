using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
class G2MaintenanceGuardCheck {
 [STAThread] static int Main() {
  try {
   string root=AppDomain.CurrentDomain.BaseDirectory;
   if(!File.Exists(Path.Combine(root,"g2-development-runtime.marker"))) throw new Exception("Missing development marker");
   Assembly core=Assembly.LoadFrom(Path.Combine(root,"CameraControl.Core.dll"));
   Type t=core.GetType("CameraControl.Core.Classes.Settings",true);
   object instance=FormatterServices.GetUninitializedObject(t);
   // A failed guard throws before any cleanup can occur: invalid retention input.
   t.GetMethod("RemoveSessionFile").Invoke(instance,new object[]{"not-a-retention-number"});
   Console.WriteLine("PASS isolated development runtime skips retention parsing, shared cleanup and installed-app restart");return 0;
  }catch(Exception e){Console.WriteLine(e);return 1;}
 }
}
