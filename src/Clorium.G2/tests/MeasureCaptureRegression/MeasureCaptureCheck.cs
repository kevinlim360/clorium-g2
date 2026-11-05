// 치수 촬영 naming and UiText language checks against the built CameraControl.Core / CameraControl.Devices (net48).
// Run-MeasureCaptureCheck.ps1 compiles this with the .NET Framework csc next to copies of the two DLLs.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using CameraControl.Core;
using CameraControl.Core.Classes;
using CameraControl.Devices;

static class MeasureCaptureCheck
{
	static int passed;
	static void Check(string name, bool ok) { if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }

	static PhotoSession Session(string folder, string template)
	{
		var session = (PhotoSession)FormatterServices.GetUninitializedObject(typeof(PhotoSession));
		typeof(PhotoSession).GetField("_locker", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(session, new object());
		session.RawExtensions = new List<string> { ".cr2", ".nef" };
		session.Folder = folder;
		session.FileNameTemplate = template;
		return session;
	}

	// args[0]: a G2 runtime folder for the third-party DLLs (log4net, ...) that the build output does not contain.
	[STAThread]
	static int Main(string[] args)
	{
		AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => { string file = Path.Combine(args[0], new AssemblyName(e.Name).Name + ".dll"); return File.Exists(file) ? Assembly.LoadFrom(file) : null; };
		return Run();
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	static int Run()
	{
		ServiceProvider.Settings = (Settings)FormatterServices.GetUninitializedObject(typeof(Settings));
		ServiceProvider.Branding = (Branding)FormatterServices.GetUninitializedObject(typeof(Branding));
		var s = ServiceProvider.Settings;
		s.CameraProperties = new CameraPropertyEnumerator();
		s.CloriumID = "M1_";
		ServiceProvider.FilenameTemplateManager = new FilenameTemplateManager();
		var camera = new BaseCameraDevice { SerialNumber = "TEST-ONLY" };
		string temp = Path.Combine(Path.GetTempPath(), "CloriumMeasure-" + Guid.NewGuid().ToString("N"));
		try
		{
			// Settings defaults and the 앞면 rule.
			var fresh = new Settings();
			Check("치수 촬영 off by default", !fresh.UseMeasureCapture);
			Check("치수자동저장 on by default", fresh.MeasureAutoSave);
			Check("off: 앞면 saves the measurement copy (as before)", fresh.FrontSavesMeasure);
			fresh.UseMeasureCapture = true; fresh.MeasureAutoSave = false;
			Check("on + unchecked: 앞면 does not save it", !fresh.FrontSavesMeasure);
			fresh.MeasureAutoSave = true;
			Check("on + checked: 앞면 saves it", fresh.FrontSavesMeasure);

			string folder = Path.Combine(temp, "M1_12345678");
			Directory.CreateDirectory(folder);
			s.IsDefect = false; s.Background = false; s.FrontBackMode = 1;

			// Template with a counter: 치수 before 앞면 takes the next 앞면 name; after 앞면 it takes that file.
			var session = Session(folder, "Barcode_[Counter 3 digit]");
			int counter = session.Counter, cameraCounter = s.CameraProperties.Get(camera).Counter;
			string before = session.MeasureBaseFileName("IMG.jpg", camera, "");
			Check("치수 does not advance the counter", session.Counter == counter && s.CameraProperties.Get(camera).Counter == cameraCounter);
			string front = session.GetNextFileName("IMG.jpg", camera, "");
			Check("치수 before 앞면 pairs with the next 앞면 (" + Path.GetFileName(front) + ")", string.Equals(before, front, StringComparison.OrdinalIgnoreCase));
			File.WriteAllText(front, "front");
			string after = session.MeasureBaseFileName("IMG.jpg", camera, "");
			Check("치수 after 앞면 pairs with that 앞면", string.Equals(after, front, StringComparison.OrdinalIgnoreCase));
			Check("치수 name ends with _F", Path.GetFileNameWithoutExtension(after).EndsWith("_F"));

			// Template without a counter: always the item's 앞면 name.
			var plain = Session(Path.Combine(temp, "M1_ABC"), "");
			string p = plain.MeasureBaseFileName("IMG.jpg", camera, "");
			Check("no counter: " + Path.GetFileName(p), Path.GetFileName(p) == "M1_ABC_F.jpg");

			// UiText: Korean / English by language, fallbacks.
			string xml = Path.Combine(temp, "Messages.xml");
			File.WriteAllText(xml, "<?xml version=\"1.0\" encoding=\"utf-8\"?><messages><m id=\"M0001\"><ko>새 한글 {0}</ko><en>New English {0}</en></m><m id=\"M0002\"><ko>한글만</ko><en></en></m></messages>");
			UiText.FilePath = xml;
			UiText.Language = "ko";
			Check("ko -> 새 한글", UiText.T("M0001", "옛 문구 {0}") == "새 한글 {0}");
			UiText.Language = "en_US";
			Check("en_US -> English", UiText.T("M0001", "옛 문구 {0}") == "New English {0}");
			Check("empty English -> Korean argument", UiText.T("M0002", "기본") == "기본");
			Check("unknown id -> Korean argument", UiText.T("M9999", "기본") == "기본");
			UiText.FilePath = Path.Combine(temp, "missing.xml");
			Check("missing file -> Korean argument", UiText.T("M0001", "기본") == "기본");
			UiText.Language = null;
			Check("no language -> Korean", UiText.IsKorean);
			Console.WriteLine("MEASURE_CAPTURE_PASSED=" + passed);
			return 0;
		}
		finally
		{
			if (Directory.Exists(temp)) Directory.Delete(temp, true);
		}
	}
}
