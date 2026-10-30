using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using CameraControl.Core.Controls.ZoomAndPan;

namespace CameraControl.Plugins.ToolPlugins;
public partial class EnfusePluginWindow : Window
{
    public EnfusePluginWindow()
    {
        InitializeComponent();
    }

    private void zoomAndPanControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        zoomAndPanControl.ScaleToFit();
    }

    private void zoomAndPanControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        //IL_0008: Unknown result type (might be due to invalid IL or missing references)
        //IL_000d: Unknown result type (might be due to invalid IL or missing references)
        //IL_003d: Unknown result type (might be due to invalid IL or missing references)
        Point curContentMousePoint = ((MouseEventArgs)e).GetPosition((IInputElement)(object)Image);
        if (zoomAndPanControl.ContentScale <= zoomAndPanControl.FitScale())
        {
            zoomAndPanControl.ZoomAboutPoint(4.0, curContentMousePoint);
        }
        else
        {
            zoomAndPanControl.ScaleToFit();
        }
    }

    private void zoomAndPanControl_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        //IL_0010: Unknown result type (might be due to invalid IL or missing references)
        //IL_0015: Unknown result type (might be due to invalid IL or missing references)
        //IL_002a: Unknown result type (might be due to invalid IL or missing references)
        //IL_006f: Unknown result type (might be due to invalid IL or missing references)
        ((RoutedEventArgs)e).Handled = true;
        Point curContentMousePoint = ((MouseEventArgs)e).GetPosition((IInputElement)(object)Image);
        if (e.Delta > 0)
        {
            zoomAndPanControl.ZoomIn(curContentMousePoint);
        }
        else if (e.Delta < 0)
        {
            if (zoomAndPanControl.ContentScale - 0.2 > zoomAndPanControl.FitScale())
            {
                zoomAndPanControl.ZoomOut(curContentMousePoint);
            }
            else
            {
                zoomAndPanControl.ScaleToFit();
            }
        }
    }

    private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
    {
        zoomAndPanControl.ScaleToFit();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
    }
}
