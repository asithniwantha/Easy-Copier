using Easy_Copier.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// ViewModel for managing the Applications library tab view.
    /// Acts as a dedicated child ViewModel that delegates to and synchronizes with <see cref="MainViewModel"/>.
    /// </summary>
    public sealed partial class AppsTabViewModel : LibraryTabViewModelBase, ISearchableTabViewModel
    {
        /// <summary>
        /// Gets the list of filtered application entries.
        /// </summary>
        public ObservableCollection<GameEntry> Apps => MainViewModel.Apps;

        /// <summary>
        /// Gets the message displayed when no apps are present or match search criteria.
        /// </summary>
        public string EmptyAppsMessage => MainViewModel.EmptyAppsMessage;

        /// <summary>
        /// Gets a value indicating whether the apps collection is empty.
        /// </summary>
        public bool IsAppsEmpty => MainViewModel.IsAppsEmpty;

        /// <summary>
        /// Gets or sets the search text query used to filter application entries.
        /// </summary>
        public string SearchText
        {
            get => MainViewModel.AppsSearchText;
            set => MainViewModel.AppsSearchText = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppsTabViewModel"/> class.
        /// </summary>
        /// <param name="mainViewModel">The main application ViewModel.</param>
        public AppsTabViewModel(MainViewModel mainViewModel)
            : base(mainViewModel)
        {
        }

        /// <inheritdoc />
        protected override void OnParentPropertyChanged(string? propertyName)
        {
            ForwardParentPropertyChanges(propertyName,
                (nameof(MainViewModel.IsAppsEmpty), nameof(IsAppsEmpty)),
                (nameof(MainViewModel.EmptyAppsMessage), nameof(EmptyAppsMessage)),
                (nameof(MainViewModel.AppsSearchText), nameof(SearchText)));
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSuggestions(string query)
        {
            return GetSuggestionsFromEntries(MainViewModel.Apps, query);
        }
    }
}
