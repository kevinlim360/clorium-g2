using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Clorium.Licensing;
using Clorium.LicenseServer;

class Program
{
    static int checks; static HttpClient admin; static string url, root; static ClientConfig config;
    static void Check(string name, bool condition) { if (!condition) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); checks++; }
    static async Task<T> Api<T>(string path, object value = null, string method = "POST") {
        var request = new HttpRequestMessage(new HttpMethod(value == null ? "GET" : method), url + path);
        if (value != null) request.Content = new StringContent(value is string ? (string)value : value is LicenseRecord ? Wire.Json((LicenseRecord)value) : value is Customer ? Wire.Json((Customer)value) : Wire.Json((StatusRequest)value), Encoding.UTF8, "application/json");
        using (request) using (var response = await admin.SendAsync(request)) { string body = await response.Content.ReadAsStringAsync(); if (!response.IsSuccessStatusCode) throw new Exception("API error " + response.StatusCode + " " + body); return Wire.Read<T>(body); }
    }
    static async Task Reject(string name, Func<Task> action) { try { await action(); } catch (InvalidDataException) { Check(name, true); return; } throw new Exception("Expected rejection " + name); }
    static LicenseClient Client(string name, string machine = null, string version = "4.7.1.5") { return new LicenseClient(config, version, Path.Combine(root, name + ".state"), Wire.Hash(machine ?? name)); }
    [STAThread] static int Main(string[] args) { try {
        if (args[0] == "--restore") { Restore(args).GetAwaiter().GetResult(); return 0; }
        if (args[0] == "--render-client") { RenderClient(args); return 0; }
        Run(args).GetAwaiter().GetResult(); Console.WriteLine("LICENSING_INTEGRATION_PASSED=" + checks); return 0;
    } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
    static async Task Restore(string[] args)
    {
        url = args[1]; root = args[2]; config = Wire.Read<ClientConfig>(File.ReadAllText(Path.Combine(root, "restore-client-config.json")));
        admin = new HttpClient(); admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", File.ReadAllText(args[3]).Trim());
        var d = await Api<Database>("/api/admin/state"); Check("restored administrator credential works", d.Licenses.Count >= 4 && d.Audit.Count > 10);
        using (var b = Client("pc-b")) { Check("old signature survives complete server restore", b.Validate() == null); await b.RefreshAsync(true); Check("old device renews after restore", b.Validate() == null); }
        using (var c = Client("offline")) Check("offline perpetual remains valid after server restore", c.Validate() == null);
        admin.Dispose(); Console.WriteLine("LICENSING_RESTORE_PASSED=" + checks);
    }
    static void RenderClient(string[] args)
    {
        root = args[1]; config = Wire.Read<ClientConfig>(File.ReadAllText(Path.Combine(root, "restore-client-config.json")));
        var app = new System.Windows.Application();
        using (var c = Client("pc-b")) {
            var window = new LicenseWindow(c); window.Show(); window.UpdateLayout();
            var image = new System.Windows.Media.Imaging.RenderTargetBitmap(560, 500, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); image.Render(window);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image)); using (var f = File.Create(args[2])) encoder.Save(f); window.Close();
        }
    }
    static async Task Run(string[] args)
    {
        url = args[0]; root = args[1]; Directory.CreateDirectory(root);
        admin = new HttpClient(); admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", File.ReadAllText(args[2]).Trim());
        using (var response = await admin.GetAsync(url + "/api/admin/public-key")) config = new ClientConfig { ServerUrl = url, PublicKey = await response.Content.ReadAsStringAsync() };
        using (var noAuth = new HttpClient()) using (var response = await noAuth.GetAsync(url + "/api/admin/state")) Check("unauthenticated admin rejected", response.StatusCode == HttpStatusCode.Unauthorized);
        var customer = await Api<Customer>("/api/admin/customers", new Customer { Company = "테스트 고객사", Contact = "통합 검사", Email = "test@example.invalid" });
        var l = await Api<LicenseRecord>("/api/admin/licenses", new LicenseRecord { CustomerId = customer.Id, Kind = "Perpetual", Seats = 1, MaxMajor = 4, GraceDays = 30 });
        using (var a = Client("pc-a")) using (var b = Client("pc-b")) {
            await a.ActivateAsync(l.Serial); Check("perpetual activates without expiry", a.Validate() == null && a.CurrentGrant.ExpiresAt == 0);
            await Reject("mistyped replacement serial rejected", () => a.ActivateAsync("MISTYPED-SERIAL")); Check("mistyped replacement preserves valid license", a.Validate() == null);
            await a.ActivateAsync(l.Serial); Check("same device activation idempotent", (await Api<Database>("/api/admin/state")).Activations.Count(x => x.Active && x.LicenseId == l.Id) == 1);
            await Reject("second PC seat limit", () => b.ActivateAsync(l.Serial));
            var g = a.CurrentGrant;
            Check("other machine denied by local verifier", LicenseSignature.Validate(g, Wire.Hash("other"), "4.7.1.5", Wire.Now, Wire.Now) != null);
            Check("unsupported major denied locally", LicenseSignature.Validate(g, g.MachineId, "5.0.0.0", Wire.Now, Wire.Now) != null);
            Check("clock rollback denied", LicenseSignature.Validate(g, g.MachineId, "4.7.1.5", Wire.Now - 600, Wire.Now) != null);
            Check("network grace expires", LicenseSignature.Validate(g, g.MachineId, "4.7.1.5", g.OfflineUntil + 1, Wire.Now) != null);
            await Api<LicenseRecord>("/api/admin/licenses/" + l.Id + "/status", new StatusRequest { Blocked = true, Reason = "통합 검사" });
            await Reject("explicit server block is not network grace", () => a.RefreshAsync(true)); Check("block persists locally", a.Validate() != null);
            await Api<LicenseRecord>("/api/admin/licenses/" + l.Id + "/status", new StatusRequest { Blocked = false, Reason = "복원 검사" });
            await a.RefreshAsync(true); Check("unblock restores existing device", a.Validate() == null);
            var saved = Wire.Read<ClientState>(Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(File.ReadAllText(Path.Combine(root, "pc-a.state"))), Encoding.UTF8.GetBytes(Wire.Product + ":state-v1"), DataProtectionScope.LocalMachine)));
            await a.ReleaseAsync(); Check("release disables old PC", a.Validate() != null);
            await b.ActivateAsync(l.Serial); Check("new PC takes released seat", b.Validate() == null);
            using (var replay = new HttpClient()) using (var response = await replay.PostAsync(url + "/api/refresh", new StringContent(Wire.Json(new DeviceOperation { License = saved.License, DeviceSecret = saved.DeviceSecret, AppVersion = "4.7.1.5", Nonce = Wire.Random() }), Encoding.UTF8, "application/json"))) Check("released token cannot refresh", response.StatusCode == HttpStatusCode.Conflict && Wire.Read<ApiReply>(await response.Content.ReadAsStringAsync()).Code == "DEVICE_REVOKED");
            var before = (await Api<Database>("/api/admin/state")).Activations.Single(x => x.LicenseId == l.Id && x.Active);
            await Api<Activation>("/api/admin/activations/" + before.Id + "/status", new StatusRequest { Blocked = true, Reason = "고장 PC 회수" });
            await Reject("forced recall denies cached refresh", () => b.RefreshAsync(true));
            await Reject("recalled PC cannot reactivate itself", () => b.ActivateAsync(l.Serial));
            await Api<Activation>("/api/admin/activations/" + before.Id + "/status", new StatusRequest { Blocked = false, Reason = "장치 재허용" });
            await b.ActivateAsync(l.Serial); Check("admin can reallow recalled PC", b.Validate() == null);
        }
        using (var b = Client("pc-b")) { Check("protected state survives process/session reopen", b.Validate() == null); await b.RefreshAsync(true); }
        var offline = await Api<LicenseRecord>("/api/admin/licenses", new LicenseRecord { CustomerId = customer.Id, Kind = "Perpetual", Seats = 1, MaxMajor = 4, GraceDays = 30, AllowPermanentOffline = true });
        using (var c = Client("offline")) {
            string req = c.OfflineRequest(offline.Serial); var response = await Api<ApiReply>("/api/admin/offline", req);
            c.ImportOffline(Wire.Json(response)); Check("offline file exchange produces perpetual PC grant", c.Validate() == null && c.CurrentGrant.OfflineUntil > Wire.Now + 86400L * 365 * 100);
            try { LicenseSignature.Verify(new SignedLicense { Payload = response.License.Payload + "!", Signature = response.License.Signature }, config.PublicKey); Check("tamper rejected", false); } catch (FormatException) { Check("tamper rejected", true); }
            using (var wrong = RSA.Create()) { wrong.KeySize = 2048; try { LicenseSignature.Verify(response.License, wrong.ToXmlString(false)); Check("wrong signer rejected", false); } catch (InvalidDataException) { Check("wrong signer rejected", true); } }
            using (var d = Client("offline-other")) { d.OfflineRequest(offline.Serial); try { d.ImportOffline(Wire.Json(response)); Check("offline response cannot migrate PC", false); } catch (InvalidDataException) { Check("offline response cannot migrate PC", true); } }
        }
        var demo = await Api<LicenseRecord>("/api/admin/licenses", new LicenseRecord { CustomerId = customer.Id, Kind = "Demo", Seats = 1, MaxMajor = 4, ExpiresAt = Wire.Now + 4 });
        using (var d = Client("demo")) {
            await d.ActivateAsync(demo.Serial); Check("demo has fixed signed expiry", d.Validate() == null && d.CurrentGrant.ExpiresAt == demo.ExpiresAt);
            await Task.Delay(5000); Check("running demo expires without restart", d.Validate() != null);
            await Reject("expired demo cannot renew", () => d.RefreshAsync(true));
            demo.Kind = "Perpetual"; demo.ExpiresAt = 0; await Api<LicenseRecord>("/api/admin/licenses/" + demo.Id, demo, "PUT");
            await d.RefreshAsync(true); Check("demo converts without reinstall/state deletion", d.Validate() == null && d.CurrentGrant.Kind == "Perpetual");
        }
        var race = await Api<LicenseRecord>("/api/admin/licenses", new LicenseRecord { CustomerId = customer.Id, Kind = "Test", Seats = 1, MaxMajor = 4, ExpiresAt = Wire.Now + 86400 });
        using (var a = Client("race1")) using (var b = Client("race2")) {
            Func<LicenseClient, Task<bool>> attempt = async c => { try { await c.ActivateAsync(race.Serial); return true; } catch (InvalidDataException) { return false; } };
            var results = await Task.WhenAll(attempt(a), attempt(b)); Check("concurrent activation cannot overbook", results.Count(x => x) == 1);
        }
        using (var old = Client("bad-version", version: "5.0.0.0")) await Reject("server enforces purchased version", () => old.ActivateAsync(l.Serial));
        using (var response = await admin.GetAsync(url + "/api/admin/export")) Check("CSV export includes customer and serial", (await response.Content.ReadAsStringAsync()).Contains(l.Serial));
        using (var response = await admin.GetAsync(url + "/api/admin/backup")) { File.WriteAllBytes(Path.Combine(root, "backup.zip"), await response.Content.ReadAsByteArrayAsync()); Check("full server backup saved", new FileInfo(Path.Combine(root, "backup.zip")).Length > 1000); }
        var state = await Api<Database>("/api/admin/state"); Check("audit links lifecycle to license", state.Audit.Any(x => x.Target == l.Id && x.Action == "고객 PC 반납") && state.Audit.Any(x => x.Target == l.Id && x.Action == "PC 강제 회수"));
        Wire.AtomicWrite(Path.Combine(root, "restore-client-config.json"), Wire.Json(config));
        Wire.AtomicWrite(Path.Combine(root, "test-result.json"), "{\"Passed\":" + checks + ",\"Runtime\":\".NET Framework 4.8\",\"Server\":\"real loopback HTTP\"}");
        admin.Dispose();
    }
}
[System.Runtime.Serialization.DataContract] class StatusRequest
{
    [System.Runtime.Serialization.DataMember] public bool Blocked { get; set; }
    [System.Runtime.Serialization.DataMember] public string Reason { get; set; }
}
