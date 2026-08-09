using Easy_Copier.Infrastructure;
using Easy_Copier.Models;
using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Documents;

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
            var selectedItems = GamesGridView.SelectedItems.Cast<GameEntry>()
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
            if (string.IsNullOrWhiteSpace(text)) return text;

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
            var paragraph = new Paragraph();

            // Split the text by lines to process each line and add formatting
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                if (line.StartsWith("CPU:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "CPU:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 255)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line.Substring(4) + "\n" });
                }
                else if (line.StartsWith("GPU:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "GPU:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line.Substring(4) + "\n" });
                }
                else if (line.StartsWith("RAM:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "RAM:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line.Substring(4) + "\n" });
                }
                else if (line.StartsWith("Storage:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "Storage:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 165, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line.Substring(8) + "\n" });
                }
                else if (line.StartsWith("OS:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "OS:", Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 0, 128)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line.Substring(3) + "\n" });
                }
                else if (line.StartsWith("Minimum:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "Minimum:", FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line.Substring(8) + "\n" });
                }
                else if (line.StartsWith("Recommended:"))
                {
                    paragraph.Inlines.Add(new Run { Text = "Recommended:", FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line.Substring(12) + "\n" });
                }
                else
                {
                    paragraph.Inlines.Add(new Run { Text = line + "\n" });
                }
            }

            return paragraph;
        }

        private async void GameCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GameEntry gameEntry)
            {
                var sourceLibraryService = AppServiceLocator.GetService<Easy_Copier.Services.ISourceLibraryService>();
                var rawRequirementsText = await sourceLibraryService.GetSystemRequirementsAsync(gameEntry.FolderPath);
                var formattedText = FormatRequirementsText(rawRequirementsText);

                var richTextBlock = new RichTextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
                };

                richTextBlock.Blocks.Add(CreateColoredParagraph(formattedText));

                var scrollViewer = new ScrollViewer
                {
                    Content = richTextBlock,
                    MaxHeight = 400,
                    MaxWidth = 400,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Padding = new Thickness(12)
                };

                var flyout = new Flyout
                {
                    Content = scrollViewer,
                    Placement = FlyoutPlacementMode.RightEdgeAlignedTop
                };

                flyout.ShowAt(fe, new FlyoutShowOptions { Position = e.GetPosition(fe) });
            }
        }
    }
}
