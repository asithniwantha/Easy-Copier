using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines methods for retrieving and formatting system requirements for library items.
    /// </summary>
    public interface IGameRequirementsService
    {
        /// <summary>
        /// Retrieves and formats system requirements for a game or item at the specified folder path.
        /// </summary>
        /// <param name="folderPath">The directory path of the library item.</param>
        /// <returns>A formatted string of system requirements, or empty string if unavailable.</returns>
        Task<string> GetFormattedSystemRequirementsAsync(string folderPath);
    }
}
