using Easy_Copier.Services;
using Easy_Copier.Views.Settings;
using System;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Implements <see cref="ISettingsNavigationService"/> mapping settings navigation tags and startup actions to Page types.
    /// </summary>
    public class SettingsNavigationService : ISettingsNavigationService
    {
        /// <inheritdoc />
        public string GetTargetTagForAction(SettingsOpenAction action)
        {
            return action switch
            {
                SettingsOpenAction.AddGameFolder => "Games",
                SettingsOpenAction.AddAppFolder => "Apps",
                SettingsOpenAction.AddTvAndFilmFolder => "FilmAndTv",
                SettingsOpenAction.AddOsImageFolder => "OsImages",
                _ => "General"
            };
        }

        /// <inheritdoc />
        public Type GetPageTypeForTag(string? tag)
        {
            return tag switch
            {
                "General" => typeof(GeneralSettingsPage),
                "Games" => typeof(GamesSettingsPage),
                "Apps" => typeof(AppsSettingsPage),
                "FilmAndTv" => typeof(FilmAndTvSettingsPage),
                "OsImages" => typeof(OsImagesSettingsPage),
                "Logs" => typeof(LogsSettingsPage),
                _ => typeof(GeneralSettingsPage)
            };
        }
    }
}

