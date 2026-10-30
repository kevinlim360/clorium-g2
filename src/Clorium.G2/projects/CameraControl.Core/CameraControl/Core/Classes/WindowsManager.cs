using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using CameraControl.Core.Interfaces;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;

namespace CameraControl.Core.Classes;

public class WindowsManager
{
	public delegate void EventEventHandler(string cmd, object o);

	private List<IWindow> WindowsList;

	public AsyncObservableCollection<WindowCommandItem> WindowCommands { get; set; }

	public virtual event EventEventHandler Event;

	public WindowsManager()
	{
		WindowsList = new List<IWindow>();
		WindowCommands = new AsyncObservableCollection<WindowCommandItem>();
	}

	public void Add(IWindow window)
	{
		WindowsList.Add(window);
	}

	public void ExecuteCommand(string cmd)
	{
		ExecuteCommand(cmd, null);
	}

	public void ExecuteCommand(string cmd, object o)
	{
		foreach (IWindow window in WindowsList)
		{
			try
			{
				window.ExecuteCommand(cmd, o);
			}
			catch (Exception exception)
			{
				Log.Error("Error to procces command " + cmd, exception);
			}
		}
		if (Event != null)
		{
			Event(cmd, o);
		}
	}

	public void ApplyTheme()
	{
		PaletteHelper paletteHelper = new PaletteHelper();
		ITheme theme = paletteHelper.GetTheme();
		IBaseTheme baseTheme2;
		if (!ServiceProvider.Settings.CurrentThemeNameNew.StartsWith("Dark"))
		{
			IBaseTheme baseTheme = new MaterialDesignLightTheme();
			baseTheme2 = baseTheme;
		}
		else
		{
			IBaseTheme baseTheme = new MaterialDesignDarkTheme();
			baseTheme2 = baseTheme;
		}
		IBaseTheme baseTheme3 = baseTheme2;
		theme.SetBaseTheme(baseTheme3);
		foreach (Swatch item in new SwatchesProvider().Swatches)
		{
			if (item.Name == ServiceProvider.Settings.CurrentThemeNameNew.Split('\\')[1])
			{
				theme.SetPrimaryColor(item.PrimaryHues[item.PrimaryHues.Count / 2].Color);
			}
		}
		paletteHelper.SetTheme(theme);
	}

	public void ApplyKeyHanding()
	{
		foreach (Window win in WindowsList.OfType<Window>())
		{
			win.KeyDown += (object sender, KeyEventArgs args) =>
			{
				TriggerClass.KeyDown(args);
			};
		}
	}

	public IWindow Get(Type t)
	{
		return WindowsList.FirstOrDefault((IWindow window) => window.GetType() == t);
	}

	public void Remove(string type)
	{
		IWindow windowToRemove = null;
		foreach (IWindow window in WindowsList.Where((IWindow window2) => window2.GetType().ToString() == type))
		{
			windowToRemove = window;
		}
		if (windowToRemove != null)
		{
			WindowsList.Remove(windowToRemove);
		}
	}

	public void RegisterKnowCommands()
	{
		AddCommandsFromType(typeof(CmdConsts));
		AddCommandsFromType(typeof(WindowsCmdConsts));
		WindowCommands = new AsyncObservableCollection<WindowCommandItem>(WindowCommands.OrderBy((WindowCommandItem x) => x.Name));
		foreach (WindowCommandItem item in WindowCommands)
		{
			switch (item.Name)
			{
			case "Capture":
				item.SetKey(Key.Space);
				break;
			case "Next_Image":
				item.SetKey(Key.Right);
				break;
			case "Prev_Image":
				item.SetKey(Key.Left);
				break;
			case "Like_Image":
				item.SetKey(Key.P);
				break;
			case "Unlike_Image":
				item.SetKey(Key.X);
				break;
			case "SelectAll_Image":
				item.SetKey(Key.A);
				item.Ctrl = true;
				break;
			case "NextSeries":
				item.SetKey(Key.Add);
				break;
			case "PrevSeries":
				item.SetKey(Key.Subtract);
				break;
			}
		}
	}

	private void AddCommandsFromType(Type type)
	{
		List<FieldInfo> fields = (from fi in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
			where fi.IsLiteral && !fi.IsInitOnly
			select fi).ToList();
		foreach (FieldInfo fieldInfo in fields)
		{
			WindowCommands.Add(new WindowCommandItem
			{
				Name = fieldInfo.GetValue(type).ToString()
			});
		}
	}
}
