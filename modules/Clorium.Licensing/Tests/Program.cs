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
    static async Task<T> Api<T>(string path, string json = null, string method = "POST") {
        var request = new HttpRequestMessage(new HttpMethod(json == null ? "GET" : method), url + path);
        if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using (request) using (var response = await admin.SendAsync(request)) { string body = await response.Content.ReadAsStringAsync(); if (!response.IsSuccessStatusCode) throw new ApiException(body); return Wire.Read<T>(body); }
    }
    static async Task ApiRejected(string name, string code, Func<Task> action) { try { await action(); } catch (ApiException ex) { Check(name, ex.Message.Contains("\"" + code + "\"")); return; } throw new Exception("Expected rejection " + name); }
    static async Task Reject(string name, Func<Task> action) { try { await action(); } catch (InvalidDataException) { Check(name, true); return; } throw new Exception("Expected rejection " + name); }
    static async Task<InvalidDataException> Denied(string name, string code, Func<Task> action) { try { await action(); } catch (InvalidDataException ex) { Check(name + " [" + code + "]", LicenseDenial.Code(ex) == code); return ex; } throw new Exception("Expected " + code + " " + name); }
    static LicenseClient Client(string name, string machine = null, string version = "4.7.1.5", ClientConfig custom = null) { return new LicenseClient(custom ?? config, version, Path.Combine(root, name + ".state"), Wire.Hash(machine ?? name)); }
    static Task<Database> State() { return Api<Database>("/api/admin/state"); }
    static Task<LicenseRecord> Issue(Customer c, string kind, int seats = 1) { return Api<LicenseRecord>("/api/admin/licenses", Wire.Json(new LicenseRecord { CustomerId = c.Id, Kind = kind, Seats = seats, MaxMajor = 4 })); }
    static async Task<ApprovalRequest> Decide(string code, ApprovalDecision decision) { var q = (await State()).Requests.Single(x => x.Code == code); return await Api<ApprovalRequest>("/api/admin/requests/" + q.Id, Wire.Json(decision)); }
    [STAThread] static int Main(string[] args) { try {
        if (args[0] == "--restore") { Restore(args).GetAwaiter().GetResult(); return 0; }
        if (args[0] == "--render-client") { RenderClient(args); return 0; }
        Run(args).GetAwaiter().GetResult(); Console.WriteLine("LICENSING_INTEGRATION_PASSED=" + checks); return 0;
    } catch (Exception ex) { Console.Error.WriteLine(ex); return 1; } }
    static async Task Restore(string[] args)
    {
        url = args[1]; root = args[2]; config = Wire.Read<ClientConfig>(File.ReadAllText(Path.Combine(root, "restore-client-config.json")));
        admin = new HttpClient(); admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", File.ReadAllText(args[3]).Trim());
        var d = await State(); Check("restored administrator credential works", d.Licenses.Count >= 4 && d.Audit.Count > 10 && d.Requests.Count > 3);
        using (var b = Client("keeper")) { Check("old signature survives complete server restore", b.Validate() == null); await b.RefreshAsync(true); Check("old device renews after restore", b.Validate() == null); }
        using (var c = Client("offline")) Check("offline perpetual remains valid after server restore", c.Validate() == null);
        using (var t = Client("temp-demo")) { Check("approved temporary remains valid after restore", t.Validate() == null); await t.RefreshAsync(true); Check("temporary renews against restored approval", t.Validate() == null && t.CurrentGrant.Kind == "Demo"); }
        var auto = Wire.Read<ClientConfig>(File.ReadAllText(Path.Combine(root, "restore-autosizing-config.json")));
        using (var a = Client("auto-pc", machine: "studio-2", version: "2.1.0.0", custom: auto)) { await a.RefreshAsync(true); Check("AutoSizing license and product registry survive restore", a.Validate() == null && (await State()).Products.Count >= 3); }
        admin.Dispose(); Console.WriteLine("LICENSING_RESTORE_PASSED=" + checks);
    }
    static void RenderClient(string[] args)
    {
        root = args[1]; config = Wire.Read<ClientConfig>(File.ReadAllText(Path.Combine(root, "restore-client-config.json")));
        var app = new System.Windows.Application();
        using (var c = Client(args.Length > 3 ? args[3] : "keeper")) {
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
        var unreachable = new ClientConfig { ServerUrl = "http://127.0.0.1:9", PublicKey = config.PublicKey };
        using (var noAuth = new HttpClient()) using (var response = await noAuth.GetAsync(url + "/api/admin/state")) Check("unauthenticated admin rejected", response.StatusCode == HttpStatusCode.Unauthorized);
        var customer = await Api<Customer>("/api/admin/customers", Wire.Json(new Customer { Company = "테스트 고객사", Contact = "통합 검사", Email = "test@example.invalid" }));

        Console.WriteLine("--- 시나리오 1: 정식 영구 · 최초 1회 인증 후 영구 사용");
        var l = await Issue(customer, "Perpetual");
        using (var a = Client("pc-a")) {
            Check("fresh install says this PC has no license", a.Validate() == Wire.NoLicense);
            await a.ActivateAsync(l.Serial); var g = a.CurrentGrant;
            Check("perpetual activates without expiry", a.Validate() == null && g.ExpiresAt == 0 && g.VersionLine == "4.7");
            await Reject("mistyped replacement serial rejected", () => a.ActivateAsync("MISTYPED-SERIAL")); Check("mistyped replacement preserves valid license", a.Validate() == null);
            await a.ActivateAsync(l.Serial); Check("same device activation idempotent", (await State()).Activations.Count(x => x.Active && x.LicenseId == l.Id) == 1);
            Check("paid lifetime survives corrected PC clock without server", LicenseSignature.Validate(g, g.MachineId, "4.7.1.5", Wire.Now - 600, Wire.Now + 86400 * 365L) == null);
            Check("one-time perpetual remains valid 100 years without server", LicenseSignature.Validate(g, g.MachineId, "4.7.1.5", Wire.Now + 100L * 366 * 86400, Wire.Now) == null && g.RefreshAfter == Wire.Forever && g.OfflineUntil == Wire.Forever);
            await Api<LicenseRecord>("/api/admin/licenses/" + l.Id + "/status", Wire.Json(new StatusRequest { Blocked = true, Reason = "통합 검사" }));
            await a.RefreshAsync(); Check("perpetual performs no automatic server revalidation", a.Validate() == null);
            await Reject("explicit server block is not network grace", () => a.RefreshAsync(true)); Check("block persists locally", a.Validate() != null);
            await Api<LicenseRecord>("/api/admin/licenses/" + l.Id + "/status", Wire.Json(new StatusRequest { Blocked = false, Reason = "복원 검사" }));
            await a.RefreshAsync(true); Check("unblock restores existing device", a.Validate() == null);
        }

        Console.WriteLine("--- 시나리오 2: 버전 업그레이드 시 1회 확인 (Major.Minor)");
        using (var a = Client("pc-a", version: "4.7.1.9")) { Check("patch build 4.7.1.9 needs no new check", a.Validate() == null && !a.VersionCheckRequired); await a.RefreshAsync(); }
        using (var a = Client("pc-a", version: "4.8.0.0", custom: unreachable)) {
            Check("upgrade to 4.8 blocks until license check", a.VersionCheckRequired && a.Validate().Contains("4.7에서 4.8"));
            await a.RefreshAsync(); Check("offline upgraded PC stays blocked and explains why", a.Validate() != null && a.LastNetworkMessage.Contains("버전 변경 확인"));
        }
        using (var a = Client("pc-a", version: "4.8.0.0")) {
            await a.RefreshAsync(); Check("upgrade check runs automatically (no force) and passes", a.Validate() == null && a.CurrentGrant.VersionLine == "4.8" && a.CurrentGrant.ExpiresAt == 0);
            var d = await State(); Check("server records upgraded version", d.Activations.Single(x => x.LicenseId == l.Id && x.Active).AppVersion == "4.8.0.0" && d.Audit.Any(x => x.Target == l.Id && x.Action == "버전 변경 확인"));
            await a.RefreshAsync(); Check("after the one check no further contact is needed", a.Validate() == null);
        }
        using (var a = Client("pc-a", version: "5.0.0.0")) {
            Check("version above purchased major denied locally", a.Validate().Contains("허용하지 않는"));
            await Denied("server refuses unpurchased major", "VERSION_DENIED", () => a.RefreshAsync(true));
        }
        using (var a = Client("pc-a", version: "4.8.0.0")) Check("refused major upgrade does not damage existing 4.8 license", a.Validate() == null);
        using (var bad = Client("bad-version", version: "5.0.0.0")) await Reject("server enforces purchased version on activation", () => bad.ActivateAsync(l.Serial));

        Console.WriteLine("--- 시나리오 3: 다른 PC로 옮긴 설치는 '라이선스 없음'");
        File.Copy(Path.Combine(root, "pc-a.state"), Path.Combine(root, "copied.state"));
        using (var copied = Client("copied", machine: "other-hardware", version: "4.8.0.0")) Check("copied license on another PC shows no-license message", copied.Validate().StartsWith("이 PC에는 클로리움 라이선스가 없습니다"));
        File.WriteAllText(Path.Combine(root, "foreign.state"), Convert.ToBase64String(Encoding.UTF8.GetBytes("state encrypted on another PC")));
        using (var foreign = Client("foreign")) Check("undecryptable state from another PC shows no-license message", foreign.Validate() == Wire.NoLicense);
        Check("undecryptable original is preserved aside", Directory.GetFiles(root, "foreign.state.unreadable-*").Length == 1);

        Console.WriteLine("--- 시나리오 4: PC 변경은 회사 승인 후 사용");
        using (var a = Client("pc-a", version: "4.8.0.0")) using (var b = Client("pc-b", version: "4.8.0.0")) {
            var pending = await Denied("new PC on a used license waits for approval", "APPROVAL_PENDING", () => b.ActivateAsync(l.Serial));
            Check("new PC shows request code and stays unlicensed", b.Validate().Contains(LicenseDenial.RequestCode(pending)) && b.PendingCode == LicenseDenial.RequestCode(pending));
            await Denied("checking again before approval is still pending", "APPROVAL_PENDING", () => b.CheckApprovalAsync());
            var d = await State(); var q = d.Requests.Single(x => x.Code == LicenseDenial.RequestCode(pending));
            Check("one queued PcChange request with PC details", d.Requests.Count(x => x.LicenseId == l.Id && x.Status == "Pending") == 1 && q.Type == "PcChange" && q.Hostname == Environment.MachineName && q.AppVersion == "4.8.0.0");
            await ApiRejected("approval without retiring the old PC exceeds seats", "SEAT_LIMIT", () => Decide(q.Code, new ApprovalDecision { Approve = true, Reason = "교체 테스트" }));
            var old = d.Activations.Single(x => x.LicenseId == l.Id && x.Active);
            await Decide(q.Code, new ApprovalDecision { Approve = true, ReplaceActivationId = old.Id, Reason = "메인보드 교체" });
            await b.CheckApprovalAsync(); Check("approved new PC works permanently", b.Validate() == null && b.CurrentGrant.ExpiresAt == 0 && b.PendingCode == null);
            Check("limitation: offline old perpetual PC is not recalled until it connects", a.Validate() == null);
            await Denied("old PC is recalled when it connects", "DEVICE_REVOKED", () => a.RefreshAsync(true)); Check("recalled old PC is blocked locally", a.Validate() != null);
            await Denied("recalled old PC cannot reactivate itself", "DEVICE_REVOKED", () => a.ActivateAsync(l.Serial));
            var saved = Wire.Read<ClientState>(Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(File.ReadAllText(Path.Combine(root, "pc-b.state"))), Encoding.UTF8.GetBytes(Wire.Product + ":state-v1"), DataProtectionScope.LocalMachine)));
            await b.ReleaseAsync(); Check("release disables PC", b.Validate() != null);
            using (var replay = new HttpClient()) using (var response = await replay.PostAsync(url + "/api/refresh", new StringContent(Wire.Json(new DeviceOperation { License = saved.License, DeviceSecret = saved.DeviceSecret, AppVersion = "4.8.0.0", Nonce = Wire.Random() }), Encoding.UTF8, "application/json"))) Check("released token cannot refresh", response.StatusCode == HttpStatusCode.Conflict && Wire.Read<ApiReply>(await response.Content.ReadAsStringAsync()).Code == "DEVICE_REVOKED");
        }
        using (var c = Client("pc-c", version: "4.8.0.0")) {
            var pending = await Denied("even after release, a different PC needs approval", "APPROVAL_PENDING", () => c.ActivateAsync(l.Serial));
            await Decide(LicenseDenial.RequestCode(pending), new ApprovalDecision { Approve = false, Reason = "구매 확인 필요" });
            var rejected = await Denied("rejected PC is told so", "REQUEST_REJECTED", () => c.CheckApprovalAsync()); Check("rejection reason reaches the PC", rejected.Message.Contains("구매 확인 필요") && c.Validate() != null);
        }
        var two = await Issue(customer, "Perpetual", 2);
        using (var k = Client("keeper")) using (var s = Client("second")) using (var t = Client("third")) {
            await k.ActivateAsync(two.Serial); await s.ActivateAsync(two.Serial); Check("first installs up to purchased seats need no approval", k.Validate() == null && s.Validate() == null);
            await Denied("PC beyond purchased seats waits for approval", "APPROVAL_PENDING", () => t.ActivateAsync(two.Serial));
        }
        File.Delete(Path.Combine(root, "second.state"));
        using (var s = Client("second")) {
            var pending = await Denied("same PC with lost/replaced license state needs approval", "APPROVAL_PENDING", () => s.ActivateAsync(two.Serial));
            await Decide(LicenseDenial.RequestCode(pending), new ApprovalDecision { Approve = true, Reason = "Windows 재설치" });
            await s.CheckApprovalAsync(); Check("reinstalled PC approved without consuming another seat", s.Validate() == null && (await State()).Activations.Count(x => x.LicenseId == two.Id && x.Active) == 2);
        }

        Console.WriteLine("--- 시나리오 5: 임시 라이선스 (데모/테스트 · 1개월/3개월/1년)");
        var temp = await Issue(customer, "Temporary");
        Check("temporary serial is issued without purpose or term", temp.Purpose == null && temp.TermMonths == 0 && temp.ExpiresAt == 0);
        using (var t = Client("temp-demo")) {
            Check("first install asks for a license", t.Validate() == Wire.NoLicense);
            var pending = await Denied("temporary serial waits for company approval", "APPROVAL_PENDING", () => t.ActivateAsync(temp.Serial));
            Check("waiting PC shows request code", t.Validate().Contains(LicenseDenial.RequestCode(pending)) && (await State()).Requests.Single(x => x.Code == LicenseDenial.RequestCode(pending)).Type == "Temporary");
            await ApiRejected("purpose other than demo/test refused", "INVALID_INPUT", () => Decide(LicenseDenial.RequestCode(pending), new ApprovalDecision { Approve = true, Purpose = "Sales", TermMonths = 1, Reason = "x" }));
            await ApiRejected("term other than 1/3/12 months refused", "INVALID_INPUT", () => Decide(LicenseDenial.RequestCode(pending), new ApprovalDecision { Approve = true, Purpose = "Demo", TermMonths = 2, Reason = "x" }));
            await Decide(LicenseDenial.RequestCode(pending), new ApprovalDecision { Approve = true, Purpose = "Demo", TermMonths = 1, Reason = "전시회 데모" });
            await t.CheckApprovalAsync(); var g = t.CurrentGrant; var rec = (await State()).Licenses.Single(x => x.Id == temp.Id);
            Check("approved demo works with 1 KST calendar month", t.Validate() == null && g.Kind == "Demo" && g.TermMonths == 1 && g.ExpiresAt == Wire.AddKstMonths(rec.ApprovedAt, 1) && rec.ExpiresAt == g.ExpiresAt);
            Check("temporary works offline until one second before end", LicenseSignature.Validate(g, g.MachineId, "4.7.1.5", g.ExpiresAt - 1, g.IssuedAt) == null);
            Check("temporary stops exactly at end", LicenseSignature.Validate(g, g.MachineId, "4.7.1.5", g.ExpiresAt, g.IssuedAt).Contains("만료"));
            Check("temporary detects PC clock rollback", LicenseSignature.Validate(g, g.MachineId, "4.7.1.5", g.IssuedAt - 600, g.IssuedAt) != null);
            await Api<LicenseRecord>("/api/admin/licenses/" + temp.Id + "/status", Wire.Json(new StatusRequest { Blocked = true, Reason = "무접속 확인" }));
            await t.RefreshAsync(); Check("temporary makes no periodic server contact during its term", t.Validate() == null);
            await Api<LicenseRecord>("/api/admin/licenses/" + temp.Id + "/status", Wire.Json(new StatusRequest { Blocked = false, Reason = "무접속 확인 끝" }));
            await Api<LicenseRecord>("/api/admin/licenses/" + temp.Id + "/term", Wire.Json(new TermChange { Purpose = "Demo", TermMonths = 3, Reason = "데모 연장" }));
            await t.RefreshAsync(true); rec = (await State()).Licenses.Single(x => x.Id == temp.Id);
            Check("re-approved term reaches PC on online check", t.CurrentGrant.TermMonths == 3 && t.CurrentGrant.ExpiresAt == Wire.AddKstMonths(rec.ApprovedAt, 3));
        }
        using (var other = Client("temp-other")) await Denied("temporary on a second PC needs PC-change approval", "APPROVAL_PENDING", () => other.ActivateAsync(temp.Serial));
        foreach (int months in new[] { 3, 12 }) {
            var tl = await Issue(customer, "Temporary");
            using (var t = Client("temp-test-" + months)) {
                var pending = await Denied("test " + months + "-month request queued", "APPROVAL_PENDING", () => t.ActivateAsync(tl.Serial));
                await Decide(LicenseDenial.RequestCode(pending), new ApprovalDecision { Approve = true, Purpose = "Test", TermMonths = months, Reason = "내부 테스트" });
                await t.CheckApprovalAsync(); var rec = (await State()).Licenses.Single(x => x.Id == tl.Id);
                Check("approved test " + (months == 12 ? "1 year" : months + " months") + " expiry", t.Validate() == null && t.CurrentGrant.Kind == "Test" && t.CurrentGrant.ExpiresAt == Wire.AddKstMonths(rec.ApprovedAt, months));
            }
        }
        var convert = await Issue(customer, "Temporary");
        using (var t = Client("temp-convert")) {
            var pending = await Denied("conversion candidate queued", "APPROVAL_PENDING", () => t.ActivateAsync(convert.Serial));
            await Decide(LicenseDenial.RequestCode(pending), new ApprovalDecision { Approve = true, Purpose = "Demo", TermMonths = 1, Reason = "구매 전 데모" }); await t.CheckApprovalAsync();
            await Api<LicenseRecord>("/api/admin/licenses/" + convert.Id, Wire.Json(new LicenseRecord { CustomerId = customer.Id, Kind = "Perpetual", Seats = 1, MaxMajor = 4, Notes = "구매 완료" }), "PUT");
            await t.RefreshAsync(true); Check("temporary converts to perpetual without reinstall", t.Validate() == null && t.CurrentGrant.Kind == "Perpetual" && t.CurrentGrant.ExpiresAt == 0 && t.CurrentGrant.OfflineUntil == Wire.Forever);
        }

        Console.WriteLine("--- 시나리오 6: 인터넷 없는 PC (오프라인 파일)");
        var offline = await Issue(customer, "Perpetual");
        using (var c = Client("offline")) {
            string req = c.OfflineRequest(offline.Serial); var response = await Api<ApiReply>("/api/admin/offline", req);
            c.ImportOffline(Wire.Json(response)); Check("offline file exchange produces perpetual PC grant", c.Validate() == null && c.CurrentGrant.OfflineUntil == Wire.Forever);
            try { LicenseSignature.Verify(new SignedLicense { Payload = response.License.Payload + "!", Signature = response.License.Signature }, config.PublicKey); Check("tamper rejected", false); } catch (FormatException) { Check("tamper rejected", true); }
            using (var wrong = RSA.Create()) { wrong.KeySize = 2048; try { LicenseSignature.Verify(response.License, wrong.ToXmlString(false)); Check("wrong signer rejected", false); } catch (InvalidDataException) { Check("wrong signer rejected", true); } }
            using (var d = Client("offline-other")) { d.OfflineRequest(offline.Serial); try { d.ImportOffline(Wire.Json(response)); Check("offline response cannot migrate PC", false); } catch (InvalidDataException) { Check("offline response cannot migrate PC", true); } }
        }
        var offlineTemp = await Issue(customer, "Temporary");
        using (var c = Client("offline-temp")) {
            string req = c.OfflineRequest(offlineTemp.Serial);
            await ApiRejected("offline temporary file is queued for approval first", "APPROVAL_PENDING", () => Api<ApiReply>("/api/admin/offline", req));
            var q = (await State()).Requests.Single(x => x.LicenseId == offlineTemp.Id); Check("offline request is marked in queue", q.Offline && q.Status == "Pending");
            await Decide(q.Code, new ApprovalDecision { Approve = true, Purpose = "Test", TermMonths = 12, Reason = "오프라인 현장 테스트" });
            c.ImportOffline(Wire.Json(await Api<ApiReply>("/api/admin/offline", req))); Check("same request file after approval yields 1-year test", c.Validate() == null && c.CurrentGrant.Kind == "Test" && c.CurrentGrant.TermMonths == 12);
        }

        Console.WriteLine("--- 시나리오 7: 계정 중지 · 동시 요청");
        customer.Disabled = true; await Api<Customer>("/api/admin/customers", Wire.Json(customer));
        using (var k = Client("keeper")) { await Denied("disabled customer cannot recheck", "ACCOUNT_DISABLED", () => k.RefreshAsync(true)); }
        customer.Disabled = false; await Api<Customer>("/api/admin/customers", Wire.Json(customer));
        using (var k = Client("keeper")) { Check("denied state shown until next successful check", k.Validate() != null); await k.RefreshAsync(true); Check("re-enabled customer restores license", k.Validate() == null); }
        var race = await Issue(customer, "Perpetual");
        using (var a = Client("race1")) using (var b = Client("race2")) {
            Func<LicenseClient, Task<bool>> attempt = async c => { try { await c.ActivateAsync(race.Serial); return true; } catch (InvalidDataException) { return false; } };
            var results = await Task.WhenAll(attempt(a), attempt(b)); Check("concurrent activation cannot overbook", results.Count(x => x) == 1 && (await State()).Activations.Count(x => x.LicenseId == race.Id) == 1);
        }

        Console.WriteLine("--- 시나리오 8: 제품별 라이선스 (클로리움 · 클로리움 오토사이징 · 새 제품)");
        var auto = new ClientConfig { ServerUrl = url, PublicKey = config.PublicKey, Product = Products.AutoSizing, ProductName = "클로리움 오토사이징" };
        var registry = (await State()).Products;
        Check("server starts with Clorium and AutoSizing products", registry.Any(p => p.Code == Products.Clorium && p.Name == "클로리움") && registry.Any(p => p.Code == Products.AutoSizing && p.Name == "클로리움 오토사이징"));
        Check("licenses issued without product belong to Clorium", (await State()).Licenses.Single(x => x.Id == l.Id).Product == Products.Clorium);
        var autoPaid = await Api<LicenseRecord>("/api/admin/licenses", Wire.Json(new LicenseRecord { Product = Products.AutoSizing, CustomerId = customer.Id, Kind = "Perpetual", Seats = 1, MaxMajor = 2 }));
        var cloriumHere = await Issue(customer, "Perpetual");
        using (var ac = Client("auto-pc", machine: "studio-2", version: "2.1.0.0", custom: auto)) using (var cc = Client("clorium-pc", machine: "studio-2")) {
            Check("standalone AutoSizing first install says AutoSizing has no license", ac.Validate() == Wire.NoLicenseFor("클로리움 오토사이징"));
            var wrong = await Denied("Clorium serial refused in AutoSizing", "PRODUCT_DENIED", () => ac.ActivateAsync(l.Serial)); Check("message names both products", wrong.Message.Contains("클로리움용") && wrong.Message.Contains("클로리움 오토사이징 시리얼"));
            await Denied("AutoSizing serial refused in Clorium", "PRODUCT_DENIED", () => cc.ActivateAsync(autoPaid.Serial));
            await ac.ActivateAsync(autoPaid.Serial); await cc.ActivateAsync(cloriumHere.Serial);
            Check("same PC holds Clorium and AutoSizing licenses independently", ac.Validate() == null && cc.Validate() == null && ac.CurrentGrant.Product == Products.AutoSizing && cc.CurrentGrant.Product == Products.Clorium && ac.CurrentGrant.MachineId == cc.CurrentGrant.MachineId);
            Check("AutoSizing uses its own version line and major", ac.CurrentGrant.VersionLine == "2.1" && ac.CurrentGrant.MaxMajor == 2);
            Check("Clorium grant does not unlock AutoSizing", LicenseSignature.Validate(cc.CurrentGrant, cc.CurrentGrant.MachineId, "2.1.0.0", Wire.Now, Wire.Now, Products.AutoSizing, "클로리움 오토사이징") == Wire.NoLicenseFor("클로리움 오토사이징"));
        }
        File.Copy(Path.Combine(root, "clorium-pc.state"), Path.Combine(root, "auto-copied.state"));
        using (var ac = Client("auto-copied", machine: "studio-2", version: "2.1.0.0", custom: auto)) Check("Clorium state renamed as AutoSizing state is not a license", ac.Validate() == Wire.NoLicenseFor("클로리움 오토사이징"));
        using (var ac = Client("auto-pc-new", machine: "studio-3", version: "2.1.0.0", custom: auto)) {
            var pending = await Denied("AutoSizing moved to another PC needs approval", "APPROVAL_PENDING", () => ac.ActivateAsync(autoPaid.Serial));
            var q = (await State()).Requests.Single(x => x.Code == LicenseDenial.RequestCode(pending)); Check("approval request belongs to the AutoSizing license", q.LicenseId == autoPaid.Id && ac.Validate().Contains("클로리움 오토사이징"));
        }
        var autoTemp = await Api<LicenseRecord>("/api/admin/licenses", Wire.Json(new LicenseRecord { Product = Products.AutoSizing, CustomerId = customer.Id, Kind = "Temporary", Seats = 1, MaxMajor = 2 }));
        using (var ac = Client("auto-temp", machine: "studio-4", version: "2.1.0.0", custom: auto)) {
            var pending = await Denied("AutoSizing temporary waits for approval", "APPROVAL_PENDING", () => ac.ActivateAsync(autoTemp.Serial));
            await Decide(LicenseDenial.RequestCode(pending), new ApprovalDecision { Approve = true, Purpose = "Demo", TermMonths = 3, Reason = "오토사이징 데모" });
            await ac.CheckApprovalAsync(); Check("AutoSizing 3-month demo works", ac.Validate() == null && ac.CurrentGrant.Kind == "Demo" && ac.CurrentGrant.TermMonths == 3 && ac.CurrentGrant.Product == Products.AutoSizing);
        }
        await ApiRejected("product code must be a plain identifier", "INVALID_INPUT", () => Api<Product>("/api/admin/products", Wire.Json(new Product { Code = "새 제품 !", Name = "잘못된 코드" })));
        await ApiRejected("license for an unregistered product refused", "INVALID_INPUT", () => Api<LicenseRecord>("/api/admin/licenses", Wire.Json(new LicenseRecord { Product = "Clorium.Unknown", CustomerId = customer.Id, Kind = "Perpetual", Seats = 1, MaxMajor = 1 })));
        await Api<Product>("/api/admin/products", Wire.Json(new Product { Code = "Clorium.Viewer3D", Name = "클로리움 3D 뷰어" }));
        var future = await Api<LicenseRecord>("/api/admin/licenses", Wire.Json(new LicenseRecord { Product = "Clorium.Viewer3D", CustomerId = customer.Id, Kind = "Perpetual", Seats = 1, MaxMajor = 1 }));
        var viewer = new ClientConfig { ServerUrl = url, PublicKey = config.PublicKey, Product = "Clorium.Viewer3D", ProductName = "클로리움 3D 뷰어" };
        using (var vc = Client("viewer", machine: "studio-2", version: "1.0.0.0", custom: viewer)) {
            await vc.ActivateAsync(future.Serial); Check("newly registered product uses the same license system", vc.Validate() == null && vc.CurrentGrant.Product == "Clorium.Viewer3D" && vc.CurrentGrant.ExpiresAt == 0);
            await Api<Product>("/api/admin/products", Wire.Json(new Product { Code = "Clorium.Viewer3D", Name = "클로리움 3D 뷰어", Disabled = true }));
            await Denied("disabled product refuses online check", "PRODUCT_DISABLED", () => vc.RefreshAsync(true));
            await ApiRejected("disabled product cannot be issued", "PRODUCT_DISABLED", () => Api<LicenseRecord>("/api/admin/licenses", Wire.Json(new LicenseRecord { Product = "Clorium.Viewer3D", CustomerId = customer.Id, Kind = "Perpetual", Seats = 1, MaxMajor = 1 })));
            await Api<Product>("/api/admin/products", Wire.Json(new Product { Code = "Clorium.Viewer3D", Name = "클로리움 3D 뷰어", Disabled = false }));
            await vc.RefreshAsync(true); Check("re-enabled product works again", vc.Validate() == null);
        }
        using (var cc = Client("clorium-pc", machine: "studio-2")) Check("disabling another product leaves Clorium untouched", cc.Validate() == null);

        Console.WriteLine("--- 기록 · 내보내기 · 백업");
        using (var response = await admin.GetAsync(url + "/api/admin/export")) { string csv = await response.Content.ReadAsStringAsync(); Check("CSV export includes customer and serial", csv.Contains(l.Serial)); Check("CSV shows temporary purpose and term", csv.Contains(temp.Serial) && csv.Contains("임시") && csv.Contains("3개월") && csv.Contains("승인대기")); Check("CSV separates products", csv.TrimStart('﻿').StartsWith("제품,") && csv.Contains("\"클로리움 오토사이징\"") && csv.Contains(autoPaid.Serial)); }
        using (var response = await admin.GetAsync(url + "/api/admin/backup")) { File.WriteAllBytes(Path.Combine(root, "backup.zip"), await response.Content.ReadAsByteArrayAsync()); Check("full server backup saved", new FileInfo(Path.Combine(root, "backup.zip")).Length > 1000); }
        var state = await State();
        Check("audit links PC change lifecycle to license", new[] { "PC 변경 승인 요청", "PC 변경으로 기존 PC 회수", "PC 변경 승인", "고객 PC 반납", "승인 요청 거절" }.All(action => state.Audit.Any(x => x.Target == l.Id && x.Action == action)));
        Check("audit links temporary lifecycle to license", new[] { "임시 사용 승인 요청", "임시 라이선스 승인", "임시 기간 재승인" }.All(action => state.Audit.Any(x => x.Target == temp.Id && x.Action == action)));
        Wire.AtomicWrite(Path.Combine(root, "restore-client-config.json"), Wire.Json(config));
        Wire.AtomicWrite(Path.Combine(root, "restore-autosizing-config.json"), Wire.Json(auto));
        Wire.AtomicWrite(Path.Combine(root, "test-result.json"), "{\"Passed\":" + checks + ",\"Runtime\":\".NET Framework 4.8\",\"Server\":\"real loopback HTTP\"}");
        admin.Dispose();
    }
}
class ApiException : Exception { public ApiException(string body) : base(body) { } }
[System.Runtime.Serialization.DataContract] class StatusRequest
{
    [System.Runtime.Serialization.DataMember] public bool Blocked { get; set; }
    [System.Runtime.Serialization.DataMember] public string Reason { get; set; }
}
