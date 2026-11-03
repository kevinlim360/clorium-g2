using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Clorium.Licensing
{
    [DataContract] public class ClientConfig
    {
        [DataMember] public string ServerUrl { get; set; }
        [DataMember] public string PublicKey { get; set; }
        // Empty in configs exported before products existed: those builds are Clorium.
        [DataMember] public string Product { get; set; }
        [DataMember] public string ProductName { get; set; }
    }
    [DataContract] public class ClientState
    {
        [DataMember] public SignedLicense License { get; set; }
        [DataMember] public string DeviceSecret { get; set; } = Wire.Random(48);
        [DataMember] public long LastSeen { get; set; }
        [DataMember] public string Denied { get; set; }
        [DataMember] public string PendingNonce { get; set; }
        [DataMember] public string PendingSerial { get; set; }
        [DataMember] public string PendingCode { get; set; }
    }
    // Server policy denials stay InvalidDataException (what callers already catch); the server code travels in Exception.Data.
    public static class LicenseDenial
    {
        public static InvalidDataException Create(string code, string message, string requestCode) { var ex = new InvalidDataException(message); ex.Data["LicenseCode"] = code; ex.Data["RequestCode"] = requestCode; return ex; }
        public static string Code(Exception ex) { return ex == null ? null : ex.Data["LicenseCode"] as string; }
        public static string RequestCode(Exception ex) { return ex == null ? null : ex.Data["RequestCode"] as string; }
    }
    public class LicenseClient : IDisposable
    {
        private readonly ClientConfig config;
        private readonly string statePath;
        private readonly string machine;
        private readonly string version;
        private readonly string product;
        private readonly string productName;
        private readonly object gate = new object();
        private readonly SemaphoreSlim operation = new SemaphoreSlim(1, 1);
        private readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        private readonly FileStream exclusive;
        private ClientState state;
        private string storageError;
        public string LastNetworkMessage { get; private set; }
        public string MachineId { get { return machine; } }
        public string Version { get { return version; } }
        public string Product { get { return product; } }
        public string ProductName { get { return productName; } }
        public LicenseClient(ClientConfig config, string version, string customStatePath = null, string testMachine = null)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.PublicKey)) throw new InvalidDataException("라이선스 공개키 설정이 없습니다.");
            Uri uri; if (!Uri.TryCreate(config.ServerUrl, UriKind.Absolute, out uri) || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))) throw new InvalidDataException("인증 서버는 HTTPS 주소여야 합니다.");
            product = string.IsNullOrWhiteSpace(config.Product) ? Products.Clorium : config.Product.Trim();
            if (!Products.IsValidCode(product)) throw new InvalidDataException("라이선스 제품 설정이 올바르지 않습니다.");
            productName = string.IsNullOrWhiteSpace(config.ProductName) ? Products.DefaultName(product) : config.ProductName.Trim();
            this.config = config; this.version = version; machine = testMachine ?? ReadMachineId();
            // Each product keeps its own state, so a standalone AutoSizing PC and a Clorium PC are licensed independently.
            statePath = customStatePath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Clorium", "Licensing", Products.StateFileName(product));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(statePath)));
            exclusive = new FileStream(statePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            try {
                state = File.Exists(statePath) ? Wire.Read<ClientState>(Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(File.ReadAllText(statePath)), Entropy(), DataProtectionScope.LocalMachine))) : new ClientState();
                if (string.IsNullOrEmpty(state.DeviceSecret)) throw new InvalidDataException();
                if (!File.Exists(statePath)) Persist();
            } catch (Exception ex) when (!(ex is IOException) && !(ex is UnauthorizedAccessException)) {
                // A state copied from another PC cannot be decrypted here: keep the original aside and start as an unlicensed PC.
                try { File.Move(statePath, statePath + ".unreadable-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss")); state = new ClientState(); Persist(); }
                catch { state = new ClientState(); storageError = "PC 인증 저장소를 읽을 수 없습니다. " + Wire.NoLicenseFor(productName); }
            } catch { state = new ClientState(); storageError = "라이선스 저장소에 접근할 수 없습니다. 설치 폴더 권한을 확인해 주세요."; }
        }
        public string PendingCode { get { lock (gate) return state.PendingCode; } }
        public string PendingSerial { get { lock (gate) return state.PendingSerial; } }
        public bool VersionCheckRequired { get { var g = CurrentGrant; return g != null && g.Product == product && g.MachineId == machine && LicenseSignature.VersionChanged(g, version); } }
        private byte[] Entropy() { return Encoding.UTF8.GetBytes(product + ":state-v1"); }
        public static string ReadMachineId()
        {
            string uuid = null;
            try { using (var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct")) using (var items = searcher.Get()) foreach (ManagementBaseObject item in items) using (item) { string s = Convert.ToString(item["UUID"]); Guid id; if (Guid.TryParse(s, out id) && id != Guid.Empty && s != "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF") { uuid = id.ToString("N"); break; } } } catch (ManagementException) { } catch (UnauthorizedAccessException) { } catch (System.Runtime.InteropServices.COMException) { }
            if (uuid == null) using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography")) uuid = Convert.ToString(key == null ? null : key.GetValue("MachineGuid"));
            if (string.IsNullOrWhiteSpace(uuid)) throw new InvalidDataException("PC 식별값을 읽지 못했습니다.");
            return Wire.Hash(Wire.Product + ":hardware-v1:" + uuid.Trim().ToUpperInvariant());
        }
        private void Persist() { Wire.AtomicWrite(statePath, Convert.ToBase64String(ProtectedData.Protect(Wire.Bytes(state), Entropy(), DataProtectionScope.LocalMachine))); }
        public Grant CurrentGrant { get { lock (gate) { try { return LicenseSignature.Verify(state.License, config.PublicKey); } catch { return null; } } } }
        public string Validate()
        {
            lock (gate) {
                if (storageError != null) return storageError;
                if (!string.IsNullOrEmpty(state.Denied)) return state.Denied;
                if (state.License == null) return string.IsNullOrEmpty(state.PendingCode) ? Wire.NoLicenseFor(productName) : productName + " 회사 승인 대기 중입니다 (요청번호 " + state.PendingCode + "). 승인 후 '승인 확인'을 눌러 주세요.";
                try {
                    var g = LicenseSignature.Verify(state.License, config.PublicKey);
                    string error = LicenseSignature.Validate(g, machine, version, Wire.Now, state.LastSeen, product, productName);
                    if (error != null) return error;
                    if (Wire.Now - state.LastSeen >= 60) { state.LastSeen = Math.Max(state.LastSeen, Wire.Now); Persist(); }
                    return null;
                } catch (Exception ex) when (ex is InvalidDataException || ex is FormatException || ex is CryptographicException || ex is System.Runtime.Serialization.SerializationException) { return Wire.NoLicenseFor(productName); }
                catch (IOException) { return "라이선스 저장소에 쓸 수 없습니다. 설치 폴더 권한을 확인해 주세요."; }
                catch (UnauthorizedAccessException) { return "라이선스 저장소 접근 권한이 없습니다."; }
            }
        }
        private ActivationRequest Request(string serial)
        {
            if (storageError != null) throw new InvalidDataException(storageError);
            return new ActivationRequest { Serial = Wire.NormalizeSerial(serial), Product = product, MachineId = machine, Hostname = Environment.MachineName,
                OsVersion = Environment.OSVersion.VersionString, AppVersion = version, DeviceSecret = state.DeviceSecret, Nonce = Wire.Random() };
        }
        private void Accept(ApiReply reply, string nonce)
        {
            var g = LicenseSignature.Verify(reply.License, config.PublicKey);
            if (g.Nonce != nonce) throw new InvalidDataException("인증 응답이 현재 요청과 일치하지 않습니다.");
            string error = LicenseSignature.Validate(g, machine, version, Wire.Now, g.IssuedAt, product, productName);
            if (error != null) throw new InvalidDataException(error);
            state.License = reply.License; state.LastSeen = Math.Max(Wire.Now, g.IssuedAt); state.Denied = null; state.PendingNonce = null; state.PendingSerial = null; state.PendingCode = null;
            Persist(); LastNetworkMessage = "인증 완료";
        }
        private async Task<ApiReply> Post(string path, object body, bool invalidateCurrent = true)
        {
            string json = body is ActivationRequest ? Wire.Json((ActivationRequest)body) : Wire.Json((DeviceOperation)body);
            using (var response = await http.PostAsync(config.ServerUrl.TrimEnd('/') + path, new StringContent(json, Encoding.UTF8, "application/json")).ConfigureAwait(false)) {
                string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                // Only our structured policy denials invalidate a cached entitlement; network/server overload does not.
                if ((int)response.StatusCode == 409) {
                    var denied = Wire.Read<ApiReply>(text);
                    // A version above the purchased major is already refused locally, so it never poisons the stored license.
                    if (invalidateCurrent && new[] { "BLOCKED", "EXPIRED", "DEVICE_REVOKED", "INVALID_GRANT", "DEVICE_SECRET", "UNKNOWN_LICENSE", "ACCOUNT_DISABLED" }.Contains(denied.Code))
                        lock (gate) { state.Denied = denied.Message ?? "인증이 거부되었습니다."; Persist(); }
                    throw LicenseDenial.Create(denied.Code, denied.Message, denied.RequestCode);
                }
                response.EnsureSuccessStatusCode(); var reply = Wire.Read<ApiReply>(text);
                if (reply.Code != "OK" && reply.Code != "RELEASED") throw new InvalidDataException("인증 서버 응답을 확인할 수 없습니다.");
                return reply;
            }
        }
        public async Task ActivateAsync(string serial)
        {
            await operation.WaitAsync().ConfigureAwait(false);
            try {
                ActivationRequest request; bool sameLicense;
                lock (gate) { request = Request(serial); sameLicense = CurrentGrant != null && CurrentGrant.Serial == request.Serial; }
                ApiReply reply;
                try { reply = await Post("/api/activate", request, sameLicense).ConfigureAwait(false); }
                catch (InvalidDataException ex) when (LicenseDenial.Code(ex) == "APPROVAL_PENDING" || LicenseDenial.Code(ex) == "REQUEST_REJECTED") {
                    lock (gate) { if (state.License == null) { state.PendingSerial = request.Serial; state.PendingCode = LicenseDenial.RequestCode(ex); Persist(); } }
                    throw;
                }
                lock (gate) Accept(reply, request.Nonce);
            } finally { operation.Release(); }
        }
        // Asks the server again with the serial that is waiting for company approval.
        public Task CheckApprovalAsync()
        {
            string serial = PendingSerial; if (string.IsNullOrEmpty(serial)) throw new InvalidDataException("승인 대기 중인 요청이 없습니다. 시리얼번호를 입력해 인증해 주세요.");
            return ActivateAsync(serial);
        }
        public async Task RefreshAsync(bool force = false)
        {
            if (!await operation.WaitAsync(0).ConfigureAwait(false)) return;
            try {
                DeviceOperation request;
                lock (gate) {
                    // Besides an explicit request, only an expired temporary term or a Major.Minor change contacts the server.
                    var g = CurrentGrant; if (g == null || g.Product != product || (!force && Wire.Now < g.RefreshAfter && !LicenseSignature.VersionChanged(g, version))) return;
                    request = new DeviceOperation { License = state.License, DeviceSecret = state.DeviceSecret, AppVersion = version, Hostname = Environment.MachineName, OsVersion = Environment.OSVersion.VersionString, Nonce = Wire.Random() };
                }
                var reply = await Post("/api/refresh", request).ConfigureAwait(false);
                lock (gate) Accept(reply, request.Nonce);
            } catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException) { LastNetworkMessage = VersionCheckRequired ? "인증 서버에 연결하지 못했습니다. 버전 변경 확인을 위해 인터넷 연결 후 다시 시도해 주세요." : "서버 연결 대기 · 기존 인증의 유효기간 안에서 계속 사용합니다."; }
            finally { operation.Release(); }
        }
        public async Task ReleaseAsync()
        {
            await operation.WaitAsync().ConfigureAwait(false);
            try {
                DeviceOperation r; lock (gate) { if (state.License == null) return; r = new DeviceOperation { License = state.License, DeviceSecret = state.DeviceSecret, AppVersion = version }; }
                await Post("/api/release", r).ConfigureAwait(false);
                lock (gate) { state.License = null; state.Denied = "이 PC의 라이선스를 반납했습니다. 새 PC에서 인증해 주세요."; Persist(); }
            } finally { operation.Release(); }
        }
        public string OfflineRequest(string serial)
        {
            operation.Wait(); try { lock (gate) { var r = Request(serial); state.PendingNonce = r.Nonce; state.PendingSerial = r.Serial; Persist(); return Wire.Json(r); } } finally { operation.Release(); }
        }
        public void ImportOffline(string response)
        {
            operation.Wait(); try { lock (gate) {
                if (storageError != null) throw new InvalidDataException(storageError);
                if (string.IsNullOrEmpty(state.PendingNonce)) throw new InvalidDataException("먼저 이 PC에서 오프라인 요청 파일을 만들어 주세요.");
                var reply = Wire.Read<ApiReply>(response); var g = LicenseSignature.Verify(reply.License, config.PublicKey);
                if (g.Serial != state.PendingSerial) throw new InvalidDataException("요청한 시리얼과 다른 응답입니다.");
                Accept(reply, state.PendingNonce);
            } } finally { operation.Release(); }
        }
        public void Dispose() { http.Dispose(); exclusive.Dispose(); operation.Dispose(); }
    }
}
