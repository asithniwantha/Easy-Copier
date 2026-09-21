using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel backing the About window, providing application version metadata, author information, and repository links.
    /// </summary>
    public partial class AboutViewModel(IProcessService processService) : ObservableObject
    {
        private readonly IProcessService _processService = processService ?? throw new ArgumentNullException(nameof(processService));

        /// <summary>
        /// Event raised when the view requests to be closed.
        /// </summary>
        public event EventHandler? CloseRequested;

        [RelayCommand]
        private void CloseWindow()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets the display application version string extracted from informational assembly attributes.
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
        /// Gets the developer information string.
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
