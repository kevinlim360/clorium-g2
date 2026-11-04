// net48 check of the actual G2 XAML/Settings wiring for 오토사이징 (run by Run-UiBinding.ps1 against a built CameraControl):
// 클로리움 설정 switch -> Settings.UseAutoSizing -> left-rail 오토사이징 button visibility, persistence, legacy launcher gone.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Media;
using CameraControl.Core;
using CameraControl.Core.Classes;
using Newtonsoft.Json;

class UiBinding
{
	static int count; static string root;
	static void Check(string name, bool value) { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); count++; }
	static void Pump() { Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.DataBind, new Action(delegate { })); }
	const string Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation", Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

	static XmlElement Element(string file, string name)
	{
		var doc = new XmlDocument(); doc.Load(Path.Combine(root, "projects/CameraControl/CameraControl", file));
		var e = doc.GetElementsByTagName("*").Cast<XmlElement>().FirstOrDefault(x => x.GetAttribute("Name") == name || x.GetAttribute("Name", Xaml) == name);
		if (e == null) throw new Exception("missing element " + name); return e;
	}

	// The element inside a Grid that supplies only the converter its binding needs (app styles/icons are not loaded).
	static FrameworkElement Parse(XmlElement source, params string[] dropAttributes)
	{
		foreach (var a in dropAttributes) source.RemoveAttribute(a);
		foreach (var child in source.ChildNodes.Cast<XmlNode>().ToArray()) source.RemoveChild(child);
		string xml = "<Grid xmlns=\"" + Presentation + "\" xmlns:x=\"" + Xaml + "\" xmlns:Core=\"clr-namespace:CameraControl.Core;assembly=CameraControl.Core\" xmlns:T=\"clr-namespace:CameraControl.Core.Translation;assembly=CameraControl.Core\"><Grid.Resources><BooleanToVisibilityConverter x:Key=\"BooleanToVisibilityConverter1\" /><BooleanToVisibilityConverter x:Key=\"BooleanToVisibilityConverter\" /></Grid.Resources>"
			+ source.OuterXml.Replace(" xmlns=\"" + Presentation + "\"", "").Replace(" xmlns:x=\"" + Xaml + "\"", "") + "</Grid>";
		var grid = (Grid)XamlReader.Parse(xml); return (FrameworkElement)grid.Children[0];
	}

	[STAThread]
	static int Main(string[] args)
	{
		root = args[0]; string bin = args[1], core = args[2];
		AppDomain.CurrentDomain.AssemblyResolve += (s, e) => { string name = new AssemblyName(e.Name).Name + ".dll"; foreach (string dir in new[] { core, bin, Path.Combine(root, "dependencies/Managed") }) { string f = Path.Combine(dir, name); if (File.Exists(f)) return Assembly.LoadFrom(f); } return null; };
		try { Run(bin); Console.WriteLine(count + " UI binding checks passed"); return 0; }
		catch (Exception e) { Console.Error.WriteLine(e); return 1; }
	}

	static void Run(string bin)
	{
		ServiceProvider.Branding = (Branding)FormatterServices.GetUninitializedObject(typeof(Branding));
		var s = new Settings(); ServiceProvider.Settings = s;
		Check("오토사이징 is off by default, path empty", !s.UseAutoSizing && s.AutoSizingPath == "");

		var railXml = Element("MainWindow.xaml", "but_Ruler");
		Check("rail label is 오토사이징", railXml.GetElementsByTagName("TextBlock").Cast<XmlElement>().Any(t => t.GetAttribute("Text") == "오토사이징"));
		Check("rail visibility bound to Settings.UseAutoSizing (not Branding)", railXml.GetAttribute("Visibility").Contains("ServiceProvider.Settings") && railXml.GetAttribute("Visibility").Contains("Path=UseAutoSizing"));
		var rail = (Button)Parse(railXml, "Style", "Click");
		Pump(); Check("rail button hidden while disabled", rail.Visibility == Visibility.Collapsed);

		var switchXml = Element("windows/SettingsWnd.xaml", "G2Feat_autosizing");
		var toggle = (CheckBox)Parse(switchXml, "Style"); toggle.DataContext = s;
		Pump(); Check("settings switch shows OFF", toggle.IsChecked == false);
		toggle.IsChecked = true; Pump();
		Check("turning the switch ON enables 오토사이징", s.UseAutoSizing);
		Check("rail button appears", rail.Visibility == Visibility.Visible);
		toggle.IsChecked = false; Pump();
		Check("turning OFF hides the rail button again", !s.UseAutoSizing && rail.Visibility == Visibility.Collapsed);
		s.UseAutoSizing = true; Pump(); Check("setting change from code updates switch and rail", toggle.IsChecked == true && rail.Visibility == Visibility.Visible);

		var card = switchXml; while (card != null && card.GetAttribute("Style") != "{StaticResource G2Card}") card = card.ParentNode as XmlElement;
		Check("card has executable path row bound to AutoSizingPath with browse button", card != null && card.OuterXml.Contains("{Binding AutoSizingPath}") && card.OuterXml.Contains("Btn_AutoSizingPath_Click"));
		Check("measurement scale rows kept (MetaScale/DiskSize/calibration)", card.OuterXml.Contains("{Binding MetaScale}") && card.OuterXml.Contains("{Binding DiskSize}") && card.OuterXml.Contains("Btn_Calibration_Click"));

		s.AutoSizingPath = @"D:\Tools\CloriumAutoSizing\Clorium AutoSizing.exe";
		var json = JsonConvert.SerializeObject(s);
		var loaded = JsonConvert.DeserializeObject<Settings>(json);
		Check("JSON settings persist UseAutoSizing and path", loaded.UseAutoSizing && loaded.AutoSizingPath == s.AutoSizingPath);
		Check("older settings file without the keys stays OFF", !JsonConvert.DeserializeObject<Settings>("{}").UseAutoSizing);
		var xs = new System.Xml.Serialization.XmlSerializer(typeof(Settings)); var w = new StringWriter(); xs.Serialize(w, s);
		var x = (Settings)xs.Deserialize(new StringReader(w.ToString()));
		Check("XML settings persist UseAutoSizing and path", x.UseAutoSizing && x.AutoSizingPath == s.AutoSizingPath);

		string mainCs = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/MainWindow.xaml.cs"));
		Check("legacy launcher and exit kill removed from source", !mainCs.Contains("Program Files (x86)\\\\\\\\Clorium\\\\Clorium AutoSizing") && !mainCs.Contains("GetProcessesByName(\"Clorium AutoSizing\")"));
		BackgroundRemoval(s, mainCs);
		ImageAdjust(s, mainCs);
		Superfocus(s);
		SettingsAndCapture(s, bin);
		byte[] exe = File.ReadAllBytes(Path.Combine(bin, "CameraControl.exe"));
		Check("built CameraControl.exe has no ColorCompensationWnd, contains the 이미지보정 window and client", Find(exe, Encoding.UTF8.GetBytes("ColorCompensationWnd")) < 0 && Find(exe, Encoding.Unicode.GetBytes("Clorium_ColorCompensation.json")) < 0 && Find(exe, Encoding.UTF8.GetBytes("G2ColorAdjustWnd")) >= 0 && Find(exe, Encoding.UTF8.GetBytes("CloriumImageAdjust")) >= 0);
		Check("built CameraControl.exe has no PhotoScissors/PhotoRoom", Find(exe, Encoding.Unicode.GetBytes("PhotoScissors.exe")) < 0 && Find(exe, Encoding.Unicode.GetBytes("photoroom.com")) < 0 && Find(exe, Encoding.UTF8.GetBytes("RemoveBackgroundWnd")) < 0);
		Check("built CameraControl.exe contains the Matting Studio client", Find(exe, Encoding.UTF8.GetBytes("CloriumBackgroundRemoval")) >= 0 && Find(exe, Encoding.Unicode.GetBytes("http://127.0.0.1:8765")) >= 0);
		string bgr = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/Classes/CloriumBackgroundRemoval.cs"));
		Check("배경제거 opens G2's own window (no browser page)", Find(exe, Encoding.UTF8.GetBytes("G2BackgroundRemovalWnd")) >= 0 && bgr.Contains("G2BackgroundRemovalWnd.ShowFor(target)") && !bgr.Contains("OpenUrl") && !bgr.Contains("?mode=") && File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl.csproj")).Contains("windows/G2BackgroundRemovalWnd.xaml"));
		string settingsXaml = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/windows/SettingsWnd.xaml"));
		Check("촬영음: sound list bound to ShutterSound next to the switch, with 듣기", settingsXaml.Contains("ItemsSource=\"{Binding Source={x:Static Classes:ShutterSounds.All}}\" SelectedValue=\"{Binding ShutterSound}\"") && settingsXaml.Contains("Click=\"ShutterSoundPreview_Click\"") && new Settings().ShutterSound == 1 && JsonConvert.DeserializeObject<Settings>("{\"ShutterSound\":5}").ShutterSound == 5);
		string startup = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/StartUpWindow.xaml"));
		Check("시작 화면 is the logo animation (no logo image, closes after the intro)", startup.Contains("x:Key=\"Intro\"") && !startup.Contains("logo_big.png") && File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/App.xaml.cs")).Contains("_startUpWindow.CloseWhenReady()"));
		string live = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/windows/LiveViewWnd.xaml"));
		Check("라이브뷰 uses the G2 design (g2theme, G2 page/stage, no MaterialDesign paper or cards)", live.Contains("styles/g2theme.xaml") && live.Contains("Background=\"{DynamicResource G2Page}\"") && live.Contains("{DynamicResource G2Stage}") && !live.Contains("MaterialDesignPaper") && !live.Contains("materialDesign:Card"));
		Check("built CameraControl.exe has no legacy AutoSizing path", Find(exe, Encoding.Unicode.GetBytes("Clorium\\Clorium AutoSizing.exe")) < 0);
		Check("built CameraControl.exe contains the Desk client", Find(exe, Encoding.UTF8.GetBytes("CloriumAutoSizing")) >= 0 && Find(exe, Encoding.Unicode.GetBytes("CloriumAutoSizing.")) >= 0);
	}

	// 2026-11-03 배경제거: Matting Studio replaces PhotoScissors, the PhotoRoom window and the 배경 제거 촬영 mode.
	static void BackgroundRemoval(Settings s, string mainCs)
	{
		Check("배경제거 is off by default, script path empty", !new Settings().UseBackgroundRemoval && new Settings().BackgroundRemovalPath == "");
		s.UseBackgroundRemoval = false;
		var railXml = Element("MainWindow.xaml", "but_AIRemoveBg");
		Check("rail label is 배경제거 and bound to Settings.UseBackgroundRemoval", railXml.GetElementsByTagName("TextBlock").Cast<XmlElement>().Any(t => t.GetAttribute("Text") == "배경제거") && railXml.GetAttribute("Visibility").Contains("ServiceProvider.Settings") && railXml.GetAttribute("Visibility").Contains("Path=UseBackgroundRemoval"));
		var rail = (Button)Parse(railXml, "Style", "Click");
		var switchXml = Element("windows/SettingsWnd.xaml", "G2Feat_removebg");
		var toggle = (CheckBox)Parse(switchXml, "Style"); toggle.DataContext = s;
		Pump(); Check("배경제거 rail hidden and switch OFF while disabled", rail.Visibility == Visibility.Collapsed && toggle.IsChecked == false);
		toggle.IsChecked = true; Pump(); Check("switch ON shows the 배경제거 rail button", s.UseBackgroundRemoval && rail.Visibility == Visibility.Visible);
		toggle.IsChecked = false; Pump(); Check("switch OFF hides it again", !s.UseBackgroundRemoval && rail.Visibility == Visibility.Collapsed);
		var card = switchXml; while (card != null && card.GetAttribute("Style") != "{StaticResource G2Card}") card = card.ParentNode as XmlElement;
		Check("배경제거 card: script path row with browse, no legacy capture options", card.OuterXml.Contains("{Binding BackgroundRemovalPath}") && card.OuterXml.Contains("Btn_BackgroundRemovalPath_Click") && !card.OuterXml.Contains("ModeBgPng") && !card.OuterXml.Contains("BGTargetColor") && !card.OuterXml.Contains("ControlRemoveBackground"));
		s.BackgroundRemovalPath = @"D:\Tools\Matting\outputs\matting-studio\Start.ps1"; s.UseBackgroundRemoval = true;
		var loaded = JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(s));
		Check("JSON settings persist UseBackgroundRemoval and path", loaded.UseBackgroundRemoval && loaded.BackgroundRemovalPath == s.BackgroundRemovalPath);
		var legacy = JsonConvert.DeserializeObject<Settings>("{\"ShowRemoveBg\":true,\"ModeBgPng\":true}");
		Check("older settings with the removed capture mode still load, feature OFF", legacy != null && !legacy.UseBackgroundRemoval);
		string mainXaml = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/MainWindow.xaml"));
		Check("배경 제거 촬영 panel and its controls removed", !mainXaml.Contains("Name=\"RemoveBg\"") && !mainXaml.Contains("But_FrontCaptureBG_Click") && !mainXaml.Contains("Name=\"ModeA\"") && !mainXaml.Contains("Bg_Auto") && !mainXaml.Contains("Path=ShowRemoveBg"));
		Check("normal F/B panel no longer hidden by the old mode", mainXaml.Contains("<!-- capture face: F / B / D (normal mode) -->"));
		Check("legacy capture/launch code removed", !mainCs.Contains("CaptureBackground") && !mainCs.Contains("RemoveBackgroundWnd") && !mainCs.Contains("removeBG") && !mainCs.Contains("\"PhotoScissors\"") && !mainCs.Contains("SwitchToRemoveBg"));
		string csproj = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl.csproj"));
		Check("old windows removed from the project", !csproj.Contains("RemoveBackgroundWnd") && !File.Exists(Path.Combine(root, "projects/CameraControl/CameraControl/ViewModel/AIRemoveBackgroundViewModel.cs")));
		string presets = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/windows/SettingsWnd.G2.cs"));
		Check("presets copy UseBackgroundRemoval, not the removed options", presets.Contains("\"UseBackgroundRemoval\"") && !presets.Contains("\"ShowRemoveBg\"") && !presets.Contains("\"ModeBgPng\""));
	}

	// 2026-11-03 이미지보정: the G2ColorAdjustWnd rail button is switched by 클로리움 설정; ColorCompensationWnd is removed.
	static void ImageAdjust(Settings s, string mainCs)
	{
		Check("이미지보정 is off by default", !new Settings().UseImageAdjust);
		s.UseImageAdjust = false;
		var railXml = Element("MainWindow.xaml", "but_ImageAdjust");
		Check("rail label is 이미지보정 and bound to Settings.UseImageAdjust", railXml.GetElementsByTagName("TextBlock").Cast<XmlElement>().Any(t => t.GetAttribute("Text") == "이미지보정") && railXml.GetAttribute("Visibility").Contains("ServiceProvider.Settings") && railXml.GetAttribute("Visibility").Contains("Path=UseImageAdjust") && railXml.GetAttribute("Click") == "but_ImageAdjust_Click");
		var rail = (Button)Parse(railXml, "Style", "Click");
		var switchXml = Element("windows/SettingsWnd.xaml", "G2Feat_imageadjust");
		var toggle = (CheckBox)Parse(switchXml, "Style"); toggle.DataContext = s;
		Pump(); Check("이미지보정 rail hidden and switch OFF while disabled", rail.Visibility == Visibility.Collapsed && toggle.IsChecked == false);
		toggle.IsChecked = true; Pump(); Check("switch ON shows the 이미지보정 rail button", s.UseImageAdjust && rail.Visibility == Visibility.Visible);
		toggle.IsChecked = false; Pump(); Check("switch OFF hides it again", !s.UseImageAdjust && rail.Visibility == Visibility.Collapsed);
		s.UseImageAdjust = true; Pump(); Check("이미지보정 set from code updates switch and rail", toggle.IsChecked == true && rail.Visibility == Visibility.Visible);
		var card = switchXml; while (card != null && card.GetAttribute("Style") != "{StaticResource G2Card}") card = card.ParentNode as XmlElement;
		Check("이미지보정 card has the template folder row", card != null && card.OuterXml.Contains("Btn_ImageAdjustTemplates_Click") && card.OuterXml.Contains("color-templates.xml"));
		var loaded = JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(s));
		var xs = new System.Xml.Serialization.XmlSerializer(typeof(Settings)); var w = new StringWriter(); xs.Serialize(w, s);
		Check("JSON and XML settings persist UseImageAdjust", loaded.UseImageAdjust && ((Settings)xs.Deserialize(new StringReader(w.ToString()))).UseImageAdjust);
		Check("older settings file without the key stays OFF", !JsonConvert.DeserializeObject<Settings>("{}").UseImageAdjust);
		Check("rail handler opens through CloriumImageAdjust, legacy handler gone", mainCs.Contains("CloriumImageAdjust.Open(") && !mainCs.Contains("but_ColorCompensation_Click") && !mainCs.Contains("ColorCompensationWnd"));
		string layouts = "";
		foreach (string f in new[] { "LayoutGrid.xaml", "LayoutGridRight.xaml", "LayoutNormal.xaml", "LayoutBase.cs" }) layouts += File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/Layouts", f));
		Check("thumbnail menu item and handler for the old window removed", !layouts.Contains("ViewColorCompensation") && !layouts.Contains("ColorCompensationWnd"));
		string csproj = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl.csproj"));
		string cc = Path.Combine(root, "projects/CameraControl/CameraControl");
		Check("old window and view model removed from the project", !csproj.Contains("ColorCompensationWnd") && !File.Exists(Path.Combine(cc, "windows/ColorCompensationWnd.xaml")) && !File.Exists(Path.Combine(cc, "ViewModel/ColorCompensationViewModel.cs")) && csproj.Contains("G2ColorAdjustWnd.xaml"));
		Check("presets copy UseImageAdjust", File.ReadAllText(Path.Combine(cc, "windows/SettingsWnd.G2.cs")).Contains("\"UseImageAdjust\""));
	}

	// 2026-11-03 수퍼포커스: default off like the other G2 features; the legacy focus stacking panels are removed.
	static void Superfocus(Settings s)
	{
		Check("수퍼포커스 is off by default; older settings without the key stay off", !new Settings().UseSuperfocus && !JsonConvert.DeserializeObject<Settings>("{}").UseSuperfocus);
		Check("settings saved with 수퍼포커스 ON keep it ON", JsonConvert.DeserializeObject<Settings>("{\"UseSuperfocus\":true}").UseSuperfocus);
		s.UseSuperfocus = false;
		var railXml = Element("MainWindow.xaml", "but_Superfocus");
		var rail = (Button)Parse(railXml, "Style", "Click");
		var launcher = (Button)Parse(Element("windows/LiveViewWnd.xaml", "SuperfocusLauncher"), "Style", "Click");
		var toggle = (CheckBox)Parse(Element("windows/SettingsWnd.xaml", "G2Feat_superfocus"), "Style"); toggle.DataContext = s;
		Pump(); Check("수퍼포커스 rail and live view launcher hidden while disabled", rail.Visibility == Visibility.Collapsed && launcher.Visibility == Visibility.Collapsed && toggle.IsChecked == false);
		toggle.IsChecked = true; Pump(); Check("switch ON shows the rail button and the live view launcher", s.UseSuperfocus && rail.Visibility == Visibility.Visible && launcher.Visibility == Visibility.Visible);
		toggle.IsChecked = false; Pump(); Check("switch OFF hides both again", !s.UseSuperfocus && rail.Visibility == Visibility.Collapsed && launcher.Visibility == Visibility.Collapsed);
		string live = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/windows/LiveViewWnd.xaml"));
		string vm = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/ViewModel/LiveViewViewModel.cs"));
		Check("legacy focus stacking panels and A/B jump buttons removed", !live.Contains("ExpanderFocusStacking") && !live.Contains("MoveACommand") && !live.Contains("FocusStackingCommand") && !live.Contains("FocusControl_Click"));
		Check("legacy focus stacking code removed from the live view model", !vm.Contains("FocusStacking") && !vm.Contains("_focusStackinMode") && !vm.Contains("PhotoCount") && vm.Contains("LockCurrentNearCommand"));
	}

	static XmlElement Ancestor(XmlElement e, string tag) { var p = e.ParentNode as XmlElement; while (p != null && p.LocalName != tag) p = p.ParentNode as XmlElement; return p; }

	// 2026-11-03 settings/capture requests: 일반 first, 라이선스 menu, light/dark theme, backup folder, square-board
	// calibration rows, barcode card under 장치 · 연결, one-press 하자 button, single underscores in file names.
	static void SettingsAndCapture(Settings s, string bin)
	{
		var doc = new XmlDocument(); doc.Load(Path.Combine(root, "projects/CameraControl/CameraControl/windows/SettingsWnd.xaml"));
		var tabs = doc.GetElementsByTagName("TabControl").Cast<XmlElement>().First(t => t.GetAttribute("Name", Xaml) == "G2Tabs");
		var firstTab = tabs.GetElementsByTagName("TabItem").Cast<XmlElement>().First();
		Check("클로리움 설정 opens on 일반 (first tab)", tabs.GetAttribute("SelectedIndex") == "0" && firstTab.GetAttribute("Tag") == "일반");
		var barcode = Element("windows/SettingsWnd.xaml", "G2Feat_barcode");
		Check("바코드 연동 card moved to 장치 · 연결", Ancestor(barcode, "TabItem").GetAttribute("Tag") == "장치 · 연결");

		string xaml = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/windows/SettingsWnd.xaml"));
		Check("theme: 밝은 화면 / 어두운 화면 only, old colour list removed", xaml.Contains("SelectedIndex=\"{Binding ScreenTheme}\"><ComboBoxItem Content=\"밝은 화면\" /><ComboBoxItem Content=\"어두운 화면\" />") && !xaml.Contains("{Binding Themes}") && !xaml.Contains("CurrentThemeNameNew"));
		Check("어두운 화면 is off by default", !new Settings().UseDarkScreen && new Settings().ScreenTheme == 0);
		s.ScreenTheme = 1;
		var dark = JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(s));
		Check("어두운 화면 choice persists (UseDarkScreen)", s.UseDarkScreen && dark.UseDarkScreen && dark.ScreenTheme == 1);
		s.ScreenTheme = 0;

		int backupAt = xaml.IndexOf("{Binding BackupFolder}");
		Check("백업 폴더 row (path + folder button) shown only while 항상 원본 백업 is on", backupAt > 0 && xaml.LastIndexOf("<StackPanel Visibility=\"{Binding UseAlwaysBackup", backupAt) > xaml.LastIndexOf("Tag=\"항상 원본 백업 \"", backupAt) && xaml.IndexOf("Btn_BackupFolder_Click", backupAt) > 0);
		s.BackupFolder = @"E:\Clorium Backup";
		Check("백업 폴더 persists (JSON), empty by default", JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(s)).BackupFolder == @"E:\Clorium Backup" && new Settings().BackupFolder == "");

		Check("calibration board selector (원판 / 정사각형 판) bound to CalibrationTarget", xaml.Contains("SelectedIndex=\"{Binding CalibrationTarget}\"><ComboBoxItem Content=\"원판\" /><ComboBoxItem Content=\"정사각형 판\" />"));
		Check("disk size row only for 원판, board size + result only for 정사각형 판", Regex.IsMatch(xaml, "Tag=\"기준 원판 크기 \">.*?DataTrigger Binding=\"\\{Binding CalibrationTarget\\}\" Value=\"0\"") && Regex.IsMatch(xaml, "Tag=\"정사각형 판 크기 \">.*?DataTrigger Binding=\"\\{Binding CalibrationTarget\\}\" Value=\"1\".*?\\{Binding SquareBoardSize\\}.*?\\{Binding SquareCalibrationSummary\\}", RegexOptions.Singleline));
		Check("square board defaults: 원판 selected, 500 mm, not calibrated", new Settings().CalibrationTarget == 0 && new Settings().SquareBoardSize == 500 && new Settings().SquareCalibration == "" && new Settings().SquareCalibrationSummary.StartsWith("아직"));

		string main = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/MainWindow.xaml"));
		string mainCs = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/MainWindow.xaml.cs"));
		Check("설정 menu: 라이선스 right under 클로리움 설정", Regex.IsMatch(main, "<MenuItem Header=\"클로리움 설정\"[^\\n]*\\n\\s*<MenuItem Header=\"라이선스\" Click=\"OpenLicense_Click\" />") && mainCs.Contains("new G2LicenseWnd"));
		var defect = Element("MainWindow.xaml", "but_DefectCapture");
		Check("하자 button: a capture button shown by 하자 촬영 setting, toggle removed", defect.LocalName == "Button" && defect.GetAttribute("Click") == "But_DefectCapture_Click" && defect.GetAttribute("Visibility").Contains("Path=UseDefectCapture") && !main.Contains("Check_Defect"));
		Check("하자 capture sets _D for one shot and clears it when nothing was captured", Regex.IsMatch(mainCs, "IsDefect = true;.*?if \\(!await CaptureAsync\\(shouldWait: true\\)\\)\\s*\\{[^}]*IsDefect = false;", RegexOptions.Singleline));
		Check("square calibration written to the measurement copy only (WriteMetadata XPKeywords tag)", Regex.IsMatch(mainCs, "metadata2.Comment = host;.*?CalibrationTarget == 1.*?metadata2.SetQuery\\(SquareBoardCalibration.MetadataQuery", RegexOptions.Singleline));

		Assembly core = typeof(Settings).Assembly;
		var tidy = core.GetType("CameraControl.Core.Classes.PhotoSession").GetMethod("TidyUnderscores", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
		Func<string, string> T = n => (string)tidy.Invoke(null, new object[] { n });
		Check("file names: C1_360photo__011_F -> C1_360photo_011_F", T("C1_360photo__011_F") == "C1_360photo_011_F");
		Check("file names: _360photo__001 -> 360photo_001, sub folders kept", T("_360photo__001") == "360photo_001" && T(@"sub\__a__b_D") == @"sub\a_b_D" && T("test_001_PR_F") == "test_001_PR_F");
		Check("Core has the square-board calibration", core.GetType("CameraControl.Core.Classes.SquareBoardCalibration") != null);

		Assembly g2 = Assembly.LoadFrom(Path.Combine(bin, "CameraControl.exe"));
		var palette = g2.GetType("CameraControl.Classes.G2Palette", true);
		var darkProp = palette.GetProperty("Dark");
		Func<string, ResourceDictionary> make = n => (ResourceDictionary)Activator.CreateInstance(g2.GetType("CameraControl.Classes." + n, true));
		darkProp.SetValue(null, false);
		var light = make("G2Palette"); var lightMd = make("G2MaterialOverrides");
		darkProp.SetValue(null, true);
		var darkP = make("G2Palette"); var darkMd = make("G2MaterialOverrides");
		darkProp.SetValue(null, false);
		Func<ResourceDictionary, string, string> C = (d, k) => ((SolidColorBrush)d[k]).Color.ToString();
		Check("palette 밝은 화면 keeps the design-A colours", C(light, "G2Page") == "#FFF3F4F6" && C(light, "G2Panel") == "#FFFFFFFF" && C(light, "G2Text") == "#FF16181C" && C(light, "G2Accent") == "#FF1F5FD6");
		Check("palette 어두운 화면: dark page/panel, light text", C(darkP, "G2Page") == "#FF101113" && C(darkP, "G2Panel") == "#FF1B1D21" && C(darkP, "G2Text") == "#FFF1F3F5" && C(darkP, "G2OnAccent") == "#FFFFFFFF");
		Check("legacy MaterialDesign/MahApps brushes follow the palette", C(lightMd, "MaterialDesignPaper") == "#FFF3F4F6" && C(darkMd, "MaterialDesignPaper") == "#FF101113" && C(darkMd, "MahApps.Brushes.Text") == "#FFF1F3F5");
		string theme = File.ReadAllText(Path.Combine(root, "projects/CameraControl/styles/g2theme.xaml"));
		Check("g2theme takes all colour tokens from G2Palette", theme.Contains("<cls:G2Palette />") && !theme.Contains("<SolidColorBrush x:Key=\"G2Page\""));
		int literal = 0;
		foreach (string f in new[] { "CameraControl/MainWindow.xaml", "CameraControl/windows/SettingsWnd.xaml", "CameraControl/windows/G2LicenseWnd.xaml" })
			literal += Regex.Matches(File.ReadAllText(Path.Combine(root, "projects/CameraControl", f)), "(Background|Foreground|BorderBrush)=\"#FF(FFFFFF|F3F4F6|16181C|E3ECFB|FFF1DF|EEF0F3)\"").Count;
		Check("no pinned light colours left on G2 window surfaces", literal == 0);

		// 테마 바로 전환: colour tokens are DynamicResource and Apply() refreshes palettes that already exist.
		int staticTokens = 0;
		foreach (string f in new[] { "CameraControl/MainWindow.xaml", "CameraControl/windows/SettingsWnd.xaml", "CameraControl/windows/G2LicenseWnd.xaml", "CameraControl/windows/G2ColorAdjustWnd.xaml", "CameraControl/Layouts/LayoutNormal.xaml", "CameraControl/Controls/Controler.xaml", "styles/g2theme.xaml" })
			staticTokens += Regex.Matches(File.ReadAllText(Path.Combine(root, "projects/CameraControl", f)), "StaticResource G2(Page|Panel2?|Hover|Line2?|Stage|Overlay|Text|Sub|Accent(Hover|Soft)?|Pr(Bg|Text)?|Ok(Bg)?|Err|SwitchOff|Neutral(Text)?|Thumb|OnAccent)\\}").Count;
		Check("G2 colour tokens are DynamicResource everywhere (" + staticTokens + " static left)", staticTokens == 0);
		var apply = palette.GetMethod("Apply");
		var existing = make("G2Palette"); var existingMd = make("G2MaterialOverrides");
		apply.Invoke(null, new object[] { true });
		Check("Apply(dark) switches palettes that are already open", C(existing, "G2Page") == "#FF101113" && C(existing, "G2Text") == "#FFF1F3F5" && C(existingMd, "MaterialDesignPaper") == "#FF101113");
		apply.Invoke(null, new object[] { false });
		Check("Apply(light) switches them back", C(existing, "G2Page") == "#FFF3F4F6" && C(existingMd, "MaterialDesignPaper") == "#FFF3F4F6");
		string settingsCs = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/windows/SettingsWnd.xaml.cs"));
		Check("theme choice applies at once (no restart chip) and on close", !Regex.IsMatch(xamlOf("windows/SettingsWnd.xaml"), "Tag=\"테마 [^\"]*\">(?:(?!</Border>).)*재시작", RegexOptions.Singleline) && settingsCs.Contains("e.PropertyName == \"UseDarkScreen\"") && settingsCs.Contains("G2Palette.ApplyScreenTheme()"));
		string lightCs = File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl/Classes/CloriumLightControl.cs"));
		Check("light app gets the theme (start, open, setting change)", lightCs.Contains("\\\"command\\\":\\\"theme\\\",\\\"dark\\\":") && Regex.Matches(lightCs, "ApplyTheme\\(ServiceProvider.Settings.UseDarkScreen\\)").Count >= 3);
	}

	static string xamlOf(string rel) { return File.ReadAllText(Path.Combine(root, "projects/CameraControl/CameraControl", rel)); }

	static int Find(byte[] data, byte[] pattern)
	{
		for (int i = 0; i <= data.Length - pattern.Length; i++) { int j = 0; while (j < pattern.Length && data[i + j] == pattern[j]) j++; if (j == pattern.Length) return i; }
		return -1;
	}
}
