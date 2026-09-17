using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Models;
using System;
using System.Collections.ObjectModel;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing the Films &amp; TV series library tab view.
    /// Acts as a dedicated child ViewModel that delegates to and synchronizes with <see cref="MainViewModel"/>.
    /// </summary>
    public sealed partial class TvAndFilmsTabViewModel : ObservableObject
    {
        /// <summary>
        /// Gets the parent <see cref="MainViewModel"/> instance.
        /// </summary>
        public MainViewModel MainViewModel { get; }

        /// <summary>
        /// Gets the list of filtered film and TV series entries.
        /// </summary>
        public ObservableCollection<GameEntry> TvAndFilms => MainViewModel.TvAndFilms;

        /// <summary>
        /// Gets a value indicating whether library scanning is in progress.
        /// </summary>
        public bool IsScanning => MainViewModel.IsScanning;

        /// <summary>
        /// Gets the message displayed when no films/TV series are present or match search criteria.
        /// </summary>
        public string EmptyTvAndFilmsMessage => MainViewModel.EmptyTvAndFilmsMessage;

        /// <summary>
        /// Gets a value indicating whether the films/TV series collection is empty.
        /// </summary>
        public bool IsTvAndFilmsEmpty => MainViewModel.IsTvAndFilmsEmpty;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvAndFilmsTabViewModel"/> class.
        /// </summary>
        /// <param name="mainViewModel">The main application ViewModel.</param>
        public TvAndFilmsTabViewModel(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // Subscribe to parent ViewModel property changes to notify UI of tab-specific state updates.
            MainViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsScanning) ||
                    e.PropertyName == nameof(MainViewModel.IsTvAndFilmsEmpty))
                {
                    OnPropertyChanged(nameof(IsScanning));
                    OnPropertyChanged(nameof(IsTvAndFilmsEmpty));
                }
                if (e.PropertyName == nameof(MainViewModel.EmptyTvAndFilmsMessage))
                {
                    OnPropertyChanged(nameof(EmptyTvAndFilmsMessage));
                }
            };
        }
    }
}
