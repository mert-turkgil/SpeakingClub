using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SpeakingClub.Services
{
    /// <summary>
    /// Background service that periodically cleans up temporary files
    /// (uploaded images, GIFs, videos that were abandoned during blog/quiz creation)
    /// </summary>
    public class TempFileCleanupService : BackgroundService
    {
        private readonly ILogger<TempFileCleanupService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IServiceScopeFactory _scopes;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run every hour
        private readonly TimeSpan _fileMaxAge = TimeSpan.FromHours(24); // Delete files older than 24 hours

        public TempFileCleanupService(
            ILogger<TempFileCleanupService> logger,
            IWebHostEnvironment env,
            IServiceScopeFactory scopes)
        {
            _logger = logger;
            _env = env;
            _scopes = scopes;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Temp File Cleanup Service started.");

            // Run initial cleanup on startup
            await CleanupTempFilesAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    await CleanupTempFilesAsync();
                }
                catch (OperationCanceledException)
                {
                    // Expected when service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in temp file cleanup service");
                }
            }

            _logger.LogInformation("Temp File Cleanup Service stopped.");
        }

        private Task CleanupTempFilesAsync()
        {
            try
            {
                string tempPath = Path.Combine(_env.WebRootPath, "temp");

                if (!Directory.Exists(tempPath))
                {
                    _logger.LogDebug("Temp directory does not exist: {Path}", tempPath);
                    return Task.CompletedTask;
                }

                var files = Directory.GetFiles(tempPath);
                var cutoffTime = DateTime.Now - _fileMaxAge;
                int deletedCount = 0;
                long freedBytes = 0;

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastWriteTime < cutoffTime)
                        {
                            freedBytes += fileInfo.Length;
                            File.Delete(file);
                            deletedCount++;
                            _logger.LogDebug("Deleted old temp file: {File}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temp file: {File}", file);
                    }
                }

                if (deletedCount > 0)
                {
                    _logger.LogInformation(
                        "Temp file cleanup completed: {Count} files deleted, {Size:N0} bytes freed",
                        deletedCount,
                        freedBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during temp file cleanup");
            }

            CleanupOldLogs();
            return CleanupEmailRecordsAsync();
        }

        // E-mail records (privacy page): the send log is kept for 180 days; sent outbox rows (they hold the e-mail's
        // content for rendering) for 30 days. Unsent ones stay until they are sent or given up.
        private async Task CleanupEmailRecordsAsync()
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SpeakingClub.Data.SpeakingClubContext>();
                var logCutoff = DateTime.UtcNow.AddDays(-180);
                var outboxCutoff = DateTime.UtcNow.AddDays(-30);
                var logs = await db.EmailLogs.Where(l => l.CreatedAt < logCutoff).ExecuteDeleteAsync();
                var outbox = await db.EmailOutbox.Where(o => o.ProcessedAt != null && o.ProcessedAt < outboxCutoff).ExecuteDeleteAsync();
                if (logs + outbox > 0)
                    _logger.LogInformation("E-mail records cleaned up: {Logs} log rows, {Outbox} outbox rows", logs, outbox);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during e-mail record cleanup");
            }
        }

        // Server logs (logs/stdout_*.log written by IIS) can contain IP addresses. The privacy policy says they are
        // kept for at most 30 days, so older log files are deleted here.
        private void CleanupOldLogs()
        {
            try
            {
                var logPath = Path.Combine(_env.ContentRootPath, "logs");
                if (!Directory.Exists(logPath))
                    return;

                var cutoff = DateTime.Now.AddDays(-30);
                foreach (var file in Directory.GetFiles(logPath, "*.log"))
                {
                    try
                    {
                        if (File.GetLastWriteTime(file) < cutoff)
                            File.Delete(file);
                    }
                    catch (IOException)
                    {
                        // The log currently being written is locked; it is young anyway
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during log cleanup");
            }
        }
    }
}
