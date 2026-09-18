using CommunityToolkit.Mvvm.Input;
using Easy_Copier.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing the OS Images library tab view.
    /// Acts as a dedicated child ViewModel that delegates to and synchronizes with <see cref="MainViewModel"/>.
    /// </summary>
    public sealed partial class OsImagesTabViewModel : LibraryTabViewModelBase
    {
        /// <summary>
        /// Gets the list of filtered OS Image entries.
        /// </summary>
        public ObservableCollection<GameEntry> OsImages => MainViewModel.OsImages;

        /// <summary>
        /// Gets the message displayed when no OS Images are present or match search criteria.
        /// </summary>
        public string EmptyOsImagesMessage => MainViewModel.EmptyOsImagesMessage;

        /// <summary>
        /// Gets a value indicating whether the OS Images collection is empty.
        /// </summary>
        public bool IsOsImagesEmpty => MainViewModel.IsOsImagesEmpty;

        /// <summary>
        /// Gets the list of available sorting options for OS Images.
        /// </summary>
        public IReadOnlyList<OsImageSortOption> AvailableOsImageSortOptions => MainViewModel.AvailableOsImageSortOptions;

        /// <summary>
        /// Gets or sets the selected sorting option for OS Images.
        /// </summary>
        public OsImageSortOption SelectedOsImageSortOption
        {
            get => MainViewModel.SelectedOsImageSortOption;
            set => MainViewModel.SelectedOsImageSortOption = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether OS Images are sorted in ascending order.
        /// </summary>
        public bool IsOsImageSortAscending
        {
            get => MainViewModel.IsOsImageSortAscending;
            set => MainViewModel.IsOsImageSortAscending = value;
        }

        /// <summary>
        /// Gets the command to toggle the sort direction for OS Images.
        /// </summary>
        public IRelayCommand ToggleOsImageSortDirectionCommand => MainViewModel.ToggleOsImageSortDirectionCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="OsImagesTabViewModel"/> class.
        /// </summary>
        /// <param name="mainViewModel">The main application ViewModel.</param>
        public OsImagesTabViewModel(MainViewModel mainViewModel)
            : base(mainViewModel)
        {
        }

        /// <inheritdoc />
        protected override void OnParentPropertyChanged(string? propertyName)
        {
            if (propertyName == nameof(MainViewModel.IsOsImagesEmpty))
            {
                OnPropertyChanged(nameof(IsOsImagesEmpty));
            }
            else if (propertyName == nameof(MainViewModel.EmptyOsImagesMessage))
            {
                OnPropertyChanged(nameof(EmptyOsImagesMessage));
            }
            else if (propertyName == nameof(MainViewModel.SelectedOsImageSortOption))
            {
                OnPropertyChanged(nameof(SelectedOsImageSortOption));
            }
            else if (propertyName == nameof(MainViewModel.IsOsImageSortAscending))
            {
                OnPropertyChanged(nameof(IsOsImageSortAscending));
            }
        }
    }
}
