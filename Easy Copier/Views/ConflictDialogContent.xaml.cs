using Easy_Copier.Infrastructure;
using Microsoft.UI.Xaml.Controls;

namespace Easy_Copier.Views
{
    /// <summary>
    /// UserControl content for folder conflict resolution dialogs.
    /// </summary>
    public sealed partial class ConflictDialogContent : UserControl
    {
        /// <summary>
        /// Gets the description message explaining the folder conflict.
        /// </summary>
        public string ItemNameMessage { get; }

        /// <summary>
        /// Gets the formatted source size string.
        /// </summary>
        public string SourceSizeText { get; }

        /// <summary>
        /// Gets the formatted source file count string.
        /// </summary>
        public string SourceFilesText { get; }

        /// <summary>
        /// Gets the formatted destination size string.
        /// </summary>
        public string DestinationSizeText { get; }

        /// <summary>
        /// Gets the formatted destination file count string.
        /// </summary>
        public string DestinationFilesText { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the selected action should apply to all subsequent conflicts.
        /// </summary>
        public bool IsApplyToAllChecked { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictDialogContent"/> class.
        /// </summary>
        /// <param name="itemName">The name of the conflicting item.</param>
        /// <param name="srcSize">Total byte size of source item.</param>
        /// <param name="srcCount">File count of source item.</param>
        /// <param name="destSize">Total byte size of destination item.</param>
        /// <param name="destCount">File count of destination item.</param>
        public ConflictDialogContent(string itemName, long srcSize, int srcCount, long destSize, int destCount)
        {
            ItemNameMessage = $"The destination already contains an item named '{itemName}'.";
            SourceSizeText = $"Size: {FormattingHelpers.FormatBytes(srcSize)}";
            SourceFilesText = $"Files: {srcCount}";
            DestinationSizeText = $"Size: {FormattingHelpers.FormatBytes(destSize)}";
            DestinationFilesText = $"Files: {destCount}";

            InitializeComponent();
        }
    }
}
