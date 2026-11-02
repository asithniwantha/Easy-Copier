using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl representing the OS Images tab in the library view.
    /// </summary>
    public sealed partial class OsImagesTabView : UserControl, ILibraryTabView
    {
        /// <summary>
        /// Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(OsImagesTabViewModel),
                typeof(OsImagesTabView),
                new PropertyMetadata(null, OnViewModelPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="OsImagesTabViewModel"/> for this control.
        /// </summary>
        public OsImagesTabViewModel? ViewModel
        {
            get => (OsImagesTabViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// Event raised when the selection in the OS Images grid changes.
        /// </summary>
        public event SelectionChangedEventHandler? SelectionChanged;

        /// <summary>
        /// Gets the selected items from the OS Images grid.
        /// </summary>
        public IList<object> SelectedItems => OsImagesGridView.SelectedItems;

        /// <inheritdoc />
        public bool IsOsImagesTab => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="OsImagesTabView"/> class.
        /// </summary>
        public OsImagesTabView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries()
        {
            return OsImagesGridView.GetSelectedEntries();
        }

        /// <inheritdoc />
        public void ClearSelection()
        {
            OsImagesGridView.ClearSingleSelection();
        }

        /// <summary>
        /// Handles changes to the <see cref="ViewModelProperty"/> to hook clear selection notifications.
        /// </summary>
        private static void OnViewModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is OsImagesTabView view)
            {
                if (e.OldValue is OsImagesTabViewModel oldVm)
                {
                    oldVm.ClearSelectionRequested -= view.OnClearSelectionRequested;
                }

                if (e.NewValue is OsImagesTabViewModel newVm)
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
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the OS Images grid, updating the coordinator and raising local events.
        /// </summary>
        private void OsImagesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel?.UpdateSelection(OsImagesGridView.GetSelectedEntries());
            SelectionChanged?.Invoke(this, e);
        }
    }
}
