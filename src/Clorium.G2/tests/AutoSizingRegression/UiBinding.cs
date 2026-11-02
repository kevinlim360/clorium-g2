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
		byte[] exe = File.ReadAllBytes(Path.Combine(bin, "CameraControl.exe"));
		Check("built CameraControl.exe has no legacy AutoSizing path", Find(exe, Encoding.Unicode.GetBytes("Clorium\\Clorium AutoSizing.exe")) < 0);
		Check("built CameraControl.exe contains the Desk client", Find(exe, Encoding.UTF8.GetBytes("CloriumAutoSizing")) >= 0 && Find(exe, Encoding.Unicode.GetBytes("CloriumAutoSizing.")) >= 0);
	}

	static int Find(byte[] data, byte[] pattern)
	{
		for (int i = 0; i <= data.Length - pattern.Length; i++) { int j = 0; while (j < pattern.Length && data[i + j] == pattern[j]) j++; if (j == pattern.Length) return i; }
		return -1;
	}
}
