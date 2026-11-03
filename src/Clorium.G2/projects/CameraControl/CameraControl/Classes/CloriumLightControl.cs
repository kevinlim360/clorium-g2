using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CameraControl.Core;
using CameraControl.Devices;

namespace CameraControl.Classes;

// G2 ↔ Clorium Light Control (Godox Bluetooth app). Replaces the removed DMX/ArtNet controller.
// The light app owns Bluetooth, registrations and sequence numbers; G2 only asks it, over the
// app's per-user local named pipe, to show its window or power every registered light on/off.
// Protocol (light app work/app/control_server.py): one UTF-8 JSON request and one JSON response
// per connection, message-mode pipe. Nothing here throws into capture code.
internal static class CloriumLightControl
{
	public const string ExecutableName = "CloriumLightControl.exe";

	private static DateTime _lastLaunch = DateTime.MinValue;

	private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

	private static int _generation;

	public static bool Enabled => ServiceProvider.Settings.UseCloriumLight;

	private static string PipeName => "CloriumLightControl." + Regex.Replace(Environment.GetEnvironmentVariable("USERNAME") ?? Environment.UserName ?? "user", "[^A-Za-z0-9_.-]", "_");

	public static string ExecutablePath
	{
		get
		{
			string path = ServiceProvider.Settings.CloriumLightPath;
			return string.IsNullOrWhiteSpace(path) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CloriumLightControl", ExecutableName) : path.Trim().Trim('"');
		}
	}

	public static bool IsRunning() => Send("{\"command\":\"ping\"}", 300, 2000)?.GetProperty("ok").GetBoolean() == true;

	// G2 startup: start the light app if needed, then turn the lights on (as the DMX controller did).
	public static void EnsureStarted()
	{
		if (!Enabled) return;
		Task.Run(() =>
		{
			if (!IsRunning())
			{
				if (!Launch(showErrors: false)) return;
				DateTime deadline = DateTime.Now.AddSeconds(30);
				while (!IsRunning())
				{
					if (DateTime.Now > deadline) return;
					Thread.Sleep(500);
				}
			}
			ApplyTheme(ServiceProvider.Settings.UseDarkScreen);
			PowerAsync(true, 30000).Wait();
		});
	}

	// Rail button: bring the running app forward, or start it.
	public static void OpenWindow()
	{
		if (Send("{\"command\":\"show\"}", 300, 3000)?.GetProperty("ok").GetBoolean() == true)
		{
			ApplyTheme(ServiceProvider.Settings.UseDarkScreen);
			return;
		}
		if (Launch(showErrors: true)) ApplyThemeWhenReady();
	}

	// 클로리움 설정 > 테마 (밝은/어두운 화면) for the light app window. A light app without the theme command (before
	// 0.7.11) answers "unknown command"; that is only logged.
	public static void ApplyTheme(bool dark)
	{
		if (Enabled) ApplyThemeAsync(dark);
	}

	internal static Task<bool> ApplyThemeAsync(bool dark)
	{
		return Task.Run(() =>
		{
			JsonElement? response = Send("{\"command\":\"theme\",\"dark\":" + (dark ? "true" : "false") + "}", 300, 5000);
			Log.Debug("CloriumLight : theme " + (dark ? "dark" : "light") + " -> " + (response?.GetRawText() ?? "not reachable"));
			return response != null && response.Value.TryGetProperty("ok", out JsonElement ok) && ok.ValueKind == JsonValueKind.True;
		});
	}

	// After a launch: wait for the control pipe, then send the theme.
	private static void ApplyThemeWhenReady()
	{
		Task.Run(() =>
		{
			DateTime deadline = DateTime.Now.AddSeconds(30);
			while (!IsRunning())
			{
				if (DateTime.Now > deadline) return;
				Thread.Sleep(500);
			}
			ApplyTheme(ServiceProvider.Settings.UseDarkScreen);
		});
	}

	// Power every registered light. Returns false (and logs) on any failure or timeout.
	// Requests run one at a time and the latest wins: an "off" after a shot that is still queued when the
	// next shot asks for "on" is dropped, and a busy light app (still sending the previous command) is retried.
	public static Task<bool> PowerAsync(bool on, int timeoutMs = 15000)
	{
		if (!Enabled) return Task.FromResult(false);
		int generation = Interlocked.Increment(ref _generation);
		return Task.Run(() =>
		{
			string name = "CloriumLight : power " + (on ? "on" : "off");
			DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
			if (!Gate.Wait(timeoutMs)) return false;
			try
			{
				while (true)
				{
					if (generation != Volatile.Read(ref _generation))
					{
						Log.Debug(name + " - superseded by a newer request");
						return false;
					}
					int remaining = (int)(deadline - DateTime.Now).TotalMilliseconds;
					if (remaining < 500) return false;
					string request = "{\"command\":\"power\",\"on\":" + (on ? "true" : "false") + ",\"timeout\":" + Math.Max(1, remaining / 1000) + "}";
					JsonElement? response = Send(request, 1000, remaining + 1000);
					if (response == null)
					{
						Log.Debug(name + " - light app not reachable");
						return false;
					}
					if (response.Value.TryGetProperty("error", out JsonElement error) && error.ValueKind == JsonValueKind.String && error.GetString() == "busy")
					{
						Thread.Sleep(300);
						continue;
					}
					Log.Debug(name + " -> " + response.Value.GetRawText());
					return response.Value.TryGetProperty("ok", out JsonElement ok) && ok.ValueKind == JsonValueKind.True;
				}
			}
			finally
			{
				Gate.Release();
			}
		});
	}

	// Fire-and-forget for paths that must not wait (after capture, power save, buttons).
	public static void Power(bool on)
	{
		if (Enabled) PowerAsync(on);
	}

	private static bool Launch(bool showErrors)
	{
		string path = ExecutablePath;
		if (!File.Exists(path))
		{
			Log.Error("CloriumLight : executable not found " + path);
			if (showErrors)
			{
				MessageBox.Show("클로리움 조명 제어 프로그램을 찾을 수 없습니다.\n\n" + path + "\n\n클로리움 설정 > 확장 기능 > 조명 조절에서 실행 파일 경로를 확인해 주세요.", "조명 조절", MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}
			return false;
		}
		lock (typeof(CloriumLightControl))
		{
			// The light app allows one instance; a second start shows its own warning box.
			if ((DateTime.Now - _lastLaunch).TotalSeconds < 10) return true;
			_lastLaunch = DateTime.Now;
		}
		try
		{
			Process.Start(new ProcessStartInfo(path) { WorkingDirectory = Path.GetDirectoryName(path), UseShellExecute = true });
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("CloriumLight : unable to start " + path, ex);
			if (showErrors)
			{
				MessageBox.Show("클로리움 조명 제어 프로그램을 시작하지 못했습니다.\n\n" + ex.Message, "조명 조절", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
