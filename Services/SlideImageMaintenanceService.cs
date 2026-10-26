using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpeakingClub.Data;

namespace SpeakingClub.Services
{
    /// <summary>
    /// Once, shortly after the site starts: makes sure every home page slide has a main photo of at most
    /// 1920 px and real 600 / 1200 px copies. Slides uploaded before the admin resized photos (AdminController,
    /// ImageOptimizer) still pointed at 4000 px camera files, so phones downloaded 500–800 KB per slide.
    /// The original files are left in wwwroot/img; only the slide's references change. Failures are logged
    /// with the reason (for example SkiaSharp's native library missing on the server).
    /// </summary>
    public class SlideImageMaintenanceService : BackgroundService
    {
        private const int MainWidth = 1920;          // same sizes as AdminController
        private static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(20);

        private readonly IServiceScopeFactory _scopes;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SlideImageMaintenanceService> _logger;

        public SlideImageMaintenanceService(IServiceScopeFactory scopes, IWebHostEnvironment env, ILogger<SlideImageMaintenanceService> logger)
        {
            _scopes = scopes;
            _env = env;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(StartDelay, stoppingToken);   // let the site answer its first requests first
                await FixSlidesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Slide image check failed");
            }
        }

        private async Task FixSlidesAsync(CancellationToken ct)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SpeakingClubContext>();
            var folder = Path.Combine(_env.WebRootPath, "img");
            var slides = await db.Slide.ToListAsync(ct);
            var changed = 0;

            foreach (var slide in slides)
            {
                var main = slide.CarouselImage;
                var mainWidth = Width(folder, main);
                if (mainWidth == null) continue;   // missing or unreadable: logged in Width

                var mainChanged = false;
                if (mainWidth > MainWidth * 1.25)
                {
                    var smaller = Resize(folder, main, MainWidth);
                    if (smaller == null) continue;
                    slide.CarouselImage = smaller;
                    mainChanged = true;
                }

                var phone = Copy(folder, slide.CarouselImage, slide.CarouselImage600w, 600, mainChanged);
                var tablet = Copy(folder, slide.CarouselImage, slide.CarouselImage1200w, 1200, mainChanged);
                if (phone != null) slide.CarouselImage600w = phone;
                if (tablet != null) slide.CarouselImage1200w = tablet;

                if (mainChanged || phone != null || tablet != null)
                {
                    changed++;
                    _logger.LogInformation("Slide {Id}: photos resized (main {Main}, 600 px {Phone}, 1200 px {Tablet})",
                        slide.SlideId, slide.CarouselImage, slide.CarouselImage600w, slide.CarouselImage1200w);
                }
            }

            if (changed > 0)
                await db.SaveChangesAsync(ct);
            _logger.LogInformation("Slide image check: {Changed} of {Total} slides updated", changed, slides.Count);
        }

        // A new copy when the current one is missing, much too wide (e.g. a 4000 px photo stored as "600 px")
        // or the main photo was just replaced; otherwise null (keep the current copy).
        private string? Copy(string folder, string main, string? current, int width, bool mainChanged)
        {
            var needed = mainChanged
                || string.IsNullOrEmpty(current)
                || !File.Exists(Path.Combine(folder, current))
                || Width(folder, current) is int w && w > width * 1.25;
            return needed ? Resize(folder, main, width) : null;
        }

        private int? Width(string folder, string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            var path = Path.Combine(folder, fileName);
            if (!File.Exists(path))
            {
                _logger.LogWarning("Slide photo {Name} is missing", fileName);
                return null;
            }
            var width = ImageOptimizer.GetWidth(path);
            if (width == null)
                _logger.LogWarning(ImageOptimizer.LastError, "Slide photo {Name} could not be read", fileName);
            return width;
        }

        private string? Resize(string folder, string source, int width)
        {
            var fileName = $"{Guid.NewGuid()}.jpg";
            if (ImageOptimizer.TrySaveJpeg(Path.Combine(folder, source), Path.Combine(folder, fileName), width))
                return fileName;
            _logger.LogWarning(ImageOptimizer.LastError, "Slide photo {Name} could not be resized to {Width} px", source, width);
            return null;
        }
    }
}
