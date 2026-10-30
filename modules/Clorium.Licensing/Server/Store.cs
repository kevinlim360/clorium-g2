using Clorium.Licensing;
using System.Security.Cryptography;
using System.Text.Json;

namespace Clorium.LicenseServer;

public class RuleException : Exception
{
    public string Code { get; }
    public RuleException(string code, string message) : base(message) { Code = code; }
}
public sealed class Store : IDisposable
{
    private readonly object gate = new();
    private readonly string root;
    private readonly FileStream exclusive;
    private readonly RSA signing;
    private Database current;
    public ServerSettings Settings { get; }
    public string BackupFolder => Path.Combine(root, "backups");
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public Store(string folder)
    {
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
            current = new Database(); Save(current);
        } else {
            Settings = JsonSerializer.Deserialize<ServerSettings>(File.ReadAllText(settingsPath), Json);
            signing = RSA.Create(); signing.FromXmlString(File.ReadAllText(keyPath));
            if (signing.ToXmlString(false) != Settings.PublicKey) throw new InvalidDataException("서명키와 설정이 일치하지 않습니다.");
            current = JsonSerializer.Deserialize<Database>(File.ReadAllText(dbPath), Json) ?? throw new InvalidDataException("저장소가 비어 있습니다.");
        }
    }
    public bool IsAdmin(string token) { return token != null && CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(Wire.Hash(token)), System.Text.Encoding.ASCII.GetBytes(Settings.AdminTokenHash)); }
    private void Save(Database d) { Wire.AtomicWrite(Path.Combine(root, "licenses.json"), JsonSerializer.Serialize(d, Json)); }
    public T Change<T>(Func<Database, T> action) { lock (gate) { var next = JsonSerializer.Deserialize<Database>(JsonSerializer.Serialize(current, Json), Json); T result = action(next); Save(next); current = next; return result; } }
    public Database Snapshot() { lock (gate) return JsonSerializer.Deserialize<Database>(JsonSerializer.Serialize(current, Json), Json); }
    public static void Log(Database d, string action, string target, string detail = "") { d.Audit.Add(new Audit { Action = action, Target = target, Detail = detail }); }
    public static void Require(bool condition, string code, string message) { if (!condition) throw new RuleException(code, message); }
    public static void Text(string text, int max, string field) { Require(text != null && text.Trim().Length > 0 && text.Length <= max, "INVALID_INPUT", field + " 입력을 확인해 주세요."); }
    public static void Policy(LicenseRecord l) {
        Require(l.Kind is "Perpetual" or "Demo" or "Test", "INVALID_INPUT", "라이선스 종류가 잘못되었습니다.");
        Require(l.Seats > 0 && l.Seats <= 10000 && l.MaxMajor > 0 && l.MaxMajor <= 999 && l.GraceDays >= 1 && l.GraceDays <= 365, "INVALID_INPUT", "설치 수/버전/유예기간 범위를 확인해 주세요.");
        Require(l.Kind == "Perpetual" ? l.ExpiresAt == 0 : l.ExpiresAt > Wire.Now, "INVALID_INPUT", "영구권은 만료일 없음, 데모/테스트는 미래 만료일이어야 합니다.");
        Require(l.Kind == "Perpetual" || !l.AllowPermanentOffline, "INVALID_INPUT", "기간제는 무기한 오프라인으로 발급할 수 없습니다.");
    }
    private static void Usable(LicenseRecord l, string version) {
        Require(l != null, "UNKNOWN_LICENSE", "시리얼번호를 확인해 주세요.");
        Require(!l.Blocked, "BLOCKED", "회사에서 차단한 라이선스입니다.");
        Require(l.ExpiresAt == 0 || Wire.Now < l.ExpiresAt, "EXPIRED", "사용 기간이 만료되었습니다.");
        Require(Version.TryParse(version, out var v) && v.Major <= l.MaxMajor, "VERSION_DENIED", "허용한 주요 버전을 초과했습니다.");
    }
    private ApiReply GrantFor(Database d, LicenseRecord l, Activation a, string nonce)
    {
        long now = Wire.Now;
        long until = a.Offline && l.AllowPermanentOffline ? 253402300799 : now + l.GraceDays * 86400L;
        if (l.ExpiresAt > 0) until = Math.Min(until, l.ExpiresAt);
        a.LastGrantUntil = until;
        return new ApiReply { Code = "OK", ServerTime = now, License = LicenseSignature.Sign(new Grant {
            LicenseId = l.Id, ActivationId = a.Id, Customer = d.Customers.Single(x => x.Id == l.CustomerId).Company,
            Serial = l.Serial, MachineId = a.MachineId, Kind = l.Kind, MaxMajor = l.MaxMajor, ExpiresAt = l.ExpiresAt,
            IssuedAt = now, RefreshAfter = now + 86400, OfflineUntil = until, Nonce = nonce
        }, signing) };
    }
    public ApiReply Activate(ActivationRequest r, bool offline = false) { return Change(d => {
        Require(r.Product == Wire.Product, "PRODUCT_DENIED", "제품이 일치하지 않습니다.");
        Text(r.MachineId, 64, "PC 식별값"); Require(r.MachineId.Length == 64 && r.MachineId.All(Uri.IsHexDigit), "INVALID_INPUT", "PC 식별값 형식이 잘못되었습니다.");
        Text(r.DeviceSecret, 128, "PC 인증값"); Require(r.DeviceSecret.Length >= 40, "INVALID_INPUT", "PC 인증값이 너무 짧습니다.");
        Text(r.Hostname, 200, "PC 이름"); Text(r.OsVersion, 200, "OS"); Text(r.AppVersion, 40, "버전"); Text(r.Nonce, 100, "요청 ID");
        var l = d.Licenses.SingleOrDefault(x => x.Serial == Wire.NormalizeSerial(r.Serial)); Usable(l, r.AppVersion);
        var a = d.Activations.LastOrDefault(x => x.LicenseId == l.Id && x.MachineId == r.MachineId);
        Require(a == null || !a.Blocked, "DEVICE_REVOKED", "회수한 PC입니다. 회사에 재허용을 요청해 주세요.");
        if (a != null && a.Active) Require(a.SecretHash == Wire.Hash(r.DeviceSecret), "DEVICE_SECRET", "기존 PC 인증값이 다릅니다. 회사에서 이전 PC를 회수해야 합니다.");
        else {
            Require(d.Activations.Count(x => x.LicenseId == l.Id && x.Active) < l.Seats, "SEAT_LIMIT", "설치 가능 대수를 사용 중입니다. 기존 PC에서 반납하거나 회사에 이전을 요청해 주세요.");
            a = new Activation { LicenseId = l.Id, MachineId = r.MachineId, SecretHash = Wire.Hash(r.DeviceSecret) }; d.Activations.Add(a);
            Log(d, offline ? "오프라인 인증" : "PC 인증", l.Id, r.Hostname);
        }
        a.Hostname = r.Hostname; a.AppVersion = r.AppVersion; a.OsVersion = r.OsVersion; a.Offline = offline; a.LastSeen = Wire.Now;
        return GrantFor(d, l, a, r.Nonce);
    }); }
    public ApiReply Device(DeviceOperation r, bool release) { return Change(d => {
        Grant g; try { g = LicenseSignature.Verify(r.License, Settings.PublicKey); } catch { throw new RuleException("INVALID_GRANT", "서명된 인증 정보가 잘못되었습니다."); }
        var a = d.Activations.SingleOrDefault(x => x.Id == g.ActivationId && x.LicenseId == g.LicenseId);
        Require(a != null && a.Active && !a.Blocked && a.MachineId == g.MachineId, "DEVICE_REVOKED", "이 PC의 라이선스가 반납 또는 회수되었습니다.");
        Require(a.SecretHash == Wire.Hash(r.DeviceSecret ?? ""), "DEVICE_SECRET", "PC 인증값이 일치하지 않습니다.");
        var l = d.Licenses.Single(x => x.Id == a.LicenseId);
        if (release) { a.Active = false; Log(d, "고객 PC 반납", l.Id, a.Hostname); return new ApiReply { Code = "RELEASED", ServerTime = Wire.Now }; }
        Usable(l, r.AppVersion); Text(r.Nonce, 100, "요청 ID");
        a.AppVersion = r.AppVersion; a.LastSeen = Wire.Now;
        return GrantFor(d, l, a, r.Nonce);
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
