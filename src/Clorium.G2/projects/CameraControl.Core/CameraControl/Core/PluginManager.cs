using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CameraControl.Core.Classes;
using CameraControl.Core.Interfaces;
using CameraControl.Core.Plugin;
using CameraControl.Devices;
using CameraControl.Devices.Classes;

namespace CameraControl.Core;

public class PluginManager : BaseFieldClass
{
	private AsyncObservableCollection<IPlugin> _plugins;

	private AsyncObservableCollection<IExportPlugin> _exportPlugins;

	private AsyncObservableCollection<IMainWindowPlugin> _mainWindowPlugins;

	private AsyncObservableCollection<IToolPlugin> _toolPlugins;

	private AsyncObservableCollection<IAutoExportPlugin> _autoExportPlugins;

	private AsyncObservableCollection<IImageTransformPlugin> _imageTransformPlugins;

	private AsyncObservableCollection<IPanelPlugin> _panelPlugins;

	private AsyncObservableCollection<IPanelPlugin> _toolBarPlugins;

	public AsyncObservableCollection<PluginInfo> AvaiablePlugins { get; set; }

	public AsyncObservableCollection<IPlugin> Plugins
	{
		get
		{
			return _plugins;
		}
		set
		{
			_plugins = value;
			NotifyPropertyChanged("Plugins");
		}
	}

	public AsyncObservableCollection<IExportPlugin> ExportPlugins
	{
		get
		{
			return _exportPlugins;
		}
		set
		{
			_exportPlugins = value;
			NotifyPropertyChanged("ExportPlugins");
		}
	}

	public AsyncObservableCollection<IMainWindowPlugin> MainWindowPlugins
	{
		get
		{
			return _mainWindowPlugins;
		}
		set
		{
			_mainWindowPlugins = value;
			NotifyPropertyChanged("MainWindowPlugins");
		}
	}

	public IMainWindowPlugin SelectedWindow { get; set; }

	public AsyncObservableCollection<IToolPlugin> ToolPlugins
	{
		get
		{
			return _toolPlugins;
		}
		set
		{
			_toolPlugins = value;
			NotifyPropertyChanged("ToolPlugins");
		}
	}

	public AsyncObservableCollection<IAutoExportPlugin> AutoExportPlugins
	{
		get
		{
			return _autoExportPlugins;
		}
		set
		{
			_autoExportPlugins = value;
			NotifyPropertyChanged("AutoExportPlugins");
		}
	}

	public AsyncObservableCollection<IImageTransformPlugin> ImageTransformPlugins
	{
		get
		{
			return _imageTransformPlugins;
		}
		set
		{
			_imageTransformPlugins = value;
			NotifyPropertyChanged("ImageTransformPlugins");
		}
	}

	public AsyncObservableCollection<IPanelPlugin> PanelPlugins
	{
		get
		{
			return _panelPlugins;
		}
		set
		{
			_panelPlugins = value;
			NotifyPropertyChanged("PanelPlugins");
		}
	}

	public AsyncObservableCollection<IPanelPlugin> ToolBarPlugins
	{
		get
		{
			return _toolBarPlugins;
		}
		set
		{
			_toolBarPlugins = value;
			NotifyPropertyChanged("ToolBarPlugins");
		}
	}

	public string PluginsFolder => Path.Combine(Settings.DataFolder, "Plugins");

