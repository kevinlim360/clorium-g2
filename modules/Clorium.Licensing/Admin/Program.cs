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
    private readonly DataGrid requests = Table();
    private readonly DataGrid products = Table();
    private readonly ComboBox productFilter = new() { Width = 190, Padding = new Thickness(8, 5, 8, 5) };
    private readonly TabItem requestTab;
    private const string AllProducts = "전체 제품";
    private bool populating;
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
        connection.Children.Add(Label("제품")); connection.Children.Add(productFilter); productFilter.SelectionChanged += (s, e) => { if (!populating) Populate(); };
        heading.Children.Add(connection);
        var footer = new Border { Background = Brushes.White, Padding = new Thickness(12), Margin = new Thickness(0, 12, 0, 0), Child = status }; DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var tabs = new TabControl { Background = Brushes.White, Padding = new Thickness(16) }; root.Children.Add(tabs);
        var licensePanel = new DockPanel();
        var actions = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        actions.Children.Add(Button("+ 라이선스 발급", () => EditLicense(false))); actions.Children.Add(Button("정책 변경 · 정식 전환", () => EditLicense(true))); actions.Children.Add(Button("임시 기간 재승인", ChangeTerm));
        actions.Children.Add(Button("차단 / 해제", SetLicenseStatus)); actions.Children.Add(Button("오프라인 요청 처리", Offline));
        actions.Children.Add(Button("CSV 내보내기", () => Download("/api/admin/export", "clorium-licenses.csv", "CSV 파일|*.csv")));
        actions.Children.Add(Label("검색")); actions.Children.Add(search); search.TextChanged += (s, e) => Populate();
        DockPanel.SetDock(actions, Dock.Top); licensePanel.Children.Add(actions);
        Column(licenses, "제품", "Product", 150); Column(licenses, "고객사", "Company", 170); Column(licenses, "시리얼번호", "Serial", 285); Column(licenses, "종류", "Kind", 150); Column(licenses, "설치 PC", "Seats", 85); Column(licenses, "허용 버전", "Version", 85); Column(licenses, "만료", "Expiry", 155); Column(licenses, "상태", "Status", 95);
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
        var rp = new DockPanel(); var ra = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        ra.Children.Add(Button("선택 요청 승인", () => Decide(true))); ra.Children.Add(Button("선택 요청 거절", () => Decide(false)));
        ra.Children.Add(new TextBlock { Text = "임시 라이선스는 목적(데모/테스트)과 기간(1개월·3개월·1년)을 정해 최종 승인합니다. PC 변경은 회수할 기존 PC를 선택합니다.", Foreground = Brushes.SlateGray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 6) });
        DockPanel.SetDock(ra, Dock.Top); rp.Children.Add(ra); rp.Children.Add(requests);
        Column(requests, "요청번호", "Code", 100); Column(requests, "제품", "Product", 140); Column(requests, "유형", "Type", 100); Column(requests, "고객사", "Company", 170); Column(requests, "시리얼번호", "Serial", 260); Column(requests, "PC 이름", "Hostname", 160); Column(requests, "버전", "AppVersion", 90); Column(requests, "요청 일시", "RequestedAt", 140); Column(requests, "상태", "Status", 90); Column(requests, "처리 내용", "Decision", 300);
        tabs.Items.Add(requestTab = new TabItem { Header = "승인 대기", Content = rp });
        var pp = new DockPanel(); var pa = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        pa.Children.Add(Button("+ 제품 등록", () => EditProduct(false))); pa.Children.Add(Button("제품 수정 · 인증 중지", () => EditProduct(true)));
        pa.Children.Add(new TextBlock { Text = "제품마다 시리얼·설치 PC·승인이 따로 관리됩니다. 같은 PC에 클로리움과 오토사이징을 각각 인증할 수 있습니다. 클로리움에 통합된 오토사이징은 클로리움 라이선스를 사용합니다.", Foreground = Brushes.SlateGray, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 6), TextWrapping = TextWrapping.Wrap, MaxWidth = 760 });
        DockPanel.SetDock(pa, Dock.Top); pp.Children.Add(pa); pp.Children.Add(products);
        Column(products, "제품 이름", "Name", 220); Column(products, "제품 코드", "Code", 220); Column(products, "라이선스", "Licenses", 90); Column(products, "활성 PC", "Devices", 90); Column(products, "승인 대기", "Pending", 90); Column(products, "상태", "Status", 100); Column(products, "비고", "Notes", 360);
        tabs.Items.Add(new TabItem { Header = "제품", Content = pp });
        var cp = new DockPanel(); var ca = new WrapPanel(); ca.Children.Add(Button("+ 고객사 등록", () => EditCustomer(false))); ca.Children.Add(Button("+ 직원 계정", () => EditCustomer(false, true))); ca.Children.Add(Button("고객사 · 직원 수정", () => EditCustomer(true))); DockPanel.SetDock(ca, Dock.Top); cp.Children.Add(ca); cp.Children.Add(customers);
        Column(customers, "고객사", "Company", 260); Column(customers, "담당자", "Contact", 160); Column(customers, "연락처 · 이메일", "Email", 280); Column(customers, "비고", "Notes", 350);
        Column(customers, "직원 계정", "IsInternal", 80); Column(customers, "계정 중지", "Disabled", 80);
        tabs.Items.Add(new TabItem { Header = "고객사 · 직원 계정", Content = cp });
        var settings = new StackPanel { MaxWidth = 850, HorizontalAlignment = HorizontalAlignment.Left };
        settings.Children.Add(new TextBlock { Text = "운영 · 배포", FontSize = 21, Margin = new Thickness(0, 0, 0, 16) });
        settings.Children.Add(new TextBlock { Text = "고객 모듈에는 서버 주소와 공개키만 배포합니다. 관리자 인증키와 서명 개인키는 회사 서버에서만 보관합니다.\n\n백업에는 서명 개인키·관리자 인증 설정·고객 정보가 포함됩니다. 접근이 제한된 암호화 디스크에 보관하세요. 서버를 복원할 때 세 파일을 함께 복원해야 기존 고객의 인증이 유지됩니다.", TextWrapping = TextWrapping.Wrap, LineHeight = 24 });
        settings.Children.Add(Button("고객 모듈 연결 설정 내보내기", ExportConfig)); settings.Children.Add(Button("서버 전체 백업 저장", () => Download("/api/admin/backup", "clorium-license-backup.zip", "서버 백업|*.zip")));
        settings.Children.Add(new TextBlock { Text = "인증 정책\n정식 영구권은 최초 인증 후 같은 PC에서 재접속 없이 사용합니다. 발급 뒤 원격으로 즉시 회수할 수 없습니다.\n\n프로그램의 Major.Minor 버전이 바뀌면(예: 4.7 → 4.8) 고객 PC가 한 번 온라인 확인합니다. 허용 주요 버전 안이면 자동 승인됩니다. 4.7.1.x 같은 패치는 확인하지 않습니다.\n\n다른 PC로 옮긴 설치는 '라이선스가 없습니다'로 표시됩니다. 최초 설치 대수를 넘는 새 PC는 승인 대기 탭에 올라오며, 회사가 기존 PC를 회수하고 승인해야 사용할 수 있습니다.\n\n임시 라이선스는 시리얼만 먼저 발급합니다. 고객 PC에서 입력하면 승인 대기에 올라오고, 목적(데모/테스트)과 기간(1개월·3개월·1년)을 정해 승인한 시점부터 한국시간 달력 기준으로 계산합니다. 기간 중에는 재접속하지 않습니다.", Margin = new Thickness(0, 24, 0, 0), TextWrapping = TextWrapping.Wrap, LineHeight = 24 });
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
        string chosen = productFilter.SelectedItem as string;
        var names = new[] { AllProducts }.Concat(data.Products.Select(p => p.Name)).ToList();
        if (!(productFilter.ItemsSource is List<string> old) || !old.SequenceEqual(names)) { populating = true; productFilter.ItemsSource = names; productFilter.SelectedItem = names.Contains(chosen) ? chosen : AllProducts; populating = false; }
        string code = data.Products.FirstOrDefault(p => p.Name == productFilter.SelectedItem as string)?.Code;
        bool InScope(string licenseId) => code == null || data.Licenses.Any(l => l.Id == licenseId && l.Product == code);
        var scoped = data.Licenses.Where(l => InScope(l.Id)).ToList();
        int pending = data.Requests.Count(x => x.Status == "Pending" && InScope(x.LicenseId));
        summary.Text = (code == null ? "라이선스 관리" : productFilter.SelectedItem) + "   " + scoped.Count + "개  ·  활성 PC " + data.Activations.Count(a => a.Active && InScope(a.LicenseId)) + "대  ·  고객사 " + data.Customers.Count + "곳  ·  승인 대기 " + pending + "건";
        requestTab.Header = pending > 0 ? "승인 대기 (" + pending + ")" : "승인 대기";
        var rows = scoped.Select(l => new LicenseRow(l, data)).Where(r => (r.Product + r.Company + r.Serial + r.Kind).Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        licenses.ItemsSource = rows; customers.ItemsSource = data.Customers;
        requests.ItemsSource = data.Requests.Where(x => InScope(x.LicenseId)).OrderByDescending(x => x.Status == "Pending").ThenByDescending(x => x.RequestedAt).Select(x => new RequestRow(x, data)).ToList();
        products.ItemsSource = data.Products.Select(p => new ProductRow(p, data)).ToList();
        licenses.SelectedItem = rows.FirstOrDefault(x => x.Id == selectedId) ?? rows.FirstOrDefault();
    }
    private LicenseRecord Selected() { if (licenses.SelectedItem is not LicenseRow row) throw new InvalidOperationException("라이선스를 먼저 선택해 주세요."); return data.Licenses.Single(x => x.Id == row.Id); }
    private void ShowDetail()
    {
        if (licenses.SelectedItem is not LicenseRow row) { devices.ItemsSource = null; history.ItemsSource = null; return; }
        selectedId = row.Id;
        var l = data.Licenses.Single(x => x.Id == row.Id);
        detail.Text = row.Product + "   /   " + row.Company + "   /   " + l.Serial + "\n" + (!l.IsTemporary ? "최초 1회 인증 · 동일 PC 영구 사용 · 주기 접속 없음" : !l.IsApproved ? "임시 · 고객 PC 요청 후 승인 대기 탭에서 목적·기간 승인" : "임시 " + row.Kind + " · " + Date(l.ApprovedAt) + " 승인 · " + Date(l.ExpiresAt) + " 만료 · 기간 중 재접속 없음") + " · 버전 변경(Major.Minor) 시 1회 확인" + (string.IsNullOrWhiteSpace(l.Notes) ? "" : " · " + l.Notes);
        devices.ItemsSource = data.Activations.Where(x => x.LicenseId == row.Id).OrderByDescending(x => x.Active).Select(x => new DeviceRow(x)).ToList();
        history.ItemsSource = data.Audit.Where(x => x.Target == row.Id).OrderByDescending(x => x.At).Select(x => new { At = Date(x.At), x.Action, x.Detail }).ToList();
    }
    private async Task EditCustomer(bool edit, bool staff = false)
    {
        Customer c = edit ? customers.SelectedItem as Customer : new Customer { IsInternal = staff }; if (c == null) throw new InvalidOperationException("고객사 / 직원을 선택해 주세요.");
        var form = new FormWindow(edit ? "고객사 · 직원 수정" : staff ? "직원 계정 등록" : "고객사 등록"); var company = form.Field("고객사명 / 직원 이름", c.Company); var contact = form.Field("담당자 / 부서", c.Contact); var email = form.Field("연락처 / 직원 ID · 이메일", c.Email); var notes = form.Field("비고", c.Notes);
        var internalAccount = form.Check("내부 직원 계정", c.IsInternal); var disabled = form.Check("계정 중지 (이후 온라인 인증·승인 거부)", c.Disabled);
        if (form.Open(this)) { using var r = await Send("/api/admin/customers", HttpMethod.Post, new Customer { Id = c.Id, Company = company.Text, Contact = contact.Text, Email = email.Text, Notes = notes.Text, IsInternal = internalAccount.IsChecked == true, Disabled = disabled.IsChecked == true }); await Refresh(); }
    }
    private async Task EditLicense(bool edit)
    {
        var l = edit ? Selected() : new LicenseRecord(); if (data.Customers.Count == 0) throw new InvalidOperationException("고객사 탭에서 고객사를 먼저 등록해 주세요.");
        var form = new FormWindow(edit ? "정책 변경 · 정식 전환" : "라이선스 발급");
        var sellable = edit ? data.Products.Where(p => p.Code == l.Product).ToList() : data.Products.Where(p => !p.Disabled).ToList();
        if (sellable.Count == 0) throw new InvalidOperationException("제품 탭에서 사용 중인 제품을 먼저 등록해 주세요.");
        string filtered = data.Products.FirstOrDefault(p => p.Name == productFilter.SelectedItem as string)?.Code;
        var product = form.Choose("제품", sellable.Select(p => p.Name).ToArray(), Math.Max(0, sellable.FindIndex(p => p.Code == (edit ? l.Product : filtered ?? Products.Clorium)))); product.IsEnabled = !edit;
        var customer =form.Choose("고객사 / 직원 계정", data.Customers.Select(x => (x.IsInternal ? "[직원] " : "") + x.Company).ToArray(), Math.Max(0, data.Customers.FindIndex(x => x.Id == l.CustomerId))); customer.IsEnabled = !edit;
        var kind = form.Choose("라이선스 종류", new[] { "정식 영구", "임시 (승인 시 목적·기간 결정)" }, l.IsTemporary ? 1 : 0); kind.IsEnabled = !edit || l.IsTemporary;
        var seats = form.Field("설치 허용 PC 수", l.Seats.ToString()); var version = form.Field("허용 주요 버전 (예: 4 → 4.x)", l.MaxMajor.ToString());
        var notes = form.Field("발급 · 변경 사유와 비고", l.Notes);
        form.Help(edit && l.IsTemporary ? "임시를 정식 영구로 바꾸면 고객 PC가 '온라인 재인증'할 때 영구 인증으로 바뀝니다. 기간 연장은 '임시 기간 재승인'을 사용하세요." : "정식 영구: 최초 1회 인증 후 같은 PC에서 무접속 영구 사용.\n임시: 시리얼만 먼저 발급합니다. 고객 PC에서 입력하면 승인 대기 탭에 올라오고, 데모/테스트 목적과 1개월·3개월·1년 기간을 정해 승인합니다.");
        if (form.Open(this)) {
            if (!int.TryParse(seats.Text, out var s) || !int.TryParse(version.Text, out var v)) throw new InvalidOperationException("설치 수·버전은 숫자로 입력해 주세요.");
            var changes = new LicenseRecord { Product = sellable[product.SelectedIndex].Code, CustomerId = data.Customers[customer.SelectedIndex].Id, Kind = kind.SelectedIndex == 0 ? "Perpetual" : "Temporary", Seats = s, MaxMajor = v, Notes = notes.Text };
            using var r = await Send("/api/admin/licenses" + (edit ? "/" + l.Id : ""), edit ? HttpMethod.Put : HttpMethod.Post, changes); await Refresh();
        }
    }
    private static readonly string[] Purposes = { "Demo", "Test" };
    private (ComboBox purpose, ComboBox term) TermFields(FormWindow form, string purpose, int months)
    {
        var p = form.Choose("라이선스 목적", new[] { "데모", "테스트" }, purpose == "Test" ? 1 : 0);
        var t = form.Choose("사용 기간 (승인 시점부터 한국시간 달력 기준)", new[] { "1개월", "3개월", "1년" }, Math.Max(0, Array.IndexOf(Wire.TemporaryTerms, months)));
        return (p, t);
    }
    private async Task ChangeTerm()
    {
        var l = Selected(); if (!l.IsTemporary) throw new InvalidOperationException("임시 라이선스를 선택해 주세요.");
        if (!l.IsApproved) throw new InvalidOperationException("아직 승인 전입니다. 고객 PC 요청이 승인 대기 탭에 올라오면 그곳에서 승인해 주세요.");
        var form = new FormWindow("임시 기간 재승인"); var (p, t) = TermFields(form, l.Purpose, l.TermMonths); var reason = form.Field("재승인 사유", "");
        form.Help("지금 시점부터 새 기간을 계산합니다. 고객 PC는 '온라인 재인증'을 누르거나 만료 후 접속할 때 새 기간을 받습니다.");
        if (!form.Open(this)) return;
        int months = Wire.TemporaryTerms[t.SelectedIndex]; string end = ManagerWindow.Date(Wire.AddKstMonths(Wire.Now, months));
        if (MessageBox.Show(this, (p.SelectedIndex == 0 ? "데모" : "테스트") + " · " + t.SelectedItem + "\n만료 예정: " + end + "\n\n이 기간으로 최종 승인합니까?", "최종 승인", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        using var r = await Send("/api/admin/licenses/" + l.Id + "/term", HttpMethod.Post, new TermChange { Purpose = Purposes[p.SelectedIndex], TermMonths = months, Reason = reason.Text }); await Refresh();
        status.Text = "임시 기간을 재승인했습니다. 만료 " + end;
    }
    private async Task Decide(bool approve)
    {
        if (requests.SelectedItem is not RequestRow row) throw new InvalidOperationException("요청을 선택해 주세요.");
        var q = data.Requests.Single(x => x.Id == row.Id); if (q.Status != "Pending") throw new InvalidOperationException("이미 처리한 요청입니다.");
        var l = data.Licenses.Single(x => x.Id == q.LicenseId);
        var form = new FormWindow(approve ? (l.IsApproved ? "PC 변경 승인" : "임시 라이선스 승인") : "요청 거절");
        form.Help(row.Product + " · " + row.Company + " · " + l.Serial + "\n요청 PC: " + q.Hostname + " · " + q.AppVersion + " · 요청번호 " + q.Code);
        (ComboBox purpose, ComboBox term) termFields = default; ComboBox replace = null;
        var active = data.Activations.Where(x => x.LicenseId == l.Id && x.Active).ToList();
        if (approve && !l.IsApproved) termFields = TermFields(form, null, 1);
        if (approve && active.Count > 0) {
            var choices = new[] { active.Count < l.Seats ? "회수 없이 추가 (남은 설치 수 사용)" : "선택하세요" }.Concat(active.Select(x => x.Hostname + " · " + x.AppVersion + " · 마지막 " + Date(x.LastSeen))).ToArray();
            replace = form.Choose("회수할 기존 PC", choices, active.Count == 1 && active.Count >= l.Seats ? 1 : 0);
            form.Help("회수한 PC는 온라인으로 다시 인증할 수 없습니다. 인터넷에 연결하지 않는 영구권 PC의 기존 인증은 즉시 회수되지 않습니다.");
        }
        var reason = form.Field(approve ? "승인 사유" : "거절 사유", "");
        if (!form.Open(this)) return;
        var decision = new ApprovalDecision { Approve = approve, Reason = reason.Text };
        string confirm = row.Product + " · " + (approve ? "승인" : "거절") + " · " + q.Hostname;
        if (approve && !l.IsApproved) { decision.Purpose = Purposes[termFields.purpose.SelectedIndex]; decision.TermMonths = Wire.TemporaryTerms[termFields.term.SelectedIndex]; confirm += "\n" + (decision.Purpose == "Demo" ? "데모" : "테스트") + " · " + termFields.term.SelectedItem + " · 만료 예정 " + ManagerWindow.Date(Wire.AddKstMonths(Wire.Now, decision.TermMonths)); }
        if (replace != null && replace.SelectedIndex > 0) { decision.ReplaceActivationId = active[replace.SelectedIndex - 1].Id; confirm += "\n기존 PC 회수: " + active[replace.SelectedIndex - 1].Hostname; }
        if (MessageBox.Show(this, confirm + "\n\n최종 " + (approve ? "승인" : "거절") + "합니까?", approve ? "최종 승인" : "거절", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        using var r = await Send("/api/admin/requests/" + q.Id, HttpMethod.Post, decision); await Refresh();
        status.Text = q.Code + (approve ? " 승인 완료 · 고객 PC에서 '승인 확인'을 누르면 사용할 수 있습니다." + (q.Offline ? " 오프라인 PC는 같은 요청 파일을 다시 처리해 응답 파일을 전달하세요." : "") : " 거절 완료");
    }
    private async Task SetLicenseStatus() { var l = Selected(); var form = new FormWindow(l.Blocked ? "라이선스 차단 해제" : "라이선스 차단"); var reason = form.Field("처리 사유", ""); form.Help("온라인 확인 시 반영됩니다. 오프라인 PC에는 발급한 확인 기한까지 권한이 남습니다."); if (form.Open(this)) { using var r = await Send("/api/admin/licenses/" + l.Id + "/status", HttpMethod.Post, new { Blocked = !l.Blocked, Reason = reason.Text }); await Refresh(); } }
    private async Task SetDeviceStatus(bool blocked) { if (devices.SelectedItem is not DeviceRow a) throw new InvalidOperationException("설치 PC를 선택해 주세요."); var form = new FormWindow(blocked ? "PC 강제 회수" : "PC 재인증 허용"); var reason = form.Field("처리 사유", ""); form.Help("고장 PC 회수 후 새 PC에서 같은 시리얼로 인증합니다. 옛 PC의 기존 오프라인 권한은 즉시 회수되지 않습니다."); if (form.Open(this)) { using var r = await Send("/api/admin/activations/" + a.Id + "/status", HttpMethod.Post, new { Blocked = blocked, Reason = reason.Text }); await Refresh(); } }
    private async Task Offline()
    {
        var open = new OpenFileDialog { Filter = "클로리움 인증 요청|*.clreq" }; if (open.ShowDialog(this) != true) return;
        string raw = File.ReadAllText(open.FileName); var request = Wire.Read<ActivationRequest>(raw);
        var save = new SaveFileDialog { Filter = "클로리움 인증 응답|*.clresp", FileName = Path.GetFileNameWithoutExtension(open.FileName) + ".clresp" }; if (save.ShowDialog(this) != true) return;
        string productName = data.Products.FirstOrDefault(p => p.Code == request.Product)?.Name ?? request.Product;
        if (MessageBox.Show(this, productName + "\n" + request.Hostname + " · " + request.AppVersion + "\n" + request.Serial + "\n\n이 PC의 오프라인 인증을 처리합니까? 새 PC·임시 라이선스는 먼저 승인 대기에 등록되며, 승인 후 같은 요청 파일을 다시 처리합니다.", "오프라인 처리", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        try { using var response = await Send("/api/admin/offline", HttpMethod.Post, raw: raw); Wire.AtomicWrite(save.FileName, await response.Content.ReadAsStringAsync()); await Refresh(); status.Text = "응답 파일을 저장했습니다. 고객에게 전달해 주세요."; }
        catch (InvalidOperationException ex) when (ex.Message.Contains("APPROVAL_PENDING")) { await Refresh(); status.Text = "승인 대기 탭에 등록했습니다. 승인한 뒤 같은 요청 파일을 다시 처리해 주세요."; status.Foreground = Brushes.DarkGoldenrod; }
    }
    private async Task ExportConfig()
    {
        if (data.Products.Count == 0) throw new InvalidOperationException("서버에 연결해 제품 목록을 먼저 불러와 주세요.");
        var form = new FormWindow("고객 모듈 연결 설정"); var product = form.Choose("제품 (배포할 프로그램)", data.Products.Select(p => p.Name).ToArray(), 0);
        form.Help("클로리움에 통합된 오토사이징은 클로리움 설정을 사용합니다. 다른 PC에 단독 설치하는 오토사이징은 '클로리움 오토사이징' 설정으로 빌드합니다.");
        if (!form.Open(this)) return; var p = data.Products[product.SelectedIndex];
        using var response = await Send("/api/admin/public-key"); string key = await response.Content.ReadAsStringAsync();
        var save = new SaveFileDialog { Filter = "클로리움 연결 설정|*.json", FileName = p.Code.ToLowerInvariant() + "-license.json" };
        if (save.ShowDialog(this) == true) { Wire.AtomicWrite(save.FileName, Wire.Json(new ClientConnection { ServerUrl = server.Text.TrimEnd('/'), PublicKey = key, Product = p.Code, ProductName = p.Name })); status.Text = p.Name + " 공개 연결 설정을 저장했습니다. 해당 제품의 고객 배포 빌드에 포함하세요."; }
    }
    private async Task EditProduct(bool edit)
    {
        Product p = edit ? data.Products.SingleOrDefault(x => x.Code == (products.SelectedItem as ProductRow)?.Code) : new Product(); if (p == null) throw new InvalidOperationException("제품을 선택해 주세요.");
        var form = new FormWindow(edit ? "제품 수정 · 인증 중지" : "제품 등록");
        var code = form.Field("제품 코드 (영문, 등록 후 변경 불가 · 예: Clorium.AutoSizing)", p.Code); code.IsEnabled = !edit;
        var name = form.Field("제품 이름 (고객 화면에 표시)", p.Name); var notes = form.Field("비고", p.Notes);
        var disabled = form.Check("인증 중지 (새 인증·승인·온라인 확인 거부, 인증된 PC는 다음 접속 때 적용)", p.Disabled);
        form.Help("새 제품은 같은 라이선스 체계(영구·임시·버전 확인·PC 변경 승인)를 그대로 사용합니다. 고객 모듈은 '고객 모듈 연결 설정 내보내기'에서 이 제품을 골라 빌드합니다.");
        if (form.Open(this)) { using var r = await Send("/api/admin/products", HttpMethod.Post, new Product { Code = code.Text.Trim(), Name = name.Text, Notes = notes.Text, Disabled = disabled.IsChecked == true }); await Refresh(); }
    }
    private async Task Download(string endpoint, string name, string filter) { var save = new SaveFileDialog { FileName = name, Filter = filter }; if (save.ShowDialog(this) != true) return; using var r = await Send(endpoint); PrivateFiles.Save(save.FileName, await r.Content.ReadAsByteArrayAsync()); status.Text = "접근을 제한하여 저장했습니다: " + save.FileName; }
    public static string Date(long t) => t == 0 ? "영구" : DateTimeOffset.FromUnixTimeSeconds(t).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public void LoadExample()
    {
        data.Products.Add(new Product { Code = Products.Clorium, Name = "클로리움" }); data.Products.Add(new Product { Code = Products.AutoSizing, Name = "클로리움 오토사이징" });
        var c = new Customer { Id = "example", Company = "샘플 고객사 · 디자인 확인용", Contact = "김담당", Email = "contact@example.com" }; data.Customers.Add(c);
        data.Licenses.Add(new LicenseRecord { CustomerId = c.Id, Product = Products.AutoSizing, Seats = 1 });
        foreach (string purpose in new[] { null, "Demo", "Test" }) { var l = new LicenseRecord { CustomerId = c.Id, Kind = purpose == null ? "Perpetual" : "Temporary", Purpose = purpose, TermMonths = purpose == "Demo" ? 1 : 12, Seats = 3, ApprovedAt = Wire.Now, ExpiresAt = purpose == null ? 0 : Wire.AddKstMonths(Wire.Now, purpose == "Demo" ? 1 : 12) }; data.Licenses.Add(l); data.Activations.Add(new Activation { LicenseId = l.Id, Hostname = "STUDIO-PC-01", AppVersion = "4.7.1.5", OsVersion = "Windows 11", Active = true }); data.Audit.Add(new Audit { Target = l.Id, Action = "라이선스 발급", Detail = "화면 확인용 예시 데이터" }); }
        var waiting = new LicenseRecord { CustomerId = c.Id, Kind = "Temporary" }; data.Licenses.Add(waiting); data.Requests.Add(new ApprovalRequest { Code = "R-EXAMPL", Type = "Temporary", LicenseId = waiting.Id, Hostname = "DEMO-LAPTOP", AppVersion = "4.7.1.5" }); Populate();
        status.Text = "화면 확인용 예시 · 실제 고객 데이터와 서버에 연결하지 않았습니다.";
    }
    public async Task VerifyConnectedUi(string png, string result)
    {
        await Refresh();
        if (data.Licenses.Count < 4 || licenses.Items.Count != data.Licenses.Count || customers.Items.Count != data.Customers.Count) throw new InvalidOperationException("서버 조회가 화면에 연결되지 않았습니다.");
        var first = (LicenseRow)licenses.Items[0]; search.Text = first.Serial;
        if (licenses.Items.Count != 1 || devices.ItemsSource == null || history.ItemsSource == null) throw new InvalidOperationException("검색/선택/상세 연결 오류");
        search.Text = "찾을수없는고객사"; if (licenses.Items.Count != 0) throw new InvalidOperationException("검색 빈 상태 오류"); search.Text = "";
        if (data.Licenses.Any(l => l.IsTemporary && l.IsApproved) && !licenses.Items.Cast<LicenseRow>().Any(r => r.Kind.StartsWith("임시") && r.Expiry != "영구")) throw new InvalidOperationException("임시 기간 표시 오류");
        if (products.Items.Count != data.Products.Count || data.Products.Count < 2) throw new InvalidOperationException("제품 목록 표시 오류");
        var auto = data.Products.First(p => p.Code == Products.AutoSizing); productFilter.SelectedItem = auto.Name;
        if (licenses.Items.Count != data.Licenses.Count(l => l.Product == auto.Code) || licenses.Items.Count == 0 || licenses.Items.Cast<LicenseRow>().Any(r => r.Product != auto.Name)) throw new InvalidOperationException("제품 필터 오류");
        productFilter.SelectedItem = AllProducts;
        if (requests.Items.Count != data.Requests.Count ||(data.Requests.Any(x => x.Status == "Pending") && !((string)requestTab.Header).Contains("("))) throw new InvalidOperationException("승인 대기 표시 오류");
        using (var backup = await Send("/api/admin/backup")) PrivateFiles.Save(result + ".backup.zip", await backup.Content.ReadAsByteArrayAsync());
        UpdateLayout();
        var image = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32); image.Render(this);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using (var f = File.Create(png)) encoder.Save(f);
        File.WriteAllText(result, "PASS real server connection, customer/license display, serial search, empty search, selected device/history binding, approval queue binding, product list and product filter, private backup save; rendered actual WPF controls.");
    }
}
public class LicenseRow
{
    public string Id { get; } public string Product { get; } public string Company { get; } public string Serial { get; } public string Kind { get; } public string Seats { get; } public string Version { get; } public string Expiry { get; } public string Status { get; }
    public LicenseRow(LicenseRecord l, Database d)
    {
        Id = l.Id; Product = d.Products.FirstOrDefault(p => p.Code == l.Product)?.Name ?? l.Product; Company = d.Customers.FirstOrDefault(x => x.Id == l.CustomerId)?.Company; Serial = l.Serial;
        Kind = !l.IsTemporary ? "정식 영구" : !l.IsApproved ? "임시 · 승인 전" : "임시 " + (l.Purpose == "Demo" ? "데모" : "테스트") + (l.TermMonths > 0 ? " " + (l.TermMonths == 12 ? "1년" : l.TermMonths + "개월") : "");
        Seats = d.Activations.Count(x => x.LicenseId == l.Id && x.Active) + " / " + l.Seats; Version = l.MaxMajor + ".x";
        Expiry = !l.IsTemporary ? "영구" : !l.IsApproved ? "승인 시 결정" : ManagerWindow.Date(l.ExpiresAt);
        Status = l.Blocked ? "차단" : d.Products.Any(p => p.Code == l.Product && p.Disabled) ? "제품 중지" : d.Customers.Any(c => c.Id == l.CustomerId && c.Disabled) ? "계정 중지" : d.Requests.Any(x => x.LicenseId == l.Id && x.Status == "Pending") ? "승인 대기" : l.IsTemporary && l.IsApproved && Wire.Now >= l.ExpiresAt ? "만료" : "정상";
    }
}
public class RequestRow
{
    public string Id { get; } public string Code { get; } public string Product { get; } public string Type { get; } public string Company { get; } public string Serial { get; } public string Hostname { get; } public string AppVersion { get; } public string RequestedAt { get; } public string Status { get; } public string Decision { get; }
    public RequestRow(ApprovalRequest q, Database d)
    {
        var l = d.Licenses.FirstOrDefault(x => x.Id == q.LicenseId); Id = q.Id; Code = q.Code; Product = d.Products.FirstOrDefault(p => p.Code == l?.Product)?.Name ?? l?.Product; Type = q.Type == "Temporary" ? "임시 사용" : "PC 변경";
        Company = d.Customers.FirstOrDefault(x => x.Id == l?.CustomerId)?.Company; Serial = l?.Serial; Hostname = q.Hostname + (q.Offline ? " (오프라인)" : ""); AppVersion = q.AppVersion;
        RequestedAt = ManagerWindow.Date(q.RequestedAt); Status = q.Status == "Pending" ? "대기" : q.Status == "Approved" ? "승인" : "거절"; Decision = q.Decision;
    }
}
public class DeviceRow
{
    public string Id { get; } public string Hostname { get; } public string AppVersion { get; } public string OsVersion { get; } public string LastSeen { get; } public string Status { get; } public string Mode { get; }
    public DeviceRow(Activation a) { Id = a.Id; Hostname = a.Hostname; AppVersion = a.AppVersion; OsVersion = a.OsVersion; LastSeen = ManagerWindow.Date(a.LastSeen); Status = a.Blocked ? "강제 회수" : a.Active ? "활성" : "반납"; Mode = a.Offline ? "오프라인" : "온라인"; }
}
public class ProductRow
{
    public string Code { get; } public string Name { get; } public int Licenses { get; } public int Devices { get; } public int Pending { get; } public string Status { get; } public string Notes { get; }
    public ProductRow(Product p, Database d)
    {
        Code = p.Code; Name = p.Name; Notes = p.Notes; Status = p.Disabled ? "인증 중지" : "사용";
        var ids = d.Licenses.Where(l => l.Product == p.Code).Select(l => l.Id).ToHashSet(); Licenses = ids.Count;
        Devices = d.Activations.Count(a => a.Active && ids.Contains(a.LicenseId)); Pending = d.Requests.Count(r => r.Status == "Pending" && ids.Contains(r.LicenseId));
    }
}
[System.Runtime.Serialization.DataContract] public class ClientConnection
{
    [System.Runtime.Serialization.DataMember] public string ServerUrl { get; set; }
    [System.Runtime.Serialization.DataMember] public string PublicKey { get; set; }
    [System.Runtime.Serialization.DataMember] public string Product { get; set; }
    [System.Runtime.Serialization.DataMember] public string ProductName { get; set; }
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
