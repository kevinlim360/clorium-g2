using Mono.Cecil;
using System;
using System.Linq;
var a = AssemblyDefinition.ReadAssembly(args[0]);
var m = a.MainModule;
var typedEventHandler = m.Types.First(t => t.Namespace=="Windows.Foundation" && t.Name=="TypedEventHandler`2");
Console.WriteLine($"TypedEventHandler generic={typedEventHandler.GenericParameters.Count} methods={typedEventHandler.Methods.Count} base={typedEventHandler.BaseType}");
foreach (var x in typedEventHandler.Methods) Console.WriteLine(x.FullName);
var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, m.TypeSystem.Void);
ctor.ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
ctor.Parameters.Add(new ParameterDefinition("target", ParameterAttributes.None, m.TypeSystem.Object));
ctor.Parameters.Add(new ParameterDefinition("method", ParameterAttributes.None, m.TypeSystem.IntPtr));
typedEventHandler.Methods.Add(ctor);
for (int i=m.Types.Count-1;i>=0;i--) {
  var t=m.Types[i];
  if (t.Name=="<Module>" || (t.Namespace=="Windows.Foundation" && (t.Name.StartsWith("IAsync") || t.Name.StartsWith("Async") || t.Name.StartsWith("TypedEventHandler")))) continue;
  m.Types.RemoveAt(i);
}
a.Write(args[1]);
Console.WriteLine($"Retained {m.Types.Count} types in {a.Name.FullName}");
