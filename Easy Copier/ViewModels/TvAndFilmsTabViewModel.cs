using Easy_Copier.Models;
using System.Collections.ObjectModel;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing the Films &amp; TV series library tab view.
    /// Acts as a dedicated child ViewModel that delegates to and synchronizes with <see cref="MainViewModel"/>.
    /// </summary>
    public sealed partial class TvAndFilmsTabViewModel : LibraryTabViewModelBase
    {
        /// <summary>
        /// Gets the list of filtered film and TV series entries.
        /// </summary>
        public ObservableCollection<GameEntry> TvAndFilms => MainViewModel.TvAndFilms;

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
            : base(mainViewModel)
        {
        }

        /// <inheritdoc />
        protected override void OnParentPropertyChanged(string? propertyName)
        {
            if (propertyName == nameof(MainViewModel.IsTvAndFilmsEmpty))
            {
                OnPropertyChanged(nameof(IsTvAndFilmsEmpty));
            }
            else if (propertyName == nameof(MainViewModel.EmptyTvAndFilmsMessage))
            {
                OnPropertyChanged(nameof(EmptyTvAndFilmsMessage));
            }
        }
    }
}
