using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Defines operations for loading, saving, and retrieving application configuration settings.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// Asynchronously loads settings from disk or returns cached settings if already loaded.
        /// </summary>
        /// <returns>A task returning the active <see cref="AppSettings"/> instance.</returns>
        Task<AppSettings> LoadSettingsAsync();

        /// <summary>
        /// Synchronously loads settings from disk or returns cached settings if already loaded.
        /// </summary>
        /// <returns>The active <see cref="AppSettings"/> instance.</returns>
        AppSettings LoadSettingsSync();

        /// <summary>
        /// Asynchronously saves application settings to disk and updates memory cache.
        /// </summary>
        /// <param name="settings">The <see cref="AppSettings"/> instance to persist.</param>
        /// <returns>A task representing the save operation.</returns>
        Task SaveSettingsAsync(AppSettings settings);

        /// <summary>
        /// Returns the full local file path to the settings JSON file.
        /// </summary>
        /// <returns>The file path string.</returns>
        string GetSettingsFilePath();
    }

    /// <summary>
    /// Service for reading and persisting application configuration settings to <c>appsettings.json</c>.
    /// </summary>
    /// <param name="logger">The logger instance for logging configuration operations.</param>
    public class SettingsService(ILogger<SettingsService> logger) : ISettingsService
    {
        /// <summary>
        /// Indented JSON serializer options for formatting settings files.
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// Logger instance used for diagnostic logging.
        /// </summary>
        private readonly ILogger<SettingsService> _logger = logger;

        /// <summary>
        /// Name of the settings file saved in AppData.
        /// </summary>
        private const string SettingsFileName = "appsettings.json";

        /// <summary>
        /// In-memory cached copy of settings.
        /// </summary>
        private AppSettings? _cachedSettings;

        /// <summary>
        /// Gets the absolute file path to the local <c>appsettings.json</c> file in AppData.
        /// </summary>
        /// <returns>The full file path string.</returns>
        public string GetSettingsFilePath()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataFolder, "EasyCopier");
            _ = Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, SettingsFileName);
        }

        /// <summary>
        /// Asynchronously loads settings from disk, creating default settings if the file does not exist.
        /// </summary>
        /// <returns>A task returning the loaded <see cref="AppSettings"/>.</returns>
        public async Task<AppSettings> LoadSettingsAsync()
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            string settingsPath = GetSettingsFilePath();

            try
            {
                if (!File.Exists(settingsPath))
                {
                    _logger.LogInformation("Settings file not found, creating default settings");
                    AppSettings defaultSettings = new();
                    await SaveSettingsAsync(defaultSettings);
                    _cachedSettings = defaultSettings;
                    return defaultSettings;
                }

                string json = await File.ReadAllTextAsync(settingsPath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings == null)
                {
                    _logger.LogWarning("Failed to deserialize settings, using defaults");
                    _cachedSettings = new AppSettings();
                    return _cachedSettings;
                }

                _logger.LogInformation("Settings loaded successfully from {Path}", settingsPath);
                _cachedSettings = settings;
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings from {Path}", settingsPath);
                _cachedSettings = new AppSettings();
                return _cachedSettings;
            }
        }

        /// <summary>
        /// Synchronously loads settings from disk, returning cached values if available or defaults if missing.
        /// </summary>
        /// <returns>The loaded <see cref="AppSettings"/>.</returns>
        public AppSettings LoadSettingsSync()
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            string settingsPath = GetSettingsFilePath();
            try
            {
                if (!File.Exists(settingsPath))
                {
                    _cachedSettings = new AppSettings();
                    return _cachedSettings;
                }

                string json = File.ReadAllText(settingsPath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);
                _cachedSettings = settings ?? new AppSettings();
                return _cachedSettings;
            }
            catch
            {
                _cachedSettings = new AppSettings();
                return _cachedSettings;
            }
        }

        /// <summary>
        /// Asynchronously writes settings to disk and updates the memory cache.
        /// </summary>
        /// <param name="settings">The application settings to serialize and save.</param>
        /// <returns>A task representing the save operation.</returns>
        public async Task SaveSettingsAsync(AppSettings settings)
        {
            string settingsPath = GetSettingsFilePath();

            try
            {
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                await File.WriteAllTextAsync(settingsPath, json);

                // Invalidate or update cache on save
                _cachedSettings = settings;

                _logger.LogInformation("Settings saved successfully to {Path}", settingsPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings to {Path}", settingsPath);
                throw;
            }
        }
    }
}
