using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Infrastructure;
using System;
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

        public string AppVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "Unknown";
            }
        }

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
