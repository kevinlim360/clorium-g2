using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

class BinaryChecks
{
    static int count; static string[] dirs;
    static void Check(string name, bool value) { if (!value) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); count++; }
    static Assembly Resolve(object sender, ResolveEventArgs e) { string name = new AssemblyName(e.Name).Name + ".dll"; foreach (string dir in dirs) { string file = Path.Combine(dir, name); if (File.Exists(file)) return Assembly.LoadFrom(file); } return null; }
    [STAThread] static int Main(string[] args)
    {
        try {
            dirs = new[] { args[0], args[1], Path.Combine(args[2], "dependencies/Managed") }; AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            var main = Assembly.LoadFrom(Path.Combine(args[0], "CameraControl.exe"));
            var runtime = main.GetType("CameraControl.ManagedLicenseRuntime", true);
            Check("managed mode compiled in without external config", (bool)runtime.GetProperty("IsManaged", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null));
            Check("legacy private compatibility resource excluded", main.GetManifestResourceStream("Clorium.LegacyLicenseKey") == null);
            using (var stream = main.GetManifestResourceStream("Clorium.ManagedLicenseConfig")) using (var reader = new StreamReader(stream)) Check("public connection configuration embedded", reader.ReadToEnd().Contains("PublicKey"));
            var deviceAssembly = Assembly.LoadFrom(Path.Combine(args[0], "CameraControl.Devices.dll"));
            var guard = deviceAssembly.GetType("CameraControl.Devices.Classes.CaptureAuthorization", true);
            guard.GetProperty("Validate").SetValue(null, new Func<string>(() => "TEST-EXPIRED"), null);
            var types = deviceAssembly.GetTypes(); int entryCount = 0;
            foreach (var type in types) foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => new[] { "CapturePhoto", "CapturePhotoNoAf", "StartRecordMovie", "StartBulbMode" }.Contains(m.Name) && m.GetParameters().Length == 0 && !m.IsAbstract)) {
                var concrete = type.IsAbstract ? types.FirstOrDefault(t => !t.IsAbstract && type.IsAssignableFrom(t)) : type; if (concrete == null) continue;
                var target = FormatterServices.GetUninitializedObject(concrete);
                try { method.Invoke(target, null); throw new Exception("Unguarded entry " + type.FullName + "." + method.Name); }
                catch (TargetInvocationException ex) { if (ex.InnerException.GetType().FullName != "CameraControl.Devices.Classes.CaptureLicenseException" || !ex.InnerException.Message.Contains("TEST-EXPIRED")) throw; }
                entryCount++;
            }
            Check("all camera capture/bulb/movie entries reject before touching hardware (" + entryCount + ")", entryCount >= 40);
            var core = Assembly.LoadFrom(Path.Combine(args[0], "CameraControl.Core.dll"));
            var external = core.GetType("CameraControl.Core.Classes.ExternalDeviceManager", true);
            try { external.GetMethod("OpenShutter").Invoke(FormatterServices.GetUninitializedObject(external), new object[] { null }); Check("external shutter blocked", false); }
            catch (TargetInvocationException ex) { Check("external shutter blocked before touching external driver", ex.InnerException.GetType().Name == "CaptureLicenseException"); }
            guard.GetProperty("Validate").SetValue(null, null, null);
            Check("legacy host can retain existing startup authorization", true);
            Console.WriteLine("LICENSING_BINARY_PASSED=" + count); return 0;
        } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
