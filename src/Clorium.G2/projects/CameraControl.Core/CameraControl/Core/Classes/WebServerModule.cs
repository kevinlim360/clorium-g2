using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CameraControl.Core.Scripting;
using CameraControl.Devices;
using Griffin.WebServer;
using Griffin.WebServer.Files;
using Griffin.WebServer.Modules;
using Newtonsoft.Json;

namespace CameraControl.Core.Classes;

public class WebServerModule : IWorkerModule, IHttpModule
{
	private string _lineFormat = "            <img u=\"image\" src=\"@image@\" />";

	private bool _liveViewFirstRun = true;

	public void BeginRequest(IHttpContext context)
	{
	}

	public void EndRequest(IHttpContext context)
	{
	}

	public void HandleRequestAsync(IHttpContext context, Action<IAsyncModuleResult> callback)
	{
		callback(new AsyncModuleResult(context, HandleRequest(context)));
	}

	public ModuleResult HandleRequest(IHttpContext context)
	{
		try
		{
			if (string.IsNullOrEmpty(context.Request.Uri.AbsolutePath) || context.Request.Uri.AbsolutePath == "/")
			{
				string str = context.Request.Uri.Scheme + "://" + context.Request.Uri.Host;
				if (context.Request.Uri.Port != 80)
				{
					str = str + ":" + context.Request.Uri.Port;
				}
				string uriString = str + context.Request.Uri.AbsolutePath + "index.html";
				if (!string.IsNullOrEmpty(context.Request.Uri.Query))
				{
					uriString = uriString + "?" + context.Request.Uri.Query;
				}
				context.Request.Uri = new Uri(uriString);
			}
			if (context.Request.Uri.AbsolutePath.StartsWith("/thumb/large"))
			{
				string requestFile = Path.GetFileName(context.Request.Uri.AbsolutePath.Replace("/", "\\"));
				foreach (FileItem item in ServiceProvider.Settings.DefaultSession.Files)
				{
					if (Path.GetFileName(item.LargeThumb) == requestFile || item.Name == requestFile)
					{
						SendFile(context, (!File.Exists(item.LargeThumb)) ? Path.Combine(Settings.WebServerFolder, "logo.png") : item.LargeThumb);
						SendFile(context, item.LargeThumb);
						return ModuleResult.Continue;
					}
				}
			}
			if (context.Request.Uri.AbsolutePath.StartsWith("/thumb/small"))
			{
				string requestFile2 = Path.GetFileName(context.Request.Uri.AbsolutePath.Replace("/", "\\"));
				foreach (FileItem item2 in ServiceProvider.Settings.DefaultSession.Files)
				{
					if (Path.GetFileName(item2.SmallThumb) == requestFile2 || item2.Name == requestFile2)
					{
						SendFile(context, (!File.Exists(item2.SmallThumb)) ? Path.Combine(Settings.WebServerFolder, "logo.png") : item2.SmallThumb);
						return ModuleResult.Continue;
					}
				}
			}
			if (context.Request.Uri.AbsolutePath.StartsWith("/preview.jpg"))
			{
				SendFile(context, ServiceProvider.Settings.SelectedBitmap.FileItem.LargeThumb);
			}
			if (context.Request.Uri.AbsolutePath.StartsWith("/session.json"))
			{
				string s = JsonConvert.SerializeObject(ServiceProvider.Settings.DefaultSession, Formatting.Indented);
				SendData(context, Encoding.ASCII.GetBytes(s));
			}
			if (context.Request.Uri.AbsolutePath.StartsWith("/settings.json"))
			{
				string s2 = JsonConvert.SerializeObject(ServiceProvider.Settings, Formatting.Indented);
				SendData(context, Encoding.ASCII.GetBytes(s2));
			}
			if (context.Request.Uri.AbsolutePath.StartsWith("/filelist.json"))
			{
				List<FileListItem> items = ServiceProvider.Settings.DefaultSession.Files.Select((FileItem fileItem) => new FileListItem
				{
					FileName = fileItem.FileName,
					LargeThumb = "/thumb/large/" + Path.GetFileName(fileItem.LargeThumb),
					SmallThumb = "/thumb/small/" + Path.GetFileName(fileItem.SmallThumb),
					Original = "/image/" + Path.GetFileName(fileItem.FileName),
					Name = fileItem.Name,
					Width = ((fileItem.FileInfo.Width > 0) ? fileItem.FileInfo.Width : 3000),
					Height = ((fileItem.FileInfo.Height > 0) ? fileItem.FileInfo.Height : 2000)
				}).ToList();
				string s3 = JsonConvert.SerializeObject(items, Formatting.Indented);
				SendData(context, Encoding.ASCII.GetBytes(s3));
			}
			if (context.Request.Uri.AbsolutePath.StartsWith("/liveview.jpg") && ServiceProvider.DeviceManager.SelectedCameraDevice != null && ServiceProvider.DeviceManager.LiveViewImage.ContainsKey(ServiceProvider.DeviceManager.SelectedCameraDevice))
			{
				SendDataFile(context, ServiceProvider.DeviceManager.LiveViewImage[ServiceProvider.DeviceManager.SelectedCameraDevice], MimeTypeProvider.Instance.Get("liveview.jpg"));
			}
			if (context.Request.Uri.AbsolutePath.StartsWith("/liveviewwebcam.jpg") && ServiceProvider.DeviceManager.SelectedCameraDevice != null)
			{
				if (_liveViewFirstRun)
				{
					ServiceProvider.WindowsManager.ExecuteCommand("LiveViewWnd_Show");
					Thread.Sleep(500);
					ServiceProvider.WindowsManager.ExecuteCommand("All_Minimize");
					ServiceProvider.WindowsManager.ExecuteCommand("LiveView_NoProcess");
					_liveViewFirstRun = false;
				}
				if (ServiceProvider.DeviceManager.LiveViewImage.ContainsKey(ServiceProvider.DeviceManager.SelectedCameraDevice))
				{
					SendDataFile(context, ServiceProvider.DeviceManager.LiveViewImage[ServiceProvider.DeviceManager.SelectedCameraDevice], MimeTypeProvider.Instance.Get("liveview.jpg"));
				}
			}
			if (context.Request.Uri.AbsolutePath.StartsWith("/image/"))
			{
				foreach (FileItem item3 in ServiceProvider.Settings.DefaultSession.Files)
				{
					if (Path.GetFileName(item3.FileName) == Path.GetFileName(context.Request.Uri.AbsolutePath.Replace("/", "\\")))
					{
						SendFile(context, item3.FileName);
						return ModuleResult.Continue;
					}
				}
			}
			string slc = context.Request.QueryString["slc"];
			if (ServiceProvider.Settings.AllowWebserverActions && !string.IsNullOrEmpty(slc))
			{
				string response = "";
				try
				{
					CommandLineProcessor processor = new CommandLineProcessor();
					processor.SetCamera(context.Request.QueryString["camera"]);
					object resp = processor.Pharse(new string[3]
					{
						context.Request.QueryString["slc"],
						context.Request.QueryString["param1"],
						context.Request.QueryString["param2"]
					});
					if (resp is IEnumerable<string> list)
					{
						foreach (string o in list)
						{
							response = response + o + "\n";
						}
					}
					else if (resp != null)
					{
						response = resp.ToString();
					}
				}
				catch (Exception ex)
				{
					response = ex.Message;
				}
				if (string.IsNullOrEmpty(response))
				{
					response = "OK";
				}
				byte[] buffer = Encoding.UTF8.GetBytes(response);
				context.Response.AddHeader("Content-Length", buffer.Length.ToString());
				context.Response.ContentType = "text/html";
				context.Response.Body = new MemoryStream();
				context.Response.Body.Write(buffer, 0, buffer.Length);
				context.Response.Body.Position = 0L;
				return ModuleResult.Continue;
			}
			string fullpath = GetFullPath(context.Request.Uri);
			if (!string.IsNullOrEmpty(fullpath) && File.Exists(fullpath))
			{
				if (Path.GetFileName(fullpath) == "slide.html")
				{
					string file = File.ReadAllText(fullpath);
					string template = Path.Combine(Settings.WebServerFolder, "template.txt");
					string jsontemplate = Path.Combine(Settings.WebServerFolder, "template.json");
					bool json = false;
					if (File.Exists(template))
					{
						_lineFormat = File.ReadAllText(template);
					}
					if (File.Exists(jsontemplate))
					{
						_lineFormat = File.ReadAllText(jsontemplate);
						json = true;
					}
					StringBuilder builder = new StringBuilder();
					foreach (FileItem item4 in ServiceProvider.Settings.DefaultSession.Files)
					{
						if (json && builder.Length > 0)
						{
							builder.AppendLine(",");
						}
						string tempStr = _lineFormat.Replace("@image@", "/thumb/large/" + Path.GetFileName(item4.LargeThumb));
						tempStr = tempStr.Replace("@image_thumb@", "/thumb/small/" + Path.GetFileName(item4.SmallThumb));
						tempStr = tempStr.Replace("@image_url@", "/image/" + Path.GetFileName(item4.FileName));
						tempStr = tempStr.Replace("@title@", item4.Name);
						tempStr = tempStr.Replace("@width@", (item4.FileInfo != null && item4.FileInfo.Width > 0) ? item4.FileInfo.Width.ToString() : "3000");
						tempStr = tempStr.Replace("@height@", (item4.FileInfo != null && item4.FileInfo.Height > 0) ? item4.FileInfo.Height.ToString() : "2000");
						tempStr = tempStr.Replace("@desc@", (item4.FileInfo != null) ? (item4.FileInfo.InfoLabel ?? "") : "");
						builder.AppendLine(json ? CleanForJson(tempStr) : tempStr);
					}
					file = file.Replace("@@image_list@@", builder.ToString());
					byte[] buffer2 = Encoding.UTF8.GetBytes(file);
					context.Response.AddHeader("Content-Length", buffer2.Length.ToString());
					context.Response.Body = new MemoryStream();
					context.Response.Body.Write(buffer2, 0, buffer2.Length);
					context.Response.Body.Position = 0L;
				}
				else
				{
					SendFile(context, fullpath);
				}
			}
			string cmd = context.Request.QueryString["CMD"];
			string param = context.Request.QueryString["PARAM"];
			if (ServiceProvider.Settings.AllowWebserverActions && !string.IsNullOrEmpty(cmd))
			{
				ServiceProvider.WindowsManager.ExecuteCommand(cmd, param);
			}
		}
		catch (Exception exception)
		{
			Log.Error("Web server error", exception);
		}
		return ModuleResult.Continue;
	}

