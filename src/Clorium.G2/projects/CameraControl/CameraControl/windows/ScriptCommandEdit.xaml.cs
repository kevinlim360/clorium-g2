using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace CameraControl.windows;

public partial class ScriptCommandEdit : Window, IComponentConnector
{
	public ScriptCommandEdit(UserControl control)
	{
		InitializeComponent();
		stackPanel.Children.Add(control);
	}

	private void button1_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
