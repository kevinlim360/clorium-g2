using System;
using System.IO;
using System.Xml.Serialization;
using CameraControl.Devices;

namespace CameraControl.Core.Classes;

public class Branding
{
	private string _startupScreenImage;

	private string _logoImage;

	private string _defaultThumbImage;

	public string StartupScreenImage
	{
		get
		{
			return (_startupScreenImage != null && !_startupScreenImage.Contains("\\")) ? Path.Combine(Settings.ApplicationFolder, _startupScreenImage) : _startupScreenImage;
		}
		set
		{
			_startupScreenImage = value;
		}
	}

	public string LogoImage
	{
		get
		{
			return (_logoImage != null && !_logoImage.Contains("\\")) ? Path.Combine(Settings.ApplicationFolder, _logoImage) : _logoImage;
		}
		set
		{
			_logoImage = value;
		}
	}

	public string DefaultThumbImage
	{
		get
		{
			return (_defaultThumbImage != null && !_defaultThumbImage.Contains("\\")) ? Path.Combine(Settings.ApplicationFolder, _defaultThumbImage) : _defaultThumbImage;
		}
		set
		{
			_defaultThumbImage = value;
		}
	}

	public string ApplicationDataFolder { get; set; }

	public string ApplicationTitle { get; set; }

	public string DefaultMissingThumbImage { get; set; }

	public bool ResetSettingsOnLoad { get; set; }

	public bool ShowAboutWindow { get; set; }

	public bool OnlineReference { get; set; }

	public bool ShowStartupScreen { get; set; }

	public bool ShowStartupScreenAnimation { get; set; }

	public bool ShowWelcomeScreen { get; set; }

	public bool CheckForUpdate { get; set; }

	public bool ShowRefreshButtonMainWindow { get; set; }

	public bool ShowDownloadButtonMainWindow { get; set; }

	public bool ShowBracketingButtonMainWindow { get; set; }

	public bool ShowTimelapseButtonMainWindow { get; set; }

	public bool ShowFullScreenButtonMainWindow { get; set; }

	public bool ShowLiveViewButtonMainWindow { get; set; }

	public bool ShowBrowseButtonMainWindow { get; set; }

	public bool ShowTagsButtonMainWindow { get; set; }

	public bool ShowAstronomyButtonMainWindow { get; set; }

	public bool ShowMultiCameraButtonMainWindow { get; set; }

	public bool ShowWifiCameraButtonMainWindow { get; set; }

	public bool ShowPrintButtonMainWindow { get; set; }

	public bool ShowSettingMenuMainWindow { get; set; }

	public bool ShowMenuMenuMainWindow { get; set; }

	public bool ShowCameraSelectorMainWindow { get; set; }

	public bool ShowRightPanelMainWindow { get; set; }

	public bool ShowCameraPropertiesMainWindow { get; set; }

	public bool ShowFocusControlLiveView { get; set; }

	public bool ShowFocusStackingLiveView { get; set; }

	public bool ShowControlLiveView { get; set; }

	public bool ShowOverlayLiveView { get; set; }

	public bool ShowDisplayLiveView { get; set; }

	public bool ShowLevelLiveView { get; set; }

	public bool ShowAutofocusLiveView { get; set; }

	public bool ShowLuminosityLiveView { get; set; }

	public bool ShowMotionDetectionLiveView { get; set; }

	public bool ShowAnimation { get; set; }

	public bool ShowHistogram { get; set; }

	public bool ShowPreviewPanel { get; set; }

	public bool ShowSessionPanel { get; set; }

	public bool ShowAutoExportPanel { get; set; }

	public bool ShowPresetPanel { get; set; }

	public bool ShowImagePropertiesPanel { get; set; }

	public bool ShowMetadataPanel { get; set; }

	public bool ShowMainMenu { get; set; }

	public bool ShowBattery { get; set; }

	public string DefaultTheme { get; set; }

	public string HomePageUrl { get; set; }

	public bool ShowThumbPanel { get; set; }

	public bool ShowViewMenu { get; set; }

	public bool ShowCameraMenu { get; set; }

	public bool ShowPluginMenu { get; set; }

	public bool ShowRulerButtonMainWindow { get; set; }

	public bool ShowLightButtonMainWindow { get; set; }

	public bool ShowTurnTableButtonMainWindow { get; set; }

	public bool ShowEnfuseButtonMainWindow { get; set; }

