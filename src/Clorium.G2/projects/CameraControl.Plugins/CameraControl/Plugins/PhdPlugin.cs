using System.Windows;
using CameraControl.Core.Interfaces;
using CameraControl.Plugins.Astro;

namespace CameraControl.Plugins;

public class PhdPlugin : IToolPlugin
{
	public string Title { get; set; }

	public string Id => "{26528ED8-0747-454F-BAEB-ACEF2BEF73C7}";

	public bool Execute()
	{
		PHDWnd wnd = new PHDWnd();
		((Window)wnd).Show();
		return true;
	}

	public void Init()
	{
	}

	public PhdPlugin()
	{
		Title = "PHD Guiding";
	}
}
