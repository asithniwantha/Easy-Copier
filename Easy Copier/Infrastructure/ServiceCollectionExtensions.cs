using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Easy_Copier.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
#if DEBUG
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
#else
                builder.SetMinimumLevel(LogLevel.Information);
#endif
            });

            services.AddSingleton<Services.ISettingsService, Services.SettingsService>();
            services.AddSingleton<Services.ICopyHistoryService, Services.CopyHistoryService>();
            services.AddSingleton<Services.IReportService, Services.ReportService>();
            services.AddSingleton<Services.ILibraryCacheService, Services.LibraryCacheService>();
            services.AddSingleton<Services.ISourceLibraryService, Services.SourceLibraryService>();
            services.AddSingleton<Services.IFolderPickerService, FolderPickerService>();
            services.AddSingleton<Services.IGameScannerService, Services.GameScannerService>();
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
            services.AddSingleton<Services.IGameInfoDownloadService, Services.GameInfoDownloadService>();

            return services;
        }

        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            _ = services.AddSingleton<ViewModels.MainViewModel>();
            _ = services.AddTransient<ViewModels.SettingsViewModel>();
            _ = services.AddTransient<ViewModels.HistoryViewModel>();

            return services;
        }
    }

    public static class AppServiceLocator
    {
        private static IServiceProvider? _instance;

        public static void Initialize(IServiceProvider provider)
        {
            _instance = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public static T GetService<T>() where T : class
        {
            return _instance == null
                ? throw new InvalidOperationException("AppServiceLocator not initialized")
                : _instance.GetRequiredService<T>();
        }
    }
}
