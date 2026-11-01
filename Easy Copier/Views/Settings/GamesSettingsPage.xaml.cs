using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace Easy_Copier.Views.Settings
{
    /// <summary>
    /// Represents the games settings page in the settings navigation.
    /// </summary>
    public sealed partial class GamesSettingsPage : Page
    {
        /// <summary>
        /// Gets the settings view model associated with this page.
        /// </summary>
        public SettingsViewModel? ViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GamesSettingsPage"/> class.
        /// </summary>
        public GamesSettingsPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <inheritdoc/>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);
            base.OnNavigatedTo(e);
            if (e.Parameter is SettingsViewModel vm)
            {
                ViewModel = vm;
                DataContext = vm;
                Bindings.Update();
            }
        }
    }
}
