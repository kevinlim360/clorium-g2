using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Security.Cryptography;
const string Name="XamlGeneratedNamespace.GeneratedInternalTypeHelper";
string Digest(MethodDefinition m)=>m.HasBody?string.Join("|",m.Body.Instructions.Select(i=>i.OpCode.ToString()+":"+(i.Operand?.ToString()??"")))+string.Join("|",m.Body.Variables.Select(v=>v.VariableType.FullName)):"";
IEnumerable<TypeDefinition> All(IEnumerable<TypeDefinition> types){foreach(var t in types){yield return t;foreach(var n in All(t.NestedTypes))yield return n;}}
Dictionary<string,string> Bodies(ModuleDefinition module)=>All(module.Types).Where(t=>t.FullName!=Name).SelectMany(t=>t.Methods).ToDictionary(m=>m.FullName,Digest);
var output=Path.GetFullPath(args[1]);
using var template=ModuleDefinition.ReadModule(args[0]);
using var module=ModuleDefinition.ReadModule(output,new ReaderParameters{InMemory=true,ReadSymbols=true});
if(module.Types.Any(t=>t.FullName==Name)){Console.WriteLine("Core WPF helper already present.");return;}
var before=Bodies(module);
var source=template.Types.Single(t=>t.FullName==Name);
if(source.Fields.Count!=0||source.NestedTypes.Count!=0||source.Methods.Count!=6)throw new Exception("Unexpected WPF helper shape.");
var copy=new TypeDefinition(source.Namespace,source.Name,source.Attributes,module.ImportReference(source.BaseType));module.Types.Add(copy);
foreach(var old in source.Methods){
 if(old.Body.HasExceptionHandlers||old.Body.HasVariables)throw new Exception("Unexpected helper method body.");
 var method=new MethodDefinition(old.Name,old.Attributes,module.ImportReference(old.ReturnType)){ImplAttributes=old.ImplAttributes};copy.Methods.Add(method);
 foreach(var p in old.Parameters)method.Parameters.Add(new ParameterDefinition(p.Name,p.Attributes,module.ImportReference(p.ParameterType)));
 method.Body.MaxStackSize=old.Body.MaxStackSize;
 foreach(var original in old.Body.Instructions){
  var instruction=Instruction.Create(OpCodes.Nop);instruction.OpCode=original.OpCode;
  instruction.Operand=original.Operand switch {MethodReference mr=>module.ImportReference(mr),TypeReference tr=>module.ImportReference(tr),FieldReference fr=>module.ImportReference(fr),ParameterDefinition pd=>method.Parameters[pd.Index],Instruction=>throw new Exception("Unexpected helper branch."),_=>original.Operand};method.Body.Instructions.Add(instruction);
 }
}
var temp=Path.Combine(Path.GetDirectoryName(output)!,"core-compat-"+Guid.NewGuid().ToString("N")+".dll");
try{
 module.Write(temp,new WriterParameters{WriteSymbols=true});
 using(var check=ModuleDefinition.ReadModule(temp)){var after=Bodies(check);if(before.Count!=after.Count||before.Any(p=>!after.TryGetValue(p.Key,out var value)||value!=p.Value))throw new Exception("Non-helper IL changed.");}
 File.Copy(temp,output,true);File.Copy(Path.ChangeExtension(temp,".pdb"),Path.ChangeExtension(output,".pdb"),true);
 Console.WriteLine("Core WPF helper preserved; all non-helper method IL unchanged.");
}finally{if(File.Exists(temp))File.Delete(temp);if(File.Exists(Path.ChangeExtension(temp,".pdb")))File.Delete(Path.ChangeExtension(temp,".pdb"));}