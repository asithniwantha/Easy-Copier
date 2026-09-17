using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Models;
using System;
using System.Collections.ObjectModel;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing the Applications library tab view.
    /// Acts as a dedicated child ViewModel that delegates to and synchronizes with <see cref="MainViewModel"/>.
    /// </summary>
    public sealed partial class AppsTabViewModel : ObservableObject
    {
        /// <summary>
        /// Gets the parent <see cref="MainViewModel"/> instance.
        /// </summary>
        public MainViewModel MainViewModel { get; }

        /// <summary>
        /// Gets the list of filtered application entries.
        /// </summary>
        public ObservableCollection<GameEntry> Apps => MainViewModel.Apps;

        /// <summary>
        /// Gets a value indicating whether library scanning is in progress.
        /// </summary>
        public bool IsScanning => MainViewModel.IsScanning;

        /// <summary>
        /// Gets the message displayed when no apps are present or match search criteria.
        /// </summary>
        public string EmptyAppsMessage => MainViewModel.EmptyAppsMessage;

        /// <summary>
        /// Gets a value indicating whether the apps collection is empty.
        /// </summary>
        public bool IsAppsEmpty => MainViewModel.IsAppsEmpty;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppsTabViewModel"/> class.
        /// </summary>
        /// <param name="mainViewModel">The main application ViewModel.</param>
        public AppsTabViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // Subscribe to parent ViewModel property changes to notify UI of tab-specific state updates.
            MainViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsScanning) ||
                    e.PropertyName == nameof(MainViewModel.IsAppsEmpty))
                {
                    OnPropertyChanged(nameof(IsScanning));
                    OnPropertyChanged(nameof(IsAppsEmpty));
                }
                if (e.PropertyName == nameof(MainViewModel.EmptyAppsMessage))
                {
                    OnPropertyChanged(nameof(EmptyAppsMessage));
                }
            };
        }
    }
}
