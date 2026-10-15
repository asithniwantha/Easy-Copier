using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing the Films &amp; TV series library tab view.
    /// Acts as a dedicated child ViewModel that delegates to and synchronizes with <see cref="MainViewModel"/>.
    /// </summary>
    public sealed partial class TvAndFilmsTabViewModel : LibraryTabViewModelBase, ISearchableTabViewModel
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
        /// Gets the list of available sorting options for TV and films.
        /// </summary>
        public IReadOnlyList<TvAndFilmsSortOption> AvailableTvAndFilmsSortOptions => MainViewModel.AvailableTvAndFilmsSortOptions;

        /// <summary>
        /// Gets or sets the selected sorting option for TV and films.
        /// </summary>
        public TvAndFilmsSortOption SelectedTvAndFilmsSortOption
        {
            get => MainViewModel.SelectedTvAndFilmsSortOption;
            set => MainViewModel.SelectedTvAndFilmsSortOption = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether TV and films are sorted in ascending order.
        /// </summary>
        public bool IsTvAndFilmsSortAscending
        {
            get => MainViewModel.IsTvAndFilmsSortAscending;
            set => MainViewModel.IsTvAndFilmsSortAscending = value;
        }

        /// <summary>
        /// Gets the command to toggle the sort direction for TV and films.
        /// </summary>
        public IRelayCommand ToggleTvAndFilmsSortDirectionCommand => MainViewModel.ToggleTvAndFilmsSortDirectionCommand;

        /// <summary>
        /// Gets or sets the search text query used to filter film and TV series entries.
        /// </summary>
        public string SearchText
        {
            get => MainViewModel.TvAndFilmsSearchText;
            set => MainViewModel.TvAndFilmsSearchText = value;
        }

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
            ForwardParentPropertyChanges(propertyName,
                (nameof(MainViewModel.IsTvAndFilmsEmpty), nameof(IsTvAndFilmsEmpty)),
                (nameof(MainViewModel.EmptyTvAndFilmsMessage), nameof(EmptyTvAndFilmsMessage)),
                (nameof(MainViewModel.SelectedTvAndFilmsSortOption), nameof(SelectedTvAndFilmsSortOption)),
                (nameof(MainViewModel.IsTvAndFilmsSortAscending), nameof(IsTvAndFilmsSortAscending)),
                (nameof(MainViewModel.TvAndFilmsSearchText), nameof(SearchText)));
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSuggestions(string query)
        {
            return GetSuggestionsFromEntries(MainViewModel.TvAndFilms, query);
        }
    }
}
