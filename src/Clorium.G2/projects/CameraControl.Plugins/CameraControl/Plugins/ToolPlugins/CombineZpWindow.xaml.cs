using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core.Classes;

namespace CameraControl.Plugins.ToolPlugins;
public partial class CombineZpWindow : Window
{
    public CombineZpWindow()
    {
        InitializeComponent();
    }

    private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
    {
        PhotoUtils.Run("http://www.hadleyweb.pwp.blueyonder.co.uk/");
    }
}
