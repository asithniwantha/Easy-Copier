using Easy_Copier.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Implements <see cref="IGameRequirementsService"/> to retrieve and format system requirements using <see cref="ISourceLibraryService"/>.
    /// </summary>
    public class GameRequirementsService : IGameRequirementsService
    {
        private readonly ISourceLibraryService _sourceLibraryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameRequirementsService"/> class.
        /// </summary>
        /// <param name="sourceLibraryService">Service used to retrieve raw system requirement text.</param>
        public GameRequirementsService(ISourceLibraryService sourceLibraryService)
        {
            _sourceLibraryService = sourceLibraryService ?? throw new ArgumentNullException(nameof(sourceLibraryService));
        }

        /// <inheritdoc />
        public async Task<string> GetFormattedSystemRequirementsAsync(string folderPath)
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
