using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Clorium.Licensing;
using CameraControl.Devices.Classes;

namespace CameraControl;

internal static class ManagedLicenseRuntime
{
    private static LicenseClient client;
    private static DispatcherTimer timer;
    private static bool checking;
    internal static bool IsManaged
    {
        get {
#if CLORIUM_MANAGED_LICENSE
            return true;
#else
            return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clorium-license.json"));
#endif
        }
    }
    internal static bool Initialize()
    {
        if (!IsManaged) return true;
        try {
            string json;
#if CLORIUM_MANAGED_LICENSE
            using (var resource = typeof(App).Assembly.GetManifestResourceStream("Clorium.ManagedLicenseConfig")) {
                if (resource == null) throw new InvalidDataException("배포된 인증 설정이 없습니다. 회사에서 발급한 설치본을 다시 설치해 주세요.");
                using (var reader = new StreamReader(resource)) json = reader.ReadToEnd();
            }
#else
            json = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clorium-license.json"));
#endif
            client = new LicenseClient(Wire.Read<ClientConfig>(json), typeof(App).Assembly.GetName().Version.ToString());
            CaptureAuthorization.Validate = () => client.Validate();
            if (client.Validate() != null) {
                try { client.RefreshAsync(true).GetAwaiter().GetResult(); } catch (InvalidDataException) { }
                if (client.Validate() != null) Show();
            } else { _ = Refresh(); }
            if (client.Validate() != null) return false;
            timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            timer.Tick += async (s, e) => { await Refresh(); string error = client.Validate(); if (error != null && Application.Current.MainWindow != null) Application.Current.MainWindow.Title = "Clorium · 라이선스 확인 필요"; };
            timer.Start(); return true;
        } catch (Exception ex) { MessageBox.Show(ex.Message, "클로리움 라이선스", MessageBoxButton.OK, MessageBoxImage.Warning); return false; }
    }
    private static async System.Threading.Tasks.Task Refresh()
    {
        if (checking || client == null) return;
        checking = true;
        try { await client.RefreshAsync(); } catch (Exception ex) { System.Diagnostics.Trace.WriteLine("License sync: " + ex.GetType().Name); } finally { checking = false; }
    }
    internal static void Show()
    {
        if (client == null) { MessageBox.Show("현재 설치본은 기존 발급 라이선스를 사용합니다. 새 인증 방식은 회사에서 발급한 설치본으로 전환할 때 적용됩니다.", "클로리움 라이선스"); return; }
        var app = Application.Current; var previous = app.ShutdownMode; var previousMain = app.MainWindow;
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try { var window = new LicenseWindow(client); if (previousMain != null && previousMain.IsVisible) window.Owner = previousMain; window.ShowDialog(); }
        finally { app.MainWindow = previousMain; app.ShutdownMode = previous; }
    }
}
