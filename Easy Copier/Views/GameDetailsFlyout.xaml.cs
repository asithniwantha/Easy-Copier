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
        public ViewModels.GameDetailsViewModel ViewModel { get; }

        public GameDetailsFlyout(ViewModels.GameDetailsViewModel viewModel, string formattedSysReqText, string folderPath)
        {
            ViewModel = viewModel;
            InitializeComponent();
            PopulateSysReqs(formattedSysReqText);
            _ = ViewModel.LoadFolderContentsAsync(folderPath);
        }

        private void PopulateSysReqs(string formattedText)
        {
            SysReqTextBlock.Blocks.Clear();
            SysReqTextBlock.Blocks.Add(CreateColoredParagraph(formattedText));
        }

        private static Paragraph CreateColoredParagraph(string text)
        {
            Paragraph paragraph = new();
            if (string.IsNullOrWhiteSpace(text))
            {
                paragraph.Inlines.Add(new Run { Text = "System requirements not available.\n", FontStyle = Windows.UI.Text.FontStyle.Italic, Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray) });
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
    }
}
