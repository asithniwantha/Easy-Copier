using Easy_Copier.Models;
using Easy_Copier.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Implements UI dialog display services using WinUI 3 ContentDialog controls.
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly IWindowService _windowService;
        private readonly IAppWindowContext _appWindowContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogService"/> class.
        /// </summary>
        /// <param name="windowService">The window service dependency.</param>
        /// <param name="appWindowContext">The main application window context provider.</param>
        public DialogService(IWindowService windowService, IAppWindowContext appWindowContext)
        {
            _windowService = windowService;
            _appWindowContext = appWindowContext;
        }

        /// <summary>
        /// Displays a folder conflict resolution dialog allowing the user to select how to handle duplicate items.
        /// </summary>
        /// <param name="itemName">The name of the conflicting item.</param>
        /// <param name="srcSize">The total byte size of the source item.</param>
        /// <param name="srcCount">The total file count in the source item.</param>
        /// <param name="destSize">The total byte size of the existing destination item.</param>
        /// <param name="destCount">The total file count in the existing destination item.</param>
        /// <returns>A tuple containing the selected <see cref="CopyAction"/> and a boolean indicating if the action applies to all subsequent conflicts.</returns>
        public async Task<(CopyAction Action, bool ApplyToAll)> ShowConflictDialogAsync(string itemName, long srcSize, int srcCount, long destSize, int destCount)
        {
            if (_appWindowContext.MainXamlRoot is not XamlRoot xamlRoot)
            {
                return (CopyAction.Skip, false); // Fallback if no window
            }

            ConflictDialogContent dialogContent = new(itemName, srcSize, srcCount, destSize, destCount);

            ContentDialog dialog = new()
            {
                Title = "Folder Conflict",
                Content = dialogContent,
                PrimaryButtonText = "Replace Everything",
                SecondaryButtonText = "Merge",
                CloseButtonText = "Skip",
                XamlRoot = xamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };

            ContentDialogResult result = await dialog.ShowAsync();
            bool applyToAll = dialogContent.IsApplyToAllChecked;
            CopyAction selectedAction = result switch
            {
                ContentDialogResult.Primary => CopyAction.Replace,
                ContentDialogResult.Secondary => CopyAction.Merge,
                _ => CopyAction.Skip
            };

            return (selectedAction, applyToAll);
        }

        /// <summary>
        /// Displays a standard modal message dialog with a title, message body, and close button text.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The body text of the message.</param>
        /// <param name="closeButtonText">The text displayed on the close button. Defaults to "OK".</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
