using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace Clorium.Licensing
{
    public static class Wire
    {
        public const string Product = "Clorium.CameraControl";
        public const long Forever = 253402300799;
        public static byte[] Bytes<T>(T value) { using (var s = new MemoryStream()) { new DataContractJsonSerializer(typeof(T)).WriteObject(s, value); return s.ToArray(); } }
        public static string Json<T>(T value) { return Encoding.UTF8.GetString(Bytes(value)); }
        public static T Read<T>(string json) { if (json == null || json.Length > 1048576) throw new InvalidDataException("파일 크기가 너무 큽니다."); using (var s = new MemoryStream(Encoding.UTF8.GetBytes(json))) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(s); }
        public static long Now { get { return DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }
        public static string Hash(string value) { using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant(); }
        public static string Random(int bytes = 32) { var b = new byte[bytes]; using (var r = RandomNumberGenerator.Create()) r.GetBytes(b); return Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_'); }
        public static string Serial() { const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; var b = new byte[24]; using (var r = RandomNumberGenerator.Create()) r.GetBytes(b); string raw = new string(b.Select(x => alphabet[x & 31]).ToArray()); return "CLG2-" + string.Join("-", Enumerable.Range(0, 4).Select(i => raw.Substring(i * 6, 6))); }
        public static string NormalizeSerial(string s) { return (s ?? "").Trim().ToUpperInvariant(); }
        public static void AtomicWrite(string path, string value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                using (var fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                    var b = Encoding.UTF8.GetBytes(value); fs.Write(b, 0, b.Length); fs.Flush(true);
                }
                if (File.Exists(path)) File.Replace(tmp, path, path + ".bak"); else File.Move(tmp, path);
            } finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }
    }
    [DataContract] public class ActivationRequest
    {
        [DataMember] public string Serial { get; set; }
        [DataMember] public string Product { get; set; } = Wire.Product;
        [DataMember] public string MachineId { get; set; }
        [DataMember] public string Hostname { get; set; }
        [DataMember] public string OsVersion { get; set; }
        [DataMember] public string AppVersion { get; set; }
        [DataMember] public string Nonce { get; set; }
        [DataMember] public string DeviceSecret { get; set; }
    }
    [DataContract] public class SignedLicense
    {
        [DataMember] public string Payload { get; set; }
        [DataMember] public string Signature { get; set; }
    }
    [DataContract] public class Grant
    {
        [DataMember] public int Schema { get; set; } = 1;
        [DataMember] public string Product { get; set; } = Wire.Product;
        [DataMember] public string LicenseId { get; set; }
        [DataMember] public string ActivationId { get; set; }
        [DataMember] public string Customer { get; set; }
        [DataMember] public string Serial { get; set; }
        [DataMember] public string MachineId { get; set; }
        [DataMember] public string Kind { get; set; }
        [DataMember] public int MaxMajor { get; set; }
        [DataMember] public long IssuedAt { get; set; }
        [DataMember] public long ExpiresAt { get; set; }
        [DataMember] public long RefreshAfter { get; set; }
        [DataMember] public int RenewalMonths { get; set; }
        [DataMember] public long OfflineUntil { get; set; }
        [DataMember] public string Nonce { get; set; }
    }
    [DataContract] public class ApiReply
    {
        [DataMember] public string Code { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public SignedLicense License { get; set; }
        [DataMember] public long ServerTime { get; set; }
    }
    [DataContract] public class DeviceOperation
    {
        [DataMember] public SignedLicense License { get; set; }
        [DataMember] public string DeviceSecret { get; set; }
        [DataMember] public string AppVersion { get; set; }
        [DataMember] public string Hostname { get; set; }
        [DataMember] public string OsVersion { get; set; }
        [DataMember] public string Nonce { get; set; }
    }
    public static class LicenseSignature
    {
        public static SignedLicense Sign(Grant value, RSA key) {
            byte[] bytes = Wire.Bytes(value);
            return new SignedLicense { Payload = Convert.ToBase64String(bytes), Signature = Convert.ToBase64String(key.SignData(bytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) };
        }
        public static Grant Verify(SignedLicense signed, string publicKey) {
            if (signed == null || signed.Payload == null || signed.Payload.Length > 32768 || signed.Signature == null || signed.Signature.Length > 4096) throw new InvalidDataException("라이선스 형식이 올바르지 않습니다.");
            byte[] bytes = Convert.FromBase64String(signed.Payload);
            using (var rsa = RSA.Create()) {
                rsa.FromXmlString(publicKey);
                if (!rsa.VerifyData(bytes, Convert.FromBase64String(signed.Signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) throw new InvalidDataException("라이선스 서명이 올바르지 않습니다.");
            }
            var g = Wire.Read<Grant>(Encoding.UTF8.GetString(bytes));
            if (g.Schema != 1 || g.Product != Wire.Product || string.IsNullOrWhiteSpace(g.ActivationId) || (g.Kind != "Perpetual" && g.Kind != "Demo" && g.Kind != "Test") || g.IssuedAt <= 0 || g.OfflineUntil < g.IssuedAt || (g.Kind != "Perpetual" && g.ExpiresAt <= 0)) throw new InvalidDataException("라이선스 권한이 올바르지 않습니다.");
            return g;
        }
        public static string Validate(Grant g, string machine, string version, long now, long lastSeen) {
            if (g.MachineId != machine) return "다른 PC에 발급된 라이선스입니다.";
            Version v; if (!Version.TryParse(version, out v) || v.Major > g.MaxMajor) return "구매한 라이선스에서 허용하지 않는 프로그램 버전입니다.";
            bool lifetime = g.Kind == "Perpetual" && g.ExpiresAt == 0 && g.OfflineUntil == Wire.Forever && g.RefreshAfter == Wire.Forever;
            if (!lifetime && now + 300 < Math.Max(g.IssuedAt, lastSeen)) return "PC 시간이 이전으로 변경되었습니다. 시간을 맞춘 뒤 온라인 인증해 주세요.";
            if (g.ExpiresAt > 0 && now >= g.ExpiresAt) return g.RenewalMonths == 3 ? "직원용 3개월 인증 기한입니다. 인터넷에 연결해 재인증해 주세요." : "사용 기간이 만료되었습니다.";
            if (now >= g.OfflineUntil) return "인증 확인 기한이 지났습니다. 서버에 연결해 주세요.";
            return null;
        }
    }
}
