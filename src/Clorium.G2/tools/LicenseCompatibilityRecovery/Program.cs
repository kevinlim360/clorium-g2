using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Recover only the existing, hash-pinned license compatibility value; never print its value.
class RecoverLicenseCompatibility {
 static int Main(string[] args) {
  try {
   var map=JsonDocument.Parse(File.ReadAllBytes(args[1]));
   string expected=map.RootElement.GetProperty("originalInputSha256").GetString();
   byte[] original=null;
   using(var launcher=ModuleDefinition.ReadModule(args[0])) {
    var seed=launcher.Resources.OfType<EmbeddedResource>().Single(r=>r.Name=="DeploymentSeed");
    using(var input=seed.GetResourceStream())using(var zip=new ZipArchive(input,ZipArchiveMode.Read))
    foreach(var entry in zip.Entries) {
     byte[] bytes;using(var stream=entry.Open())using(var memory=new MemoryStream()){stream.CopyTo(memory);bytes=memory.ToArray();}
     using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()==expected)original=bytes;
    }
   }
   if(original==null)throw new InvalidDataException();
   var values=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
   using(var stream=new MemoryStream(original))using(var module=ModuleDefinition.ReadModule(stream)) {
    var type=module.Types.Single(t=>t.FullName=="CameraControl.App");
    var field=type.Fields.Single(f=>f.Name=="Encryption_key");
    foreach(var method in type.Methods.Where(m=>m.HasBody)) {
     string previous=null;
     foreach(var instruction in method.Body.Instructions) {
      if(instruction.OpCode==OpCodes.Ldstr)previous=(string)instruction.Operand;
      else if(instruction.OpCode==OpCodes.Stfld&&previous!=null) {
       var target=instruction.Operand as FieldReference;if(target!=null&&target.FullName==field.FullName)values.Add(previous);previous=null;
      } else if(instruction.OpCode!=OpCodes.Nop)previous=null;
     }
    }
   }
   if(values.Count!=1||string.IsNullOrEmpty(values.Single())||File.Exists(args[2]))throw new InvalidDataException();
   File.WriteAllText(args[2],values.Single(),new UTF8Encoding(false));
   Console.WriteLine("Recovered the existing hash-verified compatibility configuration. No values printed.");return 0;
  }catch(Exception ex){Console.Error.WriteLine("Compatibility recovery failed: "+ex.GetType().Name);return 1;}
 }
}
