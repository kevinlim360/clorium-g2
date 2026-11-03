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
// Requests per minute per client address; only the isolated test run raises it.
int permits = builder.Configuration.GetValue<int?>("rate-limit") ?? 120;
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c => RateLimitPartition.GetFixedWindowLimiter(
        c.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
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
    catch (RuleException ex) { context.Response.StatusCode = 409; await context.Response.WriteAsync(Wire.Json(new ApiReply { Code = ex.Code, Message = ex.Message, RequestCode = ex.RequestCode, ServerTime = Wire.Now })); }
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
    if (existing == null) { c.Id = Guid.NewGuid().ToString("N"); d.Customers.Add(c); } else { existing.Company = c.Company; existing.Contact = c.Contact; existing.Email = c.Email; existing.Notes = c.Notes; existing.IsInternal = c.IsInternal; existing.Disabled = c.Disabled; }
    Store.Log(d, "고객 정보 저장", c.Id, c.Company); return c;
}));
app.MapPost("/api/admin/licenses", (LicenseRecord l) => store.Change(d => {
    l.Product ??= Products.Clorium;
    Store.Policy(d, l); Store.Require(d.Customers.Any(x => x.Id == l.CustomerId), "INVALID_INPUT", "고객사를 선택해 주세요.");
    // Temporary purpose and term are chosen at approval, not at issue.
    l.Purpose = null; l.TermMonths = 0; l.ApprovedAt = 0; l.ExpiresAt = 0; l.RenewalMonths = 0; l.NextReauthAt = 0;
    l.Id = Guid.NewGuid().ToString("N"); l.Serial = Wire.Serial(); l.CreatedAt = Wire.Now; l.Blocked = false;
    d.Licenses.Add(l); Store.Log(d, "라이선스 발급", l.Id, Store.ProductName(d, l.Product) + " · " + (l.IsTemporary ? "임시 · 최초 PC 요청 시 목적·기간 승인" : "영구")); return l;
}));
app.MapPut("/api/admin/licenses/{id}", (string id, LicenseRecord changes) => store.Change(d => {
    var l = d.Licenses.SingleOrDefault(x => x.Id == id); Store.Require(l != null, "NOT_FOUND", "라이선스가 없습니다.");
    changes.Product = l.Product; // a serial never moves to another product
    Store.Policy(d, changes); Store.Require(changes.Seats >= d.Activations.Count(x => x.LicenseId == id && x.Active), "SEAT_LIMIT", "현재 활성 PC보다 적게 줄일 수 없습니다.");
    Store.Require(l.Kind == changes.Kind || changes.Kind == "Perpetual", "INVALID_INPUT", "영구 라이선스를 임시로 바꿀 수 없습니다. 새 임시 시리얼을 발급해 주세요.");
    if (l.IsTemporary && changes.Kind == "Perpetual") { l.Purpose = null; l.TermMonths = 0; l.ApprovedAt = 0; l.ExpiresAt = 0; Store.Log(d, "정식 영구 전환", id, changes.Notes ?? ""); }
    l.Kind = changes.Kind; l.MaxMajor = changes.MaxMajor; l.Seats = changes.Seats; l.Notes = changes.Notes;
    Store.Log(d, "라이선스 정책 변경", id, (l.IsTemporary ? "임시" : "영구") + " · PC " + l.Seats + " · " + l.MaxMajor + ".x"); return l;
}));
app.MapPost("/api/admin/licenses/{id}/term", (string id, TermChange t) => store.ChangeTerm(id, t));
app.MapPost("/api/admin/products", (Product p) => store.SaveProduct(p));
app.MapPost("/api/admin/requests/{id}", (string id, ApprovalDecision decision) => store.Decide(id, decision));
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
    var rows = new List<string> { "제품,고객사,담당자,시리얼,종류,목적,기간,허용버전,설치허용수,활성PC수,승인대기,만료KST,차단,직원계정" };
    foreach (var l in d.Licenses) { var c = d.Customers.Single(x => x.Id == l.CustomerId); rows.Add(string.Join(",", new[] { Store.ProductName(d, l.Product), c.Company, c.Contact, l.Serial, l.IsTemporary ? "임시" : "영구", l.Purpose == "Demo" ? "데모" : l.Purpose == "Test" ? "테스트" : "", l.TermMonths > 0 ? Store.TermText(l.TermMonths) : "", l.MaxMajor.ToString(), l.Seats.ToString(), d.Activations.Count(x => x.LicenseId == l.Id && x.Active).ToString(), d.Requests.Count(x => x.LicenseId == l.Id && x.Status == "Pending").ToString(), !l.IsTemporary ? "영구" : l.ApprovedAt == 0 ? "승인 전" : Store.Kst(l.ExpiresAt), l.Blocked.ToString(), c.IsInternal.ToString() }.Select(Cell))); }
    return Results.File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(string.Join("\r\n", rows))).ToArray(), "text/csv", "clorium-licenses.csv");
});
app.Logger.LogInformation("Clorium license server ready. Admin bootstrap credential: data/admin-token.txt (keep private).");
app.Run();
public class StatusChange { public bool Blocked { get; set; } public string Reason { get; set; } }
