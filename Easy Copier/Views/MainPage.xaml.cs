using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Views
{
    public sealed partial class MainPage : Page
    {
        private static readonly string[] LineSeparators = ["\r\n", "\n"];

        public MainViewModel? ViewModel { get; private set; }

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);

            base.OnNavigatedTo(e);
            if (e.Parameter is MainViewModel viewModel)
            {
                ViewModel = viewModel;
                DataContext = ViewModel;
                Bindings.Update();

                viewModel.ItemQueued += (s, ev) => ClearGameSelection();

                _ = viewModel.InitializeAsync();
            }
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            ClearGameSelection();
        }

        private void GamesGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private void AppsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private void TvAndFilmsGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCombinedSelection();
        }

        private void UpdateCombinedSelection()
        {
            IEnumerable<GameEntry> tvItems = TvAndFilmsGridView?.SelectedItems.Cast<GameEntry>() ?? [];
            IEnumerable<GameEntry> selectedItems = GamesGridView.SelectedItems.Cast<GameEntry>()
                .Concat(AppsGridView.SelectedItems.Cast<GameEntry>())
                .Concat(tvItems);
            ViewModel?.UpdateSelectionSummary(selectedItems);
        }

        private void ClearGameSelection()
        {
            GamesGridView.SelectedItems.Clear();
            AppsGridView.SelectedItems.Clear();
        }

        private static Paragraph CreateColoredParagraph(string text)
        {
            Paragraph paragraph = new();

            // Split the text by lines to process each line and add formatting
            string[] lines = text.Split(LineSeparators, StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (line.StartsWith("CPU:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "CPU:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 255)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[4..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 149, 237)) });
                }
                else if (line.StartsWith("GPU:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "GPU:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[4..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 100, 100)) });
                }
                else if (line.StartsWith("RAM:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "RAM:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[4..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 205, 50)) });
                }
                else if (line.StartsWith("Storage:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "Storage:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[8..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 200, 100)) });
                }
                else if (line.StartsWith("OS:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "OS:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 0, 128)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[3..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 186, 85, 211)) });
                }
                else if (line.StartsWith("Minimum:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "Minimum:", FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[8..] + "\n" });
                }
                else if (line.StartsWith("Recommended:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "Recommended:", FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[12..] + "\n" });
                }
                else
                {
                    paragraph.Inlines.Add(new Run { Text = line + "\n" });
                }
            }

            return paragraph;
        }

        private ScrollViewer CreateFolderContentsView(string folderPath)
        {
            StackPanel stackPanel = new() { Spacing = 4 };

            try
            {
                if (System.IO.Directory.Exists(folderPath))
                {
                    IOrderedEnumerable<string> dirs = System.IO.Directory.GetDirectories(folderPath).OrderBy(d => d);
                    IOrderedEnumerable<string> files = System.IO.Directory.GetFiles(folderPath).OrderBy(f => f);

                    foreach (string dir in dirs)
                    {
                        stackPanel.Children.Add(CreateFileFolderItem(dir, true));
                    }

                    foreach (string file in files)
                    {
                        stackPanel.Children.Add(CreateFileFolderItem(file, false));
                    }

                    if (stackPanel.Children.Count == 0)
                    {
                        stackPanel.Children.Add(new TextBlock { Text = "Empty folder", Opacity = 0.5, FontStyle = Windows.UI.Text.FontStyle.Italic });
                    }
                }
                else
                {
                    stackPanel.Children.Add(new TextBlock { Text = "Folder not found", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) });
                }
            }
            catch (Exception ex)
            {
                stackPanel.Children.Add(new TextBlock { Text = $"Error loading folder: {ex.Message}", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) });
            }

            return new ScrollViewer
            {
                Content = stackPanel,
                MaxHeight = 400,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(12)
            };
        }

        private Grid CreateFileFolderItem(string path, bool isFolder)
        {
            string name = System.IO.Path.GetFileName(path);
            Grid itemGrid = new() { ColumnSpacing = 8 };
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            FontIcon icon = new()
            {
                Glyph = isFolder ? "\uE8D5" : "\uE7C3", // Folder or Document icon
                FontSize = 16,
                Foreground = isFolder ? new SolidColorBrush(Microsoft.UI.Colors.Gold) : new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };
            Grid.SetColumn(icon, 0);

            TextBlock nameBlock = new()
            {
                Text = name,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 250,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(nameBlock, 1);

            TextBlock sizeBlock = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.6,
                FontSize = 12
            };
            Grid.SetColumn(sizeBlock, 2);

            itemGrid.Children.Add(icon);
            itemGrid.Children.Add(nameBlock);
            itemGrid.Children.Add(sizeBlock);

            if (isFolder)
            {
                sizeBlock.Text = "Calculating...";
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        long size = CalculateDirectorySize(new System.IO.DirectoryInfo(path));
                        _ = DispatcherQueue.TryEnqueue(() =>
                        {
                            sizeBlock.Text = Easy_Copier.Infrastructure.FormattingHelpers.FormatBytes(size);
                        });
                    }
                    catch
                    {
                        _ = DispatcherQueue.TryEnqueue(() =>
                        {
                            sizeBlock.Text = "Unknown";
                        });
                    }
                });
            }
            else
            {
                try
                {
                    long size = new System.IO.FileInfo(path).Length;
                    sizeBlock.Text = Easy_Copier.Infrastructure.FormattingHelpers.FormatBytes(size);
                }
                catch
                {
                    sizeBlock.Text = "Unknown";
                }
            }

            return itemGrid;
        }

        private static long CalculateDirectorySize(System.IO.DirectoryInfo directoryInfo)
        {
            long size = 0;
            try
            {
                System.IO.FileInfo[] files = directoryInfo.GetFiles();
                foreach (System.IO.FileInfo file in files)
                {
                    size += file.Length;
                }

                System.IO.DirectoryInfo[] subDirs = directoryInfo.GetDirectories();
                foreach (System.IO.DirectoryInfo dir in subDirs)
                {
                    size += CalculateDirectorySize(dir);
                }
            }
            catch
            {
                // Ignore access errors
            }
            return size;
        }

        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameEntry gameEntry && ViewModel is not null)
            {
                string formattedText = await ViewModel.GetFormattedSystemRequirementsAsync(gameEntry.FolderPath);

                RichTextBlock richTextBlock = new()
                {
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
                };

                richTextBlock.Blocks.Add(CreateColoredParagraph(formattedText));

                ScrollViewer sysReqScrollViewer = new()
                {
                    Content = richTextBlock,
                    MaxHeight = 400,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Padding = new Thickness(12)
                };

                UIElement folderContentsView = CreateFolderContentsView(gameEntry.FolderPath);

                Grid combinedGrid = new();
                combinedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                combinedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Pixel) });
                combinedGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Grid.SetColumn((FrameworkElement)folderContentsView, 0);

                Border separator = new()
                {
                    Background = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    Width = 1,
                    Margin = new Thickness(8, 12, 8, 12),
                    Opacity = 0.5
                };
                Grid.SetColumn(separator, 1);

                Grid.SetColumn(sysReqScrollViewer, 2);

                combinedGrid.Children.Add(folderContentsView);
                combinedGrid.Children.Add(separator);
                combinedGrid.Children.Add(sysReqScrollViewer);

                Style flyoutStyle = new(typeof(FlyoutPresenter));
                flyoutStyle.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, double.PositiveInfinity));

                Flyout flyout = new()
                {
                    Content = combinedGrid,
                    Placement = FlyoutPlacementMode.RightEdgeAlignedTop,
                    FlyoutPresenterStyle = flyoutStyle
                };

                flyout.ShowAt(fe, new FlyoutShowOptions { Position = e.GetPosition(fe) });
            }
        }
    }
}
