using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

if(args.Length!=3) { Console.Error.WriteLine("Usage: AssemblyAudit ORIGINAL REBUILT NEW_REPORT.json"); return 2; }
if(File.Exists(args[2])) { Console.Error.WriteLine("Report already exists; use a new path."); return 2; }
var original=Inspect(args[0]); var rebuilt=Inspect(args[1]);
var origApi=((IEnumerable<string>)original["publicApi"]).ToHashSet();
var buildApi=((IEnumerable<string>)rebuilt["publicApi"]).ToHashSet();
var origRes=(SortedDictionary<string,string>)original["resources"];
var buildRes=(SortedDictionary<string,string>)rebuilt["resources"];
var result=new {original,rebuilt,missingPublicApi=origApi.Except(buildApi).Order().ToArray(),addedPublicApi=buildApi.Except(origApi).Order().ToArray(),resourceBytesMatch=origRes.Count==buildRes.Count && origRes.All(kv=>buildRes.TryGetValue(kv.Key,out var value)&&value==kv.Value)};
File.WriteAllText(args[2],JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"API missing={result.missingPublicApi.Length}, added={result.addedPublicApi.Length}; resourcesEqual={result.resourceBytesMatch}");

return 0;

static string HashFile(string path) { using var stream=File.OpenRead(path);return Convert.ToHexStringLower(SHA256.HashData(stream)); }

