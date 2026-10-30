using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;

// This file contains only public WMPLib interop metadata operations. Product method bodies are never edited.
const string GuidAttributeName = "System.Runtime.InteropServices.GuidAttribute";
var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, WriteIndented = true };
if (args.Length == 3 && args[0] == "export")
{
    if (File.Exists(args[2])) throw new IOException("Output spec already exists.");
    using var original = AssemblyDefinition.ReadAssembly(args[1], new ReaderParameters { ReadSymbols = false });
    var localTypes = original.MainModule.Types.ToDictionary(t => t.FullName);
    var spec = new InteropSpec
    {
        SchemaVersion = 1,
        SourceAssemblyName = original.Name.Name,
        SourceAssemblyVersion = original.Name.Version.ToString(),
        SourceSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[1]))).ToLowerInvariant(),
        Types = original.MainModule.Types.Where(t => t.Namespace == "WMPLib").Select(t => new TypeSpec
        {
            FullName = t.FullName,
            Attributes = (int)t.Attributes,
            Interfaces = t.Interfaces.Select(i => i.InterfaceType.FullName).ToList(),
            HasGuidAttribute = t.CustomAttributes.Any(a => a.AttributeType.FullName == GuidAttributeName),
            Methods = t.Methods.Select(m => new MethodSpec
            {
                Name = m.Name,
                ReturnType = m.ReturnType.FullName,
                Attributes = (int)m.Attributes,
                ImplAttributes = (int)m.ImplAttributes,
                Parameters = m.Parameters.Select(p => new ParameterSpec
                {
                    Type = p.ParameterType.FullName,
                    Name = p.Name,
                    Attributes = (int)p.Attributes
                }).ToList()
            }).ToList()
        }).ToList()
    };
    if (spec.Types.Count == 0 || spec.Types.SelectMany(t => t.Interfaces).Any(i => !localTypes.ContainsKey(i)))
        throw new InvalidDataException("Interop spec requires local WMPLib interfaces only.");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[2]))!);
    File.WriteAllText(args[2], JsonSerializer.Serialize(spec, jsonOptions) + Environment.NewLine);
    Console.WriteLine($"Exported interop types={spec.Types.Count}, methods={spec.Types.Sum(t => t.Methods.Count)}");
    return 0;
}
if (args.Length == 3 && args[0] == "verify")
{
    var spec = ReadSpec(args[1]);
    using var assembly = AssemblyDefinition.ReadAssembly(args[2], new ReaderParameters { ReadSymbols = false });
    Verify(spec, assembly);
    Console.WriteLine($"Verified interop types={spec.Types.Count}, methods={spec.Types.Sum(t => t.Methods.Count)}");
    return 0;
}
if (args.Length == 4 && args[0] == "restore")
{
    if (File.Exists(args[3])) throw new IOException("Output assembly already exists.");
    var spec = ReadSpec(args[1]);
    var preserveSymbols = File.Exists(Path.ChangeExtension(args[2], ".pdb"));
    if (File.Exists(Path.ChangeExtension(args[3], ".pdb")))
        throw new IOException("Output symbols already exist.");
    using var rebuilt = AssemblyDefinition.ReadAssembly(args[2], new ReaderParameters { ReadSymbols = preserveSymbols });
    if (rebuilt.Name.Name != spec.SourceAssemblyName || rebuilt.Name.Version.ToString() != spec.SourceAssemblyVersion)
        throw new InvalidDataException("Compiled assembly identity differs from interop spec source.");
    var beforeBusinessIl = BusinessIlFingerprint(rebuilt);
    var rebuiltTypes = rebuilt.MainModule.Types.ToDictionary(t => t.FullName);
    var wmplibNames = rebuilt.MainModule.Types.Where(t => t.Namespace == "WMPLib").Select(t => t.FullName).ToHashSet();
    if (!wmplibNames.SetEquals(spec.Types.Select(t => t.FullName))) throw new InvalidDataException("WMPLib type set differs from spec.");
    var changedMethods = 0;
    foreach (var before in spec.Types)
    {
        var after = rebuiltTypes[before.FullName];
        after.Attributes = (TypeAttributes)before.Attributes;
        after.Interfaces.Clear();
        foreach (var inherited in before.Interfaces)
        {
            if (!rebuiltTypes.TryGetValue(inherited, out var local)) throw new InvalidDataException($"Missing interop interface: {inherited}");
            after.Interfaces.Add(new InterfaceImplementation(local));
        }
        if (after.Methods.Count != before.Methods.Count) throw new InvalidDataException($"Interop method count mismatch: {before.FullName}");
        foreach (var method in before.Methods)
        {
            var candidates = after.Methods.Where(m => m.Name == method.Name && m.ReturnType.FullName == method.ReturnType
                && m.Parameters.Count == method.Parameters.Count
                && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(method.Parameters.Select(p => p.Type))).ToArray();
            if (candidates.Length != 1) throw new InvalidDataException($"Interop method match failed: {before.FullName}::{method.Name}, candidates={candidates.Length}");
            var target = candidates[0];
            target.Attributes = (MethodAttributes)method.Attributes;
            target.ImplAttributes = (MethodImplAttributes)method.ImplAttributes;
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                target.Parameters[i].Name = method.Parameters[i].Name;
                target.Parameters[i].Attributes = (ParameterAttributes)method.Parameters[i].Attributes;
            }
            changedMethods++;
        }
        if (!before.HasGuidAttribute)
        {
            for (var i = after.CustomAttributes.Count - 1; i >= 0; i--)
                if (after.CustomAttributes[i].AttributeType.FullName == GuidAttributeName) after.CustomAttributes.RemoveAt(i);
        }
    }
    Verify(spec, rebuilt);
    if (BusinessIlFingerprint(rebuilt) != beforeBusinessIl) throw new InvalidDataException("Non-WMPLib method bodies changed in memory.");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[3]))!);
    rebuilt.Write(args[3], new WriterParameters { WriteSymbols = preserveSymbols });
    Console.WriteLine($"Restored interop types={spec.Types.Count}, methods={changedMethods}; non-WMPLib IL unchanged");
    return 0;
}
Console.Error.WriteLine("Usage: export ORIGINAL_EXE SPEC_JSON | restore SPEC_JSON COMPILED_EXE NEW_OUTPUT | verify SPEC_JSON ASSEMBLY");
return 2;

