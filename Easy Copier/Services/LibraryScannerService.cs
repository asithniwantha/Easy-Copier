using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public class LibraryScannerService : ILibraryScannerService
    {
        private readonly IGameScannerService _gameScannerService;

        public LibraryScannerService(IGameScannerService gameScannerService)
        {
            _gameScannerService = gameScannerService;
        }

        public async Task<(IReadOnlyList<GameEntry> Games, IReadOnlyList<GameEntry> Apps, IReadOnlyList<GameEntry> TvAndFilms)> ScanAllLibrariesAsync(
            AppSettings settings,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);

            List<GameEntry> allGames = [];
            List<GameEntry> allApps = [];
            List<GameEntry> allTvAndFilms = [];

            if (settings.GameSourceFolders != null && settings.GameSourceFolders.Count > 0)
            {
                IReadOnlyList<GameEntry> games = await _gameScannerService.ScanLibraryAsync(
                    settings.GameSourceFolders,
                    LibraryCategory.Game,
                    progress,
                    cancellationToken: cancellationToken);

                allGames.AddRange(games);
            }

            if (settings.AppSourceFolders != null && settings.AppSourceFolders.Count > 0)
            {
                IReadOnlyList<GameEntry> apps = await _gameScannerService.ScanLibraryAsync(
                    settings.AppSourceFolders,
                    LibraryCategory.App,
                    progress,
                    cancellationToken: cancellationToken);

                allApps.AddRange(apps);
            }

            if (settings.TvAndFilmSourceFolders != null && settings.TvAndFilmSourceFolders.Count > 0)
            {
                IReadOnlyList<GameEntry> tvAndFilms = await _gameScannerService.ScanLibraryAsync(
                    settings.TvAndFilmSourceFolders,
                    LibraryCategory.TvAndFilm,
                    progress,
                    settings.VideoFileExtensions,
                    cancellationToken: cancellationToken);

                allTvAndFilms.AddRange(tvAndFilms);
            }

            return (allGames, allApps, allTvAndFilms);
        }

        public async Task<string> FindDuplicatesReportAsync(
            AppSettings settings,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var (games, apps, tvAndFilms) = await ScanAllLibrariesAsync(settings, progress, cancellationToken);

            List<GameEntry> allEntries = [];
            allEntries.AddRange(games);
            allEntries.AddRange(apps);
            allEntries.AddRange(tvAndFilms);

            var duplicates = allEntries
                .GroupBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Count == 0)
            {
                return "No duplicates found across source libraries.";
            }

            System.Text.StringBuilder reportBuilder = new();
            reportBuilder.AppendLine($"Found {duplicates.Count} duplicated items:");
            reportBuilder.AppendLine();

            foreach (var group in duplicates)
            {
                reportBuilder.AppendLine($"- {group.Key} ({group.Count()} copies):");
                foreach (var entry in group)
                {
                    reportBuilder.AppendLine($"  • [{entry.Category}] {entry.FolderPath}");
                }
                reportBuilder.AppendLine();
            }

            reportBuilder.AppendLine("Cleanup Recommendation: Consider deleting the duplicate folders shown above to save disk space.");
            return reportBuilder.ToString().TrimEnd();
        }
    }
}
