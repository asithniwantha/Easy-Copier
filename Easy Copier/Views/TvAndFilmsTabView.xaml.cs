using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl representing the Films &amp; TV series tab in the library view.
    /// </summary>
    public sealed partial class TvAndFilmsTabView : UserControl, ILibraryTabView
    {
        /// <summary>
        /// Identifies the <see cref="ViewModel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                nameof(ViewModel),
                typeof(TvAndFilmsTabViewModel),
                typeof(TvAndFilmsTabView),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the <see cref="TvAndFilmsTabViewModel"/> for this control.
        /// </summary>
        public TvAndFilmsTabViewModel? ViewModel
        {
            get => (TvAndFilmsTabViewModel?)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        /// <summary>
        /// Event raised when the selection in the TV and films grid changes.
        /// </summary>
        public event SelectionChangedEventHandler? SelectionChanged;

        /// <summary>
        /// Gets the collection of currently selected items from the TV and films grid.
        /// </summary>
        /// <value>
        /// An <see cref="IList{T}"/> containing the selected objects in the TV and films grid view.
        /// </value>
        public IList<object> SelectedItems => TvAndFilmsGridView.SelectedItems;

        /// <inheritdoc />
        public bool IsOsImagesTab => false;

        private const int PreviewDelayMs = 500;

        private static readonly HashSet<string> PreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v", ".webm"
        };

        private CancellationTokenSource? _previewCts;
        private Grid? _previewHost;
        private MediaPlayerElement? _previewElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="TvAndFilmsTabView"/> class.
        /// </summary>
        public TvAndFilmsTabView()
        {
            InitializeComponent();
        }

        /// <inheritdoc />
        public IEnumerable<GameEntry> GetSelectedEntries()
        {
            return TvAndFilmsGridView.GetSelectedEntries();
        }

        /// <inheritdoc />
        public void ClearSelection()
        {
            TvAndFilmsGridView.ClearMultiSelection();
        }

        /// <summary>
        /// Handles the <see cref="Selector.SelectionChanged"/> event for the TV and films grid.
        /// </summary>
        /// <param name="sender">The source of the selection changed event.</param>
        /// <param name="e">The event data describing selection modifications.</param>
        private void TvAndFilmsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Handles the Click event for the Open Folder button on a TV/film card.
        /// </summary>
        /// <param name="sender">The source of the click event.</param>
        /// <param name="e">The event arguments.</param>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FlyoutHelper.HandleOpenFolderClick(sender);
        }

        /// <summary>
        /// Handles the RightTapped event on a TV/film card to present the details flyout.
        /// </summary>
        /// <param name="sender">The source of the right-tap event.</param>
        /// <param name="e">The event arguments containing input position details.</param>
        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            await FlyoutHelper.ShowVideoDetailsFlyoutAsync(sender, e);
        }

        /// <summary>
        /// Starts a delayed hover preview so quick mouse passes don't spawn players.
        /// </summary>
        private async void CoverArea_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is not Grid { DataContext: GameEntry entry } coverGrid ||
                e.Pointer.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                return;
            }

            StopPreview();
            CancellationTokenSource cts = new();
            _previewCts = cts;
            _previewHost = coverGrid;

            MediaPlayer? player = null;
            try
            {
                await Task.Delay(PreviewDelayMs, cts.Token);
                string? videoPath = await Task.Run(() => FindPreviewVideo(entry.FolderPath), cts.Token);
                if (videoPath is null || cts.IsCancellationRequested)
                {
                    return;
                }

                player = new MediaPlayer
                {
                    IsMuted = true,
                    IsLoopingEnabled = true,
                    AutoPlay = true,
                    Source = MediaSource.CreateFromUri(new Uri(videoPath))
                };

                // Jump past intros/logos once the duration is known.
                player.MediaOpened += (s, _) =>
                {
                    TimeSpan duration = s.PlaybackSession.NaturalDuration;
                    if (duration > TimeSpan.FromSeconds(60))
                    {
                        s.PlaybackSession.Position = duration * 0.2;
                    }
                };

                if (cts.IsCancellationRequested)
                {
                    return;
                }

                // Uniform preserves the aspect ratio; UniformToFill rendered stretched here.
                MediaPlayerElement element = new()
                {
                    Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
                    AreTransportControlsEnabled = false,
                    IsHitTestVisible = false
                };
                element.SetMediaPlayer(player);

                // Insert below the badge and open-folder button so they stay clickable.
                coverGrid.Children.Insert(Math.Min(2, coverGrid.Children.Count), element);
                _previewElement = element;
                player = null;
            }
            catch (OperationCanceledException)
            {
                // Pointer left before the preview started.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hover preview failed: {ex.Message}");
            }
            finally
            {
                player?.Dispose();
            }
        }

        /// <summary>
        /// Stops the hover preview when the pointer leaves the cover art.
        /// </summary>
        private void CoverArea_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            StopPreview();
        }

        /// <summary>
        /// Cancels any pending preview and releases the active player.
        /// </summary>
        private void StopPreview()
        {
            if (_previewCts is not null)
            {
                _previewCts.Cancel();
                _previewCts.Dispose();
                _previewCts = null;
            }

            if (_previewElement is not null)
            {
                _ = _previewHost?.Children.Remove(_previewElement);
                _previewElement.MediaPlayer?.Dispose();
                _previewElement.SetMediaPlayer(null);
                _previewElement = null;
            }

            _previewHost = null;
        }

        /// <summary>
        /// Resolves the first playable video for an entry, which may be a file or a folder.
        /// </summary>
        /// <param name="path">The entry path; external input, so failures yield null.</param>
        /// <returns>The file path to the first playable video file found, or <see langword="null"/> if none exists or the directory cannot be read.</returns>
        private static string? FindPreviewVideo(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    return PreviewExtensions.Contains(Path.GetExtension(path)) ? path : null;
                }

                if (!Directory.Exists(path))
                {
                    return null;
                }

                return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Where(f => PreviewExtensions.Contains(Path.GetExtension(f)))
                    .OrderByDescending(f => new FileInfo(f).Length)
                    .FirstOrDefault();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// Handles text changes in the search box to provide suggestions and update background highlights.
        /// </summary>
        /// <param name="sender">The source <see cref="AutoSuggestBox"/> control.</param>
        /// <param name="args">The event arguments containing text change details.</param>
        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            AutoSuggestBoxHelper.HandleTextChanged(sender, args, ViewModel);
        }

        /// <summary>
        /// Handles the event when a suggestion is chosen from the AutoSuggestBox.
        /// </summary>
        /// <param name="sender">The source <see cref="AutoSuggestBox"/> control.</param>
        /// <param name="args">The event arguments containing chosen suggestion details.</param>
        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            AutoSuggestBoxHelper.HandleSuggestionChosen(args, ViewModel);
        }

        /// <summary>
        /// Handles the event when the user submits a query from the AutoSuggestBox.
        /// </summary>
        /// <param name="sender">The source <see cref="AutoSuggestBox"/> control.</param>
        /// <param name="args">The event arguments containing query submission details.</param>
        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            AutoSuggestBoxHelper.HandleQuerySubmitted(args, ViewModel);
        }
    }
}
