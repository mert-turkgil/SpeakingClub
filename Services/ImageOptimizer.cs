using System;
using System.IO;
using SkiaSharp;

namespace SpeakingClub.Services
{
    /// <summary>
    /// Makes web-sized JPEG copies of uploaded photos (SkiaSharp). Phone photos and camera files are often
    /// 4000 px wide and several MB; the home page slider needs 600 / 1200 / 1920 px versions of a few hundred KB.
    /// Respects the EXIF orientation, flattens transparency onto white and never enlarges an image.
    /// </summary>
    public static class ImageOptimizer
    {
        public const int JpegQuality = 80;

        /// <summary>Width in pixels of an image file, or null if it can't be read.</summary>
        public static int? GetWidth(string path)
        {
            try
            {
                using var codec = SKCodec.Create(path);
                if (codec == null) return null;
                var swap = SwapsSides(codec.EncodedOrigin);
                return swap ? codec.Info.Height : codec.Info.Width;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Writes a JPEG of <paramref name="source"/> no wider than <paramref name="maxWidth"/> to
        /// <paramref name="targetPath"/>. Returns false when the image can't be decoded (the caller keeps the original).
        /// </summary>
        public static bool TrySaveJpeg(Stream source, string targetPath, int maxWidth)
        {
            try
            {
                using var codec = SKCodec.Create(source);
                if (codec == null) return false;
                using var decoded = SKBitmap.Decode(codec);
                if (decoded == null) return false;
                using var upright = Orient(decoded, codec.EncodedOrigin);

                var width = Math.Min(maxWidth, upright.Width);
                var height = Math.Max(1, (int)Math.Round(upright.Height * (width / (double)upright.Width)));

                using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);
                using (var image = SKImage.FromBitmap(upright))
                {
                    // Mipmapped sampling keeps strong downscales (4000 → 600 px) smooth instead of jagged
                    canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                }
                canvas.Flush();

                using var snapshot = surface.Snapshot();
                using var data = snapshot.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
                if (data == null) return false;
                using var output = File.Create(targetPath);
                data.SaveTo(output);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TrySaveJpeg(string sourcePath, string targetPath, int maxWidth)
        {
            using var stream = File.OpenRead(sourcePath);
            return TrySaveJpeg(stream, targetPath, maxWidth);
        }

        private static bool SwapsSides(SKEncodedOrigin origin) =>
            origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        // Turns a camera photo the right way up (EXIF orientation 3, 6 and 8; the mirrored ones are rare)
        private static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
        {
            if (origin is not (SKEncodedOrigin.BottomRight or SKEncodedOrigin.RightTop or SKEncodedOrigin.LeftBottom))
                return source.Copy();

            var swap = SwapsSides(origin);
            var result = new SKBitmap(swap ? source.Height : source.Width, swap ? source.Width : source.Height);
            using var canvas = new SKCanvas(result);
            switch (origin)
            {
                case SKEncodedOrigin.BottomRight:   // 180°
                    canvas.RotateDegrees(180, result.Width / 2f, result.Height / 2f);
                    break;
                case SKEncodedOrigin.RightTop:      // 90° clockwise
                    canvas.Translate(result.Width, 0);
                    canvas.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.LeftBottom:    // 90° counter-clockwise
                    canvas.Translate(0, result.Height);
                    canvas.RotateDegrees(270);
                    break;
            }
            canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest), null);  // a pure rotation: no resampling needed
            return result;
        }
    }
}
