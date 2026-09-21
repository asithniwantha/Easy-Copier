using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Easy_Copier.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Service managing right-click card flyouts and folder launch interactions for library views.
    /// </summary>
    public class FlyoutService : IFlyoutService
    {
        /// <inheritdoc />
        public void HandleOpenFolderClick(object sender, MainViewModel? mainViewModel)
        {
            if (sender is Button { DataContext: GameEntry gameEntry } && mainViewModel != null)
            {
                mainViewModel.OpenItemFolderCommand.Execute(gameEntry.FolderPath);
            }
        }

        /// <inheritdoc />
        public async Task ShowGameDetailsFlyoutAsync(object sender, RightTappedRoutedEventArgs e, MainViewModel? mainViewModel)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (sender is not FrameworkElement fe || fe.DataContext is not GameEntry gameEntry || mainViewModel == null)
            {
                return;
            }

            string formattedText = await mainViewModel.GetFormattedSystemRequirementsAsync(gameEntry.FolderPath);
            GameDetailsViewModel gameDetailsViewModel = mainViewModel.CreateGameDetailsViewModel();
            GameDetailsFlyout detailsFlyout = new(gameDetailsViewModel, formattedText, gameEntry.FolderPath);

            Style flyoutStyle = new(typeof(FlyoutPresenter));
            flyoutStyle.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, double.PositiveInfinity));

            PresentFlyout(fe, e, detailsFlyout, flyoutStyle);
        }

        /// <inheritdoc />
        public void ShowOsImageDetailsFlyout(object sender, RightTappedRoutedEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            if (sender is not FrameworkElement fe || fe.DataContext is not GameEntry gameEntry)
            {
                return;
            }

            OsImageDetailsViewModel osImageVm = new();
            osImageVm.Initialize(gameEntry.Name, gameEntry.FolderPath);
            OsImageDetailsFlyout osImageFlyout = new(osImageVm);

            PresentFlyout(fe, e, osImageFlyout, null);
        }

        private static void PresentFlyout(FrameworkElement targetElement, RightTappedRoutedEventArgs e, UIElement content, Style? presenterStyle)
        {
            Flyout flyout = new()
            {
                Content = content,
                Placement = FlyoutPlacementMode.RightEdgeAlignedTop
            };

            if (presenterStyle != null)
            {
                flyout.FlyoutPresenterStyle = presenterStyle;
            }

            flyout.ShowAt(targetElement, new FlyoutShowOptions { Position = e.GetPosition(targetElement) });
        }
    }
}
