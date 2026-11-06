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
		var info = (Application.Current as App)?.LegacyLicenseInfo() ?? (null, "", global::CameraControl.Devices.UiText.T("M0423", "라이선스 정보를 읽을 수 없습니다."));
		IdentifierBox.Text = string.IsNullOrEmpty(info.Identifiers) ? global::CameraControl.Devices.UiText.T("M0091", "PC 식별 정보를 읽지 못했습니다.") : info.Identifiers;
		if (info.KeyFile != null)
		{
			StateText.Text = global::CameraControl.Devices.UiText.T("M0424", "사용 중 · 이 PC용 키 파일 확인됨");
			KeyFileText.Text = info.KeyFile;
		}
		else
		{
			StateChip.Background = (System.Windows.Media.Brush)FindResource("G2PrBg");
			StateText.Foreground = (System.Windows.Media.Brush)FindResource("G2Pr");
			StateText.Text = global::CameraControl.Devices.UiText.T("M0425", "키 파일 없음");
			KeyFileText.Text = global::CameraControl.Devices.UiText.T("M0426", "D:\\ 드라이브에 이 PC용 키 파일이 없습니다.");
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

	// Moved here from the main window's version label (double-click) with the version itself.
	public const string ChangeLogPath = "D:\\Clorium Data\\Clorium_Version.md";

	private void ChangeLog_Click(object sender, RoutedEventArgs e)
	{
		if (!System.IO.File.Exists(ChangeLogPath))
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0427", "변경 내역 파일이 없습니다.\n") + ChangeLogPath, global::CameraControl.Devices.UiText.T("M0428", "변경 내역"), MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}
		try
		{
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("notepad.exe", "\"" + ChangeLogPath + "\"") { UseShellExecute = false });
		}
		catch (Exception ex)
		{
			global::CameraControl.Core.Classes.G2Dialog.Show(global::CameraControl.Devices.UiText.T("M0429", "파일을 열 수 없습니다: ") + ex.Message, global::CameraControl.Devices.UiText.T("M0428", "변경 내역"));
		}
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
