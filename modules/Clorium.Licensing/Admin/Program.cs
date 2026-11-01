using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Clorium.Licensing;
using Clorium.LicenseServer;
using Microsoft.Win32;

namespace Clorium.LicenseManager;

public static class Program
{
    [STAThread] public static int Main(string[] args)
    {
        var app = new Application();
        var window = new ManagerWindow();
        if (args.Length == 5 && args[0] == "--check") {
            window.ConnectionUrl = args[1]; window.BootstrapToken = File.ReadAllText(args[2]).Trim();
            window.Loaded += async (s, e) => { try { await window.VerifyConnectedUi(args[3], args[4]); app.Shutdown(0); } catch (Exception ex) { File.WriteAllText(args[4] + ".error", ex.ToString()); app.Shutdown(1); } };
            return app.Run(window);
        }
        if (args.Length == 2 && args[0] == "--render") {
            window.LoadExample(); window.Width = 1440; window.Height = 900; window.Show(); window.UpdateLayout();
            var image = new RenderTargetBitmap(1440, 900, 96, 96, PixelFormats.Pbgra32); image.Render(window);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using (var file = File.Create(args[1])) encoder.Save(file); window.Close(); return 0;
        }
        for (int i = 0; i + 1 < args.Length; i++) {
            if (args[i] == "--server") window.ConnectionUrl = args[++i];
            else if (args[i] == "--token-file") window.BootstrapToken = File.ReadAllText(args[++i]).Trim();
        }
        return app.Run(window);
    }
}
public class ManagerWindow : Window
{
    public string ConnectionUrl { set => server.Text = value; }
    public string BootstrapToken { set { password.Password = value; Loaded += async (s, e) => { try { await Refresh(); } catch (Exception ex) { status.Text = ex.Message; } }; } }
    private readonly TextBox server = new() { Text = "http://127.0.0.1:5088", Width = 255 };
    private readonly PasswordBox password = new() { Width = 225 };
    private readonly TextBox search = new() { Width = 280 };
    private readonly TextBlock summary = new() { FontSize = 24, FontWeight = FontWeights.SemiBold };
    private readonly TextBlock status = new() { Text = "서버 주소와 관리자 인증키를 입력해 연결하세요.", TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock detail = new() { Text = "라이선스를 선택하면 설치 PC와 변경 이력이 표시됩니다.", Margin = new Thickness(0, 0, 0, 12), TextWrapping = TextWrapping.Wrap };
    private readonly DataGrid licenses = Table();
    private readonly DataGrid customers = Table();
    private readonly DataGrid devices = Table();
    private readonly DataGrid history = Table();
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private Database data = new();
    private bool busy;
    private string selectedId;
    public ManagerWindow()
    {
        Title = "Clorium · 라이선스 관리"; Width = 1440; Height = 900; MinWidth = 1160; MinHeight = 740;
        FontFamily = new FontFamily("Malgun Gothic"); FontSize = 13; Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));
        Resources.Add(typeof(Button), new Style(typeof(Button)) { Setters = { new Setter(Control.PaddingProperty, new Thickness(13, 7, 13, 7)), new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 7, 6)), new Setter(Control.BackgroundProperty, Brushes.White), new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(211, 218, 228))) } });
        Resources.Add(typeof(TextBox), new Style(typeof(TextBox)) { Setters = { new Setter(Control.PaddingProperty, new Thickness(8, 6, 8, 6)), new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center) } });
        var root = new DockPanel { Margin = new Thickness(28, 22, 28, 18) }; Content = root;
        var heading = new StackPanel(); DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        heading.Children.Add(new TextBlock { Text = "CLORIUM   /   LICENSE MANAGER", Foreground = Brushes.SlateGray, FontSize = 12, Margin = new Thickness(0, 0, 0, 10) });
        heading.Children.Add(summary);
        var connection = new WrapPanel { Margin = new Thickness(0, 18, 0, 14), VerticalAlignment = VerticalAlignment.Center };
        connection.Children.Add(Label("인증 서버")); connection.Children.Add(server); connection.Children.Add(Label("관리자 인증키")); connection.Children.Add(password); connection.Children.Add(Button("연결 · 새로고침", Refresh));
        heading.Children.Add(connection);
        var footer = new Border { Background = Brushes.White, Padding = new Thickness(12), Margin = new Thickness(0, 12, 0, 0), Child = status }; DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var tabs = new TabControl { Background = Brushes.White, Padding = new Thickness(16) }; root.Children.Add(tabs);
        var licensePanel = new DockPanel();
        var actions = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        actions.Children.Add(Button("+ 라이선스 발급", () => EditLicense(false))); actions.Children.Add(Button("정책 · 기간 변경", () => EditLicense(true)));
        actions.Children.Add(Button("차단 / 해제", SetLicenseStatus)); actions.Children.Add(Button("오프라인 요청 처리", Offline));
        actions.Children.Add(Button("CSV 내보내기", () => Download("/api/admin/export", "clorium-licenses.csv", "CSV 파일|*.csv")));
        actions.Children.Add(Label("검색")); actions.Children.Add(search); search.TextChanged += (s, e) => Populate();
        DockPanel.SetDock(actions, Dock.Top); licensePanel.Children.Add(actions);
        Column(licenses, "고객사", "Company", 190); Column(licenses, "시리얼번호", "Serial", 285); Column(licenses, "종류", "Kind", 95); Column(licenses, "설치 PC", "Seats", 85); Column(licenses, "허용 버전", "Version", 85); Column(licenses, "만료 / 재인증", "Expiry", 155); Column(licenses, "상태", "Status", 80);
        licenses.SelectionChanged += (s, e) => ShowDetail();
        var split = new Grid(); split.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.1, GridUnitType.Star) }); split.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) }); split.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        split.Children.Add(licenses); var splitter = new GridSplitter { Height = 5, HorizontalAlignment = HorizontalAlignment.Stretch }; Grid.SetRow(splitter, 1); split.Children.Add(splitter);
        var bottom = new DockPanel { Margin = new Thickness(0, 12, 0, 0) }; Grid.SetRow(bottom, 2); split.Children.Add(bottom); DockPanel.SetDock(detail, Dock.Top); bottom.Children.Add(detail);
        var lowerTabs = new TabControl(); bottom.Children.Add(lowerTabs);
        var dp = new DockPanel(); var da = new WrapPanel(); da.Children.Add(Button("선택 PC 강제 회수", () => SetDeviceStatus(true))); da.Children.Add(Button("회수 PC 재인증 허용", () => SetDeviceStatus(false))); DockPanel.SetDock(da, Dock.Top); dp.Children.Add(da); dp.Children.Add(devices);
        Column(devices, "PC 이름", "Hostname", 170); Column(devices, "설치 버전", "AppVersion", 100); Column(devices, "운영체제", "OsVersion", 240); Column(devices, "마지막 확인", "LastSeen", 160); Column(devices, "상태", "Status", 110); Column(devices, "방식", "Mode", 90);
        Column(history, "일시", "At", 170); Column(history, "작업", "Action", 180); Column(history, "상세 · 사유", "Detail", 570);
        lowerTabs.Items.Add(new TabItem { Header = "설치 PC", Content = dp }); lowerTabs.Items.Add(new TabItem { Header = "변경 이력", Content = history });
        licensePanel.Children.Add(split); tabs.Items.Add(new TabItem { Header = "라이선스", Content = licensePanel });
        var cp = new DockPanel(); var ca = new WrapPanel(); ca.Children.Add(Button("+ 고객사 등록", () => EditCustomer(false))); ca.Children.Add(Button("+ 직원 계정", () => EditCustomer(false, true))); ca.Children.Add(Button("고객사 · 직원 수정", () => EditCustomer(true))); DockPanel.SetDock(ca, Dock.Top); cp.Children.Add(ca); cp.Children.Add(customers);
        Column(customers, "고객사", "Company", 260); Column(customers, "담당자", "Contact", 160); Column(customers, "연락처 · 이메일", "Email", 280); Column(customers, "비고", "Notes", 350);
        Column(customers, "직원 계정", "IsInternal", 80); Column(customers, "계정 중지", "Disabled", 80);
        tabs.Items.Add(new TabItem { Header = "고객사 · 직원 계정", Content = cp });
        var settings = new StackPanel { MaxWidth = 850, HorizontalAlignment = HorizontalAlignment.Left };
        settings.Children.Add(new TextBlock { Text = "운영 · 배포", FontSize = 21, Margin = new Thickness(0, 0, 0, 16) });
        settings.Children.Add(new TextBlock { Text = "고객 모듈에는 서버 주소와 공개키만 배포합니다. 관리자 인증키와 서명 개인키는 회사 서버에서만 보관합니다.\n\n백업에는 서명 개인키·관리자 인증 설정·고객 정보가 포함됩니다. 접근이 제한된 암호화 디스크에 보관하세요. 서버를 복원할 때 세 파일을 함께 복원해야 기존 고객의 인증이 유지됩니다.", TextWrapping = TextWrapping.Wrap, LineHeight = 24 });
        settings.Children.Add(Button("고객 모듈 연결 설정 내보내기", ExportConfig)); settings.Children.Add(Button("서버 전체 백업 저장", () => Download("/api/admin/backup", "clorium-license-backup.zip", "서버 백업|*.zip")));
        settings.Children.Add(new TextBlock { Text = "인증 정책\n정식 영구권은 최초 인증 후 같은 PC에서 재접속 없이 사용합니다. 발급 뒤 원격으로 즉시 회수할 수 없습니다.\n\n직원 계정에는 데모/테스트 3개월 재인증을 연결하세요. 다음 인증일은 한국시간 기준 달력 3개월입니다. 기한이 되면 인터넷으로 자동 재인증하고, 연결하지 못하면 새 촬영이 제한됩니다. 계정을 중지하면 다음 재인증을 거부합니다.\n\n설치 대수·기존 PC 반납/회수·버전 제한은 유지됩니다. 직원용 라이선스의 오프라인 파일 발급은 허용하지 않습니다.", Margin = new Thickness(0, 24, 0, 0), TextWrapping = TextWrapping.Wrap, LineHeight = 24 });
        tabs.Items.Add(new TabItem { Header = "운영 · 백업", Content = settings });
        Populate(); Closed += (s, e) => http.Dispose();
    }
    private static DataGrid Table() => new() { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, SelectionMode = DataGridSelectionMode.Single, HeadersVisibility = DataGridHeadersVisibility.Column, GridLinesVisibility = DataGridGridLinesVisibility.Horizontal, HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(228, 233, 240)), RowHeight = 40, ColumnHeaderHeight = 38, BorderThickness = new Thickness(0), AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(248, 250, 252)), HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
    private static void Column(DataGrid grid, string title, string binding, double width) => grid.Columns.Add(new DataGridTextColumn { Header = title, Binding = new Binding(binding), Width = width });
    private static TextBlock Label(string text) => new() { Text = text, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 9, 6) };
    private Button Button(string text, Func<Task> action) { var b = new Button { Content = text }; b.Click += async (s, e) => { if (busy) return; busy = true; b.IsEnabled = false; try { await action(); } catch (Exception ex) { status.Text = ex.Message; status.Foreground = Brushes.Firebrick; } finally { busy = false; b.IsEnabled = true; } }; return b; }
    private async Task<HttpResponseMessage> Send(string path, HttpMethod method = null, object value = null, string raw = null)
    {
        if (!Uri.TryCreate(server.Text.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback))) throw new InvalidOperationException("서버 주소는 HTTPS를 사용해 주세요. 로컬 시험 주소만 HTTP를 허용합니다.");
        var req = new HttpRequestMessage(method ?? HttpMethod.Get, server.Text.TrimEnd('/') + path); req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", password.Password.Trim());
        if (value != null || raw != null) req.Content = new StringContent(raw ?? JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
        var response = await http.SendAsync(req); req.Dispose();
        if (!response.IsSuccessStatusCode) { string message = await response.Content.ReadAsStringAsync(); response.Dispose(); if (message.Length > 600) message = message.Substring(0, 600); throw new InvalidOperationException("서버 요청 실패: " + message); }
        return response;
    }
    private async Task Refresh() { using var response = await Send("/api/admin/state"); data = JsonSerializer.Deserialize<Database>(await response.Content.ReadAsStringAsync(), Json); Populate(); status.Text = "연결됨 · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " · 변경 사항을 불러왔습니다."; status.Foreground = Brushes.DarkGreen; }
    private void Populate()
    {
        string q = search.Text.Trim();
        summary.Text = "라이선스 관리   " + data.Licenses.Count + "개  ·  활성 PC " + data.Activations.Count(a => a.Active) + "대  ·  고객사 " + data.Customers.Count + "곳";
        var rows = data.Licenses.Select(l => new LicenseRow(l, data)).Where(r => (r.Company + r.Serial + r.Kind).Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        licenses.ItemsSource = rows; customers.ItemsSource = data.Customers;
        licenses.SelectedItem = rows.FirstOrDefault(x => x.Id == selectedId) ?? rows.FirstOrDefault();
    }
    private LicenseRecord Selected() { if (licenses.SelectedItem is not LicenseRow row) throw new InvalidOperationException("라이선스를 먼저 선택해 주세요."); return data.Licenses.Single(x => x.Id == row.Id); }
    private void ShowDetail()
    {
        if (licenses.SelectedItem is not LicenseRow row) { devices.ItemsSource = null; history.ItemsSource = null; return; }
        selectedId = row.Id;
        var l = data.Licenses.Single(x => x.Id == row.Id);
        detail.Text = row.Company + "   /   " + l.Serial + "\n" + (l.Kind == "Perpetual" ? "최초 1회 인증 · 동일 PC 영구 사용 · 주기 접속 없음" : l.RenewalMonths == 3 ? "직원용 · 3개월 재인증 · 다음 " + (l.NextReauthAt == 0 ? "최초 등록 후 결정" : Date(l.NextReauthAt)) : "고정 만료 · 오프라인 유예 " + l.GraceDays + "일") + (string.IsNullOrWhiteSpace(l.Notes) ? "" : " · " + l.Notes);
        devices.ItemsSource = data.Activations.Where(x => x.LicenseId == row.Id).OrderByDescending(x => x.Active).Select(x => new DeviceRow(x)).ToList();
        history.ItemsSource = data.Audit.Where(x => x.Target == row.Id).OrderByDescending(x => x.At).Select(x => new { At = Date(x.At), x.Action, x.Detail }).ToList();
    }
    private async Task EditCustomer(bool edit, bool staff = false)
    {
        Customer c = edit ? customers.SelectedItem as Customer : new Customer { IsInternal = staff }; if (c == null) throw new InvalidOperationException("고객사 / 직원을 선택해 주세요.");
        var form = new FormWindow(edit ? "고객사 · 직원 수정" : staff ? "직원 계정 등록" : "고객사 등록"); var company = form.Field("고객사명 / 직원 이름", c.Company); var contact = form.Field("담당자 / 부서", c.Contact); var email = form.Field("연락처 / 직원 ID · 이메일", c.Email); var notes = form.Field("비고", c.Notes);
        var internalAccount = form.Check("내부 직원 계정", c.IsInternal); var disabled = form.Check("직원 계정 중지 (다음 재인증 차단)", c.Disabled);
        if (form.Open(this)) { using var r = await Send("/api/admin/customers", HttpMethod.Post, new Customer { Id = c.Id, Company = company.Text, Contact = contact.Text, Email = email.Text, Notes = notes.Text, IsInternal = internalAccount.IsChecked == true, Disabled = disabled.IsChecked == true }); await Refresh(); }
    }
    private async Task EditLicense(bool edit)
    {
        var l = edit ? Selected() : new LicenseRecord(); if (data.Customers.Count == 0) throw new InvalidOperationException("고객사 탭에서 고객사를 먼저 등록해 주세요.");
        var form = new FormWindow(edit ? "정책 · 기간 변경" : "라이선스 발급");
        var customer = form.Choose("고객사 / 직원 계정", data.Customers.Select(x => (x.IsInternal ? "[직원] " : "") + x.Company).ToArray(), Math.Max(0, data.Customers.FindIndex(x => x.Id == l.CustomerId))); customer.IsEnabled = !edit;
        var kind = form.Choose("라이선스 종류", new[] { "영구", "데모", "테스트" }, l.Kind == "Demo" ? 1 : l.Kind == "Test" ? 2 : 0);
        var seats = form.Field("설치 허용 PC 수", l.Seats.ToString()); var version = form.Field("허용 주요 버전 (예: 4 → 4.x)", l.MaxMajor.ToString());
        var recurring = form.Check("직원용 · 달력 3개월마다 온라인 재인증", l.RenewalMonths == 3);
        var grace = form.Field("고정 만료 데모의 오프라인 유예 (일)", l.GraceDays.ToString()); var expiry = form.Field("고정 만료 데모 · 테스트 만료 (yyyy-MM-dd HH:mm)", l.ExpiresAt == 0 ? DateTime.Now.AddDays(30).ToString("yyyy-MM-dd HH:mm") : DateTimeOffset.FromUnixTimeSeconds(l.ExpiresAt).LocalDateTime.ToString("yyyy-MM-dd HH:mm"));
        void Mode() { bool permanent = kind.SelectedIndex == 0; bool internalCustomer = data.Customers[customer.SelectedIndex].IsInternal; recurring.IsEnabled = false; recurring.IsChecked = !permanent && internalCustomer; expiry.IsEnabled = grace.IsEnabled = !permanent && !internalCustomer; }
        kind.SelectionChanged += (s,e) => Mode(); customer.SelectionChanged += (s,e) => { if (!edit && data.Customers[customer.SelectedIndex].IsInternal) { kind.SelectedIndex = 2; recurring.IsChecked = true; } Mode(); }; recurring.Checked += (s,e) => Mode(); recurring.Unchecked += (s,e) => Mode();
        if (!edit && data.Customers[customer.SelectedIndex].IsInternal) { kind.SelectedIndex = 2; recurring.IsChecked = true; } Mode();
        var notes = form.Field("발급 · 변경 사유와 비고", l.Notes);
        form.Help("영구권: 최초 1회 인증 후 무접속 사용. 직원용: 3개월 기한마다 온라인 재인증. 직원 ID는 고객사 · 직원 계정 탭에서 등록합니다.");
        if (form.Open(this)) {
            if (!int.TryParse(seats.Text, out var s) || !int.TryParse(version.Text, out var v) || !int.TryParse(grace.Text, out var g)) throw new InvalidOperationException("설치 수·버전·유예기간은 숫자로 입력해 주세요.");
            long expires = 0; if (kind.SelectedIndex != 0 && recurring.IsChecked != true) { if (!DateTime.TryParseExact(expiry.Text, "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt)) throw new InvalidOperationException("만료일 형식은 yyyy-MM-dd HH:mm 입니다."); expires = new DateTimeOffset(dt).ToUnixTimeSeconds(); }
            var changes = new LicenseRecord { CustomerId = data.Customers[customer.SelectedIndex].Id, Kind = kind.SelectedIndex == 0 ? "Perpetual" : kind.SelectedIndex == 1 ? "Demo" : "Test", Seats = s, MaxMajor = v, GraceDays = g, ExpiresAt = expires, RenewalMonths = recurring.IsChecked == true ? 3 : 0, Notes = notes.Text };
            using var r = await Send("/api/admin/licenses" + (edit ? "/" + l.Id : ""), edit ? HttpMethod.Put : HttpMethod.Post, changes); await Refresh();
        }
    }
    private async Task SetLicenseStatus() { var l = Selected(); var form = new FormWindow(l.Blocked ? "라이선스 차단 해제" : "라이선스 차단"); var reason = form.Field("처리 사유", ""); form.Help("온라인 확인 시 반영됩니다. 오프라인 PC에는 발급한 확인 기한까지 권한이 남습니다."); if (form.Open(this)) { using var r = await Send("/api/admin/licenses/" + l.Id + "/status", HttpMethod.Post, new { Blocked = !l.Blocked, Reason = reason.Text }); await Refresh(); } }
    private async Task SetDeviceStatus(bool blocked) { if (devices.SelectedItem is not DeviceRow a) throw new InvalidOperationException("설치 PC를 선택해 주세요."); var form = new FormWindow(blocked ? "PC 강제 회수" : "PC 재인증 허용"); var reason = form.Field("처리 사유", ""); form.Help("고장 PC 회수 후 새 PC에서 같은 시리얼로 인증합니다. 옛 PC의 기존 오프라인 권한은 즉시 회수되지 않습니다."); if (form.Open(this)) { using var r = await Send("/api/admin/activations/" + a.Id + "/status", HttpMethod.Post, new { Blocked = blocked, Reason = reason.Text }); await Refresh(); } }
    private async Task Offline()
    {
        var open = new OpenFileDialog { Filter = "클로리움 인증 요청|*.clreq" }; if (open.ShowDialog(this) != true) return;
        string raw = File.ReadAllText(open.FileName); var request = Wire.Read<ActivationRequest>(raw);
        var save = new SaveFileDialog { Filter = "클로리움 인증 응답|*.clresp", FileName = Path.GetFileNameWithoutExtension(open.FileName) + ".clresp" }; if (save.ShowDialog(this) != true) return;
        if (MessageBox.Show(this, request.Hostname + " · " + request.AppVersion + "\n" + request.Serial + "\n\n이 PC에 오프라인 라이선스를 발급합니까? 설치 대수 1개를 사용합니다.", "오프라인 발급", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        using var response = await Send("/api/admin/offline", HttpMethod.Post, raw: raw); Wire.AtomicWrite(save.FileName, await response.Content.ReadAsStringAsync()); await Refresh(); status.Text = "응답 파일을 저장했습니다. 고객에게 전달해 주세요.";
    }
    private async Task ExportConfig() { using var response = await Send("/api/admin/public-key"); string key = await response.Content.ReadAsStringAsync(); var save = new SaveFileDialog { Filter = "클로리움 연결 설정|*.json", FileName = "clorium-license.json" }; if (save.ShowDialog(this) == true) { Wire.AtomicWrite(save.FileName, Wire.Json(new ClientConnection { ServerUrl = server.Text.TrimEnd('/'), PublicKey = key })); status.Text = "공개 연결 설정을 저장했습니다. 신규 고객 배포 빌드에 포함하세요."; } }
    private async Task Download(string endpoint, string name, string filter) { var save = new SaveFileDialog { FileName = name, Filter = filter }; if (save.ShowDialog(this) != true) return; using var r = await Send(endpoint); PrivateFiles.Save(save.FileName, await r.Content.ReadAsByteArrayAsync()); status.Text = "접근을 제한하여 저장했습니다: " + save.FileName; }
    public static string Date(long t) => t == 0 ? "영구" : DateTimeOffset.FromUnixTimeSeconds(t).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public void LoadExample()
    {
        var c = new Customer { Id = "example", Company = "샘플 고객사 · 디자인 확인용", Contact = "김담당", Email = "contact@example.com" }; data.Customers.Add(c);
        foreach (string kind in new[] { "Perpetual", "Demo", "Test" }) { var l = new LicenseRecord { CustomerId = c.Id, Kind = kind, Seats = 3, ExpiresAt = kind == "Perpetual" ? 0 : Wire.Now + 86400 * 30 }; data.Licenses.Add(l); data.Activations.Add(new Activation { LicenseId = l.Id, Hostname = "STUDIO-PC-01", AppVersion = "4.7.1.5", OsVersion = "Windows 11", Active = true }); data.Audit.Add(new Audit { Target = l.Id, Action = "라이선스 발급", Detail = "화면 확인용 예시 데이터" }); } Populate();
        status.Text = "화면 확인용 예시 · 실제 고객 데이터와 서버에 연결하지 않았습니다.";
    }
    public async Task VerifyConnectedUi(string png, string result)
    {
        await Refresh();
        if (data.Licenses.Count < 4 || licenses.Items.Count != data.Licenses.Count || customers.Items.Count != data.Customers.Count) throw new InvalidOperationException("서버 조회가 화면에 연결되지 않았습니다.");
        var first = (LicenseRow)licenses.Items[0]; search.Text = first.Serial;
        if (licenses.Items.Count != 1 || devices.ItemsSource == null || history.ItemsSource == null) throw new InvalidOperationException("검색/선택/상세 연결 오류");
        search.Text = "찾을수없는고객사"; if (licenses.Items.Count != 0) throw new InvalidOperationException("검색 빈 상태 오류"); search.Text = "";
        if (data.Licenses.Any(l => l.RenewalMonths == 3) && !licenses.Items.Cast<LicenseRow>().Any(r => r.Kind.StartsWith("직원") && r.Expiry != "영구")) throw new InvalidOperationException("직원 재인증 표시 오류");
        using (var backup = await Send("/api/admin/backup")) PrivateFiles.Save(result + ".backup.zip", await backup.Content.ReadAsByteArrayAsync());
        UpdateLayout();
        var image = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32); image.Render(this);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using (var f = File.Create(png)) encoder.Save(f);
        File.WriteAllText(result, "PASS real server connection, customer/license display, serial search, empty search, selected device/history binding, private backup save; rendered actual WPF controls.");
    }
}
public class LicenseRow
{
    public string Id { get; } public string Company { get; } public string Serial { get; } public string Kind { get; } public string Seats { get; } public string Version { get; } public string Expiry { get; } public string Status { get; }
    public LicenseRow(LicenseRecord l, Database d) { Id = l.Id; Company = d.Customers.FirstOrDefault(x => x.Id == l.CustomerId)?.Company; Serial = l.Serial; Kind = (l.RenewalMonths == 3 ? "직원 " : "") + (l.Kind == "Perpetual" ? "영구" : l.Kind == "Demo" ? "데모" : "테스트"); Seats = d.Activations.Count(x => x.LicenseId == l.Id && x.Active) + " / " + l.Seats; Version = l.MaxMajor + ".x"; Expiry = l.RenewalMonths == 3 ? (l.NextReauthAt == 0 ? "최초 등록 후 결정" : ManagerWindow.Date(l.NextReauthAt)) : ManagerWindow.Date(l.ExpiresAt); Status = l.Blocked ? "차단" : d.Customers.Any(c => c.Id == l.CustomerId && c.Disabled && l.RenewalMonths == 3) ? "계정 중지" : l.RenewalMonths == 3 && l.NextReauthAt > 0 && Wire.Now >= l.NextReauthAt ? "재인증 필요" : l.ExpiresAt > 0 && Wire.Now >= l.ExpiresAt ? "만료" : "정상"; }
}
public class DeviceRow
{
    public string Id { get; } public string Hostname { get; } public string AppVersion { get; } public string OsVersion { get; } public string LastSeen { get; } public string Status { get; } public string Mode { get; }
    public DeviceRow(Activation a) { Id = a.Id; Hostname = a.Hostname; AppVersion = a.AppVersion; OsVersion = a.OsVersion; LastSeen = ManagerWindow.Date(a.LastSeen); Status = a.Blocked ? "강제 회수" : a.Active ? "활성" : "반납"; Mode = a.Offline ? "오프라인" : "온라인"; }
}
[System.Runtime.Serialization.DataContract] public class ClientConnection
{
    [System.Runtime.Serialization.DataMember] public string ServerUrl { get; set; }
    [System.Runtime.Serialization.DataMember] public string PublicKey { get; set; }
}
public class FormWindow : Window
{
    private readonly StackPanel panel = new() { Margin = new Thickness(24) };
    public FormWindow(string title) { Title = title; Width = 510; SizeToContent = SizeToContent.Height; MaxHeight = 850; WindowStartupLocation = WindowStartupLocation.CenterOwner; FontFamily = new FontFamily("Malgun Gothic"); FontSize = 13; Background = Brushes.White; Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; }
    public TextBox Field(string label, string text) { panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 12, 0, 5) }); var box = new TextBox { Text = text ?? "", Padding = new Thickness(8, 6, 8, 6) }; panel.Children.Add(box); return box; }
    public ComboBox Choose(string label, string[] items, int selected) { panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 12, 0, 5) }); var box = new ComboBox { ItemsSource = items, SelectedIndex = selected, Padding = new Thickness(8) }; panel.Children.Add(box); return box; }
    public CheckBox Check(string text, bool value) { var box = new CheckBox { Content = text, IsChecked = value, Margin = new Thickness(0, 16, 0, 8) }; panel.Children.Add(box); return box; }
    public void Help(string text) => panel.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.SlateGray, Margin = new Thickness(0, 12, 0, 8) });
    public bool Open(Window owner) { Owner = owner; var button = new Button { Content = "저장 · 실행", Padding = new Thickness(18, 9, 18, 9), Margin = new Thickness(0, 20, 0, 0), IsDefault = true }; button.Click += (s, e) => DialogResult = true; panel.Children.Add(button); return ShowDialog() == true; }
}
