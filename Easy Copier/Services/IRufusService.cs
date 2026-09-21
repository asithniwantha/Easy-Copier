using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Contract for Rufus integration and ISO image launching operations.
    /// </summary>
    public interface IRufusService
    {
        /// <summary>
        /// Resolves the Rufus executable path and launches Rufus with the specified ISO file path.
        /// </summary>
        /// <param name="isoPath">The full path to the target ISO file.</param>
        /// <returns>A task returning a tuple containing a boolean success indicator and a status message.</returns>
        Task<(bool Success, string Message)> LaunchWithIsoAsync(string isoPath);
    }
}
