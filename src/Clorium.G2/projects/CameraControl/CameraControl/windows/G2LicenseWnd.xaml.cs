using System;
using System.Windows;
using CameraControl.Core;

namespace CameraControl.windows;

// 설정 > 라이선스: shows the start-up license result (matched D:\ key file or none) and the PC identifiers to report.
public partial class G2LicenseWnd : Window
{
	public G2LicenseWnd()
	{
		InitializeComponent();
		VersionText.Text = ServiceProvider.Settings?.Version_Debug ?? "-";
		var info = (Application.Current as App)?.LegacyLicenseInfo() ?? (null, "", "라이선스 정보를 읽을 수 없습니다.");
		IdentifierBox.Text = string.IsNullOrEmpty(info.Identifiers) ? "PC 식별 정보를 읽지 못했습니다." : info.Identifiers;
		if (info.KeyFile != null)
		{
			StateText.Text = "사용 중 · 이 PC용 키 파일 확인됨";
			KeyFileText.Text = info.KeyFile;
		}
		else
		{
			StateChip.Background = (System.Windows.Media.Brush)FindResource("G2PrBg");
			StateText.Foreground = (System.Windows.Media.Brush)FindResource("G2Pr");
			StateText.Text = "키 파일 없음";
			KeyFileText.Text = "D:\\에 이 PC와 일치하는 키 파일이 없습니다.";
		}
		if (!string.IsNullOrEmpty(info.Error))
		{
			ErrorText.Text = info.Error;
			ErrorText.Visibility = Visibility.Visible;
		}
	}

	private void Copy_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(IdentifierBox.Text);
		}
		catch (System.Runtime.InteropServices.ExternalException)
		{
			// clipboard busy: the text can still be selected and copied by hand
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
