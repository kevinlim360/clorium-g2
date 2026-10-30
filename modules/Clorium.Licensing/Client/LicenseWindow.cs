using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace Clorium.Licensing
{
    public class LicenseWindow : Window
    {
        private readonly LicenseClient client;
        private readonly TextBox serial = new TextBox { Height = 36, FontSize = 16, VerticalContentAlignment = VerticalAlignment.Center };
        private readonly TextBlock status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 16) };
        private readonly StackPanel actions = new StackPanel();
        public LicenseWindow(LicenseClient client)
        {
            this.client = client; Title = "클로리움 라이선스"; Width = 560; Height = 500; MinHeight = 480; MinWidth = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner; FontFamily = new FontFamily("Malgun Gothic"); FontSize = 13; Background = Brushes.White;
            var panel = new StackPanel { Margin = new Thickness(28) };
            panel.Children.Add(new TextBlock { Text = "Clorium  /  제품 인증", FontSize = 23, FontWeight = FontWeights.SemiBold });
            panel.Children.Add(new TextBlock { Text = "시리얼을 한 번 입력하면 이 PC에서 자동으로 인증합니다.", Margin = new Thickness(0, 10, 0, 20), Foreground = Brushes.DimGray });
            panel.Children.Add(new TextBlock { Text = "시리얼번호", Margin = new Thickness(0, 0, 0, 6) }); panel.Children.Add(serial); panel.Children.Add(status); panel.Children.Add(actions);
            Add("인증하고 계속", async () => { await client.ActivateAsync(serial.Text); Refresh(); if (client.Validate() == null) Close(); });
            Add("인증 상태 새로고침", async () => { await client.RefreshAsync(true); Refresh(); });
            Add("오프라인 요청 파일 저장", () => { var dialog = new SaveFileDialog { Filter = "클로리움 인증 요청|*.clreq", FileName = "Clorium-" + Environment.MachineName + ".clreq" }; if (dialog.ShowDialog() == true) { File.WriteAllText(dialog.FileName, client.OfflineRequest(serial.Text)); status.Text = "요청 파일을 회사 담당자에게 전달하고 응답 파일을 가져오세요. 요청에는 이 PC의 인증값이 포함됩니다."; } return Task.CompletedTask; });
            Add("오프라인 응답 파일 가져오기", () => { var dialog = new OpenFileDialog { Filter = "클로리움 인증 응답|*.clresp" }; if (dialog.ShowDialog() == true) { client.ImportOffline(File.ReadAllText(dialog.FileName)); Refresh(); if (client.Validate() == null) Close(); } return Task.CompletedTask; });
            Add("이 PC에서 반납 · 다른 PC로 이전", async () => {
                if (MessageBox.Show(this, "현재 PC에서 새 촬영을 할 수 없게 됩니다. 진행 중인 촬영·저장을 마친 후 반납하세요.\n계속하시겠습니까?", "PC 이전", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) { await client.ReleaseAsync(); Refresh(); }
            });
            Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; Refresh();
        }
        private void Add(string text, Func<Task> action)
        {
            var button = new Button { Content = text, Height = 34, Margin = new Thickness(0, 0, 0, 6), HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12, 0, 0, 0) };
            button.Click += async (s, e) => { actions.IsEnabled = false; try { await action(); } catch (Exception ex) { status.Text = ex.Message; status.Foreground = Brushes.Firebrick; } finally { actions.IsEnabled = true; } };
            actions.Children.Add(button);
        }
        private void Refresh()
        {
            string error = client.Validate(); var g = client.CurrentGrant;
            if (g != null) serial.Text = g.Serial;
            status.Foreground = error == null ? Brushes.DarkGreen : Brushes.Firebrick;
            status.Text = error ?? (g.Customer + " · " + (g.Kind == "Perpetual" ? "영구 사용" : g.Kind == "Demo" ? "데모" : "테스트") + " · " + client.Version + "\n" + (g.ExpiresAt == 0 ? "사용권 만료 없음" : "만료 " + DateTimeOffset.FromUnixTimeSeconds(g.ExpiresAt).ToLocalTime().ToString("yyyy-MM-dd HH:mm")));
        }
    }
}
