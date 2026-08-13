using Easy_Copier.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    public interface ISettingsService
    {
        Task<AppSettings> LoadSettingsAsync();
        AppSettings LoadSettingsSync();
        Task SaveSettingsAsync(AppSettings settings);
        string GetSettingsFilePath();
    }

    public class SettingsService(ILogger<SettingsService> logger) : ISettingsService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private readonly ILogger<SettingsService> _logger = logger;
        private const string SettingsFileName = "appsettings.json";
        private AppSettings? _cachedSettings;

        public string GetSettingsFilePath()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataFolder, "EasyCopier");
            _ = Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, SettingsFileName);
        }

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
