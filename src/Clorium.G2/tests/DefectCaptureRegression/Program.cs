using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PhotoSession = Session;
namespace System.Windows {
 public class Window {}
 public enum MessageBoxButton { OK } public enum MessageBoxImage { Exclamation }
 public static class MessageBox { public static void Show(string x) { } public static void Show(string x,string y,MessageBoxButton b,MessageBoxImage i) { } }
 public class Application { public static Application Current=new(); public Dispatch Dispatcher=new(); }
 public class Dispatch { public void Invoke(Action a)=>a(); }
}
public interface ICameraDevice { bool IsConnected {get;set;} bool IsBusy {get;set;} void CapturePhoto(); }
public class FakeCamera:ICameraDevice { public bool IsConnected {get;set;}=true;public bool IsBusy {get;set;} public void CapturePhoto(){Program.DeviceCapture();} }
public class CameraProperty { public int Counter,CounterInc=1,Delay; public bool UseExternalShutter;public object SelectedConfig; }
public class Props { public CameraProperty P=new();public CameraProperty Get(ICameraDevice d)=>P; }
public class Settings { public bool IsAestheticImprovementActive; public bool isBtnPressed,GetMeasurePicture,IsDefect,Background,TurnTableShot,ModeLighting;public bool ThumbailLoaded=true;public int FrontBackMode,LightPowerSaveTime=99999,ExternalDeviceWaitForFocus,ExternalDeviceWaitForCapture;public string CloriumID="M1_",CameraAlias="",TurnTableShot_PathName="",TurnTableShot_FileName="";public Props CameraProperties=new(); public bool UseDebugLog;public string DownloadFolder=Program.ProductRoot;public static string SessionFolder=>Path.Combine(Program.TempBase,"config");public Session DefaultSession;public int TodaySessionCounter,TotalSessionCounter,AddedSessions,LoadedSessions;public Session LoadSession(string f){LoadedSessions++;return new Session{Counter=7};}public void Add(Session s){AddedSessions++;} }
public class PluginManager {public object SelectedWindow;}
public class EditSession {public Session Session;public object Owner;public EditSession(Session s){Session=s;}}
public class Branding {public bool ControlMultiCamera,ControlLighting;}
public class Devices { public ICameraDevice SelectedCameraDevice=new FakeCamera();public Dictionary<ICameraDevice,string> LastCapturedImage=new(); }
public class Windows {public void ExecuteCommand(string c,bool b){} }
public class External {public void AssertFocus(object c){}public void OpenShutter(object c){}public void CloseShutter(object c){} }
public class Analytics {public void CameraCapture(ICameraDevice d){} }
public interface Template {string Pharse(string m,Session s,ICameraDevice d,string f,string t);}
public class CounterTemplate:Template {public string Pharse(string m,Session s,ICameraDevice d,string f,string t)=>s.Counter.ToString("000");}
public class TemplateRegistry {public Dictionary<string,Template> TemplatesMap=new(){{"[Counter 3 digit]",new CounterTemplate()}};public Dictionary<string,Template> Templates=>TemplatesMap;}
public static class ServiceProvider {public static PluginManager PluginManager=new();public static Settings Settings=new();public static Branding Branding=new();public static Devices DeviceManager=new();public static Windows WindowsManager=new();public static TemplateRegistry FilenameTemplateManager=new();public static External ExternalDeviceManager=new();public static Analytics Analytics=new();}
public class StaticHelper {public static StaticHelper Instance=new();public string SystemMessage="";public static string GetUniqueFilename(string prefix,int count,string ext){while(File.Exists(prefix+count+ext))count++;return prefix+count+ext;}}
public static class Log {public static void Debug(string s,bool verbose=false){}public static void Error(string s,Exception e){} }
public class LightControlWnd {public static LightControlWnd Instance=new();public void AllLightOn(){} }
public class MainMenuViewModel {public static bool IsMusinsaFrontOK,IsMusinsaBackOK;
private bool CanStartNewCaptureSession()
	{
		if (ServiceProvider.Settings.isBtnPressed || !ServiceProvider.Settings.ThumbailLoaded)
		{
			MessageBox.Show("촬영 이미지의 저장과 썸네일 처리가 끝난 후 다음 제품으로 이동하세요.");
			return false;
		}
		return true;
	}
private static void ResetCapturePurposeForNewSession()
	{
		// A retry keeps its capture purpose. A successfully created product session does not.
		ServiceProvider.Settings.IsDefect = false;
		ServiceProvider.Settings.FrontBackMode = 0;
		ServiceProvider.Settings.GetMeasurePicture = false;
		ServiceProvider.Settings.Background = false;
	}
public string CreateNewSessionFolder(string barcord)
	{
		if (!CanStartNewCaptureSession()) return null;
		string defaultsessionfile = Path.Combine(Settings.SessionFolder, "Default.json");
		PhotoSession session = new PhotoSession();
		if (File.Exists(defaultsessionfile))
		{
			session = ServiceProvider.Settings.LoadSession(defaultsessionfile);
			session.Files.Clear();
		}
		EditSession editSession = new EditSession(session);
		editSession.Owner = ServiceProvider.PluginManager.SelectedWindow as Window;
		string year = DateTime.Now.ToString("yyyy");
		string month = DateTime.Now.ToString("MM");
		string day = DateTime.Now.ToString("dd");
		editSession.Session.Name = ServiceProvider.Settings.CloriumID + barcord;
		editSession.Session.Folder = Path.Combine(ServiceProvider.Settings.DownloadFolder, year.ToString() + "년" + month.ToString() + "월\\", day.ToString() + "일\\", editSession.Session.Name);
		string orginalFolder = editSession.Session.Folder;
		for (int i = 1; i < 100; i++)
		{
			if (!Directory.Exists(editSession.Session.Folder))
			{
				Directory.CreateDirectory(editSession.Session.Folder);
				break;
			}
			editSession.Session.Folder = orginalFolder + "___" + i;
			Log.Debug("세션 중복 : " + i, ServiceProvider.Settings.UseDebugLog);
		}
		ServiceProvider.Settings.Add(editSession.Session);
		ServiceProvider.Settings.DefaultSession = editSession.Session;
		ResetCapturePurposeForNewSession();
		ServiceProvider.Settings.TodaySessionCounter++;
		ServiceProvider.Settings.TotalSessionCounter++;
		return editSession.Session.Folder;
	}
}
public class Session {public string Name="";public List<object> Files=new();public string Folder=Program.TempFolder;public string FileNameTemplate="Barcode_[Counter 3 digit]";public int Counter;readonly object _locker=new();string _lastFilename;static List<string> RawExtensions=new(){".nef",".cr2",".cr3"};public bool AllowOverWrite;
 public string FormatOnly()=>FormatFileName(ServiceProvider.DeviceManager.SelectedCameraDevice,"IMG.JPG","temp")+".jpg";
 private string FormatFileName(ICameraDevice device, string file, string tempfile, bool incremetCounter = true)
	{
		CameraProperty property = ServiceProvider.Settings.CameraProperties.Get(device);
		if (incremetCounter)
		{
			property.Counter += property.CounterInc;
			Counter++;
		}
		string res = FileNameTemplate.Replace("Barcode", "");
		Regex regPattern = new Regex("\\[(.*?)\\]", RegexOptions.Singleline);
		MatchCollection matchX = regPattern.Matches(res);
		foreach (Match match in matchX)
		{
			if (ServiceProvider.FilenameTemplateManager.Templates.ContainsKey(match.Value))
			{
				res = res.Replace(match.Value, ServiceProvider.FilenameTemplateManager.Templates[match.Value].Pharse(match.Value, this, device, file, tempfile));
			}
		}
		while (res.Contains("\\\\"))
		{
			res = res.Replace("\\\\", "\\");
		}
		if (res.StartsWith("\\"))
		{
			res = res.Substring(1);
		}
		string CloriumID = ServiceProvider.Settings.CloriumID.Replace("_", "");
		string lastFolderName = Path.GetFileName(Folder.TrimEnd(Path.DirectorySeparatorChar));
		int index = lastFolderName.IndexOf('_');
		string barcode = ((index > 0) ? lastFolderName.Substring(index + 1) : "");
		string temp = ((!ServiceProvider.Branding.ControlMultiCamera) ? lastFolderName : (CloriumID + ServiceProvider.Settings.CameraAlias + "_" + barcode));
		res = temp + res;
		if (ServiceProvider.Settings.TurnTableShot)
		{
			string pathname = "";
			return (!ServiceProvider.Branding.ControlMultiCamera) ? (ServiceProvider.Settings.TurnTableShot_PathName + "\\" + CloriumID + ServiceProvider.Settings.TurnTableShot_FileName) : (ServiceProvider.Settings.TurnTableShot_PathName + "\\" + CloriumID + ServiceProvider.Settings.CameraAlias + "_" + ServiceProvider.Settings.TurnTableShot_FileName);
		}
		// Defect capture takes precedence over a stale front/back selection.
		if (ServiceProvider.Settings.IsDefect)
		{
			res += "_D";
		}
		else if (ServiceProvider.Settings.FrontBackMode == 1)
		{
			res += ServiceProvider.Settings.IsAestheticImprovementActive ? "_PR_F" : "_F";
		}
		else if (ServiceProvider.Settings.FrontBackMode == 2)
		{
			res += ServiceProvider.Settings.IsAestheticImprovementActive ? "_PR_B" : "_B";
		}
		else if (ServiceProvider.Settings.Background)
		{
			res += "_BG";
		}
		return res;
	}
 public string GetNextFileName(string file, ICameraDevice device, string tempfile)
	{
		lock (_locker)
		{
			string ext = Path.GetExtension(file);
			if (string.IsNullOrEmpty(ext))
			{
				ext = ".nef";
			}
			int attempts = 0;
			bool templateHasCounter = FileNameTemplate != null && FileNameTemplate.Contains("[Counter");
			if (!string.IsNullOrEmpty(_lastFilename) && RawExtensions.Contains(ext.ToLower()) && !RawExtensions.Contains(Path.GetExtension(_lastFilename).ToLower()))
			{
				string rawfile = Path.Combine(Folder, FormatFileName(device, ext, tempfile, incremetCounter: false) + ((!ext.StartsWith(".")) ? "." : "") + ext);
				if (!File.Exists(rawfile))
				{
					return rawfile;
				}
			}
			string unique;
			while (true)
			{
				string candidate = ((!ServiceProvider.Settings.Background) ? Path.Combine(Folder, FormatFileName(device, file, tempfile) + ((!ext.StartsWith(".")) ? "." : "") + ext) : Path.Combine(Folder, FormatFileName(device, file, tempfile, incremetCounter: false) + ((!ext.StartsWith(".")) ? "." : "") + ".jpeg"));
				if (!File.Exists(candidate) || AllowOverWrite)
				{
					_lastFilename = candidate;
					return candidate;
				}
				attempts++;
				if (attempts >= 1000)
				{
					throw new IOException($"Unable to generate unique filename after {1000} attempts: {candidate}");
				}
				if (!templateHasCounter)
				{
					string dir = Path.GetDirectoryName(Path.Combine(Folder, candidate)) ?? Folder;
					string baseName = Path.GetFileNameWithoutExtension(candidate);
					unique = StaticHelper.GetUniqueFilename(dir + "\\" + baseName + "_", attempts - 1, Path.GetExtension(candidate));
					if (!File.Exists(unique) || AllowOverWrite)
					{
						break;
					}
				}
			}
			_lastFilename = unique;
			return unique;
		}
	}
}
public class Toggle {public bool? IsChecked=false;}
public class RemoveBackgroundWnd {public Task PrecessingOneImage(string f)=>Task.CompletedTask;public Task PrecessingTwoImages(string f)=>Task.CompletedTask;}
public class MainCapture {
 bool is270CaptureMode,isCalibrationMode;DateTime lastPowerSaveTime=DateTime.Now;
 public Toggle ModeA=new(){IsChecked=true},Bg_Auto=new();void Backgroud_LightOff(){}void Backgroud_LightOn(){}
 void setExifOrientation(bool x){} public Task Front(bool rotate=false,bool bg=false)=>CaptureOfFrontView(rotate,bg);public Task Back(bool rotate=false,bool bg=false)=>CaptureOfBackView(rotate,bg);public Task<bool> Direct(bool wait=true)=>CaptureAsync(wait);
 public void PhotoFinally(){var eventArgs=new {CameraDevice=ServiceProvider.DeviceManager.SelectedCameraDevice};{
			eventArgs.CameraDevice.IsBusy = false;
			ServiceProvider.Settings.GetMeasurePicture = false;
			ServiceProvider.Settings.FrontBackMode = 0;
			is270CaptureMode = false;
			isCalibrationMode = false;
			ServiceProvider.Settings.Background = false;
			ServiceProvider.Settings.IsDefect = false;
			ServiceProvider.Settings.isBtnPressed = false;
		}}
 private async Task CaptureOfFrontView(bool rotate270, bool removeBG)
	{
		if (ServiceProvider.Settings.isBtnPressed || !ServiceProvider.Settings.ThumbailLoaded)
		{
			return;
		}
		Log.Debug("전면 촬영 Click : " + (rotate270 ? "270 회전" : "무 회전"));
		is270CaptureMode = rotate270;
		setExifOrientation(rotate270);
		if (ServiceProvider.Settings.IsDefect)
		{
			ServiceProvider.Settings.FrontBackMode = 0;
			ServiceProvider.Settings.GetMeasurePicture = false;
			ServiceProvider.Settings.Background = false;
			await CaptureAsync(shouldWait: true);
			return;
		}
		ServiceProvider.Settings.FrontBackMode = 1;
		ServiceProvider.Settings.GetMeasurePicture = true;
		if (!removeBG)
		{
			if (!await CaptureAsync(shouldWait: true))
			{
				return;
			}
		}
		else
		{
			if (!await CaptureBackground())
			{
				return;
			}
		}
		MainMenuViewModel.IsMusinsaFrontOK = true;
		ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_OverlayActive", true);
		ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_OverlayLastCapture", true);
	}
 private async Task CaptureOfBackView(bool rotate270, bool removeBG)
	{
		if (!ServiceProvider.Settings.isBtnPressed && ServiceProvider.Settings.ThumbailLoaded)
		{
			Log.Debug("후면 촬영 Click : " + (rotate270 ? "270 회전" : "무 회전"));
			is270CaptureMode = rotate270;
			setExifOrientation(rotate270);
			ServiceProvider.Settings.FrontBackMode = 2;
			if (!removeBG)
			{
				if (!await CaptureAsync(shouldWait: true))
				{
					return;
				}
			}
			else
			{
				if (!await CaptureBackground())
				{
					return;
				}
			}
			MainMenuViewModel.IsMusinsaBackOK = true;
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_OverlayActive", false);
			ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_OverlayLastCapture", false);
		}
	}
 private async Task<bool> CaptureAsync(bool shouldWait)
	{
		if (!ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected)
		{
			ResetFailedCaptureState();
			System.Windows.MessageBox.Show("⚠\ufe0f 연결된 카메라가 없습니다..");
			return false;
		}
		ServiceProvider.Settings.isBtnPressed = true;
		ServiceProvider.Settings.ThumbailLoaded = false;
		try
		{
			if ((DateTime.Now - lastPowerSaveTime).TotalMinutes >= (double)ServiceProvider.Settings.LightPowerSaveTime || (ServiceProvider.Branding.ControlLighting && ServiceProvider.Settings.ModeLighting))
			{
				LightControlWnd.Instance.AllLightOn();
				Thread.Sleep(200);
			}
			lastPowerSaveTime = DateTime.Now;
			if (!await CameraHelper.CaptureAsync())
			{
				ResetFailedCaptureState();
				return false;
			}
			if (!shouldWait)
			{
				return true;
			}
			return await WaitForCaptureComplete(30);
		}
		catch (Exception ex)
		{
			ResetFailedCaptureState();
			StaticHelper.Instance.SystemMessage = ex.Message;
			Log.Error("Error : Capture()", ex);
			return false;
		}
	}
 private async Task<bool> WaitForCaptureComplete(int maxRetries)
	{
		int timer = 0;
		while (ServiceProvider.Settings.isBtnPressed)
		{
			if (timer++ > maxRetries)
			{
				return false;
			}
			await Task.Delay(100);
		}
		return true;
	}
 private void ResetFailedCaptureState()
	{
		// Restore the purpose before re-enabling capture after a start failure.
		ServiceProvider.Settings.FrontBackMode = 0;
		ServiceProvider.Settings.GetMeasurePicture = false;
		ServiceProvider.Settings.Background = false;
		is270CaptureMode = false;
		isCalibrationMode = false;
		ServiceProvider.Settings.ThumbailLoaded = true;
		ServiceProvider.Settings.isBtnPressed = false;
	}
 private async Task<bool> CaptureBackground()
	{
		ServiceProvider.Settings.Background = false;
		if (!(await CaptureAsync(shouldWait: true)))
		{
			System.Windows.MessageBox.Show("\ud83d\udea8조명[ON] 타임오버", "경고", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			return false;
		}
		if (ModeA.IsChecked == false)
		{
			ServiceProvider.Settings.Background = true;
			Backgroud_LightOff();
			await Task.Delay(300);
			bool success = await CaptureAsync(shouldWait: true);
			Backgroud_LightOn();
			if (!success)
			{
				System.Windows.MessageBox.Show("\ud83d\udea8조명[OFF] 타임오버", "경고", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return false;
			}
		}
		if (Bg_Auto.IsChecked == false)
		{
			return true;
		}
		string filename = ServiceProvider.DeviceManager.LastCapturedImage[ServiceProvider.DeviceManager.SelectedCameraDevice];
		if (filename != null)
		{
			RemoveBackgroundWnd wnd = new RemoveBackgroundWnd();
			if (ModeA.IsChecked == true)
			{
				await wnd.PrecessingOneImage(filename);
			}
			else
			{
				await wnd.PrecessingTwoImages(filename);
			}
		}
		return true;
	}
}
public static class CameraHelper {
 public static async Task<bool> CaptureAsync()
	{
		try
		{
			return await Task.Run(() => CaptureAsyncWithError());
		}
		catch (Exception ex)
		{
			Exception e = ex;
			// A failed exposure has no PhotoCaptured finally callback to clear its purpose.
			ServiceProvider.Settings.FrontBackMode = 0;
			ServiceProvider.Settings.GetMeasurePicture = false;
			ServiceProvider.Settings.Background = false;
			ServiceProvider.Settings.isBtnPressed = false;
			ServiceProvider.Settings.ThumbailLoaded = true;
			StaticHelper.Instance.SystemMessage = e.Message;
			Application.Current.Dispatcher.Invoke(() =>
			{
				MessageBox.Show("포커스가 맞지 않습니다.\r\n박스내 포커스를 맞추고 다시 시도해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				Log.Debug("포커스가 맞지 않습니다.");
			});
			return false;
		}
	}
 public static bool CaptureAsyncWithError(ICameraDevice device = null)
	{
		return CaptureInternal(device ?? ServiceProvider.DeviceManager.SelectedCameraDevice);
	}
 private static bool CaptureInternal(object o)
	{
		if (!(o is ICameraDevice camera))
		{
			return false;
		}
		ServiceProvider.DeviceManager.LastCapturedImage[camera] = "-";
		CameraProperty property = ServiceProvider.Settings.CameraProperties.Get(camera);
		for (int i = 0; i < property.Delay; i++)
		{
			StaticHelper.Instance.SystemMessage = "Counting down " + (property.Delay - i);
			Thread.Sleep(1000);
		}
		if (property.UseExternalShutter && property.SelectedConfig != null)
		{
			ServiceProvider.ExternalDeviceManager.AssertFocus(property.SelectedConfig);
			Thread.Sleep(ServiceProvider.Settings.ExternalDeviceWaitForFocus);
			ServiceProvider.ExternalDeviceManager.OpenShutter(property.SelectedConfig);
			Thread.Sleep(ServiceProvider.Settings.ExternalDeviceWaitForCapture);
			ServiceProvider.ExternalDeviceManager.CloseShutter(property.SelectedConfig);
			return true;
		}
		camera.CapturePhoto();
		ServiceProvider.Analytics.CameraCapture(camera);
		return true;
	}
}
public class Result {public string test {get;set;}public bool passed {get;set;}public object details {get;set;}}
public static class Program {
 public static string TempFolder=Path.Combine(Path.GetTempPath(),"Clorium2161-"+Guid.NewGuid().ToString("N"),"M1_12345678");
 public static string TempBase=>Directory.GetParent(TempFolder).FullName;public static string ProductRoot=>Path.Combine(TempBase,"products");
 public static string Outcome="success",LastName="";static Session session;static MainCapture main;static List<Result> results=new();static List<string> names=new();static List<string> flags=new();static int shots;static bool oneFailure;
 static void Reset(){ServiceProvider.Settings=new();ServiceProvider.DeviceManager=new();MainMenuViewModel.IsMusinsaFrontOK=false;MainMenuViewModel.IsMusinsaBackOK=false;session=new();session.Folder=TempFolder;session.Counter=names.Count+1;main=new();Outcome="success";LastName="";shots=0;oneFailure=false;}
 public static void DeviceCapture(){shots++;if(Outcome=="focus-error"||(Outcome=="second-fails"&&shots==2))throw new IOException("Simulated camera failure");if(Outcome=="pending")return;if(Outcome=="delayed"){Task.Run(async()=>{await Task.Delay(200);OnPhoto();});return;}OnPhoto();}
 public static void OnPhoto(){var s=ServiceProvider.Settings;LastName=session.GetNextFileName("IMG.jpg",ServiceProvider.DeviceManager.SelectedCameraDevice,"temp");names.Add(LastName);flags.Add($"{s.FrontBackMode}/{s.IsDefect}/{s.GetMeasurePicture}/{s.Background}");Directory.CreateDirectory(Path.GetDirectoryName(LastName));File.WriteAllText(LastName,"synthetic test payload, not a camera JPEG");ServiceProvider.DeviceManager.LastCapturedImage[ServiceProvider.DeviceManager.SelectedCameraDevice]=LastName;main.PhotoFinally();s.ThumbailLoaded=true;}
 static void Check(string name,bool passed,object details){results.Add(new(){test=name,passed=passed,details=details});if(!passed)Console.Error.WriteLine("FAIL: "+name);}
 static void ExpectSuffix(string test,string suffix){Check(test,LastName.EndsWith(suffix),new{LastName});}
 public static async Task Main(){try{Directory.CreateDirectory(TempFolder);
  foreach(int mode in new[]{0,1,2})foreach(bool defect in new[]{false,true})foreach(bool bg in new[]{false,true}){Reset();var s=ServiceProvider.Settings;s.FrontBackMode=mode;s.IsDefect=defect;s.Background=bg;var name=session.FormatOnly();string expected=defect?"_D.jpg":mode==1?"_F.jpg":mode==2?"_B.jpg":bg?"_BG.jpg":".jpg";Check($"suffix matrix mode={mode}, defect={defect}, background={bg}",name.EndsWith(expected),new{name});}
  Reset();await main.Front();ExpectSuffix("normal front","_F.jpg");Check("front completion marker",MainMenuViewModel.IsMusinsaFrontOK,new{MainMenuViewModel.IsMusinsaFrontOK});Check("front creates measurement intent",flags.Last()=="1/False/True/False",new{state=flags.Last()});Check("normal completion clears purpose",ServiceProvider.Settings.FrontBackMode==0&&!ServiceProvider.Settings.GetMeasurePicture&&!ServiceProvider.Settings.IsDefect&&!ServiceProvider.Settings.isBtnPressed,new{ServiceProvider.Settings.FrontBackMode});
  ServiceProvider.Settings.IsDefect=true;await main.Front();ExpectSuffix("defect after normal front","_D.jpg");Check("defect disables measurement",flags.Last()=="0/True/False/False",new{state=flags.Last()});
  Reset();await main.Back();ExpectSuffix("normal back","_B.jpg");Check("back completion marker",MainMenuViewModel.IsMusinsaBackOK,new{MainMenuViewModel.IsMusinsaBackOK});ServiceProvider.Settings.IsDefect=true;await main.Front();ExpectSuffix("defect after normal back","_D.jpg");
  foreach(string direction in new[]{"front","back"}){Reset();Outcome="focus-error";if(direction=="front")await main.Front();else await main.Back();var s=ServiceProvider.Settings;Check(direction+" failure restores purpose and readiness",s.FrontBackMode==0&&!s.GetMeasurePicture&&!s.Background&&!s.isBtnPressed&&s.ThumbailLoaded,new{s.FrontBackMode,s.GetMeasurePicture,s.isBtnPressed,s.ThumbailLoaded});Check(direction+" failure does not mark completed",!MainMenuViewModel.IsMusinsaFrontOK&&!MainMenuViewModel.IsMusinsaBackOK,new{MainMenuViewModel.IsMusinsaFrontOK,MainMenuViewModel.IsMusinsaBackOK});Outcome="success";s.IsDefect=true;await main.Front();ExpectSuffix("REGRESSION: defect after failed "+direction,"_D.jpg");Check("no measurement contamination after failed "+direction,flags.Last()=="0/True/False/False",new{state=flags.Last()});}
  Reset();ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected=false;await main.Front();Check("disconnected front resets mode",ServiceProvider.Settings.FrontBackMode==0&&!ServiceProvider.Settings.GetMeasurePicture&&ServiceProvider.Settings.ThumbailLoaded,new{ServiceProvider.Settings.FrontBackMode});ServiceProvider.DeviceManager.SelectedCameraDevice.IsConnected=true;ServiceProvider.Settings.IsDefect=true;await main.Front();ExpectSuffix("REGRESSION: reconnected defect","_D.jpg");
  Reset();ServiceProvider.Settings.IsDefect=true;Outcome="focus-error";await main.Front();Check("failed defect keeps selection for retry",ServiceProvider.Settings.IsDefect&&!ServiceProvider.Settings.isBtnPressed,new{ServiceProvider.Settings.IsDefect});Outcome="success";await main.Front();ExpectSuffix("defect retry","_D.jpg");
  Reset();Outcome="focus-error";bool direct=await main.Direct();Check("helper false propagates to Main",!direct,new{direct});
  Reset();Outcome="delayed";await main.Front();ExpectSuffix("delayed callback keeps front purpose","_F.jpg");Check("delayed callback releases capture",!ServiceProvider.Settings.isBtnPressed,new{shots});
  Reset();Outcome="pending";bool started=await main.Direct(false);Check("nonwaiting capture remains busy until event",started&&ServiceProvider.Settings.isBtnPressed,new{started});await main.Front();Check("busy button ignores extra front request",shots==1,new{shots});OnPhoto();Check("late event releases pending request",!ServiceProvider.Settings.isBtnPressed,new{shots});
  Reset();Outcome="pending";await main.Front();Check("timeout holds busy for late file",ServiceProvider.Settings.isBtnPressed&&!MainMenuViewModel.IsMusinsaFrontOK,new{ServiceProvider.Settings.isBtnPressed});await main.Back();Check("timeout prevents overlapping next request",shots==1,new{shots});OnPhoto();ExpectSuffix("late timed out file retains original purpose","_F.jpg");
  Reset();await main.Front(true);ExpectSuffix("rotated front","_F.jpg");Reset();ServiceProvider.Settings.FrontBackMode=1;ServiceProvider.Settings.GetMeasurePicture=true;ServiceProvider.Settings.Background=true;ServiceProvider.Settings.IsDefect=true;await main.Front(true);ExpectSuffix("rotated defect neutralizes stale flags","_D.jpg");Check("rotated defect no measurement intent",flags.Last()=="0/True/False/False",new{state=flags.Last()});
  Reset();await main.Front(false,true);ExpectSuffix("one exposure background mode front","_F.jpg");Check("one exposure mode completion",MainMenuViewModel.IsMusinsaFrontOK,new{shots});
  Reset();main.ModeA.IsChecked=false;await main.Front(false,true);Check("two exposure background mode",shots==2&&LastName.EndsWith("_BG.jpeg")&&MainMenuViewModel.IsMusinsaFrontOK,new{shots,LastName});
  Reset();main.ModeA.IsChecked=false;Outcome="second-fails";await main.Front(false,true);Check("background second failure is not completed",shots==2&&!MainMenuViewModel.IsMusinsaFrontOK&&!ServiceProvider.Settings.Background,new{shots});
  Reset();ServiceProvider.Branding.ControlMultiCamera=true;ServiceProvider.Settings.CameraAlias="_A";ServiceProvider.Settings.IsDefect=true;await main.Front();Check("multi-camera defect naming",Path.GetFileName(LastName).StartsWith("M1_A_12345678_")&&LastName.EndsWith("_D.jpg"),new{LastName});ServiceProvider.Branding.ControlMultiCamera=false;
  Reset();for(int i=0;i<30;i++){if(i%3==0)await main.Front();else if(i%3==1)await main.Back();else{ServiceProvider.Settings.IsDefect=true;await main.Front();}string suffix=i%3==0?"_F.jpg":i%3==1?"_B.jpg":"_D.jpg";Check("mixed sequence shot "+(i+1),LastName.EndsWith(suffix),new{LastName});}
  Reset();var first=session.GetNextFileName("IMG.jpg",ServiceProvider.DeviceManager.SelectedCameraDevice,"");File.WriteAllText(first,"collision");session.Counter--;var next=session.GetNextFileName("IMG.jpg",ServiceProvider.DeviceManager.SelectedCameraDevice,"");Check("existing file gets unique counter",first!=next&&!File.Exists(next),new{first,next});
  Reset();session.FileNameTemplate="Barcode";first=session.GetNextFileName("IMG.jpg",ServiceProvider.DeviceManager.SelectedCameraDevice,"");File.WriteAllText(first,"collision");next=session.GetNextFileName("IMG.jpg",ServiceProvider.DeviceManager.SelectedCameraDevice,"");Check("counterless template handles collision",first!=next&&!File.Exists(next),new{first,next});
  Reset();session.AllowOverWrite=true;first=session.GetNextFileName("IMG.jpg",ServiceProvider.DeviceManager.SelectedCameraDevice,"");File.WriteAllText(first,"keep");session.Counter--;next=session.GetNextFileName("IMG.jpg",ServiceProvider.DeviceManager.SelectedCameraDevice,"");Check("overwrite option keeps path",first==next,new{first,next});
  var menu=new MainMenuViewModel();
  foreach(string previous in new[]{"front","back","defect"})foreach(bool fail in new[]{false,true}){
   Reset();var oldSession=session;Outcome=fail?"focus-error":"success";
   if(previous=="front")await main.Front();else if(previous=="back")await main.Back();else{ServiceProvider.Settings.IsDefect=true;await main.Front();}
   string barcode="NEXT_"+previous+"_"+(fail?"FAILED":"OK");Outcome="success";
   string nextFolder=menu.CreateNewSessionFolder(barcode);var state=ServiceProvider.Settings;session=state.DefaultSession;
   Check("new product after "+previous+" fail="+fail+" clears purpose",nextFolder!=null&&!state.IsDefect&&state.FrontBackMode==0&&!state.GetMeasurePicture&&!state.Background,new{nextFolder,state.IsDefect,state.FrontBackMode});
   Check("new product uses new session "+barcode,session!=oldSession&&session.Folder==nextFolder&&Path.GetFileName(nextFolder)=="M1_"+barcode,new{session.Folder});
   Check("product counters update once "+barcode,state.AddedSessions==1&&state.TodaySessionCounter==1&&state.TotalSessionCounter==1,new{state.AddedSessions});
   await main.Front();ExpectSuffix("new product front "+barcode,"_F.jpg");Check("new product file barcode "+barcode,Path.GetFileName(LastName).StartsWith("M1_"+barcode+"_"),new{LastName});
   await main.Back();ExpectSuffix("new product back "+barcode,"_B.jpg");state.IsDefect=true;await main.Front();ExpectSuffix("new product defect "+barcode,"_D.jpg");
  }
  Reset();ServiceProvider.Settings.IsDefect=true;ServiceProvider.Settings.FrontBackMode=2;ServiceProvider.Settings.GetMeasurePicture=true;ServiceProvider.Settings.Background=true;
  menu.CreateNewSessionFolder("ALL_STALE");var clean=ServiceProvider.Settings;Check("new product clears every stale capture flag",!clean.IsDefect&&clean.FrontBackMode==0&&!clean.GetMeasurePicture&&!clean.Background,new{clean.IsDefect,clean.FrontBackMode});
  Reset();Outcome="pending";await main.Direct(false);var prior=session;ServiceProvider.Settings.DefaultSession=prior;int before=ServiceProvider.Settings.AddedSessions;
  var blocked=menu.CreateNewSessionFolder("BLOCK_BUSY");Check("busy product switch rejected",blocked==null&&ServiceProvider.Settings.DefaultSession==prior&&ServiceProvider.Settings.AddedSessions==before,new{blocked});
  Check("rejected switch retains pending state",ServiceProvider.Settings.isBtnPressed&&!ServiceProvider.Settings.ThumbailLoaded,new{ServiceProvider.Settings.isBtnPressed});
  OnPhoto();Check("late callback remains with previous product",Path.GetDirectoryName(LastName)==prior.Folder,new{LastName});
  var afterLate=menu.CreateNewSessionFolder("AFTER_LATE");Check("switch allowed after late callback",afterLate!=null&&!ServiceProvider.Settings.IsDefect,new{afterLate});
  Reset();ServiceProvider.Settings.ThumbailLoaded=false;ServiceProvider.Settings.IsDefect=true;blocked=menu.CreateNewSessionFolder("BLOCK_THUMB");Check("thumbnail-pending switch rejected",blocked==null&&ServiceProvider.Settings.IsDefect&&ServiceProvider.Settings.AddedSessions==0,new{blocked});ServiceProvider.Settings.ThumbailLoaded=true;
  var ready=menu.CreateNewSessionFolder("THUMB_READY");Check("thumbnail-complete switch clears defect",ready!=null&&!ServiceProvider.Settings.IsDefect,new{ready});
  Reset();Outcome="pending";await main.Front();blocked=menu.CreateNewSessionFolder("BLOCK_TIMEOUT");Check("timeout switch rejected until event settles",blocked==null&&ServiceProvider.Settings.isBtnPressed,new{blocked});OnPhoto();Outcome="success";
  var postTimeout=menu.CreateNewSessionFolder("AFTER_TIMEOUT");Check("post-timeout event permits clean session",postTimeout!=null&&!ServiceProvider.Settings.IsDefect&&ServiceProvider.Settings.FrontBackMode==0,new{postTimeout});
  Reset();ServiceProvider.Settings.IsDefect=true;var priorFolder=session.Folder;string blockedRoot=Path.Combine(TempBase,"file-not-directory");File.WriteAllText(blockedRoot,"test barrier");ServiceProvider.Settings.DownloadFolder=blockedRoot;bool pathFailed=false;try{menu.CreateNewSessionFolder("CREATE_FAILURE");}catch(IOException){pathFailed=true;}
  Check("folder creation failure preserves current defect selection",pathFailed&&ServiceProvider.Settings.IsDefect&&ServiceProvider.Settings.AddedSessions==0,new{pathFailed});
  Reset();string duplicate1=menu.CreateNewSessionFolder("DUPLICATE_PRODUCT");ServiceProvider.Settings.IsDefect=true;string duplicate2=menu.CreateNewSessionFolder("DUPLICATE_PRODUCT");Check("duplicate product session gets unique folder and clears purpose",duplicate1!=duplicate2&&duplicate2.EndsWith("___1")&&!ServiceProvider.Settings.IsDefect,new{duplicate1,duplicate2});
  Directory.CreateDirectory(Settings.SessionFolder);string configFile=Path.Combine(Settings.SessionFolder,"Default.json");File.WriteAllText(configFile,"test config placeholder");
  Reset();ServiceProvider.Settings.IsDefect=true;var templated=menu.CreateNewSessionFolder("WITH_DEFAULT_TEMPLATE");session=ServiceProvider.Settings.DefaultSession;Check("default session load also clears defect",ServiceProvider.Settings.LoadedSessions==1&&!ServiceProvider.Settings.IsDefect&&session.Counter==7,new{templated});await main.Front();ExpectSuffix("default session template front","_F.jpg");File.Delete(configFile);
  Reset();for(int product=1;product<=8;product++){ServiceProvider.Settings.IsDefect=true;string folder=menu.CreateNewSessionFolder("BATCH_"+product);session=ServiceProvider.Settings.DefaultSession;await main.Front();ExpectSuffix("batch product "+product+" front","_F.jpg");await main.Back();ExpectSuffix("batch product "+product+" back","_B.jpg");ServiceProvider.Settings.IsDefect=true;await main.Front();ExpectSuffix("batch product "+product+" defect","_D.jpg");}
  Check("synthetic stored files readable",names.All(f=>File.ReadAllText(f).StartsWith("synthetic")),new{stored=names.Count});
  foreach(bool active in new[]{false,true})foreach(string previous in new[]{"front","back","defect"})foreach(string outcome in new[]{"success","focus-error"}){
   Reset();ServiceProvider.Settings.IsAestheticImprovementActive=active;Outcome=outcome;ServiceProvider.Settings.IsDefect=previous=="defect";if(previous=="back")await main.Back();else await main.Front();
   Check("aesthetic mode survives "+previous+" "+outcome+" active="+active,ServiceProvider.Settings.IsAestheticImprovementActive==active,new{active,previous,outcome});
   Outcome="success";var created=menu.CreateNewSessionFolder("AES_"+active+previous+outcome);session=ServiceProvider.Settings.DefaultSession;
   Check("aesthetic product switch clean state "+active+previous+outcome,created!=null&&!ServiceProvider.Settings.IsDefect&&ServiceProvider.Settings.IsAestheticImprovementActive==active,new{created});
   await main.Front();ExpectSuffix("aesthetic next front "+active+previous+outcome,active?"_PR_F.jpg":"_F.jpg");await main.Back();ExpectSuffix("aesthetic next back "+active+previous+outcome,active?"_PR_B.jpg":"_B.jpg");ServiceProvider.Settings.IsDefect=true;await main.Front();ExpectSuffix("aesthetic next defect "+active+previous+outcome,"_D.jpg");
  }
  Reset();for(int product=1;product<=50;product++){
   bool active=(product/5)%2==0;ServiceProvider.Settings.IsAestheticImprovementActive=active;menu.CreateNewSessionFolder("AES_BATCH_"+product);session=ServiceProvider.Settings.DefaultSession;
   await main.Front();ExpectSuffix("aesthetic 50 product front "+product,active?"_PR_F.jpg":"_F.jpg");await main.Back();ExpectSuffix("aesthetic 50 product back "+product,active?"_PR_B.jpg":"_B.jpg");ServiceProvider.Settings.IsDefect=true;await main.Front();ExpectSuffix("aesthetic 50 product defect "+product,"_D.jpg");
  }
  Reset();ServiceProvider.Settings.IsAestheticImprovementActive=true;Outcome="focus-error";await main.Front();ServiceProvider.Settings.IsDefect=true;Outcome="success";await main.Front();ExpectSuffix("aesthetic failed front then same-product defect","_D.jpg");Check("aesthetic same-product retry keeps active mode",ServiceProvider.Settings.IsAestheticImprovementActive,new{LastName});  var report=new{scope="Exact current production method bodies, fake camera/dialogs/lighting; front/back signatures adapted async void to Task. File I/O uses synthetic payloads, not JPEG encoding.",tests=results.Count,passed=results.Count(r=>r.passed),failed=results.Count(r=>!r.passed),results};File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"results.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(new{report.tests,report.passed,report.failed,resultPath=Path.Combine(AppContext.BaseDirectory,"results.json")}));Environment.ExitCode=report.failed==0?0:1;
 }finally{var cleanup=Path.GetFullPath(Directory.GetParent(TempFolder).FullName);if(!cleanup.StartsWith(Path.GetFullPath(Path.GetTempPath()))||!Path.GetFileName(cleanup).StartsWith("Clorium2161-"))throw new IOException("Unexpected temporary cleanup path");Directory.Delete(cleanup,true);}}
}
