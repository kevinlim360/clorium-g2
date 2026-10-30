using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Security.Cryptography;
if(args.Length!=1)throw new ArgumentException("Provide source root.");
var root=Path.GetFullPath(args[0]);
using var camera=ModuleDefinition.ReadModule(Path.Combine(root,"build-output/installed/CameraControl.exe"));
var init=camera.Types.Single(t=>t.FullName=="CameraControl.App").Methods.Single(m=>m.Name=="InitializeComponent");
if(init.Body.Instructions.Count(i=>i.Operand is MethodReference m&&m.Name=="add_Startup")!=1 ||
 init.Body.Instructions.Count(i=>i.OpCode==OpCodes.Ldftn&&i.Operand is MethodReference m&&m.Name=="Application_Startup")!=1)throw new Exception("CameraControl Startup not connected exactly once.");
Console.WriteLine("PASS: rebuilt CameraControl Startup connects Application_Startup exactly once.");
using var center=ModuleDefinition.ReadModule(Path.Combine(root,"build-output/installed/Clorium Centering.exe"));
var main=center.Types.Single(t=>t.FullName=="Clorium_Centering.App").Methods.Single(m=>m.Name=="Main");
var first=main.Body.Instructions.First(i=>i.Operand is MethodReference).Operand as MethodReference;
if(first?.FullName!="System.Void Costura.AssemblyLoader::Attach(System.Boolean)")throw new Exception("Centering loader not initialized before UI.");
foreach(var r in center.Resources.OfType<EmbeddedResource>().Where(r=>r.Name.StartsWith("costura."))){
 var source=File.ReadAllBytes(Path.Combine(root,"resources/Centering",r.Name));
 if(!SHA256.HashData(source).SequenceEqual(SHA256.HashData(r.GetResourceData())))throw new Exception("Changed embedded dependency: "+r.Name);
}
Console.WriteLine("PASS: rebuilt Centering initializes embedded loader before UI; all embedded vendor resources match supplied source files.");
foreach(var f in new[]{"CameraControl.exe","CameraControl.Core.dll","CameraControl.Devices.dll","CameraControl.Plugins.dll","Canon.Eos.Framework.dll","PortableDeviceLib.dll","ZoomAndPan.dll","Clorium AutoSizing.exe","Clorium Centering.exe"}){
 if(!File.Exists(Path.Combine(root,"build-output/installed",f)))throw new Exception("Missing output: "+f);
}
if(!File.Exists(Path.Combine(root,"build-output/standalone/Clorium AutoSizing.exe")))throw new Exception("Missing standalone variant");
Console.WriteLine("PASS: all 9 installed outputs and separate AutoSizing variant present. Products were not executed.");
