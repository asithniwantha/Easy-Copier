using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using Easy_Copier.Services;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel backing the About dialog, presenting application version info, author details, and GitHub links.
    /// </summary>
    public partial class AboutViewModel : CloseableViewModelBase
    {
        /// <summary>
        /// The process service used for opening external URLs in default applications.
        /// </summary>
        private readonly IProcessService _processService;

        /// <summary>
        /// The clipboard service used for copying version diagnostics to the system clipboard.
        /// </summary>
        private readonly IClipboardService _clipboardService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutViewModel"/> class.
        /// </summary>
        /// <param name="processService">The process service for opening external URLs.</param>
        /// <param name="clipboardService">The clipboard service for copying diagnostic text.</param>
        public AboutViewModel(IProcessService processService, IClipboardService clipboardService)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
        }

        /// <summary>
        /// Gets the displayable application version string retrieved from assembly attributes.
        /// </summary>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "ViewModel properties are bound by instance references in XAML.")]
        public string AppVersion
        {
            get
            {
                string? version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrEmpty(version))
                {
                    // Truncate git commit hash if present (e.g. 1.0.0+hash)
                    int plusIndex = version.IndexOf('+', StringComparison.Ordinal);
                    return plusIndex > 0 ? version[..plusIndex] : version;
                }

                Version? fallbackVersion = Assembly.GetExecutingAssembly().GetName().Version;
                return fallbackVersion != null ? $"{fallbackVersion.Major}.{fallbackVersion.Minor}.{fallbackVersion.Build}" : "Unknown";
            }
        }

        /// <summary>
        /// Gets the developer/author information string.
        /// </summary>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "ViewModel properties are bound by instance references in XAML.")]
        public string DeveloperInfo => "Asith Niwantha";

        /// <summary>
        /// Opens the project GitHub repository in the user's default browser.
        /// </summary>
        [RelayCommand]
        private void OpenGitHubRepo()
        {
            _processService.OpenInExplorer("https://github.com/asithniwantha/Easy-Copier");
        }

        /// <summary>
        /// Opens the project GitHub issues page in the user's default browser.
        /// </summary>
        [RelayCommand]
        private void OpenGitHubIssues()
        {
            _processService.OpenInExplorer("https://github.com/asithniwantha/Easy-Copier/issues");
        }

        /// <summary>
        /// Gets or sets a value indicating whether version information was copied to the clipboard.
        /// </summary>
        [ObservableProperty]
        public partial bool IsCopied { get; set; } = false;

        /// <summary>
        /// Copies version and runtime diagnostic information to the system clipboard.
        /// </summary>
        [RelayCommand]
        private void CopyVersionInfo()
        {
            try
            {
                _clipboardService.SetText($"Easy Copier v{AppVersion}\nOS: {Environment.OSVersion}\nRuntime: .NET {Environment.Version}");
                IsCopied = true;
            }
            catch
            {
                // Fallback for environments where clipboard access is restricted
            }
        }
    }
}
