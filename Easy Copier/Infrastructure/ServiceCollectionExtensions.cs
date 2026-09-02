using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Globalization;
using System.IO;
using System;

namespace Easy_Copier.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logFolder = Path.Combine(appDataFolder, "EasyCopier", "Logs");
            Directory.CreateDirectory(logFolder);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.File(
                    Path.Combine(logFolder, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    formatProvider: CultureInfo.InvariantCulture)
                .CreateLogger();

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            services.AddSingleton<Services.ISettingsService, Services.SettingsService>();
            services.AddSingleton<Services.ICopyHistoryService, Services.CopyHistoryService>();
            services.AddSingleton<Services.IReportService, Services.ReportService>();
            services.AddSingleton<Services.ILibraryCacheService, Services.LibraryCacheService>();
            services.AddSingleton<Services.ISourceLibraryService, Services.SourceLibraryService>();
            services.AddSingleton<Services.IFolderPickerService, FolderPickerService>();
            services.AddSingleton<Services.IGameScannerService, Services.GameScannerService>();
            services.AddSingleton<Services.ILibraryScannerService, Services.LibraryScannerService>();
            services.AddSingleton<Services.IDriveDiscoveryService, Services.DriveDiscoveryService>();
            services.AddSingleton<Services.IDriveValidationService, Services.DriveValidationService>();
            services.AddSingleton<Services.IFileTransferService, Services.WindowsShellTransferService>();
            services.AddSingleton<Services.ITransferQueueService, Services.TransferQueueService>();
            services.AddSingleton<Services.IStartupService, Services.StartupService>();
            services.AddSingleton<IProcessService, ProcessService>();
            services.AddSingleton<IWindowService, WindowService>();
            services.AddSingleton<IFilePickerService, FilePickerService>();
            services.AddSingleton<IDispatcherService, DispatcherService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<Services.IHistoryDialogService, Services.HistoryDialogService>();
            services.AddSingleton<Services.IGameInfoDownloadService, Services.GameInfoDownloadService>();
            services.AddSingleton<Services.IUpdateService, Services.UpdateService>();
            services.AddSingleton<Services.IDatabaseService, Services.DatabaseService>();

            return services;
        }

        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            _ = services.AddSingleton<ViewModels.MainViewModel>();
            _ = services.AddSingleton<ViewModels.SmartAdderViewModel>();
            _ = services.AddTransient<ViewModels.SettingsViewModel>();
            _ = services.AddTransient<ViewModels.HistoryViewModel>();
            _ = services.AddTransient<ViewModels.AboutViewModel>();

            return services;
        }
    }
}
