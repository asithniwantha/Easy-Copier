using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl representing the Games tab in the library view.
    /// </summary>
    public sealed partial class GamesTabView : UserControl, ILibraryTabView
    {
        /// <summary>
        /// Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(GamesTabViewModel),
                typeof(GamesTabView),
                new PropertyMetadata(null, OnViewModelPropertyChanged));

        /// <summary>
        /// Gets or sets the <see cref="GamesTabViewModel"/> for this control.
        /// </summary>
        public GamesTabViewModel? ViewModel
        {
            get => (GamesTabViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// Event raised when the selection in the games grid changes.
        /// </summary>
        public event SelectionChangedEventHandler? SelectionChanged;

        /// <summary>
        /// Gets the selected items from the games grid.
        /// </summary>
        public IList<object> SelectedItems => GamesGridView.SelectedItems;

        /// <inheritdoc />
        public bool IsOsImagesTab => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="GamesTabView"/> class.
        /// </summary>
        public GamesTabView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries()
        {
            return GamesGridView.GetSelectedEntries();
        }

        /// <inheritdoc />
        public void ClearSelection()
        {
            GamesGridView.ClearMultiSelection();
        }

        /// <summary>
        /// Handles changes to the <see cref="ViewModelProperty"/> to hook clear selection notifications.
        /// </summary>
        private static void OnViewModelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GamesTabView view)
            {
                if (e.OldValue is GamesTabViewModel oldVm)
                {
                    oldVm.ClearSelectionRequested -= view.OnClearSelectionRequested;
                }

                if (e.NewValue is GamesTabViewModel newVm)
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
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the games grid, updating the coordinator and raising local events.
        /// </summary>
        private void GamesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel?.UpdateSelection(GamesGridView.GetSelectedEntries());
            SelectionChanged?.Invoke(this, e);
        }
    }
}
