using System.Windows;
using System.Windows.Markup;

namespace CameraControl.windows;

public partial class ErrorReportWnd : Window, IComponentConnector
{
	private string _type;

	private string _message;

	public ErrorReportWnd(string type, string message = "")
	{
		InitializeComponent();
		_type = type;
		_message = message;
	}

	private void btn_cancel_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void btn_send_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