	private string CleanForJson(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		char c = '\0';
		int len = s.Length;
		StringBuilder sb = new StringBuilder(len + 4);
		for (int i = 0; i < len; i++)
		{
			c = s[i];
			switch (c)
			{
			case '"':
			case '\\':
				sb.Append('\\');
				sb.Append(c);
				continue;
			case '/':
				sb.Append('\\');
				sb.Append(c);
				continue;
			case '\b':
				sb.Append("\\b");
				continue;
			case '\t':
				sb.Append("\\t");
				continue;
			case '\n':
				sb.Append(c);
				continue;
			case '\f':
				sb.Append("\\f");
				continue;
			case '\r':
				sb.Append(c);
				continue;
			}
			if (c < ' ')
			{
				string t = "000" + string.Format("X", c);
				sb.Append("\\u" + t.Substring(t.Length - 4));
			}
			else
			{
				sb.Append(c);
			}
		}
		return sb.ToString();
	}

	private void SendFile(IHttpContext context, string fullpath)
	{
		if (File.Exists(fullpath))
		{
			string str = MimeTypeProvider.Instance.Get(fullpath);
			FileStream fileStream = new FileStream(fullpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			context.Response.AddHeader("Content-Disposition", "inline;filename=\"" + Path.GetFileName(fullpath) + "\"");
			context.Response.ContentType = str;
			context.Response.ContentLength = (int)fileStream.Length;
			context.Response.Body = fileStream;
		}
	}

	private void SendData(IHttpContext context, byte[] data)
	{
		if (data != null)
		{
			MemoryStream stream = new MemoryStream(data);
			context.Response.ContentType = "application/json";
			context.Response.ContentLength = data.Length;
			context.Response.Body = stream;
		}
	}

	private void SendDataFile(IHttpContext context, byte[] data, string mimet)
	{
		if (data != null)
		{
			MemoryStream stream = new MemoryStream(data);
			context.Response.ContentType = mimet;
			context.Response.ContentLength = data.Length;
			context.Response.Body = stream;
		}
	}

	private string GetFullPath(Uri uri)
	{
		string file = Path.Combine(Settings.BrandingWebServerFolder, Uri.UnescapeDataString(uri.AbsolutePath.Remove(0, 1)).TrimStart('/').Replace('/', '\\'));
		if (File.Exists(file))
		{
			return file;
		}
		return Path.Combine(Settings.WebServerFolder, Uri.UnescapeDataString(uri.AbsolutePath.Remove(0, 1)).TrimStart('/').Replace('/', '\\'));
	}
}
