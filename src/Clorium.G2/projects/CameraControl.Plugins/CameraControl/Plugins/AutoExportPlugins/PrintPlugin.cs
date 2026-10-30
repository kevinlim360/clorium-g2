using System;
using System.IO;
using System.Printing;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;

namespace CameraControl.Plugins.AutoExportPlugins;

public class PrintPlugin : IAutoExportPlugin
{
	public string Name => "Print File";

	public bool Execute(FileItem item, AutoExportPluginConfig configData)
	{
		Thread thread = new Thread(() =>
		{
			Print(item, configData);
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
		return true;
	}

	private void Print(FileItem item, AutoExportPluginConfig configData)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected Obj, but got Unknown
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected Obj, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Expected Obj, but got Unknown
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			PrintDialog dlg = new PrintDialog();
			configData.IsRedy = false;
			configData.IsError = false;
			PrintPluginViewModel conf = new PrintPluginViewModel(configData);
			string outfile = PhotoUtils.ReplaceExtension(Path.GetTempFileName(), Path.GetExtension(item.Name));
			outfile = AutoExportPluginHelper.ExecuteTransformPlugins(item, configData, outfile);
			PrintCapabilities capabilities = dlg.PrintQueue.GetPrintCapabilities(dlg.PrintTicket);
			int PageWidth = (int)capabilities.PageImageableArea.ExtentWidth;
			int PageHeight = (int)capabilities.PageImageableArea.ExtentHeight;
			StackPanel panel = new StackPanel
			{
				Margin = new Thickness((double)conf.Margin)
			};
			Image image = new Image
			{
				Source = (ImageSource)(object)BitmapLoader.Instance.LoadImage(outfile, 0, conf.Rotate ? 90 : 0),
				Width = PageWidth,
				Height = PageHeight,
				Stretch = (Stretch)2
			};
			((Panel)panel).Children.Add((UIElement)(object)image);
			((UIElement)panel).UpdateLayout();
			((UIElement)panel).Measure(new Size((double)PageWidth, (double)PageHeight));
			((UIElement)panel).Arrange(new Rect(new Point(0.0, 0.0), ((UIElement)panel).DesiredSize));
			((UIElement)panel).UpdateLayout();
			dlg.PrintVisual((Visual)(object)panel, item.Name);
			image.Source = null;
			((Panel)panel).Children.Clear();
			if (outfile != item.FileName)
			{
				PhotoUtils.WaitForFile(outfile);
				File.Delete(outfile);
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error print file", ex);
			configData.IsError = true;
			configData.Error = ex.Message;
		}
		configData.IsRedy = true;
	}

	public UserControl GetConfig(AutoExportPluginConfig configData)
	{
		PrintPluginConfig printPluginConfig = new PrintPluginConfig();
		((FrameworkElement)printPluginConfig).DataContext = new PrintPluginViewModel(configData);
		return (UserControl)(object)printPluginConfig;
	}
}
