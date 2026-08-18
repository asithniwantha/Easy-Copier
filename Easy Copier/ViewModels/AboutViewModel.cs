using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Easy_Copier.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        private readonly IProcessService _processService;

        public AboutViewModel(IProcessService processService)
        {
            _processService = processService;
        }

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "ViewModel properties are bound by instance references in XAML.")]
        public string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrEmpty(version))
                {
                    // Truncate git commit hash if present (e.g. 1.0.0+hash)
                    int plusIndex = version.IndexOf('+', StringComparison.Ordinal);
                    if (plusIndex > 0)
                    {
                        return version.Substring(0, plusIndex);
                    }
                    return version;
                }

                var fallbackVersion = Assembly.GetExecutingAssembly().GetName().Version;
                return fallbackVersion != null ? $"{fallbackVersion.Major}.{fallbackVersion.Minor}.{fallbackVersion.Build}" : "Unknown";
            }
        }

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
