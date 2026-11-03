using Clorium.Licensing;
using System.Security.Cryptography;
using System.Text.Json;

namespace Clorium.LicenseServer;

public class RuleException : Exception
{
    public string Code { get; }
    public string RequestCode { get; }
    public RuleException(string code, string message, string requestCode = null) : base(message) { Code = code; RequestCode = requestCode; }
}
public sealed class Store : IDisposable
{
    private readonly object gate = new();
    private readonly string root;
    private readonly FileStream exclusive;
    private readonly RSA signing;
    private Database current;
    private readonly TimeProvider clock;
    private long Now => clock.GetUtcNow().ToUnixTimeSeconds();
    public ServerSettings Settings { get; }
    public string BackupFolder => Path.Combine(root, "backups");
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public Store(string folder, TimeProvider clock = null)
    {
        this.clock = clock ?? TimeProvider.System;
        root = Path.GetFullPath(folder); Directory.CreateDirectory(root);
        string settingsPath = Path.Combine(root, "settings.json"), keyPath = Path.Combine(root, "signing-key.xml"), dbPath = Path.Combine(root, "licenses.json");
        if (!File.Exists(settingsPath) && Directory.EnumerateFileSystemEntries(root).Any(p => !new[] { "server.log", "server-error.log" }.Contains(Path.GetFileName(p))))
            throw new InvalidDataException("전용 빈 데이터 폴더를 지정하거나 전체 백업을 복원해 주세요. 기존 폴더의 권한을 바꾸지 않았습니다.");
        if (File.Exists(settingsPath) && (!File.Exists(keyPath) || !File.Exists(dbPath)))
            throw new InvalidDataException("서버 데이터 일부가 없습니다. 새 키를 만들지 말고 전체 백업에서 복원해 주세요.");
        PrivateFiles.RestrictDirectory(root);
        exclusive = new FileStream(Path.Combine(root, "writer.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (!File.Exists(settingsPath)) {
            // Never silently replace a missing configuration while old customers still depend on its key.
            if (File.Exists(keyPath) || File.Exists(dbPath)) throw new InvalidDataException("서버 설정 일부가 없습니다. 전체 백업에서 복원해야 합니다.");
            signing = RSA.Create(3072);
            string token = Wire.Random(48);
            Settings = new ServerSettings { AdminTokenHash = Wire.Hash(token), PublicKey = signing.ToXmlString(false) };
            Wire.AtomicWrite(keyPath, signing.ToXmlString(true));
            Wire.AtomicWrite(settingsPath, JsonSerializer.Serialize(Settings, Json));
            Wire.AtomicWrite(Path.Combine(root, "admin-token.txt"), token);
            current = new Database(); Seed(current); Save(current);
        } else {
            Settings = JsonSerializer.Deserialize<ServerSettings>(File.ReadAllText(settingsPath), Json);
            signing = RSA.Create(); signing.FromXmlString(File.ReadAllText(keyPath));
            if (signing.ToXmlString(false) != Settings.PublicKey) throw new InvalidDataException("서명키와 설정이 일치하지 않습니다.");
            current = JsonSerializer.Deserialize<Database>(File.ReadAllText(dbPath), Json) ?? throw new InvalidDataException("저장소가 비어 있습니다.");
            current.Requests ??= new(); current.Products ??= new();
            if (current.Licenses.Any(l => l.Kind is "Demo" or "Test")) Change(Migrate);
            if (DefaultProducts.Any(p => !current.Products.Any(x => x.Code == p)) || current.Licenses.Any(l => string.IsNullOrEmpty(l.Product))) Change(Seed);
        }
    }
    private static readonly string[] DefaultProducts = { Products.Clorium, Products.AutoSizing };
    // Clorium and standalone AutoSizing always exist; licenses written before products existed belong to Clorium.
    private static int Seed(Database d)
    {
        foreach (var code in DefaultProducts.Where(c => !d.Products.Any(p => p.Code == c))) { d.Products.Add(new Product { Code = code, Name = Products.DefaultName(code) }); Log(d, "제품 등록", code, Products.DefaultName(code)); }
        foreach (var l in d.Licenses.Where(l => string.IsNullOrEmpty(l.Product))) l.Product = Products.Clorium;
        return 0;
    }
    public static string ProductName(Database d, string code) => d.Products.FirstOrDefault(p => p.Code == code)?.Name ?? Products.DefaultName(code);
    // Records written before the temporary-license policy: keep their current end date, or wait for approval if none was fixed yet.
    private int Migrate(Database d)
    {
        foreach (var l in d.Licenses.Where(l => l.Kind is "Demo" or "Test")) {
            long end = l.ExpiresAt > 0 ? l.ExpiresAt : l.NextReauthAt;
            l.Purpose = l.Kind; l.Kind = "Temporary"; l.ExpiresAt = end; l.ApprovedAt = end > 0 ? l.CreatedAt : 0; l.RenewalMonths = 0; l.NextReauthAt = 0;
            Log(d, "임시 라이선스 정책 전환", l.Id, end > 0 ? "기존 만료 " + Kst(end) + " 유지" : "목적·기간 승인 필요");
        }
        return 0;
    }
    public bool IsAdmin(string token) { return token != null && CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(Wire.Hash(token)), System.Text.Encoding.ASCII.GetBytes(Settings.AdminTokenHash)); }
    private void Save(Database d) { Wire.AtomicWrite(Path.Combine(root, "licenses.json"), JsonSerializer.Serialize(d, Json)); }
    public T Change<T>(Func<Database, T> action) { lock (gate) { var next = JsonSerializer.Deserialize<Database>(JsonSerializer.Serialize(current, Json), Json); T result = action(next); Save(next); current = next; return result; } }
    public Database Snapshot() { lock (gate) return JsonSerializer.Deserialize<Database>(JsonSerializer.Serialize(current, Json), Json); }
    public static void Log(Database d, string action, string target, string detail = "") { d.Audit.Add(new Audit { Action = action, Target = target, Detail = detail }); }
    public static void Require(bool condition, string code, string message) { if (!condition) throw new RuleException(code, message); }
    public static void Text(string text, int max, string field) { Require(text != null && text.Trim().Length > 0 && text.Length <= max, "INVALID_INPUT", field + " 입력을 확인해 주세요."); }
    public static string Kst(long at) => DateTimeOffset.FromUnixTimeSeconds(at).ToOffset(TimeSpan.FromHours(9)).ToString("yyyy-MM-dd HH:mm");
    public static void Policy(Database d, LicenseRecord l) {
        Require(d.Products.Any(p => p.Code == l.Product), "INVALID_INPUT", "등록된 제품을 선택해 주세요.");
        Require(!d.Products.Single(p => p.Code == l.Product).Disabled, "PRODUCT_DISABLED", "판매 중지한 제품입니다. 제품 탭에서 다시 사용으로 바꿔 주세요.");
        Require(l.Kind is "Perpetual" or "Temporary", "INVALID_INPUT", "라이선스 종류는 영구 또는 임시입니다.");
        Require(l.Seats > 0 && l.Seats <= 10000 && l.MaxMajor > 0 && l.MaxMajor <= 999, "INVALID_INPUT", "설치 수/허용 버전 범위를 확인해 주세요.");
    }
    public static void Term(string purpose, int months) {
        Require(purpose is "Demo" or "Test", "INVALID_INPUT", "임시 라이선스 목적은 데모 또는 테스트입니다.");
        Require(Wire.TemporaryTerms.Contains(months), "INVALID_INPUT", "임시 라이선스 기간은 1개월, 3개월, 1년 중 하나입니다.");
    }
    private void Usable(Database d, LicenseRecord l, string version) {
        Require(l != null, "UNKNOWN_LICENSE", "시리얼번호를 확인해 주세요.");
        Require(!d.Products.Any(p => p.Code == l.Product && p.Disabled), "PRODUCT_DISABLED", ProductName(d, l.Product) + " 제품의 인증을 회사에서 중지했습니다. 회사에 문의해 주세요.");
        Require(!l.Blocked, "BLOCKED", "회사에서 차단한 라이선스입니다.");
        Require(!d.Customers.Any(c => c.Id == l.CustomerId && c.Disabled), "ACCOUNT_DISABLED", "고객사/직원 계정이 중지되었습니다. 회사 관리자에게 문의해 주세요.");
        Require(!l.IsApproved || !l.IsTemporary || Now < l.ExpiresAt, "EXPIRED", "임시 라이선스 사용 기간이 만료되었습니다. 회사에 연장 승인을 요청해 주세요.");
        Require(Version.TryParse(version, out var v) && v.Major <= l.MaxMajor, "VERSION_DENIED", "허용한 주요 버전을 초과했습니다.");
    }
    private ApiReply GrantFor(Database d, LicenseRecord l, Activation a, string version, string nonce)
    {
        long now = Now;
        // Perpetual: never contact the server again. Temporary: valid offline until the approved end date.
        long expires = l.IsTemporary ? l.ExpiresAt : 0, until = l.IsTemporary ? l.ExpiresAt : Wire.Forever;
        a.LastGrantUntil = until; a.AppVersion = version;
        return new ApiReply { Code = "OK", ServerTime = now, License = LicenseSignature.Sign(new Grant {
            Product = l.Product, LicenseId = l.Id, ActivationId = a.Id, Customer = d.Customers.Single(x => x.Id == l.CustomerId).Company,
            Serial = l.Serial, MachineId = a.MachineId, Kind = l.IsTemporary ? l.Purpose : "Perpetual", MaxMajor = l.MaxMajor, VersionLine = Wire.Line(version),
            TermMonths = l.TermMonths, ExpiresAt = expires, IssuedAt = now, RefreshAfter = until, OfflineUntil = until, Nonce = nonce
        }, signing) };
    }
    private static string NewCode(Database d) { string code; do code = "R-" + Wire.Serial().Substring(5, 6); while (d.Requests.Any(x => x.Code == code)); return code; }
    // A new PC (or a PC whose stored state was replaced) is queued for the company instead of being refused.
    private RuleException Queue(Database d, LicenseRecord l, ActivationRequest r, string type, bool offline)
    {
        string hash = Wire.Hash(r.DeviceSecret);
        var same = d.Requests.LastOrDefault(x => x.LicenseId == l.Id && x.MachineId == r.MachineId && x.SecretHash == hash);
        if (same != null && same.Status == "Rejected") return new RuleException("REQUEST_REJECTED", "회사에서 요청 " + same.Code + "을(를) 거절했습니다. 사유: " + same.Decision, same.Code);
        if (same == null || same.Status != "Pending") {
            Require(d.Requests.Count(x => x.LicenseId == l.Id && x.Status == "Pending") < 20, "TOO_MANY_REQUESTS", "승인 대기 요청이 너무 많습니다. 회사에 문의해 주세요.");
            same = new ApprovalRequest { Code = NewCode(d), Type = type, LicenseId = l.Id, MachineId = r.MachineId, SecretHash = hash, Offline = offline };
            d.Requests.Add(same); Log(d, type == "Temporary" ? "임시 사용 승인 요청" : "PC 변경 승인 요청", l.Id, ProductName(d, l.Product) + " · " + same.Code + " · " + r.Hostname);
        }
        same.Hostname = r.Hostname; same.OsVersion = r.OsVersion; same.AppVersion = r.AppVersion; same.LastSeen = Now;
        string what = type == "Temporary" ? "임시 라이선스 승인" : "PC 변경 승인";
        return new RuleException("APPROVAL_PENDING", what + "을 기다리고 있습니다. 요청번호 " + same.Code + "을(를) 회사 담당자에게 알려 주세요. 승인 후 '승인 확인'을 누르면 사용할 수 있습니다.", same.Code);
    }
    public ApiReply Activate(ActivationRequest r, bool offline = false)
    {
        RuleException queued = null;
        var reply = Change(d => {
            Require(Products.IsValidCode(r.Product), "PRODUCT_DENIED", "제품 정보가 올바르지 않습니다.");
            Text(r.MachineId, 64, "PC 식별값"); Require(r.MachineId.Length == 64 && r.MachineId.All(Uri.IsHexDigit), "INVALID_INPUT", "PC 식별값 형식이 잘못되었습니다.");
            Text(r.DeviceSecret, 128, "PC 인증값"); Require(r.DeviceSecret.Length >= 40, "INVALID_INPUT", "PC 인증값이 너무 짧습니다.");
            Text(r.Hostname, 200, "PC 이름"); Text(r.OsVersion, 200, "OS"); Text(r.AppVersion, 40, "버전"); Text(r.Nonce, 100, "요청 ID");
            var l = d.Licenses.SingleOrDefault(x => x.Serial == Wire.NormalizeSerial(r.Serial));
            Require(l == null || l.Product == r.Product, "PRODUCT_DENIED", "이 시리얼번호는 " + (l == null ? "" : ProductName(d, l.Product)) + "용입니다. " + ProductName(d, r.Product) + "에는 " + ProductName(d, r.Product) + " 시리얼번호를 입력해 주세요.");
            Usable(d, l, r.AppVersion);
            var a = d.Activations.LastOrDefault(x => x.LicenseId == l.Id && x.MachineId == r.MachineId);
            Require(a == null || !a.Blocked, "DEVICE_REVOKED", "회수한 PC입니다. 회사에 재허용을 요청해 주세요.");
            if (a == null || !a.Active || a.SecretHash != Wire.Hash(r.DeviceSecret)) {
                // Seats are handed out automatically only for first installs; any later PC change waits for company approval.
                bool firstInstall = a == null && d.Activations.Where(x => x.LicenseId == l.Id).Select(x => x.MachineId).Distinct().Count() < l.Seats;
                if (!l.IsApproved) { queued = Queue(d, l, r, "Temporary", offline); return null; }
                if (!firstInstall) { queued = Queue(d, l, r, "PcChange", offline); return null; }
                a = new Activation { LicenseId = l.Id, MachineId = r.MachineId, SecretHash = Wire.Hash(r.DeviceSecret) }; d.Activations.Add(a);
                Log(d, offline ? "오프라인 최초 인증" : "PC 최초 인증", l.Id, r.Hostname);
            }
            a.Hostname = r.Hostname; a.OsVersion = r.OsVersion; a.Offline = offline; a.LastSeen = Now;
            return GrantFor(d, l, a, r.AppVersion, r.Nonce);
        });
        if (queued != null) throw queued;
        return reply;
    }
    public ApiReply Device(DeviceOperation r, bool release) { return Change(d => {
        Grant g; try { g = LicenseSignature.Verify(r.License, Settings.PublicKey); } catch { throw new RuleException("INVALID_GRANT", "서명된 인증 정보가 잘못되었습니다."); }
        var a = d.Activations.SingleOrDefault(x => x.Id == g.ActivationId && x.LicenseId == g.LicenseId);
        Require(a != null && a.Active && !a.Blocked && a.MachineId == g.MachineId, "DEVICE_REVOKED", "이 PC의 라이선스가 반납 또는 회수되었습니다.");
        Require(a.SecretHash == Wire.Hash(r.DeviceSecret ?? ""), "DEVICE_SECRET", "PC 인증값이 일치하지 않습니다.");
        var l = d.Licenses.Single(x => x.Id == a.LicenseId);
        if (release) { a.Active = false; Log(d, "고객 PC 반납", l.Id, a.Hostname); return new ApiReply { Code = "RELEASED", ServerTime = Now }; }
        Usable(d, l, r.AppVersion); Text(r.Nonce, 100, "요청 ID");
        if (Wire.Line(a.AppVersion) != Wire.Line(r.AppVersion)) Log(d, "버전 변경 확인", l.Id, a.Hostname + " · " + a.AppVersion + " → " + r.AppVersion);
        a.LastSeen = Now;
        return GrantFor(d, l, a, r.AppVersion, r.Nonce);
    }); }
    public ApprovalRequest Decide(string id, ApprovalDecision decision) { return Change(d => {
        var q = d.Requests.SingleOrDefault(x => x.Id == id); Require(q != null, "NOT_FOUND", "승인 요청이 없습니다.");
        Require(q.Status == "Pending", "INVALID_STATE", "이미 처리한 요청입니다.");
        Text(decision.Reason, 500, "처리 사유");
        var l = d.Licenses.Single(x => x.Id == q.LicenseId);
        q.DecidedAt = Now; q.Decision = decision.Reason;
        if (!decision.Approve) { q.Status = "Rejected"; Log(d, "승인 요청 거절", l.Id, q.Code + " · " + q.Hostname + " · " + decision.Reason); return q; }
        Require(!l.Blocked, "BLOCKED", "차단한 라이선스는 승인할 수 없습니다.");
        if (!l.IsApproved) {
            Term(decision.Purpose, decision.TermMonths);
            l.Purpose = decision.Purpose; l.TermMonths = decision.TermMonths; l.ApprovedAt = Now; l.ExpiresAt = Wire.AddKstMonths(l.ApprovedAt, l.TermMonths);
            Log(d, "임시 라이선스 승인", l.Id, q.Code + " · " + (l.Purpose == "Demo" ? "데모" : "테스트") + " · " + TermText(l.TermMonths) + " · 만료 " + Kst(l.ExpiresAt) + " KST · " + decision.Reason);
        }
        if (!string.IsNullOrEmpty(decision.ReplaceActivationId)) {
            var old = d.Activations.SingleOrDefault(x => x.Id == decision.ReplaceActivationId && x.LicenseId == l.Id);
            Require(old != null, "NOT_FOUND", "교체할 기존 PC를 찾지 못했습니다.");
            old.Active = false; old.Blocked = true; q.ReplacedActivationId = old.Id;
            Log(d, "PC 변경으로 기존 PC 회수", l.Id, old.Hostname + " → " + q.Hostname + " · " + q.Code);
        }
        // The same PC with a replaced local state (Windows reinstall etc.) gives up its previous record.
        foreach (var stale in d.Activations.Where(x => x.LicenseId == l.Id && x.MachineId == q.MachineId && x.Active)) stale.Active = false;
        Require(d.Activations.Count(x => x.LicenseId == l.Id && x.Active) < l.Seats, "SEAT_LIMIT", "설치 허용 PC 수를 모두 사용 중입니다. 교체할 기존 PC를 선택해 주세요.");
        d.Activations.Add(new Activation { LicenseId = l.Id, MachineId = q.MachineId, SecretHash = q.SecretHash, Hostname = q.Hostname, OsVersion = q.OsVersion, AppVersion = q.AppVersion, Offline = q.Offline });
        q.Status = "Approved";
        if (q.Type == "PcChange") Log(d, "PC 변경 승인", l.Id, q.Code + " · " + q.Hostname + " · " + decision.Reason);
        return q;
    }); }
    public LicenseRecord ChangeTerm(string id, TermChange t) { return Change(d => {
        var l = d.Licenses.SingleOrDefault(x => x.Id == id); Require(l != null, "NOT_FOUND", "라이선스가 없습니다.");
        Require(l.IsTemporary, "INVALID_INPUT", "임시 라이선스만 기간을 다시 승인할 수 있습니다.");
        Term(t.Purpose, t.TermMonths); Text(t.Reason, 500, "처리 사유");
        l.Purpose = t.Purpose; l.TermMonths = t.TermMonths; l.ApprovedAt = Now; l.ExpiresAt = Wire.AddKstMonths(l.ApprovedAt, l.TermMonths);
        Log(d, "임시 기간 재승인", l.Id, (l.Purpose == "Demo" ? "데모" : "테스트") + " · " + TermText(l.TermMonths) + " · 만료 " + Kst(l.ExpiresAt) + " KST · " + t.Reason);
        return l;
    }); }
    public static string TermText(int months) => months == 12 ? "1년" : months + "개월";
    public Product SaveProduct(Product p) { return Change(d => {
        Require(Products.IsValidCode(p.Code), "INVALID_INPUT", "제품 코드는 영문으로 시작하는 영문·숫자·점·하이픈 3~64자입니다 (예: Clorium.AutoSizing).");
        Text(p.Name, 100, "제품 이름"); Require((p.Notes?.Length ?? 0) <= 1000, "INVALID_INPUT", "비고가 너무 깁니다.");
        Require(!d.Products.Any(x => x.Code != p.Code && x.Name == p.Name.Trim()), "INVALID_INPUT", "같은 이름의 제품이 있습니다.");
        var existing = d.Products.SingleOrDefault(x => x.Code == p.Code);
        if (existing == null) { existing = new Product { Code = p.Code }; d.Products.Add(existing); Log(d, "제품 등록", p.Code, p.Name); }
        else Log(d, p.Disabled == existing.Disabled ? "제품 정보 변경" : p.Disabled ? "제품 인증 중지" : "제품 인증 재개", p.Code, p.Name);
        existing.Name = p.Name.Trim(); existing.Notes = p.Notes; existing.Disabled = p.Disabled; return existing;
    }); }
    public byte[] Backup() { lock (gate) {
        using var output = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(output, System.IO.Compression.ZipArchiveMode.Create, true)) {
            foreach (var file in new[] { "settings.json", "signing-key.xml", "licenses.json", "admin-token.txt" }.Where(f => File.Exists(Path.Combine(root, f)))) {
                using var writer = new StreamWriter(zip.CreateEntry(file).Open()); writer.Write(File.ReadAllText(Path.Combine(root, file)));
            }
        }
        return output.ToArray();
    } }
    public void Dispose() { signing.Dispose(); exclusive.Dispose(); }
}
