using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Models;
using System;
using System.Collections.ObjectModel;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing the Games library tab view.
    /// Acts as a dedicated child ViewModel that delegates to and synchronizes with <see cref="MainViewModel"/>.
    /// </summary>
    public sealed partial class GamesTabViewModel : ObservableObject
    {
        /// <summary>
        /// Gets the parent <see cref="MainViewModel"/> instance.
        /// </summary>
        public MainViewModel MainViewModel { get; }

        /// <summary>
        /// Gets the list of filtered game entries.
        /// </summary>
        public ObservableCollection<GameEntry> Games => MainViewModel.Games;

        /// <summary>
        /// Gets a value indicating whether library scanning is in progress.
        /// </summary>
        public bool IsScanning => MainViewModel.IsScanning;

        /// <summary>
        /// Gets the message displayed when no games are present or match search criteria.
        /// </summary>
        public string EmptyGamesMessage => MainViewModel.EmptyGamesMessage;

        /// <summary>
        /// Gets a value indicating whether the games collection is empty.
        /// </summary>
        public bool IsGamesEmpty => MainViewModel.IsGamesEmpty;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamesTabViewModel"/> class.
        /// </summary>
        /// <param name="mainViewModel">The main application ViewModel.</param>
        public GamesTabViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // Subscribe to parent ViewModel property changes to notify UI of tab-specific state updates.
            MainViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsScanning) ||
                    e.PropertyName == nameof(MainViewModel.IsGamesEmpty))
                {
                    OnPropertyChanged(nameof(IsScanning));
                    OnPropertyChanged(nameof(IsGamesEmpty));
                }
                if (e.PropertyName == nameof(MainViewModel.EmptyGamesMessage))
                {
                    OnPropertyChanged(nameof(EmptyGamesMessage));
                }
            };
        }
    }
}
