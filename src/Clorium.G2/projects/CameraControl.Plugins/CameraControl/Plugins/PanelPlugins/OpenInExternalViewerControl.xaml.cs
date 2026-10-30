using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using CameraControl.Core;

namespace CameraControl.Plugins.PanelPlugins;
public partial class OpenInExternalViewerControl : UserControl
{
    public OpenInExternalViewerControl()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        ServiceProvider.WindowsManager.ExecuteCommand("ViewExternal");
    }
}
