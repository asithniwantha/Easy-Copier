using CommunityToolkit.Mvvm.ComponentModel;
using Easy_Copier.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Abstract base ViewModel for tab-specific child ViewModels that synchronize state with <see cref="MainViewModel"/>.
    /// </summary>
    public abstract class LibraryTabViewModelBase : ObservableObject, IDisposable
    {
        /// <summary>
        /// Tracks whether the object has been disposed to prevent double disposal.
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// Gets the parent <see cref="MainViewModel"/> instance.
        /// </summary>
        public MainViewModel MainViewModel { get; }

        /// <summary>
        /// Gets a value indicating whether library scanning is in progress.
        /// </summary>
        public bool IsScanning => MainViewModel.IsScanning;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryTabViewModelBase"/> class.
        /// </summary>
        /// <param name="mainViewModel">The parent main ViewModel.</param>
        protected LibraryTabViewModelBase(MainViewModel mainViewModel)
        {
            MainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            MainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
        }

        /// <summary>
        /// Filters item entries to generate a distinct list of up to 10 autocomplete suggestions matching the query string.
        /// </summary>
        /// <param name="entries">The collection of library entries to filter.</param>
        /// <param name="query">The user input search query.</param>
        /// <returns>An enumeration of up to 10 unique matching entry names.</returns>
        public static IEnumerable<string> GetSuggestionsFromEntries(IEnumerable<GameEntry> entries, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            return entries
                .Select(i => i.Name)
                .Where(n => n.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .Take(10);
        }

        /// <summary>
        /// Handles property change events from the parent <see cref="MainViewModel"/>.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="PropertyChangedEventArgs"/> that contains the event data.</param>
        private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsScanning))
            {
                OnPropertyChanged(nameof(IsScanning));
            }

            OnParentPropertyChanged(e.PropertyName);
        }

        /// <summary>
        /// Derived classes override this method to handle parent ViewModel property change notifications.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected virtual void OnParentPropertyChanged(string? propertyName)
        {
        }

        /// <summary>
        /// Evaluates parent property change notifications and raises <see cref="ObservableObject.OnPropertyChanged(string?)"/> for matching mapped child properties.
        /// </summary>
        /// <param name="changedPropertyName">The parent property name that changed.</param>
        /// <param name="mappings">Tuples mapping parent property names to child property names.</param>
        protected void ForwardParentPropertyChanges(string? changedPropertyName, params ReadOnlySpan<(string ParentPropName, string ChildPropName)> mappings)
        {
            if (string.IsNullOrEmpty(changedPropertyName))
            {
                return;
            }

            foreach ((string? parentPropName, string? childPropName) in mappings)
            {
                if (string.Equals(changedPropertyName, parentPropName, StringComparison.Ordinal))
                {
                    OnPropertyChanged(childPropName);
                }
            }
        }

        /// <summary>
        /// Unsubscribes from parent property change notifications and disposes resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources held by this instance.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release managed resources; <c>false</c> if invoked from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                MainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
            }

            _isDisposed = true;
        }
    }
}
