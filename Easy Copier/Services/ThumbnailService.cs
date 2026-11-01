using Easy_Copier.Interop;
using Microsoft.Extensions.Logging;
using NReco.VideoConverter;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Service for extracting and caching file thumbnails using the Windows Shell API,
    /// WinRT StorageItemThumbnail fallback, and NReco FFMpeg fallback.
    /// </summary>
    public class ThumbnailService : IThumbnailService, IDisposable
    {
        /// <summary>
        /// Logger instance used for recording thumbnail extraction diagnostics and errors.
        /// </summary>
        private readonly ILogger<ThumbnailService> _logger;

        /// <summary>
        /// Limits concurrent thumbnail extraction tasks to 4 to prevent CPU, disk I/O,
        /// and thread pool saturation during bulk video library scans.
        /// </summary>
        private readonly SemaphoreSlim _extractionThrottle = new(4, 4);
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThumbnailService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ThumbnailService(ILogger<ThumbnailService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<string?> ExtractThumbnailAsync(string sourceFilePath, string cacheDirectoryPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return null;
            }

            if (!Directory.Exists(cacheDirectoryPath))
            {
                _ = Directory.CreateDirectory(cacheDirectoryPath);
            }

            string fileNameHash = ComputeHash(sourceFilePath);
            string cacheFilePath = Path.Combine(cacheDirectoryPath, $"{fileNameHash}.jpg");

            if (File.Exists(cacheFilePath))
            {
                return cacheFilePath;
            }

            // Await a slot in the concurrency throttle to prevent thread starvation and I/O thrashing.
            await _extractionThrottle.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Run extraction on thread pool workers rather than creating raw unbounded OS threads.
                // This eliminates unobserved async void crashes and properly propagates task cancellations.
                return await Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    bool shellExtractionSucceeded = false;

                    try
                    {
                        Guid shellItemGuid = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
                        FileOperationInterop.SHCreateItemFromParsingName(sourceFilePath, IntPtr.Zero, shellItemGuid, out IShellItem shellItem);

                        IShellItemImageFactory imageFactory = (IShellItemImageFactory)shellItem;

                        SIZE size = new() { cx = 200, cy = 280 };
                        SIIGBF flags = SIIGBF.SIIGBF_RESIZETOFIT;

                        int hr = imageFactory.GetImage(size, flags, out IntPtr hbitmap);
                        if (hr >= 0 && hbitmap != IntPtr.Zero)
                        {
                            try
                            {
                                using (Image image = Image.FromHbitmap(hbitmap))
                                {
                                    image.Save(cacheFilePath, ImageFormat.Jpeg);
                                }
                                shellExtractionSucceeded = true;
                                return cacheFilePath;
                            }
                            finally
                            {
                                _ = Gdi32Interop.DeleteObject(hbitmap);
                            }
                        }
                    }
                    catch (InvalidCastException)
                    {
                        _logger.LogInformation("File {FilePath} does not support IShellItemImageFactory. Falling back to StorageFile.GetThumbnailAsync...", sourceFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error during Shell extraction for {FilePath}. Falling back...", sourceFilePath);
                    }

                    if (!shellExtractionSucceeded)
                    {
                        try
                        {
                            Windows.Storage.StorageFile storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(sourceFilePath);
                            using Windows.Storage.FileProperties.StorageItemThumbnail thumbnail = await storageFile.GetThumbnailAsync(
                                Windows.Storage.FileProperties.ThumbnailMode.VideosView, 280);

                            if (thumbnail != null && thumbnail.Type == Windows.Storage.FileProperties.ThumbnailType.Image)
                            {
                                using (Stream stream = thumbnail.AsStream())
                                {
                                    using Image image = Image.FromStream(stream);
                                    image.Save(cacheFilePath, ImageFormat.Jpeg);
                                }
                                return cacheFilePath;
                            }
                            else
                            {
                                _logger.LogWarning("Fallback failed to extract a valid thumbnail for {FilePath}. Attempting NReco FFMpeg extraction...", sourceFilePath);
                                return ExtractWithFFMpeg(sourceFilePath, cacheFilePath);
                            }
                        }
                        catch (Exception winrtEx)
                        {
                            _logger.LogWarning(winrtEx, "WinRT Fallback error extracting thumbnail for {FilePath}. Attempting NReco FFMpeg extraction...", sourceFilePath);
                            return ExtractWithFFMpeg(sourceFilePath, cacheFilePath);
                        }
                    }

                    return null;
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "General error extracting thumbnail for {FilePath}", sourceFilePath);
                return null;
            }
            finally
            {
                _ = _extractionThrottle.Release();
            }
        }

        /// <summary>
        /// Extracts a frame from the video using NReco FFMpeg converter as the final fallback.
        /// </summary>
        /// <param name="sourceFilePath">The source video path.</param>
        /// <param name="cacheFilePath">The destination thumbnail image file path.</param>
        /// <returns>The cache file path if extraction succeeded; otherwise, <c>null</c>.</returns>
        private string? ExtractWithFFMpeg(string sourceFilePath, string cacheFilePath)
        {
            try
            {
                FFMpegConverter ffMpeg = new();
                using MemoryStream memoryStream = new();
                ffMpeg.GetVideoThumbnail(sourceFilePath, memoryStream, 5f);
                using (Image image = Image.FromStream(memoryStream))
                {
                    image.Save(cacheFilePath, ImageFormat.Jpeg);
                }
                return cacheFilePath;
            }
            catch (Exception ffmpegEx)
            {
                _logger.LogWarning(ffmpegEx, "NReco FFMpeg Fallback error extracting thumbnail for {FilePath}", sourceFilePath);
                return null;
            }
        }

        /// <summary>
        /// Computes an uppercase SHA256 hex string hash from an input string for unique cache file naming.
        /// </summary>
        /// <param name="input">The string input to hash (e.g., file path).</param>
        /// <returns>The SHA256 hex digest string.</returns>
        private static string ComputeHash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input.ToUpperInvariant());
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes).ToUpperInvariant();
        }

        /// <summary>
        /// Disposes managed resources used by the <see cref="ThumbnailService"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _extractionThrottle.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
