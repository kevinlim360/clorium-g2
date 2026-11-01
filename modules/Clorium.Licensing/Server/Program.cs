using Clorium.Licensing;
using Clorium.LicenseServer;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text.Json;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://127.0.0.1:5088");
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = 128 * 1024);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = null);
builder.Services.Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c => RateLimitPartition.GetFixedWindowLimiter(
        c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
string dataRoot = builder.Configuration["data"] ?? Environment.GetEnvironmentVariable("CLORIUM_LICENSE_DATA") ?? Path.Combine(AppContext.BaseDirectory, "data");
using var store = new Store(dataRoot);
if (builder.Configuration.GetValue<bool>("initialize-only")) return;
builder.Services.AddSingleton(store);
builder.Services.AddHostedService<BackupWorker>();
var app = builder.Build();
app.UseForwardedHeaders();
app.UseRateLimiter();
app.Use(async (context, next) => {
    context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    if (context.Request.Path.StartsWithSegments("/api/admin")) {
        string bearer = context.Request.Headers.Authorization.ToString();
        if (!bearer.StartsWith("Bearer ", StringComparison.Ordinal) || !store.IsAdmin(bearer.Substring(7))) {
            context.Response.StatusCode = 401; await context.Response.WriteAsJsonAsync(new { Message = "관리자 인증이 필요합니다." }); return;
        }
    }
    try { await next(); }
    catch (RuleException ex) { context.Response.StatusCode = 409; await context.Response.WriteAsync(Wire.Json(new ApiReply { Code = ex.Code, Message = ex.Message, ServerTime = Wire.Now })); }
    catch (Exception ex) when (ex is JsonException or System.Runtime.Serialization.SerializationException or FormatException or InvalidDataException or BadHttpRequestException) {
        context.Response.StatusCode = 400; await context.Response.WriteAsJsonAsync(new { Message = "요청 형식이 올바르지 않습니다." });
    }
    catch (Exception ex) { app.Logger.LogError(ex, "License operation failed"); context.Response.StatusCode = 500; await context.Response.WriteAsJsonAsync(new { Message = "서버 처리 중 오류입니다. 관리자에게 문의해 주세요." }); }
});
async Task<T> Read<T>(HttpRequest r) { using var reader = new StreamReader(r.Body); return Wire.Read<T>(await reader.ReadToEndAsync()); }
IResult Reply(ApiReply value) => Results.Text(Wire.Json(value), "application/json");
app.MapGet("/health", () => Results.Ok(new { Status = "ok" }));
app.MapPost("/api/activate", async (HttpRequest r) => Reply(store.Activate(await Read<ActivationRequest>(r))));
app.MapPost("/api/refresh", async (HttpRequest r) => Reply(store.Device(await Read<DeviceOperation>(r), false)));
app.MapPost("/api/release", async (HttpRequest r) => Reply(store.Device(await Read<DeviceOperation>(r), true)));
app.MapGet("/api/admin/state", () => store.Snapshot());
app.MapGet("/api/admin/public-key", () => Results.Text(store.Settings.PublicKey, "application/xml"));
app.MapPost("/api/admin/customers", (Customer c) => store.Change(d => {
    Store.Text(c.Company, 200, "고객사명");
    Store.Require((c.Contact?.Length ?? 0) <= 200 && (c.Email?.Length ?? 0) <= 300 && (c.Notes?.Length ?? 0) <= 2000, "INVALID_INPUT", "고객 정보가 너무 깁니다.");
    var existing = d.Customers.SingleOrDefault(x => x.Id == c.Id);
    if (c.IsInternal) Store.Text(c.Email, 300, "직원 ID / 이메일");
    if (existing != null && !c.IsInternal) Store.Require(!d.Licenses.Any(l => l.CustomerId == c.Id && l.RenewalMonths == 3), "INVALID_INPUT", "직원용 라이선스가 연결된 계정입니다. 계정 중지를 이용해 주세요.");
    if (existing == null) { c.Id = Guid.NewGuid().ToString("N"); d.Customers.Add(c); } else { existing.Company = c.Company; existing.Contact = c.Contact; existing.Email = c.Email; existing.Notes = c.Notes; existing.IsInternal = c.IsInternal; existing.Disabled = c.Disabled; }
    Store.Log(d, "고객 정보 저장", c.Id, c.Company); return c;
}));
app.MapPost("/api/admin/licenses", (LicenseRecord l) => store.Change(d => {
    Store.Policy(l); Store.Require(d.Customers.Any(x => x.Id == l.CustomerId), "INVALID_INPUT", "고객사를 선택해 주세요.");
    Store.AccountPolicy(d, l); l.NextReauthAt = 0;
    l.Id = Guid.NewGuid().ToString("N"); l.Serial = Wire.Serial(); l.CreatedAt = Wire.Now; l.Blocked = false;
    d.Licenses.Add(l); Store.Log(d, "라이선스 발급", l.Id, l.Kind); return l;
}));
app.MapPut("/api/admin/licenses/{id}", (string id, LicenseRecord changes) => store.Change(d => {
    var l = d.Licenses.SingleOrDefault(x => x.Id == id); Store.Require(l != null, "NOT_FOUND", "라이선스가 없습니다.");
    Store.Policy(changes); Store.Require(changes.Seats >= d.Activations.Count(x => x.LicenseId == id && x.Active), "SEAT_LIMIT", "현재 활성 PC보다 적게 줄일 수 없습니다.");
    changes.CustomerId = l.CustomerId; Store.AccountPolicy(d, changes);
    if (l.RenewalMonths != changes.RenewalMonths) l.NextReauthAt = 0;
    l.Kind = changes.Kind; l.MaxMajor = changes.MaxMajor; l.Seats = changes.Seats; l.GraceDays = changes.GraceDays; l.ExpiresAt = changes.ExpiresAt;
    l.AllowPermanentOffline = changes.AllowPermanentOffline; l.Notes = changes.Notes; l.RenewalMonths = changes.RenewalMonths;
    Store.Log(d, "라이선스 정책 변경", id, l.Kind); return l;
}));
app.MapPost("/api/admin/licenses/{id}/status", (string id, StatusChange r) => store.Change(d => {
    Store.Text(r.Reason, 500, "사유"); var l = d.Licenses.SingleOrDefault(x => x.Id == id); Store.Require(l != null, "NOT_FOUND", "라이선스가 없습니다.");
    l.Blocked = r.Blocked; Store.Log(d, r.Blocked ? "라이선스 차단" : "차단 해제", id, r.Reason); return l;
}));
app.MapPost("/api/admin/activations/{id}/status", (string id, StatusChange r) => store.Change(d => {
    Store.Text(r.Reason, 500, "사유"); var a = d.Activations.SingleOrDefault(x => x.Id == id); Store.Require(a != null, "NOT_FOUND", "PC 기록이 없습니다.");
    if (r.Blocked) { a.Active = false; a.Blocked = true; } else a.Blocked = false;
    Store.Log(d, r.Blocked ? "PC 강제 회수" : "PC 재인증 허용", a.LicenseId, a.Hostname + " / " + r.Reason); return a;
}));
app.MapPost("/api/admin/offline", async (HttpRequest r) => Reply(store.Activate(await Read<ActivationRequest>(r), true)));
app.MapGet("/api/admin/backup", () => Results.File(store.Backup(), "application/zip", "clorium-license-backup-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".zip"));
app.MapGet("/api/admin/export", () => {
    var d = store.Snapshot(); string Cell(string s) { s ??= ""; if (s.StartsWith('=') || s.StartsWith('+') || s.StartsWith('-') || s.StartsWith('@') || s.StartsWith('\t') || s.StartsWith('\r')) s = "'" + s; return "\"" + s.Replace("\"", "\"\"") + "\""; }
    var rows = new List<string> { "고객사,담당자,시리얼,종류,허용버전,설치허용수,활성PC수,만료UTC,차단,직원계정,재인증개월,다음인증UTC" };
    foreach (var l in d.Licenses) { var c = d.Customers.Single(x => x.Id == l.CustomerId); rows.Add(string.Join(",", new[] { c.Company, c.Contact, l.Serial, l.Kind, l.MaxMajor.ToString(), l.Seats.ToString(), d.Activations.Count(x => x.LicenseId == l.Id && x.Active).ToString(), l.RenewalMonths == 3 ? "3개월 재인증" : l.ExpiresAt == 0 ? "영구" : DateTimeOffset.FromUnixTimeSeconds(l.ExpiresAt).ToString("O"), l.Blocked.ToString(), c.IsInternal.ToString(), l.RenewalMonths.ToString(), l.NextReauthAt > 0 ? DateTimeOffset.FromUnixTimeSeconds(l.NextReauthAt).ToString("O") : "" }.Select(Cell))); }
    return Results.File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(string.Join("\r\n", rows))).ToArray(), "text/csv", "clorium-licenses.csv");
});
app.Logger.LogInformation("Clorium license server ready. Admin bootstrap credential: data/admin-token.txt (keep private).");
app.Run();
public class StatusChange { public bool Blocked { get; set; } public string Reason { get; set; } }
