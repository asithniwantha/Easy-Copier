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
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolder = Path.Combine(appDataFolder, "EasyCopier");
            _ = Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, SettingsFileName);
        }

        public async Task<AppSettings> LoadSettingsAsync()
        {
            string settingsPath = GetSettingsFilePath();

            try
            {
                if (!File.Exists(settingsPath))
                {
                    _logger.LogInformation("Settings file not found, creating default settings");
                    AppSettings defaultSettings = new();
                    await SaveSettingsAsync(defaultSettings);
                    return defaultSettings;
                }

                string json = await File.ReadAllTextAsync(settingsPath);
                AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json);

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
            string settingsPath = GetSettingsFilePath();

            try
            {
                JsonSerializerOptions options = new()
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(settings, options);
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
