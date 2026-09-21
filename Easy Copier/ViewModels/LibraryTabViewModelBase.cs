using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;

namespace Easy_Copier.ViewModels
{
    /// <summary>
    /// Abstract base ViewModel for tab-specific child ViewModels that synchronize state with <see cref="MainViewModel"/>.
    /// </summary>
    public abstract class LibraryTabViewModelBase : ObservableObject
    {
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
        protected void ForwardParentPropertyChanges(string? changedPropertyName, params (string ParentPropName, string ChildPropName)[] mappings)
        {
            if (string.IsNullOrEmpty(changedPropertyName) || mappings == null)
            {
                return;
            }

            foreach (var (parentPropName, childPropName) in mappings)
            {
                if (string.Equals(changedPropertyName, parentPropName, StringComparison.Ordinal))
                {
                    OnPropertyChanged(childPropName);
                }
            }
        }
    }
}
