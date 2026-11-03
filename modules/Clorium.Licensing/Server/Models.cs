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
    public bool IsInternal { get; set; }
    public bool Disabled { get; set; }
}
public class Product
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Notes { get; set; }
    public bool Disabled { get; set; }
    public long CreatedAt { get; set; } = Wire.Now;
}
public class LicenseRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Serial { get; set; } = Wire.Serial();
    public string Product { get; set; } = Products.Clorium;
    public string CustomerId { get; set; }
    // Perpetual: first activation per PC is permanent. Temporary: purpose and term are decided when the company approves.
    public string Kind { get; set; } = "Perpetual";
    public string Purpose { get; set; }
    public int TermMonths { get; set; }
    public long ApprovedAt { get; set; }
    public int MaxMajor { get; set; } = 4;
    public int Seats { get; set; } = 1;
    public long ExpiresAt { get; set; }
    public long CreatedAt { get; set; } = Wire.Now;
    public bool Blocked { get; set; }
    public string Notes { get; set; }
    // Read only to migrate records written before 2026-11-03 (staff 3-month / fixed-expiry demo).
    public int RenewalMonths { get; set; }
    public long NextReauthAt { get; set; }
    public bool IsTemporary => Kind == "Temporary";
    public bool IsApproved => !IsTemporary || ApprovedAt > 0;
}
public class ApprovalRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; }
    public string Type { get; set; }
    public string LicenseId { get; set; }
    public string MachineId { get; set; }
    public string SecretHash { get; set; }
    public string Hostname { get; set; }
    public string OsVersion { get; set; }
    public string AppVersion { get; set; }
    public bool Offline { get; set; }
    public string Status { get; set; } = "Pending";
    public long RequestedAt { get; set; } = Wire.Now;
    public long LastSeen { get; set; } = Wire.Now;
    public long DecidedAt { get; set; }
    public string Decision { get; set; }
    public string ReplacedActivationId { get; set; }
}
public class ApprovalDecision
{
    public bool Approve { get; set; }
    public string Purpose { get; set; }
    public int TermMonths { get; set; }
    public string ReplaceActivationId { get; set; }
    public string Reason { get; set; }
}
public class TermChange
{
    public string Purpose { get; set; }
    public int TermMonths { get; set; }
    public string Reason { get; set; }
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
    public List<Product> Products { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
    public List<LicenseRecord> Licenses { get; set; } = new();
    public List<Activation> Activations { get; set; } = new();
    public List<ApprovalRequest> Requests { get; set; } = new();
    public List<Audit> Audit { get; set; } = new();
}
public class ServerSettings
{
    public string AdminTokenHash { get; set; }
    public string PublicKey { get; set; }
}
