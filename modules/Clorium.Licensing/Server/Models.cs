using System;
using System.Collections.Generic;
using Clorium.Licensing;
namespace Clorium.LicenseServer;

public class Customer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Company { get; set; }
    public string Contact { get; set; }
    public string Email { get; set; }
    public string Notes { get; set; }
}
public class LicenseRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Serial { get; set; } = Wire.Serial();
    public string CustomerId { get; set; }
    public string Kind { get; set; } = "Perpetual";
    public int MaxMajor { get; set; } = 4;
    public int Seats { get; set; } = 1;
    public int GraceDays { get; set; } = 30;
    public bool AllowPermanentOffline { get; set; }
    public long ExpiresAt { get; set; }
    public long CreatedAt { get; set; } = Wire.Now;
    public bool Blocked { get; set; }
    public string Notes { get; set; }
}
public class Activation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string LicenseId { get; set; }
    public string MachineId { get; set; }
    public string SecretHash { get; set; }
    public string Hostname { get; set; }
    public string OsVersion { get; set; }
    public string AppVersion { get; set; }
    public bool Active { get; set; } = true;
    public bool Blocked { get; set; }
    public bool Offline { get; set; }
    public long FirstSeen { get; set; } = Wire.Now;
    public long LastSeen { get; set; } = Wire.Now;
    public long LastGrantUntil { get; set; }
}
public class Audit
{
    public long At { get; set; } = Wire.Now;
    public string Action { get; set; }
    public string Target { get; set; }
    public string Detail { get; set; }
}
public class Database
{
    public List<Customer> Customers { get; set; } = new();
    public List<LicenseRecord> Licenses { get; set; } = new();
    public List<Activation> Activations { get; set; } = new();
    public List<Audit> Audit { get; set; } = new();
}
public class ServerSettings
{
    public string AdminTokenHash { get; set; }
    public string PublicKey { get; set; }
}
