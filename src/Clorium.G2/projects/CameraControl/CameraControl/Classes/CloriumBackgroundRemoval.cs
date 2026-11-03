using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CameraControl.Core;
using CameraControl.Devices;

namespace CameraControl.Classes;

// G2 ↔ Clorium 배경제거. The engine is the local Matting Studio server (BiRefNet Flask app on http://127.0.0.1:8765);
// the screen is G2's own window (windows/G2BackgroundRemovalWnd), no browser. The rail button starts the server with its
// own Start.ps1 -NoBrowser when it is not running, then opens the window with the selected photo (or its folder) as the
// target: the window only scans it (nothing is written) and the operator presses 배경 제거 시작. The window drives the
// server's batch API (/api/batch/scan|start|<id>|cancel). Results go next to the source in *_bgr folders; originals are
// never changed. Never throws into G2.
internal static class CloriumBackgroundRemoval
{
	public const string StudioUrl = "http://127.0.0.1:8765";

	private static readonly string[] PhotoExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".tif", ".tiff", ".bmp" };

	private static readonly SemaphoreSlim StartGate = new SemaphoreSlim(1, 1);

	private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

	private static string _token;

	// Replaced by tests; the default shows the G2 배경제거 window ("" = no target).
	internal static Action<string> ShowStudio = target => Application.Current.Dispatcher.Invoke(() => CameraControl.windows.G2BackgroundRemovalWnd.ShowFor(target));

	public static bool Enabled => ServiceProvider.Settings.UseBackgroundRemoval;

	public static string ScriptPath
	{
		get
		{
			string path = ServiceProvider.Settings.BackgroundRemovalPath;
			path = string.IsNullOrWhiteSpace(path) ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CloriumBackgroundRemoval", "outputs", "matting-studio", "Start.ps1") : path.Trim().Trim('"');
			// Start.cmd only wraps Start.ps1 (and would open its own browser tab).
			return string.Equals(Path.GetExtension(path), ".cmd", StringComparison.OrdinalIgnoreCase) ? Path.ChangeExtension(path, ".ps1") : path;
		}
	}

	public static bool IsPhoto(string file) => Array.IndexOf(PhotoExtensions, Path.GetExtension(file ?? "").ToLowerInvariant()) >= 0;

	// What the rail button sends for the photo selected in G2: a supported photo itself, else its folder
	// (e.g. RAW selected: the session folder, whose JPEGs the studio lists), else nothing (studio home).
	public static string ResolveTarget(string selected)
	{
		if (string.IsNullOrWhiteSpace(selected)) return null;
		try
		{
			string full = Path.GetFullPath(selected);
			if (File.Exists(full) && IsPhoto(full)) return full;
			string folder = Path.GetDirectoryName(full);
			return !string.IsNullOrEmpty(folder) && Directory.Exists(folder) ? folder : null;
		}
		catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException || ex is IOException)
		{
			return null;
		}
	}

	// What the window gets: an existing supported photo, else an existing folder, else "" (nothing selected).
	public static string NormalizeTarget(string target)
	{
		if (string.IsNullOrWhiteSpace(target)) return "";
		// A file the engine cannot read (RAW, video...) is sent as its folder.
		if (File.Exists(target) && !IsPhoto(target)) target = Path.GetDirectoryName(Path.GetFullPath(target));
		return File.Exists(target) || Directory.Exists(target) ? target : "";
	}

	// Rail button. Finishes with the target shown ("" = window without a target; null = disabled, not started or
	// failed; the operator was told why).
	public static Task<string> Open(string target)
	{
		if (!Enabled) return Task.FromResult<string>(null);
		string shown = NormalizeTarget(target);
		return Task.Run(() =>
		{
			if (!IsRunning() && !Start()) return null;
			try
			{
				ShowStudio(shown);
				Log.Debug("BackgroundRemoval : window for " + (shown.Length == 0 ? "(none)" : shown));
				return shown;
			}
			catch (Exception ex)
			{
				Log.Error("BackgroundRemoval : unable to show the window", ex);
				ShowError("배경제거 화면을 열지 못했습니다.\n\n" + ex.Message);
				return null;
			}
		});
	}

	// ---- engine API for G2BackgroundRemovalWnd. Calls block: run them off the UI thread. Failures throw
	// ApiException with a message for the operator. ----

	public sealed class ApiException : Exception
	{
		public ApiException(string message) : base(message) { }
	}

	// GET /api/health: model ready + its loading message.
	public static (bool Ready, string Message) Health()
	{
		JsonElement health = Request(HttpMethod.Get, "/api/health", null);
		if (health.TryGetProperty("error", out JsonElement error) && error.ValueKind == JsonValueKind.String) return (false, error.GetString());
		bool ready = health.TryGetProperty("ready", out JsonElement r) && r.ValueKind == JsonValueKind.True;
		return (ready, health.TryGetProperty("message", out JsonElement m) && m.ValueKind == JsonValueKind.String ? m.GetString() : "");
	}

	// Lists the photos and their *_bgr destinations; nothing is written.
	public static JsonElement Scan(string[] paths, bool folder)
	{
		object body = folder ? (object)new { mode = "folder", path = paths[0] } : new { mode = "file", path = paths[0], paths };
		return Request(HttpMethod.Post, "/api/batch/scan", body);
	}

	public static JsonElement StartBatch(string planId, int resolution, int edgeStrength, bool opaqueProduct, bool detailRecovery)
	{
		return Request(HttpMethod.Post, "/api/batch/start", new { plan_id = planId, resolution, edge_strength = edgeStrength, opaque_product = opaqueProduct, detail_recovery = detailRecovery });
	}

	public static JsonElement BatchStatus(string jobId) => Request(HttpMethod.Get, "/api/batch/" + Uri.EscapeDataString(jobId), null);

	public static JsonElement CancelBatch(string jobId) => Request(HttpMethod.Post, "/api/batch/" + Uri.EscapeDataString(jobId) + "/cancel", new { });

	private static JsonElement Request(HttpMethod method, string path, object body)
	{
		// POST needs the per-run token the server writes into its page; a restarted server has a new one (403 once).
		for (int attempt = 0; ; attempt++)
		{
			using (HttpRequestMessage request = new HttpRequestMessage(method, StudioUrl + path))
			{
				if (body != null)
				{
					request.Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
					request.Headers.Add("X-Studio-Token", Token(attempt > 0));
				}
				(int status, string text) = Send(request);
				if (status == 403 && body != null && attempt == 0) continue;
				JsonElement json;
				try
				{
					using (JsonDocument document = JsonDocument.Parse(text)) json = document.RootElement.Clone();
				}
				catch (JsonException)
				{
					throw new ApiException("배경제거 엔진의 응답을 읽지 못했습니다. (HTTP " + status + ")");
				}
				if (status >= 200 && status < 300) return json;
				throw new ApiException(json.ValueKind == JsonValueKind.Object && json.TryGetProperty("error", out JsonElement error) && error.ValueKind == JsonValueKind.String ? error.GetString() : "배경제거 엔진 오류 (HTTP " + status + ")");
			}
		}
	}

	private static string Token(bool refresh)
	{
		if (_token != null && !refresh) return _token;
		using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, StudioUrl + "/"))
		{
			Match match = Regex.Match(Send(request).Text, "const token='([^']+)'");
			if (!match.Success) throw new ApiException("배경제거 엔진의 응답을 확인하지 못했습니다.");
			return _token = match.Groups[1].Value;
		}
	}

	private static (int Status, string Text) Send(HttpRequestMessage request)
	{
		try
		{
			using (HttpResponseMessage response = Http.SendAsync(request).Result)
			{
				return ((int)response.StatusCode, response.Content.ReadAsStringAsync().Result);
			}
		}
		catch (Exception ex) when (ex is AggregateException || ex is HttpRequestException || ex is TaskCanceledException || ex is InvalidOperationException)
		{
			throw new ApiException("배경제거 엔진에 연결하지 못했습니다. 엔진이 종료됐다면 G2의 배경제거 버튼을 다시 눌러 주세요.");
		}
	}

	public static bool IsRunning() => Probe() == true;

	// true = Matting Studio answers, false = another program answers on the port, null = nothing listening.
	// While the studio loads its model the health answer can take several seconds: a connection that is accepted
	// but not answered in time is asked again (up to ~20 s) instead of being taken for "not running".
	private static bool? Probe()
	{
		for (int attempt = 0; ; attempt++)
		{
			try
			{
				using (CancellationTokenSource timeout = new CancellationTokenSource(attempt == 0 ? 2000 : 6000))
				using (HttpResponseMessage response = Http.GetAsync(StudioUrl + "/api/health", timeout.Token).Result)
				{
					try
					{
						using (JsonDocument health = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result))
						{
							return health.RootElement.ValueKind == JsonValueKind.Object && health.RootElement.TryGetProperty("app", out JsonElement app) && app.ValueKind == JsonValueKind.String && app.GetString() == "matting-studio";
						}
					}
					catch (JsonException)
					{
						return false;
					}
				}
			}
			catch (Exception ex) when (ex is AggregateException || ex is HttpRequestException || ex is TaskCanceledException || ex is InvalidOperationException)
			{
				bool slow = ex is TaskCanceledException || ex.InnerException is TaskCanceledException;
				if (!slow || attempt >= 3 || !Listening()) return null;
			}
		}
	}

	private static bool Listening()
	{
		try
		{
			using (System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient())
			{
				return client.ConnectAsync("127.0.0.1", 8765).Wait(1000) && client.Connected;
			}
		}
		catch (Exception ex) when (ex is AggregateException || ex is System.Net.Sockets.SocketException || ex is InvalidOperationException)
		{
			return false;
		}
	}

	// One start at a time: a second click while the studio is starting waits for it instead of starting another server.
	private static bool Start()
	{
		if (!StartGate.Wait(90000)) return false;
		try
		{
			bool? probe = Probe();
			if (probe == true) return true;
			if (probe == false)
			{
				Log.Error("BackgroundRemoval : port 8765 used by another program");
				ShowError("다른 프로그램이 배경제거 주소(" + StudioUrl + ")를 사용하고 있어 시작할 수 없습니다.\n\n그 프로그램을 닫은 뒤 다시 눌러 주세요.");
				return false;
			}
			string script = ScriptPath;
			if (!File.Exists(script))
			{
				Log.Error("BackgroundRemoval : script not found " + script);
				ShowError("클로리움 배경제거(Matting Studio) 실행 스크립트를 찾을 수 없습니다.\n\n" + script + "\n\n클로리움 설정 > 배경제거에서 Start.ps1 경로를 확인해 주세요.");
				return false;
			}
			// No redirected pipes: the server Start.ps1 launches would inherit them and keep them open. The failure
			// reason comes back through a temporary file instead.
			string reasonFile = Path.Combine(Path.GetTempPath(), "clorium-bgremoval-" + Guid.NewGuid().ToString("N") + ".txt");
			string command = "try { $global:LASTEXITCODE = 0; & '" + script.Replace("'", "''") + "' -NoBrowser | Out-Null; if ($LASTEXITCODE) { throw ('Start.ps1 exit ' + $LASTEXITCODE) }; exit 0 } catch { $_.Exception.Message | Set-Content -LiteralPath '" + reasonFile.Replace("'", "''") + "' -Encoding UTF8; exit 1 }";
			ProcessStartInfo start = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"" + command.Replace("\"", "\\\"") + "\"")
			{
				WorkingDirectory = Path.GetDirectoryName(script),
				UseShellExecute = false,
				CreateNoWindow = true
			};
			try
			{
				using (Process process = Process.Start(start))
				{
					// Start.ps1 waits up to 15 s for /api/health; the model keeps loading in the background afterwards.
					if (!process.WaitForExit(60000))
					{
						Log.Error("BackgroundRemoval : Start.ps1 did not finish");
						ShowError("클로리움 배경제거가 60초 안에 시작되지 않았습니다.");
						return false;
					}
					if (process.ExitCode != 0 || !IsRunning())
					{
						string reason = (File.Exists(reasonFile) ? FirstLine(File.ReadAllText(reasonFile)) : null) ?? ("exit " + process.ExitCode);
						Log.Error("BackgroundRemoval : start failed - " + reason);
						ShowError("클로리움 배경제거를 시작하지 못했습니다.\n\n" + reason);
						return false;
					}
				}
			}
			finally
			{
				try { File.Delete(reasonFile); } catch (IOException) { } catch (UnauthorizedAccessException) { }
			}
			Log.Debug("BackgroundRemoval : studio started");
			return true;
		}
		catch (Exception ex)
		{
			Log.Error("BackgroundRemoval : unable to start", ex);
			ShowError("클로리움 배경제거를 시작하지 못했습니다.\n\n" + ex.Message);
			return false;
		}
		finally
		{
			StartGate.Release();
		}
	}

	private static string FirstLine(string text)
	{
		if (string.IsNullOrWhiteSpace(text)) return null;
		foreach (string line in text.Split('\n'))
		{
			if (!string.IsNullOrWhiteSpace(line)) return line.Trim();
		}
		return null;
	}

	private static void ShowError(string message)
	{
		Action show = () => MessageBox.Show(message, "배경제거", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		var dispatcher = Application.Current?.Dispatcher;
		if (dispatcher != null && !dispatcher.CheckAccess()) dispatcher.Invoke(show);
		else show();
	}
}
