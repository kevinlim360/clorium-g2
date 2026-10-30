using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Clorium_Centering;
public partial class ProgressRingControl : UserControl, IComponentConnector
{
    public ProgressRingControl()
    {
        InitializeComponent();
    }

    public async Task ShowAsync()
    {
        Visibility = Visibility.Visible;
        DoubleAnimation animation = new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(200.0));
        BeginAnimation(UIElement.OpacityProperty, animation);
        await Task.Delay(200);
    }

    public async Task HideAsync()
    {
        DoubleAnimation animation = new DoubleAnimation(1.0, 0.0, TimeSpan.FromMilliseconds(200.0));
        BeginAnimation(UIElement.OpacityProperty, animation);
        await Task.Delay(200);
        Visibility = Visibility.Collapsed;
    }
}
