using Easy_Copier.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    public class DialogService : IDialogService
    {
        private readonly IWindowService _windowService;
        private readonly IAppWindowContext _appWindowContext;

        public DialogService(IWindowService windowService, IAppWindowContext appWindowContext)
        {
            _windowService = windowService;
            _appWindowContext = appWindowContext;
        }

        public async Task<(CopyAction Action, bool ApplyToAll)> ShowConflictDialogAsync(string itemName, long srcSize, int srcCount, long destSize, int destCount)
        {
            if (_appWindowContext.MainXamlRoot is not XamlRoot xamlRoot)
            {
                return (CopyAction.Skip, false); // Fallback if no window
            }

            string srcSizeStr = FormattingHelpers.FormatBytes(srcSize);
            string destSizeStr = FormattingHelpers.FormatBytes(destSize);

            StackPanel contentPanel = new() { Spacing = 12 };

            contentPanel.Children.Add(new TextBlock
            {
                Text = $"The destination already contains an item named '{itemName}'.",
                TextWrapping = TextWrapping.Wrap
            });

            Grid statsGrid = new() { ColumnSpacing = 16 };
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Source Stats
            StackPanel srcStats = new();
            srcStats.Children.Add(new TextBlock { Text = "Source:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            srcStats.Children.Add(new TextBlock { Text = $"Size: {srcSizeStr}" });
            srcStats.Children.Add(new TextBlock { Text = $"Files: {srcCount}" });
            Grid.SetColumn(srcStats, 0);
            statsGrid.Children.Add(srcStats);

            // Dest Stats
            StackPanel destStats = new();
            destStats.Children.Add(new TextBlock { Text = "Destination:", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            destStats.Children.Add(new TextBlock { Text = $"Size: {destSizeStr}" });
            destStats.Children.Add(new TextBlock { Text = $"Files: {destCount}" });
            Grid.SetColumn(destStats, 1);
            statsGrid.Children.Add(destStats);

            contentPanel.Children.Add(statsGrid);

            CheckBox applyToAllCheckBox = new() { Content = "Do this for all conflicts" };
            contentPanel.Children.Add(applyToAllCheckBox);

            ContentDialog dialog = new()
            {
                Title = "Folder Conflict",
                Content = contentPanel,
                PrimaryButtonText = "Replace Everything",
                SecondaryButtonText = "Merge",
                CloseButtonText = "Skip",
                XamlRoot = xamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await dialog.ShowAsync();
            bool applyToAll = applyToAllCheckBox.IsChecked ?? false;
            CopyAction selectedAction = result switch
            {
                ContentDialogResult.Primary => CopyAction.Replace,
                ContentDialogResult.Secondary => CopyAction.Merge,
                _ => CopyAction.Skip
            };

            return (selectedAction, applyToAll);
        }

        public async Task ShowMessageDialogAsync(string title, string message, string closeButtonText = "OK")
        {
            if (_appWindowContext.MainXamlRoot is not XamlRoot xamlRoot)
            {
                return;
            }

            ContentDialog dialog = new()
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                CloseButtonText = closeButtonText,
                XamlRoot = xamlRoot
            };

            _ = await dialog.ShowAsync();
        }
    }
}
