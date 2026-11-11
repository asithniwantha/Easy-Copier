using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Represents the main page view housing library tabs and transfer action center.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// Gets the <see cref="MainViewModel"/> bound to this page.
        /// </summary>
        public MainViewModel ViewModel { get; private set; } = null!;

        /// <summary>
        /// Handles the close button click event on the global notification InfoBar.
        /// </summary>
        /// <param name="sender">The InfoBar control sending the event.</param>
        /// <param name="args">Event arguments.</param>
        private void GlobalNotification_CloseButtonClick(InfoBar sender, object args)
        {
            ViewModel?.IsGlobalNotificationVisible = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            base.OnNavigatedTo(e);
            if (e.Parameter is MainViewModel viewModel)
            {
                ViewModel = viewModel;
                DataContext = ViewModel;
                Bindings.Update();

                _ = ViewModel.InitializeAsync();
            }
        }
    }
}
