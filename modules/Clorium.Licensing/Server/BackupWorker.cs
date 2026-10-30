namespace Clorium.LicenseServer;

public class BackupWorker(Store store, ILogger<BackupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                string folder = store.BackupFolder; Directory.CreateDirectory(folder); PrivateFiles.RestrictDirectory(folder);
                string path = Path.Combine(folder, "clorium-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".zip");
                PrivateFiles.Save(path, store.Backup());
                // Retain a month of daily snapshots. Do not delete arbitrary files in the backup directory.
                foreach (var file in new DirectoryInfo(folder).GetFiles("clorium-????????-??????.zip").OrderByDescending(x => x.Name).Skip(30)) file.Delete();
            } catch (Exception ex) { logger.LogError(ex, "Automatic license backup failed"); }
            try { await Task.Delay(TimeSpan.FromDays(1), stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
}
