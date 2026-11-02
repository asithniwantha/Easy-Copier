using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl representing the Applications tab in the library view.
    /// </summary>
    public sealed partial class AppsTabView : UserControl, ILibraryTabView
    {
        /// <summary>
        /// Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(AppsTabViewModel),
                typeof(AppsTabView),
                new PropertyMetadata(null, OnViewModelPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="AppsTabViewModel"/> for this control.
        /// </summary>
        public AppsTabViewModel? ViewModel
        {
            get => (AppsTabViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// Event raised when the selection in the apps grid changes.
        /// </summary>
        public event SelectionChangedEventHandler? SelectionChanged;

        /// <summary>
        /// Gets the selected items from the apps grid.
        /// </summary>
        public IList<object> SelectedItems => AppsGridView.SelectedItems;

        /// <inheritdoc />
        public bool IsOsImagesTab => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppsTabView"/> class.
        /// </summary>
        public AppsTabView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries()
        {
            return AppsGridView.GetSelectedEntries();
        }

        /// <inheritdoc />
        public void ClearSelection()
        {
            AppsGridView.ClearMultiSelection();
        }

        /// <summary>
        /// Handles changes to the <see cref="ViewModelProperty"/> to hook clear selection notifications.
        /// </summary>
        private static void OnViewModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AppsTabView view)
            {
                if (e.OldValue is AppsTabViewModel oldVm)
                {
                    oldVm.ClearSelectionRequested -= view.OnClearSelectionRequested;
                }

                if (e.NewValue is AppsTabViewModel newVm)
                {
                    newVm.ClearSelectionRequested += view.OnClearSelectionRequested;
                }
            }
        }

        /// <summary>
        /// Handles clear selection requests from the ViewModel.
        /// </summary>
        private void OnClearSelectionRequested(object? sender, System.EventArgs e)
        {
            ClearSelection();
        }

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the applications grid, updating the coordinator and raising local events.
        /// </summary>
        private void AppsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel?.UpdateSelection(AppsGridView.GetSelectedEntries());
            SelectionChanged?.Invoke(this, e);
        }
    }
}
