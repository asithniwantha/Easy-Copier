using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Page content displaying transfer history entries within the history window.
    /// </summary>
    public sealed partial class HistoryPage : Page
    {
        /// <summary>
        /// Gets the <see cref="HistoryViewModel"/> associated with this page.
        /// </summary>
        public HistoryViewModel ViewModel { get; private set; } = null!;

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryPage"/> class.
        /// </summary>
        public HistoryPage()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            System.ArgumentNullException.ThrowIfNull(e);

            base.OnNavigatedTo(e);
            if (e.Parameter is HistoryViewModel viewModel)
            {
                ViewModel = viewModel;
                Bindings.Update();
                _ = ViewModel.InitializeAsync();
            }
        }
    }
}
