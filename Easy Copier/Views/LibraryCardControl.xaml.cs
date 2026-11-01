using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
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
    /// Reusable card control for displaying a library entry (game, application, TV/film, or OS image)
    /// within grid views, encapsulating thumbnail presentation, metadata badges, hover video previews,
    /// and contextual flyout interactions.
    /// </summary>
    public sealed partial class LibraryCardControl : UserControl
    {
        /// <summary>
        /// Delay before triggering hover video preview so quick cursor passes do not spawn playback instances.
        /// </summary>
        private const int PreviewDelayMs = 500;

        /// <summary>
        /// File extensions evaluated as playable video previews.
        /// </summary>
        private static readonly HashSet<string> PreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".m4v", ".webm"
        };

        private CancellationTokenSource? _previewCts;
        private MediaPlayerElement? _previewElement;

        /// <summary>
        /// Identifies the <see cref="Entry"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EntryProperty =
            DependencyProperty.Register(
                nameof(Entry),
                typeof(GameEntry),
                typeof(LibraryCardControl),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the library entry model represented by this card.
        /// </summary>
        public GameEntry? Entry
        {
            get => (GameEntry?)GetValue(EntryProperty);
            set => SetValue(EntryProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryCardControl"/> class.
        /// </summary>
        public LibraryCardControl()
        {
            InitializeComponent();

            // When used in an ItemsControl/GridView DataTemplate without explicit property binding,
            // synchronize the DataContext into Entry so that x:Bind can reflect data changes.
            DataContextChanged += (s, e) =>
            {
                if (DataContext is GameEntry entry)
                {
                    Entry = entry;
                }
            };

            // Release any active video playback resources when this card is detached from the visual tree
            Unloaded += (s, e) => StopPreview();
        }

        /// <summary>
        /// Reveals the open folder action button when the cursor hovers over the card.
        /// </summary>
        private void Card_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            OpenFolderButton.Opacity = 1.0;
        }

        /// <summary>
        /// Hides the open folder action button when the cursor leaves the card.
        /// </summary>
        private void Card_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            OpenFolderButton.Opacity = 0.0;
        }

        /// <summary>
        /// Handles the Click event for the Open Folder button to reveal the item in File Explorer.
        /// </summary>
        /// <param name="sender">The button control triggering the click.</param>
        /// <param name="e">The routed event arguments.</param>
        private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure button's DataContext has the item entry even if not yet inherited
            if (sender is FrameworkElement fe && fe.DataContext == null && Entry != null)
            {
                fe.DataContext = Entry;
            }

            FlyoutHelper.HandleOpenFolderClick(sender);
        }

        /// <summary>
        /// Handles the RightTapped event on the card to present the category-appropriate details flyout.
        /// </summary>
        /// <param name="sender">The framework element triggering the right-tap.</param>
        /// <param name="e">The right-tapped routed event args containing input coordinates.</param>
        private async void Card_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            await FlyoutHelper.ShowDetailsFlyoutAsync(sender, e, Entry);
        }

        /// <summary>
        /// Starts a delayed hover video preview when the pointer enters the cover area of a TV/Film item.
        /// </summary>
        /// <param name="sender">The cover area grid element.</param>
        /// <param name="e">Pointer event arguments describing the device and pointer state.</param>
        private async void CoverArea_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Only TV and Film entries support interactive video hover previews
            if (Entry?.Category != LibraryCategory.TvAndFilm ||
                e.Pointer.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                return;
            }

            StopPreview();
            CancellationTokenSource cts = new();
            _previewCts = cts;

            MediaPlayer? player = null;
            try
            {
                // Delay preview start to avoid unnecessary disk I/O on rapid cursor movements
                await Task.Delay(PreviewDelayMs, cts.Token);
                string? folderPath = Entry.FolderPath;
                if (string.IsNullOrEmpty(folderPath))
                {
                    return;
                }

                string? videoPath = await Task.Run(() => FindPreviewVideo(folderPath), cts.Token);
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

                // Skip initial intro or logos once total media duration is known
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

                MediaPlayerElement element = new()
                {
                    Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black),
                    AreTransportControlsEnabled = false,
                    IsHitTestVisible = false
                };
                element.SetMediaPlayer(player);

                // Insert below the badge and open-folder button so interactive elements stay clickable
                CoverAreaGrid.Children.Insert(Math.Min(2, CoverAreaGrid.Children.Count), element);
                _previewElement = element;
                player = null;
            }
            catch (OperationCanceledException)
            {
                // Pointer left before preview initialization completed
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hover video preview failed: {ex.Message}");
            }
            finally
            {
                player?.Dispose();
            }
        }

        /// <summary>
        /// Stops the hover video preview when the pointer leaves the cover art.
        /// </summary>
        /// <param name="sender">The cover area grid element.</param>
        /// <param name="e">Pointer event arguments.</param>
        private void CoverArea_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            StopPreview();
        }

        /// <summary>
        /// Cancels any pending preview task and disposes the active media player element.
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
                _ = CoverAreaGrid.Children.Remove(_previewElement);
                _previewElement.MediaPlayer?.Dispose();
                _previewElement.SetMediaPlayer(null);
                _previewElement = null;
            }
        }

        /// <summary>
        /// Resolves the first playable video file for an entry path, whether a direct file or directory.
        /// </summary>
        /// <param name="path">The entry folder or file path.</param>
        /// <returns>Full file path to the preview video, or <c>null</c> if none discovered.</returns>
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
                // External paths may have permission restrictions or transient I/O locks
                return null;
            }
        }
    }
}

