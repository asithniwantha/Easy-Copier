using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;

namespace Easy_Copier.Services
{
    /// <summary>
    /// Implements <see cref="IFlyoutService"/> to present item details flyouts and handle folder launch interactions.
    /// </summary>
    public class FlyoutService : IFlyoutService
    {
        private readonly IGameRequirementsService _gameRequirementsService;
        private readonly Func<GameDetailsViewModel> _gameDetailsViewModelFactory;
        private readonly Func<OsImageDetailsViewModel> _osImageDetailsViewModelFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlyoutService"/> class.
        /// </summary>
        /// <param name="gameRequirementsService">Service for retrieving and formatting system requirements.</param>
        /// <param name="gameDetailsViewModelFactory">Factory delegate for resolving <see cref="GameDetailsViewModel"/> instances.</param>
        /// <param name="osImageDetailsViewModelFactory">Factory delegate for resolving <see cref="OsImageDetailsViewModel"/> instances.</param>
        public FlyoutService(
            IGameRequirementsService gameRequirementsService,
            Func<GameDetailsViewModel> gameDetailsViewModelFactory,
            Func<OsImageDetailsViewModel> osImageDetailsViewModelFactory)
        {
            _gameRequirementsService = gameRequirementsService ?? throw new ArgumentNullException(nameof(gameRequirementsService));
            _gameDetailsViewModelFactory = gameDetailsViewModelFactory ?? throw new ArgumentNullException(nameof(gameDetailsViewModelFactory));
            _osImageDetailsViewModelFactory = osImageDetailsViewModelFactory ?? throw new ArgumentNullException(nameof(osImageDetailsViewModelFactory));
        }

        /// <inheritdoc />
        public void HandleOpenFolderClick(object sender, ICommand? openFolderCommand)
        {
            if (sender is Button { DataContext: GameEntry gameEntry } && openFolderCommand != null)
            {
                if (openFolderCommand.CanExecute(gameEntry.FolderPath))
                {
                    openFolderCommand.Execute(gameEntry.FolderPath);
                }
            }
        }

        /// <inheritdoc />
        public async Task ShowGameDetailsFlyoutAsync(object sender, Point? position)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not GameEntry gameEntry)
            {
                return;
            }

            string formattedText = await _gameRequirementsService.GetFormattedSystemRequirementsAsync(gameEntry.FolderPath);
            GameDetailsViewModel gameDetailsViewModel = _gameDetailsViewModelFactory();
            Views.GameDetailsFlyout detailsFlyout = new(gameDetailsViewModel, formattedText, gameEntry.FolderPath);

            Style flyoutStyle = new(typeof(FlyoutPresenter));
            flyoutStyle.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, double.PositiveInfinity));

            PresentFlyout(fe, position, detailsFlyout, flyoutStyle);
        }

        /// <inheritdoc />
        public void ShowOsImageDetailsFlyout(object sender, Point? position)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not GameEntry gameEntry)
            {
                return;
            }

            OsImageDetailsViewModel osImageVm = _osImageDetailsViewModelFactory();
            osImageVm.Initialize(gameEntry.Name, gameEntry.FolderPath);
            Views.OsImageDetailsFlyout osImageFlyout = new(osImageVm);

            PresentFlyout(fe, position, osImageFlyout, null);
        }

        private static void PresentFlyout(FrameworkElement targetElement, Point? position, UIElement content, Style? presenterStyle)
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

            FlyoutShowOptions? options = position.HasValue ? new FlyoutShowOptions { Position = position.Value } : null;
            flyout.ShowAt(targetElement, options);
        }
    }
}
