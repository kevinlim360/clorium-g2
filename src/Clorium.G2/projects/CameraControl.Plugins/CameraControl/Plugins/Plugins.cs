using System;
using CameraControl.Core;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Plugins.AutoExportPlugins;
using CameraControl.Plugins.ExportPlugins;
using CameraControl.Plugins.ExternalDevices;
using CameraControl.Plugins.FilenameTemplate;
using CameraControl.Plugins.ImageTransformPlugins;
using CameraControl.Plugins.MainWindowPlugins;
using CameraControl.Plugins.PanelPlugins;
using CameraControl.Plugins.ToolPlugins;

namespace CameraControl.Plugins;

public class Plugins : IPlugin
{
	public bool Register()
	{
		try
		{
			ServiceProvider.PluginManager.ExportPlugins.Add(new ExportToZip());
			ServiceProvider.PluginManager.ExportPlugins.Add(new ExportToFolder());
			ServiceProvider.PluginManager.ExportPlugins.Add(new ExportCsv());
			ServiceProvider.PluginManager.MainWindowPlugins.Add(new SimpleMainWindow());
			ServiceProvider.PluginManager.ToolPlugins.Add(new PhdPlugin());
			ServiceProvider.PluginManager.ToolPlugins.Add(new GenThumbPlugin());
			ServiceProvider.PluginManager.ToolPlugins.Add(new ImageSequencerPlugin());
			ServiceProvider.PluginManager.ToolPlugins.Add(new GenMoviePlugin());
			ServiceProvider.PluginManager.ToolPlugins.Add(new EnfusePlugin());
			ServiceProvider.PluginManager.ToolPlugins.Add(new CombineZpPlugin());
			ServiceProvider.PluginManager.ToolPlugins.Add(new StatisticsPlugin());
			ServiceProvider.PluginManager.ToolPlugins.Add(new ArduinoPlugin());
			ServiceProvider.ExternalDeviceManager.ExternalDevices.Add(new SerialPortShutterRelease());
			ServiceProvider.ExternalDeviceManager.ExternalDevices.Add(new DSUSBShutterRelease());
			ServiceProvider.ExternalDeviceManager.ExternalDevices.Add(new MultiCameraBoxShutterRelease());
			ServiceProvider.ExternalDeviceManager.ExternalDevices.Add(new DCCUSBShutterRelease());
			ServiceProvider.ExternalDeviceManager.ExternalDevices.Add(new UsbRelayRelease());
			ServiceProvider.ExternalDeviceManager.ExternalDevices.Add(new HidUsbRelay());
			ServiceProvider.ExternalDeviceManager.ExternalDevices.Add(new ArduinoShutterRelease());
			ServiceProvider.PluginManager.AutoExportPlugins.Add(new TransformPlugin());
			ServiceProvider.PluginManager.AutoExportPlugins.Add(new CopyFilePlugin());
			ServiceProvider.PluginManager.AutoExportPlugins.Add(new ExecuteFilePlugin());
			ServiceProvider.PluginManager.AutoExportPlugins.Add(new PrintPlugin());
			ServiceProvider.PluginManager.AutoExportPlugins.Add(new FtpPlugin());
			ServiceProvider.PluginManager.AutoExportPlugins.Add(new SendEmailPlugin());
			ServiceProvider.PluginManager.AutoExportPlugins.Add(new DropboxPlugin());
			ServiceProvider.PluginManager.ImageTransformPlugins.Add(new NoTransform());
			ServiceProvider.PluginManager.ImageTransformPlugins.Add(new ResizeTransform());
			ServiceProvider.PluginManager.ImageTransformPlugins.Add(new CropTransform());
			ServiceProvider.PluginManager.ImageTransformPlugins.Add(new OverlayTransform());
			ServiceProvider.PluginManager.ImageTransformPlugins.Add(new RotateTransform());
			ServiceProvider.PluginManager.ImageTransformPlugins.Add(new PixelBinning());
			ServiceProvider.PluginManager.ImageTransformPlugins.Add(new Enhance());
			ServiceProvider.PluginManager.ImageTransformPlugins.Add(new Effect());
			ServiceProvider.PluginManager.ImageTransformPlugins.Add(new Chromakey());
			ServiceProvider.PluginManager.PanelPlugins.Add(new OpenInExternalViewer());
			ExifTemplate exiftemplate = new ExifTemplate();
			ServiceProvider.FilenameTemplateManager.Templates.Add("[ArduinoLabel]", new ArduinoLabel());
			ServiceProvider.FilenameTemplateManager.Templates.Add("[BarcodeFromImage]", new BarcodeFromImage());
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Image.DateTime]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.ColorSpace]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.ExposureTime]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.ExposureProgram]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.ExposureBiasValue]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.ExposureMode]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.Flash]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.FNumber]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.FocalLength]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.FocalLengthIn35mmFilm]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.ISOSpeedRatings]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.MeteringMode]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.PixelXDimension]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.PixelYDimension]", exiftemplate);
			ServiceProvider.FilenameTemplateManager.Templates.Add("[Exif.Photo.WhiteBalance]", exiftemplate);
		}
		catch (Exception exception)
		{
			Log.Error("Error loadings plugins ", exception);
		}
		return true;
	}

	public void Init()
	{
		foreach (IToolPlugin plugin in ServiceProvider.PluginManager.ToolPlugins)
		{
			plugin.Init();
		}
	}
}
