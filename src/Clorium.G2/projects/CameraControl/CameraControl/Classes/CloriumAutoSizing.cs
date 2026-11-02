using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CameraControl.Core;
using CameraControl.Devices;

namespace CameraControl.Classes;

// G2 ↔ Clorium AutoSizing Desk (garment measurement app, net10 WinForms). Replaces the removed legacy
// "C:\Program Files (x86)\Clorium\Clorium AutoSizing.exe" launcher. The rail button opens the selected
// photo's measurement copy (치수측정\<name>_M.jpeg, Subject = mm/pixel written by MainWindow.WriteMetadata)
// in the running Desk window over its per-user local named pipe, or starts Desk with that path.
// Protocol (Desk ControlPipe.cs): one UTF-8 JSON request and one JSON response per connection, message mode.
// Desk keeps its own unsaved-work prompt; G2 never closes it. Nothing here throws into G2 code.
internal static class CloriumAutoSizing
{
	public const string ExecutableName = "Clorium AutoSizing.exe";

	public const string MeasureFolder = "치수측정";

	private static readonly string[] PhotoExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff" };

	private static DateTime _lastLaunch = DateTime.MinValue;

	[DllImport("user32.dll")]
	private static extern bool AllowSetForegroundWindow(int processId);

	public static bool Enabled => ServiceProvider.Settings.UseAutoSizing;

	private static string PipeName => "CloriumAutoSizing." + Regex.Replace(Environment.GetEnvironmentVariable("USERNAME") ?? Environment.UserName ?? "user", "[^A-Za-z0-9_.-]", "_");

	public static string ExecutablePath
	{
		get
		{
			string path = ServiceProvider.Settings.AutoSizingPath;
			return string.IsNullOrWhiteSpace(path) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CloriumAutoSizing", ExecutableName) : path.Trim().Trim('"');
		}
	}

	public static bool IsPhoto(string file) => Array.IndexOf(PhotoExtensions, Path.GetExtension(file ?? "").ToLowerInvariant()) >= 0;

	// What the rail button opens for the photo selected in G2:
	// its measurement copy (PR stripped, as WriteMetadata names it) > the photo itself > its 치수측정 folder > its folder > nothing.
	public static string ResolveTarget(string selected)
	{
		if (string.IsNullOrWhiteSpace(selected)) return null;
		try
		{
			string folder = Path.GetDirectoryName(Path.GetFullPath(selected));
			if (string.IsNullOrEmpty(folder)) return null;
			bool insideMeasure = string.Equals(Path.GetFileName(folder), MeasureFolder, StringComparison.OrdinalIgnoreCase);
			string measureFolder = insideMeasure ? folder : Path.Combine(folder, MeasureFolder);
			if (!insideMeasure)
			{
				string measure = Path.Combine(measureFolder, Regex.Replace(Path.GetFileNameWithoutExtension(selected), "_PR(?=_[FB]$)", "") + "_M.jpeg");
				if (File.Exists(measure)) return measure;
			}
			if (File.Exists(selected) && IsPhoto(selected)) return Path.GetFullPath(selected);
			if (Directory.Exists(measureFolder)) return measureFolder;
			return Directory.Exists(folder) ? folder : null;
		}
		catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is IOException)
		{
			return null;
		}
	}

	public static string BuildRequest(string target)
	{
		if (target != null && File.Exists(target)) return JsonSerializer.Serialize(new { command = "open", path = target });
		if (target != null && Directory.Exists(target)) return JsonSerializer.Serialize(new { command = "browse", path = target });
		return "{\"command\":\"show\"}";
	}

	public static bool IsRunning() => Send("{\"command\":\"ping\"}", 300, 2000)?.GetProperty("ok").GetBoolean() == true;

	// Rail button (UI thread). Returns the task that finishes with Desk's answer (null = not reached) for logging/tests.
	public static Task<JsonElement?> Open(string target)
	{
		if (!Enabled) return Task.FromResult<JsonElement?>(null);
		string request = BuildRequest(target);
		if (IsRunning()) return Task.Run(() => Deliver(request));
		if (!Launch(target, showErrors: true, out bool started)) return Task.FromResult<JsonElement?>(null);
		if (started) return Task.FromResult<JsonElement?>(null); // Desk opens the path from its command line.
		// Launched moments ago and still starting: hand this request over once its pipe is up.
		return Task.Run(() =>
		{
			DateTime deadline = DateTime.Now.AddSeconds(20);
			while (!IsRunning())
			{
				if (DateTime.Now > deadline) return (JsonElement?)null;
				Thread.Sleep(300);
			}
			return Deliver(request);
		});
	}

	private static JsonElement? Deliver(string request)
	{
		AllowSetForegroundWindow(-1);
		// Desk may show its unsaved-work prompt before answering; wait for the operator, not for a timer.
		JsonElement? response = Send(request, 1000, 120000);
		Log.Debug("AutoSizing : " + request + " -> " + (response?.GetRawText() ?? "no answer"));
		return response;
	}

	private static bool Launch(string target, bool showErrors, out bool started)
	{
		started = false;
		string path = ExecutablePath;
		if (!File.Exists(path))
		{
			Log.Error("AutoSizing : executable not found " + path);
			if (showErrors)
			{
				MessageBox.Show("클로리움 오토사이징 프로그램을 찾을 수 없습니다.\n\n" + path + "\n\n클로리움 설정 > 오토사이징에서 실행 파일 경로를 확인해 주세요.", "오토사이징", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			return false;
		}
		lock (typeof(CloriumAutoSizing))
		{
			// Desk forwards a second start to the first window, but a start in progress has no pipe yet.
			if ((DateTime.Now - _lastLaunch).TotalSeconds < 10) return true;
			_lastLaunch = DateTime.Now;
		}
		try
		{
			ProcessStartInfo start = new ProcessStartInfo(path) { WorkingDirectory = Path.GetDirectoryName(path), UseShellExecute = true };
			if (target != null)
			{
				string argument = target.Length > 3 ? target.TrimEnd('\\') : target;
				start.Arguments = "\"" + argument + "\"";
			}
			Process.Start(start);
			started = true;
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("AutoSizing : unable to start " + path, ex);
			if (showErrors)
			{
				MessageBox.Show("클로리움 오토사이징 프로그램을 시작하지 못했습니다.\n\n" + ex.Message, "오토사이징", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			return false;
		}
	}

	private static JsonElement? Send(string request, int connectMs, int responseMs)
	{
		try
		{
			using (NamedPipeClientStream pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut))
			{
				pipe.Connect(connectMs);
				pipe.ReadMode = PipeTransmissionMode.Message;
				byte[] data = Encoding.UTF8.GetBytes(request);
				pipe.Write(data, 0, data.Length);
				pipe.Flush();
				MemoryStream message = new MemoryStream();
				Task read = Task.Run(() =>
				{
					byte[] buffer = new byte[4096];
					do
					{
						int count = pipe.Read(buffer, 0, buffer.Length);
						if (count == 0) break;
						message.Write(buffer, 0, count);
					}
					while (!pipe.IsMessageComplete);
				});
				if (!read.Wait(responseMs) || message.Length == 0) return null;
				using (JsonDocument document = JsonDocument.Parse(message.ToArray()))
				{
					return document.RootElement.Clone();
				}
			}
		}
		catch (Exception ex) when (ex is TimeoutException || ex is IOException || ex is JsonException || ex is AggregateException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
		{
			return null;
		}
	}
}
