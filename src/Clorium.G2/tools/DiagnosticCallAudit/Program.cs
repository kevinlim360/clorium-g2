using Mono.Cecil;
using System.Text.Json;
if(args.Length!=2)return 2;
using var asm=AssemblyDefinition.ReadAssembly(args[0],new ReaderParameters{ReadSymbols=false});
IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types){foreach(var t in types){yield return t;foreach(var n in Types(t.NestedTypes))yield return n;}}
var calls=new List<object>();int debugCount=0,traceCount=0;
foreach(var t in Types(asm.MainModule.Types))foreach(var m in t.Methods.Where(x=>x.HasBody)){
 var relevant=m.Body.Instructions.Where(i=>i.Operand is MethodReference mr && (mr.DeclaringType.FullName=="System.Diagnostics.Debug"||mr.DeclaringType.FullName=="System.Diagnostics.Trace")).Select(i=>(MethodReference)i.Operand).ToArray();
 if(relevant.Length==0)continue;debugCount+=relevant.Count(x=>x.DeclaringType.FullName=="System.Diagnostics.Debug");traceCount+=relevant.Count(x=>x.DeclaringType.FullName=="System.Diagnostics.Trace");
 calls.Add(new{caller=m.FullName,targets=relevant.Select(x=>x.FullName).ToArray()});
}
var report=new{assembly=asm.Name.FullName,debugCount,traceCount,calls};File.WriteAllText(args[1],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"{asm.Name.Name}: Debug calls {debugCount}, Trace calls {traceCount}.");return 0;
