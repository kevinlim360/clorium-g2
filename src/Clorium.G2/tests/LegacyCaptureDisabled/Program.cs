using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

class VerifyLegacy
{
    static string root;
    static int count;
    static Assembly Resolve(object sender, ResolveEventArgs e)
    {
        string name = new AssemblyName(e.Name).Name + ".dll";
        string[] folders = { "dependencies/Managed", "projects/CameraControl/bin/Release/net48", "projects/CameraControl.Core/bin/Release/net48" };
        foreach (string folder in folders)
        {
            string path = Path.Combine(root, folder, name);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }
    static void Test(Assembly asm, string typeName, string method, object[] args)
    {
        Type type = asm.GetType(typeName, true);
        object obj = FormatterServices.GetUninitializedObject(type);
        MethodInfo member = type.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (member == null) throw new Exception("Missing " + method);
        object value = member.Invoke(obj,args);
        System.Threading.Tasks.Task task = value as System.Threading.Tasks.Task;
        if (task != null) task.GetAwaiter().GetResult();
        Console.WriteLine("PASS no camera/state access: " + type.Name + "." + method);
        count++;
    }
    static int Main(string[] args)
    {
        root=args[0];
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        Assembly asm=Assembly.LoadFrom(Path.Combine(root,"projects/CameraControl/bin/Release/net48/CameraControl.exe"));
        try
        {
            string prefix="CameraControl.ViewModel.";
            string[] focus={"StartFocusStacking","StartSimpleFocusStacking","PreviewFocusStacking","PreviewSimpleFocusStacking","GenerateFocusStackingImage","RecordMovie","RecordMovieThread","RecordMovie_Turntable"};
            foreach(string method in focus) Test(asm,prefix+"LiveViewViewModel",method,new object[0]);
            Test(asm,prefix+"LiveViewViewModel","FocusStackingTimer_Elapsed",new object[]{null,null});
            foreach(string method in new[]{"RecordMovie_Start","ThreadRecordMovie_Start"}) Test(asm,prefix+"TurnTableViewModel",method,new object[0]);
            foreach(string method in new[]{"RecordMovie","RecordMovieThread"}) Test(asm,prefix+"SimpleLiveViewViewModel",method,new object[0]);
            foreach(string method in new[]{"StartRecord","StartRecordThread"}) Test(asm,prefix+"MultipleLiveViewViewModel",method,new object[0]);
            Test(asm,prefix+"WebCamViewModel","ExecuteRecord",new object[0]);
            Assembly devices=Assembly.LoadFrom(Path.Combine(root,"projects/CameraControl.Devices/bin/Release/net48/CameraControl.Devices.dll"));
            Type policy=devices.GetType("CameraControl.Devices.LegacyCapturePolicy",true);
            foreach(string name in new[]{"FocusStackingEnabled","MovieRecordingEnabled"})
                if ((bool)policy.GetProperty(name).GetValue(null,null)) throw new Exception(name+" must be disabled");
            try { policy.GetMethod("RequireMovieRecording").Invoke(null,null); throw new Exception("Device guard did not throw"); }
            catch(TargetInvocationException ex) { if (!(ex.InnerException is NotSupportedException)) throw; }
            Console.WriteLine("PASS device-level rejection; total checks: " + (count+3));
            if(args.Length > 2)
            {
                string module=args[2];
                Assembly core=Assembly.LoadFrom(Path.Combine(root,"projects/CameraControl.Core/bin/Release/net48/CameraControl.Core.dll"));
                MethodInfo run=core.GetType("CameraControl.Core.Classes.CloriumStackProcess",true).GetMethod("RunAsync");
                object[] call={args[1],Path.Combine(module,"clorium_stack.py"),
                    new[]{Path.Combine(module,"verification-fixtures/a.png"),Path.Combine(module,"verification-fixtures/b.png")},
                    Path.Combine(module,"verification-fixtures/bridge-result.png"),null,System.Threading.CancellationToken.None};
                var task=(System.Threading.Tasks.Task<string>)run.Invoke(null,call);
                string output=task.GetAwaiter().GetResult();
                if(!File.Exists(output)) throw new Exception("Bridge did not produce output");
                Console.WriteLine("PASS C# -> Python CLI -> saved PNG: " + output);
            }
            return 0;
        }
        catch(Exception e){Console.WriteLine(e);return 1;}
    }
}
