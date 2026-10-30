using System;
using System.Linq;
using Mono.Cecil;
var assembly = AssemblyDefinition.ReadAssembly(args[0], new ReaderParameters { ReadSymbols = true });
var module = assembly.MainModule;
// The portable build references a minimal Windows.winmd for Roslyn's WinRT event
// signatures. Runtime type identities belong to the 22621 SDK FoundationContract.
var foundation = new AssemblyNameReference("Windows.Foundation.FoundationContract", new Version(4,0,0,0)) { IsWindowsRuntime = true };
module.AssemblyReferences.Add(foundation);
var mscorlib = module.AssemblyReferences.Single(r=>r.Name=="mscorlib");
foreach (var reference in module.GetTypeReferences())
{
    if (reference.Scope is AssemblyNameReference scope && scope.Name=="Windows") reference.Scope=foundation;
    if (reference.Scope is AssemblyNameReference scope2 && scope2.Name=="System.Runtime.InteropServices.WindowsRuntime") reference.Scope=mscorlib;
}
for (int i=module.AssemblyReferences.Count-1;i>=0;i--)
    if (module.AssemblyReferences[i].Name is "Windows" or "System.Runtime.InteropServices.WindowsRuntime") module.AssemblyReferences.RemoveAt(i);
foreach (var type in module.Types.Where(t => t.Namespace == "WIA" && !t.IsEnum))
{
    type.IsBeforeFieldInit = false;
    if (new[] { "DeviceInfos", "Items", "Properties", "Vector" }.Contains(type.Name))
        for (var i=type.Interfaces.Count-1; i>=0; i--)
            if (type.Interfaces[i].InterfaceType.FullName == "System.Collections.IEnumerable") type.Interfaces.RemoveAt(i);
    foreach (var method in type.Methods)
    {
        if (method.Name.StartsWith("_VtblGap"))
            method.Attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
    }
}
var events = module.GetType("WIA._IDeviceManagerEvents_Event");
foreach (var method in events.Methods.Where(m => m.Name == "add_OnEvent" || m.Name == "remove_OnEvent")) { method.IsSpecialName = false; foreach (var p in method.Parameters) p.Name = null; }
for (int i=events.CustomAttributes.Count-1;i>=0;i--)
    if(events.CustomAttributes[i].AttributeType.FullName == "System.Runtime.InteropServices.GuidAttribute") events.CustomAttributes.RemoveAt(i);
var indexer = module.GetType("WIA.IProperties");
var getter = indexer.Methods.Single(m=>m.Name=="get_Item"); getter.IsSpecialName = true;
if (!indexer.Properties.Any(p=>p.Name=="Item"))
{
    var prop = new PropertyDefinition("Item", PropertyAttributes.None, getter.ReturnType);
    prop.Parameters.Add(new ParameterDefinition("Index", ParameterAttributes.None, getter.Parameters[0].ParameterType));
    prop.GetMethod = getter;
    indexer.Properties.Add(prop);
}
var value = module.GetType("WIA.IProperty");
var setter = value.Methods.Single(m=>m.Name=="set_Value"); setter.IsSpecialName = true;
value.Properties.Single(p=>p.Name=="Value").SetMethod = setter;
var handler = module.GetType("WIA._IDeviceManagerEvents_OnEventEventHandler");
for (int i=handler.Methods.Count-1;i>=0;i--)
    if (handler.Methods[i].Name is "BeginInvoke" or "EndInvoke") handler.Methods.RemoveAt(i);
var ctor = handler.Methods.Single(m=>m.Name==".ctor");
ctor.Parameters[1].ParameterType = module.TypeSystem.UIntPtr;
foreach (var p in ctor.Parameters) p.Name = null;
ctor.Attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
var invoke = handler.Methods.Single(m=>m.Name=="Invoke");
invoke.Attributes = MethodAttributes.Public | MethodAttributes.Virtual;
assembly.Write(args[1], new WriterParameters { WriteSymbols = true });
Console.WriteLine("WIA metadata compatibility pass applied");
