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
        Task SaveSettingsAsync(AppSettings settings);
        string GetSettingsFilePath();
    }

    public class SettingsService(ILogger<SettingsService> logger) : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger = logger;
        private const string SettingsFileName = "appsettings.json";

        public string GetSettingsFilePath()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataFolder, "EasyCopier");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, SettingsFileName);
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            var settingsPath = GetSettingsFilePath();

            try
            {
                if (!File.Exists(settingsPath))
                {
                    _logger.LogInformation("Settings file not found, creating default settings");
                    var defaultSettings = new AppSettings();
                    await SaveSettingsAsync(defaultSettings);
                    return defaultSettings;
                }

                var json = await File.ReadAllTextAsync(settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings == null)
                {
                    _logger.LogWarning("Failed to deserialize settings, using defaults");
                    return new AppSettings();
                }

                _logger.LogInformation("Settings loaded successfully from {Path}", settingsPath);
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings from {Path}", settingsPath);
                return new AppSettings();
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            var settingsPath = GetSettingsFilePath();

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(settingsPath, json);

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
