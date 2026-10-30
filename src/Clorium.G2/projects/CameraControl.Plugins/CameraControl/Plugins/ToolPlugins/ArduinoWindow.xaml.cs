using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;

namespace CameraControl.Plugins.ToolPlugins;
public partial class ArduinoWindow : Window
{
    public ArduinoWindow()
    {
        InitializeComponent();
    }

    private void MetroWindow_Closed(object sender, EventArgs e)
    {
        ((ArduinoViewModel)((FrameworkElement)this).DataContext).SaveButtons();
    }
}
