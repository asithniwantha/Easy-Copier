using Easy_Copier.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Implements <see cref="IGameRequirementsService"/> to encapsulate retrieving and formatting game system requirements.
    /// </summary>
    public class GameRequirementsService : IGameRequirementsService
    {
        /// <summary>
        /// The source library service used to retrieve raw system requirements text for game items.
        /// </summary>
        private readonly ISourceLibraryService _sourceLibraryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameRequirementsService"/> class.
        /// </summary>
        /// <param name="sourceLibraryService">The source library service used to retrieve system requirements text.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceLibraryService"/> is <see langword="null"/>.</exception>
        public GameRequirementsService(ISourceLibraryService sourceLibraryService)
        {
            _sourceLibraryService = sourceLibraryService ?? throw new ArgumentNullException(nameof(sourceLibraryService));
        }

        /// <inheritdoc />
        public async Task<string> GetFormattedRequirementsAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return string.Empty;
            }

            string rawRequirementsText = await _sourceLibraryService.GetSystemRequirementsAsync(folderPath);
            return SysReqFormatter.FormatText(rawRequirementsText);
        }
    }
}
