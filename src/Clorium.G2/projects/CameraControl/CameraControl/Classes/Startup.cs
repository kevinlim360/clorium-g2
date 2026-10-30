using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;

namespace CameraControl.Classes;

public class Startup
{
	public void Configuration(IAppBuilder app)
	{
		app.UseCors(CorsOptions.AllowAll);
		try
		{
			GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromSeconds(10.0);
			GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromSeconds(6.0);
			GlobalHost.Configuration.KeepAlive = TimeSpan.FromSeconds(2.0);
		}
		catch (InvalidOperationException)
		{
		}
		HubConfiguration hubConfiguration = new HubConfiguration
		{
			EnableJSONP = true,
			EnableDetailedErrors = true,
			EnableJavaScriptProxies = true
		};
		app.MapSignalR("/signalr", hubConfiguration);
		string exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		string webFolder = Path.Combine(exePath, "SignalRWeb");
		if (!Directory.Exists(webFolder))
		{
			Directory.CreateDirectory(webFolder);
		}
		PhysicalFileSystem fileSystem = new PhysicalFileSystem(webFolder);
		FileServerOptions options = new FileServerOptions
		{
			EnableDefaultFiles = true,
			FileSystem = fileSystem,
			EnableDirectoryBrowsing = false
		};
		app.UseFileServer(options);
	}
}
