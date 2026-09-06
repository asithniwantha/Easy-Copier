using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface ILibraryScannerService
    {
        Task<(IReadOnlyList<GameEntry> Games, IReadOnlyList<GameEntry> Apps, IReadOnlyList<GameEntry> TvAndFilms)> ScanAllLibrariesAsync(
            AppSettings settings,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);

        Task<string> FindDuplicatesReportAsync(
            AppSettings settings,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
