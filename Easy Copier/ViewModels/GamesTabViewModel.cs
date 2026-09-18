using Easy_Copier.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing the Games library tab view.
    /// Acts as a dedicated child ViewModel that delegates to and synchronizes with <see cref="MainViewModel"/>.
    /// </summary>
    public sealed partial class GamesTabViewModel : LibraryTabViewModelBase
    {
        /// <summary>
        /// Gets the list of filtered game entries.
        /// </summary>
        public ObservableCollection<GameEntry> Games => MainViewModel.Games;

        /// <summary>
        /// Gets the message displayed when no games are present or match search criteria.
        /// </summary>
        public string EmptyGamesMessage => MainViewModel.EmptyGamesMessage;

        /// <summary>
        /// Gets a value indicating whether the games collection is empty.
        /// </summary>
        public bool IsGamesEmpty => MainViewModel.IsGamesEmpty;

        /// <summary>
        /// Gets the list of available game categories for filtering.
        /// </summary>
        public IReadOnlyList<GameCategory> AvailableCategories => MainViewModel.AvailableCategories;

        /// <summary>
        /// Gets or sets the currently selected game category filter.
        /// </summary>
        public GameCategory SelectedCategory
        {
            get => MainViewModel.SelectedCategory;
            set => MainViewModel.SelectedCategory = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GamesTabViewModel"/> class.
        /// </summary>
        /// <param name="mainViewModel">The main application ViewModel.</param>
        public GamesTabViewModel(MainViewModel mainViewModel)
            : base(mainViewModel)
        {
        }

        /// <inheritdoc />
        protected override void OnParentPropertyChanged(string? propertyName)
        {
            if (propertyName == nameof(MainViewModel.IsGamesEmpty))
            {
                OnPropertyChanged(nameof(IsGamesEmpty));
            }
            else if (propertyName == nameof(MainViewModel.EmptyGamesMessage))
            {
                OnPropertyChanged(nameof(EmptyGamesMessage));
            }
            else if (propertyName == nameof(MainViewModel.SelectedCategory))
            {
                OnPropertyChanged(nameof(SelectedCategory));
            }
        }
    }
}
