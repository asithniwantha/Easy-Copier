using Easy_Copier.Models;
using Easy_Copier.Services;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Implements <see cref="IFlyoutService"/> to present item details flyouts and handle folder launch interactions in UI infrastructure.
    /// </summary>
    public class FlyoutService : IFlyoutService
    {
        /// <summary>
        /// Factory delegate for creating instances of <see cref="GameDetailsViewModel"/>.
        /// </summary>
        private readonly Func<GameDetailsViewModel> _gameDetailsViewModelFactory;

        /// <summary>
        /// Factory delegate for creating instances of <see cref="OsImageDetailsViewModel"/>.
        /// </summary>
        private readonly Func<OsImageDetailsViewModel> _osImageDetailsViewModelFactory;

        /// <summary>
        /// Factory delegate for creating instances of <see cref="VideoDetailsViewModel"/>.
        /// </summary>
        private readonly Func<VideoDetailsViewModel> _videoDetailsViewModelFactory;

        /// <summary>
        /// Service for retrieving and formatting system requirements for games.
        /// </summary>
        private readonly IGameRequirementsService _gameRequirementsService;

        /// <summary>
        /// Service for launching external processes and opening File Explorer folders.
        /// </summary>
        private readonly IProcessService _processService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FlyoutService"/> class.
        /// </summary>
        /// <param name="gameDetailsViewModelFactory">Factory delegate for resolving <see cref="GameDetailsViewModel"/> instances.</param>
        /// <param name="osImageDetailsViewModelFactory">Factory delegate for resolving <see cref="OsImageDetailsViewModel"/> instances.</param>
        /// <param name="videoDetailsViewModelFactory">Factory delegate for resolving <see cref="VideoDetailsViewModel"/> instances.</param>
        /// <param name="gameRequirementsService">Service for retrieving and formatting game system requirements.</param>
        /// <param name="processService">Service for launching applications and opening folders in File Explorer.</param>
        public FlyoutService(
            Func<GameDetailsViewModel> gameDetailsViewModelFactory,
            Func<OsImageDetailsViewModel> osImageDetailsViewModelFactory,
            Func<VideoDetailsViewModel> videoDetailsViewModelFactory,
            IGameRequirementsService gameRequirementsService,
            IProcessService processService)
        {
            _gameDetailsViewModelFactory = gameDetailsViewModelFactory ?? throw new ArgumentNullException(nameof(gameDetailsViewModelFactory));
            _osImageDetailsViewModelFactory = osImageDetailsViewModelFactory ?? throw new ArgumentNullException(nameof(osImageDetailsViewModelFactory));
            _videoDetailsViewModelFactory = videoDetailsViewModelFactory ?? throw new ArgumentNullException(nameof(videoDetailsViewModelFactory));
            _gameRequirementsService = gameRequirementsService ?? throw new ArgumentNullException(nameof(gameRequirementsService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
        }

        /// <inheritdoc />
        public void HandleOpenFolderClick(object sender)
        {
            if (sender is Button { DataContext: GameEntry gameEntry } && !string.IsNullOrEmpty(gameEntry.FolderPath))
            {
                _processService.OpenInExplorer(gameEntry.FolderPath);
            }
        }

        /// <inheritdoc />
        public async Task ShowGameDetailsFlyoutAsync(object sender, Point? position)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not GameEntry gameEntry)
            {
                return;
            }

            string formattedText = await _gameRequirementsService.GetFormattedRequirementsAsync(gameEntry.FolderPath);
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

        /// <inheritdoc />
        public async Task ShowVideoDetailsFlyoutAsync(object sender, Point? position)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not GameEntry entry)
            {
                return;
            }

            VideoDetailsViewModel videoVm = _videoDetailsViewModelFactory();
            Views.VideoDetailsFlyout videoFlyout = new(videoVm);

            // Show immediately, then fill in the file list so large folders don't delay the flyout.
            PresentFlyout(fe, position, videoFlyout, null);
            await videoVm.InitializeAsync(entry.Name, entry.FolderPath);
        }

        /// <summary>
        /// Displays a WinUI flyout anchored
        /// </summary>
        /// <param name="targetElement">The target element to anchor the flyout to.</param>
        /// <param name="position">Optional coordinates relative to the target element for flyout placement.</param>
        /// <param name="content">The UI element content to display inside the flyout.</param>
        /// <param name="presenterStyle">Optional custom presenter style to apply to the flyout.</param>
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
