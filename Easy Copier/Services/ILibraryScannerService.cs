using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for scanning library folders and generating duplicate item reports.
    /// </summary>
    public interface ILibraryScannerService
    {
        /// <summary>
        /// Asynchronously scans configured library source directories and categorizes discovered items into Games, Apps, TV/Films, and OS Images.
        /// </summary>
        /// <param name="settings">The application settings containing configured library paths.</param>
        /// <param name="progress">An optional progress reporter for status updates.</param>
        /// <param name="cancellationToken">A cancellation token to observe while scanning.</param>
        /// <returns>A task returning a tuple containing lists for each content category (Games, Apps, TvAndFilms, OsImages).</returns>
        Task<(IReadOnlyList<GameEntry> Games, IReadOnlyList<GameEntry> Apps, IReadOnlyList<GameEntry> TvAndFilms, IReadOnlyList<GameEntry> OsImages)> ScanAllLibrariesAsync(
            AppSettings settings,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously scans libraries to identify duplicate game/item entries across different source folders and generates a summary report.
        /// </summary>
        /// <param name="settings">The application settings containing configured library paths.</param>
        /// <param name="progress">An optional progress reporter for status updates.</param>
        /// <param name="cancellationToken">A cancellation token to observe while scanning.</param>
        /// <returns>A task returning a formatted text report of duplicate entries found.</returns>
        Task<string> FindDuplicatesReportAsync(
            AppSettings settings,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
