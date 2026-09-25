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
        public async Task<string?> ExtractThumbnailAsync(string sourceFilePath, string cacheDirectoryPath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return null;
            }

            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(cacheDirectoryPath))
                    {
                        _ = Directory.CreateDirectory(cacheDirectoryPath);
                    }

                    // Generate a stable cache file name based on the source file path
                    string fileNameHash = ComputeHash(sourceFilePath);
                    string cacheFilePath = Path.Combine(cacheDirectoryPath, $"{fileNameHash}.jpg");

                    if (File.Exists(cacheFilePath))
                    {
                        return cacheFilePath; // Already cached
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Create IShellItem
                    Guid shellItemGuid = new("43826d1e-e718-42ee-bc55-a1e261c37bfe");
                    FileOperationInterop.SHCreateItemFromParsingName(sourceFilePath, IntPtr.Zero, shellItemGuid, out IShellItem shellItem);

                    if (shellItem is IShellItemImageFactory imageFactory)
                    {
                        SIZE size = new() { cx = 256, cy = 256 };
                        SIIGBF flags = SIIGBF.SIIGBF_RESIZETOFIT;

                        int hr = imageFactory.GetImage(size, flags, out IntPtr hbitmap);
                        if (hr >= 0 && hbitmap != IntPtr.Zero)
                        {
                            try
                            {
                                // Convert HBITMAP to Image and save as JPEG
                                using (Image image = Image.FromHbitmap(hbitmap))
                                {
                                    image.Save(cacheFilePath, ImageFormat.Jpeg);
                                }
                                return cacheFilePath;
                            }
                            finally
                            {
                                _ = Gdi32Interop.DeleteObject(hbitmap);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Failed to extract thumbnail for {FilePath}. HRESULT: {HR}", sourceFilePath, hr);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown/cancel
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error extracting thumbnail for {FilePath}", sourceFilePath);
                }

                return null;
            }, cancellationToken).ConfigureAwait(false);
        }

        private static string ComputeHash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant());
            byte[] hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }
}
