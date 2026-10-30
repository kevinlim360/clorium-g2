using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Clorium.LicenseServer;

public static class PrivateFiles
{
    public static void RestrictDirectory(string path)
    {
        if (OperatingSystem.IsWindows()) {
            var acl = new DirectorySecurity(); acl.SetAccessRuleProtection(true, false);
            var inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            foreach (var sid in new[] { WindowsIdentity.GetCurrent().User, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null) })
                acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(path).SetAccessControl(acl);
        } else File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }
    public static void Save(string path, byte[] data)
    {
        string tmp = path + "." + Guid.NewGuid().ToString("N") + ".private";
        try {
            using (var stream = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                if (OperatingSystem.IsWindows()) {
                    var acl = new FileSecurity(); acl.SetAccessRuleProtection(true, false);
                    foreach (var sid in new[] { WindowsIdentity.GetCurrent().User, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null) }) acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
                    new FileInfo(tmp).SetAccessControl(acl);
                } else File.SetUnixFileMode(tmp, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                stream.Write(data); stream.Flush(true);
            }
            File.Move(tmp, path, true);
        } finally { if (File.Exists(tmp)) File.Delete(tmp); }
    }
}
