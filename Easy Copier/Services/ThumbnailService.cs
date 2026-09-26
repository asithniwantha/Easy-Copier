using Easy_Copier.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Service for extracting and caching file thumbnails using the Windows Shell API.
    /// </summary>
    public class ThumbnailService : IThumbnailService
    {
        private readonly ILogger<ThumbnailService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThumbnailService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ThumbnailService(ILogger<ThumbnailService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<string?> ExtractThumbnailAsync(string sourceFilePath, string cacheDirectoryPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return Task.FromResult<string?>(null);
            }

            if (!Directory.Exists(cacheDirectoryPath))
            {
                _ = Directory.CreateDirectory(cacheDirectoryPath);
            }

            string fileNameHash = ComputeHash(sourceFilePath);
            string cacheFilePath = Path.Combine(cacheDirectoryPath, $"{fileNameHash}.jpg");

            if (File.Exists(cacheFilePath))
            {
                return Task.FromResult<string?>(cacheFilePath);
            }

            TaskCompletionSource<string?> tcs = new();

            Thread staThread = new(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Guid shellItemGuid = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
                    FileOperationInterop.SHCreateItemFromParsingName(sourceFilePath, IntPtr.Zero, shellItemGuid, out IShellItem shellItem);

                    try
                    {
                        IShellItemImageFactory imageFactory = (IShellItemImageFactory)shellItem;

                        SIZE size = new() { cx = 256, cy = 256 };
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
                                tcs.TrySetResult(cacheFilePath);
                            }
                            finally
                            {
                                _ = Gdi32Interop.DeleteObject(hbitmap);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to extract thumbnail for {FilePath}. HRESULT: {HR}", sourceFilePath, hr);
                            tcs.TrySetResult(null);
                        }
                    }
                    catch (InvalidCastException)
                    {
                        _logger.LogWarning("File {FilePath} does not support IShellItemImageFactory", sourceFilePath);
                        tcs.TrySetResult(null);
                    }
                }
                catch (OperationCanceledException)
                {
                    tcs.TrySetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error extracting thumbnail for {FilePath}", sourceFilePath);
                    tcs.TrySetResult(null);
                }
            })
            {
                IsBackground = true
            };

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();

            // Cancel the thread operation if token is cancelled
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            return tcs.Task;
        }

        private static string ComputeHash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant());
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
