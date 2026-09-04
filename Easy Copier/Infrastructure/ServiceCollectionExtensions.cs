using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Globalization;
using System.IO;

namespace Easy_Copier.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string logFolder = Path.Combine(appDataFolder, "EasyCopier", "Logs");
            _ = Directory.CreateDirectory(logFolder);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug(formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.File(
                    Path.Combine(logFolder, "log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    formatProvider: CultureInfo.InvariantCulture)
                .CreateLogger();

            _ = services.AddLogging(builder =>
            {
                _ = builder.ClearProviders();
                _ = builder.AddSerilog(dispose: true);
            });

            _ = services.AddSingleton<Services.ISettingsService, Services.SettingsService>();
            _ = services.AddSingleton<Services.ICopyHistoryService, Services.CopyHistoryService>();
            _ = services.AddSingleton<Services.IReportService, Services.ReportService>();
            _ = services.AddSingleton<Services.ILibraryCacheService, Services.LibraryCacheService>();
            _ = services.AddSingleton<Services.ISourceLibraryService, Services.SourceLibraryService>();
            _ = services.AddSingleton<Services.IFolderPickerService, FolderPickerService>();
            _ = services.AddSingleton<Services.IGameScannerService, Services.GameScannerService>();
            _ = services.AddSingleton<Services.ILibraryScannerService, Services.LibraryScannerService>();
            _ = services.AddSingleton<Services.IDriveDiscoveryService, Services.DriveDiscoveryService>();
            _ = services.AddSingleton<Services.IDriveValidationService, Services.DriveValidationService>();
            _ = services.AddSingleton<Services.IFileTransferService, Services.WindowsShellTransferService>();
            _ = services.AddSingleton<Services.ITransferQueueService, Services.TransferQueueService>();
            _ = services.AddSingleton<Services.IStartupService, Services.StartupService>();
            _ = services.AddSingleton<IProcessService, ProcessService>();
            _ = services.AddSingleton<IWindowService, WindowService>();
            _ = services.AddSingleton<IFilePickerService, FilePickerService>();
            _ = services.AddSingleton<IDispatcherService, DispatcherService>();
            _ = services.AddSingleton<IDialogService, DialogService>();
            _ = services.AddSingleton<IAppWindowContext, AppWindowContext>();
            _ = services.AddSingleton<Infrastructure.IHistoryDialogService, Infrastructure.HistoryDialogService>();
            _ = services.AddSingleton<Services.IGameInfoDownloadService, Services.GameInfoDownloadService>();
            _ = services.AddSingleton<Services.IUpdateService, Services.UpdateService>();
            _ = services.AddSingleton<Services.IDatabaseService, Services.DatabaseService>();

            return services;
        }

        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            _ = services.AddSingleton<ViewModels.MainViewModel>();
            _ = services.AddSingleton<ViewModels.SmartAdderViewModel>();
            _ = services.AddTransient<ViewModels.SettingsViewModel>();
            _ = services.AddTransient<ViewModels.HistoryViewModel>();
            _ = services.AddTransient<ViewModels.SmartAdderHistoryViewModel>();
            _ = services.AddTransient<ViewModels.AboutViewModel>();

            return services;
        }
    }
}
