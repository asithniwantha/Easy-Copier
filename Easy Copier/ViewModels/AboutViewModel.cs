using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel backing the About dialog, presenting application version info, author details, and GitHub links.
    /// </summary>
    public partial class AboutViewModel : ObservableObject
    {
        private readonly IProcessService _processService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AboutViewModel"/> class.
        /// </summary>
        /// <param name="processService">The process service for opening external URLs.</param>
        public AboutViewModel(IProcessService processService)
        {
            _processService = processService;
        }

        /// <summary>
        /// Event raised when the view requests the containing dialog or window to close.
        /// </summary>
        public event EventHandler? CloseRequested;

        [RelayCommand]
        private void CloseWindow()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
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

        [RelayCommand]
        private void OpenGitHubRepo()
        {
            _processService.OpenInExplorer("https://github.com/asithniwantha/Easy-Copier");
        }

        [RelayCommand]
        private void OpenGitHubIssues()
        {
            _processService.OpenInExplorer("https://github.com/asithniwantha/Easy-Copier/issues");
        }
    }
}
