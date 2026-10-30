using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Xml;
using CameraControl.Core.Classes;
using CameraControl.Core.Scripting.ScriptCommands;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.Core.Scripting;

public class ScriptManager : BaseFieldClass
{
	public delegate void MessageEventHandler(object sender, MessageEventArgs e);

	private System.Timers.Timer _timer = new System.Timers.Timer(1000.0);

	public bool ShouldStop = false;

	private bool _isBusy;

	public ScriptObject CurrentScript { get; set; }

	public bool IsBusy
	{
		get
		{
			return _isBusy;
		}
		set
		{
			_isBusy = value;
			NotifyPropertyChanged("IsBusy");
		}
	}

	public List<IScriptCommand> AvaiableCommands { get; set; }

	public List<IScriptCommand> EditableCommands => AvaiableCommands.Where((IScriptCommand x) => x.HaveEditControl).ToList();

	public event MessageEventHandler OutPutMessageReceived;

	public ScriptManager()
	{
		AvaiableCommands = new List<IScriptCommand>
		{
			new Alert(),
			new BulbCapture(),
			new Capture(),
			new CaptureAll(),
			new Echo(),
			new ExitLoop(),
			new IfCommand(),
			new Loop(),
			new PHDGuiding(),
			new SelectCamera(),
			new SetVariable(),
			new SetCamera(),
			new Stop(),
			new WaitCommand()
		};
		_timer.Elapsed += _timer_Elapsed;
		_timer.AutoReset = true;
		ServiceProvider.DeviceManager.CameraConnected += DeviceManager_CameraConnected;
		ServiceProvider.DeviceManager.CameraDisconnected += DeviceManager_CameraDisconnected;
		ServiceProvider.DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
	}

	private void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
	{
	}

	private void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
	{
		if (CurrentScript != null)
		{
			CurrentScript.Variabiles["cameraccount"] = ServiceProvider.DeviceManager.ConnectedDevices.Count.ToString();
		}
	}

	private void DeviceManager_CameraConnected(ICameraDevice cameraDevice)
	{
		if (CurrentScript != null)
		{
			CurrentScript.Variabiles["cameraccount"] = ServiceProvider.DeviceManager.ConnectedDevices.Count.ToString();
		}
	}

	private void _timer_Elapsed(object sender, ElapsedEventArgs e)
	{
		GenerateVariabiles();
	}

	private void GenerateVariabiles()
	{
		if (CurrentScript != null)
		{
			CurrentScript.Variabiles["timelong"] = DateTime.Now.ToString("HH:mm:ss");
			CurrentScript.Variabiles["time"] = DateTime.Now.ToString("HH:mm");
			CurrentScript.Variabiles["day"] = DateTime.Now.Day.ToString();
			CurrentScript.Variabiles["cameraccount"] = ServiceProvider.DeviceManager.ConnectedDevices.Count.ToString();
		}
	}

	public void Save(ScriptObject scriptObject, string fileName)
	{
		XmlDocument doc = new XmlDocument();
		XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
		doc.AppendChild(docNode);
		XmlNode rootNode = doc.CreateElement("dccscript");
		rootNode.Attributes.Append(CreateAttribute(doc, "UseExternal", scriptObject.UseExternal ? "true" : "false"));
		rootNode.Attributes.Append(CreateAttribute(doc, "SelectedConfig", (scriptObject.SelectedConfig == null) ? "" : scriptObject.SelectedConfig.Name));
		doc.AppendChild(rootNode);
		XmlNode commandsNode = doc.CreateElement("commands");
		rootNode.AppendChild(commandsNode);
		foreach (IScriptCommand avaiableCommand in scriptObject.Commands)
		{
			commandsNode.AppendChild(avaiableCommand.Save(doc));
		}
		doc.Save(fileName);
	}

	public ScriptObject Load(string fileName)
	{
		ScriptObject res = new ScriptObject();
		XmlDocument doc = new XmlDocument();
		doc.Load(fileName);
		XmlNode rootNode = doc.SelectSingleNode("/dccscript");
		if (rootNode == null)
		{
			throw new ArgumentException("Wrong start of script. Should use ScriptObject");
		}
		if (GetValue(rootNode, "UseExternal") == "true")
		{
			res.UseExternal = true;
		}
		res.SelectedConfig = ServiceProvider.Settings.DeviceConfigs.Get(GetValue(rootNode, "SelectedConfig"));
		XmlNode commandNode = doc.SelectSingleNode("/dccscript/commands");
		if (commandNode != null)
		{
			foreach (XmlNode node in commandNode.ChildNodes)
			{
				foreach (IScriptCommand command in AvaiableCommands)
				{
					if (command.Name.ToLower() == node.Name.ToLower())
					{
						res.Commands.Add(((IScriptCommand)Activator.CreateInstance(command.GetType())).Load(node));
					}
				}
			}
		}
		return res;
	}

	public bool Verify(ScriptObject scriptObject)
	{
		if (scriptObject == null)
		{
			return false;
		}
		bool res = true;
		if (scriptObject.Commands.Count == 0)
		{
			ServiceProvider.ScriptManager.OutPut("No commands are defined");
			res = false;
		}
		return res;
	}

	public static string GetValue(XmlNode node, string atribute)
	{
		if (node.Attributes != null && node.Attributes[atribute] == null)
		{
			return "";
		}
		return node.Attributes[atribute].Value;
	}

	public static XmlAttribute CreateAttribute(XmlDocument doc, string name, string val)
	{
		XmlAttribute attribute = doc.CreateAttribute(name);
		attribute.Value = val;
		return attribute;
	}

	public void Execute(ScriptObject scriptObject)
	{
		ShouldStop = false;
		IsBusy = true;
		scriptObject.Variabiles.Items.Clear();
		scriptObject.ExitLoop = false;
		CurrentScript = scriptObject;
		GenerateVariabiles();
		foreach (IScriptCommand command in scriptObject.Commands)
		{
			command.IsExecuted = false;
			command.Executing = false;
		}
		_timer.Start();
		Thread thread = new Thread(ExecuteThread);
		thread.Start(scriptObject);
	}

	private void ExecuteThread(object o)
	{
		try
		{
			OutPut("Script execution started");
			ScriptObject scriptObject = o as ScriptObject;
			scriptObject.ExecuteCommands(scriptObject.Commands);
			OutPut(ShouldStop ? "Script execution stopped" : "Script execution done");
		}
		catch (Exception ex)
		{
			OutPut("Error executing script " + ex.Message);
			Log.Error("Error executing script", ex);
			StaticHelper.Instance.SystemMessage = ex.Message;
		}
		IsBusy = false;
		_timer.Stop();
	}

	public void Stop()
	{
		ShouldStop = true;
		OutPut("Script execution stopping ....");
	}

	public void OnOutPutMessageReceived(MessageEventArgs e)
	{
		OutPutMessageReceived?.Invoke(this, e);
	}

	public void OutPut(string text)
	{
		OnOutPutMessageReceived(new MessageEventArgs(text));
	}
}
