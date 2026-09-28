using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for retrieving and formatting game system requirements.
    /// </summary>
    public interface IGameRequirementsService
    {
        /// <summary>
        /// Asynchronously retrieves and formats system requirements for a specified game item directory.
        /// </summary>
        /// <param name="folderPath">The file system directory path to inspect for system requirements.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the formatted system requirements text string.</returns>
        Task<string> GetFormattedRequirementsAsync(string folderPath);
    }
}
