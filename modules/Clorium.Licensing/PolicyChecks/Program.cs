using Clorium.Licensing;
using Clorium.LicenseServer;

var folder = Path.GetFullPath(args[0]);
var clock = new TestClock { Value = new DateTimeOffset(2030, 1, 31, 18, 0, 0, TimeSpan.FromHours(9)) };
int checks = 0;
void Check(string title, bool passed) { if (!passed) throw new Exception("FAIL " + title); checks++; Console.WriteLine("PASS " + title); }
void Reject(string title, string code, Action action) { try { action(); throw new Exception("Expected rejection " + title); } catch (RuleException e) { Check(title, e.Code == code); } }
ActivationRequest Request(string serial, string machine, string version = "4.7.1.5") => new() { Serial = serial, MachineId = Wire.Hash(machine), DeviceSecret = new string('a', 64), Hostname = "POLICY-CHECK", OsVersion = "Windows", AppVersion = version, Nonce = Wire.Random() };
DeviceOperation Op(SignedLicense token, string version = "4.7.1.5") => new() { License = token, DeviceSecret = new string('a', 64), AppVersion = version, Nonce = Wire.Random() };
Grant Read(Store s, ApiReply r) => LicenseSignature.Verify(r.License, s.Settings.PublicKey);
string Pending(Store s, Action a) { try { a(); throw new Exception("Expected approval request"); } catch (RuleException e) when (e.Code == "APPROVAL_PENDING") { return s.Snapshot().Requests.Single(x => x.Code == e.RequestCode).Id; } }
long Kst(int y, int m, int d, int h) => new DateTimeOffset(y, m, d, h, 0, 0, TimeSpan.FromHours(9)).ToUnixTimeSeconds();
using (var server = new Store(folder, clock)) {
    var customer = new Customer { Company = "정책 검사" };
    server.Change(d => { d.Customers.Add(customer); return 0; });
    LicenseRecord Temp() { var l = new LicenseRecord { CustomerId = customer.Id, Kind = "Temporary" }; server.Change(d => { d.Licenses.Add(l); return 0; }); return l; }
    // Approval time 2030-01-31 18:00 KST: calendar months clamp to month ends and leap days.
    foreach (var (months, due) in new[] { (1, Kst(2030, 2, 28, 18)), (3, Kst(2030, 4, 30, 18)), (12, Kst(2031, 1, 31, 18)) }) {
        var l = Temp(); string id = Pending(server, () => server.Activate(Request(l.Serial, "pc-" + months)));
        server.Decide(id, new ApprovalDecision { Approve = true, Purpose = "Test", TermMonths = months, Reason = "검사" });
        var g = Read(server, server.Activate(Request(l.Serial, "pc-" + months)));
        Check("KST " + Store.TermText(months) + " from 01-31 ends " + Store.Kst(due), g.ExpiresAt == due && g.OfflineUntil == due && g.TermMonths == months);
    }
    // 2028-01-31 01:00 KST is still 01-30 in UTC; a UTC calendar would end on 03-01 KST instead of the leap day.
    clock.Value = new DateTimeOffset(2028, 1, 31, 1, 0, 0, TimeSpan.FromHours(9));
    var leap = Temp(); string leapId = Pending(server, () => server.Activate(Request(leap.Serial, "leap")));
    server.Decide(leapId, new ApprovalDecision { Approve = true, Purpose = "Demo", TermMonths = 1, Reason = "윤년" });
    var lg = Read(server, server.Activate(Request(leap.Serial, "leap")));
    Check("early-morning KST approval uses Korean date, leap February 29", lg.ExpiresAt == Kst(2028, 2, 29, 1));
    Check("temporary stays usable offline right before end", LicenseSignature.Validate(lg, Wire.Hash("leap"), "4.7.1.5", lg.ExpiresAt - 1, lg.IssuedAt) == null);
    Check("temporary stops at end without server", LicenseSignature.Validate(lg, Wire.Hash("leap"), "4.7.1.5", lg.ExpiresAt, lg.IssuedAt) != null);
    clock.Value = clock.Value.AddDays(10);
    Check("refresh inside the term keeps the approved end", Read(server, server.Device(Op(server.Activate(Request(leap.Serial, "leap")).License), false)).ExpiresAt == lg.ExpiresAt);
    clock.Value = DateTimeOffset.FromUnixTimeSeconds(lg.ExpiresAt);
    Reject("server refuses an expired temporary", "EXPIRED", () => server.Activate(Request(leap.Serial, "leap")));
    var extended = server.ChangeTerm(leap.Id, new TermChange { Purpose = "Demo", TermMonths = 1, Reason = "연장" });
    Check("re-approval after expiry counts from re-approval time", extended.ExpiresAt == clock.Value.ToOffset(TimeSpan.FromHours(9)).AddMonths(1).ToUnixTimeSeconds());
    Check("extended temporary works again", Read(server, server.Activate(Request(leap.Serial, "leap"))).ExpiresAt == extended.ExpiresAt);
    Reject("re-approval refuses an unknown term", "INVALID_INPUT", () => server.ChangeTerm(leap.Id, new TermChange { Purpose = "Demo", TermMonths = 6, Reason = "x" }));
    var paid = new LicenseRecord { CustomerId = customer.Id, Kind = "Perpetual" }; server.Change(d => { d.Licenses.Add(paid); return 0; });
    var forever = Read(server, server.Activate(Request(paid.Serial, "paid")));
    Check("perpetual has no expiry or periodic contact", forever.ExpiresAt == 0 && forever.RefreshAfter == Wire.Forever && forever.OfflineUntil == Wire.Forever && forever.VersionLine == "4.7");
    Check("perpetual local check passes 4.7.9.9 patch", LicenseSignature.Validate(forever, Wire.Hash("paid"), "4.7.9.9", Wire.Now, Wire.Now) == null);
    Check("perpetual local check requires one check for 4.8", LicenseSignature.Validate(forever, Wire.Hash("paid"), "4.8.0.0", Wire.Now, Wire.Now) != null);
    Check("server reissues for 4.8 without approval", Read(server, server.Device(Op(server.Activate(Request(paid.Serial, "paid")).License, "4.8.0.0"), false)).VersionLine == "4.8");
    Reject("PC change on a perpetual license is queued", "APPROVAL_PENDING", () => server.Activate(Request(paid.Serial, "paid-new")));
    Reject("unapproved temporary refuses unknown purpose at approval", "INVALID_INPUT", () => server.Decide(Pending(server, () => server.Activate(Request(Temp().Serial, "x"))), new ApprovalDecision { Approve = true, Purpose = "Sales", TermMonths = 1, Reason = "x" }));
}
// A database written by the earlier policy (staff 3-month / fixed-expiry demo) is converted on start.
var legacyFolder = folder + "-legacy";
using (var old = new Store(legacyFolder, clock)) old.Change(d => {
    var c = new Customer { Company = "기존" }; d.Customers.Add(c);
    d.Licenses.Add(new LicenseRecord { CustomerId = c.Id, Kind = "Test", RenewalMonths = 3, NextReauthAt = Wire.Now + 86400 });
    d.Licenses.Add(new LicenseRecord { CustomerId = c.Id, Kind = "Demo", ExpiresAt = Wire.Now + 7200 });
    d.Licenses.Add(new LicenseRecord { CustomerId = c.Id, Kind = "Test", RenewalMonths = 3 }); return 0; });
using (var reopened = new Store(legacyFolder, clock)) {
    var ls = reopened.Snapshot().Licenses;
    Check("legacy records belong to Clorium and default products are registered", ls.All(l => l.Product == Products.Clorium) && reopened.Snapshot().Products.Select(p => p.Code).OrderBy(c => c).SequenceEqual(new[] { Products.AutoSizing, Products.Clorium }));
    Check("legacy demo/test become temporary keeping their end", ls.All(l => l.Kind == "Temporary" && l.RenewalMonths == 0) && ls.Count(l => l.IsApproved && l.ExpiresAt > Wire.Now) == 2);
    Check("legacy staff license without a fixed end waits for approval", ls.Count(l => !l.IsApproved) == 1);
}
using (var reopened = new Store(folder, clock)) Check("approvals and terms survive server restart", reopened.Snapshot().Licenses.Count(l => l.IsTemporary && l.IsApproved) == 4 && reopened.Snapshot().Requests.Count(r => r.Status == "Approved") == 4);
Console.WriteLine("LICENSING_POLICY_PASSED=" + checks);
class TestClock : TimeProvider { public DateTimeOffset Value { get; set; } public override DateTimeOffset GetUtcNow() => Value; }
