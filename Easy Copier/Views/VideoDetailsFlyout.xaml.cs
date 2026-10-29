using Easy_Copier.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace Easy_Copier.Views
{
    /// <summary>
    /// Flyout content showing the path, files and sizes of a film or TV series folder.
    /// </summary>
    public sealed partial class VideoDetailsFlyout : UserControl
    {
        /// <summary>
        /// Gets the <see cref="VideoDetailsViewModel"/> backing this flyout.
        /// </summary>
        public VideoDetailsViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoDetailsFlyout"/> class.
        /// </summary>
        /// <param name="viewModel">The view model containing the video details.</param>
        public VideoDetailsFlyout(VideoDetailsViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
        }
    }
}