	public bool ShowAIRemoveBgMainWindow { get; set; }

	public bool ShowCameraColorTemperaturePanel { get; set; }

	public bool ShowFrontBackPanel { get; set; }

	public bool ShowProduct { get; set; }

	public bool ControlPassword { get; set; }

	public bool ControlLighting { get; set; }

	public bool ControlTurnTable { get; set; }

	public bool ControlMultiCamera { get; set; }

	public bool ControlMonstarDeck { get; set; }

	public bool ControlBarcodeScanner { get; set; }

	public bool ControlRFIDScanner { get; set; }

	public bool ControlRemoveBackground { get; set; }

	public bool ControlAutoSizing { get; set; }

	public bool ControlWebCam { get; set; }

	public bool ControlFTPServer { get; set; }

	public bool ControlLEDDimmer { get; set; }

	public bool UseThemeSelector => string.IsNullOrEmpty(DefaultTheme);

	public string DefaultSettings => Path.Combine(Settings.BrandingFolder, "default_settings.xml");

	public Branding()
	{
		ApplicationTitle = "Clorium";
		ApplicationDataFolder = string.Empty;
		ResetSettingsOnLoad = false;
		CheckForUpdate = false;
		ShowStartupScreen = true;
		ShowStartupScreenAnimation = true;
		ShowWelcomeScreen = false;
		ShowAboutWindow = false;
		ShowMainMenu = true;
		ShowViewMenu = true;
		ShowCameraMenu = true;
		ShowPluginMenu = false;
		OnlineReference = false;
		ShowSettingMenuMainWindow = true;
		ShowMenuMenuMainWindow = true;
		ShowCameraPropertiesMainWindow = true;
		ShowCameraSelectorMainWindow = true;
		ShowRightPanelMainWindow = true;
		ShowMultiCameraButtonMainWindow = true;
		ShowLiveViewButtonMainWindow = true;
		ShowFullScreenButtonMainWindow = true;
		ShowBrowseButtonMainWindow = true;
		ShowAIRemoveBgMainWindow = true;
		ShowRulerButtonMainWindow = true;
		ShowLightButtonMainWindow = true;
		ShowTurnTableButtonMainWindow = true;
		ShowWifiCameraButtonMainWindow = false;
		ShowDownloadButtonMainWindow = false;
		ShowBracketingButtonMainWindow = false;
		ShowTimelapseButtonMainWindow = false;
		ShowAstronomyButtonMainWindow = false;
		ShowPrintButtonMainWindow = false;
		ShowEnfuseButtonMainWindow = false;
		ShowBattery = true;
		ShowAnimation = true;
		ShowThumbPanel = true;
		ShowHistogram = false;
		ShowSessionPanel = true;
		ShowPresetPanel = true;
		ShowCameraColorTemperaturePanel = true;
		ShowFrontBackPanel = true;
		ShowProduct = true;
		ShowPreviewPanel = false;
		ShowImagePropertiesPanel = false;
		ShowAutoExportPanel = false;
		ShowMetadataPanel = false;
		ShowAutofocusLiveView = true;
		ShowOverlayLiveView = true;
		ShowDisplayLiveView = true;
		ShowLuminosityLiveView = false;
		ShowControlLiveView = false;
		ShowFocusControlLiveView = false;
		ShowFocusStackingLiveView = false;
		ShowMotionDetectionLiveView = false;
		ShowLevelLiveView = false;
		ControlPassword = false;
		ControlLighting = false;
		ControlTurnTable = false;
		ControlMultiCamera = false;
		ControlMonstarDeck = false;
		ControlBarcodeScanner = false;
		ControlRFIDScanner = false;
		ControlLEDDimmer = false;
		ControlRemoveBackground = false;
		ControlAutoSizing = false;
		ControlWebCam = false;
		ControlFTPServer = false;
	}

	public static Branding LoadBranding()
	{
		Branding branding = new Branding();
		string filename = Path.Combine(Settings.ApplicationFolder, "Branding.xml");
		try
		{
			if (File.Exists(filename))
			{
				XmlSerializer mySerializer = new XmlSerializer(typeof(Branding));
				FileStream myFileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
				branding = (Branding)mySerializer.Deserialize(myFileStream);
				myFileStream.Close();
			}
		}
		catch (Exception message)
		{
			Log.Error(message);
		}
		return branding;
	}
}
