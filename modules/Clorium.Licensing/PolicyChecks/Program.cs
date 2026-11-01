using Clorium.Licensing;
using Clorium.LicenseServer;

var folder = Path.GetFullPath(args[0]);
var clock = new TestClock { Value = new DateTimeOffset(2030, 3, 1, 0, 30, 0, TimeSpan.FromHours(9)) };
int checks = 0;
void Check(string title, bool passed) { if (!passed) throw new Exception("FAIL " + title); checks++; Console.WriteLine("PASS " + title); }
void Reject(string title, string code, Action action) { try { action(); throw new Exception("Expected rejection " + title); } catch (RuleException e) { Check(title, e.Code == code); } }
ActivationRequest Request(string serial, string machine = "staff-pc") => new() { Serial = serial, MachineId = Wire.Hash(machine), DeviceSecret = new string('a',64), Hostname = "POLICY-CHECK", OsVersion = "Windows", AppVersion = "4.7.1.5", Nonce = Wire.Random() };
DeviceOperation Op(SignedLicense token) => new() { License = token, DeviceSecret = new string('a',64), AppVersion = "4.7.1.5", Nonce = Wire.Random() };
using (var server = new Store(folder, clock)) {
    var staff = new Customer { Company = "직원 테스트", Email = "staff-id", IsInternal = true };
    var perpetual = new LicenseRecord { CustomerId = staff.Id, Kind = "Perpetual" };
    var test = new LicenseRecord { CustomerId = staff.Id, Kind = "Test", RenewalMonths = 3 };
    server.Change(d => { d.Customers.Add(staff); d.Licenses.Add(perpetual); d.Licenses.Add(test); return 0; });
    Reject("internal fixed-term demo cannot bypass staff policy", "INVALID_POLICY", () => Store.AccountPolicy(server.Snapshot(), new LicenseRecord { CustomerId = staff.Id, Kind = "Demo", ExpiresAt = Wire.Now+86400 }));
    var forever = LicenseSignature.Verify(server.Activate(Request(perpetual.Serial, "paid-pc")).License, server.Settings.PublicKey);
    Check("paid first online activation has no expiry or periodic contact", forever.ExpiresAt == 0 && forever.RefreshAfter == Wire.Forever && forever.OfflineUntil == Wire.Forever);
    var issued = server.Activate(Request(test.Serial));
    var g = LicenseSignature.Verify(issued.License, server.Settings.PublicKey);
    long due = new DateTimeOffset(2030,6,1,0,30,0,TimeSpan.FromHours(9)).ToUnixTimeSeconds();
    Check("KST month rollover uses June1 not UTC May28", g.ExpiresAt == due && g.RefreshAfter == due && g.OfflineUntil == due && g.RenewalMonths == 3);
    clock.Value = clock.Value.AddMonths(1);
    var early = LicenseSignature.Verify(server.Device(Op(issued.License), false).License, server.Settings.PublicKey);
    Check("early refresh does not extend three-month deadline", early.ExpiresAt == due);
    var reactivated = server.Activate(Request(test.Serial));
    Check("same-serial reactivation does not extend deadline", LicenseSignature.Verify(reactivated.License, server.Settings.PublicKey).ExpiresAt == due);
    server.Device(Op(reactivated.License), true);
    var moved = server.Activate(Request(test.Serial, "new-staff-pc"));
    Check("release and different-PC activation keep license deadline", LicenseSignature.Verify(moved.License, server.Settings.PublicKey).ExpiresAt == due);
    Reject("staff offline exchange is refused", "ONLINE_REQUIRED", () => server.Activate(Request(test.Serial, "new-staff-pc"), true));
    Check("staff local validation allows just before boundary", LicenseSignature.Validate(LicenseSignature.Verify(moved.License, server.Settings.PublicKey), Wire.Hash("new-staff-pc"), "4.7.1.5", due-1, early.IssuedAt) == null);
    Check("staff offline use stops exactly at boundary", LicenseSignature.Validate(LicenseSignature.Verify(moved.License, server.Settings.PublicKey), Wire.Hash("new-staff-pc"), "4.7.1.5", due, early.IssuedAt) != null);
    clock.Value = DateTimeOffset.FromUnixTimeSeconds(due);
    var renewed = server.Device(Op(moved.License), false);
    long next = clock.Value.ToOffset(TimeSpan.FromHours(9)).AddMonths(3).ToUnixTimeSeconds();
    Check("expired signed staff token renews online for next three months", LicenseSignature.Verify(renewed.License, server.Settings.PublicKey).ExpiresAt == next);
    server.Change(d => { d.Customers.Single(c => c.Id == staff.Id).Disabled = true; return 0; });
    Reject("disabled employee cannot refresh", "ACCOUNT_DISABLED", () => server.Device(Op(renewed.License), false));
    Reject("disabled employee cannot reactivate", "ACCOUNT_DISABLED", () => server.Activate(Request(test.Serial, "new-staff-pc")));
    server.Change(d => { d.Customers.Single(c => c.Id == staff.Id).Disabled = false; return 0; });
    Check("re-enabled staff retains previous renewal deadline", LicenseSignature.Verify(server.Device(Op(renewed.License),false).License,server.Settings.PublicKey).ExpiresAt == next);
    foreach (var date in new[] { new DateTimeOffset(2030,1,31,18,0,0,TimeSpan.FromHours(9)), new DateTimeOffset(2027,11,30,18,0,0,TimeSpan.FromHours(9)) }) {
        clock.Value = date;
        var demo = new LicenseRecord { CustomerId = staff.Id, Kind = "Demo", RenewalMonths = 3 }; server.Change(d => { d.Licenses.Add(demo); return 0; });
        var result = LicenseSignature.Verify(server.Activate(Request(demo.Serial, Wire.Random())).License, server.Settings.PublicKey);
        Check("calendar boundary " + date.ToString("yyyy-MM-dd"), result.ExpiresAt == date.AddMonths(3).ToUnixTimeSeconds());
    }
}
using (var reopened = new Store(folder, clock)) Check("next renewal deadlines survive server restart", reopened.Snapshot().Licenses.Count(l => l.RenewalMonths==3 && l.NextReauthAt>0) == 3);
Console.WriteLine("LICENSING_POLICY_PASSED=" + checks);
class TestClock : TimeProvider { public DateTimeOffset Value { get; set; } public override DateTimeOffset GetUtcNow() => Value; }
