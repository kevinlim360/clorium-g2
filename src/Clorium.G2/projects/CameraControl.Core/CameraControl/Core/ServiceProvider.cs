using System;
using System.IO;
using System.Text;
using CameraControl.Core.Classes;
using CameraControl.Core.Database;
using CameraControl.Core.Scripting;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;

namespace CameraControl.Core;

public class ServiceProvider : BaseFieldClass
{
	public delegate void FileTransferedEventHandler(object sender, FileItem fileItem);

	private static readonly ILog _log = LogManager.GetLogger("DCC");

	private static PipeServerT _pipeServer;

	public static string AppName = "Clorium";

	public static string LogFile;

	public static Settings Settings { get; set; }

	public static CameraDeviceManager DeviceManager { get; set; }

	public static TriggerClass Trigger { get; set; }

	public static WindowsManager WindowsManager { get; set; }

	public static QueueManager QueueManager { get; set; }

	public static PluginManager PluginManager { get; set; }

	public static Branding Branding { get; set; }

	public static ScriptManager ScriptManager { get; set; }

	public static FilenameTemplateManager FilenameTemplateManager { get; set; }

	public static ExternalDeviceManager ExternalDeviceManager { get; set; }

	public static Analytics Analytics { get; set; }

	public static CameraControl.Core.Database.Database Database { get; set; }

	public static event FileTransferedEventHandler FileTransfered;

	public static void Configure()
	{
		ConfigureLog();
		ConfigureDatabase();
	}

	public static void ConfigureLog()
	{
		if (string.IsNullOrEmpty(LogFile))
		{
			LogFile = Path.Combine(Settings.DataFolder, "Log", "app.log");
			Configure(LogFile);
		}
	}

	public static void ConfigureDatabase()
	{
		try
		{
			Database = new CameraControl.Core.Database.Database(Path.Combine(Settings.DataFolder, "database.db"));
		}
		catch (DllNotFoundException ex)
		{
			if (Log.IsVerbose)
			{
				Log.Error(string.Format("Error(ignored): Database at {0}: {1}", Path.Combine(Settings.DataFolder, "database.db"), ex.Message), ex);
			}
			else
			{
				Log.Error(string.Format("Error(ignored): Database at {0}: {1}", Path.Combine(Settings.DataFolder, "database.db"), ex.Message), null);
			}
		}
	}

	public static void Configure(string logFile)
	{
		Configure(AppName, logFile);
		Log.LogDebug += Log_LogDebug;
		Log.LogError += Log_LogError;
		Log.LogInfo += Log_LogInfo;
		Log.Debug("");
		Log.Debug("============================================================================");
		Log.Debug("===========================          Clorium App starting          ===========================");
		Log.Debug("============================================================================");
		try
		{
			Log.Debug("Clorium Version : " + Settings.Version_Debug);
		}
		catch
		{
		}
		Analytics = new Analytics();
		Log.Debug("Init : Analytics", Settings.UseDebugLog);
		DeviceManager = new CameraDeviceManager();
		Log.Debug("Init : CameraDeviceManager", Settings.UseDebugLog);
		QueueManager = new QueueManager();
		Log.Debug("Init : QueueManager", Settings.UseDebugLog);
		ScriptManager = new ScriptManager();
		Log.Debug("Init : ScriptManager", Settings.UseDebugLog);
		FilenameTemplateManager = new FilenameTemplateManager();
		Log.Debug("Init : FilenameTemplateManager", Settings.UseDebugLog);
		PluginManager = new PluginManager();
		Log.Debug("Init : PluginManager", Settings.UseDebugLog);
		ExternalDeviceManager = new ExternalDeviceManager();
		Log.Debug("Init : ExternalDeviceManager", Settings.UseDebugLog);
		Trigger = new TriggerClass();
		Log.Debug("Init : TriggerClass", Settings.UseDebugLog);
		_pipeServer = new PipeServerT();
		_pipeServer.Listen("DCCPipe");
		Log.Debug("Init : PipeServer", Settings.UseDebugLog);
	}

	public static void Configure(string appfolder, string logFile)
	{
		if (!_log.Logger.Repository.Configured)
		{
			RollingFileAppender fileAppender = new RollingFileAppender
			{
				Layout = new PatternLayout("%d [%t]%-5p %c [%x] - %m%n"),
				MaximumFileSize = "1000KB",
				MaxSizeRollBackups = 5,
				RollingStyle = RollingFileAppender.RollingMode.Size,
				AppendToFile = true,
				File = logFile,
				ImmediateFlush = true,
				LockingModel = new FileAppender.MinimalLock(),
				Name = "XXXRollingFileAppender",
				Encoding = Encoding.UTF8
			};
			fileAppender.ActivateOptions();
			BasicConfigurator.Configure(fileAppender);
			TraceAppender ta = new TraceAppender();
			ta.Layout = new PatternLayout("%d [%t]%-5p %c [%x] - %m%n");
			BasicConfigurator.Configure(ta);
		}
	}

	private static void Log_LogError(LogEventArgs e)
	{
		_log.Error(e.Message, e.Exception);
	}

	private static void Log_LogDebug(LogEventArgs e)
	{
		_log.Debug(e.Message, e.Exception);
	}

	private static void Log_LogInfo(LogEventArgs e)
	{
		_log.Info(e.Message, e.Exception);
	}

	public static void OnFileTransfered(FileItem fileitem)
	{
		FileTransfered?.Invoke(null, fileitem);
	}
}
