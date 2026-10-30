using System;

namespace CameraControl.Devices.Classes;

// Assigned before camera initialization. Legacy hosts retain their existing startup license check.
public static class CaptureAuthorization
{
    public static Func<string> Validate { private get; set; }
    public static void AssertAllowed()
    {
        string error = Validate?.Invoke();
        if (error != null) throw new CaptureLicenseException("라이선스 확인: " + error + "\n파일 메뉴의 ‘라이선스 · PC 이전’에서 확인해 주세요.");
    }
}

public sealed class CaptureLicenseException : InvalidOperationException
{
    public CaptureLicenseException(string message) : base(message) { }
}