static Dictionary<string,object> Inspect(string path) {
 using var stream=File.OpenRead(path);using var pe=new PEReader(stream);var md=pe.GetMetadataReader();var asm=md.GetAssemblyDefinition();var provider=new Names();
 bool Visible(TypeDefinitionHandle h) {
  var t=md.GetTypeDefinition(h);var v=t.Attributes&TypeAttributes.VisibilityMask;
  return v==TypeAttributes.Public || ((v is TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem) && Visible(t.GetDeclaringType()));
 }
 string TypeName(EntityHandle h) => h.IsNil ? "" : h.Kind switch {HandleKind.TypeDefinition=>provider.GetTypeFromDefinition(md,(TypeDefinitionHandle)h,0),HandleKind.TypeReference=>provider.GetTypeFromReference(md,(TypeReferenceHandle)h,0),HandleKind.TypeSpecification=>provider.GetTypeFromSpecification(md,null,(TypeSpecificationHandle)h,0),_=>""};
 var api=new SortedSet<string>();
 foreach(var handle in md.TypeDefinitions) {
  if(!Visible(handle))continue;
  var t=md.GetTypeDefinition(handle);var name=TypeName(handle);
  api.Add($"type {name} flags={t.Attributes} base={TypeName(t.BaseType)} interfaces={string.Join(',',t.GetInterfaceImplementations().Select(h=>TypeName(md.GetInterfaceImplementation(h).Interface)).Order())}");
  foreach(var h in t.GetFields()) {
   var f=md.GetFieldDefinition(h);var vis=f.Attributes&FieldAttributes.FieldAccessMask;
   if(vis is not (FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem))continue;
   string constant="";if(!f.GetDefaultValue().IsNil){var c=md.GetConstant(f.GetDefaultValue());constant=$"={c.TypeCode}:sha256:{Convert.ToHexStringLower(SHA256.HashData(md.GetBlobBytes(c.Value)))}";}
   api.Add($"field {name}::{md.GetString(f.Name)} {f.DecodeSignature(provider,null)} flags={f.Attributes}{constant}");
  }
  foreach(var h in t.GetMethods()) {
   var m=md.GetMethodDefinition(h);var vis=m.Attributes&MethodAttributes.MemberAccessMask;
   if(vis is not (MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem))continue;
   var sig=m.DecodeSignature(provider,null);
   var ps=m.GetParameters().Select(ph=>md.GetParameter(ph)).Select(p=>$"{p.SequenceNumber}:{p.Attributes}:{md.GetString(p.Name)}");
   api.Add($"method {name}::{md.GetString(m.Name)}`{sig.GenericParameterCount}({string.Join(',',sig.ParameterTypes)}) -> {sig.ReturnType} flags={m.Attributes} params={string.Join(',',ps)}");
  }
 }
 var resources=new SortedDictionary<string,string>();
 var resourceSizes=new SortedDictionary<string,int>();
 foreach(var h in md.ManifestResources) {
  var r=md.GetManifestResource(h);if(!r.Implementation.IsNil)continue;
  var block=pe.GetSectionData(pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress).GetReader(checked((int)r.Offset),pe.PEHeaders.CorHeader.ResourcesDirectory.Size-checked((int)r.Offset));
  var bytes=block.ReadBytes(block.ReadInt32());resources[md.GetString(r.Name)]=Convert.ToHexStringLower(SHA256.HashData(bytes));resourceSizes[md.GetString(r.Name)]=bytes.Length;
 }
 return new(){{"path",path},{"sha256",HashFile(path)},{"assemblyName",md.GetString(asm.Name)},{"assemblyVersion",asm.Version.ToString()},{"assemblyCulture",md.GetString(asm.Culture)},{"assemblyFlags",asm.Flags.ToString()},{"assemblyPublicKey",Convert.ToHexStringLower(md.GetBlobBytes(asm.PublicKey))},{"machine",pe.PEHeaders.CoffHeader.Machine.ToString()},{"subsystem",pe.PEHeaders.PEHeader!.Subsystem.ToString()},{"corFlags",pe.PEHeaders.CorHeader!.Flags.ToString()},{"metadataVersion",md.MetadataVersion},{"typeCount",md.TypeDefinitions.Count},{"methodCount",md.MethodDefinitions.Count},{"publicApi",api.ToArray()},{"resources",resources},{"resourceSizes",resourceSizes}};
}
sealed class Names:ISignatureTypeProvider<string,object?> {
 public string GetArrayType(string element,ArrayShape shape)=>element+"["+new string(',',shape.Rank-1)+"]";
 public string GetByReferenceType(string element)=>element+"&";
 public string GetFunctionPointerType(MethodSignature<string> s)=>"fn("+string.Join(',',s.ParameterTypes)+")->"+s.ReturnType;
 public string GetGenericInstantiation(string type,ImmutableArray<string> args)=>type+"<"+string.Join(',',args)+">";
 public string GetGenericMethodParameter(object? context,int index)=>"!!"+index;
 public string GetGenericTypeParameter(object? context,int index)=>"!"+index;
 public string GetModifiedType(string modifier,string element,bool required)=>element+(required?" modreq(":" modopt(")+modifier+")";
 public string GetPinnedType(string element)=>element+" pinned";
 public string GetPointerType(string element)=>element+"*";
 public string GetPrimitiveType(PrimitiveTypeCode code)=>code.ToString();
 public string GetSZArrayType(string element)=>element+"[]";
 public string GetTypeFromDefinition(MetadataReader md,TypeDefinitionHandle h,byte raw) {var t=md.GetTypeDefinition(h);var p=t.GetDeclaringType();return !p.IsNil?GetTypeFromDefinition(md,p,raw)+"+"+md.GetString(t.Name):(string.IsNullOrEmpty(md.GetString(t.Namespace))?"":md.GetString(t.Namespace)+".")+md.GetString(t.Name);}
 public string GetTypeFromReference(MetadataReader md,TypeReferenceHandle h,byte raw) {var t=md.GetTypeReference(h);return t.ResolutionScope.Kind==HandleKind.TypeReference?GetTypeFromReference(md,(TypeReferenceHandle)t.ResolutionScope,raw)+"+"+md.GetString(t.Name):(string.IsNullOrEmpty(md.GetString(t.Namespace))?"":md.GetString(t.Namespace)+".")+md.GetString(t.Name);}
 public string GetTypeFromSpecification(MetadataReader md,object? context,TypeSpecificationHandle h,byte raw)=>md.GetTypeSpecification(h).DecodeSignature(this,context);
}