	public string PluginsFolderInInstallFolder => Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "Plugins");

	public PluginManager()
	{
		Plugins = new AsyncObservableCollection<IPlugin>();
		ExportPlugins = new AsyncObservableCollection<IExportPlugin>();
		ToolPlugins = new AsyncObservableCollection<IToolPlugin>();
		MainWindowPlugins = new AsyncObservableCollection<IMainWindowPlugin>();
		AvaiablePlugins = new AsyncObservableCollection<PluginInfo>();
		AutoExportPlugins = new AsyncObservableCollection<IAutoExportPlugin>();
		ImageTransformPlugins = new AsyncObservableCollection<IImageTransformPlugin>();
		PanelPlugins = new AsyncObservableCollection<IPanelPlugin>();
		ToolBarPlugins = new AsyncObservableCollection<IPanelPlugin>();
	}

	public IAutoExportPlugin GetAutoExportPlugin(string type)
	{
		return AutoExportPlugins.FirstOrDefault((IAutoExportPlugin x) => x.Name == type);
	}

	public IImageTransformPlugin GetImageTransformPlugin(string type)
	{
		return ImageTransformPlugins.FirstOrDefault((IImageTransformPlugin x) => x.Name == type);
	}

	public void CopyPlugins()
	{
		if (Directory.Exists(PluginsFolderInInstallFolder))
		{
			if (!Directory.Exists(PluginsFolder))
			{
				Directory.CreateDirectory(PluginsFolder);
			}
			string[] folders = Directory.GetDirectories(PluginsFolderInInstallFolder);
			string[] array = folders;
			foreach (string folder in array)
			{
				CopyFilesRecursively(new DirectoryInfo(folder), new DirectoryInfo(Path.Combine(PluginsFolder, Path.GetFileName(folder))));
			}
		}
	}

	public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
	{
		if (!target.Exists)
		{
			target.Create();
		}
		DirectoryInfo[] directories = source.GetDirectories();
		foreach (DirectoryInfo dir in directories)
		{
			CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
		}
		System.IO.FileInfo[] files = source.GetFiles();
		foreach (System.IO.FileInfo file in files)
		{
			string newfile = Path.Combine(target.FullName, file.Name);
			try
			{
				if (!File.Exists(newfile))
				{
					file.CopyTo(newfile, overwrite: true);
				}
				else if (File.GetLastWriteTimeUtc(newfile) < file.LastWriteTimeUtc)
				{
					file.CopyTo(newfile, overwrite: true);
				}
			}
			catch (Exception exception)
			{
				Log.Debug("Unable to copy file:" + newfile, exception);
			}
		}
	}

	public void LoadPlugins(string pluginFolder)
	{
		if (!Directory.Exists(pluginFolder))
		{
			return;
		}
		string[] folders = Directory.GetDirectories(pluginFolder);
		string[] array = folders;
		foreach (string folder in array)
		{
			string configFile = Path.Combine(folder, "dcc.plugin");
			if (!File.Exists(configFile))
			{
				continue;
			}
			try
			{
				PluginInfo pluginInfo = PluginInfo.Load(configFile);
				if (File.Exists(Path.Combine(folder, "disabled")))
				{
					break;
				}
				string assemblyFile = Path.Combine(folder, pluginInfo.AssemblyFileName);
				AvaiablePlugins.Add(pluginInfo);
				if (!File.Exists(assemblyFile))
				{
					Log.Error("Assembly file not exist " + assemblyFile);
					continue;
				}
				Assembly pluginAssembly = null;
				pluginAssembly = Assembly.LoadFrom(assemblyFile);
				if (pluginAssembly == null)
				{
					Log.Error("Error loading assembly");
					continue;
				}
				Type[] exportedTypes = pluginAssembly.GetExportedTypes();
				Type[] array2 = exportedTypes;
				foreach (Type exportedType in array2)
				{
					if (exportedType.IsAbstract)
					{
						continue;
					}
					object pluginObject = null;
					try
					{
						pluginObject = Activator.CreateInstance(exportedType);
					}
					catch (Exception exception)
					{
						Log.Error("Error loading type " + exportedType.FullName, exception);
					}
					if (pluginObject == null)
					{
						continue;
					}
					IPlugin plugin = pluginObject as IPlugin;
					try
					{
						if (plugin != null)
						{
							plugin.Register();
							Plugins.Add(plugin);
						}
					}
					catch (Exception exception2)
					{
						Log.Error("Error registering plugiin.", exception2);
					}
				}
			}
			catch (Exception exception3)
			{
				Log.Error("Error loading plugin " + configFile, exception3);
			}
		}
	}

	public void LoadPluginsOld(string pluginFolder)
	{
		if (!Directory.Exists(pluginFolder))
		{
			return;
		}
		string[] files = Directory.GetFiles(pluginFolder, "*.dll");
		string[] array = files;
		foreach (string pluginFile in array)
		{
			Assembly pluginAssembly = null;
			try
			{
				Log.Debug("LoadPlugins from:" + pluginFile);
				pluginAssembly = Assembly.LoadFrom(pluginFile);
			}
			catch (BadImageFormatException)
			{
				Log.Error($" {pluginFile} has a bad image format");
			}
			catch (Exception exception)
			{
				Log.Error("Error loading plugin :", exception);
			}
			if (pluginAssembly == null)
			{
				continue;
			}
			try
			{
				Type[] exportedTypes = pluginAssembly.GetExportedTypes();
				Type[] array2 = exportedTypes;
				foreach (Type exportedType in array2)
				{
					if (exportedType.IsAbstract)
					{
						continue;
					}
					object pluginObject = null;
					try
					{
						pluginObject = Activator.CreateInstance(exportedType);
					}
					catch (Exception exception2)
					{
						Log.Error("Error loading type " + exportedType.FullName, exception2);
					}
					if (pluginObject == null)
					{
						continue;
					}
					IPlugin plugin = pluginObject as IPlugin;
					try
					{
						if (plugin != null)
						{
							plugin.Register();
							Plugins.Add(plugin);
						}
					}
					catch (Exception exception3)
					{
						Log.Error("Error registering plugiin.", exception3);
					}
				}
			}
			catch (Exception exception4)
			{
				Log.Error("Error loading plugin  ", exception4);
			}
		}
	}

	public IExecutePlugin GetExecutePlugin(string id)
	{
		return ToolPlugins.Where((IToolPlugin plugin) => plugin.Id == id && plugin is IExecutePlugin).Cast<IExecutePlugin>().FirstOrDefault();
	}
}
