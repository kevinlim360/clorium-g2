using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
try {
 if(args.Length!=3){Console.Error.WriteLine("Usage: InitializeDeploymentSettings PROVISIONING_MAP.json ORIGINAL_PRODUCT.exe NEW_LOCAL_SETTINGS.json");return 2;}
 if(File.Exists(args[2]))throw new InvalidOperationException("Output exists; select a new local settings file.");
 using var map=JsonDocument.Parse(File.ReadAllBytes(args[0]));
 var expected=map.RootElement.GetProperty("originalInputSha256").GetString();
 using(var check=File.OpenRead(args[1]))if(!StringComparer.OrdinalIgnoreCase.Equals(expected,Convert.ToHexStringLower(SHA256.HashData(check))))throw new InvalidOperationException("Original product SHA does not match this candidate map.");
 using var input=File.OpenRead(args[1]);using var pe=new PEReader(input);
 if(!pe.HasMetadata)throw new InvalidOperationException("Expected managed product metadata.");
 var md=pe.GetMetadataReader();
 string TypeName(TypeDefinitionHandle h){var t=md.GetTypeDefinition(h);var p=t.GetDeclaringType();return !p.IsNil?TypeName(p)+"+"+md.GetString(t.Name):(md.GetString(t.Namespace) is var ns&&ns.Length>0?ns+".":"")+md.GetString(t.Name);}
 var instructions=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)!).ToDictionary(o=>unchecked((ushort)o.Value));
 var stores=new Dictionary<int,HashSet<string>>();
 foreach(var h in md.MethodDefinitions) {
  var m=md.GetMethodDefinition(h);if(m.RelativeVirtualAddress==0)continue;
  var bytes=pe.GetMethodBody(m.RelativeVirtualAddress).GetILBytes()!;int pos=0;string? previousString=null;
  while(pos<bytes.Length){
   ushort code=bytes[pos++];if(code==0xfe)code=(ushort)(0xfe00|bytes[pos++]);
   if(!instructions.TryGetValue(code,out var op))throw new InvalidOperationException("Unsupported IL opcode.");
   int size=op.OperandType switch {OperandType.InlineNone=>0,OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,OperandType.InlineVar=>2,OperandType.InlineI8 or OperandType.InlineR=>8,OperandType.InlineSwitch=>4+checked(BitConverter.ToInt32(bytes,pos)*4),_=>4};
   if(size<0||pos+size>bytes.Length)throw new InvalidOperationException("Invalid IL operand.");
   if(op==OpCodes.Ldstr){int token=BitConverter.ToInt32(bytes,pos);previousString=md.GetUserString(MetadataTokens.UserStringHandle(token&0x00ffffff));}
   else if((op==OpCodes.Stfld||op==OpCodes.Stsfld)&&previousString!=null){int token=BitConverter.ToInt32(bytes,pos);if(!stores.TryGetValue(token,out var values))stores[token]=values=new();values.Add(previousString);previousString=null;}
   else if(op!=OpCodes.Nop)previousString=null;
   pos+=size;
  }
 }
 var settings=new SortedDictionary<string,string>(StringComparer.Ordinal);
 var fields=map.RootElement.GetProperty("fields");if(fields.GetArrayLength()<1||fields.GetArrayLength()>16)throw new InvalidOperationException("Unexpected settings field count.");
 foreach(var item in fields.EnumerateArray()) {
  var environment=item.GetProperty("environmentVariable").GetString()!;
  if(!Regex.IsMatch(environment,"^CLORIUM_[A-Z0-9_]+$"))throw new InvalidOperationException("Unexpected environment variable name.");
  var type=item.GetProperty("originalMetadataTypeName").GetString();var field=item.GetProperty("fieldName").GetString();
  var matches=md.TypeDefinitions.Where(h=>TypeName(h)==type).SelectMany(h=>md.GetTypeDefinition(h).GetFields()).Where(h=>md.GetString(md.GetFieldDefinition(h).Name)==field).ToArray();
  if(matches.Length!=1)throw new InvalidOperationException("Expected unique metadata field.");
  var signature=md.GetBlobBytes(md.GetFieldDefinition(matches[0]).Signature);
  if(signature.Length!=2||signature[0]!=0x06||signature[1]!=0x0e)throw new InvalidOperationException("Expected direct string field signature.");
  string value;
  var storageKind=item.TryGetProperty("storageKind",out var kind)?kind.GetString():"direct-string-store";
  if(storageKind=="metadata-string-constant"){
   var definition=md.GetFieldDefinition(matches[0]);var constantHandle=definition.GetDefaultValue();
   if((definition.Attributes&FieldAttributes.Literal)==0||constantHandle.IsNil)throw new InvalidOperationException("Expected literal metadata field.");
   var constant=md.GetConstant(constantHandle);
   if(constant.TypeCode!=ConstantTypeCode.String)throw new InvalidOperationException("Expected string metadata constant.");
   value=Encoding.Unicode.GetString(md.GetBlobBytes(constant.Value));
  }else if(storageKind=="direct-string-store"){
   if(!stores.TryGetValue(MetadataTokens.GetToken(matches[0]),out var candidates)||candidates.Count!=1)throw new InvalidOperationException("Expected one direct string initializer; no guessed value will be used.");
   value=candidates.Single();
  }else throw new InvalidOperationException("Unexpected metadata storage kind.");
  settings.Add(environment,value);
 }
 var full=Path.GetFullPath(args[2]);Directory.CreateDirectory(Path.GetDirectoryName(full)!);
 using(var output=new FileStream(full,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
  if(!OperatingSystem.IsWindows())File.SetUnixFileMode(full,UnixFileMode.UserRead|UnixFileMode.UserWrite);
  using var writer=new StreamWriter(output,new UTF8Encoding(false));writer.Write(JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}));writer.WriteLine();
 }
 Console.WriteLine($"Local settings initialized: {settings.Count} fields. Values are not printed. Keep this file outside source ZIPs and Git.");return 0;
} catch(Exception ex) {
 // Never print customer values, original IL, source text or an exception message containing them.
 Console.Error.WriteLine($"Settings initialization failed ({ex.GetType().Name}). Verify candidate hash, metadata map and a new local output path.");return 1;
}
