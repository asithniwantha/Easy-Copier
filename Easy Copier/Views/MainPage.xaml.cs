using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.Services;
using Easy_Copier.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Easy_Copier.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; }

        public MainPage()
        {
            ViewModel = AppServiceLocator.GetService<MainViewModel>();
            InitializeComponent();
            DataContext = ViewModel;

            ViewModel.ItemQueued += (s, e) => ClearGameSelection();

            _ = ViewModel.InitializeAsync();
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

        private void UpdateCombinedSelection()
        {
            IEnumerable<GameEntry> selectedItems = GamesGridView.SelectedItems.Cast<GameEntry>()
                .Concat(AppsGridView.SelectedItems.Cast<GameEntry>());
            ViewModel.UpdateSelectionSummary(selectedItems);
        }

        private void ClearGameSelection()
        {
            GamesGridView.SelectedItems.Clear();
            AppsGridView.SelectedItems.Clear();
        }

        private string FormatRequirementsText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            text = Regex.Replace(text, @"(?<!\n)\s*Processor:", "\nCPU:");
            text = Regex.Replace(text, @"(?<!\n)\s*Graphics:", "\nGPU:");
            text = Regex.Replace(text, @"(?<!\n)\s*Memory:", "\nRAM:");
            text = Regex.Replace(text, @"(?<!\n)\s*OS\s*\*?:", "\nOS:");
            text = Regex.Replace(text, @"(?<!\n)\s*Storage:", "\nStorage:");
            text = Regex.Replace(text, @"(?<!\n)\s*DirectX:", "\nDirectX:");
            text = Regex.Replace(text, @"(?<!\n)\s*Sound Card:", "\nSound Card:");
            text = Regex.Replace(text, @"(?<!\n)\s*VR Support:", "\nVR Support:");
            text = Regex.Replace(text, @"(?<!\n)\s*Additional Notes:", "\nAdditional Notes:");
            text = Regex.Replace(text, @"(?<!\n)\s*Requires a 64-bit processor and operating system", "\nRequires a 64-bit processor and operating system");

            // Remove the spurious "CPU:" before "Minimum:" and "Recommended:" if it exists
            text = Regex.Replace(text, @"CPU:\s*Minimum:", "Minimum:");
            text = Regex.Replace(text, @"CPU:\s*Recommended:", "Recommended:");

            text = Regex.Replace(text, @"(?<!\n)\s*Minimum:", "\nMinimum:");
            text = Regex.Replace(text, @"(?<!\n)\s*Recommended:", "\nRecommended:");
            text = text.Replace("&amp;", "&");
            return text;
        }

        private Paragraph CreateColoredParagraph(string text)
        {
            Paragraph paragraph = new();

            // Split the text by lines to process each line and add formatting
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (line.StartsWith("CPU:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "CPU:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 255)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[4..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 149, 237)) });
                }
                else if (line.StartsWith("GPU:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "GPU:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[4..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 100, 100)) });
                }
                else if (line.StartsWith("RAM:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "RAM:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[4..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 205, 50)) });
                }
                else if (line.StartsWith("Storage:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "Storage:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[8..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 200, 100)) });
                }
                else if (line.StartsWith("OS:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "OS:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 0, 128)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[3..] + "\n", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 186, 85, 211)) });
                }
                else if (line.StartsWith("Minimum:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "Minimum:", FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[8..] + "\n" });
                }
                else if (line.StartsWith("Recommended:"))
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

        private UIElement CreateFolderContentsView(string folderPath)
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
                        stackPanel.Children.Add(CreateFileFolderItem(System.IO.Path.GetFileName(dir), true));
                    }

                    foreach (string file in files)
                    {
                        stackPanel.Children.Add(CreateFileFolderItem(System.IO.Path.GetFileName(file), false));
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
                MaxWidth = 300,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(12)
            };
        }

        private UIElement CreateFileFolderItem(string name, bool isFolder)
        {
            StackPanel itemPanel = new() { Orientation = Orientation.Horizontal, Spacing = 8 };

            FontIcon icon = new()
            {
                Glyph = isFolder ? "\uE8D5" : "\uE7C3", // Folder or Document icon
                FontSize = 16,
                Foreground = isFolder ? new SolidColorBrush(Microsoft.UI.Colors.Gold) : new SolidColorBrush(Microsoft.UI.Colors.Gray)
            };

            TextBlock textBlock = new()
            {
                Text = name,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 250,
                VerticalAlignment = VerticalAlignment.Center
            };

            itemPanel.Children.Add(icon);
            itemPanel.Children.Add(textBlock);

            return itemPanel;
        }

        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameEntry gameEntry)
            {
                ISourceLibraryService sourceLibraryService = AppServiceLocator.GetService<Easy_Copier.Services.ISourceLibraryService>();
                string rawRequirementsText = await sourceLibraryService.GetSystemRequirementsAsync(gameEntry.FolderPath);
                string formattedText = FormatRequirementsText(rawRequirementsText);

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
                    MaxWidth = 400,
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

                Flyout flyout = new()
                {
                    Content = combinedGrid,
                    Placement = FlyoutPlacementMode.RightEdgeAlignedBottom,
                };

                flyout.ShowAt(fe, new FlyoutShowOptions { Position = e.GetPosition(fe) });
            }
        }
    }
}