InteropSpec ReadSpec(string path)
{
    var spec = JsonSerializer.Deserialize<InteropSpec>(File.ReadAllText(path), jsonOptions) ?? throw new InvalidDataException("Empty interop spec.");
    if (spec.SchemaVersion != 1 || spec.Types.Count == 0 || spec.SourceAssemblyName != "CameraControl")
        throw new InvalidDataException("Unsupported interop spec.");
    if (spec.Types.Any(t => !t.FullName.StartsWith("WMPLib.", StringComparison.Ordinal)) ||
        spec.Types.Select(t => t.FullName).Distinct(StringComparer.Ordinal).Count() != spec.Types.Count)
        throw new InvalidDataException("Interop spec contains invalid or repeated types.");
    return spec;
}
void Verify(InteropSpec spec, AssemblyDefinition assembly)
{
    var actualTypes = assembly.MainModule.Types.Where(t => t.Namespace == "WMPLib").ToDictionary(t => t.FullName);
    if (!actualTypes.Keys.ToHashSet().SetEquals(spec.Types.Select(t => t.FullName))) throw new InvalidDataException("WMPLib type set mismatch.");
    foreach (var wanted in spec.Types)
    {
        var actual = actualTypes[wanted.FullName];
        if ((int)actual.Attributes != wanted.Attributes ||
            !actual.Interfaces.Select(i => i.InterfaceType.FullName).SequenceEqual(wanted.Interfaces) ||
            actual.CustomAttributes.Any(a => a.AttributeType.FullName == GuidAttributeName) != wanted.HasGuidAttribute ||
            actual.Methods.Count != wanted.Methods.Count)
            throw new InvalidDataException($"WMPLib type metadata mismatch: {wanted.FullName}");
        foreach (var method in wanted.Methods)
        {
            var matches = actual.Methods.Where(m => m.Name == method.Name && m.ReturnType.FullName == method.ReturnType &&
                m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(method.Parameters.Select(p => p.Type))).ToArray();
            if (matches.Length != 1) throw new InvalidDataException($"WMPLib method mismatch: {wanted.FullName}::{method.Name}");
            var m = matches[0];
            if ((int)m.Attributes != method.Attributes || (int)m.ImplAttributes != method.ImplAttributes ||
                !m.Parameters.Select(p => p.Name).SequenceEqual(method.Parameters.Select(p => p.Name)) ||
                !m.Parameters.Select(p => (int)p.Attributes).SequenceEqual(method.Parameters.Select(p => p.Attributes)))
                throw new InvalidDataException($"WMPLib method metadata mismatch: {wanted.FullName}::{method.Name}");
        }
    }
}
string BusinessIlFingerprint(AssemblyDefinition assembly)
{
    using var sha = SHA256.Create();
    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    foreach (var type in assembly.MainModule.Types.Where(t => t.Namespace != "WMPLib").OrderBy(t => t.FullName, StringComparer.Ordinal))
    foreach (var method in type.Methods.OrderBy(m => m.FullName, StringComparer.Ordinal))
    {
        writer.Write(method.FullName);
        if (!method.HasBody) continue;
        foreach (var instruction in method.Body.Instructions)
        {
            writer.Write(instruction.OpCode.Code.ToString());
            writer.Write(instruction.Operand?.ToString() ?? string.Empty);
        }
    }
    writer.Flush();
    return Convert.ToHexString(sha.ComputeHash(stream.ToArray()));
}
public sealed class InteropSpec
{
    public int SchemaVersion { get; set; }
    public string SourceAssemblyName { get; set; } = "";
    public string SourceAssemblyVersion { get; set; } = "";
    public string SourceSha256 { get; set; } = "";
    public List<TypeSpec> Types { get; set; } = new();
}
public sealed class TypeSpec
{
    public string FullName { get; set; } = "";
    public int Attributes { get; set; }
    public List<string> Interfaces { get; set; } = new();
    public bool HasGuidAttribute { get; set; }
    public List<MethodSpec> Methods { get; set; } = new();
}
public sealed class MethodSpec
{
    public string Name { get; set; } = "";
    public string ReturnType { get; set; } = "";
    public int Attributes { get; set; }
    public int ImplAttributes { get; set; }
    public List<ParameterSpec> Parameters { get; set; } = new();
}
public sealed class ParameterSpec
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public int Attributes { get; set; }
}
