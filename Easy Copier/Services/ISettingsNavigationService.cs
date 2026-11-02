using Easy_Copier.Infrastructure;
using System;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Service for resolving and managing navigation in the settings view.
    /// </summary>
    public interface ISettingsNavigationService
    {
        /// <summary>
        /// Resolves the navigation tag for a specified startup open action.
        /// </summary>
        /// <param name="action">The open action specified when launching settings.</param>
        /// <returns>The string navigation tag identifying the target settings page.</returns>
        string GetTargetTagForAction(SettingsOpenAction action);

        /// <summary>
        /// Resolves the Page type corresponding to a navigation tag.
        /// </summary>
        /// <param name="tag">The navigation tag string.</param>
        /// <returns>The resolved <see cref="Type"/> of the settings page.</returns>
        Type GetPageTypeForTag(string? tag);
    }
}

