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
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run every hour
        private readonly TimeSpan _fileMaxAge = TimeSpan.FromHours(24); // Delete files older than 24 hours

        public TempFileCleanupService(
            ILogger<TempFileCleanupService> logger,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
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

            return Task.CompletedTask;
        }
    }
}
