using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace CameraControl.Core.Classes;

// New offline engine bridge. It does not call the disabled legacy capture path.
public static class CloriumStackProcess
{
    public static string DescribeProgress(string line)
    {
        if (!line.StartsWith("{")) return line;
        try { return (string)JObject.Parse(line)["message"] ?? "합성 처리 중"; }
        catch (Newtonsoft.Json.JsonException) { return "합성 처리 중"; }
    }

    private static string Quote(string value)
    {
        // Windows filenames cannot contain double quotes; reject unexpected CLI data.
        if (value.IndexOf('"') >= 0) throw new ArgumentException("Invalid path");
        return "\"" + value + "\"";
    }

    public static Task<string> RunAsync(string python, string engineScript,
        IEnumerable<string> images, string output, IProgress<string> progress,
        CancellationToken cancellationToken)
    {
        string[] inputs = images.Select(Path.GetFullPath).ToArray();
        if (inputs.Length < 2) throw new ArgumentException("At least two focus images are required");
        output = Path.GetFullPath(output);
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            string cancelFile = Path.Combine(Path.GetDirectoryName(output), ".clorium-stack-cancel-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var arguments = new List<string> { Path.GetFullPath(engineScript) };
            arguments.AddRange(inputs);
            arguments.AddRange(new[] { "--output", output, "--cancel-file", cancelFile });
            var info = new ProcessStartInfo(Path.GetFullPath(python), string.Join(" ", arguments.Select(Quote)))
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            try
            {
                using (var process = new Process { StartInfo = info })
                {
                    process.Start();
                    Task<string> errors = process.StandardError.ReadToEndAsync();
                    using (cancellationToken.Register(() => File.WriteAllText(cancelFile, "cancel")))
                    {
                        string result = null;
                        string line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            progress?.Report(line);
                            if (line.StartsWith("{"))
                            {
                                JObject item = JObject.Parse(line);
                                if (item["output"] != null) result = (string)item["output"];
                            }
                        }
                        process.WaitForExit();
                        string error = errors.GetAwaiter().GetResult();
                        // A successfully saved output wins a late cancellation race.
                        if (process.ExitCode == 0 && result != null && File.Exists(result)) return result;
                        cancellationToken.ThrowIfCancellationRequested();
                        throw new InvalidOperationException("Clorium Stack failed (" + process.ExitCode + "): " + error);
                    }
                }
            }
            finally { if (File.Exists(cancelFile)) File.Delete(cancelFile); }
        });
    }
}
