using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for extracting and caching thumbnails from files.
    /// </summary>
    public interface IThumbnailService
    {
        /// <summary>
        /// Asynchronously extracts a thumbnail from the specified source file and saves it to the cache directory.
        /// </summary>
        /// <param name="sourceFilePath">The full path of the source file (e.g., a video file).</param>
        /// <param name="cacheDirectoryPath">The full path of the directory where the thumbnail should be saved.</param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>A task that represents the asynchronous operation, returning the full path to the cached thumbnail if successful, or <c>null</c> otherwise.</returns>
        Task<string?> ExtractThumbnailAsync(string sourceFilePath, string cacheDirectoryPath, CancellationToken cancellationToken = default);
    }
}
