using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Linq;

namespace Easy_Copier.Views
{
    public sealed partial class GameDetailsFlyout : UserControl
    {
        public GameDetailsFlyout(string formattedSysReqText, string folderPath)
        {
            InitializeComponent();
            PopulateSysReqs(formattedSysReqText);
            PopulateFolderContents(folderPath);
        }

        private void PopulateSysReqs(string formattedText)
        {
            SysReqTextBlock.Blocks.Add(CreateColoredParagraph(formattedText));
        }

        private static Paragraph CreateColoredParagraph(string text)
        {
            Paragraph paragraph = new();
            if (string.IsNullOrWhiteSpace(text))
            {
                paragraph.Inlines.Add(new Run { Text = "System requirements not available.\n", FontStyle = Windows.UI.Text.FontStyle.Italic, Opacity = 0.5 });
                return paragraph;
            }

            string[] lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (line.StartsWith("CPU:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "CPU:", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0, 0, 255)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[4..] + "\n", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 149, 237)) });
                }
                else if (line.StartsWith("GPU:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "GPU:", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 0, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[4..] + "\n", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 100, 100)) });
                }
                else if (line.StartsWith("RAM:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "RAM:", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0, 128, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[4..] + "\n", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 50, 205, 50)) });
                }
                else if (line.StartsWith("Storage:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "Storage:", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 165, 0)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[8..] + "\n", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 200, 100)) });
                }
                else if (line.StartsWith("OS:", StringComparison.Ordinal))
                {
                    paragraph.Inlines.Add(new Run { Text = "OS:", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 128, 0, 128)), FontWeight = FontWeights.Bold });
                    paragraph.Inlines.Add(new Run { Text = line[3..] + "\n", Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 186, 85, 211)) });
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

        private void PopulateFolderContents(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    IOrderedEnumerable<string> dirs = Directory.GetDirectories(folderPath).OrderBy(d => d);
                    IOrderedEnumerable<string> files = Directory.GetFiles(folderPath).OrderBy(f => f);

                    foreach (string dir in dirs)
                    {
                        FolderContentsPanel.Children.Add(CreateFileFolderItem(dir, true));
                    }

                    foreach (string file in files)
                    {
                        FolderContentsPanel.Children.Add(CreateFileFolderItem(file, false));
                    }

                    if (FolderContentsPanel.Children.Count == 0)
                    {
                        FolderContentsPanel.Children.Add(new TextBlock { Text = "Empty folder", Opacity = 0.5, FontStyle = Windows.UI.Text.FontStyle.Italic });
                    }
                }
                else
                {
                    FolderContentsPanel.Children.Add(new TextBlock { Text = "Folder not found", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) });
                }
            }
            catch (Exception ex)
            {
                FolderContentsPanel.Children.Add(new TextBlock { Text = $"Error loading folder: {ex.Message}", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) });
            }
        }

        private Grid CreateFileFolderItem(string path, bool isFolder)
        {
            string name = Path.GetFileName(path);
            Grid itemGrid = new() { ColumnSpacing = 8 };
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            FontIcon icon = new()
            {
                Glyph = isFolder ? "\uE8D5" : "\uE7C3",
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
                        long size = Easy_Copier.Infrastructure.FileSystemHelpers.CalculateDirectorySize(new DirectoryInfo(path));
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
                    long size = new FileInfo(path).Length;
                    sizeBlock.Text = Easy_Copier.Infrastructure.FormattingHelpers.FormatBytes(size);
                }
                catch
                {
                    sizeBlock.Text = "Unknown";
                }
            }

            return itemGrid;
        }
    }
}
