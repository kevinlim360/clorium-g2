using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;

namespace CameraControl;

internal static class LicenseFileMatcher
{
    internal static bool IsUsableIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        string normalized = value.Trim();
        return !new[] { "N/A", "Default string", "To be filled by OEM" }
            .Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    // Keep both the original WMI spelling and its trimmed spelling for issued-file compatibility.
    internal static IEnumerable<string> IdentifierCandidates(IEnumerable<string> values)
    {
        return values.Where(IsUsableIdentifier).SelectMany(value => new[] { value, value.Trim() })
            .Distinct(StringComparer.Ordinal);
    }

    internal static string FindMatchingFile(string directory, IEnumerable<string> identifiers,
        IEnumerable<string> keys, Func<string, string, string> encrypt, Func<string, string> clean)
    {
        foreach (string identifier in IdentifierCandidates(identifiers))
        foreach (string key in keys.Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal))
        {
            string token = clean(encrypt(identifier, key));
            if (string.IsNullOrEmpty(token)) continue;
            string path = Path.Combine(directory, token + ".lock");
            if (File.Exists(path)) return path;
        }
        return null;
    }

    internal static string ReadCompatibilityKey()
    {
        // Release builds receive the original issued-key configuration outside the source tree.
        using (Stream stream = typeof(LicenseFileMatcher).Assembly.GetManifestResourceStream("Clorium.LegacyLicenseKey"))
        {
            if (stream == null) return null;
            using (var reader = new StreamReader(stream)) return reader.ReadToEnd();
        }
    }

    internal static List<string> ReadHardwareIdentifiers()
    {
        var identifiers = new List<string>();
        ReadHardwareValues("SELECT SerialNumber FROM Win32_BIOS", "SerialNumber", identifiers);
        ReadHardwareValues("SELECT MACAddress FROM Win32_NetworkAdapterConfiguration", "MACAddress", identifiers);
        ReadHardwareValues("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber", identifiers);
        ReadHardwareValues("SELECT SerialNumber FROM Win32_DiskDrive", "SerialNumber", identifiers);
        return identifiers;
    }

    private static void ReadHardwareValues(string query, string property, List<string> values)
    {
        // One unavailable WMI class/device must not prevent another matching device from being checked.
        try
        {
            using (var searcher = new ManagementObjectSearcher(query))
            using (var results = searcher.Get())
            foreach (ManagementBaseObject item in results)
            using (item)
            {
                try { if (item[property] != null) values.Add(item[property].ToString()); }
                catch (ManagementException) { }
            }
        }
        catch (ManagementException) { }
        catch (System.Runtime.InteropServices.COMException) { }
        catch (UnauthorizedAccessException) { }
    }
}
